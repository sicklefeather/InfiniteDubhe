namespace InfiniteDubhe.Core;

/// <summary>纹理句柄（渲染契约，跨层共享）。由 <c>Rendering.Texture2D</c> 实现。</summary>
public interface ITexture
{
    int Width { get; }
    int Height { get; }
}
