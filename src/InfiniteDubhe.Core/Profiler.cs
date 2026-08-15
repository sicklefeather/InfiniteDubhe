using System.Diagnostics;

namespace InfiniteDubhe.Core;

/// <summary>性能分析采样阶段。</summary>
public enum ProfilerPhase
{
    FixedUpdate,
    Update,
    Render,
}

/// <summary>
/// 性能分析器（M3）：统计帧耗时（总/分阶段）、Draw Call 数与每帧 GC 分配。
/// 由主循环（GameHost）与渲染器（SpriteBatch）写入；<see cref="Enabled"/> 为 false 时零开销（仅一次布尔判断）。
/// </summary>
public static class Profiler
{
    private static long _frameStart;
    private static long _phaseStart;
    private static long _allocStart;

    /// <summary>是否开启采样。关闭时所有方法直接返回，无计时开销。</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>当前帧总耗时（毫秒）。</summary>
    public static float FrameTimeMs { get; private set; }

    /// <summary>本帧固定步长阶段累计耗时（毫秒，多次 FixedUpdate 求和）。</summary>
    public static float FixedUpdateTimeMs { get; private set; }

    /// <summary>本帧 Update 阶段耗时（毫秒）。</summary>
    public static float UpdateTimeMs { get; private set; }

    /// <summary>本帧 Render 阶段耗时（毫秒）。</summary>
    public static float RenderTimeMs { get; private set; }

    /// <summary>本帧 Draw Call 数量（由 SpriteBatch 上报）。</summary>
    public static int DrawCalls { get; private set; }

    /// <summary>本帧主线程 GC 分配字节数。</summary>
    public static long AllocatedBytes { get; private set; }

    /// <summary>按帧耗时推算的 FPS。</summary>
    public static double Fps { get; private set; }

    internal static void BeginFrame()
    {
        if (!Enabled) return;
        _frameStart = Stopwatch.GetTimestamp();
        _allocStart = GC.GetAllocatedBytesForCurrentThread();
        DrawCalls = 0;
        FixedUpdateTimeMs = UpdateTimeMs = RenderTimeMs = 0f;
    }

    internal static void BeginPhase(ProfilerPhase phase)
    {
        if (!Enabled) return;
        _phaseStart = Stopwatch.GetTimestamp();
    }

    internal static void EndPhase(ProfilerPhase phase)
    {
        if (!Enabled) return;
        float ms = (Stopwatch.GetTimestamp() - _phaseStart) * 1000f / Stopwatch.Frequency;
        switch (phase)
        {
            case ProfilerPhase.FixedUpdate: FixedUpdateTimeMs += ms; break;
            case ProfilerPhase.Update: UpdateTimeMs += ms; break;
            case ProfilerPhase.Render: RenderTimeMs += ms; break;
        }
    }

    internal static void RecordDrawCall()
    {
        if (!Enabled) return;
        DrawCalls++;
    }

    internal static void EndFrame()
    {
        if (!Enabled) return;
        FrameTimeMs = (Stopwatch.GetTimestamp() - _frameStart) * 1000f / Stopwatch.Frequency;
        AllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - _allocStart;
        Fps = FrameTimeMs > 0f ? 1000f / FrameTimeMs : 0.0;
    }
}
