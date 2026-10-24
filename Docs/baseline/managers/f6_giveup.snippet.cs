// OBSOLETE since BG_BTN was removed (user decision 2026-10-13): the two buttons this drives no longer exist.
// Kept only as the record of how f6_giveup.txt (before) and f6_giveup.after_stage5.txt (after) were produced.
// Use back_button.snippet.cs for the minigame's remaining way out.
//
// Stage 5 lever (F6): what Give-up and Cancel-direct leave behind.
// Not compiled by Unity (outside Assets). Paste as the body of Unity MCP execute_code in Play mode, fresh session.
// Writes Docs/baseline/managers/f6_giveup.txt and returns at once; poll the file for the final "DONE" line.
//
// A: reach 2.2 (Shaking), press BG_BTN/BTN_Reset (give-up: CancelMinigame + shaker.Clear) and see where the flow is.
// B: same for BG_BTN/BTN_Next (cancel-direct: CancelMinigame only).
// After each, try the player's way forward: pour again and advance with IngredientAdded().
// The two buttons are inactive in the scene (BG_BTN activeSelf=false), so onClick is invoked directly.
var cmds = Bar410.GameFlow.GameFlowCommands.Instance;
if (cmds == null) return "no GameFlowCommands.Instance, enter Play mode first";
var root = cmds.gameObject;
var cocktail = UnityEngine.Object.FindFirstObjectByType<CocktailSystemManager>(FindObjectsInactive.Include);
var shaker = UnityEngine.Object.FindFirstObjectByType<ShakerContents>(FindObjectsInactive.Include);
var minigames = UnityEngine.Object.FindFirstObjectByType<MinigameSystemManager>();
var runner = UnityEngine.Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>(FindObjectsInactive.Include);
if (runner != null && runner.IsDialogueRunning) runner.Stop();
var methodPanel = root.GetComponentInChildren<MethodPanel>(true);
var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers"));
var tracePath = System.IO.Path.Combine(dir, "f6_giveup.txt");
System.IO.File.WriteAllText(tracePath, "# F6 trace, scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "\n");
int errors = 0, warnings = 0; var firstErrors = new System.Collections.Generic.List<string>();
Application.LogCallback hook = (msg, st, type) => {
  if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) { errors++; if (firstErrors.Count < 5) firstErrors.Add(msg.Split('\n')[0]); }
  else if (type == LogType.Warning) warnings++;
};
Application.logMessageReceived += hook;

UnityEngine.UI.Button Btn(string path) { var t = root.transform.Find(path); return t == null ? null : t.GetComponent<UnityEngine.UI.Button>(); }
string Snap(string label) {
  var gin = root.transform.Find("[CocktailSystem]/Base Spirits/BaseSpirit - Gin");
  var flat = gin != null ? gin.GetComponent<Interactable_2_5DObject>() : null;
  var src = gin != null ? gin.GetComponent<BottleIngredientSource>() : null;
  var sh3 = root.transform.Find("[CocktailSystem]/CocktailShaker");
  var shaker3D = sh3 != null ? sh3.GetComponent<Interactable_3DObject>() : null;
  return label
    + " | phase=" + Bar410.GameFlow.GameFlowCommands.Yarn_CurrentPhase()
    + " step=" + Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep()
    + " mgType=" + Bar410.GameFlow.GameFlowCommands.Yarn_MinigameType()
    + " mgResult=" + Bar410.GameFlow.GameFlowCommands.Yarn_MinigameResult()
    + " mgPhase=" + minigames.ActivePhase
    + " | shaker.parts=" + shaker.TotalParts + " method=" + shaker.CurrentCocktail.PreparationMethod
    + " | lock: bottle=" + (flat != null ? flat.Interactable.ToString() : "none")
    + " src=" + (src != null ? src.enabled.ToString() : "none")
    + " shaker3D=" + (shaker3D != null ? shaker3D.Interactable.ToString() : "none")
    + " | methodPanel=" + (methodPanel != null && methodPanel.gameObject.activeSelf ? "on" : "off");
}
void Pour() {
  var t = cocktail.Order.Target;
  if (t == null) return;
  foreach (var a in t.AlcoholList) shaker.TryToAddAlcohol(a.Type, a.Amount);
  foreach (var l in t.LiqueurList) shaker.TryToAddLiqueur(l.Type, l.Amount);
  foreach (var m in t.MixerList) shaker.TryToAddMixer(m.Type, m.Amount);
}
System.Collections.IEnumerator Log(string label) {
  yield return null; yield return null;
  System.IO.File.AppendAllText(tracePath, Snap(label) + "\n");
}
System.Collections.IEnumerator Until(System.Func<bool> cond, float seconds, string what) {
  float end = Time.realtimeSinceStartup + seconds;
  while (!cond() && Time.realtimeSinceStartup < end) yield return null;
  if (!cond()) System.IO.File.AppendAllText(tracePath, "!! TIMEOUT waiting for " + what + "\n");
}
System.Collections.IEnumerator Idle() { yield return Until(() => minigames.ActivePhase == MinigamePhase.Idle, 20f, "minigame Idle"); }
System.Collections.IEnumerator Enter22(string tag) {
  Pour();
  methodPanel.transform.Find("BTN_Shaking").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "minigame Play " + tag);
  yield return Log(tag + "_in22");
}
System.Collections.IEnumerator Run() {
  yield return new WaitForSeconds(1f);
  UnityEngine.Random.InitState(410);
  cmds.PrepareDrinks();
  yield return Log("S01_prepareDrinks");

  // A: give-up
  yield return Enter22("A");
  Btn("[MiniGameSystem]/Canvas_MiniGame/BG_BTN/BTN_Reset")?.onClick.Invoke();
  yield return Log("A1_giveUp_pressed");
  yield return Idle();
  yield return Log("A2_giveUp_outroDone");
  Pour();
  cmds.IngredientAdded();
  yield return Log("A3_pourAgain_ingredientAdded");
  cmds.AnotherIngredient();
  yield return Idle();
  yield return Log("A4_anotherIngredient_command");

  // B: cancel-direct
  yield return Enter22("B");
  Btn("[MiniGameSystem]/Canvas_MiniGame/BG_BTN/BTN_Next")?.onClick.Invoke();
  yield return Idle();
  yield return Log("B1_cancelDirect_outroDone");
  cmds.IngredientAdded();
  yield return Log("B2_ingredientAdded");

  Application.logMessageReceived -= hook;
  System.IO.File.AppendAllText(tracePath, "console: errors=" + errors + " warnings=" + warnings + "\n");
  foreach (var e in firstErrors) System.IO.File.AppendAllText(tracePath, "  error: " + e + "\n");
  System.IO.File.AppendAllText(tracePath, "DONE\n");
}
cmds.StartCoroutine(Run());
return "trace started -> " + tracePath;
