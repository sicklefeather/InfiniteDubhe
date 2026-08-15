using System.Numerics;
using InfiniteDubhe.Core;
using InfiniteDubhe.Physics;
using InfiniteDubhe.Scene;
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

        // 往返。
        var json = serializer.Serialize(scene);
        var restored = serializer.Deserialize(json);

        // 场景名 + 根对象。
        Assert.Equal(scene.Name, restored.Name);
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
