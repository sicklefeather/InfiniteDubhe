using System.Collections;

namespace InfiniteDubhe.Core;

/// <summary>
/// 延时调用与协程调度器。由主循环每帧 <see cref="Update"/> 推进，
/// 固定步长经 <see cref="FixedUpdate"/> 释放 <see cref="WaitForFixedUpdate"/> 等待。
/// </summary>
public sealed class Scheduler
{
    private readonly List<Coroutine> _coroutines = new();
    private readonly List<Coroutine> _pending = new();

    /// <summary>当前活动（含尚未启动）协程数量。</summary>
    public int ActiveCount => _coroutines.Count + _pending.Count;

    /// <summary>启动协程；首次推进发生在下一次 <see cref="Update"/>。</summary>
    public Coroutine StartCoroutine(IEnumerator routine)
    {
        ArgumentNullException.ThrowIfNull(routine);
        var c = new Coroutine(routine);
        _pending.Add(c);   // 延迟到下一次 Update 提交，避免迭代中修改集合
        return c;
    }

    /// <summary>延时调用：<paramref name="delaySeconds"/> 秒后执行一次 <paramref name="action"/>。</summary>
    public Coroutine Invoke(Action action, float delaySeconds)
    {
        ArgumentNullException.ThrowIfNull(action);
        return StartCoroutine(DelayThen(action, delaySeconds));
    }

    /// <summary>停止指定协程（若尚未结束）。</summary>
    public void Stop(Coroutine coroutine)
    {
        ArgumentNullException.ThrowIfNull(coroutine);
        coroutine.Stop();
    }

    /// <summary>停止全部协程。</summary>
    public void StopAll()
    {
        foreach (var c in _coroutines) c.Stop();
        _coroutines.Clear();
        _pending.Clear();
    }

    /// <summary>每帧推进协程（<paramref name="dt"/> 为缩放后的帧间隔）。</summary>
    public void Update(float dt)
    {
        if (_pending.Count > 0)
        {
            _coroutines.AddRange(_pending);
            _pending.Clear();
        }

        for (var i = _coroutines.Count - 1; i >= 0; i--)
        {
            if (_coroutines[i].Step(dt)) continue;
            _coroutines.RemoveAt(i);
        }
    }

    /// <summary>固定步长：释放所有等待固定步长的协程一档。</summary>
    public void FixedUpdate()
    {
        foreach (var c in _coroutines) c.NotifyFixedUpdate();
    }

    private static IEnumerator DelayThen(Action action, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        action();
    }
}

/// <summary>协程句柄：标识一个运行中的协程，用于查询/停止。</summary>
public sealed class Coroutine
{
    private readonly IEnumerator _enumerator;
    private float _waitSeconds;
    private int _fixedWaits;
    private bool _waitFrame;

    /// <summary>协程是否已结束（自然结束或已停止）。</summary>
    public bool IsFinished { get; private set; }

    internal Coroutine(IEnumerator enumerator) => _enumerator = enumerator;

    internal void Stop() => IsFinished = true;

    internal void NotifyFixedUpdate()
    {
        if (_fixedWaits > 0) _fixedWaits--;
    }

    /// <summary>推进一帧；返回 true 表示仍在运行，false 表示已结束。</summary>
    internal bool Step(float dt)
    {
        if (IsFinished) return false;

        if (_waitSeconds > 0f)
        {
            _waitSeconds -= dt;
            if (_waitSeconds > 0f) return true;
        }
        else if (_fixedWaits > 0)
        {
            return true;   // 等待下一次固定步长
        }
        else if (_waitFrame)
        {
            _waitFrame = false;
        }

        while (true)
        {
            if (!_enumerator.MoveNext())
            {
                IsFinished = true;
                return false;
            }

            switch (_enumerator.Current)
            {
                case null:
                    _waitFrame = true;   // 等待一帧
                    return true;
                case WaitForSeconds ws:
                    _waitSeconds = ws.Seconds - dt;   // 本帧即开始计时
                    if (_waitSeconds > 0f) return true;
                    break;               // 本帧内已到期：立即继续
                case WaitForFixedUpdate:
                    _fixedWaits = 1;     // 等待下一次固定步长
                    return true;
                default:
                    break;               // 未知 yield 指令：忽略并继续（暂不支持嵌套协程）
            }
        }
    }
}

/// <summary>协程 yield 指令：等待固定秒数（缩放时间，受 TimeScale 影响）。</summary>
public readonly struct WaitForSeconds
{
    public float Seconds { get; }

    public WaitForSeconds(float seconds) => Seconds = Math.Max(0f, seconds);
}

/// <summary>协程 yield 指令：等待下一次固定步长（FixedUpdate）。</summary>
public readonly struct WaitForFixedUpdate { }
