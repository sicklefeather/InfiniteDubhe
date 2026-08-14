using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using InfiniteDubhe.Core;
using InfiniteDubhe.Platform;

namespace InfiniteDubhe.Platform.Windows;

/// <summary>
/// D3D11 图形上下文：创建设备/交换链，提供清屏与呈现。
/// Rendering 层是唯一接触具体后端的层，此处封装最小跨平台面。
/// </summary>
public sealed unsafe class WindowsGraphicsContext : IGraphicsContext, IDisposable
{
    private readonly WindowsWindow _window;

    private D3D11? _d3d11;
    private DXGI? _dxgi;
    private ComPtr<IDXGIFactory2> _factory;
    private ComPtr<IDXGISwapChain1> _swapchain;
    private ComPtr<ID3D11Device> _device;
    private ComPtr<ID3D11DeviceContext> _context;
    private bool _initialized;

    public WindowsGraphicsContext(WindowsWindow window)
    {
        _window = window;
        window.Load += Initialize;
    }

    public void Clear(Color color)
    {
        if (!_initialized) return;

        using var backBuffer = _swapchain.GetBuffer<ID3D11Texture2D>(0);
        ComPtr<ID3D11RenderTargetView> rtv = default;
        SilkMarshal.ThrowHResult(_device.CreateRenderTargetView(backBuffer, null, ref rtv));

        var c = new[] { color.R, color.G, color.B, color.A };
        _context.ClearRenderTargetView(rtv, ref c[0]);

        var viewport = new Viewport(0, 0, _window.FramebufferWidth, _window.FramebufferHeight, 0, 1);
        _context.RSSetViewports(1, in viewport);
        _context.OMSetRenderTargets(1, ref rtv, ref Unsafe.NullRef<ID3D11DepthStencilView>());

        rtv.Dispose();
    }

    public void SwapBuffers()
    {
        if (!_initialized) return;
        _swapchain.Present(_window.VSync ? 1u : 0u, 0);
    }

    public void MakeCurrent() { /* 单线程 MVP：无需绑定 */ }

    public NativeGraphicsContext Native => new(_device, _context, _swapchain);

    public void Dispose()
    {
        _factory.Dispose();
        _swapchain.Dispose();
        _device.Dispose();
        _context.Dispose();
        _d3d11?.Dispose();
        _dxgi?.Dispose();
    }

    private void Initialize()
    {
        var dxgi = DXGI.GetApi(_window.Silk, false);
        var d3d11 = D3D11.GetApi(_window.Silk, false);
        _dxgi = dxgi;
        _d3d11 = d3d11;

        SilkMarshal.ThrowHResult(d3d11.CreateDevice(
            default(ComPtr<IDXGIAdapter>),
            D3DDriverType.Hardware,
            Software: default,
            (uint)CreateDeviceFlag.BgraSupport,
            null,
            0,
            D3D11.SdkVersion,
            ref _device,
            null,
            ref _context));

        var desc = new SwapChainDesc1
        {
            BufferCount = 2,
            Format = Format.FormatB8G8R8A8Unorm,
            BufferUsage = DXGI.UsageRenderTargetOutput,
            SwapEffect = SwapEffect.FlipDiscard,
            SampleDesc = new SampleDesc(1, 0)
        };

        _factory = dxgi.CreateDXGIFactory<IDXGIFactory2>();

        SilkMarshal.ThrowHResult(_factory.CreateSwapChainForHwnd(
            _device,
            _window.Hwnd,
            in desc,
            null,
            ref Unsafe.NullRef<IDXGIOutput>(),
            ref _swapchain));

        _window.Resized += ResizeSwapchain;
        _initialized = true;
    }

    private void ResizeSwapchain()
    {
        if (!_initialized) return;
        SilkMarshal.ThrowHResult(_swapchain.ResizeBuffers(
            0,
            (uint)_window.FramebufferWidth,
            (uint)_window.FramebufferHeight,
            Format.FormatB8G8R8A8Unorm,
            0));
    }
}
