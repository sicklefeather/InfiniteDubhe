using System.Numerics;
using InfiniteDubhe.Audio;
using InfiniteDubhe.Core;
using InfiniteDubhe.Input;
using InfiniteDubhe.Physics;
using InfiniteDubhe.Rendering;
using InfiniteDubhe.Resources;
using InfiniteDubhe.Scene;
using InfiniteDubhe.UI;

namespace Sandbox;

/// <summary>
/// M2 示例游戏：在同一场景中演示物理（重力/刚体/碰撞）、音频（SFX/BGM/淡入淡出）、
/// 动画（帧动画 + 补间）、UI（文本/按钮/面板/图片 + 布局 + 事件）与调试绘制（射线）。
/// </summary>
public sealed class SandboxGame : Game
{
    private const float GroundY = 680f;
    private const float BoxSize = 32f;
    private const float BallRadius = 16f;

    private Scene? _scene;
    private Renderer? _renderer;
    private ITexture? _white;
    private ITexture? _circle;
    private ITexture? _coinSheet;

    private GameObject? _coin;
    private SpriteAnimator? _coinAnimator;

    private readonly List<GameObject> _dynamicBodies = new();

    private Text? _statusText;
    private Text? _fpsText;
    private AudioClip? _sfx;
    private AudioClip? _bgm;

    // 延迟生成队列：按钮点击发生在场景遍历中，不能直接修改场景集合。
    private int _pendingBoxes;
    private int _pendingBalls;
    private bool _pendingClear;
    private float _autoSpawnTimer = 1f;

    public SandboxGame(GameConfig config) : base(config) { }

    protected override void Initialize() => Log.Info("Sandbox (M2) initialized.");

    protected override void LoadContent()
    {
        _renderer = GetService<Renderer>()!;
        var sceneManager = GetService<SceneManager>()!;
        _scene = new Scene("M2Demo");
        sceneManager.Load(_scene);

        _white = _renderer.CreateTexture(1, 1, new byte[] { 255, 255, 255, 255 });
        _circle = GenerateCircle(_renderer, (int)(BallRadius * 2f));

        BuildAudio();
        BuildPhysics();
        BuildCoin();
        BuildUi();

        StartCoinMotion();
        Log.Info($"Sandbox (M2) ready. Audio available: {Audio.IsAvailable}.");
    }

    protected override void Update(float dt)
    {
        // 自动补充箱子，保持物理演示持续活跃。
        _autoSpawnTimer -= dt;
        if (_autoSpawnTimer <= 0f)
        {
            _autoSpawnTimer = 1.1f;
            _pendingBoxes++;
        }

        // 处理延迟生成（场景遍历结束之后，本帧 OnUpdate 内）。
        while (_pendingBoxes-- > 0) SpawnBox();
        while (_pendingBalls-- > 0) SpawnBall();
        if (_pendingClear)
        {
            _pendingClear = false;
            foreach (var go in _dynamicBodies) go.Destroy();
            _dynamicBodies.Clear();
        }

        // 状态文本。
        if (_statusText is not null) _statusText.Content = $"Bodies: {_dynamicBodies.Count}";
        if (_fpsText is not null)
            _fpsText.Content = $"FPS: {(int)(1f / MathF.Max(Time.DeltaTime, 1e-4f))}";

        // 调试绘制：从鼠标向下发射射线，命中处画圆。
        var mouse = Input.MousePosition;
        Debug.DrawRay(mouse, new Vector2(0f, 1f), Color.FromRgb(255, 220, 0), 400f, 2f);
        var hit = Physics2D.Raycast(mouse, mouse + new Vector2(0f, 400f));
        if (hit is not null)
            Debug.DrawCircle(hit.Value.Point, 6f, Color.FromRgb(230, 50, 50), 24, 2f);
    }

    protected override void UnloadContent() => Log.Info("Sandbox content unloaded.");

    protected override void Shutdown() => Log.Info("Sandbox shutdown.");

    // ---- 物理 ----

    private void BuildPhysics()
    {
        _scene!.CreateObject("PhysicsWorld").AddComponent<PhysicsWorld2D>().Gravity = new Vector2(0f, 980f);

        CreateStatic("Ground", new Vector2(Config.Width / 2f, GroundY), new Vector2(Config.Width, 40f), Color.FromRgb(60, 60, 84));
        CreateStatic("WallLeft", new Vector2(-20f, Config.Height / 2f), new Vector2(40f, Config.Height), Color.FromRgb(60, 60, 84));
        CreateStatic("WallRight", new Vector2(Config.Width + 20f, Config.Height / 2f), new Vector2(40f, Config.Height), Color.FromRgb(60, 60, 84));
    }

    private void CreateStatic(string name, Vector2 position, Vector2 size, Color color)
    {
        var go = _scene!.CreateObject(name);
        go.Transform.Position = position;
        go.Transform.Scale = size;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.Texture = _white;
        sr.Origin = size * 0.5f;
        sr.Color = color;
        go.AddComponent<Rigidbody2D>().Type = BodyType2D.Static;
        go.AddComponent<BoxCollider2D>().Size = size;
    }

    private void SpawnBox()
    {
        float x = 100f + Random.Shared.NextSingle() * (Config.Width - 200f);
        var go = _scene!.CreateObject($"Box{_dynamicBodies.Count}");
        go.Transform.Position = new Vector2(x, 80f);
        go.Transform.Scale = new Vector2(BoxSize, BoxSize);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.Texture = _white;
        sr.Origin = new Vector2(BoxSize * 0.5f, BoxSize * 0.5f);
        sr.Color = RandomColor();
        go.AddComponent<Rigidbody2D>().Type = BodyType2D.Dynamic;
        go.AddComponent<BoxCollider2D>().Size = new Vector2(BoxSize, BoxSize);
        _dynamicBodies.Add(go);
    }

    private void SpawnBall()
    {
        float x = 100f + Random.Shared.NextSingle() * (Config.Width - 200f);
        var go = _scene!.CreateObject($"Ball{_dynamicBodies.Count}");
        go.Transform.Position = new Vector2(x, 80f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.Texture = _circle;
        sr.Origin = new Vector2(BallRadius, BallRadius);
        sr.Color = RandomColor();
        go.AddComponent<Rigidbody2D>().Type = BodyType2D.Dynamic;
        go.AddComponent<CircleCollider2D>().Radius = BallRadius;
        _dynamicBodies.Add(go);
    }

    private static Color RandomColor()
    {
        byte r = (byte)Random.Shared.Next(120, 256);
        byte g = (byte)Random.Shared.Next(120, 256);
        byte b = (byte)Random.Shared.Next(120, 256);
        return Color.FromRgb(r, g, b);
    }

    // ---- 动画 ----

    private void BuildCoin()
    {
        _coinSheet = GenerateCoinSheet(_renderer!, 4, 48);
        _coin = _scene!.CreateObject("Coin");
        _coin.Transform.Position = new Vector2(640f, 320f);
        var sr = _coin.AddComponent<SpriteRenderer>();
        sr.Texture = _coinSheet;
        sr.Origin = new Vector2(24f, 24f);

        _coinAnimator = _coin.AddComponent<SpriteAnimator>();
        _coinAnimator.AddClip(SpriteAnimationClip.FromRow("spin", new Rectangle(0, 0, 48, 48), 4, 8, true));
        _coinAnimator.Play("spin");
    }

    /// <summary>用补间让金币左右往返（完成回调串联下一段，无限循环）。</summary>
    private void StartCoinMotion()
    {
        if (_coin is null) return;
        float y = _coin.Transform.Position.Y;

        Tween.To(() => _coin.Transform.Position, v => _coin.Transform.Position = v,
            new Vector2(740f, y), 1.6f, Ease.InOutSine,
            () => Tween.To(() => _coin.Transform.Position, v => _coin.Transform.Position = v,
                new Vector2(540f, y), 1.6f, Ease.InOutSine, StartCoinMotion));
    }

    private void ToggleSpin()
    {
        if (_coinAnimator is null) return;
        if (_coinAnimator.IsPlaying) _coinAnimator.Pause();
        else _coinAnimator.Resume();
    }

    // ---- UI ----

    private void BuildUi()
    {
        var canvas = _scene!.CreateObject("Canvas").AddComponent<Canvas>();
        canvas.Initialize(_renderer!);

        canvas.Add(new Text("InfiniteDubhe  M2  Demo", 3f)
        {
            Anchor = new Vector2(0.5f, 0f),
            Pivot = new Vector2(0.5f, 0f),
            Position = new Vector2(0f, 18f),
        });

        // 左面板：物理。
        var left = canvas.Add(new Panel
        {
            Size = new Vector2(240f, 340f),
            Position = new Vector2(16f, 64f),
            Color = Color.FromRgb(22, 26, 40, 220),
            Layout = LayoutDirection.Vertical,
            Spacing = 10f,
            Padding = new Vector2(14f, 14f),
        });
        left.AddChild(new Text("Physics", 2f));
        left.AddChild(MakeButton("Spawn Box", () => _pendingBoxes++));
        left.AddChild(MakeButton("Spawn Ball", () => _pendingBalls++));
        left.AddChild(MakeButton("Clear", () => _pendingClear = true));

        // 右面板：音频 + 动画 + 图片。
        var right = canvas.Add(new Panel
        {
            Size = new Vector2(260f, 460f),
            Anchor = new Vector2(1f, 0f),
            Pivot = new Vector2(1f, 0f),
            Position = new Vector2(-16f, 64f),
            Color = Color.FromRgb(22, 26, 40, 220),
            Layout = LayoutDirection.Vertical,
            Spacing = 10f,
            Padding = new Vector2(14f, 14f),
        });
        right.AddChild(new Text("Audio", 2f));
        right.AddChild(MakeButton("Play SFX", PlaySfx));
        right.AddChild(MakeButton("Play BGM", PlayBgm));
        right.AddChild(MakeButton("Stop BGM", StopBgm));
        right.AddChild(new Text("Animation", 2f));
        right.AddChild(MakeButton("Toggle Spin", ToggleSpin));
        right.AddChild(new Text("Image (sprite sheet)", 1.5f));
        right.AddChild(new Image { Texture = _coinSheet, Size = new Vector2(232f, 58f) });

        _statusText = canvas.Add(new Text("Bodies: 0", 1.5f)
        {
            Anchor = new Vector2(0f, 1f),
            Pivot = new Vector2(0f, 1f),
            Position = new Vector2(16f, -16f),
        });
        _fpsText = canvas.Add(new Text("FPS: --", 1.5f)
        {
            Anchor = new Vector2(1f, 1f),
            Pivot = new Vector2(1f, 1f),
            Position = new Vector2(-16f, -16f),
            Color = Color.FromRgb(200, 220, 255),
        });
    }

    private Button MakeButton(string label, Action onClick)
    {
        var button = new Button(label, 2f) { Size = new Vector2(212f, 40f) };
        button.Clicked += _ => onClick();
        return button;
    }

    // ---- 音频 ----

    private void PlaySfx() => Audio.PlaySfx(_sfx!, 1f, 1f);

    private void PlayBgm() => Audio.PlayBgm(_bgm!, 0.6f, 1.5f);

    private void StopBgm() => Audio.StopBgm(1f);

    private void BuildAudio()
    {
        _sfx = MakeTone(660f, 0.25f);
        _bgm = MakeArpeggio();
    }

    private static AudioClip MakeTone(float frequency, float seconds, int sampleRate = 22050)
    {
        int count = (int)(sampleRate * seconds);
        var samples = new short[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = MathF.Exp(-8f * t); // 指数衰减，短促“哔”。
            samples[i] = (short)(MathF.Sin(2f * MathF.PI * frequency * t) * envelope * 16000f);
        }
        return AudioClip.Create(samples, sampleRate, 1);
    }

    private static AudioClip MakeArpeggio(int sampleRate = 22050)
    {
        float[] notes = { 261.63f, 329.63f, 392f, 523.25f, 392f, 329.63f, 293.66f, 261.63f };
        const float noteDuration = 0.5f;
        int total = (int)(sampleRate * notes.Length * noteDuration);
        var samples = new short[total];
        for (int i = 0; i < total; i++)
        {
            float t = i / (float)sampleRate;
            int noteIndex = Math.Min((int)(t / noteDuration), notes.Length - 1);
            float local = t - noteIndex * noteDuration;
            float envelope = MathF.Exp(-3f * local);
            samples[i] = (short)(MathF.Sin(2f * MathF.PI * notes[noteIndex] * t) * envelope * 9000f);
        }
        return AudioClip.Create(samples, sampleRate, 1);
    }

    // ---- 程序化纹理 ----

    private static ITexture GenerateCircle(Renderer renderer, int size)
    {
        var rgba = new byte[size * size * 4];
        float c = (size - 1) * 0.5f;
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                if (dx * dx + dy * dy <= r * r)
                {
                    int o = (y * size + x) * 4;
                    rgba[o] = rgba[o + 1] = rgba[o + 2] = rgba[o + 3] = 255;
                }
            }
        }
        return renderer.CreateTexture(size, size, rgba);
    }

    /// <summary>生成一枚“旋转金币”精灵表：4 帧椭圆，宽度逐帧收缩（模拟转动）。</summary>
    private static ITexture GenerateCoinSheet(Renderer renderer, int frames, int size)
    {
        int width = size * frames;
        var rgba = new byte[width * size * 4];
        float[] halfWidths = { size * 0.5f, size * 0.34f, size * 0.13f, size * 0.34f };

        for (int f = 0; f < frames; f++)
        {
            float cx = size * 0.5f, cy = size * 0.5f;
            float rx = halfWidths[f], ry = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - cx) / rx;
                    float ny = (y + 0.5f - cy) / ry;
                    if (nx * nx + ny * ny > 1f) continue;
                    int o = (y * width + f * size + x) * 4;
                    rgba[o] = 255;
                    rgba[o + 1] = 200;
                    rgba[o + 2] = 40;
                    rgba[o + 3] = 255;
                }
            }
        }
        return renderer.CreateTexture(width, size, rgba);
    }
}
