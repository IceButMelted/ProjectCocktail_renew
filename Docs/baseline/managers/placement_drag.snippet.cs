// Placement drag check, added in Stage 3. Not compiled by Unity (outside Assets).
// Paste as the body of Unity MCP execute_code while in Play mode. Drives DragableObject through its
// public pointer handlers with PointerEventData (no real mouse), so it covers the path that
// DragableObject -> N_PlacementSystem.Current -> StartDrag/ReleaseObject takes, plus IngredientHoverDetector.
// Writes Docs/baseline/managers/placement_drag.txt; diff it against the committed file.
var cmds = Bar410.GameFlow.GameFlowCommands.Instance;
if (cmds == null) return "no GameFlowCommands.Instance, enter Play mode first";
var root = cmds.gameObject;
var runner = UnityEngine.Object.FindFirstObjectByType<Yarn.Unity.DialogueRunner>(FindObjectsInactive.Include);
if (runner != null && runner.IsDialogueRunning) runner.Stop();
var outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers/placement_drag.txt"));
System.IO.File.WriteAllText(outPath, "# placement drag simulation (PointerEventData driven, no real mouse)\n");
void Line(string s) { System.IO.File.AppendAllText(outPath, s + "\n"); }
System.Collections.IEnumerator Run() {
  yield return new WaitForSeconds(1f);
  cmds.PrepareDrinks();           // unlocks the bottles
  yield return null; yield return null;
  foreach (var path in new[] { "[CocktailSystem]/Base Spirits/BaseSpirit - Gin", "[CocktailSystem]/Mixer/Mixer - Soda" }) {
    var t = root.transform.Find(path);
    if (t == null) { Line(path + " | missing"); continue; }
    var d = t.GetComponent<DragableObject>();
    var home = t.position;
    var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) {
      button = UnityEngine.EventSystems.PointerEventData.InputButton.Left, position = new Vector2(100, 100) };
    Line(path + " | interactable=" + d.Interactable + " beingDrags=" + d.BeingDrags);
    d.OnPointerDown(ev);
    ev.position = new Vector2(220, 220);
    d.OnDrag(ev);
    yield return null;
    Line("  afterDrag  | beingDrags=" + d.BeingDrags + " isDragging=" + d.IsDragging + " anyDragging=" + DragableObject.IsAnyDragging + " moved=" + ((t.position - home).magnitude > 0.001f));
    d.OnPointerUp(ev);
    yield return null; yield return null;
    Line("  afterUp    | beingDrags=" + d.BeingDrags + " isDragging=" + d.IsDragging + " anyDragging=" + DragableObject.IsAnyDragging + " backHome=" + ((t.position - home).magnitude < 0.01f));
  }
  var hovered = IngredientHoverDetector.ResolveHoveredShaker();
  Line("hoverDetector | resolved without error, hovered shaker=" + (hovered != null ? hovered.name : "null (mouse not over shaker)"));
  Line("DONE");
}
cmds.StartCoroutine(Run());
return "started -> " + outPath;
