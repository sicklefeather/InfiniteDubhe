namespace InfiniteDubhe.Platform;

/// <summary>高精度时钟抽象。</summary>
public interface IClock
{
    /// <summary>返回自上次调用以来的秒数，并推进基准。</summary>
    float Tick();

    /// <summary>累计运行时间（秒）。</summary>
    float TotalSeconds { get; }
}
