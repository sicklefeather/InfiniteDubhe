using System.Numerics;
using InfiniteDubhe.Core;
using InfiniteDubhe.Scene;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;

namespace InfiniteDubhe.Physics;

/// <summary>
/// 2D 刚体：为所属 GameObject 提供物理模拟。需与 <see cref="Collider2D"/> 搭配，
/// 并挂到存在 <see cref="PhysicsWorld2D"/> 的场景中（活动世界由该组件在 Awake 时登记）。
/// </summary>
public sealed class Rigidbody2D : Component
{
    private Body? _body;
    private PhysicsWorld2D? _world;
    private readonly HashSet<Collider2D> _contacts = new();

    /// <summary>刚体类型。</summary>
    public BodyType2D Type { get; set; } = BodyType2D.Dynamic;

    /// <summary>线性阻尼（0=无）。</summary>
    public float LinearDamping { get; set; }

    /// <summary>角阻尼（0=无）。</summary>
    public float AngularDamping { get; set; }

    /// <summary>锁定旋转（true 时刚体不因碰撞/扭矩而转动）。</summary>
    public bool FixedRotation { get; set; }

    /// <summary>忽略重力。</summary>
    public bool IgnoreGravity { get; set; }

    /// <summary>进入碰撞（首次接触）。</summary>
    public event Action<Collision2D>? CollisionEnter;

    /// <summary>持续碰撞（接触期间每个物理步）。</summary>
    public event Action<Collision2D>? CollisionStay;

    /// <summary>离开碰撞（接触结束）。</summary>
    public event Action<Collision2D>? CollisionExit;

    /// <summary>线速度（像素/秒）。</summary>
    public Vector2 Velocity
    {
        get => _body is null ? Vector2.Zero : AetherMath.ToPixels(_body.LinearVelocity);
        set { if (_body is not null) _body.LinearVelocity = AetherMath.ToUnits(value); }
    }

    /// <summary>角速度（度/秒）。</summary>
    public float AngularVelocity
    {
        get => _body is null ? 0f : _body.AngularVelocity * AetherMath.Rad2Deg;
        set { if (_body is not null) _body.AngularVelocity = value * AetherMath.Deg2Rad; }
    }

    /// <summary>质量（由碰撞体形状与密度决定，只读）。</summary>
    public float Mass => _body?.Mass ?? 0f;

    /// <summary>施加持续力（像素/秒² 方向）。</summary>
    public void AddForce(Vector2 force)
    {
        if (_body is not null) _body.ApplyForce(AetherMath.ToUnits(force));
    }

    /// <summary>施加瞬时冲量（直接改变动量，单位像素/秒）。</summary>
    public void AddImpulse(Vector2 impulse)
    {
        if (_body is not null) _body.ApplyLinearImpulse(AetherMath.ToUnits(impulse));
    }

    /// <summary>施加扭矩。</summary>
    public void AddTorque(float torque)
    {
        if (_body is not null) _body.ApplyTorque(torque / (AetherMath.PixelsPerUnit * AetherMath.PixelsPerUnit));
    }

    protected override void OnEnable()
        => Physics2D.ActiveWorld?.Register(this);

    protected override void OnDisable()
        => _world?.Unregister(this);

    protected override void OnDestroy()
        => _world?.Unregister(this);

    /// <summary>把刚体挂到指定物理世界（幂等）。读取当前 Transform 创建底层 Body 与所有碰撞体 Fixture。</summary>
    internal void Attach(PhysicsWorld2D world)
    {
        if (_body is not null) return;
        _world = world;

        // +y 向下（屏幕坐标），故旋转角度取反以保持屏幕上的旋转方向一致。
        _body = world.AetherWorld.CreateBody(
            AetherMath.ToUnits(Transform.WorldPosition),
            -Transform.RotationDeg * AetherMath.Deg2Rad,
            ToAetherType(Type));

        _body.LinearDamping = LinearDamping;
        _body.AngularDamping = AngularDamping;
        _body.FixedRotation = FixedRotation;
        _body.IgnoreGravity = IgnoreGravity;

        foreach (var collider in GameObject.GetComponents().OfType<Collider2D>())
            AttachCollider(collider);
    }

    /// <summary>从物理世界卸载刚体并销毁底层 Body。</summary>
    internal void DetachFromWorld()
    {
        if (_body is null) return;

        _world?.AetherWorld.Remove(_body);
        foreach (var collider in GameObject.GetComponents().OfType<Collider2D>())
            collider.Fixture = null;

        _body = null;
        _world = null;
        _contacts.Clear();
    }

    /// <summary>把 Transform 同步到底层 Body（静态/运动学刚体每步调用）。</summary>
    internal void SyncTransformToBody()
    {
        if (_body is null) return;
        _body.SetTransform(
            AetherMath.ToUnits(Transform.WorldPosition),
            -Transform.RotationDeg * AetherMath.Deg2Rad);
    }

    /// <summary>把底层 Body 同步回 Transform（动态刚体每步调用）。</summary>
    internal void SyncBodyToTransform()
    {
        if (_body is null) return;

        var worldPosition = AetherMath.ToPixels(_body.Position);
        if (Transform.Parent is null)
        {
            Transform.Position = worldPosition;
        }
        else if (Matrix3x2.Invert(Transform.Parent.LocalToWorld, out var parentInv))
        {
            Transform.Position = Vector2.Transform(worldPosition, parentInv);
        }

        Transform.RotationDeg = -_body.Rotation * AetherMath.Rad2Deg;
    }

    private void AttachCollider(Collider2D collider)
    {
        var fixture = _body!.CreateFixture(collider.CreateShape());
        fixture.Friction = collider.Friction;
        fixture.Restitution = collider.Restitution;
        fixture.IsSensor = collider.IsSensor;
        fixture.Tag = collider;
        fixture.OnCollision = OnCollision;
        fixture.OnSeparation = OnSeparation;
        collider.Fixture = fixture;
    }

    private bool OnCollision(Fixture sender, Fixture other, Contact contact)
    {
        if (other.Tag is Collider2D collider && _contacts.Add(collider))
            CollisionEnter?.Invoke(new Collision2D(collider));
        return true;
    }

    private void OnSeparation(Fixture sender, Fixture other, Contact contact)
    {
        if (other.Tag is Collider2D collider && _contacts.Remove(collider))
            CollisionExit?.Invoke(new Collision2D(collider));
    }

    /// <summary>每物理步对当前所有接触触发 <see cref="CollisionStay"/>（由物理世界在 Step 后调用）。</summary>
    internal void NotifyStay()
    {
        if (_contacts.Count == 0) return;
        foreach (var collider in _contacts)
            CollisionStay?.Invoke(new Collision2D(collider));
    }

    private static BodyType ToAetherType(BodyType2D type) => type switch
    {
        BodyType2D.Static => BodyType.Static,
        BodyType2D.Kinematic => BodyType.Kinematic,
        _ => BodyType.Dynamic,
    };
}
