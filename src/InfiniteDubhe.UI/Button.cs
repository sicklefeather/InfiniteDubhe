using InfiniteDubhe.Core;

namespace InfiniteDubhe.UI;

/// <summary>
/// 按钮控件：背景色（随悬停/按压变化）+ 居中文本标签，响应点击（<see cref="UIElement.Clicked"/>）。
/// </summary>
public sealed class Button : UIElement
{
    private readonly Text _label;

    /// <summary>常态背景色。</summary>
    public Color BackgroundColor { get; set; } = Color.FromRgb(70, 70, 96);

    /// <summary>悬停背景色。</summary>
    public Color HoverColor { get; set; } = Color.FromRgb(96, 96, 128);

    /// <summary>按压背景色。</summary>
    public Color PressedColor { get; set; } = Color.FromRgb(48, 48, 72);

    /// <summary>按钮文本。</summary>
    public string Label
    {
        get => _label.Content;
        set => _label.Content = value;
    }

    /// <summary>文本颜色。</summary>
    public Color TextColor
    {
        get => _label.Color;
        set => _label.Color = value;
    }

    /// <summary>文本缩放倍率。</summary>
    public float FontScale
    {
        get => _label.Scale;
        set => _label.Scale = value;
    }

    public Button(string label = "", float fontScale = 2f)
    {
        Interactable = true;
        _label = new Text(label, fontScale) { Anchor = new System.Numerics.Vector2(0.5f, 0.5f), Pivot = new System.Numerics.Vector2(0.5f, 0.5f) };
        AddChild(_label);
    }

    protected override void OnSubmit(ICollection<SpriteDrawCommand> commands, ITexture white, BitmapFont? font, int layer, float depth)
    {
        var saved = Color;
        Color = IsPressed ? PressedColor : IsHovered ? HoverColor : BackgroundColor;
        SubmitSolid(commands, white, layer, depth);
        Color = saved;
    }
}
