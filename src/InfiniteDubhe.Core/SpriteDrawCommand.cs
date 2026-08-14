using System.Numerics;

namespace InfiniteDubhe.Core;

/// <summary>单条精灵绘制指令（POD，供批处理）。</summary>
public struct SpriteDrawCommand
{
    public ITexture? Texture;
    public Rectangle SourceRect;
    public Vector2 Position;
    public float Rotation;      // 弧度（内部热路径）
    public Vector2 Origin;
    public Vector2 Scale;
    public Color Color;
    public SpriteEffects Effects;
    public int Layer;           // 排序层
    public float LayerDepth;    // 同层内深度
}
