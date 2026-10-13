// Stage 0 baseline lever: full drink loop through the real HSM + scene buttons.
// Not compiled by Unity (outside Assets). Paste as the body of Unity MCP execute_code while in Play mode,
// started from a fresh Play session so every run begins from the same state.
// Writes Docs/baseline/managers/flow_trace.txt (one line per step) and screens/NN_name.png, returns at once.
// Poll the trace file for the final "DONE" line, then diff it against the committed baseline trace.
var cmds = Bar410.GameFlow.GameFlowCommands.Instance;
if (cmds == null) return "no GameFlowCommands.Instance, enter Play mode first";
var root = cmds.gameObject;
var cocktail = UnityEngine.Object.FindFirstObjectByType<CocktailSystemManager>(FindObjectsInactive.Include);
var shaker = UnityEngine.Object.FindFirstObjectByType<ShakerContents>(FindObjectsInactive.Include);
var garnish = UnityEngine.Object.FindFirstObjectByType<Bar410.GameFlow.GarnishFlowBridge>();
var minigames = UnityEngine.Object.FindFirstObjectByType<MinigameSystemManager>();
var runner = UnityEngine.Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>(FindObjectsInactive.Include);
// The scene starts a chapter dialogue that auto-advances; stop it so only this script drives the flow.
if (runner != null && runner.IsDialogueRunning) runner.Stop();
var glassGuid = AssetDatabase.FindAssets("t:SO_GlassOption");
if (glassGuid.Length == 0) return "no SO_GlassOption asset";
var glass = AssetDatabase.LoadAssetAtPath<SO_GlassOption>(AssetDatabase.GUIDToAssetPath(glassGuid[0]));
var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers"));
var tracePath = System.IO.Path.Combine(dir, "flow_trace.txt");
var screens = System.IO.Path.Combine(dir, "screens");
System.IO.Directory.CreateDirectory(screens);
System.IO.File.WriteAllText(tracePath, "# flow trace, scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + ", resolution=" + Screen.width + "x" + Screen.height + ", chapter dialogue stopped at start\n");
int errors = 0, warnings = 0; var firstErrors = new System.Collections.Generic.List<string>();
Application.LogCallback hook = (msg, st, type) => {
  if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) { errors++; if (firstErrors.Count < 5) firstErrors.Add(msg.Split('\n')[0]); }
  else if (type == LogType.Warning) warnings++;
};
Application.logMessageReceived += hook;

string On(Transform t) { return t == null ? "missing" : (t.gameObject.activeSelf ? "on" : "off"); }
string Panel(string path) { return On(root.transform.Find(path)); }
UnityEngine.UI.Button Btn(string path) { var t = root.transform.Find(path); return t == null ? null : t.GetComponent<UnityEngine.UI.Button>(); }
string Snap(string label) {
  var o = cocktail.Order;
  var gin = root.transform.Find("[CocktailSystem]/Base Spirits/BaseSpirit - Gin");
  var flat = gin != null ? gin.GetComponent<Interactable_2_5DObject>() : null;
  var drag = gin != null ? gin.GetComponent<DragableObject>() : null;
  var src = gin != null ? gin.GetComponent<BottleIngredientSource>() : null;
  var sh3 = root.transform.Find("[CocktailSystem]/CocktailShaker");
  var shaker3D = sh3 != null ? sh3.GetComponent<Interactable_3DObject>() : null;
  var postIt = root.GetComponentInChildren<Post_It_Order>(true);
  var book = Btn("Canvas/Panel - Hub/BTN - Open_CloseBook");
  var cam = CinimachineCameraSwitcher.Instance != null ? CinimachineCameraSwitcher.Instance.ActiveCameraId : "noSwitcher";
  return label
    + " | phase=" + Bar410.GameFlow.GameFlowCommands.Yarn_CurrentPhase()
    + " step=" + Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep()
    + " mgType=" + Bar410.GameFlow.GameFlowCommands.Yarn_MinigameType()
    + " mgResult=" + Bar410.GameFlow.GameFlowCommands.Yarn_MinigameResult()
    + " mgPhase=" + minigames.ActivePhase
    + " | order.has=" + o.HasOrder + " scored=" + o.IsScored + " result=" + o.Result + " target=" + o.TargetName
    + " | shaker.parts=" + shaker.TotalParts + " ice=" + shaker.CurrentCocktail.AddIce + " method=" + shaker.CurrentCocktail.PreparationMethod
    + " | glass=" + (PlacedGlassInstance.Current != null) + " canFinish=" + garnish.CanFinishGarnish
    + " | cam=" + cam
    + " | lock: bottle=" + (flat != null ? flat.Interactable.ToString() : "none")
    + " drag=" + (drag != null ? drag.Interactable.ToString() : "none")
    + " src=" + (src != null ? src.enabled.ToString() : "none")
    + " shaker3D=" + (shaker3D != null ? shaker3D.Interactable.ToString() : "none")
    + " bookBtn=" + (book != null ? book.interactable.ToString() : "none")
    + " | method=" + Panel("Canvas/Panel - Method") + " serve=" + Panel("Canvas/Panel - Serve") + " garnish=" + Panel("Canvas/Panel - Granish UI")
    + " visWS=" + Panel("[CocktailSystem]/CocktailShaker/Canvas - World Space/PanelWS - VisurlCocktail")
    + " book=" + Panel("Canvas/Panel - BookUI") + " postit=" + On(postIt != null ? postIt.transform : null)
    + " | dialogue=" + (runner != null && runner.IsDialogueRunning);
}
void Pour() {
  var t = cocktail.Order.Target;
  if (t == null) return;
  foreach (var a in t.AlcoholList) shaker.TryToAddAlcohol(a.Type, a.Amount);
  foreach (var l in t.LiqueurList) shaker.TryToAddLiqueur(l.Type, l.Amount);
  foreach (var m in t.MixerList) shaker.TryToAddMixer(m.Type, m.Amount);
}
int n = 0;
System.Collections.IEnumerator Log(string label, bool shot = false) {
  yield return null; yield return null;
  System.IO.File.AppendAllText(tracePath, Snap(label) + "\n");
  if (shot) { ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(screens, (++n).ToString("00") + "_" + label + ".png")); yield return null; yield return null; }
}
System.Collections.IEnumerator Until(System.Func<bool> cond, float seconds, string what) {
  float end = Time.realtimeSinceStartup + seconds;
  while (!cond() && Time.realtimeSinceStartup < end) yield return null;
  if (!cond()) System.IO.File.AppendAllText(tracePath, "!! TIMEOUT waiting for " + what + "\n");
}
System.Collections.IEnumerator Run() {
  yield return new WaitForSeconds(1f);
  yield return Log("S00_start", true);

  // Entering step 2 picks the order with UnityEngine.Random in this same call, so seeding here makes the order repeatable.
  UnityEngine.Random.InitState(410);
  cmds.PrepareDrinks();
  yield return Log("S01_prepareDrinks", true);

  Pour();
  yield return Log("S02_ingredientsPoured", true);

  Btn("Canvas/Panel - Method/BTN_Shaking")?.onClick.Invoke();
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "minigame Play");
  yield return Log("S03_minigameRunning", true);

  var active = minigames.GetMinigame(minigames.ActiveType);
  var complete = active?.GetType().GetMethod("Complete", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
  if (complete != null) complete.Invoke(active, null);
  else System.IO.File.AppendAllText(tracePath, "!! no Complete() on active minigame\n");
  yield return Until(() => Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep() == "Garnish", 10f, "step Garnish after win");
  yield return Log("S04_minigameWon_autoAdvance", true);

  garnish.ChooseGlass(glass);
  garnish.ToggleIce(true);
  yield return Log("S05_glassChosen_iceOn", true);
  garnish.Pour();
  yield return Until(() => garnish.CanFinishGarnish, 10f, "pour finished");
  yield return Log("S06_poured", true);

  cmds.RemakeDrink();
  yield return Log("S07_remakeFromGarnish", true);

  Pour();
  cmds.IngredientAdded();
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "minigame Play (2)");
  cmds.DrinkComplete();
  yield return Until(() => Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep() == "Garnish", 5f, "step Garnish (2)");
  yield return Log("S08_drinkCompleteViaCommand_cancelPath");

  garnish.ChooseGlass(glass);
  garnish.Pour();
  yield return Until(() => garnish.CanFinishGarnish, 10f, "pour finished (2)");
  garnish.TryFinishGarnish();
  yield return Log("S09_serveStep", true);

  Btn("Canvas/Panel - Serve/BTN_Reset (2)")?.onClick.Invoke();
  yield return Log("S10_remakeFromServe", true);

  Pour();
  cmds.IngredientAdded();
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "minigame Play (3)");
  cmds.DrinkComplete();
  yield return Until(() => Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep() == "Garnish", 5f, "step Garnish (3)");
  garnish.ChooseGlass(glass);
  garnish.Pour();
  yield return Until(() => garnish.CanFinishGarnish, 10f, "pour finished (3)");
  garnish.TryFinishGarnish();
  yield return Log("S11_serveStep_again");

  Btn("Canvas/Panel - Serve/BTN_Serving")?.onClick.Invoke();
  yield return Until(() => cocktail.Order.IsScored, 5f, "order scored");
  yield return Log("S12_served", true);

  cmds.CloseBar();
  yield return Log("S13_closeBar", true);
  cmds.NextDay();
  yield return Log("S14_nextDay", true);

  Application.logMessageReceived -= hook;
  System.IO.File.AppendAllText(tracePath, "console: errors=" + errors + " warnings=" + warnings + "\n");
  foreach (var e in firstErrors) System.IO.File.AppendAllText(tracePath, "  error: " + e + "\n");
  System.IO.File.AppendAllText(tracePath, "DONE\n");
}
cmds.StartCoroutine(Run());
return "trace started -> " + tracePath;
