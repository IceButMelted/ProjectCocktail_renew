// Stage 9a lever: the shell chrome, with real pointer clicks.
// Book: open (BTN - Open_CloseBook), next page, previous page, close (BTN_Close); order post-it toggle button;
// then ONE of the two scene-load buttons (EndShiftButton or Panel_Thx/Image/Button), chosen with `which` below.
// Clicking a scene-load button really asks for the loading scene, so it is the last thing the snippet does: the
// target scene name is read in the same frame and the file is closed before Unity switches scene. Run it twice
// from a fresh Play session, once per button.
// The flow is taken to step 2.1 first (the book button is locked while the customer talks).
// Buttons are found by NAME so the snippet survives moving them (Canvas -> Canvas_Shell) and who owns the listener.
// Not compiled by Unity (outside Assets). Paste as the body of Unity MCP execute_code in Play mode.
// Writes Docs/baseline/managers/shell_ui.<which>.txt; poll for the final "DONE" line.
var which = "endshift";   // "endshift" or "thanks"
var cmds = Bar410.GameFlow.GameFlowCommands.Instance;
if (cmds == null) return "no GameFlowCommands.Instance, enter Play mode first";
var root = cmds.gameObject;
var runner = UnityEngine.Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>(FindObjectsInactive.Include);
if (runner != null && runner.IsDialogueRunning) runner.Stop();
var book = UnityEngine.Object.FindFirstObjectByType<BookUI_V2>(FindObjectsInactive.Include);
var postIt = UnityEngine.Object.FindFirstObjectByType<Post_It_Order>(FindObjectsInactive.Include);
var outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers/shell_ui." + which + ".txt"));
System.IO.File.WriteAllText(outPath, "# shell chrome, real pointer clicks (" + which + ")\n");
void Line(string s) { System.IO.File.AppendAllText(outPath, s + "\n"); }
Transform FindDeep(Transform from, string n) { foreach (var t in from.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
GameObject Find(string n) { var t = FindDeep(root.transform, n); return t == null ? null : t.gameObject; }
void PointerClick(GameObject go) {
  var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
  UnityEngine.EventSystems.ExecuteEvents.Execute(go, ev, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
}
string BookState() { return "book.active=" + book.gameObject.activeSelf + " IsOpen=" + book.IsOpen + " spread=" + book.CurrentSpreadIndex; }
string PostItState() { return "postIt.visible=" + postIt.IsPostItVisible + " outScreen=" + postIt.IsOutScreen; }
System.Collections.IEnumerator Frames() { yield return null; yield return null; }
System.Collections.IEnumerator Run() {
  yield return new WaitForSeconds(1f);
  var open = Find("BTN - Open_CloseBook"); var next = Find("BTN_NextPage"); var prev = Find("BTN_PreviousPage"); var close = Find("BTN_Close");
  var toggle = postIt.GetComponentInChildren<UnityEngine.UI.Button>(true);
  Line("found: open=" + (open != null) + " next=" + (next != null) + " prev=" + (prev != null) + " close=" + (close != null) + " postItButton=" + (toggle != null));
  Line("U0 start | " + BookState() + " | " + PostItState());
  // the book button is locked while the customer talks: step 2.1 is where the player can use the chrome
  UnityEngine.Random.InitState(410);
  cmds.PrepareDrinks(); yield return Frames();
  Line("U0b step 2.1 | " + BookState() + " | " + PostItState() + " | openInteractable=" + open.GetComponent<UnityEngine.UI.Button>().interactable);

  PointerClick(open); yield return Frames();           Line("U1 click open     | " + BookState());
  PointerClick(next); yield return Frames();           Line("U2 click next     | " + BookState());
  PointerClick(next); yield return Frames();           Line("U3 click next     | " + BookState());
  PointerClick(prev); yield return Frames();           Line("U4 click previous | " + BookState());
  PointerClick(close); yield return Frames();          Line("U5 click close    | " + BookState());
  PointerClick(open); yield return Frames();           Line("U6 click open     | " + BookState());
  PointerClick(close); yield return Frames();          Line("U7 click close    | " + BookState());

  PointerClick(toggle.gameObject); yield return Frames(); Line("U8 post-it toggle  | " + PostItState());
  PointerClick(toggle.gameObject); yield return Frames(); Line("U9 post-it toggle  | " + PostItState());

  // the scene-load button: only the requested scene name can be read before Unity leaves this scene
  var btn = which == "thanks" ? Find("Panel_Thx") : Find("EndShiftButton");
  var go = which == "thanks" ? FindDeep(btn.transform, "Button").gameObject : btn;
  var wasActive = new System.Collections.Generic.List<GameObject>();
  for (var t = go.transform; t != null && t != root.transform; t = t.parent) if (!t.gameObject.activeSelf) { wasActive.Add(t.gameObject); t.gameObject.SetActive(true); }
  yield return Frames();
  SceneLoaderBridge.TargetScene = "(untouched)";
  PointerClick(go);
  Line("U10 click " + which + " | TargetScene=" + SceneLoaderBridge.TargetScene);
  Line("DONE");
}
cmds.StartCoroutine(Run());
return "started -> " + outPath;
