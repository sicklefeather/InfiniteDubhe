namespace InfiniteDubhe.Audio;

/// <summary>
/// 音频门面：SFX / BGM 播放、主音量与生命周期。
/// 由 GameHost 在启动时 <see cref="Initialize"/>、每帧 <see cref="Update"/>、退出时 <see cref="Shutdown"/>。
/// OpenAL 未安装时 <see cref="IsAvailable"/> 为 false，所有播放调用安全地成为空操作。
/// </summary>
public static class Audio
{
    private static AudioManager? _manager;
    private static AudioVoice? _bgm;

    /// <summary>音频后端是否可用（OpenAL 已初始化）。</summary>
    public static bool IsAvailable => _manager?.IsAvailable ?? false;

    /// <summary>当前背景音乐实例（无则 null）。</summary>
    public static AudioVoice? BgmVoice => _bgm;

    /// <summary>主音量（作用于所有声音，0..1）。</summary>
    public static float MasterVolume
    {
        get => _manager?.MasterVolume ?? 1f;
        set { if (_manager is not null) _manager.MasterVolume = value; }
    }

    internal static void Initialize()
    {
        if (_manager is not null) return;
        _manager = new AudioManager();
    }

    internal static void Shutdown()
    {
        _bgm = null;
        _manager?.Dispose();
        _manager = null;
    }

    internal static void Update(float deltaSeconds)
    {
        if (_manager is { IsAvailable: true } manager)
            manager.Update(deltaSeconds);
    }

    /// <summary>播放一次性音效。</summary>
    public static AudioVoice? PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        => Play(clip, false, volume, pitch);

    /// <summary>播放声音（可循环）。</summary>
    public static AudioVoice? Play(AudioClip clip, bool loop = false, float volume = 1f, float pitch = 1f)
    {
        if (clip is null) return null;
        if (_manager is not { IsAvailable: true } manager) return null;

        var voice = manager.CreateVoice(clip, volume, pitch, loop);
        voice.Play();
        return voice;
    }

    /// <summary>切换背景音乐（循环播放，替换当前 BGM）。</summary>
    public static void PlayBgm(AudioClip clip, float volume = 1f, float fadeInSeconds = 0f)
    {
        StopBgm();
        var voice = Play(clip, true, volume);
        if (voice is null) return;

        _bgm = voice;
        if (fadeInSeconds > 0f)
        {
            voice.Volume = 0f;
            voice.FadeTo(volume, fadeInSeconds);
        }
    }

    /// <summary>停止当前背景音乐（可淡出）。</summary>
    public static void StopBgm(float fadeOutSeconds = 0f)
    {
        if (_bgm is null) return;
        var old = _bgm;
        _bgm = null;

        if (fadeOutSeconds > 0f)
            old.FadeOut(fadeOutSeconds);
        else
            old.Dispose();
    }

    /// <summary>实时调整当前 BGM 音量（不影响后续 BGM 的默认音量）。</summary>
    public static void SetBgmVolume(float volume)
    {
        if (_bgm is not null) _bgm.Volume = volume;
    }
}
