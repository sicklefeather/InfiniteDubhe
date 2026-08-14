using InfiniteDubhe.Core;

namespace InfiniteDubhe.Platform;

/// <summary>图形上下文的最小跨平台面。底层设备/交换链创建在 Platform 实现，Rendering 层负责真正绘制。</summary>
public interface IGraphicsContext
{
    /// <summary>清空当前渲染目标。</summary>
    void Clear(Color color);

    /// <summary>提交渲染帧（交换缓冲）。</summary>
    void SwapBuffers();

    /// <summary>绑定上下文（多线程渲染时用；单线程 MVP 为空操作）。</summary>
    void MakeCurrent();

    /// <summary>后端图形句柄（供 Rendering 层进行纹理/缓冲/着色器操作）。</summary>
    NativeGraphicsContext Native { get; }
}
