// ============================================================
//  MixingMinigame.cs — GDD 3.2.2
//  Click to stop the needle inside the target zone the required
//  number of times.
//
//  SOLID — S (Single Responsibility):
//    Owns mixing gameplay logic only.
//    Sliding   → PanelSlider (via base).
//    Input     → IInputProvider (via base.Input).
//    Config    → resolved once in Awake — no per-property
//                null-checks scattered through the game loop.
//
//  SOLID — L (Liskov Substitution):
//    ProcessedGame() calls base first (IsRunning guard + Input.Poll)
//    so the base contract is never violated.
//
//  SOLID — O (Open / Closed):
//    GameType property registers this game with the manager
//    registry without any change to MinigameSystemManager.
//
//  Config fallback:
//    If no SO_MixingSetting is assigned in the Inspector,
//    a default instance is created via ScriptableObject.CreateInstance.
//    The SO's own field-initialisers supply the defaults —
//    no duplicate "fallback" fields needed here.
// ============================================================

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

    // ── Inspector ──────────────────────────────────────────

    [Header("UI")]
    [Tooltip("Slider showing the moving needle (0–1). Non-interactable.")]
    [SerializeField] private Slider _needleSlider;

    [Tooltip("Slider showing hit progress (hits / requiredHits). Non-interactable.")]
    [SerializeField] private Slider _hitsProgressSlider;

    [Tooltip("Slider whose handle visually marks the target zone.")]
    [SerializeField] private Slider _targetZoneSlider;

    // ── Runtime State ──────────────────────────────────────

    public float NeedlePosition { get; private set; }
    public int Hits { get; private set; }

    private float _needleSpeed;
    private float _needleDirection = 1f; // +1 = right, -1 = left

    private float _zoneCenter;
    private float _zoneHalfSize;

    private float ZoneMin => Mathf.Max(0f, _zoneCenter - _zoneHalfSize);
    private float ZoneMax => Mathf.Min(1f, _zoneCenter + _zoneHalfSize);

    private bool _isPanelSlidingIn = false;
    private bool _isPanelSlidingOut = false;

    private float _handleOriginalHeight;

    // ── Unity Lifecycle ────────────────────────────────────

    protected override void Awake()
    {
        base.Awake(); // creates PanelSlider

        _cfg = Setting as SO_MixingSetting;
        if (_cfg == null)
        {
            _cfg = ScriptableObject.CreateInstance<SO_MixingSetting>();
            Debug.LogWarning($"[{nameof(MixingMinigame)}] No SO_MixingSetting assigned — using default values.");
        }
    }

    private void OnDestroy()
    {
        if (_cfg != null && !UnityEditor.AssetDatabase.Contains(_cfg))
            Destroy(_cfg);
    }

    // ── IMinigame ──────────────────────────────────────────

    public override void StartGame()
    {
        _handleOriginalHeight = _targetZoneSlider.handleRect.sizeDelta.y;
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
        base.ProcessedGame();        // IsRunning guard + Input.Poll()
        if (!IsRunning) return;

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

        // ── Move needle ───────────────────────────────────
        NeedlePosition += _needleSpeed * _needleDirection * dt;

        // ── Bounce at walls ───────────────────────────────
        if (NeedlePosition >= 1f) { NeedlePosition = 1f; _needleDirection = -1f; }
        else if (NeedlePosition <= 0f) { NeedlePosition = 0f; _needleDirection = 1f; }

        // ── Register click ────────────────────────────────
        if (Input.IsClickedThisFrame)
        {
            bool inZone = NeedlePosition >= ZoneMin && NeedlePosition <= ZoneMax;
            if (inZone) OnHit();
            else OnMiss();

            Debug.Log($"[MixingMinigame] {(inZone ? "HIT" : "MISS")} | " +
                      $"Needle: {NeedlePosition:P0} | Zone: [{ZoneMin:F2}–{ZoneMax:F2}] | " +
                      $"Hits: {Hits}/{_cfg.RequiredHits} | Speed: {_needleSpeed:F2}");
        }

        // ── Win condition ─────────────────────────────────
        if (Hits >= _cfg.RequiredHits)
            _isPanelSlidingOut = true;

        UpdateUI();
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
        UpdateHitsProgressVisual();
    }

    // ── Protected Hooks ────────────────────────────────────

    protected override void OnProcessing() => UpdateUI();

    protected override void ResetGame()
    {
        NeedlePosition = 0f;
        _needleDirection = 1f;
        Hits = 0;

        _needleSpeed = _cfg.NeedleInitSpeed;
        _zoneHalfSize = _cfg.TargetZoneMaxSize / 2f;

        RandomizeZonePosition();

        _isPanelSlidingIn = !IsRunning;
        _isPanelSlidingOut = false;

        Debug.Log("[MixingMinigame] Reset");
        base.ResetGame();
    }

    // ── Private: Hit / Miss ────────────────────────────────

    private void OnHit()
    {
        Hits++;
        _zoneHalfSize = Mathf.Max(
            _zoneHalfSize - _cfg.TargetZoneShrinkPerHit / 2f,
            _cfg.TargetZoneMinSize / 2f);

        _needleSpeed = Mathf.Min(
            _needleSpeed + _cfg.NeedleSpeedIncreasePerHit,
            _cfg.NeedleMaxSpeed);

        RandomizeZonePosition();
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
        _targetZoneSlider.value = _zoneCenter;

        float trackWidth = (_targetZoneSlider.transform as RectTransform).rect.width;
        float handleWidth = _zoneHalfSize * 2f * trackWidth;

        _targetZoneSlider.handleRect.sizeDelta =
            new Vector2(handleWidth, _handleOriginalHeight);
    }

    private void UpdateHitsProgressVisual()
        => _hitsProgressSlider.value = (float)Hits / _cfg.RequiredHits;
}