using System.Numerics;

namespace InfiniteDubhe.Physics;

/// <summary>物理静态门面：把调用转发给当前场景的活动 <see cref="PhysicsWorld2D"/>。</summary>
public static class Physics2D
{
    /// <summary>当前活动物理世界（由 <see cref="PhysicsWorld2D"/> 在 Awake/OnEnable 时登记）。</summary>
    internal static PhysicsWorld2D? ActiveWorld { get; set; }

    /// <summary>对活动世界发射射线，返回最近命中（无世界或无命中时返回 null）。</summary>
    public static RaycastHit2D? Raycast(Vector2 from, Vector2 to)
        => ActiveWorld?.Raycast(from, to);

    /// <summary>收集活动世界中覆盖 <paramref name="point"/> 的碰撞体，返回新增数量。</summary>
    public static int OverlapPoint(Vector2 point, ICollection<Collider2D> results)
        => ActiveWorld?.OverlapPoint(point, results) ?? 0;
}
