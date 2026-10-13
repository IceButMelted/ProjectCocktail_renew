// Stage 6 lever: everything the Garnish step does that flow_trace does not look at.
// Tabs, ice icons and their interactable flags, glass grid lock, garnish slot / item / rim, Pour and Finish gating,
// the Reset button, re-entry defaults and the Serve panel buttons. Every press is a real pointer click through the
// EventSystem (Button.onClick.Invoke() would skip `interactable`).
// Looks everything up by NAME so it keeps working when panels move between canvases or the bridge is split.
// Not compiled by Unity (outside Assets). Paste as the body of Unity MCP execute_code in Play mode, fresh session.
// Writes Docs/baseline/managers/garnish_ui.txt; poll for the final "DONE" line.
var cmds = Bar410.GameFlow.GameFlowCommands.Instance;
if (cmds == null) return "no GameFlowCommands.Instance, enter Play mode first";
var root = cmds.gameObject;
var cocktail = UnityEngine.Object.FindFirstObjectByType<CocktailSystemManager>(FindObjectsInactive.Include);
var shaker = UnityEngine.Object.FindFirstObjectByType<ShakerContents>(FindObjectsInactive.Include);
var minigames = UnityEngine.Object.FindFirstObjectByType<MinigameSystemManager>();
var runner = UnityEngine.Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>(FindObjectsInactive.Include);
if (runner != null && runner.IsDialogueRunning) runner.Stop();
var outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers/garnish_ui.txt"));
System.IO.File.WriteAllText(outPath, "# garnish ui behaviour, real pointer clicks\n");
void Line(string s) { System.IO.File.AppendAllText(outPath, s + "\n"); }
int errors = 0, warnings = 0; var warnTexts = new System.Collections.Generic.List<string>();
Application.LogCallback hook = (msg, st, type) => {
  if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
  else if (type == LogType.Warning) { warnings++; if (warnTexts.Count < 8) warnTexts.Add(msg.Split('\n')[0]); }
};
Application.logMessageReceived += hook;

var all = root.GetComponentsInChildren<Transform>(true);
GameObject Obj(string n) { foreach (var t in all) if (t.name.Trim() == n) return t.gameObject; return null; }
UnityEngine.UI.Button B(string n) { var o = Obj(n); return o != null ? o.GetComponent<UnityEngine.UI.Button>() : null; }
string On(string n) { var o = Obj(n); return o == null ? "missing" : (o.activeSelf ? "on" : "off"); }
string Int(string n) { var b = B(n); return b == null ? "-" : (b.interactable ? "T" : "F"); }
string Act(string n) { var o = Obj(n); return o == null ? "-" : (o.activeSelf ? "on" : "off"); }
PlacedGlassInstance Glass() { return UnityEngine.Object.FindFirstObjectByType<PlacedGlassInstance>(); }
bool CanFinish() {
  foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true)) {
    if (mb == null) continue;
    var p = mb.GetType().GetProperty("CanFinishGarnish") ?? mb.GetType().GetProperty("CanFinish");
    if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(mb);
  }
  return false;
}
string SlotSprites(PlacedGlassInstance g) {
  if (g == null) return "-";
  var f = typeof(PlacedGlassInstance).GetField("_garnishSlots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
  var slots = (SpriteRenderer[])f.GetValue(g);
  var r = typeof(PlacedGlassInstance).GetField("_garnishRim", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(g) as SpriteRenderer;
  string S(SpriteRenderer s) { return s == null ? "none" : (s.sprite != null ? s.sprite.name : "empty"); }
  return "slot0=" + S(slots[0]) + " slot1=" + S(slots[1]) + " rim=" + S(r);
}
string G(string tag) {
  var g = Glass();
  var slosh = g != null ? g.GetComponentInChildren<WaterSlosh>(true) : null;
  var grid = new[] { "BTN - ChooseHiBall", "BTN - ChooseRock", "BTN - ChooseMartini", "BTN - ChooseMagrita" };
  return tag + " | step=" + Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep()
    + " | panels: garnish=" + On("Panel - Granish UI") + " glass=" + On("Panel - Glass Selection") + " item=" + On("Panel - Garnish Item")
    + " rim=" + On("Panel - Garnish Rim") + " serve=" + On("Panel - Serve")
    + " | pour=" + Int("BTN - Pour") + " finish=" + Int("BTN - FinishGranish") + " reset=" + Int("BTN -  Reset")
    + " | ice: add=" + Act("BTN - AdddIce") + "/" + Int("BTN - AdddIce") + " remove=" + Act("BTN - RemoveIce") + "/" + Int("BTN - RemoveIce")
    + " flag=" + shaker.CurrentCocktail.AddIce
    + " | glassGrid=" + string.Concat(grid.Select(n => Int(n)))
    + " | glass=" + (g != null ? g.Option.name : "none") + " visualIce=" + (slosh != null && slosh.iceSprite != null ? slosh.iceSprite.activeSelf.ToString() : "-") + " " + SlotSprites(g)
    + " | canFinish=" + CanFinish();
}
void Click(UnityEngine.UI.Button b) {
  if (b == null) { Line("!! missing button"); return; }
  var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
  UnityEngine.EventSystems.ExecuteEvents.Execute(b.gameObject, ev, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
}
void ClickName(string n) { Click(B(n)); }
void Pour() {
  var t = cocktail.Order.Target;
  foreach (var a in t.AlcoholList) shaker.TryToAddAlcohol(a.Type, a.Amount);
  foreach (var l in t.LiqueurList) shaker.TryToAddLiqueur(l.Type, l.Amount);
  foreach (var m in t.MixerList) shaker.TryToAddMixer(m.Type, m.Amount);
}
System.Collections.IEnumerator Until(System.Func<bool> cond, float seconds, string what) {
  float end = Time.realtimeSinceStartup + seconds;
  while (!cond() && Time.realtimeSinceStartup < end) yield return null;
  if (!cond()) Line("!! TIMEOUT waiting for " + what);
}
System.Collections.IEnumerator Frames() { yield return null; yield return null; }
System.Collections.IEnumerator ToGarnish(string tag) {
  Pour(); yield return Frames();
  cmds.SelectShaking();
  cmds.IngredientAdded();
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "minigame Play " + tag);
  var active = minigames.GetMinigame(minigames.ActiveType);
  var complete = active?.GetType().GetMethod("Complete", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
  if (complete != null) complete.Invoke(active, null);
  yield return Until(() => Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep() == "Garnish", 10f, "Garnish " + tag);
  yield return Frames();
}
System.Collections.IEnumerator Run() {
  yield return new WaitForSeconds(1f);
  Line(G("G0 start"));
  UnityEngine.Random.InitState(410);
  cmds.PrepareDrinks(); yield return Frames();
  yield return ToGarnish("1");
  Line(G("G1 garnish entered"));

  // tabs
  ClickName("BTN - GarnishItem"); yield return Frames(); Line(G("G2a tab item"));
  ClickName("BTN - GarnishRim"); yield return Frames(); Line(G("G2b tab rim"));
  ClickName("BTN - GlassSelection"); yield return Frames(); Line(G("G2c tab glass"));

  // glass: pick, then swap before pouring
  ClickName("BTN - ChooseHiBall"); yield return Frames(); Line(G("G3a glass HiBall"));
  ClickName("BTN - ChooseMartini"); yield return Frames(); Line(G("G3b glass Martini (swap)"));

  // ice round trip before the pour
  ClickName("BTN - AdddIce"); yield return Frames(); Line(G("G4a ice added"));
  ClickName("BTN - RemoveIce"); yield return Frames(); Line(G("G4b ice removed"));
  ClickName("BTN - AdddIce"); yield return Frames(); Line(G("G4c ice added again"));

  // garnish item / rim: slot 1 chosen on the glass, then an item, then a rim
  var g = Glass();
  var slotButtons = g != null ? g.GetComponentsInChildren<GarnishSlotButton>(true) : new GarnishSlotButton[0];
  Line("   garnish slot buttons on glass: " + slotButtons.Length);
  ClickName("BTN - GarnishItem"); yield return Frames();
  if (slotButtons.Length > 1) slotButtons[1].OnPointerClick(null);
  var itemPanel = Obj("Panel - Garnish Item");
  var itemBtn = itemPanel != null ? itemPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(b => b.name.StartsWith("BTN - Garnish Item")) : null;
  Click(itemBtn); yield return Frames(); Line(G("G5a item on slot 1"));
  ClickName("BTN - GarnishRim"); yield return Frames();
  var rimPanel = Obj("Panel - Garnish Rim");
  var rimBtn = rimPanel != null ? rimPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(b => b.name.StartsWith("BTN")) : null;
  Click(rimBtn); yield return Frames(); Line(G("G5b rim"));
  ClickName("BTN - GlassSelection"); yield return Frames();

  // finish before pouring must be refused
  ClickName("BTN - FinishGranish"); yield return Frames(); Line(G("G6 finish before pour (refused)"));

  // pour
  ClickName("BTN - Pour"); yield return Frames(); Line(G("G7a pour pressed"));
  yield return Until(() => CanFinish(), 10f, "pour finished");
  yield return Frames(); Line(G("G7b pour finished"));
  ClickName("BTN - RemoveIce"); yield return Frames(); Line(G("G7c remove ice after pour (refused)"));
  ClickName("BTN - ChooseRock"); yield return Frames(); Line(G("G7d swap glass after pour (refused)"));

  // reset
  ClickName("BTN -  Reset"); yield return Frames();
  yield return Until(() => Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep() == "AddIngredient", 3f, "AddIngredient after reset");
  Line(G("G8 reset pressed"));

  // second pass: defaults on re-entry, then finish -> serve -> remake
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Idle, 20f, "Idle");
  cocktail.ResetCocktail(); yield return Frames();
  yield return ToGarnish("2");
  Line(G("G9 garnish re-entered"));
  ClickName("BTN - ChooseHiBall"); yield return Frames();
  ClickName("BTN - Pour"); yield return Frames();
  yield return Until(() => CanFinish(), 10f, "pour finished (2)");
  ClickName("BTN - FinishGranish"); yield return Frames();
  Line(G("G10 finish pressed"));
  Line("   serve button positions: serving=" + (B("BTN_Serving") != null ? ((RectTransform)B("BTN_Serving").transform).position.ToString("F1") : "-") + " remake=" + (B("BTN_Reset (2)") != null ? ((RectTransform)B("BTN_Reset (2)").transform).position.ToString("F1") : "-"));
  ClickName("BTN_Reset (2)"); yield return Frames();
  Line(G("G11 remake from serve"));

  Application.logMessageReceived -= hook;
  Line("console: errors=" + errors + " warnings=" + warnings);
  foreach (var w in warnTexts) Line("  warning: " + w);
  Line("DONE");
}
cmds.StartCoroutine(Run());
return "started -> " + outPath;
