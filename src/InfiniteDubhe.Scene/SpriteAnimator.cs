using InfiniteDubhe.Core;

namespace InfiniteDubhe.Scene;

/// <summary>
/// 帧动画组件：按名称播放 <see cref="SpriteAnimationClip"/>，每帧推进并把当前帧源矩形写入 <see cref="SpriteRenderer.SourceRect"/>。
/// </summary>
public sealed class SpriteAnimator : Component
{
    private readonly Dictionary<string, SpriteAnimationClip> _clips = new();
    private SpriteRenderer? _renderer;
    private SpriteAnimationClip? _current;
    private int _frameIndex;
    private float _frameTimer;
    private bool _playing;
    private bool _paused;

    /// <summary>目标精灵渲染器；Awake 时自动挂接同对象上的 <see cref="SpriteRenderer"/>（可手动覆盖）。</summary>
    public SpriteRenderer? Renderer { get => _renderer; set => _renderer = value; }

    public SpriteAnimationClip? CurrentClip => _current;
    public int CurrentFrame => _frameIndex;
    public bool IsPlaying => _playing && !_paused;

    /// <summary>播放速度倍率（1 = 原速）。</summary>
    public float Speed { get; set; } = 1f;

    protected override void Awake() => _renderer ??= GameObject.GetComponent<SpriteRenderer>();

    public void AddClip(SpriteAnimationClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        _clips[clip.Name] = clip;
    }

    public bool RemoveClip(string name) => _clips.Remove(name);

    /// <summary>播放指定名称的片段（不存在则忽略）。</summary>
    public void Play(string name)
    {
        if (!_clips.TryGetValue(name, out var clip)) return;
        _current = clip;
        _frameIndex = 0;
        _frameTimer = 0f;
        _playing = true;
        _paused = false;
        ApplyFrame();
    }

    public void Stop() { _playing = false; _paused = false; }
    public void Pause() => _paused = true;
    public void Resume() => _paused = false;

    protected override void Update()
    {
        if (!_playing || _paused || _current is null) return;
        var frames = _current.Frames;
        if (frames.Count == 0) return;

        _frameTimer += Time.ScaledDeltaTime * Speed;
        while (_frameTimer >= frames[_frameIndex].DurationSeconds)
        {
            _frameTimer -= frames[_frameIndex].DurationSeconds;
            _frameIndex++;
            if (_frameIndex >= frames.Count)
            {
                if (_current.Loop) { _frameIndex = 0; continue; }
                _frameIndex = frames.Count - 1;
                _playing = false;
                break;
            }
        }
        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (_renderer is null || _current is null || _current.Frames.Count == 0) return;
        _renderer.SourceRect = _current.Frames[_frameIndex].SourceRect;
    }
}
