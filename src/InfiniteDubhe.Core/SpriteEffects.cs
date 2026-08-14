namespace InfiniteDubhe.Core;

/// <summary>精灵翻转方式。</summary>
[Flags]
public enum SpriteEffects
{
    None = 0,
    FlipHorizontally = 1 << 0,
    FlipVertically = 1 << 1,
}
