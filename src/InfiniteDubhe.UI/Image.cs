using System.Numerics;
using InfiniteDubhe.Core;

namespace InfiniteDubhe.UI;

/// <summary>
/// 图片控件：绘制一张纹理（拉伸到 <see cref="UIElement.Size"/>），无纹理时退化为纯色矩形。
/// </summary>
public class Image : UIElement
{
    /// <summary>要绘制的纹理（null 则按 <see cref="UIElement.Color"/> 绘制纯色）。</summary>
    public ITexture? Texture { get; set; }

    protected override Vector2 MeasureSelf()
    {
        if (Size != Vector2.Zero) return Size;
        if (Texture is not null) return new Vector2(Texture.Width, Texture.Height);
        return Vector2.Zero;
    }

    protected override void OnSubmit(ICollection<SpriteDrawCommand> commands, ITexture white, BitmapFont? font, int layer, float depth)
    {
        if (Texture is null)
        {
            SubmitSolid(commands, white, layer, depth);
            return;
        }

        commands.Add(new SpriteDrawCommand
        {
            Texture = Texture,
            SourceRect = default, // 整张纹理
            Position = ComputedPosition,
            Rotation = 0f,
            Origin = Vector2.Zero,
            Scale = ComputedSize / new Vector2(Texture.Width, Texture.Height), // 拉伸到 ComputedSize
            Color = Color,
            Effects = SpriteEffects.None,
            Layer = layer,
            LayerDepth = depth,
        });
    }
}
