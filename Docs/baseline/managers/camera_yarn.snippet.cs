// Stage 8 lever: the camera surface, through the REAL Yarn command dispatcher and the real flow.
// Part A  Switch_Camera / Cut_Camera / Fix_Camera / Reset_Camera exactly as the .yarn files write them
//         (<<Switch_Camera CamController "MainCam">>, <<Fix_Camera CamController true>>, ...): dispatch status + what changed.
// Part B  the same commands with the camera object renamed, which is what an instance command cannot survive
//         (it finds its target with GameObject.Find(name), see HANDOFF 6.3 item 16).
// Part C  what the flow itself does to the camera: PrepareDrinks.Entered resets the look and cuts to the bar,
//         a full drink cuts to the shaker, TalkingWithCustomer.Entered fixes the look again.
// Reads the camera by type NAME ("CameraManager" after Stage 8, "CinimachineCameraSwitcher" before) so the same snippet
// runs on both sides of the change.
// Not compiled by Unity (outside Assets). Paste as the body of Unity MCP execute_code in Play mode, fresh session.
// Writes Docs/baseline/managers/camera_yarn.txt; poll for the final "DONE" line.
var cmds = Bar410.GameFlow.GameFlowCommands.Instance;
if (cmds == null) return "no GameFlowCommands.Instance, enter Play mode first";
var runner = UnityEngine.Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>(FindObjectsInactive.Include);
if (runner != null && runner.IsDialogueRunning) runner.Stop();
var cocktail = UnityEngine.Object.FindFirstObjectByType<CocktailSystemManager>(FindObjectsInactive.Include);
var shaker = UnityEngine.Object.FindFirstObjectByType<ShakerContents>(FindObjectsInactive.Include);
var garnish = UnityEngine.Object.FindFirstObjectByType<GarnishManager>();
var minigames = UnityEngine.Object.FindFirstObjectByType<MinigameSystemManager>();
var look = UnityEngine.Object.FindFirstObjectByType<CameraController>(FindObjectsInactive.Include);
MonoBehaviour mgr = null;
foreach (var m in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
  if (m.GetType().Name == "CameraManager" || m.GetType().Name == "CinimachineCameraSwitcher") { mgr = m; break; }
if (mgr == null || look == null) return "no camera switcher/controller in the scene";
var glassGuid = AssetDatabase.FindAssets("t:SO_GlassOption");
var glass = AssetDatabase.LoadAssetAtPath<SO_GlassOption>(AssetDatabase.GUIDToAssetPath(glassGuid[0]));
var outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers/camera_yarn.txt"));
System.IO.File.WriteAllText(outPath, "# camera surface through the real command dispatcher and the flow\n");
void Line(string s) { System.IO.File.AppendAllText(outPath, s + "\n"); }
int errors = 0, warnings = 0; var texts = new System.Collections.Generic.List<string>();
Application.LogCallback hook = (msg, st, type) => {
  if (type == LogType.Error || type == LogType.Exception) { errors++; if (texts.Count < 12) texts.Add("error: " + msg.Split('\n')[0]); }
  else if (type == LogType.Warning) { warnings++; if (texts.Count < 12) texts.Add("warning: " + msg.Split('\n')[0]); }
};
Application.logMessageReceived += hook;

string Active() { return (string)mgr.GetType().GetProperty("ActiveCameraId").GetValue(mgr); }
string Look() { return "cam=" + Active() + " fixed=" + look.IsFixedCamera + " dir=" + look.CurrentDirection; }
void ForceDirection(CameraController.ViewDirection d) {
  typeof(CameraController).GetField("currentDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(look, d);
}
object dispatcher = typeof(Yarn.Unity.DialogueRunner).GetProperty("CommandDispatcher", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).GetValue(runner);
var dispatch = System.Linq.Enumerable.First(dispatcher.GetType().GetInterfaces(), i => i.Name == "ICommandDispatcher").GetMethod("DispatchCommand");
string Dispatch(string text) {
  var r = dispatch.Invoke(dispatcher, new object[] { text, runner });
  var status = r.GetType().GetField("Status", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).GetValue(r);
  return "<<" + text + ">> -> " + status;
}
System.Collections.IEnumerator Frames() { yield return null; yield return null; }
System.Collections.IEnumerator Cmd(string text) { var s = Dispatch(text); yield return Frames(); Line("  " + s + " | " + Look()); }
System.Collections.IEnumerator Until(System.Func<bool> cond, float seconds, string what) {
  float end = Time.realtimeSinceStartup + seconds;
  while (!cond() && Time.realtimeSinceStartup < end) yield return null;
  if (!cond()) Line("!! TIMEOUT waiting for " + what);
}
void Pour() {
  var t = cocktail.Order.Target;
  if (t == null) return;
  foreach (var a in t.AlcoholList) shaker.TryToAddAlcohol(a.Type, a.Amount);
  foreach (var l in t.LiqueurList) shaker.TryToAddLiqueur(l.Type, l.Amount);
  foreach (var m in t.MixerList) shaker.TryToAddMixer(m.Type, m.Amount);
}
System.Collections.IEnumerator Commands() {
  yield return Cmd("Fix_Camera CamController true");
  yield return Cmd("Switch_Camera CamController CenterCam");
  yield return Cmd("Switch_Camera CamController \"MainCam\"");
  yield return Cmd("Cut_Camera CamController SummaryCam");
  yield return Cmd("Switch_Camera CamController NearLeftCam");
  yield return Cmd("Switch_Camera CamController \"MainCam\"");
  yield return Cmd("Fix_Camera CamController false");
  yield return Cmd("Fix_Camera CamController true");
  ForceDirection(CameraController.ViewDirection.Left);
  Line("  (look forced to Left)");
  yield return Cmd("Reset_Camera CamController");
  yield return Cmd("Fix_Camera CamController false");
  ForceDirection(CameraController.ViewDirection.Right);
  Line("  (look forced to Right, fixed=false)");
  yield return Cmd("Reset_Camera CamController");
  yield return Cmd("Fix_Camera CamController true");
}
System.Collections.IEnumerator Run() {
  yield return new WaitForSeconds(1f);

  Line("A the commands as the .yarn files write them");
  yield return Commands();
  Line("  unknown id:");
  yield return Cmd("Switch_Camera CamController NoSuchCam");

  Line("B the same commands with the camera object renamed");
  var go = look.gameObject; var oldName = go.name;
  go.name = oldName + "_renamed";
  yield return Commands();
  go.name = oldName;
  yield return Cmd("Switch_Camera CamController \"MainCam\"");

  Line("C what the flow does to the camera");
  yield return Cmd("Fix_Camera CamController false");
  ForceDirection(CameraController.ViewDirection.Left);
  Line("  before PrepareDrinks (fixed=false, look forced to Left) | " + Look());
  UnityEngine.Random.InitState(410);
  cmds.PrepareDrinks();
  yield return Frames();
  Line("  C1 PrepareDrinks entered | " + Look());
  Pour();
  yield return Frames();
  Line("  C2 drink full | " + Look());
  // an unfixed look follows the real mouse (it went Down when the cursor sat low in the Game view), so fix it again
  // for the rest of the loop and only unfix it right before the step that is being observed
  yield return Cmd("Fix_Camera CamController true");
  cmds.IngredientAdded();
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "minigame Play");
  cmds.DrinkComplete();
  yield return Until(() => Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep() == "Garnish", 5f, "step Garnish");
  yield return Frames();
  Line("  C3 garnish entered | " + Look());
  garnish.ChooseGlass(glass);
  garnish.Pour(shaker.CurrentCocktail);
  yield return Until(() => garnish.CanFinish, 10f, "pour finished");
  garnish.TryFinish();
  yield return Frames();
  Line("  C4 serve entered | " + Look());
  yield return Cmd("Fix_Camera CamController false");
  ForceDirection(CameraController.ViewDirection.Right);
  Line("  (fixed=false, look forced to Right)");
  cmds.ServeDone();
  yield return Until(() => Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep() == "TalkingWithCustomer", 5f, "step TalkingWithCustomer");
  yield return Frames();
  Line("  C5 talking entered | " + Look());

  Application.logMessageReceived -= hook;
  Line("console: errors=" + errors + " warnings=" + warnings);
  foreach (var t in texts) Line("  " + t);
  Line("DONE");
}
cmds.StartCoroutine(Run());
return "started -> " + outPath;
