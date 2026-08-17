using System.Numerics;
using InfiniteDubhe.Core;
using InfiniteDubhe.Rendering;
using InfiniteDubhe.Scene;
using InputFacade = InfiniteDubhe.Input.Input;

namespace InfiniteDubhe.UI;

/// <summary>
/// UI 画布：挂在 GameObject 上的 <see cref="Component"/>，实现 <see cref="IRenderable"/>。
/// 持有 UI 元素树，负责布局、渲染（高层 Layer 置顶）与鼠标交互（悬停/点击）。
/// 创建后需调用 <see cref="Initialize"/> 传入 <see cref="Renderer"/> 以生成白色纹理与内置字体图集。
/// </summary>
public sealed class Canvas : Component, IRenderable
{
    private readonly List<UIElement> _roots = new();

    private Renderer? _renderer;
    private ITexture? _white;
    private BitmapFont? _font;
    private int _drawOrder;

    private UIElement? _hovered;
    private UIElement? _pressed;
    private bool _wasDown;

    /// <summary>UI 渲染层（世界精灵默认 Layer 0，UI 置顶）。</summary>
    public int SortingLayer { get; set; } = 1000;

    /// <summary>内置字体图集（文本控件使用）。</summary>
    public BitmapFont? Font => _font;

    /// <summary>根元素。</summary>
    public IReadOnlyList<UIElement> Roots => _roots;

    private Vector2 ScreenSize => _renderer is null
        ? Vector2.Zero
        : new Vector2(_renderer.Camera.ViewportWidth, _renderer.Camera.ViewportHeight);

    /// <summary>用渲染器生成白色纹理与字体图集（须在创建后、渲染前调用一次）。</summary>
    public void Initialize(Renderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        _renderer = renderer;
        _white = renderer.CreateTexture(1, 1, new byte[] { 255, 255, 255, 255 });
        _font = BitmapFont.Build(renderer);
    }

    /// <summary>添加根元素。</summary>
    public T Add<T>(T element) where T : UIElement => (T)AddRoot(element);

    /// <summary>以 <see cref="UIElement"/> 基类类型添加根元素（供序列化等按类型反射重建时调用）。</summary>
    public UIElement AddRoot(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _roots.Add(element);
        return element;
    }

    /// <summary>在指定位置插入根元素（供编辑器拖拽排序 / 撤销恢复位置）。越界则夹到末尾。</summary>
    public void InsertRoot(int index, UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _roots.Insert(Math.Clamp(index, 0, _roots.Count), element);
    }

    /// <summary>移除根元素。</summary>
    public bool Remove(UIElement element) => _roots.Remove(element);

    protected override void Update()
    {
        if (_renderer is null) return;
        UpdateLayout();
        ProcessInput();
        foreach (var root in _roots)
            root.UpdateTree();
    }

    public void Submit(ICollection<SpriteDrawCommand> commands)
    {
        if (_renderer is null || _white is null) return;
        UpdateLayout();
        _drawOrder = 0;
        foreach (var root in _roots)
            root.Submit(commands, _white, _font, SortingLayer, ref _drawOrder);
    }

    private void UpdateLayout()
    {
        var size = ScreenSize;
        // UI 以「相机视野左上角」为原点布局（屏幕空间）。相机中心 = 视野中心时原点即世界 (0,0)，
        // 编辑器视口相机中心与视口尺寸可能不一致，故按相机实际视野换算，保证 UI 始终贴屏幕左上角。
        var cam = _renderer!.Camera;
        var topLeft = cam.Position - new Vector2(cam.ViewportWidth * 0.5f, cam.ViewportHeight * 0.5f);
        foreach (var root in _roots)
            if (root.Visible)
                root.UpdateLayout(topLeft, size);
    }

    private void ProcessInput()
    {
        var hovered = HitTest(InputFacade.MousePosition, _roots);

        if (!ReferenceEquals(hovered, _hovered))
        {
            _hovered?.InvokePointerExit();
            _hovered = hovered;
            _hovered?.InvokePointerEnter();
        }

        bool down = InputFacade.IsMouseButtonDown(MouseButton.Left);

        if (InputFacade.IsMouseButtonPressed(MouseButton.Left))
        {
            _pressed = hovered;
            if (_pressed is not null) _pressed.IsPressed = true;
        }

        if (!down && _wasDown)
        {
            // 本帧释放：若与按下元素相同则触发点击。
            if (_pressed is not null)
            {
                _pressed.IsPressed = false;
                if (ReferenceEquals(_pressed, hovered))
                    _pressed.InvokeClick();
                _pressed = null;
            }
        }

        _wasDown = down;
    }

    /// <summary>返回鼠标下最顶层的可交互元素（无则 null）。</summary>
    private static UIElement? HitTest(Vector2 point, IReadOnlyList<UIElement> elements)
    {
        // 逆序遍历：后绘制/后添加者在上层；子元素在父元素之上，故先递归子元素。
        for (int i = elements.Count - 1; i >= 0; i--)
        {
            var e = elements[i];
            if (!e.Visible) continue;

            var childHit = HitTest(point, e.Children);
            if (childHit is not null) return childHit;
            if (e.Interactable && e.Contains(point)) return e;
        }
        return null;
    }
}
