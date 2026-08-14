namespace InfiniteDubhe.Platform;

/// <summary>平台窗口抽象。接口不包含任何 Windows 专属类型。</summary>
public interface IWindow
{
    string Title { get; set; }
    int Width { get; }
    int Height { get; }
    bool IsClosing { get; }

    /// <summary>窗口初始化完成（可创建图形上下文/输入）。</summary>
    event Action? Load;

    event Action? Resized;
    event Action? Closing;

    /// <summary>初始化窗口（触发 <see cref="Load"/>）。</summary>
    void Initialize();

    /// <summary>非阻塞泵取窗口/输入事件（每帧调用）。</summary>
    void ProcessEvents();
}
