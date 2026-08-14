using InfiniteDubhe.Core;
using InfiniteDubhe.Platform;

namespace InfiniteDubhe.Rendering;

/// <summary>渲染器入口。M0 仅提供清屏 + 呈现；批处理/Sprite 随 M1 扩展。</summary>
public sealed class Renderer
{
    private readonly IGraphicsContext _graphics;

    public Renderer(IGraphicsContext graphics)
    {
        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
    }

    public Color ClearColor { get; set; } = Color.CornflowerBlue;

    /// <summary>用默认清屏色清空渲染目标。</summary>
    public void Clear() => Clear(ClearColor);

    /// <summary>用指定颜色清空渲染目标。</summary>
    public void Clear(Color color) => _graphics.Clear(color);

    /// <summary>提交帧（交换缓冲）。</summary>
    public void Present() => _graphics.SwapBuffers();
}
