using System.Buffers.Binary;

namespace InfiniteDubhe.Audio;

/// <summary>WAV (RIFF/PCM) 解码器：支持 8/16-bit、单/双声道，统一输出 16-bit PCM。</summary>
internal static class WavDecoder
{
    public static AudioClip Decode(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Span<byte> header = stackalloc byte[12];
        if (stream.Read(header) != header.Length)
            throw new InvalidDataException("文件过短，不是有效的 WAV。");
        if (!header[..4].SequenceEqual("RIFF"u8) || !header[8..12].SequenceEqual("WAVE"u8))
            throw new InvalidDataException("缺少 RIFF/WAVE 标记。");

        int channels = 0, sampleRate = 0, bits = 0;
        short[]? samples = null;

        Span<byte> chunk = stackalloc byte[8];
        Span<byte> fmt = stackalloc byte[16];
        while (stream.Read(chunk) == chunk.Length)
        {
            uint size = BinaryPrimitives.ReadUInt32LittleEndian(chunk[4..]);

            if (chunk[..4].SequenceEqual("fmt "u8))
            {
                if (stream.Read(fmt) != fmt.Length)
                    throw new InvalidDataException("fmt 块不完整。");

                short audioFormat = BinaryPrimitives.ReadInt16LittleEndian(fmt[..2]);
                channels = BinaryPrimitives.ReadInt16LittleEndian(fmt[2..4]);
                sampleRate = (int)BinaryPrimitives.ReadUInt32LittleEndian(fmt[4..8]);
                bits = BinaryPrimitives.ReadInt16LittleEndian(fmt[14..16]);

                if (audioFormat != 1)
                    throw new NotSupportedException($"仅支持 PCM 格式 WAV（当前 format={audioFormat}）。");
                if (bits != 8 && bits != 16)
                    throw new NotSupportedException($"仅支持 8/16-bit PCM（当前 bits={bits}）。");

                int extra = (int)size - 16; // 跳过 fmt 块剩余部分（如 WAVE_FORMAT_EXTENSIBLE）
                if (extra > 0) stream.Position += extra;
            }
            else if (chunk[..4].SequenceEqual("data"u8))
            {
                samples = ReadSamples(stream, (int)size, bits);
                break;
            }
            else
            {
                stream.Position += (int)size + (int)(size & 1); // 未知块：跳过（按 2 字节对齐）
            }
        }

        if (samples is null)
            throw new InvalidDataException("未找到 data 块。");

        return new AudioClip(samples, sampleRate, channels);
    }

    private static short[] ReadSamples(Stream stream, int dataSize, int bits)
    {
        var raw = new byte[dataSize];
        stream.ReadExactly(raw);

        int bytesPerSample = bits / 8;
        var samples = new short[dataSize / bytesPerSample];

        if (bits == 16)
        {
            for (int i = 0; i < samples.Length; i++)
                samples[i] = BinaryPrimitives.ReadInt16LittleEndian(raw.AsSpan(i * 2, 2));
        }
        else
        {
            for (int i = 0; i < samples.Length; i++)
                samples[i] = (short)((raw[i] - 128) << 8); // 8-bit 无符号 → 居中到有符号 16-bit
        }
        return samples;
    }
}
