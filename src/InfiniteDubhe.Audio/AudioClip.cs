namespace InfiniteDubhe.Audio;

/// <summary>
/// 已解码的音频片段（16-bit 交错 PCM）。由 <see cref="FromWav(Stream)"/> 从 WAV 载入，
/// 或经 <see cref="Create"/> 程序化生成；不可变。
/// </summary>
public sealed class AudioClip
{
    /// <summary>交错 PCM 采样（16-bit，单声道顺序 / 双声道 LRLR 交错）。</summary>
    public short[] Samples { get; }

    /// <summary>采样率（Hz）。</summary>
    public int SampleRate { get; }

    /// <summary>声道数（1 = 单声道，2 = 立体声）。</summary>
    public int Channels { get; }

    /// <summary>总时长。</summary>
    public TimeSpan Duration { get; }

    internal AudioClip(short[] samples, int sampleRate, int channels)
    {
        Samples = samples;
        SampleRate = sampleRate;
        Channels = channels;
        Duration = TimeSpan.FromSeconds((double)samples.Length / Math.Max(1, channels) / Math.Max(1, sampleRate));
    }

    /// <summary>从 WAV 文件载入音频。</summary>
    public static AudioClip FromWav(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        using var stream = File.OpenRead(path);
        return FromWav(stream);
    }

    /// <summary>从 WAV 流载入音频（支持 8/16-bit PCM、单/双声道）。</summary>
    public static AudioClip FromWav(Stream stream) => WavDecoder.Decode(stream);

    /// <summary>用原始 PCM 数据程序化构造音频。</summary>
    public static AudioClip Create(short[] samples, int sampleRate, int channels)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (channels != 1 && channels != 2) throw new ArgumentOutOfRangeException(nameof(channels));
        return new AudioClip(samples, sampleRate, channels);
    }
}
