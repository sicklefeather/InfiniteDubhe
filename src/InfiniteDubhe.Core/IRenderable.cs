namespace InfiniteDubhe.Core;

/// <summary>
/// 可渲染对象（渲染契约）。由 <c>Scene.SpriteRenderer</c> 等实现；
/// 渲染器经此收集绘制指令，从而避免 <c>Scene</c> 反向依赖 <c>Rendering</c>。
/// </summary>
public interface IRenderable
{
    /// <summary>收集本对象的绘制指令。集合由引擎池化。</summary>
    void Submit(ICollection<SpriteDrawCommand> commands);
}
