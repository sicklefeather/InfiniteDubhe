using InfiniteDubhe.Core;
using InfiniteDubhe.Platform;
using InfiniteDubhe.Rendering;
using InfiniteDubhe.Resources;
using InfiniteDubhe.Scene;
using AudioFacade = InfiniteDubhe.Audio.Audio;
using InputFacade = InfiniteDubhe.Input.Input;

namespace InfiniteDubhe.Engine;

/// <summary>
/// 引擎运行时：装配平台/渲染/场景/资源/输入并驱动主循环（固定步长 + 可变渲染）。
/// 位于 Engine（依赖 Core + Platform + Rendering + Scene + Input + Resources），使 Core 保持零平台依赖。
/// </summary>
public sealed class GameHost
{
    private readonly IPlatformBootstrap _platform;
    private readonly List<IRenderable> _renderables = new();

    public GameHost(IPlatformBootstrap platform)
    {
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
    }

    public void Run(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        Coroutines.Global.StopAll();   // 重置全局协程，避免多次 Run 残留
        Tween.StopAll();               // 重置全局补间

        var window = _platform.CreateWindow(game.Config);
        var graphics = _platform.CreateGraphicsContext(window);
        var input = _platform.CreateInput(window);
        var clock = _platform.CreateClock();
        var fileSystem = _platform.CreateFileSystem();

        var renderer = new Renderer(graphics, game.Config.Width, game.Config.Height);
        var sceneManager = new SceneManager();
        var resources = new ResourceManager();
        resources.RegisterLoader<ITexture>(new TextureLoader(fileSystem, (w, h, rgba) => renderer.CreateTexture(w, h, rgba)));

        // 注入全局门面与子系统访问入口。
        InputFacade.Source = input;
        game.Services = new ServiceLocator()
            .Add(sceneManager)
            .Add(resources)
            .Add(renderer);

        Time.FixedDeltaTime = game.Config.FixedTimestep;

        try
        {
            window.Initialize();      // 触发 Load：创建图形设备 + 输入设备
            renderer.Initialize();    // 设备就绪后创建 SpriteBatch 等 GPU 资源
            AudioFacade.Initialize(); // 音频子系统（OpenAL 未安装时静默降级）

            game.OnInitialize();
            game.OnLoadContent();

            var accumulator = 0f;
            while (!window.IsClosing)
            {
                Profiler.BeginFrame();

                input.Update();        // 推进边沿触发状态
                window.ProcessEvents();

                // Esc 退出（M0 内置；关闭按钮经 IsClosing 处理）。
                if (input.IsKeyPressed(Key.Escape))
                {
                    break;
                }

                var dt = Math.Min(clock.Tick(), game.Config.MaxDeltaTime);
                Time.AdvanceFrame(dt);

                // 固定步长累积（缩放，TimeScale = 0 时暂停）。
                accumulator += Time.ScaledDeltaTime;
                var iterations = 0;
                Profiler.BeginPhase(ProfilerPhase.FixedUpdate);
                while (accumulator >= Time.FixedDeltaTime && iterations++ < 100)
                {
                    sceneManager.FixedUpdate();
                    game.OnFixedUpdate(Time.FixedDeltaTime);
                    Coroutines.Global.FixedUpdate();
                    accumulator -= Time.FixedDeltaTime;
                }
                Profiler.EndPhase(ProfilerPhase.FixedUpdate);

                Profiler.BeginPhase(ProfilerPhase.Update);
                sceneManager.Update();
                game.OnUpdate(Time.ScaledDeltaTime);
                Coroutines.Global.Update(Time.ScaledDeltaTime);
                Tween.Update(Time.ScaledDeltaTime);
                AudioFacade.Update(Time.ScaledDeltaTime);
                Profiler.EndPhase(ProfilerPhase.Update);

                Profiler.BeginPhase(ProfilerPhase.Render);
                renderer.Clear();
                game.OnRender();

                _renderables.Clear();
                sceneManager.CollectRenderables(_renderables);
                renderer.Draw(_renderables);
                renderer.Present();
                Profiler.EndPhase(ProfilerPhase.Render);

                Profiler.EndFrame();

                sceneManager.EndOfFrame();
            }

            game.OnUnloadContent();
            game.OnShutdown();
        }
        finally
        {
            AudioFacade.Shutdown();
            (renderer as IDisposable)?.Dispose();
            (graphics as IDisposable)?.Dispose();
            (input as IDisposable)?.Dispose();
            (window as IDisposable)?.Dispose();
        }
    }

    /// <summary>最小服务定位器：按类型解析子系统，供 <see cref="Game.GetService{T}"/> 使用。</summary>
    private sealed class ServiceLocator : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        public ServiceLocator Add<T>(T instance) where T : class
        {
            _services[typeof(T)] = instance;
            return this;
        }

        public object? GetService(Type serviceType)
            => _services.TryGetValue(serviceType, out var service) ? service : null;
    }
}
