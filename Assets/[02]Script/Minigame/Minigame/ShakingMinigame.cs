using UnityEngine;
using UnityEngine.UI;
using static E_Cocktail;

public class ShakingMinigame : BaseMiniGame
{
    // ── IMinigame — Registry Key ───────────────────────────

    /// <summary>
    /// Registers this game with MinigameSystemManager automatically.
    /// No code change in the manager is ever needed.
    /// </summary>
    public override Enum_MiniGameType GameType => Enum_MiniGameType.Shaking;

    // ── Config ─────────────────────────────────────────────

    /// <summary>Resolved once in Awake — never null after that.</summary>
    private SO_ShakingSetting _cfg;

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

    private float _handleOriginalWidth;

    // ── Unity Lifecycle ────────────────────────────────────

    private void Awake() => _cfg = ResolveSetting<SO_ShakingSetting>();

    // ── IMinigame ──────────────────────────────────────────

    /// <summary>
    /// One frame of shaking gameplay. Only ever called during MinigamePhase.Play,
    /// so there is nothing to guard against here.
    /// </summary>
    protected override void OnTick(float dt)
    {
        // ── Click → raise gauge ───────────────────────────
        if (Input.IsClickedThisFrame)
        {
            GaugeValue = Mathf.Clamp01(GaugeValue + _cfg.GaugeIncreasePerClick * _cfg.DifficultyMultiplier);
            //Animation Minigame Controller
            amc.PlayAnimation();
        }
        // ── Decay gauge ───────────────────────────────────
        GaugeValue = Mathf.Clamp01(GaugeValue - _cfg.GaugeDecayRate * dt);

        // ── Progress while in zone ────────────────────────
        bool inZone = GaugeValue >= _zoneBorderMin && GaugeValue <= _zoneBorderMax;
        if (inZone)
        {

            float gained = _cfg.ProgressIncreaseRate * dt;
            TimeInZone += gained;
            ShrinkTargetZone(gained);
        }

        // ── Win condition ─────────────────────────────────
        if (TimeInZone >= _cfg.Duration)
            Complete();
    }

    public override string GetGameState()
        => $"Shaking | Gauge: {GaugeValue:P0} | Progress: {TimeInZone:F1}s/{_cfg.Duration:F1}s | {Phase}";

    // ── UI ─────────────────────────────────────────────────

    public override void UpdateUI()
    {
        base.UpdateUI();
        _gaugeSlider.value = GaugeValue;
        _progressSlider.value = TimeInZone / _cfg.Duration;
        UpdateTargetZoneVisual();
        //UpdateProgressBarColor();
    }

    // ── FSM Hooks ──────────────────────────────────────────

    /// <summary>Entering Play — the intro is over, so the layout is settled and cacheable.</summary>
    protected override void OnEnter()
    {
        _handleOriginalWidth = _targetZoneSlider.handleRect.sizeDelta.x;

        GaugeValue = 0f;
        TimeInZone = 0f;

        InitTargetZone();

        Debug.Log("[ShakingMinigame] Started");
    }

    protected override void OnExit()
        => Debug.Log($"[ShakingMinigame] Ended | Gauge={GaugeValue:F2} | Progress={TimeInZone:F2}");

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
        //UpdateProgressBarColor();
    }

    private void ShrinkTargetZone(float progressDelta)
    {
        if (_zoneSize <= _cfg.TargetZoneMinSize) return;

        _zoneSize = Mathf.Max(
            _zoneSize - _cfg.TargetZoneShrinkPerProgress * progressDelta,
            _cfg.TargetZoneMinSize);

        float halfSize = _zoneSize / 2f;
        _zoneBorderMin = _zoneCenter - halfSize;
        _zoneBorderMax = _zoneCenter + halfSize;
    }

    private void UpdateTargetZoneVisual()
    {
        _targetZoneSlider.value = _zoneCenter;

        float trackHeight = (_targetZoneSlider.transform as RectTransform).rect.height;
        _targetZoneSlider.handleRect.sizeDelta =
            new Vector2(_handleOriginalWidth, _zoneSize * trackHeight);
    }

    private void UpdateProgressBarColor()
    {
        float t = Mathf.SmoothStep(0f, 1f, TimeInZone / _cfg.Duration);
        _progressSlider.fillRect
            .GetComponent<Image>().color =
            Oklab.OklabLerp(_cfg.ProgressBarStartColor, _cfg.ProgressBarEndColor, t);
    }
}