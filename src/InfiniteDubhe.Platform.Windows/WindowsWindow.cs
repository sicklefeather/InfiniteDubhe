using Silk.NET.Maths;
using Silk.NET.Windowing;
using InfiniteDubhe.Core;
using InfiniteDubhe.Platform;

namespace InfiniteDubhe.Platform.Windows;

/// <summary>Silk.NET 窗口的 Windows 实现。</summary>
public sealed class WindowsWindow : IWindow, IDisposable
{
    private readonly Silk.NET.Windowing.IWindow _window;
    private readonly bool _vsync;

    internal Silk.NET.Windowing.IWindow Silk => _window;
    /// <summary>窗口原生句柄（HWND），供编辑器弹出模态对话框等 Windows 专属功能使用。</summary>
    public nint Hwnd => _window.Native!.DXHandle!.Value;
    internal int FramebufferWidth => _window.FramebufferSize.X;
    internal int FramebufferHeight => _window.FramebufferSize.Y;
    internal bool VSync => _vsync;

    public string Title { get => _window.Title; set => _window.Title = value; }
    public int Width => _window.Size.X;
    public int Height => _window.Size.Y;
    public bool IsClosing => _window.IsClosing;

    public event Action? Load;
    public event Action? Resized;
    public event Action? Closing;

    public WindowsWindow(GameConfig config)
    {
        _vsync = config.VSync;

        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(config.Width, config.Height);
        options.Title = config.Title;
        // 关键：禁用默认 OpenGL 上下文，改用我们自己的 D3D11。
        options.API = GraphicsAPI.None;

        _window = Window.Create(options);
        _window.FramebufferResize += OnFramebufferResize;
        _window.Closing += OnClosing;
    }

    public void Initialize()
    {
        _window.Initialize();
        Load?.Invoke();
    }

    public void ProcessEvents() => _window.DoEvents();

    public void Dispose() => _window.Dispose();

    private void OnFramebufferResize(Vector2D<int> size) => Resized?.Invoke();
    private void OnClosing() => Closing?.Invoke();
}
