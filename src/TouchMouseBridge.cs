using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZzzToolkit;

/// <summary>
/// 触屏模拟：把鼠标动作翻译成真正的 Windows 触摸事件。
///
/// 为什么需要它：绝区零移动端 UI 的虚拟摇杆走的是 Unity 的 Touchscreen 设备
/// （元数据里能看到 Touchscreen / TouchPhase 那一组），鼠标产生不了触摸事件，
/// 所以其他按键能用、唯独摇杆不动。
/// 这里用系统自带的触摸注入 API（InitializeTouchInjection / InjectTouchInput）
/// 在操作系统层面伪造触摸输入，游戏会把它当成真手指。
///
/// 注意：这是操作系统提供的输入注入，**不会向游戏进程注入任何代码**。
/// 只有当目标游戏窗口在前台、且开关打开时才会接管鼠标；其余情况一律放行。
/// </summary>
public sealed class TouchMouseBridge : IDisposable
{
    // ── Win32 ────────────────────────────────────────────────────────────
    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;

    private const uint PT_TOUCH = 0x00000002;
    private const uint POINTER_FLAG_INRANGE = 0x00000002;
    private const uint POINTER_FLAG_INCONTACT = 0x00000004;
    private const uint POINTER_FLAG_PRIMARY = 0x00002000;
    private const uint POINTER_FLAG_DOWN = 0x00010000;
    private const uint POINTER_FLAG_UPDATE = 0x00020000;
    private const uint POINTER_FLAG_UP = 0x00040000;
    private const uint TOUCH_MASK_CONTACTAREA = 0x00000001;
    private const uint TOUCH_MASK_PRESSURE = 0x00000004;
    private const uint TOUCH_FEEDBACK_NONE = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINTER_INFO
    {
        public uint pointerType; public uint pointerId; public uint frameId;
        public uint pointerFlags; public IntPtr sourceDevice; public IntPtr hwndTarget;
        public POINT ptPixelLocation; public POINT ptHimetricLocation;
        public POINT ptPixelLocationRaw; public POINT ptHimetricLocationRaw;
        public uint dwTime; public uint historyCount; public int InputData; public uint dwKeyStates;
        public ulong PerformanceCount; public int ButtonChangeType;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINTER_TOUCH_INFO
    {
        public POINTER_INFO pointerInfo;
        public uint touchFlags; public uint touchMask;
        public RECT rcContact; public RECT rcContactRaw;
        public uint orientation; public uint pressure;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt; public uint mouseData; public uint flags; public uint time; public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool InitializeTouchInjection(uint maxCount, uint dwMode);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool InjectTouchInput(uint count, [In] POINTER_TOUCH_INFO[] contacts);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    // ── 状态 ─────────────────────────────────────────────────────────────
    private readonly LowLevelMouseProc _proc;      // 必须持有引用，否则会被 GC 回收
    private IntPtr _hook = IntPtr.Zero;
    private bool _touchReady;
    private bool _contactDown;
    private int _pointerId = 7;                    // 固定一个触点编号

    /// <summary>只接管这个进程的窗口；为 0 表示不限制。</summary>
    public uint TargetPid { get; set; }

    public bool IsRunning => _hook != IntPtr.Zero;

    /// <summary>已注入的触摸事件数（用于界面上确认它在工作）。</summary>
    public long InjectedCount { get; private set; }

    /// <summary>钩子被系统调用的次数（用来区分"钩子没触发"和"注入了但失败"）。</summary>
    public long HookHits { get; private set; }

    /// <summary>因为目标窗口不在前台而放行的次数。</summary>
    public long PassthroughCount { get; private set; }

    /// <summary>系统是否允许注入触摸。为 false 时说明本机这条路走不通。</summary>
    public bool InjectionAllowed { get; private set; } = true;

    public string? LastError { get; private set; }

    /// <summary>POINTER_TOUCH_INFO 的实际封送大小，x64 下必须是 144，否则系统会报参数无效。</summary>
    public static int ContactStructSize => Marshal.SizeOf<POINTER_TOUCH_INFO>();

    public TouchMouseBridge() => _proc = HookCallback;

    public bool Start(uint targetPid)
    {
        TargetPid = targetPid;
        Stop();

        _touchReady = InitializeTouchInjection(10, TOUCH_FEEDBACK_NONE);
        if (!_touchReady)
        {
            LastError = "系统不允许触摸注入（InitializeTouchInjection 失败，错误码 " +
                        Marshal.GetLastWin32Error() + "）";
            return false;
        }

        _hook = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(null), 0);
        if (_hook == IntPtr.Zero)
        {
            LastError = "安装鼠标钩子失败，错误码 " + Marshal.GetLastWin32Error();
            return false;
        }
        LastError = null;
        return true;
    }

    /// <summary>启动后先试注入一次，用来判断本机到底允不允许触摸注入。</summary>
    public bool SelfTest(int x = 4, int y = 4)
    {
        if (!_touchReady) return false;
        Inject(POINTER_FLAG_DOWN, x, y);
        Inject(POINTER_FLAG_UP, x, y);
        return InjectionAllowed;
    }

    public void Stop()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
        if (_contactDown) { Inject(POINTER_FLAG_UP, 0, 0); _contactDown = false; }
    }

    public void Dispose() => Stop();

    // ── 钩子 ─────────────────────────────────────────────────────────────
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0 || _hook == IntPtr.Zero) return CallNextHookEx(_hook, nCode, wParam, lParam);

        HookHits++;

        // 只有目标游戏窗口在前台时才接管，避免影响平时用电脑
        if (TargetPid != 0)
        {
            IntPtr fg = GetForegroundWindow();
            GetWindowThreadProcessId(fg, out uint pid);
            if (pid != TargetPid)
            {
                PassthroughCount++;
                if (_contactDown) { Inject(POINTER_FLAG_UP, 0, 0); _contactDown = false; }
                return CallNextHookEx(_hook, nCode, wParam, lParam);
            }
        }

        int msg = (int)wParam;
        var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
        int x = info.pt.X, y = info.pt.Y;

        switch (msg)
        {
            case WM_LBUTTONDOWN:
                Inject(POINTER_FLAG_DOWN, x, y);
                _contactDown = true;
                return (IntPtr)1;               // 吞掉，避免鼠标和触摸被处理两次

            case WM_MOUSEMOVE:
                if (_contactDown) { Inject(POINTER_FLAG_UPDATE, x, y); return (IntPtr)1; }
                break;

            case WM_LBUTTONUP:
                if (_contactDown)
                {
                    Inject(POINTER_FLAG_UP, x, y);
                    _contactDown = false;
                    return (IntPtr)1;
                }
                break;
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private void Inject(uint action, int x, int y)
    {
        if (!_touchReady) return;

        var contact = new POINTER_TOUCH_INFO
        {
            pointerInfo = new POINTER_INFO
            {
                pointerType = PT_TOUCH,
                pointerId = (uint)_pointerId,
                ptPixelLocation = new POINT { X = x, Y = y },
                ptPixelLocationRaw = new POINT { X = x, Y = y },
                pointerFlags = POINTER_FLAG_INRANGE | POINTER_FLAG_PRIMARY | action |
                               (action == POINTER_FLAG_UP ? 0u : POINTER_FLAG_INCONTACT),
            },
            touchFlags = 0,
            touchMask = TOUCH_MASK_CONTACTAREA | TOUCH_MASK_PRESSURE,
            rcContact = new RECT { Left = x - 4, Top = y - 4, Right = x + 4, Bottom = y + 4 },
            rcContactRaw = new RECT { Left = x - 4, Top = y - 4, Right = x + 4, Bottom = y + 4 },
            orientation = 90,
            pressure = 512,          // 触摸压力取值范围是 0–1024，超出会直接报参数无效
        };

        if (InjectTouchInput(1, new[] { contact }))
        {
            InjectedCount++;
        }
        else
        {
            int err = Marshal.GetLastWin32Error();
            InjectionAllowed = false;
            LastError = err == 87
                ? "本机系统不允许触摸注入（InjectTouchInput 报参数无效 87）。虚拟摇杆这条路在本机走不通。"
                : "InjectTouchInput 失败，错误码 " + err;
        }
    }
}
