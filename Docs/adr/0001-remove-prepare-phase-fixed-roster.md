---
status: accepted
---

# Remove the Prepare phase; the ingredient roster is always fully available

The game had a `GamePhase.Prepare` step (`PrepareBarPhase`, `BarSetupBridge`) where the player
dragged ingredient bottles onto the bar counter, and whatever ended up placed became that day's
roster (`IngredientButtonGroup.SetRoster`). We removed this phase entirely: every ingredient is
available every day, and the day loop runs `Open → Close → Open` directly with no `Prepare` state.

This follows from a broader decision to drop free-roaming, drag-based spatial arrangement from
the game (see [0002](./0002-glass-pour-fixed-position-buttons.md)) — once there's no bar layout to
arrange, a daily roster choice added complexity with no remaining design payoff. `PrepareBarPhase.cs`,
`BarSetupBridge.cs`, and their scene wiring are dead code as of this decision.
