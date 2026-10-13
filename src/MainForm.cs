using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ZzzToolkit;

public sealed class MainForm : Form
{
    // ── 控件 ─────────────────────────────────────────────────────────────
    private readonly TextBox _gamePath = new();
    private readonly Button _btnBrowseGame = new();
    private readonly Button _btnDetect = new();
    private readonly Label _lblGameState = new();

    private readonly Label _lblLayout = new();
    private readonly Button _btnMobile = new();
    private readonly Button _btnPc = new();

    private readonly TextBox _blurDir = new();
    private readonly Button _btnBrowseBlur = new();
    private readonly Button _btnApplyBlur = new();
    private readonly CheckBox _chkAutoBlur = new();
    private readonly Label _lblBlurState = new();

    private readonly Button _btnLaunch = new();
    private readonly Button _btnSelfTest = new();
    private readonly Button _btnRestore = new();
    private readonly TextBox _log = new();
    private readonly System.Windows.Forms.Timer _watch = new();

    private bool _gameWasRunning;
    private bool _configLoaded;

    private string ConfigPath => Path.Combine(AppContext.BaseDirectory, "zzz-toolkit.json");

    /// <summary>
    /// 自制 DPI 缩放：界面尺寸全部按 96 DPI 写死，这里按实际 DPI 等比放大一次。
    /// 不用 WinForms 自带的 AutoScaleMode——它在高 DPI 下只放大窗体、不放大用代码创建的控件。
    /// </summary>
    private void ApplyDpiScale()
    {
        float s;
        try { using Graphics g = Graphics.FromHwnd(IntPtr.Zero); s = g.DpiX / 96f; }
        catch { return; }
        if (s <= 1.01f) return;

        ClientSize = new Size((int)(ClientSize.Width * s), (int)(ClientSize.Height * s));
        foreach (Control c in Controls) ScaleTree(c, s);
        // 日志框用 Anchor 跟随窗体，缩放后重新贴一次
        _log.Width = ClientSize.Width - _log.Left - 10;
        _log.Height = ClientSize.Height - _log.Top - 10;
    }

    private static void ScaleTree(Control c, float s)
    {
        c.Location = new Point((int)(c.Location.X * s), (int)(c.Location.Y * s));
        if (!c.AutoSize)
            c.Size = new Size((int)(c.Size.Width * s), (int)(c.Size.Height * s));
        foreach (Control child in c.Controls) ScaleTree(child, s);
    }

    public MainForm()
    {
        Text = "绝区零工具箱 · 手机触屏 UI / 关虚化";
        ClientSize = new Size(640, 520);
        MinimumSize = new Size(640, 420);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);

        BuildUi();
        ApplyDpiScale();   // 界面按 96 DPI 设计，这里按实际 DPI 等比放大
        Load += (_, _) => OnFormLoad();
        FormClosing += (_, _) => SaveConfig();
    }

    // ── 界面 ─────────────────────────────────────────────────────────────
    private void BuildUi()
    {
        const int W = 620;
        int y = 8;

        // ── 游戏 ──
        var g1 = new GroupBox { Text = " 游戏 ", Location = new Point(10, y), Size = new Size(W, 100) };
        g1.Controls.Add(new Label { Text = "安装目录", Location = new Point(12, 30), AutoSize = true });
        _gamePath.Location = new Point(80, 27); _gamePath.Size = new Size(400, 25); _gamePath.ReadOnly = true; _gamePath.TabStop = false;
        _btnBrowseGame.Text = "浏览…"; _btnBrowseGame.Location = new Point(486, 26); _btnBrowseGame.Size = new Size(122, 27);
        _btnBrowseGame.Click += (_, _) => BrowseGame();
        _btnDetect.Text = "自动检测"; _btnDetect.Location = new Point(486, 56); _btnDetect.Size = new Size(122, 27);
        _btnDetect.Click += (_, _) => DetectGame(true);
        _lblGameState.Location = new Point(12, 60); _lblGameState.Size = new Size(468, 34); _lblGameState.AutoSize = false;
        g1.Controls.AddRange(new Control[] { _gamePath, _btnBrowseGame, _btnDetect, _lblGameState });
        Controls.Add(g1); y += 108;

        // ── 手机触屏 UI ──
        var g2 = new GroupBox { Text = " 手机触屏 UI（改游戏设置文件，不注入、不改游戏本体）", Location = new Point(10, y), Size = new Size(W, 104) };
        _lblLayout.Location = new Point(12, 28); _lblLayout.Size = new Size(596, 26); _lblLayout.AutoSize = false;
        _btnMobile.Text = "开启手机 UI"; _btnMobile.Location = new Point(12, 60); _btnMobile.Size = new Size(150, 34);
        _btnMobile.Click += (_, _) => SwitchLayout(UiLayout.Mobile);
        _btnPc.Text = "恢复 PC UI"; _btnPc.Location = new Point(170, 60); _btnPc.Size = new Size(150, 34);
        _btnPc.Click += (_, _) => SwitchLayout(UiLayout.Pc);
        _btnSelfTest.Text = "格式自检"; _btnSelfTest.Location = new Point(328, 60); _btnSelfTest.Size = new Size(120, 34);
        _btnSelfTest.Click += (_, _) => SelfTest();
        _btnRestore.Text = "还原备份"; _btnRestore.Location = new Point(456, 60); _btnRestore.Size = new Size(152, 34);
        _btnRestore.Click += (_, _) => RestoreBackup();
        g2.Controls.AddRange(new Control[] { _lblLayout, _btnMobile, _btnPc, _btnSelfTest, _btnRestore });
        Controls.Add(g2); y += 112;

        // ── 关虚化 ──
        var g3 = new GroupBox { Text = " 关虚化（调用你自备的补丁组件，本项目不附带）", Location = new Point(10, y), Size = new Size(W, 148) };
        g3.Controls.Add(new Label { Text = "组件目录", Location = new Point(12, 30), AutoSize = true });
        _blurDir.Location = new Point(80, 27); _blurDir.Size = new Size(400, 25); _blurDir.ReadOnly = true; _blurDir.TabStop = false;
        _btnBrowseBlur.Text = "浏览…"; _btnBrowseBlur.Location = new Point(486, 26); _btnBrowseBlur.Size = new Size(122, 27);
        _btnBrowseBlur.Click += (_, _) => BrowseBlur();
        _chkAutoBlur.Text = "游戏启动后自动应用"; _chkAutoBlur.Location = new Point(12, 64); _chkAutoBlur.Size = new Size(240, 28);
        _btnApplyBlur.Text = "应用虚化补丁"; _btnApplyBlur.Location = new Point(260, 62); _btnApplyBlur.Size = new Size(170, 34);
        _btnApplyBlur.Click += (_, _) => ApplyBlur();
        _lblBlurState.Location = new Point(12, 102); _lblBlurState.Size = new Size(596, 40); _lblBlurState.AutoSize = false;
        g3.Controls.AddRange(new Control[] { _blurDir, _btnBrowseBlur, _chkAutoBlur, _btnApplyBlur, _lblBlurState });
        Controls.Add(g3); y += 156;

        _btnLaunch.Text = "启动游戏"; _btnLaunch.Location = new Point(10, y); _btnLaunch.Size = new Size(W, 36);
        _btnLaunch.Click += (_, _) => LaunchGame();
        Controls.Add(_btnLaunch); y += 44;

        _log.Location = new Point(10, y); _log.Size = new Size(W, ClientSize.Height - y - 10);
        _log.Multiline = true; _log.ReadOnly = true; _log.ScrollBars = ScrollBars.Vertical;
        _log.BackColor = Color.FromArgb(24, 24, 28); _log.ForeColor = Color.Gainsboro;
        _log.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        Controls.Add(_log);

        _watch.Interval = 2000;
        _watch.Tick += (_, _) => WatchTick();
    }

    private void RestoreBackup()
    {
        GameSettings? s = Settings();
        if (s is null || !s.Exists) { Log("请先指定有效的游戏目录。"); return; }
        try { s.RestoreBackup(); Log("已用备份还原设置文件。"); }
        catch (Exception ex) { Log("还原失败：" + ex.Message); }
        RefreshState();
    }

    private void Log(string s)
    {
        _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + s + Environment.NewLine);
    }

    // ── 生命周期 ─────────────────────────────────────────────────────────
    private void OnFormLoad()
    {
        LoadConfig();
        if (!GameLocator.IsGameDir(_gamePath.Text)) DetectGame(false);
        RefreshState();
        _watch.Start();
        ActiveControl = null;   // 别让只读路径框一上来就高亮选中
        Log("就绪。当前 UI 布局的修改直接作用于游戏设置文件，不需要注入；关虚化需要你自备补丁组件。");
    }

    private GameSettings? Settings()
    {
        if (!GameLocator.IsGameDir(_gamePath.Text)) return null;
        return new GameSettings(_gamePath.Text);
    }

    private void RefreshState()
    {
        bool running = GameLocator.IsGameRunning();
        _lblGameState.Text = "游戏状态：" + (running ? "运行中" : "未运行") +
            (GameLocator.IsGameDir(_gamePath.Text) ? "" : "    ⚠ 还没有指定有效的游戏目录");

        GameSettings? s = Settings();
        if (s is null || !s.Exists)
        {
            _lblLayout.Text = "当前布局：—（找不到设置文件）";
        }
        else
        {
            try
            {
                UiLayout l = s.ReadLayout();
                _lblLayout.Text = "当前布局：" + Describe(l) +
                    (File.Exists(s.BackupPath) ? "    备份：已存在" : "    备份：尚未创建");
            }
            catch (Exception ex) { _lblLayout.Text = "当前布局：读取失败 — " + ex.Message; }
        }

        var blur = new BlurPatch(_blurDir.Text);
        if (!_configLoaded || string.IsNullOrWhiteSpace(_blurDir.Text))
        {
            string? auto = BlurPatch.AutoDetectComponentDir(AppContext.BaseDirectory);
            if (auto is not null) _blurDir.Text = auto;
        }
        _lblBlurState.Text = blur.IsAvailable
            ? "组件：已找到 " + blur.LoaderPath
            : "组件：未找到。把第三方补丁解压到本程序目录下的 blur 文件夹即可（本项目不附带该组件）。";
    }

    private static string Describe(UiLayout l) => l switch
    {
        UiLayout.Mobile => "移动端 / 触屏 UI",
        UiLayout.Pc => "PC UI",
        _ => "未知",
    };

    // ── 游戏目录 ─────────────────────────────────────────────────────────
    private void DetectGame(bool announce)
    {
        string? dir = GameLocator.Detect();
        if (dir is null)
        {
            if (announce) MessageBox.Show(this, "没有自动找到游戏目录，请手动浏览选择。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            _gamePath.Text = dir;
            if (announce) Log("已自动检测到游戏目录：" + dir);
        }
        _configLoaded = true;
        RefreshState();
    }

    private void BrowseGame()
    {
        using var d = new FolderBrowserDialog { Description = "选择绝区零安装目录（含 ZenlessZoneZero.exe）" };
        if (d.ShowDialog(this) == DialogResult.OK)
        {
            if (!GameLocator.IsGameDir(d.SelectedPath))
            {
                MessageBox.Show(this, "这个目录里没有找到 " + GameLocator.ExeName + "，请重新选择。", "目录无效",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _gamePath.Text = d.SelectedPath;
            RefreshState();
        }
    }

    // ── 触屏 UI ──────────────────────────────────────────────────────────
    private void SwitchLayout(UiLayout target)
    {
        GameSettings? s = Settings();
        if (s is null || !s.Exists)
        {
            MessageBox.Show(this, "请先指定有效的游戏目录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            s.WriteLayout(target);
            Log($"UI 布局已切换为「{Describe(target)}」（写入后已回读校验通过）。");
            if (GameLocator.IsGameRunning())
                Log("注意：游戏正在运行，改动会在下次启动游戏时生效。");
        }
        catch (Exception ex)
        {
            Log("切换失败：" + ex.Message);
            MessageBox.Show(this, ex.Message, "切换失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        RefreshState();
    }

    private void SelfTest()
    {
        GameSettings? s = Settings();
        if (s is null || !s.Exists) { Log("自检：请先指定有效的游戏目录。"); return; }
        try
        {
            bool ok = s.RoundTripIsLossless();
            UiLayout l = s.ReadLayout();
            Log(ok
                ? $"自检通过：解码后再编码与原文件逐字节一致；当前布局 = {Describe(l)}。"
                : "自检失败：编解码无法无损还原，你的游戏版本可能改了设置文件格式，请勿写入并反馈问题。");
        }
        catch (Exception ex) { Log("自检出错：" + ex.Message); }
    }

    // ── 关虚化 ───────────────────────────────────────────────────────────
    private void BrowseBlur()
    {
        using var d = new FolderBrowserDialog { Description = "选择补丁组件目录（里面应有 loader.exe）" };
        if (d.ShowDialog(this) == DialogResult.OK)
        {
            _blurDir.Text = d.SelectedPath;
            RefreshState();
        }
    }

    private void ApplyBlur()
    {
        var blur = new BlurPatch(_blurDir.Text);
        if (!blur.IsAvailable)
        {
            MessageBox.Show(this,
                "没有找到补丁组件。\n\n" +
                "本程序只提供「关虚化」的调用入口，不附带补丁载荷（第三方作品，未经授权不宜随本项目分发）。\n\n" +
                "请把补丁解压到本程序目录下的 blur 文件夹，或用「浏览…」指定它的目录。",
                "缺少补丁组件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        BlurPatch.RunResult r = blur.Apply();
        Log((r.Ok ? "虚化补丁：" : "虚化补丁失败：") + r.Output);
    }

    // ── 启动游戏 / 监视 ──────────────────────────────────────────────────
    private void LaunchGame()
    {
        if (!GameLocator.IsGameDir(_gamePath.Text))
        {
            MessageBox.Show(this, "请先指定有效的游戏目录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(_gamePath.Text, GameLocator.ExeName),
                WorkingDirectory = _gamePath.Text,
                UseShellExecute = true,
            });
            Log("已启动游戏。");
        }
        catch (Exception ex) { Log("启动游戏失败：" + ex.Message); }
    }

    private void WatchTick()
    {
        bool running = GameLocator.IsGameRunning();
        if (running != _gameWasRunning)
        {
            _gameWasRunning = running;
            RefreshState();
        }
        if (running && _chkAutoBlur.Checked)
        {
            var blur = new BlurPatch(_blurDir.Text);
            if (blur.IsAvailable && !_blurAppliedForThisRun)
            {
                _blurAppliedForThisRun = true;
                Log("检测到游戏已启动，自动应用虚化补丁…");
                BlurPatch.RunResult r = blur.Apply();
                Log((r.Ok ? "虚化补丁：" : "虚化补丁失败：") + r.Output);
            }
        }
        if (!running) _blurAppliedForThisRun = false;
    }

    private bool _blurAppliedForThisRun;

    // ── 配置 ─────────────────────────────────────────────────────────────
    private sealed class AppConfig
    {
        public string? GamePath { get; set; }
        public string? BlurDir { get; set; }
        public bool AutoBlur { get; set; }
    }

    private void LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigPath)) { _configLoaded = true; return; }
            AppConfig? c = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath));
            if (c is null) { _configLoaded = true; return; }
            _gamePath.Text = c.GamePath ?? "";
            _blurDir.Text = c.BlurDir ?? "";
            _chkAutoBlur.Checked = c.AutoBlur;
        }
        catch { /* 配置坏了就用默认值 */ }
        _configLoaded = true;
    }

    private void SaveConfig()
    {
        try
        {
            var c = new AppConfig
            {
                GamePath = _gamePath.Text,
                BlurDir = _blurDir.Text,
                AutoBlur = _chkAutoBlur.Checked,
            };
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(c, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* 忽略 */ }
    }
}
