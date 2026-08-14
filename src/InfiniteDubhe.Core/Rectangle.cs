namespace InfiniteDubhe.Core;

/// <summary>整数矩形（源矩形等）。POD 结构。</summary>
public readonly struct Rectangle
{
    public readonly int X;
    public readonly int Y;
    public readonly int Width;
    public readonly int Height;

    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int Right => X + Width;
    public int Bottom => Y + Height;

    /// <summary>是否为空（宽或高非正）。空矩形通常表示“使用整张纹理”。</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public static readonly Rectangle Empty = default;
}
