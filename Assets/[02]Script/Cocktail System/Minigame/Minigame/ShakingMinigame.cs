// ============================================================
//  Bar410 — ShakingMinigame
//  GDD 3.2.1: Spam click to keep gauge inside zone for Duration.
// ============================================================

using UnityEngine;

public class ShakingMinigame : BaseMiniGame
{
    // ── Config ─────────────────────────────────────────────

    private SO_ShakingSetting _cfg => Setting as SO_ShakingSetting;

    // ── Runtime State ──────────────────────────────────────

    /// <summary>Current gauge value (0–1).</summary>
    public float GaugeValue { get; private set; }

    /// <summary>Cumulative seconds spent inside the target zone.</summary>
    public float TimeInZone { get; private set; }

    private float _timer;

    // ── FSM Handlers ───────────────────────────────────────

    protected override void OnInitialize()
    {
        GaugeValue = 0f;
        TimeInZone = 0f;
        _timer = 0f;
    }

    protected override void OnProcessing()
    {
        GaugeValue = 0f;
        TimeInZone = 0f;
        _timer = _cfg != null ? _cfg.Duration * _cfg.DifficultyMultiplier : 5f;
    }

    // ── IMinigame ──────────────────────────────────────────

    /// <summary>Call each frame to advance shaking logic.</summary>
    public override void ProcessedGame()
    {
        //show visual feedback of shaking minigame on console
        if (_timer > 0f) {
            Debug.Log($"Shaking Minigame: Gauge={GaugeValue:F2}, TimeInZone={TimeInZone:F2}s, Timer={_timer:F2}s");
        }

        base.ProcessedGame(); // polls input + running guard

        // Apply click from anywhere on screen
        if (IsClickedThisFrame)
        {
            GaugeValue += _cfg.GaugeIncreasePerClick * _cfg.DifficultyMultiplier;
            GaugeValue = Mathf.Clamp01(GaugeValue);
        }

        // Decay gauge over time
        GaugeValue -= _cfg.GaugeDecayRate * Time.deltaTime;
        GaugeValue = Mathf.Clamp01(GaugeValue);

        // Accumulate in-zone time
        if (GaugeValue >= _cfg.TargetZoneMin && GaugeValue <= _cfg.TargetZoneMax)
            TimeInZone += Time.deltaTime;

        Debug.Log($"In Target Zone: {GaugeValue >= _cfg.TargetZoneMin && GaugeValue <= _cfg.TargetZoneMax}");

        // Count down
        //_timer -= Time.deltaTime;
        //if (_timer <= 0f) Evaluate();
    }

    public override string GetGameState()
        => $"Shaking | Gauge: {GaugeValue:P0} | InZone: {TimeInZone:F1}s | Timer: {_timer:F1}s | {CurrentState}";

    // ── Private ────────────────────────────────────────────

    private void Evaluate()
    {
        float total = _cfg.Duration * _cfg.DifficultyMultiplier;
        float ratio = Mathf.Clamp01(TimeInZone / total);
        bool success = ratio >= 0.6f;

        SetState(success ? MiniGameState.Success : MiniGameState.Fail);
    }
}