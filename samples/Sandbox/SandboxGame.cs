using InfiniteDubhe.Core;

namespace Sandbox;

/// <summary>M0 示例游戏：清屏 + 帧日志 + Esc/关闭按钮退出。</summary>
public sealed class SandboxGame : Game
{
    private int _frameCount;

    public SandboxGame(GameConfig config) : base(config) { }

    protected override void Initialize()
    {
        Log.Info("Sandbox initialized.");
    }

    protected override void LoadContent()
    {
        Log.Info("Content loaded (M0: no assets).");
    }

    protected override void Update(float dt)
    {
        _frameCount++;
        if (_frameCount % 60 == 0)
        {
            Log.Info($"Frame {_frameCount}, Time {Time.TotalTime:F2}s, dt {dt:F4}");
        }
    }

    protected override void UnloadContent()
    {
        Log.Info("Content unloaded.");
    }

    protected override void Shutdown()
    {
        Log.Info($"Sandbox shutdown after {_frameCount} frames.");
    }
}
