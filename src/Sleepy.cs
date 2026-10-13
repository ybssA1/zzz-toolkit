using System.Text;

namespace ZzzToolkit;

/// <summary>
/// 绝区零 `GENERAL_DATA.bin` 的存储编码（社区称 "Sleepy"）。
///
/// 文件结构：
///   25 字节头部（模拟 .NET BinaryFormatter 流：SerializedStreamHeader / Object /
///   BinaryReference / String / Single / StringArray / Single）
///   + 7bit 变长整数（载荷字节数）
///   + 载荷（逐字节与 29 字节魔数循环异或；魔数中 (b &amp; 0xC0) == 0xC0 的槽位是"逃逸"标记，
///     该槽位异或结果为非 0 时，把下一个普通字节 + 0x40 还原）
///   + 1 字节尾部 0x0B（MessageEnd）
///
/// 算法来源：CollapseLauncher/Collapse#466 的社区讨论与 fffonion/ZZZConfigManager。
/// 本实现做过往返校验：解码后再编码必须与原文件逐字节一致。
/// </summary>
public static class Sleepy
{
    private static readonly byte[] Magic =
    {
        85, 110, 209, 150, 116, 209, 131, 206, 149, 110, 103, 105, 110,
        208, 181, 46, 71, 208, 176, 109, 101, 206, 159, 98, 106, 101, 209, 129, 116
    };

    private static readonly byte[] Header =
    {
        0x00,
        0x01, 0x00, 0x00, 0x00,
        0xFF, 0xFF, 0xFF, 0xFF,
        0x01, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x06,
        0x01, 0x00, 0x00, 0x00
    };

    private const byte Footer = 0x0B;

    public static string Decode(byte[] data)
    {
        if (data.Length < Header.Length + 1)
            throw new InvalidDataException("文件过短，不是有效的 GENERAL_DATA.bin。");

        for (int i = 0; i < Header.Length; i++)
        {
            if (data[i] != Header[i])
                throw new InvalidDataException(
                    $"文件头部与预期不符（第 {i} 字节：期望 0x{Header[i]:X2}，实际 0x{data[i]:X2}）。" +
                    "该文件可能来自更新的游戏版本，或已被其他工具改写。");
        }

        int p = Header.Length;
        int length = 0, shift = 0;
        byte b;
        do
        {
            if (shift == 35) throw new InvalidDataException("长度字段格式错误。");
            b = data[p++];
            length |= (b & 0x7F) << shift;
            shift += 7;
        }
        while ((b & 0x80) != 0);

        if (p + length > data.Length)
            throw new InvalidDataException("文件长度与头部声明不符，文件可能已损坏。");

        bool[] evil = new bool[Magic.Length];
        for (int i = 0; i < Magic.Length; i++) evil[i] = (Magic[i] & 0xC0) == 0xC0;

        var chars = new List<byte>(length);
        bool pending = false;
        for (int i = 0; i < length; i++)
        {
            int n = i % Magic.Length;
            byte ch = (byte)(data[p + i] ^ Magic[n]);
            if (evil[n])
            {
                pending = ch != 0;
            }
            else
            {
                if (pending) { ch = (byte)(ch + 0x40); pending = false; }
                chars.Add(ch);
            }
        }

        return Encoding.Latin1.GetString(chars.ToArray());
    }

    public static byte[] Encode(string content)
    {
        byte[] src = Encoding.UTF8.GetBytes(content);
        if (src.Length != content.Length)
            throw new NotSupportedException("设置内容含非 ASCII 字符，本实现不支持写入。");

        bool[] evil = new bool[Magic.Length];
        for (int i = 0; i < Magic.Length; i++) evil[i] = (Magic[i] & 0xC0) == 0xC0;

        var body = new List<byte>(src.Length + 16);
        int i2 = 0;
        for (int j = 0; j < src.Length; j++)
        {
            int n = i2 % Magic.Length;
            byte ch = src[j];
            if (evil[n])
            {
                byte flag = 0;
                if (src[j] > 0x40) { ch = (byte)(ch - 0x40); flag = 1; }
                body.Add((byte)(flag ^ Magic[n]));
                n = ++i2 % Magic.Length;
            }
            body.Add((byte)(ch ^ Magic[n]));
            i2++;
        }

        var lenBytes = new List<byte>();
        int v = body.Count;
        while (v >= 0x80) { lenBytes.Add((byte)((v & 0x7F) | 0x80)); v >>= 7; }
        lenBytes.Add((byte)v);

        var outBuf = new byte[Header.Length + lenBytes.Count + body.Count + 1];
        int o = 0;
        Buffer.BlockCopy(Header, 0, outBuf, o, Header.Length); o += Header.Length;
        lenBytes.CopyTo(outBuf, o); o += lenBytes.Count;
        body.CopyTo(outBuf, o); o += body.Count;
        outBuf[o] = Footer;
        return outBuf;
    }
}
