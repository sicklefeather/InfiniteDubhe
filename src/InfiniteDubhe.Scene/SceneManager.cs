using InfiniteDubhe.Core;

namespace InfiniteDubhe.Scene;

/// <summary>
/// 场景生命周期管理（切换/卸载/激活）。支持一个**常驻全局场景**（放全局对象）与一个**当前关卡场景**，
/// 步进/渲染时两者都驱动（全局先、关卡后）。不做文件 I/O 与反序列化（由 Resources 层负责）。
/// </summary>
public sealed class SceneManager
{
    /// <summary>常驻全局场景（跨关卡存活的对象，如游戏管理器/音频）。</summary>
    public Scene? Global { get; private set; }

    /// <summary>当前关卡场景。</summary>
    public Scene? Current { get; private set; }

    /// <summary>设置常驻全局场景（传 null 清除）。</summary>
    public void SetGlobal(Scene? scene) => Global = scene;

    /// <summary>切换当前关卡场景（传 null 清除）。</summary>
    public void Load(Scene? scene) => Current = scene;

    internal void Update()
    {
        Global?.Update();
        Current?.Update();
    }

    internal void FixedUpdate()
    {
        Global?.FixedUpdate();
        Current?.FixedUpdate();
    }

    internal void EndOfFrame()
    {
        Global?.EndOfFrame();
        Current?.EndOfFrame();
    }

    internal void CollectRenderables(ICollection<IRenderable> renderables)
    {
        Global?.CollectRenderables(renderables);
        Current?.CollectRenderables(renderables);
    }
}
