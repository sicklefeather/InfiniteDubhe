namespace InfiniteDubhe.Core;

/// <summary>发布/订阅事件总线。</summary>
public interface IEventBus
{
    /// <summary>订阅事件，返回可释放句柄（释放即取消订阅）。</summary>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler);

    /// <summary>取消订阅。</summary>
    void Unsubscribe<TEvent>(Action<TEvent> handler);

    /// <summary>发布事件。</summary>
    void Publish<TEvent>(TEvent e);
}
