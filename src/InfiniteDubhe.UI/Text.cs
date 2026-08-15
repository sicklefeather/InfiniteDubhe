using System.Numerics;
using InfiniteDubhe.Core;

namespace InfiniteDubhe.UI;

/// <summary>
/// 文本控件：用内置位图字体（<see cref="BitmapFont"/>）绘制字符串。
/// 尺寸按文本长度与缩放自动计算（<see cref="UIElement.Size"/> 被忽略）。
/// </summary>
public sealed class Text : UIElement
{
    private string _content;
    private float _scale;

    /// <summary>显示文本。</summary>
    public string Content
    {
        get => _content;
        set => _content = value ?? string.Empty;
    }

    /// <summary>缩放倍率（1 = 原始 5×7 像素字形）。</summary>
    public float Scale
    {
        get => _scale;
        set => _scale = MathF.Max(0f, value);
    }

    public Text(string content = "", float scale = 1f)
    {
        _content = content ?? string.Empty;
        _scale = MathF.Max(0f, scale);
        Color = Color.White;
    }

    /// <summary>无参构造（供序列化等按类型反射重建）。</summary>
    public Text() : this("", 1f) { }

    protected override Vector2 MeasureSelf()
        => _scale <= 0f
            ? Vector2.Zero
            : new Vector2(_content.Length * BitmapFont.CellWidth * _scale, BitmapFont.GlyphHeight * _scale);

    protected override void OnSubmit(ICollection<SpriteDrawCommand> commands, ITexture white, BitmapFont? font, int layer, float depth)
    {
        if (font is null || _scale <= 0f || _content.Length == 0) return;

        var cursor = ComputedPosition;
        float advance = BitmapFont.CellWidth * _scale;
        var scale = new Vector2(_scale, _scale);

        foreach (char c in _content)
        {
            var src = font.GetGlyph(c);
            if (!src.IsEmpty)
            {
                commands.Add(new SpriteDrawCommand
                {
                    Texture = font.Texture,
                    SourceRect = src,
                    Position = cursor,
                    Rotation = 0f,
                    Origin = Vector2.Zero,
                    Scale = scale,
                    Color = Color,
                    Effects = SpriteEffects.None,
                    Layer = layer,
                    LayerDepth = depth,
                });
            }
            cursor.X += advance;
        }
    }
}
