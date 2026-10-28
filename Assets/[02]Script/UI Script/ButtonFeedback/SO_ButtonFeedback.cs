using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public struct ScaleStep
{
    public float scale;      // multiplier of the button's base scale
    public float duration;   // seconds
    public Ease ease;
    public float overshoot;  // used only by Back eases
}

/// <summary>
/// Shared tuning for <see cref="ButtonScaleFeedback"/>. Never written at runtime.
/// Make another asset when one button needs different feel.
/// </summary>
[CreateAssetMenu(menuName = "Bar410/UI/Button Feedback")]
public class SO_ButtonFeedback : ScriptableObject
{
    [Tooltip("Pointer enters / controller selects.")]
    public ScaleStep hover = new ScaleStep { scale = 1.08f, duration = 0.18f, ease = Ease.OutBack, overshoot = 1.2f };

    [Tooltip("Pointer held down.")]
    public ScaleStep hold = new ScaleStep { scale = 0.94f, duration = 0.08f, ease = Ease.OutQuad };

    [Tooltip("Back to the resting size (also used on exit). Its scale is not used.")]
    public ScaleStep release = new ScaleStep { scale = 1f, duration = 0.15f, ease = Ease.OutBack, overshoot = 1.5f };

    [Tooltip("Punch on click, then release back to the resting size.")]
    public ScaleStep click = new ScaleStep { scale = 1.12f, duration = 0.08f, ease = Ease.OutQuad };

    [Tooltip("Keep animating while Time.timeScale is 0 (Pause).")]
    public bool useUnscaledTime = true;

    // Played by ButtonFeedbackSound. Empty = silent.
    // A normal click fires Release then Click in the same frame, so leave releaseSound empty
    // unless both should be heard.
    [Header("Sound (UiSFX)")]
    [SoundId(SoundChannel.UiSFX)] public string hoverSound;
    [SoundId(SoundChannel.UiSFX)] public string holdSound;
    [SoundId(SoundChannel.UiSFX)] public string releaseSound;
    [SoundId(SoundChannel.UiSFX)] public string clickSound;

    public string SoundFor(ButtonFeedbackStep step) => step switch
    {
        ButtonFeedbackStep.Hover => hoverSound,
        ButtonFeedbackStep.Hold => holdSound,
        ButtonFeedbackStep.Release => releaseSound,
        _ => clickSound,
    };
}
