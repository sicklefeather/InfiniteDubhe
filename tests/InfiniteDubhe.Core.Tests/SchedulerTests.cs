using System.Collections;
using InfiniteDubhe.Core;
using Xunit;

namespace InfiniteDubhe.Core.Tests;

public class SchedulerTests
{
    [Fact]
    public void Invoke_ZeroDelay_RunsAfterFirstUpdate()
    {
        var s = new Scheduler();
        var ran = 0;
        s.Invoke(() => ran++, 0f);

        Assert.Equal(0, ran);          // 尚未推进
        s.Update(0f);
        Assert.Equal(1, ran);          // 首帧推进后执行
    }

    [Fact]
    public void Invoke_PositiveDelay_WaitsThenRuns()
    {
        var s = new Scheduler();
        var ran = 0;
        s.Invoke(() => ran++, 1f);

        s.Update(0.4f);
        Assert.Equal(0, ran);
        s.Update(0.4f);
        Assert.Equal(0, ran);
        s.Update(0.3f);                // 累计 1.1s ≥ 1s
        Assert.Equal(1, ran);
    }

    [Fact]
    public void Coroutine_WaitForSeconds_YieldsAcrossFrames()
    {
        var s = new Scheduler();
        var log = new List<int>();
        s.StartCoroutine(Seq(log));

        s.Update(0.01f);               // 首帧：执行到 yield
        Assert.Equal(new[] { 1 }, log);

        s.Update(0.01f);               // 等待中
        s.Update(0.01f);
        Assert.Equal(new[] { 1 }, log);

        s.Update(0.2f);                // 累计 ≥ 0.1s，续跑到 yield null
        Assert.Equal(new[] { 1, 2 }, log);

        s.Update(0.01f);               // 下一帧：yield null 恢复
        Assert.Equal(new[] { 1, 2, 3 }, log);
    }

    [Fact]
    public void Coroutine_WaitForFixedUpdate_RequiresFixedStep()
    {
        var s = new Scheduler();
        var log = new List<int>();
        s.StartCoroutine(FixedSeq(log));

        s.Update(0.016f);              // 首帧执行到 WaitForFixedUpdate
        Assert.Equal(new[] { 1 }, log);

        s.Update(0.016f);              // 未到固定步长，继续等
        Assert.Equal(new[] { 1 }, log);

        s.FixedUpdate();
        s.Update(0.016f);              // 固定步长后恢复
        Assert.Equal(new[] { 1, 2 }, log);
    }

    [Fact]
    public void Stop_MarksFinished()
    {
        var s = new Scheduler();
        var ran = 0;
        var c = s.StartCoroutine(NeverEnding(() => ran++));

        s.Update(0.016f);
        Assert.Equal(1, ran);

        s.Stop(c);
        Assert.True(c.IsFinished);
        s.Update(0.016f);
        Assert.Equal(1, ran);          // 停止后不再推进
        Assert.Equal(0, s.ActiveCount); // 已移除
    }

    private static IEnumerator Seq(List<int> log)
    {
        log.Add(1);
        yield return new WaitForSeconds(0.1f);
        log.Add(2);
        yield return null;             // 等待一帧
        log.Add(3);
    }

    private static IEnumerator FixedSeq(List<int> log)
    {
        log.Add(1);
        yield return new WaitForFixedUpdate();
        log.Add(2);
    }

    private static IEnumerator NeverEnding(Action tick)
    {
        while (true)
        {
            tick();
            yield return null;
        }
    }
}
