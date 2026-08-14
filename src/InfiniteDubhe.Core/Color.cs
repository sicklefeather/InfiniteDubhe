namespace InfiniteDubhe.Core;

/// <summary>RGBA 颜色，float 分量（0–1）。POD 结构，避免装箱。</summary>
public readonly struct Color
{
    public readonly float R;
    public readonly float G;
    public readonly float B;
    public readonly float A;

    public Color(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static Color FromRgb(byte r, byte g, byte b, byte a = 255)
        => new(r / 255f, g / 255f, b / 255f, a / 255f);

    public static readonly Color Black = new(0f, 0f, 0f, 1f);
    public static readonly Color White = new(1f, 1f, 1f, 1f);
    public static readonly Color CornflowerBlue = FromRgb(100, 149, 237);
}
