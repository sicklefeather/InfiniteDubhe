using System.Numerics;

namespace InfiniteDubhe.Core;

/// <summary>
/// 补间/插值门面：对任意值（float / Vector2 / Color）在指定时长内按缓动函数过渡到目标值。
/// 由引擎运行时每帧 <see cref="Update"/> 推进（缩放时间）；用法对齐 DOTween 的 Tween.To。
/// </summary>
public static class Tween
{
    private static readonly List<TweenHandle> _active = new();

    // ---- float ----
    public static TweenHandle To(Func<float> getter, Action<float> setter, float to, float duration, Ease ease = Ease.Linear, Action? onComplete = null)
        => FromTo(setter, getter(), to, duration, ease, onComplete);

    public static TweenHandle FromTo(Action<float> setter, float from, float to, float duration, Ease ease = Ease.Linear, Action? onComplete = null)
    {
        ArgumentNullException.ThrowIfNull(setter);
        return Start(duration, ease, t => setter(from + (to - from) * t), onComplete);
    }

    // ---- Vector2 ----
    public static TweenHandle To(Func<Vector2> getter, Action<Vector2> setter, Vector2 to, float duration, Ease ease = Ease.Linear, Action? onComplete = null)
        => FromTo(setter, getter(), to, duration, ease, onComplete);

    public static TweenHandle FromTo(Action<Vector2> setter, Vector2 from, Vector2 to, float duration, Ease ease = Ease.Linear, Action? onComplete = null)
    {
        ArgumentNullException.ThrowIfNull(setter);
        return Start(duration, ease, t => setter(Vector2.Lerp(from, to, t)), onComplete);
    }

    // ---- Color ----
    public static TweenHandle To(Func<Color> getter, Action<Color> setter, Color to, float duration, Ease ease = Ease.Linear, Action? onComplete = null)
        => FromTo(setter, getter(), to, duration, ease, onComplete);

    public static TweenHandle FromTo(Action<Color> setter, Color from, Color to, float duration, Ease ease = Ease.Linear, Action? onComplete = null)
    {
        ArgumentNullException.ThrowIfNull(setter);
        return Start(duration, ease, t => setter(Color.Lerp(from, to, t)), onComplete);
    }

    /// <summary>停止全部补间（不触发 OnComplete）。</summary>
    public static void StopAll()
    {
        foreach (var h in _active) h.Stop();
        _active.Clear();
    }

    /// <summary>每帧推进（<paramref name="dt"/> 为缩放后的帧间隔）。</summary>
    internal static void Update(float dt)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
            if (!_active[i].Tick(dt))
                _active.RemoveAt(i);
    }

    private static TweenHandle Start(float duration, Ease ease, Action<float> apply, Action? onComplete)
    {
        var handle = new TweenHandle(duration, ease, apply, onComplete);
        if (duration <= 0f) handle.CompleteImmediately();
        else _active.Add(handle);
        return handle;
    }
}

/// <summary>补间句柄：标识一个运行中的补间，用于查询/暂停/恢复/停止。</summary>
public sealed class TweenHandle
{
    private readonly Action<float> _apply;
    private readonly float _duration;
    private readonly Ease _ease;
    private float _elapsed;
    private bool _paused;
    private bool _stopped;

    /// <summary>是否正在运行（暂停/停止/完成均为 false）。</summary>
    public bool IsPlaying { get; private set; } = true;

    /// <summary>完成回调（自然完成触发；<see cref="Stop"/> 不触发）。</summary>
    public Action? OnComplete { get; set; }

    internal TweenHandle(float duration, Ease ease, Action<float> apply, Action? onComplete)
    {
        _duration = Math.Max(0f, duration);
        _ease = ease;
        _apply = apply;
        OnComplete = onComplete;
    }

    /// <summary>推进一帧；返回 true 表示仍在运行，false 表示已结束。</summary>
    internal bool Tick(float dt)
    {
        if (_stopped) return false;
        if (_paused) return true;

        _elapsed += dt;
        float t = _duration <= 0f ? 1f : _elapsed / _duration;
        if (t >= 1f)
        {
            _apply(1f);
            Complete();
            return false;
        }
        _apply(Easing.Apply(_ease, t));
        return true;
    }

    internal void CompleteImmediately()
    {
        if (_stopped) return;
        _apply(1f);
        Complete();
    }

    private void Complete()
    {
        _stopped = true;
        IsPlaying = false;
        OnComplete?.Invoke();
    }

    public void Pause() { if (!_stopped) { _paused = true; IsPlaying = false; } }
    public void Resume() { if (!_stopped) { _paused = false; IsPlaying = true; } }
    public void Stop() { _stopped = true; IsPlaying = false; }
}
