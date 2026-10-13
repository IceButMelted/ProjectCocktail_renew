---
status: accepted
---

# Glass and pour move from drag-and-drop to a fixed position + buttons

The "Glass Freedom" feature let the player drag a glass from a shelf onto a placement zone, then
drag the mixing shaker onto that glass to pour. We're reversing that interaction model: the
serving glass now spawns already placed at a fixed spot, chosen from a UI list instead of dragged;
the shaker is likewise fixed beside that spot, and pouring happens via a button press instead of a
drag. The camera is also locked to a single forward angle for the whole game
(`CameraController.IsFixedCamera`), unrelated to the separate `CinimachineCameraSwitcher` used for
dialogue camera cuts.

This is a deliberate reversal of recently-built drag-and-drop work, not an oversight — glass
choice, ice, and garnish decoration are being consolidated into one UI-driven Garnish step with no
spatial dragging at all, consistent with removing bar-layout dragging entirely (see
[0001](./0001-remove-prepare-phase-fixed-roster.md)). `GlassShelfSlot`, the drag-handling half of
`GlassPlacementZone`, and `PlacedGlassInstance`'s `DragableObject` requirement become obsolete and
should be replaced with a direct "instantiate at a fixed transform" + button-driven flow.
