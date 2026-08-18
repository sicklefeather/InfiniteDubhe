using System.Numerics;
using InfiniteDubhe.Core;
using InfiniteDubhe.Physics;
using InfiniteDubhe.Scene;
using InfiniteDubhe.UI;
using Xunit;
using SceneType = InfiniteDubhe.Scene.Scene;

namespace InfiniteDubhe.Resources.Tests;

public sealed class SceneSerializerTests
{
    [Fact]
    public void RoundTrip_PreservesHierarchyTransformsAndComponents()
    {
        var resources = new ResourceManager();
        resources.RegisterLoader<ITexture>(new FakeTextureLoader());
        var serializer = new SceneSerializer(resources);

        var scene = new SceneType("Test");

        // 层级：Root（含 SpriteRenderer + PhysicsWorld2D）→ Child（含 Rigidbody2D + BoxCollider2D）。
        var root = scene.CreateObject("Root");
        root.Transform.Position = new Vector2(10, 20);
        root.Transform.RotationDeg = 30f;
        root.Transform.Scale = new Vector2(2, 3);

        var child = root.CreateChild("Child");
        child.Transform.Position = new Vector2(5, 6);
        child.Transform.RotationDeg = -15f;

        var sr = root.AddComponent<SpriteRenderer>();
        sr.Texture = resources.Load<ITexture>("textures/player.png");
        sr.Color = Color.FromRgb(200, 100, 50);
        sr.Origin = new Vector2(16, 16);
        sr.Effects = SpriteEffects.FlipHorizontally;
        sr.Layer = 3;
        sr.LayerDepth = 0.5f;
        sr.SourceRect = new Rectangle(0, 0, 32, 32);

        var world = root.AddComponent<PhysicsWorld2D>();
        world.Gravity = new Vector2(0, 980);

        var rb = child.AddComponent<Rigidbody2D>();
        rb.Type = BodyType2D.Dynamic;
        rb.LinearDamping = 0.1f;
        rb.FixedRotation = true;
        rb.IgnoreGravity = true;

        var bc = child.AddComponent<BoxCollider2D>();
        bc.Size = new Vector2(32, 32);
        bc.Friction = 0.5f;
        bc.IsSensor = true;

        // 往返（场景名不入 JSON，由调用方命名——如文件名）。
        var json = serializer.Serialize(scene);
        var restored = serializer.Deserialize(json, "FromFile");

        // 场景名 + 根对象。
        Assert.Equal("FromFile", restored.Name);
        var restoredRoot = Assert.Single(restored.RootObjects);
        Assert.Equal(root.Id, restoredRoot.Id);
        Assert.Equal("Root", restoredRoot.Name);
        Assert.Equal(new Vector2(10, 20), restoredRoot.Transform.Position);
        Assert.Equal(30f, restoredRoot.Transform.RotationDeg);
        Assert.Equal(new Vector2(2, 3), restoredRoot.Transform.Scale);

        // 子对象（经 Transform.Children + Owner 访问）。
        var restoredChildTransform = Assert.Single(restoredRoot.Transform.Children);
        var restoredChild = restoredChildTransform.Owner;
        Assert.Equal(child.Id, restoredChild.Id);
        Assert.Equal("Child", restoredChild.Name);
        Assert.Equal(new Vector2(5, 6), restoredChild.Transform.Position);
        Assert.Equal(-15f, restoredChild.Transform.RotationDeg);

        // SpriteRenderer（含纹理路径重连）。
        var restoredSr = Assert.IsType<SpriteRenderer>(restoredRoot.GetComponent<SpriteRenderer>());
        Assert.NotNull(restoredSr.Texture);
        Assert.Equal("textures/player.png", resources.GetPath(restoredSr.Texture!));
        Assert.Equal(Color.FromRgb(200, 100, 50), restoredSr.Color);
        Assert.Equal(new Vector2(16, 16), restoredSr.Origin);
        Assert.Equal(SpriteEffects.FlipHorizontally, restoredSr.Effects);
        Assert.Equal(3, restoredSr.Layer);
        Assert.Equal(0.5f, restoredSr.LayerDepth);
        Assert.Equal(new Rectangle(0, 0, 32, 32), restoredSr.SourceRect);

        // 跨程序集组件（Physics）。
        var restoredWorld = Assert.IsType<PhysicsWorld2D>(restoredRoot.GetComponent<PhysicsWorld2D>());
        Assert.Equal(new Vector2(0, 980), restoredWorld.Gravity);

        var restoredRb = Assert.IsType<Rigidbody2D>(restoredChild.GetComponent<Rigidbody2D>());
        Assert.Equal(BodyType2D.Dynamic, restoredRb.Type);
        Assert.Equal(0.1f, restoredRb.LinearDamping);
        Assert.True(restoredRb.FixedRotation);
        Assert.True(restoredRb.IgnoreGravity);

        var restoredBc = Assert.IsType<BoxCollider2D>(restoredChild.GetComponent<BoxCollider2D>());
        Assert.Equal(new Vector2(32, 32), restoredBc.Size);
        Assert.Equal(0.5f, restoredBc.Friction);
        Assert.True(restoredBc.IsSensor);
    }

    [Fact]
    public void Serialize_IsStableAcrossRoundTrip()
    {
        var resources = new ResourceManager();
        resources.RegisterLoader<ITexture>(new FakeTextureLoader());
        var serializer = new SceneSerializer(resources);

        var scene = new SceneType("Stable");
        var go = scene.CreateObject("A");
        go.Transform.Position = new Vector2(1.5f, 2.5f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.Color = Color.FromRgb(10, 20, 30, 128);
        sr.Texture = resources.Load<ITexture>("a.png");

        var once = serializer.Serialize(scene);
        var twice = serializer.Serialize(serializer.Deserialize(once));
        Assert.Equal(once, twice);
    }

    [Fact]
    public void TextureReference_ResolvesByGuidAfterPathChange()
    {
        var resources = new ResourceManager();
        resources.RegisterLoader<ITexture>(new FakeTextureLoader());
        var resolver = new FakeGuidResolver();
        var serializer = new SceneSerializer(resources, resolver);

        var scene = new SceneType("Test");
        var go = scene.CreateObject("A");
        go.AddComponent<SpriteRenderer>().Texture = resources.Load<ITexture>("tex/a.png");

        var json = serializer.Serialize(scene);

        // 模拟资源改名：GUID 仍能解析到新路径。
        resolver.SetPath(resolver.GetGuid("tex/a.png"), "tex/a_renamed.png");

        var restored = serializer.Deserialize(json);
        var sr = restored.RootObjects.Single().GetComponent<SpriteRenderer>()!;
        Assert.NotNull(sr.Texture);
        Assert.Equal("tex/a_renamed.png", resources.GetPath(sr.Texture)); // 经 GUID 解析到新路径
    }

    [Fact]
    public void RoundTrip_PreservesCanvasUiTree()
    {
        var resources = new ResourceManager();
        resources.RegisterLoader<ITexture>(new FakeTextureLoader());
        var serializer = new SceneSerializer(resources);

        var scene = new SceneType("UI");
        var canvas = scene.CreateObject("Canvas").AddComponent<Canvas>();

        // 根 1：Panel（含 1 个 Text 子元素）。
        var panel = canvas.Add(new Panel
        {
            Size = new Vector2(240f, 120f),
            Position = new Vector2(16f, 64f),
            Color = Color.FromRgb(22, 26, 40, 220),
            Layout = LayoutDirection.Vertical,
            Spacing = 8f,
        });
        panel.AddChild(new Text("Score: 100", 2f) { Color = Color.White });

        // 根 2：Button（自动文本标签不应被重复序列化）。
        canvas.Add(new Button("Play", 2f)
        {
            Size = new Vector2(160f, 40f),
            BackgroundColor = Color.FromRgb(70, 70, 96),
        });

        // 根 3：Image（纹理走路径重连）。
        canvas.Add(new Image { Texture = resources.Load<ITexture>("tex/coin.png"), Size = new Vector2(64f, 64f) });

        var restored = serializer.Deserialize(serializer.Serialize(scene));

        var restoredCanvas = Assert.IsType<Canvas>(Assert.Single(restored.RootObjects).GetComponent<Canvas>());
        Assert.Equal(3, restoredCanvas.Roots.Count);

        // Panel + 子 Text。
        var restoredPanel = Assert.IsType<Panel>(restoredCanvas.Roots[0]);
        Assert.Equal(new Vector2(240f, 120f), restoredPanel.Size);
        Assert.Equal(new Vector2(16f, 64f), restoredPanel.Position);
        Assert.Equal(Color.FromRgb(22, 26, 40, 220), restoredPanel.Color);
        Assert.Equal(LayoutDirection.Vertical, restoredPanel.Layout);
        Assert.Equal(8f, restoredPanel.Spacing);
        var restoredText = Assert.IsType<Text>(Assert.Single(restoredPanel.Children));
        Assert.Equal("Score: 100", restoredText.Content);
        Assert.Equal(2f, restoredText.Scale);

        // Button：标签未重复（仍只有 1 个自动标签子元素），属性经 Label/BackgroundColor 承载。
        var restoredButton = Assert.IsType<Button>(restoredCanvas.Roots[1]);
        Assert.Equal("Play", restoredButton.Label);
        Assert.Equal(new Vector2(160f, 40f), restoredButton.Size);
        Assert.Equal(Color.FromRgb(70, 70, 96), restoredButton.BackgroundColor);
        Assert.Single(restoredButton.Children);

        // Image：纹理路径重连。
        var restoredImage = Assert.IsType<Image>(restoredCanvas.Roots[2]);
        Assert.NotNull(restoredImage.Texture);
        Assert.Equal("tex/coin.png", resources.GetPath(restoredImage.Texture!));
        Assert.Equal(new Vector2(64f, 64f), restoredImage.Size);
    }

    private sealed class FakeGuidResolver : IAssetGuidResolver
    {
        private readonly Dictionary<string, Guid> _pathToGuid = new();
        private readonly Dictionary<Guid, string> _guidToPath = new();

        public Guid GetGuid(string path)
        {
            if (!_pathToGuid.TryGetValue(path, out var guid))
            {
                guid = Guid.NewGuid();
                _pathToGuid[path] = guid;
                _guidToPath[guid] = path;
            }
            return guid;
        }

        public string? GetPath(Guid guid) => _guidToPath.TryGetValue(guid, out var p) ? p : null;

        public void SetPath(Guid guid, string path) => _guidToPath[guid] = path;
    }

    private sealed class FakeTexture : ITexture
    {
        public int Width => 32;
        public int Height => 32;
    }

    private sealed class FakeTextureLoader : IResourceLoader<ITexture>
    {
        public ITexture Load(string path) => new FakeTexture();
    }
}
