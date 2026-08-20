// ============================================================
//  IInputProvider.cs — Input abstraction for minigames.
//
//  SOLID — I (Interface Segregation):
//    A focused, single-method interface. Minigames only need to
//    ask "was there a click this frame?" — nothing else.
//
//  SOLID — D (Dependency Inversion):
//    BaseMiniGame depends on IInputProvider, not on
//    Input.GetMouseButtonDown directly.  The default Unity
//    implementation is injected automatically; swap it in tests
//    or for touch/gamepad without touching any minigame class.
//
//  SOLID — O (Open / Closed):
//    Add touch input, gamepad input, or replay input by writing
//    a new IInputProvider — nothing existing changes.
// ============================================================

using UnityEngine;

// ── Interface ──────────────────────────────────────────────

/// <summary>
/// Provides per-frame click/tap state to a minigame.
/// Call Poll() once per frame, then read IsClickedThisFrame.
/// </summary>
public interface IInputProvider
{
    /// <summary>True only on the frame Poll() detected a primary click/tap.</summary>
    bool IsClickedThisFrame { get; }

    /// <summary>Samples input for this frame. Call once per Update.</summary>
    void Poll();
}

// ── Default Unity Implementation ───────────────────────────

/// <summary>
/// Standard mouse / touch implementation.
/// Injected by BaseMiniGame when no alternative is provided.
/// </summary>
public class UnityInputProvider : IInputProvider
{
    public bool IsClickedThisFrame { get; private set; }

    /// <summary>Reads left-mouse or first touch. Safe to call every frame.</summary>
    public void Poll()
        => IsClickedThisFrame = Input.GetMouseButtonDown(0);
}
