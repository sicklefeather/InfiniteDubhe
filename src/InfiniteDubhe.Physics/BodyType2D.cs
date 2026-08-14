namespace InfiniteDubhe.Physics;

/// <summary>刚体类型（映射到 Aether.Physics2D 的 BodyType）。</summary>
public enum BodyType2D
{
    /// <summary>静态：不受力、不可移动（如地面/墙壁）。</summary>
    Static,

    /// <summary>运动学：由代码（Transform）驱动，不受物理力影响，但会推开动态刚体。</summary>
    Kinematic,

    /// <summary>动态：完全受物理模拟（重力/碰撞/力）。</summary>
    Dynamic,
}
