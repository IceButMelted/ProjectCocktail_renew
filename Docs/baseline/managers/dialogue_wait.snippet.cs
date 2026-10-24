// Stage 7d lever: does a REAL dialogue node park on <<wait_for_task SystemGame>> until the drink is served?
// Starts the Day 1 order node, advances its lines like a player, and looks at CocktailSystemManager.IsWaitingForTask
// and whether the dialogue is still running once the order has been placed and again after the order is scored.
// Before Stage 7d every <<... SystemGame ...>> command failed in the real scene (no object with that name), so the
// node ran straight through: IsWaitingForTask stayed False and the dialogue kept going.
// Not compiled by Unity (outside Assets). Paste as the body of Unity MCP execute_code in Play mode, fresh session.
// Writes Docs/baseline/managers/dialogue_wait.txt; poll for the final "DONE" line.
var cmds = Bar410.GameFlow.GameFlowCommands.Instance;
if (cmds == null) return "no GameFlowCommands.Instance, enter Play mode first";
var runner = UnityEngine.Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>(FindObjectsInactive.Include);
var cocktail = UnityEngine.Object.FindFirstObjectByType<CocktailSystemManager>(FindObjectsInactive.Include);
if (runner.IsDialogueRunning) runner.Stop();
var outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers/dialogue_wait.txt"));
System.IO.File.WriteAllText(outPath, "# a real order node against wait_for_task\n");
void Line(string s) { System.IO.File.AppendAllText(outPath, s + "\n"); }
int errors = 0; var errTexts = new System.Collections.Generic.List<string>();
Application.LogCallback hook = (msg, st, type) => {
  if (type == LogType.Error || type == LogType.Exception) { errors++; if (errTexts.Count < 6) errTexts.Add(msg.Split('\n')[0]); }
};
Application.logMessageReceived += hook;
string Where() { return "hasOrder=" + cocktail.Order.HasOrder + " target='" + cocktail.Order.TargetName + "' isWaitingForTask=" + CocktailSystemManager.IsWaitingForTask
  + " dialogueRunning=" + runner.IsDialogueRunning + " lastLine=" + SceneLoaderBridge.CurrentLineId; }
System.Collections.IEnumerator AutoAdvance() {
  while (runner.IsDialogueRunning) { runner.RequestNextLine(); yield return new WaitForSeconds(0.1f); }
}
System.Collections.IEnumerator Run() {
  yield return new WaitForSeconds(1f);
  cocktail.Order.Clear();
  runner.StartDialogue("D1_02_Walter_Order");
  cmds.StartCoroutine(AutoAdvance());
  float end = Time.realtimeSinceStartup + 15f;
  while (!cocktail.Order.HasOrder && Time.realtimeSinceStartup < end) yield return null;
  Line("W0 order placed by the node | " + Where());
  yield return new WaitForSeconds(3f);
  Line("W1 three seconds later, nobody served | " + Where());

  cocktail.Order.Score(RecipeMatch.None, E_Cocktail.Satisfaction.Acceptable, E_Cocktail.TypeOfCocktail.LowAlcohol, 0f, 0f);
  yield return new WaitForSeconds(3f);
  Line("W2 order scored | " + Where());

  if (runner.IsDialogueRunning) runner.Stop();
  Application.logMessageReceived -= hook;
  Line("console: errors=" + errors);
  foreach (var e in errTexts) Line("  error: " + e);
  Line("DONE");
}
cmds.StartCoroutine(Run());
return "started -> " + outPath;
