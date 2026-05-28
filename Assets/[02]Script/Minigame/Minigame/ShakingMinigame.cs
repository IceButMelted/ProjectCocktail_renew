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

    protected override void Awake()
    {
        base.Awake();

        _cfg = Setting as SO_ShakingSetting;
        if (_cfg == null)
        {
            _cfg = ScriptableObject.CreateInstance<SO_ShakingSetting>();
            Debug.LogWarning($"[{nameof(ShakingMinigame)}] No SO_ShakingSetting assigned — using default values.");
        }
    }

    private void OnDestroy()
    {
        // Only destroy if it was runtime-created (not a saved asset).
#if UNITY_EDITOR
        if (_cfg != null && !UnityEditor.AssetDatabase.Contains(_cfg))
            Destroy(_cfg);
#else
        Destroy(_cfg);
#endif
    }

    // ── IMinigame ──────────────────────────────────────────

    public override void StartGame()
    {
        _handleOriginalWidth = _targetZoneSlider.handleRect.sizeDelta.x;
        InitTargetZone();
        ResetGame();
        base.StartGame();
        Debug.Log("[ShakingMinigame] Started");
    }

    /// <summary>
    /// Processes one frame of shaking gameplay.
    /// base.ProcessedGame() is called first — it guards IsRunning
    /// and polls the input provider.
    /// </summary>
    public override void ProcessedGame()
    {
        base.ProcessedGame();   // IsRunning guard + Input.Poll()
        if(!IsRunning) return;  

        float dt = Time.deltaTime;

        // ── Click → raise gauge ───────────────────────────
        if (Input.IsClickedThisFrame)
            GaugeValue = Mathf.Clamp01(GaugeValue + _cfg.GaugeIncreasePerClick * _cfg.DifficultyMultiplier);

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
            CurrentSlidePhase = SlidePhase.RemoveMinigame;

        UpdateUI();
    }

    public override void EndGame()
    {
        base.EndGame();
        Debug.Log($"[ShakingMinigame] Ended | Gauge={GaugeValue:F2} | Progress={TimeInZone:F2} | {CurrentState}");
    }

    public override string GetGameState()
        => $"Shaking | Gauge: {GaugeValue:P0} | Progress: {TimeInZone:F1}s/{_cfg.Duration:F1}s | {CurrentState}";

    // ── UI ─────────────────────────────────────────────────

    public override void UpdateUI()
    {
        base.UpdateUI();
        _gaugeSlider.value = GaugeValue;
        _progressSlider.value = TimeInZone / _cfg.Duration;
        UpdateTargetZoneVisual();
        UpdateProgressBarColor();
    }

    // ── FSM Hooks ──────────────────────────────────────────

    protected override void OnProcessing() => UpdateUI();

    protected override void ResetGame()
    {
        GaugeValue = 0f;
        TimeInZone = 0f;

        // Slide in only when starting fresh, not when replaying mid-session.
        CurrentSlidePhase = IsRunning ? SlidePhase.None : SlidePhase.InitMinigame;

        Debug.Log("[ShakingMinigame] Reset");
        base.ResetGame();
    }

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