using System.Diagnostics;

namespace ZzzToolkit;

/// <summary>
/// 触控增强：用改造过的 TouchUILaunch 启动游戏并注入载荷，把**鼠标动作当成触摸**送给游戏。
/// 这样手机 UI 的虚拟摇杆就能被鼠标（或任何串流软件送过来的鼠标动作）驱动。
///
/// 载荷来自 GPL-3.0 的 Jerry12228/TouchUILaunch，本项目只加了一样东西：
/// 在它的 window_proc 里增加"鼠标作为第三条触摸来源"，由环境变量 TOUCHUI_MOUSE 控制。
/// 它读的是鼠标，不要求电脑有触摸屏，也不依赖任何串流软件 —— 换成任何能把手指变成
/// 鼠标的软件（UU远程 / ToDesk / Parsec / Moonlight / Windows 远程桌面）都能用。
/// </summary>
public sealed class TouchLaunch
{
    public const string ExeName = "TouchUILaunch.exe";
    public const string DllName = "TouchUILaunch.dll";

    /// <summary>载荷放这里时会被自动识别。</summary>
    public const string FolderName = "touch";

    private readonly string _dir;

    public TouchLaunch(string dir) => _dir = dir;

    public string Dir => _dir;

    public string ExePath => Path.Combine(_dir, ExeName);

    public bool IsAvailable =>
        File.Exists(ExePath) && File.Exists(Path.Combine(_dir, DllName));

    public static string? AutoDetect(string baseDir)
    {
        string bundled = Path.Combine(baseDir, FolderName);
        if (File.Exists(Path.Combine(bundled, ExeName))) return bundled;
        return null;
    }

    /// <summary>
    /// 启动游戏并注入载荷。游戏必须**尚未运行**：该工具只注入它自己新建的进程，
    /// 不会附身已经在跑的游戏。
    /// </summary>
    public void Start(string gameExe)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ExePath,
            WorkingDirectory = _dir,
            UseShellExecute = false,             // 需要它来传环境变量
            Arguments = $"--ZZZ --game \"{gameExe}\" --log",
        };
        // 打开"鼠标即触摸"。不设这一项，载荷行为与作者原版完全一致。
        psi.Environment["TOUCHUI_MOUSE"] = "1";
        Process.Start(psi);
    }
}
