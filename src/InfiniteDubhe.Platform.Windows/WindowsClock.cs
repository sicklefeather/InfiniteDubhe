using System.Diagnostics;
using InfiniteDubhe.Platform;

namespace InfiniteDubhe.Platform.Windows;

/// <summary>基于 Stopwatch 的高精度时钟。</summary>
public sealed class WindowsClock : IClock
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _lastTicks;

    public float Tick()
    {
        var now = _stopwatch.ElapsedTicks;
        var delta = now - _lastTicks;
        _lastTicks = now;
        return (float)(delta / (double)Stopwatch.Frequency);
    }

    public float TotalSeconds => (float)(_stopwatch.ElapsedTicks / (double)Stopwatch.Frequency);
}
