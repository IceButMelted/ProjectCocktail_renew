// Stage 5 lever: the minigame's back button end to end, with a real pointer click.
// Button -> MinigameSystemManager.BackRequested -> MinigameFlowBridge -> AnotherIngredient() -> HSM 2.2 -> 2.1.
// Not compiled by Unity (outside Assets). Paste as the body of Unity MCP execute_code in Play mode, fresh session.
// Writes Docs/baseline/managers/back_button.txt; poll for the final "DONE" line.
// Expected: B1 step=Minigame, mgPhase leaves Idle; B2 step=AddIngredient, drink kept (parts=10), shaker3D=True after the outro,
//           B3 a second run starts (step=Minigame) so the flow is not stuck.
var cmds = Bar410.GameFlow.GameFlowCommands.Instance;
if (cmds == null) return "no GameFlowCommands.Instance, enter Play mode first";
var root = cmds.gameObject;
var cocktail = UnityEngine.Object.FindFirstObjectByType<CocktailSystemManager>(FindObjectsInactive.Include);
var shaker = UnityEngine.Object.FindFirstObjectByType<ShakerContents>(FindObjectsInactive.Include);
var minigames = UnityEngine.Object.FindFirstObjectByType<MinigameSystemManager>();
var runner = UnityEngine.Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>(FindObjectsInactive.Include);
if (runner != null && runner.IsDialogueRunning) runner.Stop();
var panel = root.GetComponentInChildren<MethodPanel>(true);
var outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers/back_button.txt"));
System.IO.File.WriteAllText(outPath, "# minigame back button, real pointer click\n");
void Line(string s) { System.IO.File.AppendAllText(outPath, s + "\n"); }
void Pour() {
  var t = cocktail.Order.Target;
  foreach (var a in t.AlcoholList) shaker.TryToAddAlcohol(a.Type, a.Amount);
  foreach (var l in t.LiqueurList) shaker.TryToAddLiqueur(l.Type, l.Amount);
  foreach (var m in t.MixerList) shaker.TryToAddMixer(m.Type, m.Amount);
}
string State(string tag) {
  var sh3 = root.transform.Find("[CocktailSystem]/CocktailShaker")?.GetComponent<Interactable_3DObject>();
  return tag + " | step=" + Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep() + " mgPhase=" + minigames.ActivePhase
    + " mgResult=" + Bar410.GameFlow.GameFlowCommands.Yarn_MinigameResult() + " parts=" + shaker.TotalParts
    + " shaker3D=" + (sh3 != null ? sh3.Interactable.ToString() : "none") + " methodPanel=" + (panel.gameObject.activeSelf ? "on" : "off");
}
void PointerClick(GameObject go) {
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
  UnityEngine.Random.InitState(410);
  cmds.PrepareDrinks(); yield return null; yield return null;
  Pour(); yield return null; yield return null;
  PointerClick(panel.transform.Find("BTN_Shaking").gameObject);
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "Play");
  Line(State("B1 minigame running"));

  var back = root.transform.Find("[MiniGameSystem]/Canvas_MiniGame/Canvas_ShakingMinigame/BTN - Cancel Minigame");
  if (back == null) { Line("!! no BTN - Cancel Minigame"); Line("DONE"); yield break; }
  PointerClick(back.gameObject);
  yield return null; yield return null;
  Line(State("B2a right after back click"));
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Idle, 20f, "Idle");
  yield return null; yield return null;
  Line(State("B2b outro finished"));

  PointerClick(panel.transform.Find("BTN_Mixing").gameObject);
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "Play (2)");
  Line(State("B3 second run"));
  Line("DONE");
}
cmds.StartCoroutine(Run());
return "started -> " + outPath;
