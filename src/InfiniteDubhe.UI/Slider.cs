using System.Numerics;
using InfiniteDubhe.Core;
using InputFacade = InfiniteDubhe.Input.Input;

namespace InfiniteDubhe.UI;

/// <summary>
/// 滑杆：轨道 + 填充 + 手柄，按住拖动（或点击轨道）改变 <see cref="Value"/>（0..1）。
/// 拖拽期间持续触发 <see cref="ValueChanged"/>。
/// </summary>
public sealed class Slider : UIElement
{
    /// <summary>当前值（0..1）。</summary>
    [Range(0f, 1f)]
    public float Value { get; set; }

    /// <summary>轨道色。</summary>
    public Color TrackColor { get; set; } = Color.FromRgb(40, 44, 62);

    /// <summary>已填充部分颜色。</summary>
    public Color FillColor { get; set; } = Color.FromRgb(120, 200, 255);

    /// <summary>手柄颜色。</summary>
    public Color KnobColor { get; set; } = Color.FromRgb(210, 220, 240);

    /// <summary>值变化（拖拽期间持续触发）。</summary>
    public event Action<Slider>? ValueChanged;

    public Slider(float value = 0f)
    {
        Value = value;
        Interactable = true;
        Size = new Vector2(200f, 16f);
    }

    /// <summary>无参构造（供序列化等按类型反射重建）。</summary>
    public Slider() : this(0f) { }

    protected override void OnUpdate()
    {
        if (!IsPressed) return;
        float t = (InputFacade.MousePosition.X - ComputedPosition.X) / MathF.Max(1e-3f, ComputedSize.X);
        SetValue(Math.Clamp(t, 0f, 1f));
    }

    private void SetValue(float value)
    {
        if (MathF.Abs(value - Value) < 1e-6f) return;
        Value = value;
        ValueChanged?.Invoke(this);
    }

    protected override void OnSubmit(ICollection<SpriteDrawCommand> commands, ITexture white, BitmapFont? font, int layer, float depth)
    {
        // 轨道。
        SubmitSolid(commands, white, layer, depth);

        float t = Math.Clamp(Value, 0f, 1f);
        float knob = MathF.Max(ComputedSize.Y, 4f);
        float knobCenterX = ComputedPosition.X + t * ComputedSize.X;

        // 填充。
        if (t > 0f)
            SubmitRect(commands, white, layer, depth + 0.001f,
                ComputedPosition, new Vector2(knobCenterX - ComputedPosition.X, ComputedSize.Y), FillColor);

        // 手柄（正方形，垂直居中）。
        SubmitRect(commands, white, layer, depth + 0.002f,
            new Vector2(knobCenterX - knob * 0.5f, ComputedPosition.Y + (ComputedSize.Y - knob) * 0.5f),
            new Vector2(knob, knob), KnobColor);
    }
}
