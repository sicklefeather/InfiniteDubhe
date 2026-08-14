using InfiniteDubhe.Core;

namespace InfiniteDubhe.Platform;

/// <summary>PAL 组合根：负责装配具体平台实现。Host 只依赖这些抽象。</summary>
public interface IPlatformBootstrap
{
    IWindow CreateWindow(GameConfig config);

    IGraphicsContext CreateGraphicsContext(IWindow window);

    IInputSource CreateInput(IWindow window);

    IFileSystem CreateFileSystem();

    IClock CreateClock();
}
