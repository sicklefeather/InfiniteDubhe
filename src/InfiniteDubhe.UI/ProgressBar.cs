using System.Numerics;
using InfiniteDubhe.Core;

namespace InfiniteDubhe.UI;

/// <summary>
/// 进度条：背景 + 按 <see cref="Progress"/>（0..1）比例的填充条。非交互。
/// </summary>
public sealed class ProgressBar : UIElement
{
    /// <summary>进度（0..1）。</summary>
    [Range(0f, 1f)]
    public float Progress { get; set; }

    /// <summary>背景色。</summary>
    public Color BackgroundColor { get; set; } = Color.FromRgb(40, 44, 62);

    /// <summary>填充色。</summary>
    public Color FillColor { get; set; } = Color.FromRgb(120, 200, 255);

    public ProgressBar(float progress = 0f)
    {
        Progress = progress;
        Size = new Vector2(200f, 16f);
    }

    /// <summary>无参构造（供序列化等按类型反射重建）。</summary>
    public ProgressBar() : this(0f) { }

    protected override void OnSubmit(ICollection<SpriteDrawCommand> commands, ITexture white, BitmapFont? font, int layer, float depth)
    {
        // 背景。
        SubmitSolid(commands, white, layer, depth);

        // 填充。
        float t = Math.Clamp(Progress, 0f, 1f);
        if (t <= 0f) return;
        SubmitRect(commands, white, layer, depth + 0.001f,
            ComputedPosition, new Vector2(ComputedSize.X * t, ComputedSize.Y), FillColor);
    }
}
