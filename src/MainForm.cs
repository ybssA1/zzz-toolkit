using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ZzzToolkit;

public sealed class MainForm : Form
{
    // ── 界面控件 ─────────────────────────────────────────────────────────
    private readonly TextBox _gamePath = new();
    private readonly Button _btnBrowse = new();
    private readonly Button _btnDetect = new();

    private readonly CheckBox _chkBlur = new();
    private readonly Button _btnApplyBlur = new();
    private readonly Label _lblBlurState = new();

    private readonly CheckBox _chkMobileUi = new();
    private readonly Label _lblUiState = new();

    private readonly CheckBox _chkTouchSim = new();
    private readonly Label _lblTouchState = new();

    private readonly Button _btnLaunch = new();
    private readonly LinkLabel _lnkAuthor = new();
    private readonly TextBox _log = new();

    private readonly System.Windows.Forms.Timer _watch = new();

    private bool _gameWasRunning;
    private bool _blurAppliedThisRun;
    private bool _loading;

    public const string AuthorName = "Ex_M";
    public const string AuthorUid = "44434084";
    public const string AuthorUrl = "https://space.bilibili.com/44434084";

    private string ConfigPath => Path.Combine(AppContext.BaseDirectory, "zzz-toolkit.json");

    public MainForm()
    {
        Text = "绝区零工具箱";
        ClientSize = new Size(560, 448);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);

        BuildUi();
        ApplyDpiScale();

        Load += (_, _) => OnFormLoad();
        FormClosing += (_, _) => SaveConfig();
    }

    // ── 界面 ─────────────────────────────────────────────────────────────
    private void BuildUi()
    {
        const int W = 540;
        int y = 8;

        // ① 游戏路径
        var g1 = new GroupBox { Text = " 游戏路径 ", Location = new Point(10, y), Size = new Size(W, 84) };
        g1.Controls.Add(new Label { Text = "安装目录", Location = new Point(12, 30), AutoSize = true });
        _gamePath.Location = new Point(80, 27); _gamePath.Size = new Size(310, 25); _gamePath.ReadOnly = true; _gamePath.TabStop = false;
        _btnBrowse.Text = "浏览…"; _btnBrowse.Location = new Point(396, 26); _btnBrowse.Size = new Size(62, 27);
        _btnBrowse.Click += (_, _) => BrowseGame();
        _btnDetect.Text = "自动检测"; _btnDetect.Location = new Point(462, 26); _btnDetect.Size = new Size(68, 27);
        _btnDetect.Click += (_, _) => DetectGame(true);
        g1.Controls.AddRange(new Control[] { _gamePath, _btnBrowse, _btnDetect });
        Controls.Add(g1); y += 92;

        // ②③④ 三个开关
        var g2 = new GroupBox { Text = " 功能开关 ", Location = new Point(10, y), Size = new Size(W, 148) };
        _chkBlur.Text = "反虚化（关闭角色虚化 / 镜头贴近时的消隐）";
        _chkBlur.Location = new Point(12, 26); _chkBlur.Size = new Size(400, 26);
        _chkBlur.CheckedChanged += (_, _) => OnBlurToggled();
        _btnApplyBlur.Text = "立即应用"; _btnApplyBlur.Location = new Point(424, 24); _btnApplyBlur.Size = new Size(104, 30);
        _btnApplyBlur.Click += (_, _) => ApplyBlur(true);
        _lblBlurState.Location = new Point(30, 52); _lblBlurState.Size = new Size(498, 18); _lblBlurState.AutoSize = false;
        _lblBlurState.ForeColor = Color.DimGray;

        _chkMobileUi.Text = "手机触控 UI（下个游戏界面变成手机布局）";
        _chkMobileUi.Location = new Point(12, 76); _chkMobileUi.Size = new Size(400, 26);
        _chkMobileUi.CheckedChanged += (_, _) => OnMobileUiToggled();
        _lblUiState.Location = new Point(378, 80); _lblUiState.Size = new Size(150, 20); _lblUiState.AutoSize = false;
        _lblUiState.TextAlign = ContentAlignment.MiddleRight; _lblUiState.ForeColor = Color.DimGray;

        _chkTouchSim.Text = "触控增强（把鼠标当手指，修手机 UI 摇杆）";
        _chkTouchSim.Location = new Point(12, 106); _chkTouchSim.Size = new Size(400, 26);
        _chkTouchSim.CheckedChanged += (_, _) => OnTouchSimToggled();
        _lblTouchState.Location = new Point(378, 110); _lblTouchState.Size = new Size(150, 20); _lblTouchState.AutoSize = false;
        _lblTouchState.TextAlign = ContentAlignment.MiddleRight; _lblTouchState.ForeColor = Color.DimGray;

        g2.Controls.AddRange(new Control[] { _chkBlur, _btnApplyBlur, _lblBlurState, _chkMobileUi, _lblUiState, _chkTouchSim, _lblTouchState });
        Controls.Add(g2); y += 156;

        _btnLaunch.Text = "启动游戏"; _btnLaunch.Location = new Point(10, y); _btnLaunch.Size = new Size(W, 34);
        _btnLaunch.Click += (_, _) => LaunchGame();
        Controls.Add(_btnLaunch); y += 42;

        // ④ 作者署名
        var g4 = new GroupBox { Text = " 作者署名 ", Location = new Point(10, y), Size = new Size(W, 72) };
        g4.Controls.Add(new Label
        {
            Text = $"反虚化组件由 {AuthorName} 制作，版权归其所有",
            Location = new Point(12, 22), Size = new Size(516, 20), AutoSize = false,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
        });
        _lnkAuthor.Text = $"B 站 @{AuthorName}（UID: {AuthorUid}）";
        _lnkAuthor.Location = new Point(12, 44); _lnkAuthor.Size = new Size(516, 20); _lnkAuthor.AutoSize = false;
        _lnkAuthor.LinkClicked += (_, _) => OpenAuthor();
        g4.Controls.Add(_lnkAuthor);
        Controls.Add(g4); y += 80;

        _log.Location = new Point(10, y); _log.Size = new Size(W, ClientSize.Height - y - 10);
        _log.Multiline = true; _log.ReadOnly = true; _log.ScrollBars = ScrollBars.Vertical;
        _log.BackColor = Color.FromArgb(24, 24, 28); _log.ForeColor = Color.Gainsboro;
        _log.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        Controls.Add(_log);

        _watch.Interval = 2000;
        _watch.Tick += (_, _) => WatchTick();
    }

    /// <summary>按 96 DPI 设计，这里按实际 DPI 等比放大一次。</summary>
    private void ApplyDpiScale()
    {
        float s;
        try { using Graphics g = Graphics.FromHwnd(IntPtr.Zero); s = g.DpiX / 96f; }
        catch { return; }
        if (s <= 1.01f) return;

        ClientSize = new Size((int)(ClientSize.Width * s), (int)(ClientSize.Height * s));
        foreach (Control c in Controls) ScaleTree(c, s);
        _log.Width = ClientSize.Width - _log.Left - 10;
        _log.Height = ClientSize.Height - _log.Top - 10;
    }

    private static void ScaleTree(Control c, float s)
    {
        c.Location = new Point((int)(c.Location.X * s), (int)(c.Location.Y * s));
        if (!c.AutoSize) c.Size = new Size((int)(c.Size.Width * s), (int)(c.Size.Height * s));
        foreach (Control child in c.Controls) ScaleTree(child, s);
    }

    private void Log(string s) =>
        _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + s + Environment.NewLine);

    // ── 生命周期 ─────────────────────────────────────────────────────────
    private void OnFormLoad()
    {
        _loading = true;          // 必须在 DetectGame / RefreshUiState 之前置位，否则初始化会触发写入
        try
        {
            LoadConfig();
            if (!GameLocator.IsGameDir(_gamePath.Text)) DetectGame(false);
            RefreshUiState();
        }
        finally { _loading = false; }

        _watch.Start();
        ActiveControl = null;
        Log($"反虚化组件由 B 站 @{AuthorName}（UID {AuthorUid}）制作，版权归其所有。");
        Log("手机触控 UI 走游戏自身设置，改完需要重启游戏生效。");
    }

    private GameSettings? Settings()
    {
        if (!GameLocator.IsGameDir(_gamePath.Text)) return null;
        return new GameSettings(_gamePath.Text);
    }

    private BlurPatch Blur()
    {
        string dir = BlurPatch.AutoDetectComponentDir(AppContext.BaseDirectory) ?? Path.Combine(AppContext.BaseDirectory, "blur");
        return new BlurPatch(dir);
    }

    private void RefreshUiState()
    {
        GameSettings? s = Settings();
        if (s is null || !s.Exists)
        {
            _lblUiState.Text = "找不到设置文件";
            _chkMobileUi.Enabled = false;
        }
        else
        {
            _chkMobileUi.Enabled = true;
            try
            {
                _chkMobileUi.Checked = s.ReadLayout() == UiLayout.Mobile;
                _lblUiState.Text = _chkMobileUi.Checked ? "当前：手机布局" : "当前：PC 布局";
            }
            catch (Exception ex) { _lblUiState.Text = "读取失败"; Log("读取设置失败：" + ex.Message); }
        }

        BlurPatch b = Blur();
        _btnApplyBlur.Enabled = b.IsAvailable;
        _lblBlurState.Text = b.IsAvailable
            ? (GameLocator.IsGameRunning() ? "组件已就绪，游戏运行中，可立即应用" : "组件已就绪，点「启动游戏」后会自动应用")
            : "缺少反虚化组件（blur 文件夹）";
    }

    // ── 游戏路径 ─────────────────────────────────────────────────────────
    private void DetectGame(bool announce)
    {
        string? dir = GameLocator.Detect();
        if (dir is null)
        {
            if (announce) MessageBox.Show(this, "没有自动找到游戏目录，请手动选择。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            _gamePath.Text = dir;
            if (announce) Log("已找到游戏目录：" + dir);
        }
        RefreshUiState();
    }

    private void BrowseGame()
    {
        using var d = new FolderBrowserDialog { Description = "选择绝区零安装目录（里面应有 ZenlessZoneZero.exe）" };
        if (d.ShowDialog(this) != DialogResult.OK) return;
        if (!GameLocator.IsGameDir(d.SelectedPath))
        {
            MessageBox.Show(this, "这个目录里没有 " + GameLocator.ExeName + "，请重新选择。", "目录无效",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _gamePath.Text = d.SelectedPath;
        RefreshUiState();
    }

    // ── 开关一：反虚化 ───────────────────────────────────────────────────
    private void OnBlurToggled()
    {
        if (_loading) return;
        if (_chkBlur.Checked)
        {
            if (!Blur().IsAvailable)
            {
                MessageBox.Show(this,
                    "缺少反虚化组件。\n\n请把 Ex_M 的反虚化组件（loader.exe 与 zencen.dll）放进本程序目录下的 blur 文件夹。",
                    "缺少组件", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _loading = true; _chkBlur.Checked = false; _loading = false;
                return;
            }
            Log("反虚化：已开启，游戏启动时会自动应用。");
            if (GameLocator.IsGameRunning()) ApplyBlur(false);
        }
        else
        {
            Log("反虚化：已关闭。已注入的补丁要等游戏重启才会消失。");
            _lblBlurState.Text = "已关闭（重启游戏后效果消失）";
        }
    }

    private void ApplyBlur(bool manual)
    {
        BlurPatch b = Blur();
        if (!b.IsAvailable)
        {
            if (manual) MessageBox.Show(this, "缺少反虚化组件（blur 文件夹）。", "缺少组件",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!GameLocator.IsGameRunning())
        {
            if (manual) MessageBox.Show(this, "游戏还没启动。先点「启动游戏」，进游戏后再应用。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_blurAppliedThisRun && !manual) return;

        BlurPatch.RunResult r = b.Apply();
        _blurAppliedThisRun = true;
        Log((r.Ok ? "反虚化：" : "反虚化失败：") + r.Output.Replace("\r\n", " ").Replace("\n", " "));
        _lblBlurState.Text = r.Ok ? "已应用" : "应用失败，详见下方日志";
    }

    // ── 开关二：手机触控 UI ──────────────────────────────────────────────
    private void OnMobileUiToggled()
    {
        if (_loading) return;
        GameSettings? s = Settings();
        if (s is null || !s.Exists) { RefreshUiState(); return; }

        UiLayout target = _chkMobileUi.Checked ? UiLayout.Mobile : UiLayout.Pc;
        try
        {
            s.WriteLayout(target);
            _lblUiState.Text = target == UiLayout.Mobile ? "当前：手机布局" : "当前：PC 布局";
            Log($"手机触控 UI：已{(target == UiLayout.Mobile ? "开启" : "关闭")}，重启游戏后生效。");
            if (GameLocator.IsGameRunning()) Log("（游戏正在运行，改动下次启动生效。）");
        }
        catch (Exception ex)
        {
            Log("切换失败：" + ex.Message);
            MessageBox.Show(this, ex.Message, "切换失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _loading = true; _chkMobileUi.Checked = !_chkMobileUi.Checked; _loading = false;
        }
    }

    // ── 开关三：触屏模拟 ─────────────────────────────────────────────────
    /// <summary>
    /// 把鼠标动作翻译成系统级触摸事件，专治移动端 UI 的虚拟摇杆不动
    /// （摇杆读的是 Touchscreen 设备，鼠标给不了触摸）。
    /// </summary>
    private void OnTouchSimToggled()
    {
        if (_loading) return;
        if (!_chkTouchSim.Checked)
        {
            Log("触控增强：已关闭，下次启动游戏走原版路径。");
            _lblTouchState.Text = "已关闭";
            return;
        }
        TouchLaunch tl = Touch();
        if (!tl.IsAvailable)
        {
            Log("触控增强：缺少载荷（需要 touch 文件夹里的 TouchUILaunch.exe 与 .dll）。");
            _lblTouchState.Text = "缺少载荷";
            MessageBox.Show(this,
                "缺少触控载荷。\n\n请把 touch 文件夹（含 TouchUILaunch.exe 与 TouchUILaunch.dll）放到本程序目录。",
                "缺少载荷", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _loading = true; _chkTouchSim.Checked = false; _loading = false;
            return;
        }
        if (GameLocator.IsGameRunning())
        {
            Log("触控增强：游戏已在运行，无法注入。请先退出游戏，再由本程序启动。");
            _lblTouchState.Text = "需重启游戏";
            return;
        }
        _lblTouchState.Text = "已就绪";
        Log("触控增强：已开启。点「启动游戏」时会把鼠标动作当作触摸送进游戏。");
    }

    private TouchLaunch Touch()
    {
        string? dir = TouchLaunch.AutoDetect(AppContext.BaseDirectory);
        return new TouchLaunch(dir ?? Path.Combine(AppContext.BaseDirectory, TouchLaunch.FolderName));
    }

    // ── 启动 / 监视 ──────────────────────────────────────────────────────
    private void LaunchGame()
    {
        if (!GameLocator.IsGameDir(_gamePath.Text))
        {
            MessageBox.Show(this, "请先指定游戏目录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        string exe = Path.Combine(_gamePath.Text, GameLocator.ExeName);
        try
        {
            if (_chkTouchSim.Checked)
            {
                TouchLaunch tl = Touch();
                if (tl.IsAvailable)
                {
                    tl.Start(exe);      // 它负责启动游戏并注入载荷
                    Log("已用「触控增强」启动游戏：鼠标动作会被当作触摸送进游戏，手机 UI 的摇杆可用。");
                    return;
                }
                Log("触控增强已勾选但找不到载荷，改为直接启动游戏。");
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = _gamePath.Text,
                UseShellExecute = true,
            });
            Log("已启动游戏。");
        }
        catch (Exception ex) { Log("启动失败：" + ex.Message); }
    }

    private void WatchTick()
    {
        bool running = GameLocator.IsGameRunning();
        // 触控增强要求游戏由本程序启动（载荷只注入新建的进程），游戏一起来就提示一下
        if (running && _chkTouchSim.Checked && _lblTouchState.Text == "已就绪")
        {
            _lblTouchState.Text = "生效中";
            Log("触控增强：游戏已启动，鼠标动作已作为触摸送入。");
        }

        if (running != _gameWasRunning)
        {
            _gameWasRunning = running;
            _blurAppliedThisRun = false;
            RefreshUiState();
            if (running && _chkBlur.Checked) ApplyBlur(false);

            if (!running && _chkTouchSim.Checked && _lblTouchState.Text == "生效中")
            {
                _lblTouchState.Text = "已就绪";
                Log("触控增强：游戏已退出。");
            }
        }
    }

    private void OpenAuthor()
    {
        try { Process.Start(new ProcessStartInfo { FileName = AuthorUrl, UseShellExecute = true }); }
        catch (Exception ex) { Log("打开链接失败：" + ex.Message); }
    }

    // ── 配置 ─────────────────────────────────────────────────────────────
    private sealed class AppConfig
    {
        public string? GamePath { get; set; }
        public bool BlurEnabled { get; set; }
        public bool TouchSimEnabled { get; set; }
    }

    private void LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return;
            AppConfig? c = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath));
            if (c is null) return;
            _gamePath.Text = c.GamePath ?? "";
            _chkBlur.Checked = c.BlurEnabled;
            _chkTouchSim.Checked = c.TouchSimEnabled;
        }
        catch { /* 配置坏了就用默认值 */ }
    }

    private void SaveConfig()
    {
        try
        {
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(
                new AppConfig
                {
                    GamePath = _gamePath.Text,
                    BlurEnabled = _chkBlur.Checked,
                    TouchSimEnabled = _chkTouchSim.Checked,
                },
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* 忽略 */ }
    }
}
