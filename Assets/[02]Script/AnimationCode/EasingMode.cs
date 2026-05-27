/// <summary>Which interpolation curve to apply during a timed slide.</summary>
public enum EasingMode
{
    Linear,
    EaseIn,       // slow start, fast end  (cubic)
    EaseOut,      // fast start, slow end  (cubic)
    EaseInOut,    // slow start AND end    (cubic)
    Bounce,       // ease-out bounce — overshoots [0,1] briefly
    OverShoot,    // slides past target then settles — overshoots [0,1] briefly
    Custom,       // driven by an AnimationCurve
}
