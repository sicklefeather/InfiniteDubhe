using System.Numerics;
using InfiniteDubhe.Core;

namespace InfiniteDubhe.Scene;

/// <summary>
/// 精灵渲染组件：描述“画什么、画在哪”。只持有 <see cref="ITexture"/> 句柄，
/// 不依赖具体 GPU 实现（<c>Rendering.Texture2D</c>），实现 <see cref="IRenderable"/> 供渲染器收集。
/// </summary>
public sealed class SpriteRenderer : Component, IRenderable
{
    public ITexture? Texture { get; set; }
    public Color Color { get; set; } = Color.White;
    public Vector2 Origin { get; set; }
    public SpriteEffects Effects { get; set; } = SpriteEffects.None;
    public int Layer { get; set; }
    public float LayerDepth { get; set; }

    private Rectangle _sourceRect;
    /// <summary>源矩形；空表示整张纹理。</summary>
    public Rectangle SourceRect
    {
        get => _sourceRect;
        set => _sourceRect = value;
    }

    public void Submit(ICollection<SpriteDrawCommand> commands)
    {
        if (Texture is null) return;

        var src = _sourceRect.IsEmpty
            ? new Rectangle(0, 0, Texture.Width, Texture.Height)
            : _sourceRect;

        commands.Add(new SpriteDrawCommand
        {
            Texture = Texture,
            SourceRect = src,
            Position = Transform.WorldPosition,
            Rotation = Transform.RotationDeg * MathF.PI / 180f, // 度→弧度统一在此换算
            Origin = Origin,
            Scale = Transform.Scale,
            Color = Color,
            Effects = Effects,
            Layer = Layer,
            LayerDepth = LayerDepth,
        });
    }
}
