using System.Numerics;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace InfiniteDubhe.Physics;

/// <summary>
/// System.Numerics（像素，用户面向）与 Aether.Physics2D（物理单位）之间的适配（设计 §4.1）。
/// 1 物理单位 = <see cref="PixelsPerUnit"/> 像素，以匹配 Box2D 的“米”级调参（MaxTranslation 等为编译期常量，无法运行时修改）。
/// </summary>
internal static class AetherMath
{
    /// <summary>像素 ↔ 物理单位换算。</summary>
    public const float PixelsPerUnit = 100f;

    public const float Deg2Rad = MathF.PI / 180f;
    public const float Rad2Deg = 180f / MathF.PI;

    /// <summary>像素 → 物理单位（向量）。</summary>
    public static AetherVector2 ToUnits(Vector2 px) => new(px.X / PixelsPerUnit, px.Y / PixelsPerUnit);

    /// <summary>物理单位 → 像素（向量）。</summary>
    public static Vector2 ToPixels(AetherVector2 units) => new(units.X * PixelsPerUnit, units.Y * PixelsPerUnit);

    /// <summary>像素 → 物理单位（标量）。</summary>
    public static float ToUnits(float px) => px / PixelsPerUnit;

    /// <summary>物理单位 → 像素（标量）。</summary>
    public static float ToPixels(float units) => units * PixelsPerUnit;
}
