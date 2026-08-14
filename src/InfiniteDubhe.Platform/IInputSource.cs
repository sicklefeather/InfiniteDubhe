using System.Numerics;
using InfiniteDubhe.Core;

namespace InfiniteDubhe.Platform;

/// <summary>输入源抽象。由 PAL 把平台键码映射到 <see cref="Key"/>/<see cref="MouseButton"/>。</summary>
public interface IInputSource
{
    /// <summary>键是否处于按下状态（持续）。</summary>
    bool IsKeyDown(Key key);

    /// <summary>键是否在本帧刚按下（边沿触发）。</summary>
    bool IsKeyPressed(Key key);

    Vector2 MousePosition { get; }

    bool IsMouseButtonDown(MouseButton button);

    /// <summary>每帧调用，推进瞬态（边沿触发）状态。</summary>
    void Update();
}
