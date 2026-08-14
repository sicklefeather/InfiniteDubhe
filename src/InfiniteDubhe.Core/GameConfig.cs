namespace InfiniteDubhe.Core;

/// <summary>游戏配置：窗口与主循环基础参数。</summary>
public sealed class GameConfig
{
    public string Title { get; set; } = "InfiniteDubhe";
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public bool VSync { get; set; } = true;

    /// <summary>固定步长（秒）。</summary>
    public float FixedTimestep { get; set; } = 1f / 60f;

    /// <summary>帧间隔上限（秒），防止窗口拖动导致 dt 巨大。</summary>
    public float MaxDeltaTime { get; set; } = 0.25f;
}
