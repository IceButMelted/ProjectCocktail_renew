# Bar410

A bartending simulation: the player mixes and serves cocktails to customers across a scripted game loop (order → mix → pour → garnish → serve).

## Language

### Day Loop

**Roster** (always-available):
The set of ingredients the player can pour from. Fixed and complete every day — there is no per-day selection step and no notion of an ingredient being "unavailable today."
_Avoid_: Bar Layout, Prepare — these named the drag-and-drop bar-setup step that decided the roster in an earlier design; that step no longer exists.

### Glass & Garnish

**Ice** (`AddIce`):
A yes/no recipe attribute, decided by the player during the Garnish step. Distinct from `GarnishLook` decoration — Ice is matched against the customer's order (`IceMatch`) and factors into `Satisfaction` (a `Perfect` result requires both `MethodMatch` and `IceMatch`), so it is not purely cosmetic the way Glass choice and Garnish decoration are.
_Avoid_: Confusing with the glass's decorative ice sprite (`SO_GlassOption.IceSprite`) — that's baked-in art, not this recipe attribute.

**Pour**:
The Garnish action that copies the shaker's finished drink onto the placed glass. A prerequisite for finishing Garnish.

**GarnishPoint**:
A single position on a placed glass that holds one `GarnishLook` value, chosen by the player via a picker UI after pouring. Every glass shape carries the same set of `GarnishPoint`s, at shape-specific positions.
_Avoid_: Slot, decoration slot — "slot" already names the old (superseded) fixed-cycle mechanic in the GDD, and also names the unrelated `FruitTraySlot` concept.

**Rim**:
The `GarnishPoint` category for decorations on the glass's rim (e.g. salt/sugar rim). Exactly 1 per glass.

**Side**:
The `GarnishPoint` category for decorations on the glass's side (e.g. a fruit wedge). Exactly 1 per glass.

**Topping**:
The `GarnishPoint` category for decorations on top of the glass (e.g. umbrella, cherry). Exactly 2 per glass — the only category with more than one point.

**GarnishLook**:
The specific decoration asset assigned to a `GarnishPoint` (e.g. `Lime`, `Umbrella`). Distinct from the `GarnishPoint` it's assigned to — a `GarnishLook` is content, a `GarnishPoint` is a position. Each category (`Rim`/`Side`/`Topping`) has its own restricted pool of valid `GarnishLook` values — a value valid for one category is not offered at another category's picker.

### Rules

- A `GarnishPoint` can be re-decorated freely after being set (including back to `None`) — no lock-on-first-pick.
- Finishing the Garnish step never requires any `GarnishPoint` to be set — 0 of 4 decorated is valid, same as today.
- The 2 `Topping` points are interchangeable: no fixed sub-identity, same pool for both, duplicate `GarnishLook` values allowed across them.
- Glass choice (`SO_GlassOption`) carries no garnish involvement — every placed glass starts with all 4 `GarnishPoint`s at `None`, regardless of which glass was picked.
- Glass choice, Ice, and Garnish decoration are all decided during the Garnish step only — never during Prepare Drinks.
