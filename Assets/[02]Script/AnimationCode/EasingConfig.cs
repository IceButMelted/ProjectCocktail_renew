using UnityEngine;

/// <summary>
/// Pairs an <see cref="EasingMode"/> with a total slide duration.
/// Use the static factory methods for readable call sites.
/// </summary>
public sealed class EasingConfig
{
    public readonly EasingMode Mode;
    public readonly float Duration;      // seconds; clamped > 0
    public readonly AnimationCurve Curve; // only read when Mode == Custom

    public EasingConfig(EasingMode mode, float duration, AnimationCurve curve = null)
    {
        Mode = mode;
        Duration = Mathf.Max(duration, 0.001f);
        Curve = curve;
    }

    public static EasingConfig Linear(float d)    => new(EasingMode.Linear, d);
    public static EasingConfig EaseIn(float d)    => new(EasingMode.EaseIn, d);
    public static EasingConfig EaseOut(float d)   => new(EasingMode.EaseOut, d);
    public static EasingConfig EaseInOut(float d) => new(EasingMode.EaseInOut, d);
    public static EasingConfig Bounce(float d)    => new(EasingMode.Bounce, d);
    public static EasingConfig OverShoot(float d) => new(EasingMode.OverShoot, d);

    /// <param name="curve">
    ///   X = normalised time [0..1].
    ///   Y = normalised position — may exceed [0..1] for overshoot effects.
    /// </param>
    public static EasingConfig FromCurve(AnimationCurve curve, float d)
        => new(EasingMode.Custom, d, curve);
}
