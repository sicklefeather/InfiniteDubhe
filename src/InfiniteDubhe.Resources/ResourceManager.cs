using InfiniteDubhe.Core;

namespace InfiniteDubhe.Resources;

/// <summary>
/// 资源管理器：按类型分派到已注册的 <see cref="IResourceLoader{T}"/>，带引用计数缓存。
/// 同路径资源复用同一实例，<see cref="Unload"/> 归零后才真正释放（若实现 <see cref="IDisposable"/>）。
/// </summary>
public sealed class ResourceManager
{
    private readonly Dictionary<Type, object> _loaders = new();
    private readonly Dictionary<string, Entry> _cache = new(StringComparer.Ordinal);
    private readonly Dictionary<object, string> _paths = new();

    /// <summary>资源热重载/变更触发点（M3 落地）。</summary>
    public event Action<string>? ResourceChanged;

    private sealed class Entry
    {
        public required object Resource;
        public int RefCount;
    }

    /// <summary>注册某类资源的加载器。</summary>
    public void RegisterLoader<T>(IResourceLoader<T> loader) where T : class
    {
        ArgumentNullException.ThrowIfNull(loader);
        _loaders[typeof(T)] = loader;
    }

    /// <summary>同步加载（带引用计数，同路径复用）。</summary>
    public T Load<T>(string path) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (_cache.TryGetValue(path, out var entry))
        {
            entry.RefCount++;
            return (T)entry.Resource;
        }

        var loader = GetLoader<T>();
        var resource = loader.Load(path);
        _cache[path] = new Entry { Resource = resource, RefCount = 1 };
        _paths[resource] = path;
        return resource;
    }

    /// <summary>反向查询某已缓存资源的加载路径（未缓存则返回 null）。供场景序列化把纹理句柄还原为路径。</summary>
    public string? GetPath(object resource)
        => _paths.TryGetValue(resource, out var path) ? path : null;

    /// <summary>异步加载（M1：后台线程包装同步加载）。</summary>
    public Task<T> LoadAsync<T>(string path) where T : class
        => Task.Run(() => Load<T>(path));

    /// <summary>释放一次引用；归零才真正释放。</summary>
    public void Unload(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!_cache.TryGetValue(path, out var entry)) return;
        if (--entry.RefCount > 0) return;

        _cache.Remove(path);
        _paths.Remove(entry.Resource);
        if (entry.Resource is IDisposable disposable) disposable.Dispose();
        ResourceChanged?.Invoke(path);
    }

    private IResourceLoader<T> GetLoader<T>() where T : class
    {
        if (_loaders.TryGetValue(typeof(T), out var loader) && loader is IResourceLoader<T> typed)
            return typed;

        throw new InvalidOperationException($"未注册资源类型 {typeof(T).Name} 的加载器。");
    }
}
