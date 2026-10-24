// Stage 7 lever: the customer / Yarn surface, through the REAL Yarn command dispatcher.
// Part A  NPC commands (SetEmotion, SetLookDirection, MoveIn, MoveOut) and the cocktail instance commands
//         (<<Enable_InteractableObject SystemGame false>>, <<Reset_Variable SystemGame>>, <<wait_for_task SystemGame>>):
//         dispatch status + what actually changed.
// Part B  save -> load of the NPCs through SaveLoadManager.CollectNPCData / ApplyNPCData.
// Part C  who writes the Yarn variables when an order is scored ($task_done, $satisfaction, $type_of_cocktail, $rel_*).
// Not compiled by Unity (outside Assets). Paste as the body of Unity MCP execute_code in Play mode, fresh session.
// Writes Docs/baseline/managers/customer_yarn.txt; poll for the final "DONE" line.
var cmds = Bar410.GameFlow.GameFlowCommands.Instance;
if (cmds == null) return "no GameFlowCommands.Instance, enter Play mode first";
var runner = UnityEngine.Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>(FindObjectsInactive.Include);
if (runner != null && runner.IsDialogueRunning) runner.Stop();
var cocktail = UnityEngine.Object.FindFirstObjectByType<CocktailSystemManager>(FindObjectsInactive.Include);
var outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers/customer_yarn.txt"));
System.IO.File.WriteAllText(outPath, "# customer / yarn surface through the real command dispatcher\n");
void Line(string s) { System.IO.File.AppendAllText(outPath, s + "\n"); }
int warnings = 0; var warns = new System.Collections.Generic.List<string>();
Application.LogCallback hook = (msg, st, type) => {
  if (type == LogType.Warning || type == LogType.Error) { warnings++; if (warns.Count < 12) warns.Add(type + ": " + msg.Split('\n')[0]); }
};
Application.logMessageReceived += hook;

NPC_Base Npc(string n) { foreach (var x in UnityEngine.Object.FindObjectsByType<NPC_Base>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (x.Name.ToString() == n) return x; return null; }
string State(string n) {
  var x = Npc(n);
  if (x == null) return n + ":missing";
  var sr = x.SpriteRenderer != null ? x.SpriteRenderer : x.GetComponentInChildren<SpriteRenderer>();
  return n + ":sprite=" + (sr != null && sr.sprite != null ? sr.sprite.name : "-") + " pos=" + x.transform.position.ToString("F2") + " idx=" + x.CurrentWaypointIndex;
}
string All() { return State("Walter") + " | " + State("Owen") + " | " + State("Isla"); }
// DialogueRunner.CommandDispatcher is internal, so it is reached by reflection; it is the same call the runner makes for a <<command>> line.
object dispatcher = typeof(Yarn.Unity.DialogueRunner).GetProperty("CommandDispatcher", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).GetValue(runner);
var dispatch = System.Linq.Enumerable.First(dispatcher.GetType().GetInterfaces(), i => i.Name == "ICommandDispatcher").GetMethod("DispatchCommand");   // explicit interface implementation
string Dispatch(string text) {
  var r = dispatch.Invoke(dispatcher, new object[] { text, runner });
  var rt = r.GetType();
  var status = rt.GetField("Status", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).GetValue(r);   // internal struct field
  return "<<" + text + ">> -> " + status;
}
System.Collections.IEnumerator Frames() { yield return null; yield return null; }
System.Collections.IEnumerator Until(System.Func<bool> cond, float seconds, string what) {
  float end = Time.realtimeSinceStartup + seconds;
  while (!cond() && Time.realtimeSinceStartup < end) yield return null;
  if (!cond()) Line("!! TIMEOUT waiting for " + what);
}
// a move is a coroutine; wait until the NPC has stood still for 3 frames so positions are comparable between runs
System.Collections.IEnumerator Settle(string n) {
  yield return null;
  var last = Npc(n).transform.position; int still = 0; float end = Time.realtimeSinceStartup + 10f;
  while (still < 3 && Time.realtimeSinceStartup < end) { yield return null; var p = Npc(n).transform.position; still = (p == last) ? still + 1 : 0; last = p; }
}
System.Collections.IEnumerator Cmd(string text) {
  Line("  " + Dispatch(text));
  yield return Frames();
}
System.Collections.IEnumerator Run() {
  yield return new WaitForSeconds(1f);
  // The scene's chapter dialogue runs until this snippet stops it, so how far it got (it moves and dresses Isla) depends
  // on how long the editor took to get here. Put every NPC back where the scene starts so runs are comparable.
  foreach (var npc in UnityEngine.Object.FindObjectsByType<NPC_Base>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
    if (npc.Name == E_Cocktail.NPC_Name.None) continue;
    npc.StopAllCoroutines();
    npc.CurrentWaypointIndex = -1;
    npc.transform.position = npc.Name == E_Cocktail.NPC_Name.Isla ? new Vector3(2.77f, 0.91f, 7.66f) : new Vector3(2.77f, 0.80f, 7.66f);
    var sr = npc.SpriteRenderer != null ? npc.SpriteRenderer : npc.GetComponentInChildren<SpriteRenderer>();
    if (sr != null && npc._neutralSprite != null) sr.sprite = npc._neutralSprite;
  }
  yield return Frames();
  Line("A0 initial | " + All());

  Line("A1 emotion"); yield return Cmd("SetEmotion Walter happy"); Line("   " + State("Walter"));
  yield return Cmd("SetEmotion Walter upset"); Line("   " + State("Walter"));
  yield return Cmd("SetEmotion Walter neutral"); Line("   " + State("Walter"));
  yield return Cmd("SetEmotion Nobody happy");

  Line("A2 look direction"); yield return Cmd("SetLookDirection Walter left"); Line("   " + State("Walter"));
  yield return Cmd("SetLookDirection Walter right"); Line("   " + State("Walter"));
  yield return Cmd("SetLookDirection Walter forward"); Line("   " + State("Walter"));
  yield return Cmd("SetLookDirection Walter NPC_Name.up");

  Line("A3 movement");
  yield return Cmd("MoveIn Walter 2");
  yield return Settle("Walter");
  Line("   after MoveIn Walter 2 | " + State("Walter"));
  yield return Cmd("MoveIn Isla 1");
  yield return Settle("Isla");
  Line("   after MoveIn Isla 1 | " + State("Isla"));
  yield return Cmd("MoveIn Walter 9"); Line("   after MoveIn Walter 9 (out of range) | " + State("Walter"));
  yield return Cmd("MoveOut Walter");
  yield return Settle("Walter");
  Line("   after MoveOut Walter | " + State("Walter"));

  Line("A4 cocktail commands addressed by object name");
  yield return Cmd("Enable_InteractableObject SystemGame false");
  yield return Cmd("Enable_InteractableObject [CocktailSystem] false");
  yield return Cmd("Reset_Variable SystemGame");
  var wait = Dispatch("wait_for_task SystemGame");
  Line("  " + wait);
  yield return Frames();
  Line("   IsWaitingForTask after wait_for_task SystemGame = " + CocktailSystemManager.IsWaitingForTask);
  // release whatever is parked, so the next part starts clean
  cocktail.Order.Score(RecipeMatch.None, E_Cocktail.Satisfaction.Acceptable, E_Cocktail.TypeOfCocktail.LowAlcohol, 0f, 0f);
  yield return Frames();
  yield return Until(() => !CocktailSystemManager.IsWaitingForTask, 3f, "task released");
  cocktail.Order.Clear();

  Line("B save -> load of the NPCs");
  yield return Cmd("MoveIn Walter 3");
  yield return Settle("Walter");
  yield return Cmd("SetLookDirection Walter left");
  var slm = UnityEngine.Object.FindFirstObjectByType<SaveLoadManager>(FindObjectsInactive.Include);   // [SaveLoadSystem] is inactive in the dev scene; its methods still run
  if (slm == null) { Line("!! no SaveLoadManager in the scene"); }
  else {
    Line("B1 live state | " + All());
    slm.CollectNPCData();
    var sdF = typeof(SaveLoadManager).GetField("_saveData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var sd = sdF.GetValue(slm);
    var dict = sd.GetType().GetProperty("NPCData") != null ? sd.GetType().GetProperty("NPCData").GetValue(sd) : sd.GetType().GetField("NPCData").GetValue(sd);
    var d = (System.Collections.IDictionary)dict.GetType().GetField("npcDataDict").GetValue(dict);
    var keys = new System.Collections.Generic.List<string>();
    foreach (System.Collections.DictionaryEntry e in d) {
      var v = e.Value;
      var t = v.GetType();
      keys.Add(e.Key + ":pos=" + ((Vector3)t.GetField("position").GetValue(v)).ToString("F2") + " idx=" + t.GetField("CurrentWayPointIndex").GetValue(v) + " look=" + t.GetField("CurrentLookDirection").GetValue(v));
    }
    keys.Sort();
    Line("B2 collected: " + string.Join(" | ", keys));
    // disturb the live state, then load it back
    var w = Npc("Walter"); w.transform.position = new Vector3(0, 0, 0); w.CurrentWaypointIndex = 0;
    var i = Npc("Isla"); i.transform.position = new Vector3(1, 1, 1);
    yield return Frames();
    Line("B3 disturbed | " + All());
    slm.ApplyNPCData();
    yield return Frames();
    Line("B4 restored | " + All());
  }

  Line("C yarn variables written when an order is scored");
  cocktail.Order.Clear();
  runner.VariableStorage.SetValue("$task_done", false);
  runner.VariableStorage.SetValue("$satisfaction", 0f);
  runner.VariableStorage.SetValue("$type_of_cocktail", 0f);
  float rel0; runner.VariableStorage.TryGetValue("$rel_Walter", out rel0);
  UnityEngine.Random.InitState(410);
  var drink = cocktail.RandomCocktail();
  cocktail.Order.BeginOrder(E_Cocktail.NPC_Name.Walter, OrderMode.RandomByPreferenceName, drink, AlcoholClassifier.Resolve(drink));
  var shaker = UnityEngine.Object.FindFirstObjectByType<ShakerContents>(FindObjectsInactive.Include);
  var tg = cocktail.Order.Target;
  foreach (var a in tg.AlcoholList) shaker.TryToAddAlcohol(a.Type, a.Amount);
  foreach (var l in tg.LiqueurList) shaker.TryToAddLiqueur(l.Type, l.Amount);
  foreach (var m in tg.MixerList) shaker.TryToAddMixer(m.Type, m.Amount);
  shaker.SetMethod(tg.PreparationMethod);
  cocktail.UpdateCocktailInShaker();
  cocktail.ServeDrink();
  yield return Frames();
  bool done; float sat, typ, rel1;
  runner.VariableStorage.TryGetValue("$task_done", out done);
  runner.VariableStorage.TryGetValue("$satisfaction", out sat);
  runner.VariableStorage.TryGetValue("$type_of_cocktail", out typ);
  runner.VariableStorage.TryGetValue("$rel_Walter", out rel1);
  Line("   scored=" + cocktail.Order.IsScored + " result=" + cocktail.Order.Result + " relDelta=" + cocktail.Order.RelationshipDelta
       + " | yarn $task_done=" + done + " $satisfaction=" + sat + " $type_of_cocktail=" + typ + " $rel_Walter " + rel0 + " -> " + rel1);
  cocktail.ServeDrink(); yield return Frames();
  float rel2; runner.VariableStorage.TryGetValue("$rel_Walter", out rel2);
  Line("   second ServeDrink is a no-op: $rel_Walter stays " + rel2);
  cocktail.ResetVariableInYarn();
  yield return Frames();
  runner.VariableStorage.TryGetValue("$task_done", out done);
  runner.VariableStorage.TryGetValue("$satisfaction", out sat);
  Line("   after ResetVariableInYarn: $task_done=" + done + " $satisfaction=" + sat + " orderHas=" + cocktail.Order.HasOrder);

  Application.logMessageReceived -= hook;
  Line("console: warnings+errors=" + warnings);
  foreach (var w2 in warns) Line("  " + w2);
  Line("DONE");
}
cmds.StartCoroutine(Run());
return "started -> " + outPath;
