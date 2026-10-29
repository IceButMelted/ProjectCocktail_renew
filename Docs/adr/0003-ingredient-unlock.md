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

## Owner answers (2026-10-29)

- Random orders only pick recipes the player can make now; writer-named orders are not filtered (warning only).
- Unlock days come from the owner's table: day 1 (8 ingredients), 3, 5, 7 (all 17). Plan: `Docs/Bar410_IngredientUnlock_Plan.md`.
- Each unlock is announced with a popup and a sound, through the existing `EventPopupQueue`.
- Unlocks are permanent; there is no re-lock.
- One popup per day ("Unlock New Ingredient" + the ingredient names) in BeforeOpen › Event; a story unlock waits for the next day's Event. No popup on day 1. No sound file yet.
