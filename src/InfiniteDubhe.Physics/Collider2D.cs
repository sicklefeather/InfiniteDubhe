using InfiniteDubhe.Scene;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;

namespace InfiniteDubhe.Physics;

/// <summary>
/// 碰撞体基类：描述形状参数。挂到含 <see cref="Rigidbody2D"/> 的 GameObject 上，
/// 由刚体在注册时据此创建物理 Fixture。
/// </summary>
public abstract class Collider2D : Component
{
    /// <summary>密度（影响质量）。</summary>
    public float Density { get; set; } = 1f;

    /// <summary>摩擦系数。</summary>
    public float Friction { get; set; } = 0.2f;

    /// <summary>弹性系数（0~1）。</summary>
    public float Restitution { get; set; }

    /// <summary>是否为触发器（只检测碰撞、不产生物理响应）。</summary>
    public bool IsSensor { get; set; }

    /// <summary>对应的物理 Fixture（由 <see cref="Rigidbody2D"/> 挂载后填充）。</summary>
    internal Fixture? Fixture { get; set; }

    /// <summary>创建底层 Aether 形状。</summary>
    internal abstract Shape CreateShape();
}
