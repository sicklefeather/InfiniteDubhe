namespace InfiniteDubhe.Core;

/// <summary>缓动函数类型。</summary>
public enum Ease
{
    Linear,
    InQuad, OutQuad, InOutQuad,
    InCubic, OutCubic, InOutCubic,
    InSine, OutSine, InOutSine,
    InExpo, OutExpo, InOutExpo,
    InBack, OutBack, InOutBack,
    InBounce, OutBounce, InOutBounce,
    InElastic, OutElastic, InOutElastic,
}

/// <summary>缓动函数求值（公式取自 easings.net）。输入 <paramref name="t"/> 会被夹到 [0,1]。</summary>
public static class Easing
{
    public static float Apply(Ease ease, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return ease switch
        {
            Ease.InQuad => t * t,
            Ease.OutQuad => t * (2f - t),
            Ease.InOutQuad => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t,
            Ease.InCubic => t * t * t,
            Ease.OutCubic => 1f - MathF.Pow(1f - t, 3f),
            Ease.InOutCubic => t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f,
            Ease.InSine => 1f - MathF.Cos(t * MathF.PI / 2f),
            Ease.OutSine => MathF.Sin(t * MathF.PI / 2f),
            Ease.InOutSine => -(MathF.Cos(MathF.PI * t) - 1f) / 2f,
            Ease.InExpo => t == 0f ? 0f : MathF.Pow(2f, 10f * t - 10f),
            Ease.OutExpo => t == 1f ? 1f : 1f - MathF.Pow(2f, -10f * t),
            Ease.InOutExpo => t == 0f ? 0f : t == 1f ? 1f
                : t < 0.5f ? MathF.Pow(2f, 20f * t - 10f) / 2f : (2f - MathF.Pow(2f, -20f * t + 10f)) / 2f,
            Ease.InBack => 2.70158f * t * t * t - 1.70158f * t * t,
            Ease.OutBack => 1f + 2.70158f * MathF.Pow(t - 1f, 3f) + 1.70158f * MathF.Pow(t - 1f, 2f),
            Ease.InOutBack => t < 0.5f
                ? MathF.Pow(2f * t, 2f) * ((2.5949095f + 1f) * 2f * t - 2.5949095f) / 2f
                : (MathF.Pow(2f * t - 2f, 2f) * ((2.5949095f + 1f) * (t * 2f - 2f) + 2.5949095f) + 2f) / 2f,
            Ease.OutBounce => OutBounce(t),
            Ease.InBounce => 1f - OutBounce(1f - t),
            Ease.InOutBounce => t < 0.5f ? (1f - OutBounce(1f - 2f * t)) / 2f : (1f + OutBounce(2f * t - 1f)) / 2f,
            Ease.OutElastic => t == 0f ? 0f : t == 1f ? 1f
                : MathF.Pow(2f, -10f * t) * MathF.Sin((t * 10f - 0.75f) * 2.0943951f) + 1f,
            Ease.InElastic => t == 0f ? 0f : t == 1f ? 1f
                : -(MathF.Pow(2f, 10f * t - 10f) * MathF.Sin((t * 10f - 10.75f) * 2.0943951f)),
            Ease.InOutElastic => t == 0f ? 0f : t == 1f ? 1f : t < 0.5f
                ? -(MathF.Pow(2f, 20f * t - 10f) * MathF.Sin((20f * t - 11.125f) * 1.3962634f)) / 2f
                : MathF.Pow(2f, -20f * t + 10f) * MathF.Sin((20f * t - 11.125f) * 1.3962634f) / 2f + 1f,
            _ => t,
        };
    }

    private static float OutBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        if (t < 1f / d1) return n1 * t * t;
        if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
        if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
        t -= 2.625f / d1;
        return n1 * t * t + 0.984375f;
    }
}
