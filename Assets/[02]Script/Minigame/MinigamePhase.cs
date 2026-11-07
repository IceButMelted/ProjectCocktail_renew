// ── Phase ──────────────────────────────────────────────────

/// <summary>
/// The only state machine a minigame has.
/// Runs as a straight line: Idle → Intro → Play → Outro → Idle.
///
/// Intro/Outro are owned by the Animator (see MinigamePanelAnimator);
/// Play is the only phase that ticks gameplay or accepts input.
/// </summary>
public enum MinigamePhase
{
    /// <summary>Nothing on screen. Panels sit at their hidden Animator pose.</summary>
    Idle,

    /// <summary>Intro clip playing. No input, no gameplay tick.</summary>
    Intro,

    /// <summary>Active gameplay. Input is polled and OnTick(dt) runs every frame.</summary>
    Play,

    /// <summary>Outro clip playing. When it finishes the game reports its result and returns to Idle.</summary>
    Outro
}
