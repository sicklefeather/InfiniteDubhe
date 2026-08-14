using System.Numerics;

namespace InfiniteDubhe.Rendering;

/// <summary>2D 正交相机。支持平移/缩放/旋转。角度单位：度。</summary>
public sealed class Camera
{
    /// <summary>世界坐标中位于屏幕中心的点。</summary>
    public Vector2 Position { get; set; }

    public float Zoom { get; set; } = 1f;

    public float RotationDeg { get; set; }

    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }

    public Camera(int width, int height)
    {
        ViewportWidth = width;
        ViewportHeight = height;
    }

    /// <summary>世界 → 裁剪空间 的视图矩阵（正交投影 + 相机变换）。</summary>
    public Matrix3x2 ViewMatrix
    {
        get
        {
            var rotation = -RotationDeg * MathF.PI / 180f;
            var halfW = ViewportWidth * 0.5f;
            var halfH = ViewportHeight * 0.5f;
            return
                Matrix3x2.CreateTranslation(-Position) *
                Matrix3x2.CreateRotation(rotation) *
                Matrix3x2.CreateScale(Zoom, Zoom) *
                Matrix3x2.CreateTranslation(halfW, halfH) *
                Matrix3x2.CreateScale(2f / ViewportWidth, -2f / ViewportHeight) *
                Matrix3x2.CreateTranslation(-1f, 1f);
        }
    }
}
