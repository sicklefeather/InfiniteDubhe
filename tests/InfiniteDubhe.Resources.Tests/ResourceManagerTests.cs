using InfiniteDubhe.Core;
using Xunit;

namespace InfiniteDubhe.Resources.Tests;

public sealed class ResourceManagerTests
{
    [Fact]
    public void Reload_ReplacesResourceAndFiresEvent()
    {
        var manager = new ResourceManager();
        var loader = new CountingTextureLoader();
        manager.RegisterLoader<ITexture>(loader);

        var first = manager.Load<ITexture>("a.png");
        var second = manager.Load<ITexture>("a.png");
        Assert.Same(first, second); // 同路径复用

        string? changedPath = null;
        manager.ResourceChanged += p => changedPath = p;

        manager.Reload<ITexture>("a.png");

        Assert.Equal("a.png", changedPath);
        Assert.Equal(2, loader.LoadCount);

        var third = manager.Load<ITexture>("a.png");
        Assert.NotSame(first, third); // 热重载后是新实例
    }

    private sealed class CountingTextureLoader : IResourceLoader<ITexture>
    {
        public int LoadCount;
        public ITexture Load(string path)
        {
            LoadCount++;
            return new FakeTexture();
        }
    }

    private sealed class FakeTexture : ITexture
    {
        public int Width => 1;
        public int Height => 1;
    }
}
