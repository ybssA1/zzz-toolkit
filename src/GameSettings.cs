using System.Text.RegularExpressions;

namespace ZzzToolkit;

public enum UiLayout
{
    Unknown = 0,
    /// <summary>移动端 / 触屏布局</summary>
    Mobile = 1,
    /// <summary>PC 布局</summary>
    Pc = 2,
}

/// <summary>
/// 绝区零本地设置文件 <c>GENERAL_DATA.bin</c> 的读写。
/// 其中的 <c>LocalUILayoutPlatform</c> 就是「设置 → 输入 → UI布局」那一项：
/// 1 = 移动端 / 触屏布局，2 = PC 布局。
/// </summary>
public sealed class GameSettings
{
    private const string FieldName = "LocalUILayoutPlatform";
    private static readonly Regex FieldRegex =
        new("(\"LocalUILayoutPlatform\"\\s*:\\s*)(-?\\d+)", RegexOptions.Compiled);

    public string GameDir { get; }
    public string FilePath { get; }
    public string BackupPath => FilePath + ".bak";

    public GameSettings(string gameDir)
    {
        GameDir = gameDir;
        FilePath = Path.Combine(gameDir, "ZenlessZoneZero_Data", "Persistent", "LocalStorage", "GENERAL_DATA.bin");
    }

    public bool Exists => File.Exists(FilePath);

    /// <summary>读原始文本（解码后）。</summary>
    public string ReadText()
    {
        if (!Exists) throw new FileNotFoundException("找不到游戏设置文件，请确认游戏路径是否正确。", FilePath);
        return Sleepy.Decode(File.ReadAllBytes(FilePath));
    }

    public UiLayout ReadLayout()
    {
        Match m = FieldRegex.Match(ReadText());
        if (!m.Success) return UiLayout.Unknown;
        return int.TryParse(m.Groups[2].Value, out int v) ? (UiLayout)v : UiLayout.Unknown;
    }

    /// <summary>把 UI 布局改成指定值。写入前自动备份，写入后立即回读校验。</summary>
    public void WriteLayout(UiLayout layout)
    {
        if (layout != UiLayout.Mobile && layout != UiLayout.Pc)
            throw new ArgumentOutOfRangeException(nameof(layout));

        if (GameLocator.IsGameRunning())
            throw new InvalidOperationException("游戏正在运行。游戏退出时会覆盖这个文件，请先关闭游戏再改。");

        string text = ReadText();
        Match m = FieldRegex.Match(text);
        if (!m.Success) throw new InvalidDataException("设置文件里没有找到 LocalUILayoutPlatform 字段。");

        string updated = FieldRegex.Replace(text, "${1}" + ((int)layout));
        byte[] encoded = Sleepy.Encode(updated);

        if (!File.Exists(BackupPath))
        {
            try { File.Copy(FilePath, BackupPath, false); } catch { /* 备份失败不阻塞，但要告知 */ }
        }

        // 原子替换：先写临时文件再移动，避免中途失败把原文件写坏
        string tmp = FilePath + ".tmp";
        File.WriteAllBytes(tmp, encoded);
        File.Move(tmp, FilePath, true);

        UiLayout check = ReadLayout();
        if (check != layout)
            throw new IOException($"写入后回读校验失败：期望 {layout}，实际读到 {check}。");
    }

    /// <summary>把备份还原回去。</summary>
    public void RestoreBackup()
    {
        if (!File.Exists(BackupPath)) throw new FileNotFoundException("没有找到备份文件。", BackupPath);
        if (GameLocator.IsGameRunning()) throw new InvalidOperationException("游戏正在运行，请先关闭游戏。");
        File.Copy(BackupPath, FilePath, true);
    }

    /// <summary>格式自检：解码再编码必须与原文件逐字节一致。用于确认版本兼容性。</summary>
    public bool RoundTripIsLossless()
    {
        byte[] original = File.ReadAllBytes(FilePath);
        byte[] again = Sleepy.Encode(Sleepy.Decode(original));
        return original.AsSpan().SequenceEqual(again);
    }
}
