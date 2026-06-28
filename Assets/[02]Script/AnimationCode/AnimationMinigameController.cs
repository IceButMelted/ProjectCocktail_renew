using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationMinigameController : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private string animationStateName = "YourAnimationName";

    [Header("Speed Settings")]
    [SerializeField] private float capSpeed = 1f;   // max speed
    [SerializeField] private float speedUpRate = 5f;   // units/sec ramp up
    [SerializeField] private float decayRate = 0.5f; // units/sec ramp down

    [Header("Continue Settings")]
    [SerializeField] private float continueDuration = 2f; // seconds before decay

    // ── State ──────────────────────────────────────────────
    private Animator _animator;
    private Coroutine _activeCoroutine;
    private float _currentSpeed = 0f;
    private bool _hardStarted = false; // true = StartAnimation() was called → no decay

    // ── Unity ──────────────────────────────────────────────
    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (_animator.enabled)
            _animator.speed = _currentSpeed;
    }

    // ── Public API ─────────────────────────────────────────

    /// <summary>
    /// Plays from the beginning at cap speed. Stays at cap speed indefinitely —
    /// no decay. Only StopAnimation() can bring it down.
    /// </summary>
    public void StartAnimation()
    {
        StopActiveCoroutine();

        _hardStarted = true;
        _animator.enabled = true;
        _animator.Play(animationStateName, 0, 0f);

        _activeCoroutine = StartCoroutine(RampTo(capSpeed));

        Debug.Log("[AnimationController] StartAnimation — ramping to cap, no decay");
    }

    /// <summary>Immediately freezes the animation and clears the hard-start flag.</summary>
    public void StopAnimation()
    {
        StopActiveCoroutine();

        _hardStarted = false;
        _currentSpeed = 0f;

        Debug.Log("[AnimationController] StopAnimation");
    }

    /// <summary>
    /// Ramps speed up to cap and resets the continue timer.
    /// • After StartAnimation()  → stays at cap speed when timer ends (no decay).
    /// • Standalone              → decays to 0 when timer ends.
    /// Calling again before the timer ends resets the timer.
    /// </summary>
    public void PlayAnimation()
    {
        StopActiveCoroutine();

        _activeCoroutine = StartCoroutine(PlaySequence());

        Debug.Log($"[AnimationController] PlayAnimation — ramp up, hold {continueDuration}s" +
                  (_hardStarted ? ", then hold at cap (no decay)" : ", then decay"));
    }

    /// <summary>Manually set speed (0 – capSpeed). Clears hard-start flag.</summary>
    public void SetSpeed(float speed)
    {
        StopActiveCoroutine();

        _hardStarted = false;
        _currentSpeed = Mathf.Clamp(speed, 0f, capSpeed);

        Debug.Log($"[AnimationController] SetSpeed → {_currentSpeed}");
    }

    /// <summary>Change cap speed at runtime.</summary>
    public void SetCapSpeed(float newCap)
    {
        capSpeed = Mathf.Max(0f, newCap);
        Debug.Log($"[AnimationController] SetCapSpeed → {capSpeed}");
    }

    // ── Coroutines ─────────────────────────────────────────

    // Ramps speed to capSpeed then holds forever (used by StartAnimation)
    private IEnumerator RampTo(float target)
    {
        while (!Mathf.Approximately(_currentSpeed, target))
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, target, speedUpRate * Time.deltaTime);
            yield return null;
        }
        _currentSpeed = target;
    }

    // Full PlayAnimation sequence: ramp up → hold → (decay or hold based on _hardStarted)
    private IEnumerator PlaySequence()
    {
        // 1. Ramp up to cap speed
        while (_currentSpeed < capSpeed)
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, capSpeed, speedUpRate * Time.deltaTime);
            yield return null;
        }
        _currentSpeed = capSpeed;

        // 2. Hold for continueDuration
        yield return new WaitForSeconds(continueDuration);

        // 3a. Hard-started → stay at cap speed, no decay
        if (_hardStarted)
        {
            Debug.Log("[AnimationController] Timer expired — holding cap speed (StartAnimation active)");
            yield break;
        }

        // 3b. Standalone → decay to 0
        Debug.Log("[AnimationController] Timer expired — decaying speed");
        while (_currentSpeed > 0f)
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, decayRate * Time.deltaTime);
            yield return null;
        }
        _currentSpeed = 0f;
        Debug.Log("[AnimationController] Speed reached 0 — animation stopped");
    }

    // ── Helpers ────────────────────────────────────────────

    private void StopActiveCoroutine()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }
    }

    // ── Convenience ────────────────────────────────────────

    public bool IsPlaying => _currentSpeed > 0f;
    public float CurrentSpeed => _currentSpeed;
    public float CapSpeed => capSpeed;
}

