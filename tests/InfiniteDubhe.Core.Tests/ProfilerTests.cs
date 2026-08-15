using Xunit;

namespace InfiniteDubhe.Core.Tests;

public sealed class ProfilerTests
{
    [Fact]
    public void Samples_FrameDrawCallsAndAllocation()
    {
        Profiler.Enabled = true;

        Profiler.BeginFrame();
        Profiler.RecordDrawCall();
        Profiler.RecordDrawCall();
        Profiler.RecordDrawCall();
        Profiler.BeginPhase(ProfilerPhase.Update);
        Profiler.EndPhase(ProfilerPhase.Update);
        Profiler.BeginPhase(ProfilerPhase.Render);
        Profiler.EndPhase(ProfilerPhase.Render);
        Profiler.EndFrame();

        Assert.Equal(3, Profiler.DrawCalls);
        Assert.True(Profiler.UpdateTimeMs >= 0f);
        Assert.True(Profiler.RenderTimeMs >= 0f);
        Assert.True(Profiler.FrameTimeMs >= 0f);
        Assert.True(Profiler.AllocatedBytes >= 0);

        // 下一帧重置计数器。
        Profiler.BeginFrame();
        Assert.Equal(0, Profiler.DrawCalls);
        Profiler.EndFrame();
    }

    [Fact]
    public void Disabled_SkipsSampling()
    {
        Profiler.Enabled = false;
        Profiler.BeginFrame();
        Profiler.RecordDrawCall();
        Profiler.EndFrame();

        Assert.Equal(0, Profiler.DrawCalls);
        Profiler.Enabled = true;
    }
}
