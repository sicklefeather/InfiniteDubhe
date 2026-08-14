using InfiniteDubhe.Core;
using Xunit;

namespace InfiniteDubhe.Core.Tests;

public class TimeTests
{
    [Fact]
    public void AdvanceFrame_UpdatesDeltaTime()
    {
        Time.AdvanceFrame(0.5f);
        Assert.Equal(0.5f, Time.DeltaTime);
    }

    [Fact]
    public void ScaledDeltaTime_AppliesTimeScale()
    {
        Time.AdvanceFrame(2f);
        Time.TimeScale = 0.5f;

        try
        {
            Assert.Equal(1f, Time.ScaledDeltaTime);
        }
        finally
        {
            Time.TimeScale = 1f;
        }
    }
}
