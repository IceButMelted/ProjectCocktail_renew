// ============================================================
//  ShakingMinigame.cs — GDD 3.2.1
//  Spam-click to keep the gauge inside the target zone for the
//  full duration.
//
//  SOLID — S (Single Responsibility):
//    Owns shaking gameplay logic only.
//    Sliding   → PanelSlider (via base).
//    Input     → IInputProvider (via base.Input).
//    Config    → resolved once in Awake — no per-property
//                null-checks scattered through the game loop.
//
//  SOLID — L (Liskov Substitution):
//    ProcessedGame() calls base first (IsRunning guard + Input.Poll)
//    so the base contract is never violated.
//    IsRunning check is NOT duplicated here.
//
//  SOLID — O (Open / Closed):
//    GameType property registers this game with the manager
//    registry without any change to MinigameSystemManager.
//
//  Config fallback:
//    If no SO_ShakingSetting is assigned in the Inspector,
//    a default instance is created via ScriptableObject.CreateInstance.
//    The SO's own field-initialisers supply the defaults —
//    no duplicate "fallback" fields needed in this class.
// ============================================================

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

    private bool _isPanelSlidingIn = false;
    private bool _isPanelSlidingOut = false;

    private float _handleOriginalWidth;

    // ── Unity Lifecycle ────────────────────────────────────

    protected override void Awake()
    {
        base.Awake(); // creates PanelSlider

        // Resolve config once — avoids null checks in the hot path.
        _cfg = Setting as SO_ShakingSetting;
        if (_cfg == null)
        {
            _cfg = ScriptableObject.CreateInstance<SO_ShakingSetting>();
            Debug.LogWarning($"[{nameof(ShakingMinigame)}] No SO_ShakingSetting assigned — using default values.");
        }
    }

    private void OnDestroy()
    {
        // Only destroy if it was runtime-created (not an asset reference).
        if (_cfg != null && !UnityEditor.AssetDatabase.Contains(_cfg))
            Destroy(_cfg);
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
        base.ProcessedGame();        // IsRunning guard + Input.Poll()
        if (!IsRunning) return;      // early-out after guard

        // ── Panel slide in ────────────────────────────────
        if (_isPanelSlidingIn)
        {
            if (!PanelSlider.Slide(Direction.Up, SlideFinishCondition.FullyIn)) return;
            _isPanelSlidingIn = false;
        }

        // ── Panel slide out ───────────────────────────────
        if (_isPanelSlidingOut)
        {
            if (!PanelSlider.Slide(Direction.Down, SlideFinishCondition.FullyOut)) return;
            _isPanelSlidingOut = false;
            SetState(MiniGameState.Success);
            return;
        }

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
            _isPanelSlidingOut = true;

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

    // ── Protected Hooks ────────────────────────────────────

    protected override void ResetGame()
    {
        GaugeValue = 0f;
        TimeInZone = 0f;

        _isPanelSlidingIn = !IsRunning; // slide in only on first start
        _isPanelSlidingOut = false;

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