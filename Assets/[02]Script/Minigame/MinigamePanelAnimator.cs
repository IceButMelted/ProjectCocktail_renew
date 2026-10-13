using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Thin wrapper around the Animator(s) that slide the minigame panels on and off screen.
///
/// The panels used by one minigame live under two roots:
///   • the shared stage (InitPanel / ArtWorks / Background), and
///   • that minigame's own panel.
/// Both are driven by the same two triggers, so this component just fans the
/// trigger out to every Animator in <see cref="_animators"/>.
///
/// Completion is reported by an AnimationEvent at the end of the clip
/// (<see cref="OnIntroFinished"/> / <see cref="OnOutroFinished"/>) so a designer can
/// move the event earlier to hand control back to gameplay sooner.
/// <see cref="_introFallbackDuration"/> / <see cref="_outroFallbackDuration"/> exist only
/// so a clip that is missing its event can never strand the FSM.
/// </summary>
public class MinigamePanelAnimator : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────

    [Tooltip("Every Animator that participates in this minigame's intro/outro. " +
             "Typically: the shared stage root plus this minigame's own panel root.")]
    [SerializeField] private Animator[] _animators;

    [Header("Trigger Names")]
    [SerializeField] private string _showTrigger = "Show";
    [SerializeField] private string _hideTrigger = "Hide";

    [Header("Fallback (used only if the clip has no AnimationEvent)")]
    [Tooltip("Seconds after Show() before IntroFinished fires anyway.")]
    [SerializeField] private float _introFallbackDuration = 5f;

    [Tooltip("Seconds after Hide() before OutroFinished fires anyway.")]
    [SerializeField] private float _outroFallbackDuration = 1.5f;

    // ── Events ─────────────────────────────────────────────

    /// <summary>Intro clip reached its finish marker — safe to start accepting input.</summary>
    public event Action IntroFinished;

    /// <summary>Outro clip reached its finish marker — panels are off screen.</summary>
    public event Action OutroFinished;

    // ── State ──────────────────────────────────────────────

    private Coroutine _fallback;
    private bool _awaitingIntro;
    private bool _awaitingOutro;

    // ── Public API ─────────────────────────────────────────

    /// <summary>Plays the intro. Raises <see cref="IntroFinished"/> exactly once.</summary>
    public void Show()
    {
        _awaitingIntro = true;
        _awaitingOutro = false;
        Fire(_showTrigger, _hideTrigger);
        RestartFallback(_introFallbackDuration, OnIntroFinished);
    }

    /// <summary>Plays the outro. Raises <see cref="OutroFinished"/> exactly once.</summary>
    public void Hide()
    {
        _awaitingOutro = true;
        _awaitingIntro = false;
        Fire(_hideTrigger, _showTrigger);
        RestartFallback(_outroFallbackDuration, OnOutroFinished);
    }

    // ── AnimationEvent entry points ────────────────────────
    // Wire these on the last frame of the intro / outro clips.

    public void OnIntroFinished()
    {
        if (!_awaitingIntro) return;
        _awaitingIntro = false;
        StopFallback();
        IntroFinished?.Invoke();
    }

    public void OnOutroFinished()
    {
        if (!_awaitingOutro) return;
        _awaitingOutro = false;
        StopFallback();
        OutroFinished?.Invoke();
    }

    // ── Private ────────────────────────────────────────────

    private void Fire(string set, string clear)
    {
        if (_animators == null) return;

        foreach (var animator in _animators)
        {
            if (animator == null) continue;
            animator.ResetTrigger(clear);
            animator.SetTrigger(set);
        }
    }

    private void RestartFallback(float delay, Action onElapsed)
    {
        StopFallback();
        if (delay <= 0f || !isActiveAndEnabled) return;
        _fallback = StartCoroutine(FallbackRoutine(delay, onElapsed));
    }

    private void StopFallback()
    {
        if (_fallback == null) return;
        StopCoroutine(_fallback);
        _fallback = null;
    }

    private IEnumerator FallbackRoutine(float delay, Action onElapsed)
    {
        yield return new WaitForSeconds(delay);
        _fallback = null;
        onElapsed();
    }

    private void OnDisable()
    {
        // Don't strand a caller waiting on a clip that can no longer play.
        StopFallback();
        if (_awaitingIntro) OnIntroFinished();
        else if (_awaitingOutro) OnOutroFinished();
    }
}
