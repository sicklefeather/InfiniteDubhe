using nkast.Aether.Physics2D.Collision.Shapes;

namespace InfiniteDubhe.Physics;

/// <summary>圆形碰撞体。</summary>
public sealed class CircleCollider2D : Collider2D
{
    public float Radius { get; set; } = 0.5f;

    internal override Shape CreateShape() => new CircleShape(AetherMath.ToUnits(Radius), Density);
}
