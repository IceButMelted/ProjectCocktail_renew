// GDD 3.2.2 — Click to stop the needle inside the target zone the required number of times.
//
// Needle behaviour:
//   - Accelerates based on its own position (not constant speed)
//   - Hit  → zone shrinks + speed increases + new random zone position
//   - Miss → zone grows back + speed resets to base + new random zone position
using UnityEngine;
using UnityEngine.UI;
using static E_Cocktail;

public class MixingMinigame : BaseMiniGame
{
    // ── Config ─────────────────────────────────────────────

    private SO_MixingSetting _cfg => Setting as SO_MixingSetting;

    // ── Fallback Settings (used if SO is null) ─────────────

    [Header("Fallback Settings (used if SO is missing)")]
    [SerializeField] private float _fallbackDuration = 5f;
    [SerializeField] private float _fallbackDifficultyMultiplier = 1f;
    [SerializeField] private float _fallbackTargetZoneMinSize = 0.45f;
    [SerializeField] private float _fallbackTargetZoneMaxSize = 0.55f;
    [SerializeField] private float _fallbackTargetZoneShrinkPerHit = 0.05f;
    [SerializeField] private float _fallbackTargetZoneExtendPerMiss = 0.1f;
    [SerializeField] private float _fallbackNeedleInitSpeed = 0.6f;
    [SerializeField] private float _fallbackNeedleMaxSpeed = 3f;
    [SerializeField] private float _fallbackNeedleSpeedIncreasePerHit = 0.5f;
    [SerializeField] private float _fallbackNeedleSpeedDecreasePerMiss = 0.1f;
    [SerializeField] private int _fallbackRequiredHits = 3;

    // ── Resolved values (SO or fallback) ───────────────────

    private float Duration => _cfg != null ? _cfg.Duration : _fallbackDuration;
    private float DifficultyMultiplier => _cfg != null ? _cfg.DifficultyMultiplier : _fallbackDifficultyMultiplier;
    private float TargetZoneMinSize => _cfg != null ? _cfg.TargetZoneMinSize : _fallbackTargetZoneMinSize;
    private float TargetZoneMaxSize => _cfg != null ? _cfg.TargetZoneMaxSize : _fallbackTargetZoneMaxSize;
    private float TargetZoneShrinkPerHit => _cfg != null ? _cfg.TargetZoneShrinkPerHit : _fallbackTargetZoneShrinkPerHit;
    private float TargetZoneExtendPerMiss => _cfg != null ? _cfg.TargetZoneExtendPerMiss : _fallbackTargetZoneExtendPerMiss;
    private float NeedleInitSpeed => _cfg != null ? _cfg.NeedleInitSpeed : _fallbackNeedleInitSpeed;
    private float NeedleMaxSpeed => _cfg != null ? _cfg.NeedleMaxSpeed : _fallbackNeedleMaxSpeed;
    private float NeedleSpeedIncreasePerHit => _cfg != null ? _cfg.NeedleSpeedIncreasePerHit : _fallbackNeedleSpeedIncreasePerHit;
    private float NeedleSpeedDecreasePerMiss => _cfg != null ? _cfg.NeedleSpeedDecreasePerMiss : _fallbackNeedleSpeedDecreasePerMiss;
    private int RequiredHits => _cfg != null ? _cfg.RequiredHits : _fallbackRequiredHits;

    // ── Inspector ──────────────────────────────────────────

    [Header("UI")]
    [Tooltip("Slider showing the moving needle (0–1). Non-interactable.")]
    [SerializeField] private Slider _needleSlider;

    [Tooltip("Slider showing hit progress (hits / requiredHits). Non-interactable.")]
    [SerializeField] private Slider _hitsProgressSlider;

    [Tooltip("Slider whose handle visually marks the target zone.")]
    [SerializeField] private Slider _targetZoneSlider;

    // ── Runtime State ──────────────────────────────────────

    /// <summary>Current needle position, normalized 0–1.</summary>
    public float NeedlePosition { get; private set; }

    /// <summary>Number of successful hits so far.</summary>
    public int Hits { get; private set; }

    private float _needleSpeed;
    private float _needleDirection = 1f; // +1 = moving right, -1 = moving left

    private float _zoneCenter;
    private float _zoneHalfSize;

    private float ZoneMin => Mathf.Max(0f, _zoneCenter - _zoneHalfSize);
    private float ZoneMax => Mathf.Min(1f, _zoneCenter + _zoneHalfSize);

    private bool _isPanelSlidingIn = false;
    private bool _isPanelSlidingOut = false;

    // Cached so only width changes each frame
    private float _handleOriginalHeight;

    // ── Lifecycle ──────────────────────────────────────────

    public override void StartGame()
    {
        if (_cfg == null)
            Debug.LogWarning("MixingMinigame: SO_MixingSetting not found — using fallback values.");

        _handleOriginalHeight = _targetZoneSlider.handleRect.sizeDelta.y;

        ResetGame();
        UpdateUI();
        base.StartGame();
        Debug.Log("Mixing Minigame Started");
    }

    // ── FSM Callbacks ──────────────────────────────────────

    protected override void OnProcessing() => UpdateUI();

    // ── Game Loop ──────────────────────────────────────────

    public override void ProcessedGame()
    {
        if (!IsRunning) return;

        if (_isPanelSlidingIn)
        {
            if (!SlideMinigame(Direction.Right, FinishCondition.FullyIn)) return;
            _isPanelSlidingIn = false;
        }

        if (_isPanelSlidingOut)
        {
            if (!SlideMinigame(Direction.Left, FinishCondition.FullyOut)) return;
            _isPanelSlidingOut = false;
            SetState(MiniGameState.Success);
            return;
        }

        float dt = Time.deltaTime;

        // ── 1. Move needle ────────────────────────────────────────────────────
        float currentSpeed = _needleSpeed;
        NeedlePosition += currentSpeed * _needleDirection * dt;

        // ── 2. Bounce — both walls in one pass ────────────────────────────────
        if (NeedlePosition >= 1f)
        {
            NeedlePosition = 1f;
            _needleDirection = -1f;
        }
        else if (NeedlePosition <= 0f)
        {
            NeedlePosition = 0f;
            _needleDirection = 1f;
        }

        Debug.Log($"current Speed : {currentSpeed} ; ");

        // ── 3. Poll click ─────────────────────────────────────────────────────
        base.ProcessedGame();

        // ── 4. Register click ─────────────────────────────────────────────────
        if (IsClickedThisFrame)
        {
            bool inZone = NeedlePosition >= ZoneMin && NeedlePosition <= ZoneMax;

            if (inZone) OnHit();
            else OnMiss();

            Debug.Log($"{(inZone ? "HIT" : "MISS")} | Needle: {NeedlePosition:P0} | Zone: [{ZoneMin:F2}–{ZoneMax:F2}] | Hits: {Hits}/{RequiredHits} | Speed: {_needleSpeed:F2}");
        }

        // ── 5. Win condition ──────────────────────────────────────────────────
        if (Hits >= RequiredHits)
            _isPanelSlidingOut = true;

        UpdateUI();
    }

    // ── EndGame / Reset ────────────────────────────────────

    public override void EndGame()
    {
        base.EndGame();
        Debug.Log($"Mixing Ended | Hits: {Hits}/{RequiredHits} | {CurrentState}");
    }

    protected override void ResetGame()
    {
        NeedlePosition = 0f;
        _needleDirection = 1f;
        Hits = 0;

        _needleSpeed = NeedleInitSpeed;
        _zoneHalfSize = TargetZoneMaxSize / 2f;

        RandomizeZonePosition();

        if (!IsRunning)
            _isPanelSlidingIn = true;

        _isPanelSlidingOut = false;

        Debug.Log("Mixing Minigame Reset");
        base.ResetGame();
    }

    // ── UI ─────────────────────────────────────────────────

    public override void UpdateUI()
    {
        base.UpdateUI();
        UpdateNeedleVisual();
        UpdateTargetZoneVisual();
        UpdateHitsProgressVisual();
    }

    public override string GetGameState()
        => $"Mixing | Needle: {NeedlePosition:P0} | Zone: [{ZoneMin:F2}–{ZoneMax:F2}] | Hits: {Hits}/{RequiredHits} | Speed: {_needleSpeed:F2} | {CurrentState}";

    // ── Private Hit / Miss ─────────────────────────────────

    /// <summary>Zone shrinks, speed increases, zone moves to a new random position.</summary>
    private void OnHit()
    {
        Hits++;

        _zoneHalfSize = Mathf.Max(
            _zoneHalfSize - TargetZoneShrinkPerHit / 2f,
            TargetZoneMinSize / 2f);

        _needleSpeed = Mathf.Min(
            _needleSpeed + NeedleSpeedIncreasePerHit,
            NeedleMaxSpeed);

        RandomizeZonePosition();
    }

    /// <summary>Zone grows back toward max, speed resets to base, zone moves to a new random position.</summary>
    private void OnMiss()
    {
        _zoneHalfSize = Mathf.Min(
            _zoneHalfSize + TargetZoneExtendPerMiss,
            TargetZoneMaxSize / 2f);

        _needleSpeed = _needleSpeed - NeedleSpeedDecreasePerMiss;
        _needleSpeed = Mathf.Max(NeedleInitSpeed, _needleSpeed);

        RandomizeZonePosition();
    }

    /// <summary>Picks a new random zone center that keeps the zone fully within [0, 1].</summary>
    private void RandomizeZonePosition()
    {
        float minCenter = _zoneHalfSize;
        float maxCenter = 1f - _zoneHalfSize;

        if (minCenter > maxCenter) minCenter = maxCenter; // safety for very large zones

        _zoneCenter = Random.Range(minCenter, maxCenter);
    }

    // ── Private UI Helpers ─────────────────────────────────

    private void UpdateNeedleVisual()
        => _needleSlider.value = NeedlePosition;

    /// <summary>Positions the zone handle at the zone center and scales its width to cover the zone.</summary>
    private void UpdateTargetZoneVisual()
    {
        _targetZoneSlider.value = _zoneCenter;

        float trackWidth = (_targetZoneSlider.transform as RectTransform).rect.width;
        float handleWidth = _zoneHalfSize * 2f * trackWidth;

        _targetZoneSlider.handleRect.sizeDelta = new Vector2(handleWidth, _handleOriginalHeight);
    }

    private void UpdateHitsProgressVisual()
        => _hitsProgressSlider.value = (float)Hits / RequiredHits;
}