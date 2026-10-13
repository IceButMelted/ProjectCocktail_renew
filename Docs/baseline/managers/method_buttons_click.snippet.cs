// Stage 5 lever: can a player really press Shake / Stir / Reset once the drink is full?
// flow_trace calls Button.onClick.Invoke(), which ignores Button.interactable, so it cannot see a locked button.
// This sends a real pointer click through the EventSystem (Button.OnPointerClick checks interactable).
// Not compiled by Unity (outside Assets). Paste as the body of Unity MCP execute_code in Play mode, fresh session.
// Writes Docs/baseline/managers/method_buttons_click.txt; poll for the final "DONE" line.
// Expected: both methods start the minigame (step=Minigame, mgPhase leaves Idle), interactable=True while the panel is open.
var cmds = Bar410.GameFlow.GameFlowCommands.Instance;
if (cmds == null) return "no GameFlowCommands.Instance, enter Play mode first";
var root = cmds.gameObject;
var cocktail = UnityEngine.Object.FindFirstObjectByType<CocktailSystemManager>(FindObjectsInactive.Include);
var shaker = UnityEngine.Object.FindFirstObjectByType<ShakerContents>(FindObjectsInactive.Include);
var minigames = UnityEngine.Object.FindFirstObjectByType<MinigameSystemManager>();
var runner = UnityEngine.Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>(FindObjectsInactive.Include);
if (runner != null && runner.IsDialogueRunning) runner.Stop();
var panel = root.GetComponentInChildren<MethodPanel>(true);
var outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers/method_buttons_click.txt"));
System.IO.File.WriteAllText(outPath, "# real pointer click on the method panel buttons\n");
void Line(string s) { System.IO.File.AppendAllText(outPath, s + "\n"); }
void Pour() {
  var t = cocktail.Order.Target;
  foreach (var a in t.AlcoholList) shaker.TryToAddAlcohol(a.Type, a.Amount);
  foreach (var l in t.LiqueurList) shaker.TryToAddLiqueur(l.Type, l.Amount);
  foreach (var m in t.MixerList) shaker.TryToAddMixer(m.Type, m.Amount);
}
string State(string tag, string[] names) {
  var s = tag + " | step=" + Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep() + " mgPhase=" + minigames.ActivePhase
    + " panel=" + (panel.gameObject.activeSelf ? "on" : "off");
  foreach (var n in names) {
    var b = panel.transform.Find(n)?.GetComponent<UnityEngine.UI.Button>();
    s += " " + n + ".interactable=" + (b != null ? b.interactable.ToString() : "missing");
  }
  return s;
}
void Click(string name) {
  var go = panel.transform.Find(name).gameObject;
  var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
  UnityEngine.EventSystems.ExecuteEvents.Execute(go, ev, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
}
System.Collections.IEnumerator Until(System.Func<bool> cond, float seconds, string what) {
  float end = Time.realtimeSinceStartup + seconds;
  while (!cond() && Time.realtimeSinceStartup < end) yield return null;
  if (!cond()) Line("!! TIMEOUT waiting for " + what);
}
System.Collections.IEnumerator Run() {
  yield return new WaitForSeconds(1f);
  var names = new[] { "BTN_Shaking", "BTN_Mixing", "BTN_Reset" };
  UnityEngine.Random.InitState(410);
  cmds.PrepareDrinks(); yield return null; yield return null;
  Pour(); yield return null; yield return null;
  Line(State("M1 drink full", names));

  Click("BTN_Shaking");
  yield return null; yield return null;
  Line(State("M2 after pointer click on BTN_Shaking", names));
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "Play");

  cmds.AnotherIngredient();
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Idle, 20f, "Idle");
  yield return null; yield return null;
  Line(State("M3 back in 2.1", names));

  Click("BTN_Mixing");
  yield return null; yield return null;
  Line(State("M4 after pointer click on BTN_Mixing", names));
  Line("DONE");
}
cmds.StartCoroutine(Run());
return "started -> " + outPath;
