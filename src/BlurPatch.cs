using System.Diagnostics;
using System.Text;

namespace ZzzToolkit;

/// <summary>
/// 「关虚化」功能的集成位。
///
/// 说明：关闭绝区零的角色虚化需要给游戏进程注入一个补丁 DLL 并替换渲染相关着色器，
/// 本项目**不附带**该补丁载荷（第三方作品、未授权、且带联网能力，不适合随本项目分发）。
/// 本类只负责：找到用户自备的组件目录 → 在游戏运行时调用它自带的注入器。
///
/// 组件目录里应当有注入器（默认 loader.exe）与它需要的 DLL。
/// </summary>
public sealed class BlurPatch
{
    private const string LoaderName = "loader.exe";

    public string ComponentDir { get; set; }

    public BlurPatch(string componentDir) => ComponentDir = componentDir;

    public static IEnumerable<string> CandidateDirs(string appDir)
    {
        yield return Path.Combine(appDir, "blur");
        yield return Path.Combine(appDir, "Blur");
        yield return Path.Combine(appDir, "虚化补丁");
        yield return appDir;
    }

    public static string? AutoDetectComponentDir(string appDir)
    {
        foreach (string d in CandidateDirs(appDir))
            if (File.Exists(Path.Combine(d, LoaderName))) return d;
        return null;
    }

    public string LoaderPath => Path.Combine(ComponentDir, LoaderName);

    public bool IsAvailable => File.Exists(LoaderPath);

    /// <summary>组件目录里除了注入器还有哪些文件（用于在界面上提示缺没缺 DLL）。</summary>
    public IReadOnlyList<string> PayloadFiles()
    {
        if (!Directory.Exists(ComponentDir)) return Array.Empty<string>();
        try
        {
            return Directory.GetFiles(ComponentDir)
                .Select(Path.GetFileName)
                .Where(n => n is not null && !n.Equals(LoaderName, StringComparison.OrdinalIgnoreCase))
                .Select(n => n!)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch { return Array.Empty<string>(); }
    }

    public sealed record RunResult(bool Ok, string Output);

    /// <summary>调用用户自备的注入器。游戏必须在运行中。</summary>
    public RunResult Apply(int waitMs = 8000)
    {
        if (!IsAvailable)
            return new RunResult(false, $"没有找到注入器：{LoaderPath}");

        if (!GameLocator.IsGameRunning())
            return new RunResult(false, "游戏没有在运行。请先启动绝区零，再点这个按钮。");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = LoaderPath,
                WorkingDirectory = ComponentDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
            };
            using Process? p = Process.Start(psi);
            if (p is null) return new RunResult(false, "无法启动注入器。");

            var sb = new StringBuilder();
            p.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (sb) sb.AppendLine(e.Data); };
            p.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (sb) sb.AppendLine(e.Data); };
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            if (!p.WaitForExit(waitMs))
                return new RunResult(true, "注入器已启动（仍在运行中）。\n" + Snapshot(sb));

            return new RunResult(p.ExitCode == 0,
                $"注入器退出码 {p.ExitCode}。\n" + Snapshot(sb));
        }
        catch (Exception ex)
        {
            return new RunResult(false, "调用注入器失败：" + ex.Message);
        }
    }

    private static string Snapshot(StringBuilder sb)
    {
        lock (sb)
        {
            string s = sb.ToString().Trim();
            return s.Length == 0 ? "（注入器没有输出）" : s;
        }
    }
}
