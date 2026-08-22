using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using InfiniteDubhe.Core;
using InfiniteDubhe.Platform;

namespace InfiniteDubhe.Rendering;

/// <summary>
/// 渲染器入口。持有后端设备句柄，装配 <see cref="SpriteBatch"/> 与默认相机，
/// 负责清屏、提交场景可渲染对象、呈现。Rendering 是唯一接触具体后端（D3D11）的层。
/// </summary>
public sealed class Renderer : IDisposable
{
    private readonly IGraphicsContext _graphics;
    private readonly int _width;
    private readonly int _height;

    private ComPtr<ID3D11Device> _device;
    private ComPtr<ID3D11DeviceContext> _context;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _debugTexture;
    private bool _initialized;

    public Camera Camera { get; }
    public Color ClearColor { get; set; } = Color.CornflowerBlue;

    public Renderer(IGraphicsContext graphics, int width, int height)
    {
        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        _width = width;
        _height = height;
        // 默认相机：世界 (0,0) 位于屏幕左上角（像素与世界单位 1:1）。
        Camera = new Camera(width, height) { Position = new Vector2(width / 2f, height / 2f) };
    }

    /// <summary>在窗口初始化后调用：获取后端句柄并创建 GPU 资源。</summary>
    public void Initialize()
    {
        var native = _graphics.Native;
        _device = (ComPtr<ID3D11Device>)native.Device;
        _context = (ComPtr<ID3D11DeviceContext>)native.Context;
        _spriteBatch = new SpriteBatch(_device, _context);
        _debugTexture = CreateTexture(1, 1, new byte[] { 255, 255, 255, 255 });
        _initialized = true;
    }

    /// <summary>从 RGBA 字节创建 GPU 纹理。</summary>
    public Texture2D CreateTexture(int width, int height, ReadOnlySpan<byte> rgba)
        => new(_device, width, height, rgba);

    public void Clear() => Clear(ClearColor);

    public void Clear(Color color) => _graphics.Clear(color);

    /// <summary>提交场景可渲染对象（收集指令 → 批处理 → 绘制）。</summary>
    /// <summary>创建离屏渲染目标（编辑器视口等用）。</summary>
    public RenderTarget2D CreateRenderTarget(int width, int height)
        => new(_device, width, height);

    /// <summary>提交场景可渲染对象到后备缓冲（收集指令 → 批处理 → 绘制）。</summary>
    public void Draw(IReadOnlyList<IRenderable> renderables) => DrawCore(renderables);

    /// <summary>提交场景可渲染对象到离屏渲染目标（视口用）。</summary>
    public void Draw(IReadOnlyList<IRenderable> renderables, RenderTarget2D target)
    {
        ArgumentNullException.ThrowIfNull(target);
        BindRenderTarget(target);
        ClearRenderTarget(target, ClearColor); // 离屏目标须清屏，否则旧帧内容残留（移动时拖影）
        DrawCore(renderables);
    }

    private void DrawCore(IReadOnlyList<IRenderable> renderables)
    {
        if (!_initialized || _spriteBatch is null) return;
        _spriteBatch.Begin(Camera);
        foreach (var r in renderables) r.Submit(_spriteBatch);
        _spriteBatch.End();

        // 调试绘制：独立 pass，绘制在精灵之上。
        if (_debugTexture is not null)
        {
            _spriteBatch.Begin(Camera);
            Debug.Flush(_spriteBatch, _debugTexture);
            _spriteBatch.End();
        }
        Debug.Clear();
    }

    private void BindRenderTarget(RenderTarget2D target)
    {
        var rtv = target.Rtv;
        _context.OMSetRenderTargets(1, ref rtv, ref Unsafe.NullRef<ID3D11DepthStencilView>());
        var viewport = new Viewport(0, 0, target.Width, target.Height, 0, 1);
        _context.RSSetViewports(1, in viewport);
    }

    private unsafe void ClearRenderTarget(RenderTarget2D target, Color color)
    {
        var rtv = target.Rtv;
        Span<float> rgba = stackalloc float[4] { color.R, color.G, color.B, color.A };
        _context.ClearRenderTargetView(rtv, ref rgba[0]);
    }

    public void Present() => _graphics.SwapBuffers();

    public void Dispose() => _spriteBatch?.Dispose();
}
