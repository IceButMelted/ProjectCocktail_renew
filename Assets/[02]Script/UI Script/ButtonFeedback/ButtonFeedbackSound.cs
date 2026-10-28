using UnityEngine;

/// <summary>
/// Plays the UiSFX set in the sibling <see cref="ButtonScaleFeedback"/>'s preset for each step.
/// Other reactions (VFX, haptics) can listen to <see cref="ButtonScaleFeedback.Fired"/> the same way.
/// </summary>
[RequireComponent(typeof(ButtonScaleFeedback))]
public class ButtonFeedbackSound : MonoBehaviour
{
    ButtonScaleFeedback feedback;

    void Awake() => feedback = GetComponent<ButtonScaleFeedback>();
    void OnEnable() => feedback.Fired += Play;
    void OnDisable() => feedback.Fired -= Play;

    void Play(ButtonFeedbackStep step)
    {
        string id = feedback.Preset != null ? feedback.Preset.SoundFor(step) : null;
        if (!string.IsNullOrEmpty(id)) SoundManager.Instance?.PlayUiSFX(id);
    }
}
