// Stage 5 lever: which minigame step 2.2 starts when nothing (or something) was selected.
// Not compiled by Unity (outside Assets). Paste as the body of Unity MCP execute_code in Play mode, fresh session.
// Priority (unchanged by Stage 5, now computed in MinigameSystemManager.Resolve):
//   explicit selection  >  the drink's own method (ShakerContents.RequiredMinigame)  >  default (Shaking)
// Writes Docs/baseline/managers/resolve_priority.txt and returns at once; poll for the final "DONE" line.
// Expected, in order:
//   R0 pure   Resolve(None,None)=Shaking  Resolve(None,Stiring)=Stiring  Resolve(Shaking,Stiring)=Shaking  Resolve(Stiring,None)=Stiring
//   R1 real   nothing selected, no method          -> runs Shaking  (default),  mgType stays None
//   R2 real   nothing selected, method=Stirring    -> runs Stiring  (from drink), mgType stays None
//   R3 real   <<flow_minigame Shaking>>, method=Stirring -> runs Shaking (explicit wins), mgType=Shaking
var cmds = Bar410.GameFlow.GameFlowCommands.Instance;
if (cmds == null) return "no GameFlowCommands.Instance, enter Play mode first";
var cocktail = UnityEngine.Object.FindFirstObjectByType<CocktailSystemManager>(FindObjectsInactive.Include);
var shaker = UnityEngine.Object.FindFirstObjectByType<ShakerContents>(FindObjectsInactive.Include);
var minigames = UnityEngine.Object.FindFirstObjectByType<MinigameSystemManager>();
var runner = UnityEngine.Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>(FindObjectsInactive.Include);
if (runner != null && runner.IsDialogueRunning) runner.Stop();
var outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers/resolve_priority.txt"));
System.IO.File.WriteAllText(outPath, "# minigame type resolution\n");
void Line(string s) { System.IO.File.AppendAllText(outPath, s + "\n"); }
void Pour() {
  var t = cocktail.Order.Target;
  foreach (var a in t.AlcoholList) shaker.TryToAddAlcohol(a.Type, a.Amount);
  foreach (var l in t.LiqueurList) shaker.TryToAddLiqueur(l.Type, l.Amount);
  foreach (var m in t.MixerList) shaker.TryToAddMixer(m.Type, m.Amount);
}
System.Collections.IEnumerator Until(System.Func<bool> cond, float seconds, string what) {
  float end = Time.realtimeSinceStartup + seconds;
  while (!cond() && Time.realtimeSinceStartup < end) yield return null;
  if (!cond()) Line("!! TIMEOUT waiting for " + what);
}
System.Collections.IEnumerator Back(string tag) {
  // leave 2.2 through the HSM, wait for the outro, empty the shaker so the next case starts clean
  cmds.AnotherIngredient();
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Idle, 20f, "Idle after " + tag);
  cocktail.ResetCocktail();
  yield return null; yield return null;
}
System.Collections.IEnumerator Run() {
  yield return new WaitForSeconds(1f);
  var N = E_Cocktail.Enum_MiniGameType.None; var S = E_Cocktail.Enum_MiniGameType.Shaking; var M = E_Cocktail.Enum_MiniGameType.Stiring;
  Line("R0 pure | None,None=" + minigames.Resolve(N, N) + " None,Stiring=" + minigames.Resolve(N, M)
     + " Shaking,Stiring=" + minigames.Resolve(S, M) + " Stiring,None=" + minigames.Resolve(M, N));

  UnityEngine.Random.InitState(410);
  cmds.PrepareDrinks(); yield return null; yield return null;

  Pour(); yield return null; yield return null;
  cmds.IngredientAdded();   // nothing selected, no method
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "Play R1");
  Line("R1 default | ran=" + minigames.ActiveType + " mgType=" + Bar410.GameFlow.GameFlowCommands.Yarn_MinigameType() + " method=" + shaker.CurrentCocktail.PreparationMethod);
  yield return Back("R1");

  Pour(); yield return null; yield return null;
  shaker.SetMethod(E_Cocktail.Method.Stirring);
  cmds.IngredientAdded();   // nothing selected, method = Stirring
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "Play R2");
  Line("R2 fromDrink | ran=" + minigames.ActiveType + " mgType=" + Bar410.GameFlow.GameFlowCommands.Yarn_MinigameType() + " method=" + shaker.CurrentCocktail.PreparationMethod);
  yield return Back("R2");

  Pour(); yield return null; yield return null;
  shaker.SetMethod(E_Cocktail.Method.Stirring);
  cmds.SelectMinigame("Shaking");
  cmds.IngredientAdded();   // explicit Shaking, method = Stirring
  yield return Until(() => minigames.ActivePhase == MinigamePhase.Play, 8f, "Play R3");
  Line("R3 explicit | ran=" + minigames.ActiveType + " mgType=" + Bar410.GameFlow.GameFlowCommands.Yarn_MinigameType() + " method=" + shaker.CurrentCocktail.PreparationMethod);
  yield return Back("R3");
  Line("DONE");
}
cmds.StartCoroutine(Run());
return "started -> " + outPath;
