using Silk.NET.OpenAL;

namespace InfiniteDubhe.Audio;

/// <summary>
/// OpenAL 后端：设备/上下文生命周期、缓冲上传与音源创建。
/// OpenAL 未安装时 <see cref="IsAvailable"/> 为 false，所有操作静默降级为空操作。
/// </summary>
internal unsafe sealed class AudioManager : IDisposable
{
    private readonly AL? _al;
    private readonly ALContext? _alc;
    private readonly List<AudioVoice> _voices = new();
    private readonly Dictionary<AudioClip, uint> _buffers = new();

    private Device* _device;
    private Context* _context;
    private float _masterVolume = 1f;
    private bool _disposed;

    /// <summary>OpenAL 是否成功初始化。</summary>
    public bool IsAvailable { get; }

    /// <summary>主音量（作用于监听器增益，0..1）。</summary>
    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = value;
            if (IsAvailable) _al!.SetListenerProperty(ListenerFloat.Gain, value);
        }
    }

    public AudioManager()
    {
        try
        {
            _al = AL.GetApi(true);
            _alc = ALContext.GetApi(true);
            _device = _alc.OpenDevice(null);
            if (_device == null) return;
            _context = _alc.CreateContext(_device, null);
            if (_context == null || !_alc.MakeContextCurrent(_context)) return;

            // 2D 音频：关闭距离衰减，音量仅由增益/音调决定。
            _al.DistanceModel(DistanceModel.None);
            _al.SetListenerProperty(ListenerFloat.Gain, _masterVolume);
            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
        }
    }

    internal AudioVoice CreateVoice(AudioClip clip, float volume, float pitch, bool loop)
    {
        uint buffer = UploadBuffer(clip);
        uint source = _al!.GenSource();
        _al.SetSourceProperty(source, SourceInteger.Buffer, buffer);
        _al.SetSourceProperty(source, SourceFloat.Gain, volume);
        _al.SetSourceProperty(source, SourceFloat.Pitch, pitch);
        _al.SetSourceProperty(source, SourceBoolean.Looping, loop);

        var voice = new AudioVoice(this, clip, source, volume, pitch, loop);
        _voices.Add(voice);
        return voice;
    }

    private uint UploadBuffer(AudioClip clip)
    {
        if (_buffers.TryGetValue(clip, out var existing)) return existing;

        var format = clip.Channels == 2 ? BufferFormat.Stereo16 : BufferFormat.Mono16;
        uint buffer = _al!.GenBuffer();
        _al.BufferData(buffer, format, clip.Samples, clip.SampleRate);
        _buffers[clip] = buffer;
        return buffer;
    }

    internal void Play(uint source) => _al!.SourcePlay(source);
    internal void Pause(uint source) => _al!.SourcePause(source);
    internal void Stop(uint source) => _al!.SourceStop(source);
    internal void SetVolume(uint source, float v) => _al!.SetSourceProperty(source, SourceFloat.Gain, v);
    internal void SetPitch(uint source, float p) => _al!.SetSourceProperty(source, SourceFloat.Pitch, p);
    internal void SetLoop(uint source, bool loop) => _al!.SetSourceProperty(source, SourceBoolean.Looping, loop);

    internal SourceState GetState(uint source)
    {
        _al!.GetSourceProperty(source, GetSourceInteger.SourceState, out int state);
        return (SourceState)state;
    }

    internal void ReleaseVoice(AudioVoice voice)
    {
        if (!_voices.Remove(voice)) return;
        _al!.SourceStop(voice.Source);
        _al!.DeleteSource(voice.Source);
    }

    internal void Update(float deltaSeconds)
    {
        for (int i = _voices.Count - 1; i >= 0; i--)
        {
            var voice = _voices[i];
            voice.Tick(deltaSeconds);
            // 一次性音效播放完毕（Stopped/Initial）后回收音源。
            if (!voice.Loop && voice.IsFinished)
                voice.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (IsAvailable)
        {
            foreach (var voice in _voices.ToArray()) voice.Dispose();
            foreach (var buffer in _buffers.Values) _al!.DeleteBuffer(buffer);
            _buffers.Clear();
        }

        if (_context != null && _alc is not null)
        {
            _alc.MakeContextCurrent((Context*)null);
            _alc.DestroyContext(_context);
        }
        if (_device != null && _alc is not null)
            _alc.CloseDevice(_device);
    }
}
