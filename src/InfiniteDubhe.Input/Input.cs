using System.Numerics;
using InfiniteDubhe.Core;
using InfiniteDubhe.Platform;

namespace InfiniteDubhe.Input;

/// <summary>
/// 输入静态门面：把底层 <see cref="IInputSource"/> 包装为全局可访问的只读查询。
/// 由引擎运行时（<c>Engine.GameHost</c>）在启动时注入数据源；用户业务代码无需直接持有平台句柄。
/// </summary>
public static class Input
{
    /// <summary>底层输入源。由引擎运行时注入（内部可见）。</summary>
    internal static IInputSource? Source { get; set; }

    /// <summary>键是否处于按下状态（持续）。</summary>
    public static bool IsKeyDown(Key key) => Source?.IsKeyDown(key) ?? false;

    /// <summary>键是否在本帧刚按下（边沿触发）。</summary>
    public static bool IsKeyPressed(Key key) => Source?.IsKeyPressed(key) ?? false;

    /// <summary>鼠标屏幕坐标（左上为原点，像素）。</summary>
    public static Vector2 MousePosition => Source?.MousePosition ?? Vector2.Zero;

    /// <summary>鼠标键是否处于按下状态（持续）。</summary>
    public static bool IsMouseButtonDown(MouseButton button) => Source?.IsMouseButtonDown(button) ?? false;

    /// <summary>鼠标键是否在本帧刚按下（边沿触发）。</summary>
    public static bool IsMouseButtonPressed(MouseButton button) => Source?.IsMouseButtonPressed(button) ?? false;

    /// <summary>本帧滚轮累计值（正值向上）。</summary>
    public static float MouseWheel => Source?.MouseWheel ?? 0f;
}
