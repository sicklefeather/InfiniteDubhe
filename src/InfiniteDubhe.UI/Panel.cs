using InfiniteDubhe.Core;

namespace InfiniteDubhe.UI;

/// <summary>面板控件：纯色矩形（背景/容器）。</summary>
public sealed class Panel : UIElement
{
    protected override void OnSubmit(ICollection<SpriteDrawCommand> commands, ITexture white, BitmapFont? font, int layer, float depth)
        => SubmitSolid(commands, white, layer, depth);
}
