using InfiniteDubhe.Core;

namespace InfiniteDubhe.Scene;

/// <summary>
/// 场景生命周期管理（切换/卸载/激活）。不做文件 I/O 与反序列化（由 Resources 层负责）。
/// </summary>
public sealed class SceneManager
{
    public Scene? Current { get; private set; }

    /// <summary>切换已就绪的场景（卸载旧、激活新）。MVP：直接替换引用。</summary>
    public void Load(Scene scene) => Current = scene ?? throw new ArgumentNullException(nameof(scene));

    internal void Update() => Current?.Update();
    internal void FixedUpdate() => Current?.FixedUpdate();
    internal void EndOfFrame() => Current?.EndOfFrame();
    internal void CollectRenderables(ICollection<IRenderable> renderables) => Current?.CollectRenderables(renderables);
}
