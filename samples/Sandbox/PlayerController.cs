using System.Numerics;
using InfiniteDubhe.Core;
using InfiniteDubhe.Input;
using InfiniteDubhe.Scene;

namespace Sandbox;

/// <summary>
/// 玩家控制器组件：演示组件生命周期（Awake/Start/Update）与输入驱动精灵。
/// 键盘（WASD 移动、Q/E 旋转、R/F 缩放）+ 鼠标（左键拖拽跟随）。
/// </summary>
public sealed class PlayerController : Component
{
    private const float Speed = 320f;        // 像素/秒
    private const float RotateSpeed = 150f;  // 度/秒

    protected override void Awake() => Log.Info($"PlayerController.Awake on '{GameObject.Name}'");

    protected override void Start() => Log.Info($"PlayerController.Start at {Transform.Position}");

    protected override void Update()
    {
        var dt = Time.ScaledDeltaTime;
        var t = Transform;

        // 移动（归一化，避免斜向加速）。
        var move = Vector2.Zero;
        if (InputActions.IsDown("MoveLeft")) move.X -= 1f;
        if (InputActions.IsDown("MoveRight")) move.X += 1f;
        if (InputActions.IsDown("MoveUp")) move.Y -= 1f;
        if (InputActions.IsDown("MoveDown")) move.Y += 1f;
        if (move != Vector2.Zero) move = Vector2.Normalize(move);
        t.Position += move * Speed * dt;

        // 旋转。
        if (InputActions.IsDown("RotateLeft")) t.RotationDeg += RotateSpeed * dt;
        if (InputActions.IsDown("RotateRight")) t.RotationDeg -= RotateSpeed * dt;

        // 缩放（指数逼近，避免负值）。
        if (InputActions.IsDown("ScaleUp")) t.Scale *= 1f + dt;
        if (InputActions.IsDown("ScaleDown")) t.Scale *= 1f - dt;

        // 鼠标左键拖拽：精灵跟随光标（世界 = 屏幕像素，1:1）。
        if (Input.IsMouseButtonDown(MouseButton.Left))
            t.Position = Vector2.Lerp(t.Position, Input.MousePosition, dt * 8f);
    }

    protected override void OnDestroy() => Log.Info($"PlayerController.OnDestroy on '{GameObject.Name}'");
}
