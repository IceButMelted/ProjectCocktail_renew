using UnityEngine;
using UnityEngine.UI;

using static E_Cocktail;

public class MixingMinigame : BaseMiniGame
{
    // ── Config ─────────────────────────────────────────────

    private SO_MixingSetting _cfg => Setting as SO_MixingSetting;

    // ── UI ─────────────────────────────────────────────────

    [Header("UI")]
    [Tooltip("Slider showing the moving needle position (0–1). Non-interactable.")]
    [SerializeField] private Slider _needleSlider;

    [Tooltip("Slider showing hit progress (hits / requiredHits). Non-interactable.")]
    [SerializeField] private Slider _hitsProgressSlider;

    [Tooltip("Slider whose handle visually marks the target zone — same trick as ShakingMinigame.")]
    [SerializeField] private Slider _targetZoneSlider;

    // ── Runtime State ──────────────────────────────────────

    /// <summary>Current needle position (0–1).</summary>
    public float NeedlePosition { get; private set; }

    public int Hits { get; private set; }

    private float _needleDirection = 1f; // bounces between 0 and 1

    // ── Slide Panel Flags (mirrors ShakingMinigame) ────────

    private bool _isPanelSlidingIn = false;
    private bool _isPanelSlidingOut = false;

    // ── Cached handle size ─────────────────────────────────

    private float _handleOriginalWidth;

    // ── Lifecycle ──────────────────────────────────────────

    public override void StartGame()
    {
        // Cache handle width before anything resizes it (same as Shaking)
        _handleOriginalWidth = _targetZoneSlider.handleRect.sizeDelta.x;

        base.StartGame(); // → ResetGame() via OnStandby() → then OnProcessing()
        Debug.Log("Mixing Minigame Started");
    }

    // ── FSM Handlers ───────────────────────────────────────

    protected override void OnProcessing()
    {
        // Initialise needle at center so it doesn't snap from 0
        NeedlePosition = 0.5f;
        _needleDirection = 1f;
        Hits = 0;

        // Initial visual sync
        UpdateUI();
    }

    // ── Game Loop ──────────────────────────────────────────

    public override void ProcessedGame()
    {
        if (!IsRunning) return;

        // ── Panel slide-in ─────────────────────────────────
        if (_isPanelSlidingIn)
        {
            if (!SlideMinigame(Direction.Right, FinishCondition.FullyIn)) return;
            else _isPanelSlidingIn = false;
        }

        // ── Panel slide-out (win) ──────────────────────────
        if (_isPanelSlidingOut)
        {
            if (!SlideMinigame(Direction.Left, FinishCondition.FullyOut)) return;
            else
            {
                _isPanelSlidingOut = false;
                SetState(MiniGameState.Success);
            }
        }

        base.ProcessedGame(); // polls IsClickedThisFrame

        // ── 1. Move needle ─────────────────────────────────
        float speed = _cfg.NeedleSpeed * _cfg.DifficultyMultiplier;
        NeedlePosition += _needleDirection * speed * Time.deltaTime;

        // Bounce at edges
        if (NeedlePosition >= 1f) { NeedlePosition = 1f; _needleDirection = -1f; }
        if (NeedlePosition <= 0f) { NeedlePosition = 0f; _needleDirection = 1f; }

        // ── 2. Register click ──────────────────────────────
        if (IsClickedThisFrame)
        {
            bool inZone = NeedlePosition >= _cfg.TargetZoneMin &&
                          NeedlePosition <= _cfg.TargetZoneMax;

            if (inZone) Hits++;

            Debug.Log($"Click | Needle: {NeedlePosition:P0} | InZone: {inZone} | Hits: {Hits}/{_cfg.RequiredHits}");
        }

        // ── 3. Win condition ───────────────────────────────
        if (Hits >= _cfg.RequiredHits)
            _isPanelSlidingOut = true;

        UpdateUI();
    }

    // ── EndGame ────────────────────────────────────────────

    public override void EndGame()
    {
        base.EndGame();
        Debug.Log($"Mixing Ended | Hits: {Hits}/{_cfg?.RequiredHits} | {CurrentState}");
    }

    // ── UI ─────────────────────────────────────────────────

    public override void UpdateUI()
    {
        base.UpdateUI();

        UpdateNeedleVisual();
        UpdateTargetZoneVisual();
        UpdateHitsProgressVisual();
    }

    // ── ResetGame ──────────────────────────────────────────

    protected override void ResetGame()
    {
        NeedlePosition = 0.5f;
        _needleDirection = 1f;
        Hits = 0;

        // Trigger slide-in only when starting fresh (not mid-game reset)
        if (!IsRunning)
            _isPanelSlidingIn = true;

        _isPanelSlidingOut = false;

        Debug.Log("Mixing Minigame Reset");
        base.ResetGame();
    }

    // ── Debug ──────────────────────────────────────────────

    public override string GetGameState()
        => $"Mixing | Needle: {NeedlePosition:P0} | Hits: {Hits}/{_cfg?.RequiredHits} | {CurrentState}";

    // ── Private UI Helpers ─────────────────────────────────

    /// <summary>
    /// Moves the needle slider thumb to the current needle position.
    /// Mirrors _gaugeSlider.value = GaugeValue in ShakingMinigame.
    /// </summary>
    private void UpdateNeedleVisual()
    {
        _needleSlider.value = NeedlePosition;
    }

    /// <summary>
    /// Positions the target zone handle at the center of the zone,
    /// and resizes it to cover the full zone width —
    /// exact same technique as UpdateTargetZoneVisual() in ShakingMinigame.
    /// </summary>
    private void UpdateTargetZoneVisual()
    {
        float zoneCenter = (_cfg.TargetZoneMin + _cfg.TargetZoneMax) / 2f;
        float zoneSize = _cfg.TargetZoneMax - _cfg.TargetZoneMin;

        // Move handle to zone center
        _targetZoneSlider.value = zoneCenter;

        // Resize handle width to cover the zone
        float trackWidth = (_targetZoneSlider.transform as RectTransform).rect.width;
        float handleWidth = zoneSize * trackWidth;

        RectTransform handle = _targetZoneSlider.handleRect;
        handle.sizeDelta = new Vector2(handleWidth, handle.sizeDelta.y);
    }

    /// <summary>
    /// Fills the hits progress slider proportionally to hits / requiredHits.
    /// Mirrors _ProgressSlider.value = TimeInZone / _cfg.Duration in ShakingMinigame.
    /// </summary>
    private void UpdateHitsProgressVisual()
    {
        if (_cfg == null) return;

        _hitsProgressSlider.value = (float)Hits / _cfg.RequiredHits;
    }
}