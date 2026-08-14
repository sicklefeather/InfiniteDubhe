namespace InfiniteDubhe.Physics;

/// <summary>碰撞回调数据：描述与另一个碰撞体的接触。</summary>
public readonly struct Collision2D
{
    /// <summary>对方碰撞体。</summary>
    public Collider2D Other { get; }

    /// <summary>对方刚体（若对方 GameObject 上有 <see cref="Rigidbody2D"/>）。</summary>
    public Rigidbody2D? OtherRigidbody => Other.GameObject.GetComponent<Rigidbody2D>();

    public Collision2D(Collider2D other) => Other = other;
}
