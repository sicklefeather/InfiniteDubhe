using System.Numerics;

namespace InfiniteDubhe.Core;

/// <summary>
/// 调试绘制门面：在 Update 等回调中调用 <c>Draw*</c> 累积图元，
/// 每帧由渲染器在精灵之后绘制并清空（语义对齐 Unity 的 Debug.DrawLine / DrawRay / DrawCircle）。
/// </summary>
public static class Debug
{
    private struct Line
    {
        public Vector2 A;
        public Vector2 B;
        public Color Color;
        public float Thickness;
    }

    private static readonly List<Line> Lines = new();

    /// <summary>绘制线段（世界坐标，单位像素）。</summary>
    public static void DrawLine(Vector2 from, Vector2 to, Color color, float thickness = 1f)
        => Lines.Add(new Line { A = from, B = to, Color = color, Thickness = MathF.Max(0f, thickness) });

    /// <summary>从 <paramref name="origin"/> 沿 <paramref name="direction"/> 绘制长度为 <paramref name="length"/> 的射线。</summary>
    public static void DrawRay(Vector2 origin, Vector2 direction, Color color, float length = 1f, float thickness = 1f)
    {
        if (direction == Vector2.Zero) return;
        DrawLine(origin, origin + Vector2.Normalize(direction) * length, color, thickness);
    }

    /// <summary>绘制轴对齐矩形边框（<paramref name="center"/> 为中心，<paramref name="size"/> 为完整宽高）。</summary>
    public static void DrawRect(Vector2 center, Vector2 size, Color color, float thickness = 1f)
    {
        var half = size * 0.5f;
        var tl = new Vector2(center.X - half.X, center.Y - half.Y);
        var tr = new Vector2(center.X + half.X, center.Y - half.Y);
        var br = new Vector2(center.X + half.X, center.Y + half.Y);
        var bl = new Vector2(center.X - half.X, center.Y + half.Y);
        DrawLine(tl, tr, color, thickness);
        DrawLine(tr, br, color, thickness);
        DrawLine(br, bl, color, thickness);
        DrawLine(bl, tl, color, thickness);
    }

    /// <summary>绘制圆（多边形近似）。</summary>
    public static void DrawCircle(Vector2 center, float radius, Color color, int segments = 32, float thickness = 1f)
    {
        segments = Math.Max(3, segments);
        float step = MathF.Tau / segments;
        var prev = new Vector2(center.X + radius, center.Y);
        for (int i = 1; i <= segments; i++)
        {
            float angle = step * i;
            var next = new Vector2(center.X + MathF.Cos(angle) * radius, center.Y + MathF.Sin(angle) * radius);
            DrawLine(prev, next, color, thickness);
            prev = next;
        }
    }

    /// <summary>绘制点（小方块，边长 <paramref name="size"/>）。</summary>
    public static void DrawPoint(Vector2 position, Color color, float size = 2f)
    {
        float half = size * 0.5f;
        DrawLine(new Vector2(position.X - half, position.Y), new Vector2(position.X + half, position.Y), color, size);
    }

    /// <summary>把累积的线段转换为精灵绘制指令（渲染器在每帧精灵之后调用）。</summary>
    internal static void Flush(ICollection<SpriteDrawCommand> commands, ITexture whiteTexture)
    {
        foreach (var line in Lines)
            commands.Add(ToCommand(line, whiteTexture));
    }

    /// <summary>清空本帧累积的图元（渲染器在 Flush 后调用）。</summary>
    internal static void Clear() => Lines.Clear();

    private static SpriteDrawCommand ToCommand(in Line line, ITexture tex)
    {
        var delta = line.B - line.A;
        float length = delta.Length();
        return new SpriteDrawCommand
        {
            Texture = tex,
            SourceRect = new Rectangle(0, 0, 1, 1),
            Position = (line.A + line.B) * 0.5f,
            Rotation = MathF.Atan2(delta.Y, delta.X),
            Origin = new Vector2(length * 0.5f, line.Thickness * 0.5f),
            Scale = new Vector2(length, line.Thickness),
            Color = line.Color,
            Effects = SpriteEffects.None,
            Layer = int.MaxValue,
            LayerDepth = 0f,
        };
    }
}
