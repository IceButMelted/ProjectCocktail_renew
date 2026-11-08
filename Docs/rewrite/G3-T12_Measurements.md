# G3-T12 — R3 measurements (Main_V2, UI scenes)

Measured 2026-11-09 in the **Linux Editor** (Play mode, Game view, Mono), not in a player build.
Tool: a throwaway probe that recorded `Time.unscaledDeltaTime` per frame and flow timestamps
(deleted after measuring). Not the Unity Profiler. Input was driven by code (button `onClick`,
synthesized pointer events); no real mouse or pad. Steps were triggered by in-game timers so the
Unity MCP calls (which stall the Editor for 0.3–0.7 s) stayed out of the measured windows.

## Numbers

| What | Result | Gate |
|---|---|---|
| First entry into Minigame (frame after entry / worst of next 30 frames) | 22.7 ms / 50.6 ms | ≤ 300 ms: **pass** |
| First entry into Garnish | 25.6 ms / 63.5 ms | ≤ 300 ms: **pass** |
| First entry into Serve | 32.5 ms / 66.0 ms | ≤ 300 ms: **pass** |
| fps, Talking / AddIngredient | 54–56 fps | ≥ 60: fail (Editor) |
| fps, Minigame | 38–43 fps | ≥ 60: fail (Editor) |
| fps, Garnish / Serve | 28–33 fps | ≥ 60: fail (Editor) |
| UI-scene preload (both scenes, load + register + bind + warm) | 6.8–7.4 s after the first scene load (harness: 2.5–4.8 s) | — |
| `Panel_Transition` opening clip | 1.33 s | — |
| Day start (`Part` = Event) | 7.6–8.3 s | old scene: right after the 1.33 s transition |
| Click through the RenderTexture (Main_V2, Garnish) | `EventSystem.RaycastAll` at the slot's screen point → top hit `GlassView`; `WorldView` relay → `GarnishSlotButton` (active slot became 1) | **pass** |

## Findings

1. **The preload is not hidden behind the transition.** `GameRoot.Start` awaits `PreloadAsync` before
   `Day.BootAsync`, so the day starts ~6 s later than the old scene in the Editor. Unknown in a build.
   Options for the decision point: start the preload in parallel with the transition (await both),
   or load the UI scenes during `LoadingScene`.
2. **fps is below 60 in every step in the Editor**, lowest in Garnish/Serve (an extra camera renders the
   glass stage into a RenderTexture while the main camera renders the bar). No old-scene baseline was measured
   (the old scene renders the same stage camera all the time). A player-build measurement is needed before
   judging the gate.
3. First-entry hitches are well under 300 ms after the preload warm-up.
4. Raycast through the RenderTexture works with the mouse path (`EventSystem` → `WorldView` → world
   collider, click mask Default|Draggable). The garnish slot objects ship inactive in the glass prefabs
   (open owner question in Contracts_Final §6), so this was checked with one slot activated by the test.

## Not measured
- Player build (IL2CPP or Mono), Windows, the Profiler, GC allocations.
- Real mouse / pad, `timeScale 0` (Pause).
- Visual framing of the minigame inset and the garnish glass.
