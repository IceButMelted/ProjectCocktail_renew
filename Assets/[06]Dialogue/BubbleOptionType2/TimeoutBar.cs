using System.Threading;
using UnityEngine;
using Yarn.Unity;

public class TimeoutBar : MonoBehaviour
{
    [SerializeField] private RectTransform bar;

    // Set this in the Inspector as a fallback,
    // OR let it be captured at first use.
    [Tooltip("Full width of the bar in pixels. " +
             "Set this manually if auto-capture fails.")]
    [SerializeField] private float fullWidth = 0f;

    private void Awake()
    {
        // Awake runs even on inactive objects when first activated,
        // but the layout may still not be ready — so we only
        // treat this as a hint, not the authoritative value.
        TryCaptureWidth();
    }

    private void OnEnable()
    {
        // OnEnable fires after the GameObject is activated and
        // after the first layout pass — safest place to read sizeDelta.
        TryCaptureWidth();
        ResetBar();   // ← moved here so it always resets to the real width
    }

    private void TryCaptureWidth()
    {
        if (bar == null) return;
        float w = bar.sizeDelta.x;
        // Only update if we got a real value
        if (w > 0f) fullWidth = w;
    }

    /// <summary>Resets the bar to full width instantly.</summary>
    public void ResetBar()
    {
        if (bar == null || fullWidth <= 0f) return;
        bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullWidth);
    }

    /// <summary>Shrinks the bar to zero over <paramref name="duration"/> seconds.</summary>
    public async YarnTask Shrink(float duration, CancellationToken cancellationToken)
    {
        if (bar == null) return;

        float startWidth = bar.sizeDelta.x;
        float accumulator = 0f;

        while (accumulator < duration && !cancellationToken.IsCancellationRequested)
        {
            accumulator += Time.deltaTime;
            float t = Mathf.Clamp01(accumulator / duration);
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                                          Mathf.Lerp(startWidth, 0f, t));
            await YarnTask.Yield();
        }

        bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
    }
}