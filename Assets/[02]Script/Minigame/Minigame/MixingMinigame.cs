using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static E_Cocktail;

public class MixingMinigame : BaseMiniGame
{
    // ── IMinigame — Registry Key ───────────────────────────

    public override Enum_MiniGameType GameType => Enum_MiniGameType.Stiring;

    // ── Config ─────────────────────────────────────────────

    /// <summary>Resolved once in Awake — never null after that.</summary>
    private SO_MixingSetting _cfg;

    private bool _cfgIsRuntimeCreated;

    // ── Inspector ──────────────────────────────────────────

    [Header("UI")]
    [Tooltip("Slider showing the moving needle (0–1). Non-interactable.")]
    [SerializeField] private Slider _needleSlider;

    [Tooltip("Slider showing hit progress (hits / requiredHits). Non-interactable.")]
    [SerializeField] private Slider _hitsProgressSlider;

    [Tooltip("Slider whose handle visually marks the target zone.")]
    [SerializeField] private Slider _targetZoneSlider;

    [Header("Target Zone")]
    [Tooltip("How long the target zone stays hidden after any hit or miss.")]
    [SerializeField] private float _zoneHideAfterInputDuration = 0.6f;

    [Header("Miss Penalty")]
    [Tooltip("How long the needle freezes on a miss.")]
    [SerializeField] private float _missFreezeNeedleDuration = 0.3f;

    [Header("Hit Progress Animation")]
    [Tooltip("Duration in seconds for one progress-bar step animation.")]
    [SerializeField] private float _hitProgressAnimDuration = 0.35f;

    [Tooltip("Controls the overshoot/backshoot amount. " +
             "0 = no overshoot (linear ease). 1.7 = default springy feel. " +
             "Negative values produce a back-pull before moving forward.")]
    [SerializeField] private float _overshootStrength = 1.7f;

    private RectTransform _targetZoneRect;
    private RectTransform _cachedHandleRect;

    
    private float _cachedTrackWidth;

    // ── Runtime State ──────────────────────────────────────

    public float NeedlePosition { get; private set; }
    public int Hits { get; private set; }

    private float _needleSpeed;
    private float _needleDirection = 1f; // +1 = right, -1 = left

    private float _zoneCenter;
    private float _zoneHalfSize;

    
    private float ZoneMin => Mathf.Max(0f, _zoneCenter - _zoneHalfSize);
    private float ZoneMax => Mathf.Min(1f, _zoneCenter + _zoneHalfSize);

    private float _handleOriginalHeight;

    private float _requiredHitsInverse;

    private Coroutine _hitsProgressCoroutine;

    private bool _isNeedleFrozen;
    private bool _isZoneVisible = true;

    private bool _zoneVisibilityDirty;

    private Coroutine _zonehideCoroutine;
    private Coroutine _needleFreezeCoroutine;

    // ── Unity Lifecycle ────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        _cfg = Setting as SO_MixingSetting;
        if (_cfg == null)
        {
            _cfg = ScriptableObject.CreateInstance<SO_MixingSetting>();
            _cfgIsRuntimeCreated = true;
            Debug.LogWarning($"[{nameof(MixingMinigame)}] No SO_MixingSetting assigned — using default values.");
        }
    }

    private void OnDestroy()
    {
        if (_cfg != null && _cfgIsRuntimeCreated)
            Destroy(_cfg);
    }

    // ── IMinigame ──────────────────────────────────────────

    public override void StartGame()
    {
        
        _targetZoneRect = _targetZoneSlider.transform as RectTransform;
        _cachedHandleRect = _targetZoneSlider.handleRect;
        _cachedTrackWidth = _targetZoneRect.rect.width;

        _requiredHitsInverse = _cfg.RequiredHits > 0 ? 1f / _cfg.RequiredHits : 0f;

        _handleOriginalHeight = _cachedHandleRect.sizeDelta.y;

        ResetGame();
        UpdateUI();
        base.StartGame();
        Debug.Log("[MixingMinigame] Started");
    }

    /// <summary>
    /// Processes one frame of mixing gameplay.
    /// base.ProcessedGame() is called first — it guards IsRunning
    /// and polls the input provider.
    /// </summary>
    public override void ProcessedGame()
    {
        
        base.ProcessedGame();
        if (_isNeedleFrozen) // if frozen on miss, skip needle movement but still not allow input and zone hide timer to run
            return;

        UpdateUI();

        if (!IsRunning) return;

        float dt = Time.deltaTime;

        // ── Move needle (skipped while frozen on miss) ────
        NeedlePosition += _needleSpeed * _needleDirection * dt;

        if (NeedlePosition >= 1f) { NeedlePosition = 1f; _needleDirection = -1f; }
        else if (NeedlePosition <= 0f) { NeedlePosition = 0f; _needleDirection = 1f; }


        // ── Register click ────────────────────────────────
        if (Input.IsClickedThisFrame && _isZoneVisible)
        {
            float zMin = ZoneMin;
            float zMax = ZoneMax;

            bool inZone = NeedlePosition >= zMin && NeedlePosition <= zMax;
            if (inZone) {
                //amc = Animation Minagame Controller
                amc.PlayAnimation();
                OnHit(); 
            }
            else OnMiss();

            //Debug.Log($"[MixingMinigame] {(inZone ? "HIT" : "MISS")} | " +
            //          $"Needle: {NeedlePosition:P0} | Zone: [{zMin:F2}–{zMax:F2}] | " +
            //          $"Hits: {Hits}/{_cfg.RequiredHits} | Speed: {_needleSpeed:F2}");
        }

        // ── Win condition ─────────────────────────────────
        if (Hits >= _cfg.RequiredHits)
        {
            CurrentSlidePhase = SlidePhase.RemoveMinigame;
            amc.StopAnimation();
            IsRunning = false;
        }

        
    }

    public override void EndGame()
    {
        base.EndGame();
        Debug.Log($"[MixingMinigame] Ended | Hits: {Hits}/{_cfg.RequiredHits} | {CurrentState}");
    }

    public override string GetGameState()
        => $"Mixing | Needle: {NeedlePosition:P0} | Zone: [{ZoneMin:F2}–{ZoneMax:F2}] | " +
           $"Hits: {Hits}/{_cfg.RequiredHits} | Speed: {_needleSpeed:F2} | {CurrentState}";

    // ── UI ─────────────────────────────────────────────────

    public override void UpdateUI()
    {
        base.UpdateUI();
        UpdateNeedleVisual();
        UpdateTargetZoneVisual();
        //UpdateHitsProgressVisual();
    }

    // ── FSM Hooks ──────────────────────────────────────────

    protected override void OnProcessing() => UpdateUI();

    protected override void ResetGame()
    {
        if (_zonehideCoroutine != null) { StopCoroutine(_zonehideCoroutine); _zonehideCoroutine = null; }
        if (_needleFreezeCoroutine != null) { StopCoroutine(_needleFreezeCoroutine); _needleFreezeCoroutine = null; }

        _isZoneVisible = true;
        _zoneVisibilityDirty = true; // force a SetActive(true) on next UpdateUI
        _isNeedleFrozen = false;

        NeedlePosition = 0f;
        _needleDirection = 1f;
        Hits = 0;

        _needleSpeed = _cfg.NeedleInitSpeed;
        _zoneHalfSize = _cfg.TargetZoneMaxSize / 2f;

        _hitsProgressSlider.value = 0f;

        RandomizeZonePosition();

        // Slide in only when starting fresh, not when replaying mid-session.
        CurrentSlidePhase = IsRunning ? SlidePhase.None : SlidePhase.Closing;

        Debug.Log("[MixingMinigame] Reset");
        base.ResetGame();
    }

    // ── Private: Hit / Miss ────────────────────────────────

    private void OnHit()
    {
        Hits++;
        UpdateHitsProgressVisual();

        _zoneHalfSize = Mathf.Max(
            _zoneHalfSize - _cfg.TargetZoneShrinkPerHit / 2f,
            _cfg.TargetZoneMinSize / 2f);

        _needleSpeed = Mathf.Min(
            _needleSpeed + _cfg.NeedleSpeedIncreasePerHit,
            _cfg.NeedleMaxSpeed);

        RandomizeZonePosition();
        TriggerZoneHide();
    }

    private void OnMiss()
    {
        _zoneHalfSize = Mathf.Min(
            _zoneHalfSize + _cfg.TargetZoneExtendPerMiss,
            _cfg.TargetZoneMaxSize / 2f);

        _needleSpeed = Mathf.Max(
            _needleSpeed - _cfg.NeedleSpeedDecreasePerMiss,
            _cfg.NeedleInitSpeed);

        RandomizeZonePosition();
        TriggerZoneHide();
        TriggerNeedleFreeze();
    }

    private void RandomizeZonePosition()
    {
        float minCenter = _zoneHalfSize;
        float maxCenter = 1f - _zoneHalfSize;
        if (minCenter > maxCenter) minCenter = maxCenter;
        _zoneCenter = Random.Range(minCenter, maxCenter);
    }

    // ── Private: UI Helpers ────────────────────────────────

    private void UpdateNeedleVisual()
        => _needleSlider.value = NeedlePosition;

    private void UpdateTargetZoneVisual()
    {
        if (_zoneVisibilityDirty)
        {
            _cachedHandleRect.gameObject.SetActive(_isZoneVisible);
            _zoneVisibilityDirty = false;
        }

        if (!_isZoneVisible) return;

        _targetZoneSlider.value = _zoneCenter;

        float handleWidth = _zoneHalfSize * 2f * _cachedTrackWidth;
        _cachedHandleRect.sizeDelta = new Vector2(handleWidth, _handleOriginalHeight);
    }

    /// <summary>
    /// Drives the hits progress slider to its new logical value with an
    /// EaseInOutBack animation instead of snapping instantly.
    /// </summary>
    private void UpdateHitsProgressVisual()
    {
        float target = _cfg.RequiredHits > 0 ? Hits * _requiredHitsInverse : 0f;

        if (_hitsProgressCoroutine != null)
            StopCoroutine(_hitsProgressCoroutine);

        _hitsProgressCoroutine = StartCoroutine(
            AnimateHitsProgress(_hitsProgressSlider.value, target));
    }

    /// <summary>
    /// Lerps the slider from <paramref name="from"/> to <paramref name="to"/>
    /// over <see cref="_hitProgressAnimDuration"/> seconds using an
    /// EaseInOutBack curve, whose overshoot amount is controlled by
    /// <see cref="_overshootStrength"/>.
    /// </summary>
    private IEnumerator AnimateHitsProgress(float from, float to)
    {
        float elapsed = 0f;
        float invDuration = _hitProgressAnimDuration > 0f
                               ? 1f / _hitProgressAnimDuration
                               : 1f;

        while (elapsed < _hitProgressAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed * invDuration);
            _hitsProgressSlider.value = Mathf.LerpUnclamped(from, to, EaseInOutBack(t));
            yield return null;
        }

        // Guarantee we land exactly on the target value.
        _hitsProgressSlider.value = to;
        _hitsProgressCoroutine = null;
    }

    /// <summary>
    /// EaseInOutBack — smooth in/out with configurable overshoot.
    /// <para>t = 0 → 0, t = 1 → 1. Peak overshoot near t ≈ 0.5.</para>
    /// </summary>
    /// <param name="t">Normalised time [0, 1].</param>
    private float EaseInOutBack(float t)
    {
        float c1 = _overshootStrength;
        float c2 = c1 * 1.525f;

        if (t < 0.5f)
        {       
            float inner = 2f * t;
            return (inner * inner * ((c2 + 1f) * inner - c2)) / 2f;
        }
        else
        {
            float inner = 2f * t - 2f;
            return (inner * inner * ((c2 + 1f) * inner + c2) + 2f) / 2f;
        }
    }

    // ── Private: Zone Hide ─────────────────────────────────

    /// <summary>
    /// Hides the target zone immediately, then reveals it again after
    /// <see cref="_zoneHideAfterInputDuration"/> seconds.
    /// Any in-flight hide is cancelled and restarted so rapid inputs
    /// don't stack.
    /// </summary>
    private void TriggerZoneHide()
    {
        if (_zonehideCoroutine != null)
            StopCoroutine(_zonehideCoroutine);

        _zonehideCoroutine = StartCoroutine(ZoneHideRoutine());
    }

    private IEnumerator ZoneHideRoutine()
    {
        SetZoneVisible(false);
        yield return new WaitForSeconds(_zoneHideAfterInputDuration);
        SetZoneVisible(true);
        _zonehideCoroutine = null;
    }

    // ── Private: Needle Freeze ─────────────────────────────

    /// <summary>
    /// Freezes the needle for <see cref="_missFreezeNeedleDuration"/> seconds.
    /// Any in-flight freeze is cancelled and restarted.
    /// </summary>
    private void TriggerNeedleFreeze()
    {
        if (_needleFreezeCoroutine != null)
            StopCoroutine(_needleFreezeCoroutine);

        _needleFreezeCoroutine = StartCoroutine(NeedleFreezeRoutine());
    }

    private IEnumerator NeedleFreezeRoutine()
    {
        _isNeedleFrozen = true;
        yield return new WaitForSeconds(_missFreezeNeedleDuration);
        _isNeedleFrozen = false;
        _needleFreezeCoroutine = null;
    }

    // ── Private: Helpers ───────────────────────────────────

    /// <summary>
    /// Sets zone visibility and marks the dirty flag so UpdateTargetZoneVisual
    /// flushes the SetActive call exactly once per state change instead of
    /// every frame (PERF #10).
    /// </summary>
    private void SetZoneVisible(bool visible)
    {
        if (_isZoneVisible == visible) return;
        _isZoneVisible = visible;
        _zoneVisibilityDirty = true;
    }
}