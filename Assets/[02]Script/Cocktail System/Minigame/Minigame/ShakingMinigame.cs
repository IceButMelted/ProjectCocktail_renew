// GDD 3.2.1 — Spam-click to keep the gauge inside the target zone for the full duration.
using UnityEngine;
using UnityEngine.UI;
using static E_Cocktail;

public class ShakingMinigame : BaseMiniGame
{
    // ── Config ─────────────────────────────────────────────

    private SO_ShakingSetting _cfg => Setting as SO_ShakingSetting;

    // ── Inspector ──────────────────────────────────────────

    [Header("UI")]
    [SerializeField] private Slider _gaugeSlider;
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private Slider _targetZoneSlider; // handle visually marks the zone

    // ── Runtime State ──────────────────────────────────────

    public float GaugeValue { get; private set; }
    public float TimeInZone { get; private set; }

    private float _zoneSize;
    private float _zoneCenter;
    private float _zoneBorderMin;
    private float _zoneBorderMax;

    private bool _isPanelSlidingIn = false;
    private bool _isPanelSlidingOut = false;

    // Cached so only width changes each frame
    private float _handleOriginalWidth;

    // ── Lifecycle ──────────────────────────────────────────

    public override void StartGame()
    {
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

        base.ProcessedGame(); // polls IsClickedThisFrame

        float dt = Time.deltaTime;

        // 1. Click → boost gauge
        if (IsClickedThisFrame)
            GaugeValue = Mathf.Clamp01(GaugeValue + _cfg.GaugeIncreasePerClick * _cfg.DifficultyMultiplier);

        // 2. Constant gauge decay
        GaugeValue = Mathf.Clamp01(GaugeValue - _cfg.GaugeDecayRate * dt);

        // 3. Accumulate progress while inside the zone
        bool inZone = GaugeValue >= _zoneBorderMin && GaugeValue <= _zoneBorderMax;
        if (inZone)
        {
            float gained = _cfg.ProgressIncreaseRate * dt;
            TimeInZone += gained;
            ShrinkTargetZone(gained);
        }

        Debug.Log($"Shaking | In Zone: {inZone} | Gauge={GaugeValue:F2} | Zone=[{_zoneBorderMin:F2},{_zoneBorderMax:F2}] | Progress={TimeInZone:F2}");

        // 4. Win condition
        if (TimeInZone >= _cfg.Duration)
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
        _progressSlider.value = TimeInZone / _cfg.Duration;

        UpdateTargetZoneVisual();
        UpdateProgressBarColor();
    }

    public override string GetGameState()
        => $"Shaking | Gauge: {GaugeValue:P0} | Progress: {TimeInZone:F1}s | {CurrentState}";

    // ── Private Helpers ────────────────────────────────────

    private void InitTargetZone()
    {
        _zoneSize = _cfg.TargetZoneMaxSize;

        float halfSize = _zoneSize / 2f;
        float minCenter = Mathf.Max(_cfg.InitTargetZoneMinValue, halfSize);
        float maxCenter = 1f - halfSize;

        _zoneCenter = Random.Range(minCenter, maxCenter);
        _zoneBorderMin = _zoneCenter - halfSize;
        _zoneBorderMax = _zoneCenter + halfSize;

        UpdateTargetZoneVisual();
        UpdateProgressBarColor();
    }

    /// <summary>Shrinks the zone proportionally to progress gained this frame.</summary>
    private void ShrinkTargetZone(float progressDelta)
    {
        if (_zoneSize <= _cfg.TargetZoneMinSize) return;

        _zoneSize = Mathf.Max(_zoneSize - _cfg.TargetZoneShrinkPerProgress * progressDelta, _cfg.TargetZoneMinSize);

        float halfSize = _zoneSize / 2f;
        _zoneBorderMin = _zoneCenter - halfSize;
        _zoneBorderMax = _zoneCenter + halfSize;
    }

    /// <summary>Positions the handle at the zone center and scales its height to cover the zone.</summary>
    private void UpdateTargetZoneVisual()
    {
        _targetZoneSlider.value = _zoneCenter;

        float trackHeight = (_targetZoneSlider.transform as RectTransform).rect.height;
        float handleHeight = _zoneSize * trackHeight;

        _targetZoneSlider.handleRect.sizeDelta = new Vector2(_handleOriginalWidth, handleHeight);
    }

    /// <summary>Smoothly transitions the progress bar from start to end color.</summary>
    private void UpdateProgressBarColor()
    {
        float t = Mathf.SmoothStep(0f, 1f, TimeInZone / _cfg.Duration);

        _progressSlider.fillRect
            .GetComponent<Image>().color = Oklab.OklabLerp(_cfg.ProgressBarStartColor, _cfg.ProgressBarEndColor, t);
    }
}