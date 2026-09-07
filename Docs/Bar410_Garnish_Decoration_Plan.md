# Garnish Decoration System (3 points per glass, UI picker) — Plan for /grill-with-docs

## Context

GDD §21 already reserves space for a glass decoration mechanic ("v1 scope: Slot-based asset
swap only — each decoration point is a fixed slot") but the current text describes clicking a
point as **cycling** through a predefined asset. The user's request is more specific: **each
glass has 3 distinct decoration points, and clicking a point opens a UI panel to choose the
decoration for that point** — a picker, not a cycle. That supersedes the GDD's current wording
and should be corrected there once this plan is confirmed.

This is also the exact gap `GarnishFlowBridge.cs` already flags in code:

> `TODO(design, plan Glass-freedom): the decoration step itself (what happens between a
> successful pour and pressing done) is undecided — for now the flow is playable end-to-end
> with no decoration mechanic; add one here once there is something to add.`

Today a glass carries exactly **one** `GarnishLook` value (`SO_GlassOption.Garnish`), assigned
implicitly when the player drags a glass off the shelf — and confirmed via code read that it
isn't even applied to any visual right now (`PlacedGlassInstance.Initialize()` never reads
`Option.Garnish`). Moving to "3 independently-decorated points, chosen after pouring" is a new
per-instance, per-point data and interaction model, not a small tweak — this plan exists to lay
out the shape of that change and surface the design calls it depends on, ahead of a
`/grill-with-docs` session to settle them.

## What already exists to build on

- **Click/hotspot pattern** — `Interactable_2_5DObject`
  (`Assets/[02]Script/BaseInteractable/Interactable_2_5DObject.cs`): click-vs-drag via pixel
  threshold, `S_Default/S_Hover/S_Clicked` sprite states, `OnClicked` UnityEvent. Established
  convention in this codebase: **one hotspot = one GameObject with its own Collider**, not
  sub-region raycasting against a single collider/mesh. `ScaleOnHover` gives free hover feedback
  on the same base.
- **Multi-slot precedent** — `FruitTrayGroup`/`FruitTraySlot`
  (`Assets/[02]Script/Cocktail System/Cocktail/Ingredients/`): a list of independent slot
  components, each owning spawn/despawn of its own child instance. Closest existing shape for
  "N independent points on one object."
- **Visual rendering ceiling** — `WaterSlosh` (`Assets/[02]Script/WaterSlosh_V1/WaterSlosh.cs`)
  only exposes glass/water/ice sprite slots today; there is no garnish-sprite slot at all.
  `BaseGlass.prefab` (`Assets/[04]Prefab/ServingGlass/`) has only `Water`/`Masking`/`Ice`
  children. Its 3 shape variants (`Hi_Ball_Glass`, `Margarita`, `Rock_Glass`) only override
  sprites/transforms — no extra GameObjects. **Decoration anchors do not exist yet in any glass
  prefab** and must be added, positioned per-shape.
- **No generic "open a UI list, pick one" component exists anywhere in the project.**
  `IngredientButtonUI` is the closest style precedent (Inspector-wired, one button = one
  pre-configured action) but is static, not a dynamic/data-driven list — a picker panel is new
  work.
- **Flow seam** — `GarnishFlowBridge`
  (`Assets/[02]Script/Hierarchical State Machine/Level 2 - Open Bar/GarnishFlowBridge.cs`)
  already gates Garnish entry/exit and the pour step (`OnGarnishEntered` unlocks the shaker,
  `OnZonePlaced` handles the pour, `TryFinishGarnish()` gates on `_pourComplete`). This is the
  natural place to enable/disable the 3 decoration hotspots, mirroring the existing shaker-unlock
  pattern — no new bridge class needed.
- **Ephemeral by design** — GDD §21.0: the placed glass is destroyed after every serve and never
  reused, so per-point garnish choices never need save/load handling. This plan carries that rule
  forward unchanged.
- **Cosmetic only** — GDD §21 confirms glass/garnish choices never affect Perfect/Acceptable/Fail
  scoring (§17-18). This plan does not touch `Domain/`, `DrinkScoringService`, or `S_Drink`.

## Recommended technical approach

1. **Data model** — Replace `SO_GlassOption.Garnish` (single `GarnishLook` field) with an
   options pool the picker draws from (e.g. `SO_GlassOption.AvailableGarnishes: GarnishLook[]`,
   or per-point pools — see open question 3). Give `PlacedGlassInstance` a small runtime
   structure holding the player's choice for *this instance*, e.g.
   `GarnishLook[3] _currentLooks` (index-per-point), reset to all-`None` on `Initialize()`.
2. **Anchors + hotspots** — Add 3 child GameObjects to `BaseGlass.prefab`
   (`GarnishPoint_1/2/3`), each with a `Collider` and a new small component in the same family as
   `Interactable_2_5DObject` (e.g. `GarnishPointHotspot`) that raises a click event carrying its
   point index. Each of the 3 shape variants (`Hi_Ball_Glass`/`Margarita`/`Rock_Glass`) needs
   these 3 anchors repositioned to fit its own silhouette — an art/prefab task, not code.
3. **Picker UI** — New `GarnishPickerUI` panel (nothing reusable exists): opened on a hotspot
   click, populated from that point's option pool, one button per `GarnishLook` value. Confirms
   into `PlacedGlassInstance.SetGarnish(pointIndex, look)`, which updates that point's own sprite
   (a new `SpriteRenderer` per anchor — same sibling-slot shape `WaterSlosh` already uses for
   glass/water/ice) and closes the panel.
4. **Flow gating** — Enable the 3 hotspots from `GarnishFlowBridge.OnZonePlaced` (right after a
   successful pour), disable on `OnGarnishExited` / glass destruction, using the existing
   `InteractableToggle.Apply` helper already used for the shaker. `TryFinishGarnish()` keeps its
   current `_pourComplete` gate unless design adds a completeness requirement (open question 4).
5. **Scoring** — No change. Garnish stays purely cosmetic per GDD §21.

## Open design questions — for the `/grill-with-docs` session

These are left open on purpose; resolving them is exactly what the grilling session should
produce (as `CONTEXT.md` vocabulary and/or ADRs where they're hard-to-reverse calls):

1. **Are the 3 points the same concept on every glass shape**, or does each shape define its own
   3 (e.g. "rim" / "liquid surface" / "side" — do these names need to hold across
   `Hi-Ball`/`Margarita`/`Rock`, or can each shape pick different points entirely)?
2. **Per-point option pools** — can any `GarnishLook` value go on any point, or is each point
   restricted (e.g. only `SaltRim`/`SugarRim` on a rim point, only `Lime`/`Olive`/`Twist` on a
   fruit point, `Umbrella` only on a top point)? This decides whether the data model is one flat
   pool or 3 distinct pools.
3. **Is finishing Garnish still allowed with 0/3 points decorated** (today's behavior — a pour is
   the only requirement), or should `TryFinishGarnish()` require some/all 3 points to be set?
4. **Can a point be changed after it's set, or reset to "None"?** (Reopening the picker on an
   already-decorated point — replace freely, or lock once chosen?)
5. **Does `SO_GlassOption` keep any garnish involvement at all** — e.g. a default look per point
   that the picker pre-fills — or is garnish now fully decoupled from glass choice, always
   starting blank on every placed instance?
6. **Content scope** — is `E_GarnishLook`'s current 6-value placeholder list
   (`Lime, SaltRim, SugarRim, Umbrella, Olive, Twist`) final, or does it grow/shrink once points
   have defined identities? Sprites for these currently don't exist and need scoping.
7. **GDD §21 correction** — once the interaction model is confirmed as "click opens a picker"
   (per this session's request, superseding the current "clicking cycles" text), update that
   paragraph so it stops contradicting the implementation.

## Files touched (representative)

- **New:** `Assets/[02]Script/Cocktail System/Cocktail/Glass/GarnishPointHotspot.cs`,
  `GarnishPickerUI.cs`
- **Modified:** `SO_GlassOption.cs`, `PlacedGlassInstance.cs`, `E_GarnishLook.cs` (if the option
  set changes shape), `GarnishFlowBridge.cs` (enable/disable hotspots on pour/exit)
- **Prefab/scene:** `BaseGlass.prefab` + its 3 shape variants (new anchor children), a new
  `Panel - Garnish Picker` UI object parented under the existing Garnish canvas alongside
  `Panel - Garnish UI`

## Verification

- Compile after each script addition via Unity MCP: `refresh_unity(mode=force, scope=all,
  compile=request, wait_for_ready=true)` then `read_console(types=["Error"])`.
- Manual play-mode pass in `New Cocktail System.unity`: place glass → pour → click each of the 3
  points → picker opens with the correct (per-question-2) option pool → pick → point's sprite
  updates → repeat for the other 2 points → `Finish Garnish` still gates correctly per
  question 3's answer.
- Re-run the existing scoring smoke checks from `Bar410_CocktailSystem_HANDOFF.md` §10
  (`DrinkScoringService(repo).Score(...)`) to confirm payout/relationship are unaffected by any
  garnish choice — this is the regression check that decoration stayed cosmetic-only.
