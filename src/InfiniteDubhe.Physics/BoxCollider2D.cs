using System.Numerics;
using nkast.Aether.Physics2D.Collision.Shapes;
using PolygonTools = nkast.Aether.Physics2D.Common.PolygonTools;

namespace InfiniteDubhe.Physics;

/// <summary>盒形碰撞体：中心对齐，<see cref="Size"/> 为完整宽高。</summary>
public sealed class BoxCollider2D : Collider2D
{
    public Vector2 Size { get; set; } = Vector2.One;

    internal override Shape CreateShape()
        => new PolygonShape(
            PolygonTools.CreateRectangle(AetherMath.ToUnits(Size.X * 0.5f), AetherMath.ToUnits(Size.Y * 0.5f)),
            Density);
}
