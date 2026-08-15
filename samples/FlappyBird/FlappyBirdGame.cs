using System.Numerics;
using InfiniteDubhe.Audio;
using InfiniteDubhe.Core;
using InfiniteDubhe.Input;
using InfiniteDubhe.Rendering;
using InfiniteDubhe.Scene;
using InfiniteDubhe.UI;

namespace FlappyBird;

/// <summary>
/// Flappy Bird 示例：纯程序化素材（小鸟/管道/云/地面均由代码生成），演示
/// 场景/组件、输入、UI、音频与精灵渲染。空格或鼠标左键拍翅；撞管道/落地即结束。
/// </summary>
public sealed class FlappyBirdGame : Game
{
    private enum State { Ready, Playing, GameOver }

    // 常量（竖屏 480×720）。
    private const float Gravity = 1400f;
    private const float FlapVelocity = -430f;
    private const float PipeSpeed = 190f;
    private const float PipeInterval = 1.6f;
    private const float PipeWidth = 70f;
    private const float CapWidth = 86f;
    private const float CapHeight = 26f;
    private const float GapSize = 175f;
    private const float BirdXFactor = 0.3f;
    private const float GroundHeight = 80f;

    private Scene? _scene;
    private Renderer? _renderer;
    private ITexture? _white;
    private ITexture? _birdTexture;
    private ITexture? _pipeBodyTexture;
    private ITexture? _pipeCapTexture;
    private ITexture? _cloudTexture;

    private GameObject? _bird;
    private SpriteAnimator? _animator;
    private readonly List<PipePair> _pipes = new();
    private readonly List<GameObject> _clouds = new();

    private State _state = State.Ready;
    private float _birdY;
    private float _birdVelocity;
    private float _baseBirdY;
    private float _bobTime;
    private float _pipeSpawnTimer;
    private int _score;

    private Text? _scoreText;
    private Text? _readyText;
    private Panel? _gameOverPanel;
    private Text? _gameOverScore;

    private AudioClip? _flapSfx;
    private AudioClip? _scoreSfx;
    private AudioClip? _hitSfx;
    private AudioClip? _bgm;

    private float BirdX => Config.Width * BirdXFactor;
    private float GroundY => Config.Height - GroundHeight;
    private float MinGapCenter => 150f;
    private float MaxGapCenter => GroundY - 150f;

    // 难度递增：随得分加快速度、缩小间隙、缩短生成间隔（各有限幅）。
    private float CurrentPipeSpeed => MathF.Min(PipeSpeed + _score * 6f, 350f);
    private float CurrentGap => MathF.Max(GapSize - _score * 4f, 120f);
    private float CurrentPipeInterval => MathF.Max(PipeInterval - _score * 0.03f, 1.05f);

    public FlappyBirdGame(GameConfig config) : base(config) { }

    protected override void Initialize() => Log.Info("FlappyBird initialized.");

    protected override void LoadContent()
    {
        _renderer = GetService<Renderer>()!;
        _scene = new Scene("FlappyBird");
        GetService<SceneManager>()!.Load(_scene);

        _renderer.ClearColor = Color.FromRgb(113, 197, 207); // 天空蓝

        _white = _renderer.CreateTexture(1, 1, new byte[] { 255, 255, 255, 255 });
        _birdTexture = GenerateBirdSheet(_renderer);
        _pipeBodyTexture = GeneratePipeBody(_renderer);
        _pipeCapTexture = GeneratePipeCap(_renderer);
        _cloudTexture = GenerateCloud(_renderer);

        BuildBird();
        BuildGround();
        BuildUi();
        BuildAudio();
        SpawnClouds();

        Audio.PlayBgm(_bgm!, 0.45f, 1.5f); // 循环 BGM，1.5s 淡入

        Log.Info("FlappyBird ready. Audio available: " + Audio.IsAvailable + ".");
    }

    protected override void Update(float dt)
    {
        bool flap = Input.IsKeyPressed(Key.Space) || Input.IsMouseButtonPressed(MouseButton.Left);

        switch (_state)
        {
            case State.Ready:
                BobBird(dt);
                if (flap) StartGame();
                break;
            case State.Playing:
                UpdatePlaying(dt, flap);
                break;
            case State.GameOver:
                UpdateClouds(dt);
                if (flap) ResetGame();
                break;
        }
    }

    protected override void UnloadContent() => Log.Info("FlappyBird content unloaded.");

    protected override void Shutdown() => Log.Info("FlappyBird shutdown.");

    // ---- 游戏流程 ----

    private void StartGame()
    {
        _state = State.Playing;
        _birdVelocity = FlapVelocity;
        _pipeSpawnTimer = 0.8f;
        _readyText!.Visible = false;
        Audio.PlaySfx(_flapSfx!);
    }

    private void UpdatePlaying(float dt, bool flap)
    {
        if (flap)
        {
            _birdVelocity = FlapVelocity;
            Audio.PlaySfx(_flapSfx!);
        }

        _birdVelocity += Gravity * dt;
        _birdY += _birdVelocity * dt;

        // 顶部夹紧（不致死），底部触地判定。
        if (_birdY < 12f) { _birdY = 12f; _birdVelocity = MathF.Max(0f, _birdVelocity); }

        float rotation = Math.Clamp(_birdVelocity * 0.14f, -25f, 75f);
        _bird!.Transform.Position = new Vector2(BirdX, _birdY);
        _bird.Transform.RotationDeg = rotation;

        // 上升时扑翅更快，下落时放慢。
        if (_animator is not null) _animator.Speed = _birdVelocity < 0f ? 2.2f : 1f;

        _pipeSpawnTimer -= dt;
        if (_pipeSpawnTimer <= 0f)
        {
            _pipeSpawnTimer = CurrentPipeInterval;
            SpawnPipe();
        }

        MovePipes(dt);
        RemoveOffscreenPipes();
        ScorePipes();
        UpdateClouds(dt);

        if (_birdY + 11f >= GroundY || HitsPipes())
            GameOver();
    }

    private void GameOver()
    {
        _state = State.GameOver;
        Audio.PlaySfx(_hitSfx!);
        _gameOverScore!.Content = $"Score: {_score}";
        _gameOverPanel!.Visible = true;
    }

    private void ResetGame()
    {
        _state = State.Ready;
        _score = 0;
        _scoreText!.Content = "0";
        _birdVelocity = 0f;
        _birdY = _baseBirdY;
        _bird!.Transform.RotationDeg = 0f;
        _gameOverPanel!.Visible = false;
        _readyText!.Visible = true;

        foreach (var pair in _pipes)
            foreach (var part in pair.Parts) part.Destroy();
        _pipes.Clear();
    }

    private void BobBird(float dt)
    {
        _bobTime += dt;
        _birdY = _baseBirdY + MathF.Sin(_bobTime * 4f) * 8f;
        _bird!.Transform.Position = new Vector2(BirdX, _birdY);
        _bird.Transform.RotationDeg = 0f;
    }

    // ---- 管道 ----

    private sealed class PipePair
    {
        public readonly List<GameObject> Parts = new();
        public float X;
        public float GapTop;
        public float GapBottom;
        public bool Scored;
    }

    private void SpawnPipe()
    {
        float gapCenter = MinGapCenter + Random.Shared.NextSingle() * (MaxGapCenter - MinGapCenter);
        float gapTop = gapCenter - CurrentGap * 0.5f;
        float gapBottom = gapCenter + CurrentGap * 0.5f;
        float x = Config.Width + 40f;
        float halfOverhang = (CapWidth - PipeWidth) * 0.5f;

        var pair = new PipePair { X = x, GapTop = gapTop, GapBottom = gapBottom };

        pair.Parts.Add(MakePipeBody(x, 0f, gapTop - CapHeight));
        pair.Parts.Add(MakePipeCap(x - halfOverhang, gapTop - CapHeight));
        pair.Parts.Add(MakePipeBody(x, gapBottom + CapHeight, GroundY - gapBottom - CapHeight));
        pair.Parts.Add(MakePipeCap(x - halfOverhang, gapBottom));

        _pipes.Add(pair);
    }

    private GameObject MakePipeBody(float x, float top, float height)
    {
        var go = _scene!.CreateObject("PipeBody");
        go.Transform.Position = new Vector2(x, top);
        go.Transform.Scale = new Vector2(1f, height); // 纹理为 PipeWidth×1，纵向拉伸
        var sr = go.AddComponent<SpriteRenderer>();
        sr.Texture = _pipeBodyTexture;
        return go;
    }

    private GameObject MakePipeCap(float x, float top)
    {
        var go = _scene!.CreateObject("PipeCap");
        go.Transform.Position = new Vector2(x, top);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.Texture = _pipeCapTexture;
        return go;
    }

    private void MovePipes(float dt)
    {
        float dx = -CurrentPipeSpeed * dt;
        foreach (var pair in _pipes)
        {
            pair.X += dx;
            foreach (var part in pair.Parts)
            {
                var p = part.Transform.Position;
                part.Transform.Position = new Vector2(p.X + dx, p.Y);
            }
        }
    }

    private void RemoveOffscreenPipes()
    {
        for (int i = _pipes.Count - 1; i >= 0; i--)
        {
            if (_pipes[i].X + PipeWidth < -20f)
            {
                foreach (var part in _pipes[i].Parts) part.Destroy();
                _pipes.RemoveAt(i);
            }
        }
    }

    private void ScorePipes()
    {
        foreach (var pair in _pipes)
        {
            if (!pair.Scored && pair.X + PipeWidth < BirdX)
            {
                pair.Scored = true;
                _score++;
                _scoreText!.Content = _score.ToString();
                Audio.PlaySfx(_scoreSfx!);
            }
        }
    }

    private bool HitsPipes()
    {
        const float hw = 10f, hh = 9f;
        float left = BirdX - hw, right = BirdX + hw, top = _birdY - hh, bottom = _birdY + hh;
        foreach (var pair in _pipes)
        {
            if (right < pair.X || left > pair.X + PipeWidth) continue;
            if (top < pair.GapTop || bottom > pair.GapBottom) return true;
        }
        return false;
    }

    // ---- 构建 ----

    private void BuildBird()
    {
        _baseBirdY = Config.Height * 0.45f;
        _birdY = _baseBirdY;
        _bird = _scene!.CreateObject("Bird");
        _bird.Transform.Position = new Vector2(BirdX, _birdY);
        var sr = _bird.AddComponent<SpriteRenderer>();
        sr.Texture = _birdTexture;
        sr.Origin = new Vector2(20f, 14f); // 单帧 40×28 中心，绕中心旋转
        sr.Layer = 2;

        _animator = _bird.AddComponent<SpriteAnimator>();
        _animator.AddClip(SpriteAnimationClip.FromRow("flap", new Rectangle(0, 0, 40, 28), 3, 10, true));
        _animator.Play("flap");
    }

    private void BuildGround()
    {
        var grass = _scene!.CreateObject("Grass");
        grass.Transform.Position = new Vector2(0f, GroundY);
        grass.Transform.Scale = new Vector2(Config.Width, 16f);
        var gs = grass.AddComponent<SpriteRenderer>();
        gs.Texture = _white;
        gs.Color = Color.FromRgb(120, 190, 70);
        gs.Layer = 1;

        var dirt = _scene!.CreateObject("Dirt");
        dirt.Transform.Position = new Vector2(0f, GroundY + 16f);
        dirt.Transform.Scale = new Vector2(Config.Width, GroundHeight - 16f);
        var ds = dirt.AddComponent<SpriteRenderer>();
        ds.Texture = _white;
        ds.Color = Color.FromRgb(214, 196, 130);
        ds.Layer = 1;
    }

    private void BuildUi()
    {
        var canvas = _scene!.CreateObject("Canvas").AddComponent<Canvas>();
        canvas.Initialize(_renderer!);

        _scoreText = canvas.Add(new Text("0", 5f)
        {
            Anchor = new Vector2(0.5f, 0f),
            Pivot = new Vector2(0.5f, 0f),
            Position = new Vector2(0f, 24f),
            Color = Color.White,
        });

        _readyText = canvas.Add(new Text("SPACE / CLICK to flap", 2f)
        {
            Anchor = new Vector2(0.5f, 0.4f),
            Pivot = new Vector2(0.5f, 0.5f),
            Color = Color.White,
        });

        _gameOverPanel = canvas.Add(new Panel
        {
            Size = new Vector2(340f, 190f),
            Anchor = new Vector2(0.5f, 0.5f),
            Pivot = new Vector2(0.5f, 0.5f),
            Color = Color.FromRgb(20, 24, 40, 210),
            Visible = false,
        });
        _gameOverPanel.AddChild(new Text("GAME OVER", 3f)
        {
            Anchor = new Vector2(0.5f, 0f), Pivot = new Vector2(0.5f, 0f), Position = new Vector2(0f, 20f),
        });
        _gameOverScore = _gameOverPanel.AddChild(new Text("Score: 0", 2f)
        {
            Anchor = new Vector2(0.5f, 0.5f), Pivot = new Vector2(0.5f, 0.5f), Position = new Vector2(0f, 12f),
        });
        _gameOverPanel.AddChild(new Text("SPACE / CLICK to restart", 1.5f)
        {
            Anchor = new Vector2(0.5f, 1f), Pivot = new Vector2(0.5f, 1f), Position = new Vector2(0f, -20f),
        });
    }

    private void SpawnClouds()
    {
        for (int i = 0; i < 5; i++)
        {
            var c = _scene!.CreateObject($"Cloud{i}");
            c.Transform.Position = new Vector2(Random.Shared.NextSingle() * Config.Width, 50f + Random.Shared.NextSingle() * 220f);
            float s = 0.6f + Random.Shared.NextSingle() * 0.9f;
            c.Transform.Scale = new Vector2(s, s);
            var sr = c.AddComponent<SpriteRenderer>();
            sr.Texture = _cloudTexture;
            sr.Layer = -10;
            _clouds.Add(c);
        }
    }

    private void UpdateClouds(float dt)
    {
        foreach (var cloud in _clouds)
        {
            var p = cloud.Transform.Position;
            cloud.Transform.Position = new Vector2(p.X - 22f * dt, p.Y);
            if (p.X < -110f)
                cloud.Transform.Position = new Vector2(Config.Width + 60f, 50f + Random.Shared.NextSingle() * 220f);
        }
    }

    private void BuildAudio()
    {
        _flapSfx = MakeSweep(400f, 900f, 0.12f);
        _scoreSfx = MakeTone(880f, 0.15f, 5f);
        _hitSfx = MakeTone(140f, 0.35f, 10f);
        _bgm = MakeBgm();
    }

    // ---- 程序化素材 ----

    private static ITexture GenerateBirdSheet(Renderer renderer)
    {
        const int frames = 3, w = 40, h = 28;
        int sheetW = w * frames;
        var rgba = new byte[sheetW * h * 4];
        float[] wingY = { 8f, 12f, 16f }; // 翅膀 上 / 中 / 下
        for (int f = 0; f < frames; f++)
        {
            int ox = f * w;
            FillEllipse(rgba, sheetW, h, ox + 18f, 14f, 14f, 12f, 250, 205, 40, 255); // 身体
            FillEllipse(rgba, sheetW, h, ox + 21f, wingY[f], 8f, 5f, 240, 190, 30, 255); // 翅膀
            FillEllipse(rgba, sheetW, h, ox + 27f, 10f, 5f, 5f, 255, 255, 255, 255);     // 眼白
            FillEllipse(rgba, sheetW, h, ox + 29f, 10f, 2.2f, 2.2f, 20, 20, 20, 255);    // 瞳孔
            FillEllipse(rgba, sheetW, h, ox + 36f, 16f, 6f, 3.5f, 240, 120, 20, 255);    // 喙
        }
        return renderer.CreateTexture(sheetW, h, rgba);
    }

    private static ITexture GeneratePipeBody(Renderer renderer)
    {
        int w = (int)PipeWidth, h = 1;
        var rgba = new byte[w * 4];
        for (int x = 0; x < w; x++)
        {
            float t = x / (float)(w - 1);
            float shade = 1f - 0.35f * t; // 左亮右暗，伪圆柱
            int o = x * 4;
            rgba[o] = (byte)(60 * shade + 20);
            rgba[o + 1] = (byte)(160 * shade + 30);
            rgba[o + 2] = (byte)(80 * shade + 20);
            rgba[o + 3] = 255;
        }
        return renderer.CreateTexture(w, h, rgba);
    }

    private static ITexture GeneratePipeCap(Renderer renderer)
    {
        int w = (int)CapWidth, h = (int)CapHeight;
        var rgba = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool border = x < 2 || x >= w - 2 || y < 2 || y >= h - 2;
                byte r = border ? (byte)40 : (byte)70;
                byte g = border ? (byte)90 : (byte)170;
                byte b = border ? (byte)50 : (byte)90;
                int o = (y * w + x) * 4;
                rgba[o] = r; rgba[o + 1] = g; rgba[o + 2] = b; rgba[o + 3] = 255;
            }
        }
        return renderer.CreateTexture(w, h, rgba);
    }

    private static ITexture GenerateCloud(Renderer renderer)
    {
        const int w = 96, h = 40;
        var rgba = new byte[w * h * 4];
        FillEllipse(rgba, w, h, 30f, 26f, 22f, 12f, 255, 255, 255, 235);
        FillEllipse(rgba, w, h, 52f, 20f, 18f, 13f, 255, 255, 255, 235);
        FillEllipse(rgba, w, h, 70f, 25f, 16f, 11f, 255, 255, 255, 235);
        FillEllipse(rgba, w, h, 48f, 30f, 30f, 10f, 255, 255, 255, 235);
        return renderer.CreateTexture(w, h, rgba);
    }

    private static void FillEllipse(byte[] rgba, int w, int h, float cx, float cy, float rx, float ry, byte r, byte g, byte b, byte a)
    {
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x + 0.5f - cx) / rx;
                float ny = (y + 0.5f - cy) / ry;
                if (nx * nx + ny * ny > 1f) continue;
                int o = (y * w + x) * 4;
                rgba[o] = r; rgba[o + 1] = g; rgba[o + 2] = b; rgba[o + 3] = a;
            }
        }
    }

    // ---- 音频生成 ----

    private static AudioClip MakeBgm(int sampleRate = 22050)
    {
        float[] notes = { 261.63f, 329.63f, 392f, 523.25f, 392f, 329.63f, 349.23f, 293.66f };
        const float noteDuration = 0.5f;
        int total = (int)(sampleRate * notes.Length * noteDuration);
        float loopLength = notes.Length * noteDuration;
        var samples = new short[total];
        for (int i = 0; i < total; i++)
        {
            float t = i / (float)sampleRate;
            int idx = Math.Min((int)(t / noteDuration), notes.Length - 1);
            float local = t - idx * noteDuration;
            float freq = notes[idx];

            float attack = MathF.Min(1f, local * 20f);
            float env = attack * MathF.Exp(-2.5f * local);
            // 首尾淡入淡出，保证循环无缝无爆音。
            float master = MathF.Min(Math.Clamp(t / 0.1f, 0f, 1f), Math.Clamp((loopLength - t) / 0.1f, 0f, 1f));
            float v = MathF.Sin(2f * MathF.PI * freq * t) * 0.7f + MathF.Sin(2f * MathF.PI * freq * 2f * t) * 0.3f;
            samples[i] = (short)(v * env * master * 6000f);
        }
        return AudioClip.Create(samples, sampleRate, 1);
    }

    private static AudioClip MakeTone(float frequency, float seconds, float decay, int sampleRate = 22050)
    {
        int n = (int)(sampleRate * seconds);
        var samples = new short[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sampleRate;
            samples[i] = (short)(MathF.Sin(2f * MathF.PI * frequency * t) * MathF.Exp(-decay * t) * 14000f);
        }
        return AudioClip.Create(samples, sampleRate, 1);
    }

    private static AudioClip MakeSweep(float fromHz, float toHz, float seconds, int sampleRate = 22050)
    {
        int n = (int)(sampleRate * seconds);
        var samples = new short[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sampleRate;
            float phase = 2f * MathF.PI * (fromHz * t + 0.5f * (toHz - fromHz) * t * t / seconds);
            samples[i] = (short)(MathF.Sin(phase) * MathF.Exp(-4f * t) * 14000f);
        }
        return AudioClip.Create(samples, sampleRate, 1);
    }
}
