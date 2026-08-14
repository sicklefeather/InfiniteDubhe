using System.Numerics;
using InfiniteDubhe.Core;

namespace InfiniteDubhe.UI;

/// <summary>布局方向：子元素自动排列方式。</summary>
public enum LayoutDirection
{
    /// <summary>不自动排列，子元素按各自锚点/位置摆放。</summary>
    None,

    /// <summary>垂直堆叠（自上而下）。</summary>
    Vertical,

    /// <summary>水平排列（自左向右）。</summary>
    Horizontal,
}

/// <summary>
/// UI 元素基类：锚点/枢轴/位置/尺寸布局模型 + 子元素树 + 交互事件。
/// UI 元素不是组件（由 <see cref="Canvas"/> 持有并驱动），渲染经 Canvas 的 <see cref="IRenderable"/> 提交。
/// </summary>
public abstract class UIElement
{
    private readonly List<UIElement> _children = new();

    // ---- 布局 ----
    /// <summary>锚点（父空间归一化 0..1）。元素枢轴对齐到父元素内该点。</summary>
    public Vector2 Anchor { get; set; }

    /// <summary>枢轴（自身空间归一化 0..1）。</summary>
    public Vector2 Pivot { get; set; }

    /// <summary>相对锚点的像素偏移。</summary>
    public Vector2 Position { get; set; }

    /// <summary>像素尺寸；文本等控件按内容自动计算（覆盖 <see cref="MeasureSelf"/>）。</summary>
    public Vector2 Size { get; set; }

    /// <summary>布局方向（容器用于自动排列子元素）。</summary>
    public LayoutDirection Layout { get; set; } = LayoutDirection.None;

    /// <summary>子元素间距（像素，仅 <see cref="Layout"/> 非 None 时有效）。</summary>
    public float Spacing { get; set; }

    /// <summary>内边距（像素，仅 <see cref="Layout"/> 非 None 时有效）。</summary>
    public Vector2 Padding { get; set; }

    /// <summary>着色（文字/背景/纹理 tint）。</summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>是否可见（不可见则不布局、不绘制、不参与命中测试）。</summary>
    public bool Visible { get; set; } = true;

    /// <summary>是否参与交互（命中测试/悬停/点击）。默认 false；<see cref="Button"/> 等设为 true。</summary>
    public bool Interactable { get; set; }

    // ---- 树 ----
    public UIElement? Parent { get; internal set; }
    public IReadOnlyList<UIElement> Children => _children;

    // ---- 计算后的布局（屏幕像素，由 Canvas 布局阶段填充） ----
    internal Vector2 ComputedPosition;
    internal Vector2 ComputedSize;

    // ---- 交互状态 ----
    public bool IsHovered { get; internal set; }
    public bool IsPressed { get; internal set; }

    // ---- 事件 ----
    /// <summary>点击（按下与释放都落在本元素上时触发）。</summary>
    public event Action<UIElement>? Clicked;

    /// <summary>指针进入。</summary>
    public event Action<UIElement>? PointerEnter;

    /// <summary>指针离开。</summary>
    public event Action<UIElement>? PointerExit;

    // ---- 子元素管理 ----
    public UIElement AddChild(UIElement child)
    {
        ArgumentNullException.ThrowIfNull(child);
        child.Parent?.RemoveChild(child);
        child.Parent = this;
        _children.Add(child);
        return child;
    }

    public T AddChild<T>(T child) where T : UIElement
    {
        AddChild((UIElement)child);
        return child;
    }

    public bool RemoveChild(UIElement child)
    {
        if (!_children.Remove(child)) return false;
        child.Parent = null;
        return true;
    }

    public void RemoveAllChildren()
    {
        foreach (var child in _children) child.Parent = null;
        _children.Clear();
    }

    // ---- 布局（由 Canvas 自顶向下驱动） ----
    /// <summary>返回本元素的内在尺寸（覆盖以支持自动尺寸）。</summary>
    protected virtual Vector2 MeasureSelf() => Size;

    internal void UpdateLayout(Vector2 parentTopLeft, Vector2 parentSize)
    {
        ComputedSize = MeasureSelf();
        ComputedPosition = parentTopLeft + parentSize * Anchor + Position - ComputedSize * Pivot;
        LayoutChildren();
    }

    private void LayoutChildren()
    {
        if (Layout == LayoutDirection.None)
        {
            foreach (var child in _children)
                if (child.Visible)
                    child.UpdateLayout(ComputedPosition, ComputedSize);
            return;
        }

        var content = ComputedPosition + Padding;
        var cursor = content;
        foreach (var child in _children)
        {
            if (!child.Visible) continue;
            child.ComputedSize = child.MeasureSelf();
            child.ComputedPosition = Layout == LayoutDirection.Vertical
                ? new Vector2(content.X, cursor.Y)
                : new Vector2(cursor.X, content.Y);
            if (Layout == LayoutDirection.Vertical) cursor.Y += child.ComputedSize.Y + Spacing;
            else cursor.X += child.ComputedSize.X + Spacing;
            child.LayoutChildren();
        }
    }

    // ---- 渲染（由 Canvas 自顶向下驱动） ----
    internal void Submit(ICollection<SpriteDrawCommand> commands, ITexture white, BitmapFont? font, int layer, ref int order)
    {
        if (!Visible) return;
        float depth = order++;
        OnSubmit(commands, white, font, layer, depth);
        foreach (var child in _children)
            child.Submit(commands, white, font, layer, ref order);
    }

    /// <summary>绘制自身（子类实现）。</summary>
    protected abstract void OnSubmit(ICollection<SpriteDrawCommand> commands, ITexture white, BitmapFont? font, int layer, float depth);

    /// <summary>用白色纹理按当前矩形与 <see cref="Color"/> 绘制实心矩形（面板/按钮背景等）。</summary>
    protected void SubmitSolid(ICollection<SpriteDrawCommand> commands, ITexture white, int layer, float depth)
    {
        commands.Add(new SpriteDrawCommand
        {
            Texture = white,
            SourceRect = new Rectangle(0, 0, 1, 1),
            Position = ComputedPosition,
            Rotation = 0f,
            Origin = Vector2.Zero,
            Scale = ComputedSize,
            Color = Color,
            Effects = SpriteEffects.None,
            Layer = layer,
            LayerDepth = depth,
        });
    }

    // ---- 交互（由 Canvas 驱动） ----
    internal bool Contains(Vector2 point)
        => point.X >= ComputedPosition.X && point.X < ComputedPosition.X + ComputedSize.X
        && point.Y >= ComputedPosition.Y && point.Y < ComputedPosition.Y + ComputedSize.Y;

    internal void InvokeClick() => Clicked?.Invoke(this);
    internal void InvokePointerEnter() { IsHovered = true; PointerEnter?.Invoke(this); }
    internal void InvokePointerExit() { IsHovered = false; PointerExit?.Invoke(this); }
}
