// Serve scoring check, added in Stage 4. Not compiled by Unity (outside Assets).
// Paste as the body of Unity MCP execute_code while in Play mode (fresh Play session).
// Reaches step 4 through the flow commands, then ends it three ways and records what the
// scoring committed to Yarn ($task_done, $satisfaction) each time:
//   A  ServeDone() with no button (what <<flow_serve_done>> and hotkey 7 do)
//   B1 remake from Serve (must NOT commit anything)
//   B2 the real Serve button
// Writes Docs/baseline/managers/serve_commit.txt; diff it against the committed file.
var cmds = Bar410.GameFlow.GameFlowCommands.Instance;
if (cmds == null) return "no GameFlowCommands.Instance, enter Play mode first";
var root = cmds.gameObject;
var cocktail = UnityEngine.Object.FindFirstObjectByType<CocktailSystemManager>(FindObjectsInactive.Include);
var shaker = UnityEngine.Object.FindFirstObjectByType<ShakerContents>(FindObjectsInactive.Include);
var garnish = UnityEngine.Object.FindFirstObjectByType<Bar410.GameFlow.GarnishFlowBridge>();
var minigames = UnityEngine.Object.FindFirstObjectByType<MinigameSystemManager>();
var runner = UnityEngine.Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>(FindObjectsInactive.Include);
if (runner != null && runner.IsDialogueRunning) runner.Stop();
var glassGuid = AssetDatabase.FindAssets("t:SO_GlassOption");
var glass = AssetDatabase.LoadAssetAtPath<SO_GlassOption>(AssetDatabase.GUIDToAssetPath(glassGuid[0]));
var outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers/serve_commit.txt"));
System.IO.File.WriteAllText(outPath, "# serve scoring check: what each way of leaving step 4 commits to Yarn\n");
void Line(string s) { System.IO.File.AppendAllText(outPath, s + "\n"); }
UnityEngine.UI.Button Btn(string path) { var t = root.transform.Find(path); return t == null ? null : t.GetComponent<UnityEngine.UI.Button>(); }
// Serve buttons are looked up by name so the lever survives moving the panel between canvases (Stage 6).
UnityEngine.UI.Button BtnN(string n) { foreach (var t2 in root.GetComponentsInChildren<Transform>(true)) if (t2.name == n) return t2.GetComponent<UnityEngine.UI.Button>(); return null; }
string State(string label) {
  bool done; float sat, type;
  runner.VariableStorage.TryGetValue("$task_done", out done);
  runner.VariableStorage.TryGetValue("$satisfaction", out sat);
  runner.VariableStorage.TryGetValue("$type_of_cocktail", out type);
  var o = cocktail.Order;
  return label + " | step=" + Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep()
    + " scored=" + o.IsScored + " result=" + o.Result
    + " | yarn: $task_done=" + done + " $satisfaction=" + sat + " $type_of_cocktail=" + type;
}
void Pour() {
  var t = cocktail.Order.Target;
  if (t == null) return;
  foreach (var a in t.AlcoholList) shaker.TryToAddAlcohol(a.Type, a.Amount);
  foreach (var l in t.LiqueurList) shaker.TryToAddLiqueur(l.Type, l.Amount);
  foreach (var m in t.MixerList) shaker.TryToAddMixer(m.Type, m.Amount);
}
System.Collections.IEnumerator Until(System.Func<bool> cond, float seconds, string what) {
  float end = Time.realtimeSinceStartup + seconds;
  while (!cond() && Time.realtimeSinceStartup < end) yield return null;
  if (!cond()) Line("!! TIMEOUT waiting for " + what);
}
System.Collections.IEnumerator ToServe() {
  cmds.PrepareDrinks();
  yield return null;
  Pour();
  // a minigame cancelled by the previous pass plays a long outro and cannot restart until it is Idle
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Idle, 40f, "minigame Idle");
  cmds.SelectShaking();
  cmds.IngredientAdded();
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "minigame Play");
  cmds.DrinkComplete();
  yield return Until(() => Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep() == "Garnish", 5f, "Garnish");
  garnish.ChooseGlass(glass);
  garnish.Pour();
  yield return Until(() => garnish.CanFinishGarnish, 10f, "pour finished");
  garnish.TryFinishGarnish();
  yield return null; yield return null;
}
System.Collections.IEnumerator Run() {
  yield return new WaitForSeconds(1f);
  Line(State("start"));

  UnityEngine.Random.InitState(410);
  yield return ToServe();
  Line(State("A  at Serve"));
  cmds.ServeDone();
  yield return null; yield return null;
  Line(State("A  after ServeDone() with no button"));

  runner.VariableStorage.SetValue("$task_done", false);
  yield return ToServe();
  Line(State("B  at Serve"));
  BtnN("BTN_Reset (2)")?.onClick.Invoke();
  yield return null; yield return null;
  Line(State("B1 after remake from Serve"));
  yield return ToServe();
  BtnN("BTN_Serving")?.onClick.Invoke();
  yield return null; yield return null;
  Line(State("B2 after the Serve button"));
  Line("DONE");
}
cmds.StartCoroutine(Run());
return "started -> " + outPath;
