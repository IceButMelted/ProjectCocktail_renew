using UnityEngine;

/// <summary>
/// Pure stateless easing math.
/// Separated from PanelSlider so curves can be reused by other systems.
/// </summary>
public static class EasingMath
{
    public static float Evaluate(EasingConfig cfg, float t) => cfg.Mode switch
    {
        EasingMode.Linear    => t,
        EasingMode.EaseIn    => t * t * t,
        EasingMode.EaseOut   => 1f - Mathf.Pow(1f - t, 3f),
        EasingMode.EaseInOut => t < 0.5f
                                    ? 4f * t * t * t
                                    : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f,
        EasingMode.Bounce    => EaseBounce(t),
        EasingMode.OverShoot => EaseOverShoot(t),
        EasingMode.Custom    => cfg.Curve != null ? cfg.Curve.Evaluate(t) : t,
        _                    => t,
    };

    // Ease-out bounce — 3 diminishing bounces before settling.
    // Return value briefly exceeds 1.0 → caller must use LerpUnclamped.
    private static float EaseBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (t < 1f / d1)   { return n1 * t * t; }
        if (t < 2f / d1)   { t -= 1.5f   / d1; return n1 * t * t + 0.75f; }
        if (t < 2.5f / d1) { t -= 2.25f  / d1; return n1 * t * t + 0.9375f; }
        t -= 2.625f / d1;    return n1 * t * t + 0.984375f;
    }

    // Ease-out overshoot — slides past target then settles.
    // Return value exceeds 1.0 during overshoot → caller must use LerpUnclamped.
    private static float EaseOverShoot(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
