---
status: proposed
---

# Ingredients unlock over the story; supersedes "always fully available" in 0001

[0001](./0001-remove-prepare-phase-fixed-roster.md) made every ingredient available every day. We now
start the player with a subset and unlock the rest either on a day number (`$day >= N`, set per
ingredient in an Inspector table) or by a Yarn command (`<<unlock_ingredient X>>`). Locked bottles
are hidden on the bar. The recipe book shows a locked ingredient as `???` and hides all amounts
until every ingredient of that recipe is unlocked.

0001's removal of the Prepare phase still stands: there is still no per-day selection step, the
player never chooses the roster, it only grows.

One component (`IngredientUnlocks`) is the single source of truth. Day unlocks are derived from
`$day` (already saved with Yarn data); only story unlocks are stored, in C#, and saved in their own
`SaveData` section.
