namespace InfiniteDubhe.Core;

/// <summary>全局事件门面。</summary>
public static class Events
{
    public static IEventBus Global { get; } = new EventBus();

    public static void Publish<TEvent>(TEvent e) => Global.Publish(e);

    public static IDisposable Subscribe<TEvent>(Action<TEvent> handler) => Global.Subscribe(handler);
}
