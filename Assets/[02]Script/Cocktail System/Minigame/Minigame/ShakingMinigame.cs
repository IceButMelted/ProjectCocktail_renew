// ============================================================
//  ShakingMinigame — fixed
// ============================================================
using UnityEngine;

public class ShakingMinigame : BaseMiniGame
{
    // No constructor — MonoBehaviour lifecycle only

    private SO_ShakingSetting _cfg => Setting as SO_ShakingSetting;

    public float GaugeValue { get; private set; }
    public float TimeInZone { get; private set; }

    protected override void OnProcessing()
    {
        // Don't reset here — OnInitialize already ran.
        // Only reset if you explicitly want a mid-game restart.
    }

    public override void ProcessedGame()
    {
        if (!IsRunning) return; // explicit guard after base
        // 1. Run base FIRST: sets IsClickedThisFrame, guards !IsRunning
        base.ProcessedGame();
        
        Debug.Log($"Shaking Minigame: Gauge={GaugeValue:F2}, TimeInZone={TimeInZone:F2}s");
        Debug.Log($"Max: {_cfg.TargetZoneMax} | Min: {_cfg.TargetZoneMin}");

        // 2. Click input
        if (IsClickedThisFrame)
        {
            GaugeValue += _cfg.GaugeIncreasePerClick * _cfg.DifficultyMultiplier;
            GaugeValue = Mathf.Clamp01(GaugeValue);
        }

        // 3. Decay
        GaugeValue -= _cfg.GaugeDecayRate * Time.deltaTime;
        GaugeValue = Mathf.Clamp01(GaugeValue);

        // 4. Zone accumulation
        bool inZone = GaugeValue >= _cfg.TargetZoneMin && GaugeValue <= _cfg.TargetZoneMax;
        if (inZone)
            TimeInZone += Time.deltaTime;

        Debug.Log($"In Target Zone: {inZone}");

        // 5. Win condition
        if (TimeInZone >= _cfg.Duration)
            SetState(MiniGameState.Success);
    }

    public override void EndGame()
    {
        base.EndGame();
        Debug.Log($"Shaking Minigame Ended: Final Gauge={GaugeValue:F2}, Total TimeInZone={TimeInZone:F2}s, Result={CurrentState}");
        ResetGame(); // Ensure we reset after ending, so next start is fresh
    }

    public override void UpdateUI()
    {
        base.UpdateUI();
    }

    public override string GetGameState()
        => $"Shaking | Gauge: {GaugeValue:P0} | InZone: {TimeInZone:F1}s | {CurrentState}";

    protected override void ResetGame() { 
        base.ResetGame();
        GaugeValue = 0f;
        TimeInZone = 0f;
        Debug.Log("Shaking Minigame Reset");
    }
}