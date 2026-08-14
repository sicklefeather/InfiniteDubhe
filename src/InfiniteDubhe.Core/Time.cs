namespace InfiniteDubhe.Core;

/// <summary>
/// 引擎统一时间源。由主循环（Engine.GameHost）每帧通过 <see cref="AdvanceFrame"/> 写入。
/// </summary>
public static class Time
{
    /// <summary>当前帧的原始间隔（秒，未缩放）。</summary>
    public static float DeltaTime { get; private set; }

    /// <summary>固定步长（秒），默认 1/60。</summary>
    public static float FixedDeltaTime { get; set; } = 1f / 60f;

    /// <summary>时间缩放（0 = 暂停）。</summary>
    public static float TimeScale { get; set; } = 1f;

    /// <summary>累计运行时间（秒，未缩放）。</summary>
    public static float TotalTime { get; private set; }

    /// <summary>缩放后的帧间隔（DeltaTime * TimeScale）。</summary>
    public static float ScaledDeltaTime => DeltaTime * TimeScale;

    /// <summary>由主循环每帧调用，推进时间。</summary>
    public static void AdvanceFrame(float deltaSeconds)
    {
        DeltaTime = deltaSeconds;
        TotalTime += deltaSeconds;
    }
}
