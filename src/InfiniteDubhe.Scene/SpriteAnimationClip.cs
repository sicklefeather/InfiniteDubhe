using InfiniteDubhe.Core;

namespace InfiniteDubhe.Scene;

/// <summary>帧动画单帧：源矩形 + 显示时长（秒）。</summary>
public readonly struct SpriteAnimationFrame
{
    public Rectangle SourceRect { get; }
    public float DurationSeconds { get; }

    public SpriteAnimationFrame(Rectangle sourceRect, float durationSeconds)
    {
        if (durationSeconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "帧时长必须为正数。");
        SourceRect = sourceRect;
        DurationSeconds = durationSeconds;
    }
}

/// <summary>帧动画片段（精灵序列）：一组按顺序播放的帧。</summary>
public sealed class SpriteAnimationClip
{
    public string Name { get; }
    public IReadOnlyList<SpriteAnimationFrame> Frames { get; }
    public bool Loop { get; }

    public SpriteAnimationClip(string name, IReadOnlyList<SpriteAnimationFrame> frames, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(frames);
        Name = name;
        Frames = frames;
        Loop = loop;
    }

    /// <summary>从水平排列的等宽帧构建片段（Sprite Sheet 单行）。</summary>
    public static SpriteAnimationClip FromRow(string name, Rectangle firstFrame, int frameCount, int framesPerSecond, bool loop = true)
        => FromGrid(name, firstFrame, frameCount, 1, frameCount, framesPerSecond, loop);

    /// <summary>从等宽网格（<paramref name="columns"/> × <paramref name="rows"/>）构建片段，取前 <paramref name="frameCount"/> 帧。</summary>
    public static SpriteAnimationClip FromGrid(string name, Rectangle firstFrame, int columns, int rows, int frameCount, int framesPerSecond, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (frameCount <= 0) throw new ArgumentOutOfRangeException(nameof(frameCount));
        if (framesPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(framesPerSecond));

        float duration = 1f / framesPerSecond;
        var frames = new List<SpriteAnimationFrame>(frameCount);
        for (int i = 0; i < frameCount; i++)
        {
            int col = i % columns;
            int row = i / columns;
            frames.Add(new SpriteAnimationFrame(
                new Rectangle(firstFrame.X + col * firstFrame.Width, firstFrame.Y + row * firstFrame.Height, firstFrame.Width, firstFrame.Height),
                duration));
        }
        return new SpriteAnimationClip(name, frames, loop);
    }
}
