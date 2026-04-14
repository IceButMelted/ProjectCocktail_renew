// ============================================================
//  Bar410 — MixingMinigame
//  GDD 3.2.2: Click to stop the needle in the target zone
//             the required number of times.
// ============================================================

using UnityEngine;

public class MixingMinigame : BaseMiniGame
{
    // ── Config ─────────────────────────────────────────────

    private SO_MixingSetting _cfg => Setting as SO_MixingSetting;

    // ── Runtime State ──────────────────────────────────────

    /// <summary>Current needle position (0–1).</summary>
    public float NeedlePosition { get; private set; }
    public int Hits   { get; private set; }

    private float _needleDirection = 1f; // bounces between 0 and 1

    // ── FSM Handlers ───────────────────────────────────────

    protected override void OnProcessing()
    {
        NeedlePosition   = 0f;
        Hits             = 0;
        _needleDirection = 1f;
    }

    // ── IMinigame ──────────────────────────────────────────

    public override void ProcessedGame()
    {
        if (!IsRunning) return;// explicit guard after base
        base.ProcessedGame(); // polls input + running guard

        //visual on console for information
        Debug.Log($"Needle: {NeedlePosition:P0} | Hits: {Hits}/{_cfg.RequiredHits}");
        Debug.Log($"Target Zone: {_cfg.TargetZoneMin:P0} - {_cfg.TargetZoneMax:P0}");

        // Move needle
        float speed    = _cfg.NeedleSpeed * _cfg.DifficultyMultiplier;
        NeedlePosition += _needleDirection * speed * Time.deltaTime;

        // Bounce at edges
        if (NeedlePosition >= 1f) { NeedlePosition = 1f; _needleDirection = -1f; }
        if (NeedlePosition <= 0f) { NeedlePosition = 0f; _needleDirection =  1f; }

        // Register click from anywhere on screen
        if (IsClickedThisFrame)
        {
            bool inZone = NeedlePosition >= _cfg.TargetZoneMin &&
                          NeedlePosition <= _cfg.TargetZoneMax;

            if (inZone) Hits++;

            // End when all attempts are used
            if (Hits >= _cfg.RequiredHits)
            {
                SetState(MiniGameState.Success);
                
            }
        }
    }

    public override void EndGame()
    {
        base.EndGame(); // sets IsRunning = false
        Debug.Log($"Mixing Minigame Ended: Hits={Hits}");
    }

    public override void UpdateUI()
    {
        base.UpdateUI();
    }

    public override string GetGameState()
        => $"Mixing | Needle: {NeedlePosition:P0} | Hits: {Hits}/{_cfg?.RequiredHits} | {CurrentState}";
}
