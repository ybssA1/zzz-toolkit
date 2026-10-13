using Microsoft.Win32;

namespace ZzzToolkit;

/// <summary>定位绝区零安装目录。</summary>
public static class GameLocator
{
    public const string ExeName = "ZenlessZoneZero.exe";

    /// <summary>游戏安装路径的注册表位置（米哈游启动器 HYP 自己记的那份）。</summary>
    private static readonly string[] RegistryKeys =
    {
        @"Software\miHoYo\HYP\1_2\nap_cn",
        @"Software\miHoYo\HYP\1_1\nap_cn",
        @"Software\miHoYo\HYP\1_0\nap_cn",
        @"Software\miHoYo\HYP\nap_cn",
    };

    public static string? FromRegistry()
    {
        string? user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (string sub in RegistryKeys)
        {
            foreach (RegistryKey root in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                try
                {
                    using RegistryKey? key = root.OpenSubKey(sub);
                    if (key?.GetValue("GameInstallPath") is string p && !string.IsNullOrWhiteSpace(p))
                    {
                        string dir = p.Trim().TrimEnd('\\', '/');
                        if (IsGameDir(dir)) return dir;
                    }
                }
                catch { /* 键不存在或无权访问，继续 */ }
            }
        }
        return null;
    }

    /// <summary>没读到注册表时，扫几个常见位置。</summary>
    public static string? FromCommonPaths()
    {
        var roots = new List<string>();
        foreach (DriveInfo d in DriveInfo.GetDrives())
        {
            if (!d.IsReady) continue;
            string r = d.RootDirectory.FullName;
            roots.Add(Path.Combine(r, "miHoYo Launcher", "games", "ZenlessZoneZero Game"));
            roots.Add(Path.Combine(r, "ZenlessZoneZero Game"));
            roots.Add(Path.Combine(r, "Games", "ZenlessZoneZero Game"));
            roots.Add(Path.Combine(r, "Program Files", "ZenlessZoneZero Game"));
        }
        foreach (string p in roots) if (IsGameDir(p)) return p;
        return null;
    }

    public static string? Detect() => FromRegistry() ?? FromCommonPaths();

    public static bool IsGameDir(string? dir) =>
        !string.IsNullOrWhiteSpace(dir) && File.Exists(Path.Combine(dir!, ExeName));

    /// <summary>取游戏进程的 PID；没在运行返回 0。</summary>
    public static uint GameProcessId()
    {
        var p = System.Diagnostics.Process.GetProcessesByName(
            Path.GetFileNameWithoutExtension(ExeName)).FirstOrDefault();
        return p is null ? 0u : (uint)p.Id;
    }

    public static bool IsGameRunning() =>
        System.Diagnostics.Process.GetProcessesByName(
            Path.GetFileNameWithoutExtension(ExeName)).Length > 0;
}
