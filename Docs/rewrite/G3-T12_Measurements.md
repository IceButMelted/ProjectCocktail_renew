# G3-T12 — R3 measurements (Main_V2 vs the old scene)

Measured 2026-11-09 in the **Linux Editor** (Play mode, Game view, Mono). Not a player build, not the Unity Profiler.

**Verdict**
- **fps gate (≥ 60): unproven.** No build and no Profiler. Main_V2 is faster than the old scene in every step in
  the Editor, but neither reaches 60 in the Minigame / Garnish / Serve steps.
- **Hitch gate (first entry ≤ 300 ms): passed in the Editor** (worst frame 61 ms).
- **Click through the RenderTexture: proven by the code path only** (EventSystem raycast + `WorldView` relay with
  a synthesized pointer event). No real mouse click was made.

## Method
A throwaway probe (deleted, never committed) ran the **same code in both scenes**: it labels each frame with
`HubManager.CurrentPhase`, records `Time.unscaledDeltaTime`, and drives one drink itself with in-game timers, so the
Unity MCP calls (which stall the Editor for 0.3–0.7 s) stay out of the measured windows. The drink: 10 × the same
mixer, Shaking, minigame forced to Completed after 6 s of play, first glass, pour, finish, serve.
Old scene: `GameFlowCommands.OpenBar` + `PrepareDrinks` (random order; the day-1 dialogue keeps running).
Main_V2: StubStory "customer arrives". fps windows are 3 s.

## Numbers

| | Main_V2 | Old scene (`New Drag Drop System`) |
|---|---|---|
| fps, Talking | 56–60 | 33–38 |
| fps, AddIngredient | 53–58 | 31–34 |
| fps, Minigame | 41–42 | 30–31 |
| fps, Garnish | 32 | 29–30 |
| fps, Serve | 32–33 | 30 |
| First Minigame entry (frame after entry / worst of next 30) | 25.4 / 51.3 ms | 38.2 / 44.5 ms |
| First Garnish entry | 25.0 / 60.6 ms | 28.0 / 48.2 ms |
| First Serve entry | 25.5 / 46.1 ms | 33.1 / 53.4 ms |

Likely reasons Main_V2 is faster: the Stirring, Shaking and GarnishStage cameras render only while their view is
shown (the old scene renders them every frame), and Main_V2 runs no dialogue. Not profiled.

Startup (earlier run, same Editor): UI-scene preload (load + register + bind + warm, both scenes) finished
6.8–7.4 s after the first scene load. The `Panel_Transition` opening clip is 1.33 s, and the day started (`Part` =
Event) at 7.6–8.3 s. The old scene starts the day right after the transition. **The preload is not hidden behind the
transition:** `GameRoot.Start` awaits `PreloadAsync` before `Day.BootAsync`. This is unknown in a build.

## Raycast checks (Main_V2, Garnish step)
- **Through the RenderTexture:** `EventSystem.RaycastAll` at the screen point of garnish slot 1 → top UI hit
  `GlassView`; `WorldView` relay → `GarnishSlotButton` (the active slot became 1). Code path, synthesized pointer event.
- **Decoy excluded by the click mask:** a cube on layer `PlacementZone` placed halfway between the stage camera and
  slot 1. An unmasked `Physics.Raycast` hits the decoy (`Cube`); `WorldViewHandle.TryPick` (mask Default|Draggable)
  hits `Granish_Topping_2`. **Pass.**
- Both checks activated slot 1 by hand: the slot objects ship inactive in the glass prefabs (open owner question,
  Contracts_Final §6).

## Not measured
- Player build (IL2CPP or Mono), Windows, the Profiler, GC allocations.
- Real mouse or pad, `timeScale 0` (Pause).
- Visual framing of the minigame inset and the garnish glass.
