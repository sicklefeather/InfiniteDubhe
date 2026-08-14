using System.Collections;

namespace InfiniteDubhe.Core;

/// <summary>协程/延时调用全局门面（等价于 Unity 的 StartCoroutine/Invoke）。</summary>
public static class Coroutines
{
    /// <summary>全局调度器，由引擎运行时（Engine.GameHost）每帧推进。</summary>
    public static Scheduler Global { get; } = new Scheduler();

    public static Coroutine Start(IEnumerator routine) => Global.StartCoroutine(routine);

    public static Coroutine Invoke(Action action, float delaySeconds = 0f) => Global.Invoke(action, delaySeconds);

    public static void Stop(Coroutine coroutine) => Global.Stop(coroutine);

    public static void StopAll() => Global.StopAll();
}
