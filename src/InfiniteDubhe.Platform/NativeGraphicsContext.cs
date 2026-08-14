namespace InfiniteDubhe.Platform;

/// <summary>
/// 后端图形句柄（不透明）。Platform 实现填充具体后端对象（如 D3D11 设备/上下文/交换链），
/// 由 Rendering 层解释（Rendering 是唯一允许接触具体后端的层）。字段用 <see cref="object"/>
/// 承载，避免 Platform 依赖具体后端类型。
/// </summary>
public sealed class NativeGraphicsContext
{
    public object Device { get; }
    public object Context { get; }
    public object SwapChain { get; }

    public NativeGraphicsContext(object device, object context, object swapChain)
    {
        Device = device;
        Context = context;
        SwapChain = swapChain;
    }
}
