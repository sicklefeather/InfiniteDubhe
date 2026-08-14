using System.Numerics;
using InfiniteDubhe.Core;
using InfiniteDubhe.Scene;
using nkast.Aether.Physics2D.Collision;
using nkast.Aether.Physics2D.Dynamics;

namespace InfiniteDubhe.Physics;

/// <summary>
/// 2D 物理世界：驱动一个 Aether.Physics2D 世界并管理挂载的 <see cref="Rigidbody2D"/>。
/// 每场景挂一个；Awake 时登记为 <see cref="Physics2D.ActiveWorld"/>。
/// </summary>
public sealed class PhysicsWorld2D : Component
{
    private readonly World _world = new(AetherMath.ToUnits(Vector2.Zero));
    private readonly List<Rigidbody2D> _bodies = new();

    internal World AetherWorld => _world;

    /// <summary>重力（像素/秒²，+y 向下为屏幕坐标）。</summary>
    public Vector2 Gravity
    {
        get => AetherMath.ToPixels(_world.Gravity);
        set => _world.Gravity = AetherMath.ToUnits(value);
    }

    protected override void Awake() => Physics2D.ActiveWorld = this;

    protected override void OnEnable() => Physics2D.ActiveWorld = this;

    protected override void FixedUpdate()
    {
        // 静态/运动学刚体：Transform → Body；步进；动态刚体：Body → Transform。
        foreach (var rb in _bodies)
            if (rb.Type != BodyType2D.Dynamic) rb.SyncTransformToBody();

        _world.Step(Time.FixedDeltaTime);

        foreach (var rb in _bodies)
            if (rb.Type == BodyType2D.Dynamic) rb.SyncBodyToTransform();

        foreach (var rb in _bodies)
            rb.NotifyStay();
    }

    protected override void OnDisable()
    {
        if (ReferenceEquals(Physics2D.ActiveWorld, this)) Physics2D.ActiveWorld = null;
    }

    protected override void OnDestroy()
    {
        foreach (var rb in _bodies.ToArray()) rb.DetachFromWorld();
        _bodies.Clear();
        _world.Clear();
        if (ReferenceEquals(Physics2D.ActiveWorld, this)) Physics2D.ActiveWorld = null;
    }

    internal void Register(Rigidbody2D rb)
    {
        if (_bodies.Contains(rb)) return;
        _bodies.Add(rb);
        rb.Attach(this);
    }

    internal void Unregister(Rigidbody2D rb)
    {
        if (_bodies.Remove(rb)) rb.DetachFromWorld();
    }

    /// <summary>从 <paramref name="from"/> 到 <paramref name="to"/> 发射射线，返回最近命中（无则 null）。</summary>
    public RaycastHit2D? Raycast(Vector2 from, Vector2 to)
    {
        RaycastHit2D? result = null;
        float best = float.MaxValue;

        _world.RayCast((fixture, point, normal, fraction) =>
        {
            if (fixture.Tag is Collider2D collider && fraction < best)
            {
                best = fraction;
                result = new RaycastHit2D(
                    AetherMath.ToPixels(point),
                    new Vector2(normal.X, normal.Y), // 法线为单位向量，不做单位换算
                    fraction,
                    collider);
            }
            return fraction;
        }, AetherMath.ToUnits(from), AetherMath.ToUnits(to));

        return result;
    }

    /// <summary>收集覆盖 <paramref name="point"/> 的所有碰撞体，返回新增数量。</summary>
    public int OverlapPoint(Vector2 point, ICollection<Collider2D> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        int before = results.Count;

        var p = AetherMath.ToUnits(point);
        var aabb = new AABB(
            new nkast.Aether.Physics2D.Common.Vector2(p.X - 0.001f, p.Y - 0.001f),
            new nkast.Aether.Physics2D.Common.Vector2(p.X + 0.001f, p.Y + 0.001f));

        _world.QueryAABB(fixture =>
        {
            if (fixture.Tag is Collider2D collider && fixture.TestPoint(ref p))
                results.Add(collider);
            return true;
        }, aabb);

        return results.Count - before;
    }
}
