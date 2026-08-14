namespace InfiniteDubhe.Resources;

/// <summary>某类资源的加载器。按类型注册到 <see cref="ResourceManager"/>，由其分派。</summary>
public interface IResourceLoader<T> where T : class
{
    /// <summary>同步加载资源（路径相对资源根目录）。</summary>
    T Load(string path);
}
