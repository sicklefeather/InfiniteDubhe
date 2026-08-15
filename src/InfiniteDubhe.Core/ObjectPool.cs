namespace InfiniteDubhe.Core;

/// <summary>
/// 简单对象池（M3）：复用实例避免每帧分配（对应 NFR-03）。
/// 池空时用工厂创建；<c>onRent</c>/<c>onReturn</c> 用于取出/归还时的重置。
/// </summary>
public sealed class ObjectPool<T> where T : class
{
    private readonly Func<T> _factory;
    private readonly Action<T>? _onRent;
    private readonly Action<T>? _onReturn;
    private readonly Stack<T> _items = new();

    public ObjectPool(Func<T> factory, Action<T>? onRent = null, Action<T>? onReturn = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _onRent = onRent;
        _onReturn = onReturn;
    }

    /// <summary>当前池内可复用实例数。</summary>
    public int Count => _items.Count;

    /// <summary>取一个实例（池空则新建）。</summary>
    public T Rent()
    {
        var item = _items.Count > 0 ? _items.Pop() : _factory();
        _onRent?.Invoke(item);
        return item;
    }

    /// <summary>归还实例供后续复用。</summary>
    public void Return(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _onReturn?.Invoke(item);
        _items.Push(item);
    }
}
