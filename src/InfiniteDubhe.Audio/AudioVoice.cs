using Silk.NET.OpenAL;

namespace InfiniteDubhe.Audio;

/// <summary>
/// 一个正在播放的声音实例（对应一个 OpenAL 音源）。由 <see cref="Audio"/> 播放产生。
/// 支持音量/音调/循环/暂停与淡入淡出；调用 <see cref="Dispose"/> 立即释放底层音源。
/// </summary>
public sealed class AudioVoice : IDisposable
{
    private readonly AudioManager _manager;

    private float _volume;
    private float _pitch;
    private bool _loop;
    private bool _disposed;

    // 淡入淡出状态
    private float _fadeFrom;
    private float _fadeTo;
    private float _fadeElapsed;
    private float _fadeDuration;
    private Action? _onFadeComplete;

    internal AudioVoice(AudioManager manager, AudioClip clip, uint source, float volume, float pitch, bool loop)
    {
        _manager = manager;
        Clip = clip;
        Source = source;
        _volume = volume;
        _pitch = pitch;
        _loop = loop;
    }

    public AudioClip Clip { get; }
    internal uint Source { get; }

    /// <summary>音量（0..1，与主音量相乘）。设置会取消进行中的淡入淡出。</summary>
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = value;
            _fadeDuration = 0f;
            if (!_disposed) _manager.SetVolume(Source, value);
        }
    }

    /// <summary>音调（1 = 原速，2 = 高八度）。</summary>
    public float Pitch
    {
        get => _pitch;
        set
        {
            _pitch = value;
            if (!_disposed) _manager.SetPitch(Source, value);
        }
    }

    /// <summary>是否循环播放。</summary>
    public bool Loop
    {
        get => _loop;
        set
        {
            _loop = value;
            if (!_disposed) _manager.SetLoop(Source, value);
        }
    }

    /// <summary>当前是否处于播放中（暂停/停止均返回 false）。</summary>
    public bool IsPlaying => !_disposed && _manager.GetState(Source) == SourceState.Playing;

    /// <summary>播放完毕（用于一次性音效的自动回收）。</summary>
    internal bool IsFinished => !_disposed && _manager.GetState(Source) is SourceState.Stopped or SourceState.Initial;

    public void Play() { if (!_disposed) _manager.Play(Source); }
    public void Pause() { if (!_disposed) _manager.Pause(Source); }
    public void Stop() { if (!_disposed) _manager.Stop(Source); }

    /// <summary>在 <paramref name="duration"/> 秒内把音量渐变到 <paramref name="target"/>。</summary>
    public void FadeTo(float target, float duration, Action? onComplete = null)
    {
        if (duration <= 0f)
        {
            Volume = target;
            onComplete?.Invoke();
            return;
        }
        _fadeFrom = _volume;
        _fadeTo = target;
        _fadeElapsed = 0f;
        _fadeDuration = duration;
        _onFadeComplete = onComplete;
    }

    /// <summary>在 <paramref name="duration"/> 秒内淡出到静音，完成后释放音源。</summary>
    public void FadeOut(float duration) => FadeTo(0f, duration, Dispose);

    internal void Tick(float deltaSeconds)
    {
        if (_fadeDuration <= 0f) return;
        _fadeElapsed += deltaSeconds;
        float t = Math.Clamp(_fadeElapsed / _fadeDuration, 0f, 1f);
        _volume = _fadeFrom + (_fadeTo - _fadeFrom) * t;
        _manager.SetVolume(Source, _volume);
        if (t >= 1f)
        {
            _fadeDuration = 0f;
            var cb = _onFadeComplete;
            _onFadeComplete = null;
            cb?.Invoke();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _manager.ReleaseVoice(this);
    }
}
