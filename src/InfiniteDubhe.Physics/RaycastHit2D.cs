using System.Numerics;

namespace InfiniteDubhe.Physics;

/// <summary>射线检测命中结果。</summary>
public readonly struct RaycastHit2D
{
    /// <summary>命中点（世界坐标）。</summary>
    public Vector2 Point { get; }

    /// <summary>命中面法线。</summary>
    public Vector2 Normal { get; }

    /// <summary>命中比例（0=起点，1=终点）。</summary>
    public float Fraction { get; }

    /// <summary>命中的碰撞体。</summary>
    public Collider2D Collider { get; }

    public RaycastHit2D(Vector2 point, Vector2 normal, float fraction, Collider2D collider)
    {
        Point = point;
        Normal = normal;
        Fraction = fraction;
        Collider = collider;
    }
}
