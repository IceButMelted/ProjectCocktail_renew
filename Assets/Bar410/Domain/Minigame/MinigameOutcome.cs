// ── Outcome ────────────────────────────────────────────────

/// <summary>
/// Why a run ended. Reported once, when the outro finishes.
///
/// Deliberately not a bool: the flow layer needs to tell "player finished the drink"
/// apart from "player backed out", because those lead to different HSM transitions.
/// </summary>
public enum MinigameOutcome
{
    /// <summary>Win condition met — the game ran to its natural end.</summary>
    Completed,

    /// <summary>Aborted before the win condition: close button, flow backtrack, or a game switch.</summary>
    Cancelled
}
