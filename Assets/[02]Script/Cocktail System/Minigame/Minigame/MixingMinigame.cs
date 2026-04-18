// ============================================================
//  Bar410 — MixingMinigame
//  GDD 3.2.2: Click to stop the needle in the target zone
//             the required number of times.
//
//  Logic ported from StiringMinigame.cs (MonoGame):
//    - Arrow accelerates based on its own position (not constant speed)
//    - Hit  → zone shrinks + speed increases + NEW random zone position
//    - Miss → zone grows back + speed resets to init
//    - Progress counts successful hits toward RequiredHits
//
//  SO_MixingSetting fields:
//    NeedleSpeed                — PointingArrow_InitSpeed  (base / reset speed)
//    NeedleSpeedIncreasePerHit  — PointingArrow_SpeedIncreaseRate (acceleration scalar + per-hit bonus)
//    NeedleSpeedDecreasePerHit  — unused (miss resets to base instead, matching MonoGame)
//    TargetZoneMin              — TargetZone_MinSize  (normalized 0–1)
//    TargetZoneMax              — TargetZone_MaxSize  (normalized 0–1)
//    TargetZoneDecreasePerHit   — TargetZone_DecreasePerSuccess (normalized)
//    RequiredHits               — ProgressBar_SuccessTimeToWin equivalent
// ============================================================

using UnityEngine;
using UnityEngine.UI;

using static E_Cocktail;

public class MixingMinigame : BaseMiniGame
{
    // ── Config ─────────────────────────────────────────────

    private SO_MixingSetting _cfg => Setting as SO_MixingSetting;

    // ── UI ─────────────────────────────────────────────────

    [Header("UI")]
    [Tooltip("Slider showing the moving needle (0–1). Non-interactable.")]
    [SerializeField] private Slider _needleSlider;

    [Tooltip("Slider showing hit progress (hits / requiredHits). Non-interactable.")]
    [SerializeField] private Slider _hitsProgressSlider;

    [Tooltip("Slider whose handle visually marks the target zone.")]
    [SerializeField] private Slider _targetZoneSlider;

    // ── Runtime State ──────────────────────────────────────

    /// <summary>Current needle position (0–1). Mirrors PointingArrow_CurrentValue / MaxSize.</summary>
    public float NeedlePosition { get; private set; }

    /// <summary>Successful hit count.</summary>
    public int Hits { get; private set; }

    /// <summary>Current needle speed (normalized units / sec). Modified on hit / miss.</summary>
    private float _currentSpeed;

    /// <summary>+1 moving right, -1 moving left.</summary>
    private float _needleDirection = 1f;

    /// <summary>Center of the current target zone (normalized 0–1).</summary>
    private float _zoneCenter;

    /// <summary>Current half-size of the target zone (normalized).</summary>
    private float _zoneHalfSize;

    // Derived bounds — read by UI
    private float ZoneMin => Mathf.Max(0f, _zoneCenter - _zoneHalfSize);
    private float ZoneMax => Mathf.Min(1f, _zoneCenter + _zoneHalfSize);

    // ── Slide Panel Flags (mirrors ShakingMinigame) ────────

    private bool _isPanelSlidingIn = false;
    private bool _isPanelSlidingOut = false;

    // ── Cached handle size ─────────────────────────────────

    private float _handleOriginalHeight;

    // ── Lifecycle ──────────────────────────────────────────

    public override void StartGame()
    {
        _handleOriginalHeight = _targetZoneSlider.handleRect.sizeDelta.y;

        UpdateUI();
        ResetGame();
        base.StartGame();
        Debug.Log("Mixing Minigame Started");
    }

    // ── FSM Handlers ───────────────────────────────────────

    protected override void OnProcessing()
    {
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

        float dt = Time.deltaTime;

        float accel = _cfg.GaugeSpeedIncreasePerHit; // reuse as acceleration scalar

        if (_needleDirection > 0f)
        {
            NeedlePosition += (_currentSpeed + accel * NeedlePosition) * _cfg.DifficultyMultiplier * dt;
            if (NeedlePosition >= 1f) { NeedlePosition = 1f; _needleDirection = -1f; }
        }
        else
        {
            NeedlePosition -= (_currentSpeed + accel * (1f - NeedlePosition)) * _cfg.DifficultyMultiplier * dt;
            if (NeedlePosition <= 0f) { NeedlePosition = 0f; _needleDirection = 1f; }
        }

        // ── 2. Register click ──────────────────────────────
        if (IsClickedThisFrame)
        {
            bool inZone = NeedlePosition >= ZoneMin && NeedlePosition <= ZoneMax;

            if (inZone)
            {
                OnHit();
                Debug.Log($"HIT  | Needle: {NeedlePosition:P0} | Zone: [{ZoneMin:F2}–{ZoneMax:F2}] | Hits: {Hits}/{_cfg.RequiredHits} | Speed: {_currentSpeed:F2}");
            }
            else
            {
                OnMiss();
                Debug.Log($"MISS | Needle: {NeedlePosition:P0} | Zone: [{ZoneMin:F2}–{ZoneMax:F2}] | Hits: {Hits}/{_cfg.RequiredHits} | Speed: {_currentSpeed:F2}");
            }
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
        NeedlePosition = 0f;
        _needleDirection = 1f;
        Hits = 0;

        if (_cfg != null)
        {
            _currentSpeed = _cfg.GaugeInitSpeed;
            _zoneHalfSize = _cfg.TargetZoneMax / 2f; // start at max size
        }
        else
        {
            _currentSpeed = 0.3f;
            _zoneHalfSize = 0.2f;
        }

        RandomizeZonePosition();

        if (!IsRunning)
            _isPanelSlidingIn = true;

        _isPanelSlidingOut = false;

        Debug.Log("Mixing Minigame Reset");
        base.ResetGame();
    }

    // ── Debug ──────────────────────────────────────────────

    public override string GetGameState()
        => $"Mixing | Needle: {NeedlePosition:P0} | Zone: [{ZoneMin:F2}–{ZoneMax:F2}] | Hits: {Hits}/{_cfg?.RequiredHits} | Speed: {_currentSpeed:F2} | {CurrentState}";

    // ── Private Hit / Miss ─────────────────────────────────

    /// <summary>
    /// Successful hit — mirrors StiringMinigame hit branch:
    ///   zone shrinks, speed increases, NEW random zone position.
    /// </summary>
    private void OnHit()
    {
        Hits++;

        // Shrink zone (clamp to min size)
        float minHalf = _cfg.TargetZoneMin / 2f;
        _zoneHalfSize -= _cfg.TargetZoneDecreasePerHit / 2f;
        _zoneHalfSize = Mathf.Max(_zoneHalfSize, minHalf);

        // Speed up (capped — reuse NeedleSpeedDecreasePerHit field as cap)
        _currentSpeed += _cfg.GaugeSpeedIncreasePerHit;
        _currentSpeed = Mathf.Min(_currentSpeed, _cfg.GaugeMaxSpeed); // cap

        // Move zone to a new random position
        RandomizeZonePosition();
    }

    /// <summary>
    /// Missed click — mirrors StiringMinigame miss branch:
    ///   zone grows back toward max, speed resets to base, new random zone.
    /// </summary>
    private void OnMiss()
    {
        // Grow zone back (clamp to max size)
        float maxHalf = _cfg.TargetZoneMax / 2f;
        _zoneHalfSize = Mathf.Min(_zoneHalfSize + (_cfg.TargetZoneDecreasePerHit), maxHalf);

        // Reset speed to base
        _currentSpeed = _cfg.GaugeInitSpeed;

        // Randomize zone so the player can't just retry the same spot
        RandomizeZonePosition();
    }

    /// <summary>
    /// Picks a new random center for the zone, keeping it fully within [0, 1].
    /// Mirrors InitNewTargetZone() in StiringMinigame.
    /// </summary>
    private void RandomizeZonePosition()
    {
        float minCenter = _zoneHalfSize;
        float maxCenter = 1f - _zoneHalfSize;

        if (minCenter > maxCenter) minCenter = maxCenter; // safety clamp

        _zoneCenter = Random.Range(minCenter, maxCenter);
    }

    // ── Private UI Helpers ─────────────────────────────────

    private void UpdateNeedleVisual()
    {
        _needleSlider.value = NeedlePosition;
    }

    /// <summary>
    /// Positions the target zone handle at the zone center and resizes its
    /// width to cover the current zone — same technique as ShakingMinigame.
    /// </summary>
    private void UpdateTargetZoneVisual()
    {
        _targetZoneSlider.value = _zoneCenter;

        float trackWidth = (_targetZoneSlider.transform as RectTransform).rect.width;
        float handleWidth = (_zoneHalfSize * 2f) * trackWidth;

        RectTransform handle = _targetZoneSlider.handleRect;
        handle.sizeDelta = new Vector2(handleWidth, _handleOriginalHeight);
    }

    private void UpdateHitsProgressVisual()
    {
        if (_cfg == null) return;
        _hitsProgressSlider.value = (float)Hits / _cfg.RequiredHits;
    }
}