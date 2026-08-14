using InfiniteDubhe.Core;
using Xunit;

namespace InfiniteDubhe.Core.Tests;

public class EventBusTests
{
    [Fact]
    public void Publish_InvokesSubscribedHandler()
    {
        var bus = new EventBus();
        var received = 0;

        using (bus.Subscribe<int>(i => received = i))
        {
            bus.Publish(42);
        }

        Assert.Equal(42, received);
    }

    [Fact]
    public void Unsubscribe_StopsDelivery()
    {
        var bus = new EventBus();
        var received = 0;

        var subscription = bus.Subscribe<int>(_ => received++);
        bus.Publish(0);
        subscription.Dispose();
        bus.Publish(0);

        Assert.Equal(1, received);
    }
}
