using InfiniteDubhe.Core;
using InfiniteDubhe.Platform;
using InfiniteDubhe.Rendering;

namespace InfiniteDubhe.Engine;

/// <summary>
/// 引擎运行时：装配平台/渲染并驱动主循环（固定步长 + 可变渲染）。
/// 位于 Engine（依赖 Core + Platform + Rendering），使 Core 保持零平台依赖。
/// </summary>
public sealed class GameHost
{
    private readonly IPlatformBootstrap _platform;

    public GameHost(IPlatformBootstrap platform)
    {
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
    }

    public void Run(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var window = _platform.CreateWindow(game.Config);
        var graphics = _platform.CreateGraphicsContext(window);
        var input = _platform.CreateInput(window);
        var clock = _platform.CreateClock();
        var renderer = new Renderer(graphics);

        Time.FixedDeltaTime = game.Config.FixedTimestep;

        try
        {
            window.Initialize();

            game.OnInitialize();
            game.OnLoadContent();

            var accumulator = 0f;
            while (!window.IsClosing)
            {
                input.Update();
                window.ProcessEvents();

                // Esc 退出（M0 内置，关闭按钮经 IsClosing 处理）。
                if (input.IsKeyPressed(Key.Escape))
                {
                    break;
                }

                var dt = Math.Min(clock.Tick(), game.Config.MaxDeltaTime);
                Time.AdvanceFrame(dt);

                // 固定步长累积（缩放，TimeScale = 0 时暂停）。
                accumulator += Time.ScaledDeltaTime;
                var iterations = 0;
                while (accumulator >= Time.FixedDeltaTime && iterations++ < 100)
                {
                    game.OnFixedUpdate(Time.FixedDeltaTime);
                    accumulator -= Time.FixedDeltaTime;
                }

                game.OnUpdate(Time.ScaledDeltaTime);

                renderer.Clear();
                game.OnRender();
                renderer.Present();
            }

            game.OnUnloadContent();
            game.OnShutdown();
        }
        finally
        {
            (graphics as IDisposable)?.Dispose();
            (input as IDisposable)?.Dispose();
            (window as IDisposable)?.Dispose();
        }
    }
}
