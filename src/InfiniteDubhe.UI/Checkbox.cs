using System.Numerics;
using InfiniteDubhe.Core;

namespace InfiniteDubhe.UI;

/// <summary>
/// 勾选框：方形开关，点击切换 <see cref="Checked"/> 并触发 <see cref="ValueChanged"/>。
/// 绘制边框 + 内底色；勾选时用内置字体绘制 “x” 标记。
/// </summary>
public sealed class Checkbox : UIElement
{
    /// <summary>是否勾选。</summary>
    public bool Checked { get; set; }

    /// <summary>内底色。</summary>
    public Color BackgroundColor { get; set; } = Color.FromRgb(40, 44, 62);

    /// <summary>边框色（悬停时改用 <see cref="HoverColor"/>）。</summary>
    public Color BorderColor { get; set; } = Color.FromRgb(160, 170, 200);

    /// <summary>悬停边框色。</summary>
    public Color HoverColor { get; set; } = Color.FromRgb(120, 200, 255);

    /// <summary>勾选标记色。</summary>
    public Color CheckColor { get; set; } = Color.FromRgb(120, 200, 255);

    /// <summary>勾选状态变化。</summary>
    public event Action<Checkbox>? ValueChanged;

    public Checkbox(bool @checked = false)
    {
        Checked = @checked;
        Interactable = true;
        Size = new Vector2(20f, 20f);
        Clicked += _ =>
        {
            Checked = !Checked;
            ValueChanged?.Invoke(this);
        };
    }

    /// <summary>无参构造（供序列化等按类型反射重建）。</summary>
    public Checkbox() : this(false) { }

    protected override void OnSubmit(ICollection<SpriteDrawCommand> commands, ITexture white, BitmapFont? font, int layer, float depth)
    {
        const float BorderThickness = 2f;

        // 边框（悬停高亮）→ 内底色。
        SubmitRect(commands, white, layer, depth,
            ComputedPosition, ComputedSize, IsHovered ? HoverColor : BorderColor);

        var innerPos = ComputedPosition + new Vector2(BorderThickness, BorderThickness);
        var innerSize = ComputedSize - new Vector2(BorderThickness * 2f, BorderThickness * 2f);
        SubmitRect(commands, white, layer, depth + 0.001f, innerPos, innerSize, BackgroundColor);

        // 勾选标记：用内置字体 “x” 居中。
        if (Checked && font is not null && innerSize.X > 0f && innerSize.Y > 0f)
        {
            var glyph = font.GetGlyph('x');
            float scale = MathF.Min(innerSize.X / BitmapFont.GlyphWidth, innerSize.Y / BitmapFont.GlyphHeight);
            var glyphSize = new Vector2(BitmapFont.GlyphWidth * scale, BitmapFont.GlyphHeight * scale);
            var pos = innerPos + (innerSize - glyphSize) * 0.5f;
            commands.Add(new SpriteDrawCommand
            {
                Texture = font.Texture,
                SourceRect = glyph,
                Position = pos,
                Rotation = 0f,
                Origin = Vector2.Zero,
                Scale = new Vector2(scale, scale),
                Color = CheckColor,
                Effects = SpriteEffects.None,
                Layer = layer,
                LayerDepth = depth + 0.002f,
            });
        }
    }
}
