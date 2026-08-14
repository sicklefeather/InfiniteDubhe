namespace InfiniteDubhe.Core;

/// <summary>线程安全的简单事件总线实现。</summary>
public sealed class EventBus : IEventBus
{
    private readonly Dictionary<Type, object> _handlers = new();
    private readonly object _lock = new();

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_lock)
        {
            GetOrCreateList<TEvent>().Add(handler);
        }
        return new Subscription<TEvent>(this, handler);
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_lock)
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var obj) && obj is List<Action<TEvent>> list)
            {
                list.Remove(handler);
            }
        }
    }

    public void Publish<TEvent>(TEvent e)
    {
        Action<TEvent>[] snapshot;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var obj) || obj is not List<Action<TEvent>> list)
            {
                return;
            }
            snapshot = list.ToArray();
        }

        foreach (var handler in snapshot)
        {
            handler(e);
        }
    }

    private List<Action<TEvent>> GetOrCreateList<TEvent>()
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var obj) && obj is List<Action<TEvent>> list)
        {
            return list;
        }
        var created = new List<Action<TEvent>>();
        _handlers[typeof(TEvent)] = created;
        return created;
    }

    private sealed class Subscription<TEvent> : IDisposable
    {
        private readonly EventBus _bus;
        private readonly Action<TEvent> _handler;

        public Subscription(EventBus bus, Action<TEvent> handler)
        {
            _bus = bus;
            _handler = handler;
        }

        public void Dispose() => _bus.Unsubscribe(_handler);
    }
}
