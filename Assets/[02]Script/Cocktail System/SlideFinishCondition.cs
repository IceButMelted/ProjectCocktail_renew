/// <summary>Which geometric condition marks the end of a slide.</summary>
public enum SlideFinishCondition
{
    // ── Boundary ───────────────────────────────────────────
    FullyIn,               // trailing edge clears the boundary it entered from
    FullyOut,              // leading edge clears the boundary it exits through

    // ── Edge → boundary ────────────────────────────────────
    LeftEdgeToLeftBound,
    RightEdgeToRightBound,
    TopEdgeToTopBound,
    BottomEdgeToBottomBound,

    // ── Edge → center ──────────────────────────────────────
    CenterToCenter,
    LeftEdgeToCenter,
    RightEdgeToCenter,
    TopEdgeToCenter,
    BottomEdgeToCenter,

    // ── Edge → opposite boundary (exit) ────────────────────
    BottomEdgeToTopBound,
    TopEdgeToBottomBound,
    LeftEdgeToRightBound,
    RightEdgeToLeftBound,
}
