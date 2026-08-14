using System.Numerics;
using InfiniteDubhe.Core;
using InfiniteDubhe.Input;
using InfiniteDubhe.Resources;
using InfiniteDubhe.Scene;

namespace Sandbox;

/// <summary>
/// M1 示例游戏：加载纹理 → 创建场景/实体/组件 → 用键盘/鼠标驱动精灵移动/旋转/缩放。
/// 覆盖 MVP 验收（需求 §11）的第 2、3、4 条。
/// </summary>
public sealed class SandboxGame : Game
{
    public SandboxGame(GameConfig config) : base(config) { }

    protected override void Initialize()
    {
        Log.Info("Sandbox (M1) initialized.");

        // 动作映射：命名动作 ↔ 键位（便于改键）。
        InputActions.Bind("MoveLeft", Key.A, Key.Left);
        InputActions.Bind("MoveRight", Key.D, Key.Right);
        InputActions.Bind("MoveUp", Key.W, Key.Up);
        InputActions.Bind("MoveDown", Key.S, Key.Down);
        InputActions.Bind("RotateLeft", Key.Q);
        InputActions.Bind("RotateRight", Key.E);
        InputActions.Bind("ScaleUp", Key.R);
        InputActions.Bind("ScaleDown", Key.F);
    }

    protected override void LoadContent()
    {
        var sceneManager = GetService<SceneManager>()!;
        var resources = GetService<ResourceManager>()!;

        // 场景 + 实体 + 组件。
        var scene = new Scene("Main");
        sceneManager.Load(scene);

        var player = scene.CreateObject("Player");
        player.Transform.Position = new Vector2(Config.Width / 2f, Config.Height / 2f);

        var renderer = player.AddComponent<SpriteRenderer>();
        player.AddComponent<PlayerController>();

        // 加载纹理（StbImageSharp 解码 → GPU 上传），并设为居中旋转原点。
        var texturePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Content", "player.png");
        var texture = resources.Load<ITexture>(texturePath);
        renderer.Texture = texture;
        renderer.Origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

        Log.Info($"Loaded texture '{texturePath}' ({texture.Width}x{texture.Height}).");
    }

    protected override void UnloadContent() => Log.Info("Sandbox content unloaded.");

    protected override void Shutdown() => Log.Info("Sandbox shutdown.");
}
