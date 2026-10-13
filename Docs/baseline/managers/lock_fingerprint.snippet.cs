// Ingredient lock fingerprint, added in Stage 4a. Not compiled by Unity (outside Assets).
// Paste as the body of Unity MCP execute_code while in Play mode (fresh Play session).
// For every IngredientButtonUI object plus the shaker and the recipe-book button it records the
// state each lock writer leaves behind, after each step that has a different writer:
//   Talking (start), AddIngredient entry, Yarn Enable_InteractableObject false/true,
//   ResetCocktail, drink full (ShakerContents.OnFulled), AddIngredient exit, Garnish entry,
//   remake (ShakerContents.Cleared).
// Also records how many ingredient buttons already know their ShakerContents (no first-use Find).
// Writes Docs/baseline/managers/lock_fingerprint.txt; diff it against the committed file.
var cmds = Bar410.GameFlow.GameFlowCommands.Instance;
if (cmds == null) return "no GameFlowCommands.Instance, enter Play mode first";
var root = cmds.gameObject;
var cocktail = UnityEngine.Object.FindFirstObjectByType<CocktailSystemManager>(FindObjectsInactive.Include);
var shaker = UnityEngine.Object.FindFirstObjectByType<ShakerContents>(FindObjectsInactive.Include);
var minigames = UnityEngine.Object.FindFirstObjectByType<MinigameSystemManager>();
var runner = UnityEngine.Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>(FindObjectsInactive.Include);
if (runner != null && runner.IsDialogueRunning) runner.Stop();
var outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers/lock_fingerprint.txt"));
System.IO.File.WriteAllText(outPath, "# ingredient lock fingerprint: name:btn/2.5D/3D/drag/source (T true, F false, - no component)\n");
void Line(string s) { System.IO.File.AppendAllText(outPath, s + "\n"); }
string B(bool? v) => v == null ? "-" : (v.Value ? "T" : "F");
string Sig(GameObject go) {
  var btn = go.GetComponent<UnityEngine.UI.Button>();
  var f = go.GetComponent<Interactable_2_5DObject>();
  var s = go.GetComponent<Interactable_3DObject>();
  var d = go.GetComponent<DragableObject>();
  var src = go.GetComponent<BottleIngredientSource>();
  return go.name + ":" + B(btn != null ? btn.interactable : (bool?)null) + B(f != null ? f.Interactable : (bool?)null)
    + B(s != null ? s.Interactable : (bool?)null) + B(d != null ? d.Interactable : (bool?)null) + B(src != null ? src.enabled : (bool?)null);
}
var tracked = new System.Collections.Generic.List<GameObject>();
foreach (var ui in UnityEngine.Object.FindObjectsByType<IngredientButtonUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)) tracked.Add(ui.gameObject);
tracked.Add(shaker.gameObject);
var book = root.transform.Find("Canvas_Shell/Panel - Hub/BTN - Open_CloseBook");
if (book != null) tracked.Add(book.gameObject);
tracked = tracked.Distinct().OrderBy(g => g.name).ToList();
void Snap(string label) {
  Line(label + " | " + string.Join(" ", tracked.Select(Sig)));
}
int Bound() {
  int n = 0;
  var f = typeof(IngredientButtonUI).GetField("_shaker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
  foreach (var ui in UnityEngine.Object.FindObjectsByType<IngredientButtonUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
    if (f.GetValue(ui) != null) n++;
  return n;
}
System.Collections.IEnumerator Until(System.Func<bool> cond, float seconds, string what) {
  float end = Time.realtimeSinceStartup + seconds;
  while (!cond() && Time.realtimeSinceStartup < end) yield return null;
  if (!cond()) Line("!! TIMEOUT waiting for " + what);
}
System.Collections.IEnumerator Run() {
  yield return new WaitForSeconds(1f);
  var total = UnityEngine.Object.FindObjectsByType<IngredientButtonUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
  Line("buttons that already hold their ShakerContents at start: " + Bound() + " of " + total);
  Snap("L0 talking (start)");

  UnityEngine.Random.InitState(410);
  cmds.PrepareDrinks(); yield return null; yield return null;
  Snap("L1 AddIngredient entered");

  cocktail.EnableButtonInYarn(false); yield return null;
  Snap("L2 Enable_InteractableObject false");
  cocktail.EnableButtonInYarn(true); yield return null;
  Snap("L3 Enable_InteractableObject true");
  cocktail.ResetCocktail(); yield return null;
  Snap("L4 ResetCocktail");

  var t = cocktail.Order.Target;
  foreach (var a in t.AlcoholList) shaker.TryToAddAlcohol(a.Type, a.Amount);
  foreach (var l in t.LiqueurList) shaker.TryToAddLiqueur(l.Type, l.Amount);
  foreach (var m in t.MixerList) shaker.TryToAddMixer(m.Type, m.Amount);
  yield return null; yield return null;
  Snap("L5 drink full (OnFulled)");

  cmds.SelectShaking();
  cmds.IngredientAdded();
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "minigame Play");
  Snap("L6 AddIngredient exited (minigame)");

  cmds.DrinkComplete();
  yield return Until(() => Bar410.GameFlow.GameFlowCommands.Yarn_CurrentStep() == "Garnish", 5f, "Garnish");
  yield return null;
  Snap("L7 Garnish entered");

  cmds.RemakeDrink(); yield return null; yield return null;
  Snap("L8 remake (Cleared)");
  Line("DONE");
}
cmds.StartCoroutine(Run());
return "started -> " + outPath;
