// GDD 3.2.1 — Spam-click to keep the gauge inside the target zone for the full duration.
using UnityEngine;
using UnityEngine.UI;
using static E_Cocktail;

public class ShakingMinigame : BaseMiniGame
{
    // ── Config ─────────────────────────────────────────────

    private SO_ShakingSetting _cfg => Setting as SO_ShakingSetting;

    // ── Fallback Settings (used if SO is null) ─────────────
    [Header("Fallback Settings (used if SO is missing)")]
    [SerializeField] private float _fallbackDuration = 5f;
    [SerializeField] private float _fallbackDifficultyMultiplier = 1f;
    [SerializeField] private float _fallbackTargetZoneMinSize = 0.4f;
    [SerializeField] private float _fallbackTargetZoneMaxSize = 0.7f;
    [SerializeField] private float _fallbackInitTargetZoneMinValue = 0.7f;
    [SerializeField] private float _fallbackTargetZoneShrinkPerProgress = 0.01f;
    [SerializeField] private float _fallbackGaugeDecayRate = 0.15f;
    [SerializeField] private float _fallbackGaugeIncreasePerClick = 0.08f;
    [SerializeField] private float _fallbackProgressIncreaseRate = 0.2f;
    [SerializeField] private Color _fallbackProgressBarStartColor = Color.green;
    [SerializeField] private Color _fallbackProgressBarEndColor = Color.red;

    // ── Resolved values (SO or fallback) ───────────────────
    private float Duration => _cfg != null ? _cfg.Duration : _fallbackDuration;
    private float DifficultyMultiplier => _cfg != null ? _cfg.DifficultyMultiplier : _fallbackDifficultyMultiplier;
    private float TargetZoneMinSize => _cfg != null ? _cfg.TargetZoneMinSize : _fallbackTargetZoneMinSize;
    private float TargetZoneMaxSize => _cfg != null ? _cfg.TargetZoneMaxSize : _fallbackTargetZoneMaxSize;
    private float InitTargetZoneMinValue => _cfg != null ? _cfg.InitTargetZoneMinValue : _fallbackInitTargetZoneMinValue;
    private float TargetZoneShrinkPerProgress => _cfg != null ? _cfg.TargetZoneShrinkPerProgress : _fallbackTargetZoneShrinkPerProgress;
    private float GaugeDecayRate => _cfg != null ? _cfg.GaugeDecayRate : _fallbackGaugeDecayRate;
    private float GaugeIncreasePerClick => _cfg != null ? _cfg.GaugeIncreasePerClick : _fallbackGaugeIncreasePerClick;
    private float ProgressIncreaseRate => _cfg != null ? _cfg.ProgressIncreaseRate : _fallbackProgressIncreaseRate;
    private Color ProgressBarStartColor => _cfg != null ? _cfg.ProgressBarStartColor : _fallbackProgressBarStartColor;
    private Color ProgressBarEndColor => _cfg != null ? _cfg.ProgressBarEndColor : _fallbackProgressBarEndColor;

    // ── Inspector ──────────────────────────────────────────

    [Header("UI")]
    [SerializeField] private Slider _gaugeSlider;
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private Slider _targetZoneSlider;

    // ── Runtime State ──────────────────────────────────────

    public float GaugeValue { get; private set; }
    public float TimeInZone { get; private set; }

    private float _zoneSize;
    private float _zoneCenter;
    private float _zoneBorderMin;
    private float _zoneBorderMax;

    private bool _isPanelSlidingIn = false;
    private bool _isPanelSlidingOut = false;

    private float _handleOriginalWidth;

    // ── Lifecycle ──────────────────────────────────────────

    public override void StartGame()
    {
        if (_cfg == null)
            Debug.LogWarning("ShakingMinigame: SO_ShakingSetting not found — using fallback values.");

        _handleOriginalWidth = _targetZoneSlider.handleRect.sizeDelta.x;
        InitTargetZone();
        ResetGame();
        base.StartGame();
        Debug.Log("Shaking Minigame Started");
    }

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
        }

        base.ProcessedGame();

        float dt = Time.deltaTime;

        if (IsClickedThisFrame)
            GaugeValue = Mathf.Clamp01(GaugeValue + GaugeIncreasePerClick * DifficultyMultiplier);

        GaugeValue = Mathf.Clamp01(GaugeValue - GaugeDecayRate * dt);

        bool inZone = GaugeValue >= _zoneBorderMin && GaugeValue <= _zoneBorderMax;
        if (inZone)
        {
            float gained = ProgressIncreaseRate * dt;
            TimeInZone += gained;
            ShrinkTargetZone(gained);
        }

        if (TimeInZone >= Duration)
            _isPanelSlidingOut = true;

        UpdateUI();
    }

    // ── EndGame / Reset ────────────────────────────────────

    public override void EndGame()
    {
        base.EndGame();
        Debug.Log($"Shaking Ended | Gauge={GaugeValue:F2} | Progress={TimeInZone:F2} | {CurrentState}");
    }

    protected override void ResetGame()
    {
        GaugeValue = 0f;
        TimeInZone = 0f;

        if (!IsRunning)
            _isPanelSlidingIn = true;

        _isPanelSlidingOut = false;

        Debug.Log("Shaking Minigame Reset");
        base.ResetGame();
    }

    // ── UI ─────────────────────────────────────────────────

    public override void UpdateUI()
    {
        base.UpdateUI();

        _gaugeSlider.value = GaugeValue;
        _progressSlider.value = TimeInZone / Duration;

        UpdateTargetZoneVisual();
        UpdateProgressBarColor();
    }

    public override string GetGameState()
        => $"Shaking | Gauge: {GaugeValue:P0} | Progress: {TimeInZone:F1}s | {CurrentState}";

    // ── Private Helpers ────────────────────────────────────

    private void InitTargetZone()
    {
        _zoneSize = TargetZoneMaxSize;

        float halfSize = _zoneSize / 2f;
        float minCenter = Mathf.Max(InitTargetZoneMinValue, halfSize);
        float maxCenter = 1f - halfSize;

        _zoneCenter = Random.Range(minCenter, maxCenter);
        _zoneBorderMin = _zoneCenter - halfSize;
        _zoneBorderMax = _zoneCenter + halfSize;

        UpdateTargetZoneVisual();
        UpdateProgressBarColor();
    }

    private void ShrinkTargetZone(float progressDelta)
    {
        if (_zoneSize <= TargetZoneMinSize) return;

        _zoneSize = Mathf.Max(_zoneSize - TargetZoneShrinkPerProgress * progressDelta, TargetZoneMinSize);

        float halfSize = _zoneSize / 2f;
        _zoneBorderMin = _zoneCenter - halfSize;
        _zoneBorderMax = _zoneCenter + halfSize;
    }

    private void UpdateTargetZoneVisual()
    {
        _targetZoneSlider.value = _zoneCenter;

        float trackHeight = (_targetZoneSlider.transform as RectTransform).rect.height;
        float handleHeight = _zoneSize * trackHeight;

        _targetZoneSlider.handleRect.sizeDelta = new Vector2(_handleOriginalWidth, handleHeight);
    }

    private void UpdateProgressBarColor()
    {
        float t = Mathf.SmoothStep(0f, 1f, TimeInZone / Duration);
        _progressSlider.fillRect
            .GetComponent<Image>().color = Oklab.OklabLerp(ProgressBarStartColor, ProgressBarEndColor, t);
    }
}