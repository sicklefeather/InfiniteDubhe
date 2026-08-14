namespace InfiniteDubhe.Core;

/// <summary>
/// 游戏基类。用户继承并重写生命周期钩子；主循环由引擎运行时（Engine.GameHost）驱动。
/// 本类位于 Core（无依赖），不直接引用平台/渲染类型。
/// </summary>
public abstract class Game
{
    protected Game(GameConfig config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public GameConfig Config { get; }

    protected virtual void Initialize() { }
    protected virtual void LoadContent() { }
    protected virtual void Update(float dt) { }
    protected virtual void FixedUpdate(float dt) { }
    protected virtual void Render() { }
    protected virtual void UnloadContent() { }
    protected virtual void Shutdown() { }

    // 以下内部入口供引擎运行时（InfiniteDubhe.Engine）调用，用户不直接使用。
    internal void OnInitialize() => Initialize();
    internal void OnLoadContent() => LoadContent();
    internal void OnUpdate(float dt) => Update(dt);
    internal void OnFixedUpdate(float dt) => FixedUpdate(dt);
    internal void OnRender() => Render();
    internal void OnUnloadContent() => UnloadContent();
    internal void OnShutdown() => Shutdown();
}
