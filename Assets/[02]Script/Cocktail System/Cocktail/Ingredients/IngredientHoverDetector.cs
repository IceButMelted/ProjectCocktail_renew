// ============================================================
//  IngredientHoverDetector.cs — Is the mouse currently over the
//  mixing vessel? Shared by every BottleIngredientSource /
//  FruitPieceInstance so each doesn't run its own scene lookup.
//
//  Deliberately NOT a PlacementZoneBase — that continuously re-clamps
//  the dragged object's position onto the zone every frame, which
//  visually jittered when the shaker was registered as a zone. This
//  is a plain raycast against N_InputManager's existing "what's under
//  the mouse" query (same one used to pick up a DragableObject),
//  independent of the placement/snap-back system entirely.
// ============================================================

using UnityEngine;

public static class IngredientHoverDetector
{
    private static N_InputManager _inputManager;

    /// <summary>The ShakerContents currently under the mouse, or null.</summary>
    public static ShakerContents ResolveHoveredShaker()
    {
        if (_inputManager == null) _inputManager = Object.FindFirstObjectByType<N_InputManager>();
        if (_inputManager == null) return null;

        var hovered = _inputManager.GetObjectMouseHover();
        return hovered != null ? hovered.GetComponentInParent<ShakerContents>() : null;
    }
}
