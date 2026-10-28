using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Scale feedback for a Button: hover, hold, release and click, tuned by one <see cref="SO_ButtonFeedback"/>.
/// Button.interactable (including a parent CanvasGroup) switches it off.
/// Scales are multipliers of the target's scale at Awake.
/// </summary>
public enum ButtonFeedbackStep { Hover, Hold, Release, Click }

[RequireComponent(typeof(Button))]
public class ButtonScaleFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] SO_ButtonFeedback preset;
    [Tooltip("Empty = this GameObject.")]
    [SerializeField] Transform target;

    /// <summary>Raised after each step plays, only while the button is interactable. Hook sound, VFX, etc. here.</summary>
    public event Action<ButtonFeedbackStep> Fired;

    public SO_ButtonFeedback Preset => preset;

    Button button;
    Vector3 baseScale;
    bool hovered, selected, wasInteractable;

    // Graphics under the button and their authored alpha, dimmed while not interactable.
    Graphic[] graphics;
    float[] baseAlphas;

    // Resting size: hover size while the pointer is on the button or it is selected, otherwise 1.
    float RestScale => (hovered || selected) ? preset.hover.scale : 1f;

    void Awake()
    {
        button = GetComponent<Button>();
        if (target == null) target = transform;
        baseScale = target.localScale;
        graphics = GetComponentsInChildren<Graphic>(true);
        baseAlphas = Array.ConvertAll(graphics, g => g.color.a);
        if (preset == null) Debug.LogWarning($"{name}: ButtonScaleFeedback has no preset.", this);
    }

    void OnEnable()
    {
        button.onClick.AddListener(OnClick);
        wasInteractable = button.IsInteractable();
        if (preset != null) Dim(!wasInteractable, 0f);
    }

    void OnDisable()
    {
        button.onClick.RemoveListener(OnClick);
        ResetState();
    }

    void Update()
    {
        if (preset == null) return;
        bool now = button.IsInteractable();
        if (wasInteractable && !now)
        {
            // Disabled mid-hover (HubManager fade, interactable = false): go back to base.
            hovered = selected = false;
            To(1f, preset.release);
        }
        if (wasInteractable != now) Dim(!now, preset.disabledFadeDuration);
        wasInteractable = now;
    }

    // Fades every graphic to disabledAlpha of its own alpha (or back). Only alpha is tweened,
    // so colour set by other scripts (e.g. the AutoPlay on/off tint) is kept.
    void Dim(bool dim, float duration)
    {
        for (int i = 0; i < graphics.Length; i++)
        {
            var g = graphics[i];
            if (g == null) continue;
            g.DOKill();
            float a = baseAlphas[i] * (dim ? preset.disabledAlpha : 1f);
            if (duration <= 0f) { var c = g.color; c.a = a; g.color = c; }
            else g.DOFade(a, duration).SetUpdate(preset.useUnscaledTime);
        }
    }

    public void OnPointerEnter(PointerEventData e) { hovered = true; if (Active) { To(preset.hover.scale, preset.hover); Fire(ButtonFeedbackStep.Hover); } }
    public void OnPointerExit(PointerEventData e) { hovered = false; if (Active) To(RestScale, preset.release); }
    public void OnPointerDown(PointerEventData e) { if (Active) { To(preset.hold.scale, preset.hold); Fire(ButtonFeedbackStep.Hold); } }
    public void OnPointerUp(PointerEventData e) { if (Active) { To(RestScale, preset.release); Fire(ButtonFeedbackStep.Release); } }

    // A mouse click also selects the button; only count selection that did not come from the pointer.
    public void OnSelect(BaseEventData e) { selected = !hovered; if (selected && Active) { To(preset.hover.scale, preset.hover); Fire(ButtonFeedbackStep.Hover); } }
    public void OnDeselect(BaseEventData e) { if (!selected) return; selected = false; if (Active) To(RestScale, preset.release); }

    void OnClick()
    {
        if (!Active) return;
        target.DOKill();
        DOTween.Sequence().SetTarget(target).SetUpdate(preset.useUnscaledTime)
            .Append(Step(preset.click.scale, preset.click))
            .Append(Step(RestScale, preset.release));
        Fire(ButtonFeedbackStep.Click);
    }

    void Fire(ButtonFeedbackStep step) => Fired?.Invoke(step);

    bool Active => preset != null && button.IsInteractable();

    void To(float multiplier, ScaleStep step)
    {
        target.DOKill();
        Step(multiplier, step).SetUpdate(preset.useUnscaledTime);
    }

    Tweener Step(float multiplier, ScaleStep step) =>
        target.DOScale(baseScale * multiplier, step.duration).SetEase(step.ease, step.overshoot);

    void ResetState()
    {
        hovered = selected = false;
        target.DOKill();
        target.localScale = baseScale;
    }
}
