using InfiniteDubhe.Core;
using InfiniteDubhe.Platform;

namespace InfiniteDubhe.Platform.Windows;

/// <summary>Windows PAL 组合根：装配 Silk.NET 具体实现。</summary>
public sealed class WindowsPlatformBootstrap : IPlatformBootstrap
{
    public IWindow CreateWindow(GameConfig config) => new WindowsWindow(config);

    public IGraphicsContext CreateGraphicsContext(IWindow window)
        => new WindowsGraphicsContext((WindowsWindow)window);

    public IInputSource CreateInput(IWindow window)
        => new WindowsInputSource((WindowsWindow)window);

    public IFileSystem CreateFileSystem()
        => new WindowsFileSystem(Directory.GetCurrentDirectory());

    public IClock CreateClock() => new WindowsClock();
}
