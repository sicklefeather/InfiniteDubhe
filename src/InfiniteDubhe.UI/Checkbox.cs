using System.Numerics;
using InfiniteDubhe.Core;

namespace InfiniteDubhe.UI;

/// <summary>
/// 勾选框：方形开关，点击切换 <see cref="Checked"/> 并触发 <see cref="ValueChanged"/>。
/// 绘制边框 + 内底色；勾选时用两段线段绘制 “✓” 标记（沿方向延伸半线宽，转角无缝）。
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

        // 勾选标记：两段线段拼成 “✓”（线宽随框缩放）。
        if (Checked && innerSize.X > 2f && innerSize.Y > 2f)
        {
            float thickness = MathF.Max(2f, MathF.Min(innerSize.X, innerSize.Y) * 0.14f);
            Vector2 P(float ux, float uy) => innerPos + new Vector2(innerSize.X * ux, innerSize.Y * uy);
            var corner = P(0.46f, 0.78f); // ✓ 的转角
            SubmitLine(commands, white, layer, depth + 0.002f, P(0.16f, 0.50f), corner, CheckColor, thickness);
            SubmitLine(commands, white, layer, depth + 0.002f, corner, P(0.88f, 0.20f), CheckColor, thickness);
        }
    }

    /// <summary>用白色纹理绘制一条线段（同 <see cref="InfiniteDubhe.Core.Debug"/> 的线段画法：中点定位 + 旋转 + 缩放）。</summary>
    private static void SubmitLine(ICollection<SpriteDrawCommand> commands, ITexture white, int layer, float depth,
        Vector2 a, Vector2 b, Color color, float thickness)
    {
        var delta = b - a;
        float length = delta.Length();
        if (length < 0.01f) return;

        // 沿方向延伸半线宽，转角处两段线无缝衔接。
        var dir = delta / length;
        a -= dir * (thickness * 0.5f);
        b += dir * (thickness * 0.5f);
        delta = b - a;
        length = delta.Length();

        commands.Add(new SpriteDrawCommand
        {
            Texture = white,
            SourceRect = new Rectangle(0, 0, 1, 1),
            Position = (a + b) * 0.5f,
            Rotation = MathF.Atan2(delta.Y, delta.X),
            Origin = new Vector2(length * 0.5f, thickness * 0.5f),
            Scale = new Vector2(length, thickness),
            Color = color,
            Effects = SpriteEffects.None,
            Layer = layer,
            LayerDepth = depth,
        });
    }
}
