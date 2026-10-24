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
_Avoid_: Confusing with the ice cubes drawn in the glass (`PlacedGlassInstance.ApplyIce`) — that's the visual of this attribute, not a separate setting.

**Pour**:
The Garnish action that copies the shaker's finished drink onto the placed glass. A prerequisite for finishing Garnish.

**Rim Garnish** (`SO_GarnishRimOption`):
A whole-rim treatment on the placed glass (e.g. salt or sugar rim). At most one per glass; picked from its own grid.

**Garnish Slot**:
One of the 2 positions on a placed glass that holds a Garnish Item (`PlacedGlassInstance`, clicked through `GarnishSlotButton`). Clicking a slot makes it the target of the next Garnish Item pick. Positions are authored per glass shape.
_Avoid_: `FruitTraySlot` — unrelated (the ingredient tray in Prepare Drinks).

**Garnish Item** (`SO_GarnishItemOption`):
The decoration placed in a Garnish Slot (e.g. a lime wedge, an umbrella). Tagged **Fresh** (citrus, herbs, fresh produce) or **Novelty** by `GarnishCategory`; the tag only filters the picker UI and does not restrict which slot an item goes in.

### Rules

- Any Rim Garnish or Garnish Item can be replaced or cleared at any time — no lock-on-first-pick.
- Finishing the Garnish step only needs the Pour; no decoration is required.
- Both Garnish Slots share one pool; the same item may appear in both.
- Glass choice (`SO_GlassOption`) carries no garnish involvement — every placed glass starts undecorated, regardless of which glass was picked.
- Decoration is cosmetic: it is not scored.
- Glass choice, Ice, and Garnish decoration are all decided during the Garnish step only — never during Prepare Drinks.
