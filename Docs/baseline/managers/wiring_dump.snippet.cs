// Wiring dump of the [GameLoop] tree. Not compiled by Unity (outside Assets).
// Paste as the body of Unity MCP execute_code in EDIT mode (scene "New Drag Drop System" open).
// Counts scene references (inside one child vs across children), Inspector UnityEvent calls and
// references that leave the tree, then lists every reference per object.
// Writes Docs/baseline/managers/wiring_dump.<tag>.txt. wiring_dump.txt is the Stage 0 baseline.
var tag = "current";   // change to e.g. "after_stage5" to keep a snapshot
var root = GameObject.Find("[GameLoop]");
if (root == null) return "no root";
var rt = root.transform;
var sb = new System.Text.StringBuilder();
string PathOf(Transform t){ var s=t.name; while(t!=rt && t.parent!=null){ t=t.parent; s=t.name+"/"+s;} return s; }
string Desc(UnityEngine.Object o, out bool inside, out bool isScene){
  inside=false; isScene=false;
  if (o==null) return "null";
  Transform tr=null; string tn=o.GetType().Name;
  if (o is Component c){ tr=c.transform; }
  else if (o is GameObject g){ tr=g.transform; tn="GameObject"; }
  if (tr==null || !tr.gameObject.scene.IsValid()) { return "ASSET:"+AssetDatabase.GetAssetPath(o); }
  isScene=true; inside = tr.IsChildOf(rt);
  var top = tr; while(top.parent!=null) top=top.parent;
  return (inside? PathOf(tr) : "OUTSIDE:"+top.name+"/.../"+tr.name)+"("+tn+")";
}
string Top(string p){ p = System.Text.RegularExpressions.Regex.Replace(p, @"\(\w+\)$", ""); var parts=p.Replace("OUTSIDE:","OUTSIDE/").Split('/'); if(parts[0]=="OUTSIDE") return "OUTSIDE"; return parts.Length==1? "<root>" : parts[1]; }
int totalComps=0, totalMono=0, sceneRefs=0, outRefs=0, evCalls=0, missing=0, cross=0, sameChild=0;
var perType = new System.Collections.Generic.Dictionary<string,int>();
var outTargets = new System.Collections.Generic.Dictionary<string,int>();
var matrix = new System.Collections.Generic.Dictionary<string,int>();
var monoByChild = new System.Collections.Generic.Dictionary<string,int>();
foreach (var go in root.GetComponentsInChildren<Transform>(true)) {
  var comps = go.GetComponents<Component>();
  var monos = 0; var lines = new System.Text.StringBuilder();
  foreach (var comp in comps) {
    if (comp==null){ missing++; lines.AppendLine("   !! MISSING SCRIPT"); continue; }
    totalComps++;
    var ty = comp.GetType();
    if (comp is Transform) continue;
    if (comp is MonoBehaviour) { monos++; totalMono++; perType[ty.Name] = perType.ContainsKey(ty.Name)? perType[ty.Name]+1 : 1; var tc = Top(PathOf(go.transform)); monoByChild[tc] = monoByChild.ContainsKey(tc)? monoByChild[tc]+1 : 1; }
    var ns = ty.Namespace ?? "";
    bool noisy = ns.StartsWith("UnityEngine.UI") || ns.StartsWith("TMPro") || !(comp is MonoBehaviour);
    var so = new SerializedObject(comp); var it = so.GetIterator();
    var refs = new System.Collections.Generic.List<string>();
    while (it.NextVisible(true)) {
      if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
      if (it.propertyPath=="m_Script" || it.propertyPath=="m_GameObject") continue;
      var v = it.objectReferenceValue; if (v==null) continue;
      bool isEv = it.propertyPath.Contains("m_PersistentCalls");
      if (noisy && !isEv) continue;
      bool inside, isScene; var d = Desc(v, out inside, out isScene);
      if (!isScene) continue;
      string label = it.propertyPath;
      if (isEv){ evCalls++; var mp = so.FindProperty(it.propertyPath.Replace("m_Target","m_MethodName")); label = "EVENT["+it.propertyPath.Split('.')[0]+"]->"+(mp!=null?mp.stringValue:"?"); }
      sceneRefs++;
      if (!inside){ outRefs++; outTargets[d] = outTargets.ContainsKey(d)? outTargets[d]+1 : 1; }
      var s = Top(PathOf(go.transform)); var t2 = Top(d);
      if (s != t2) { cross++; var k = s+" -> "+t2; matrix[k] = matrix.ContainsKey(k)? matrix[k]+1 : 1; } else sameChild++;
      refs.Add("      "+label+" => "+d);
    }
    if (refs.Count>0 || comp is MonoBehaviour) { lines.AppendLine("   ["+ty.Name+"] refs="+refs.Count); foreach(var r in refs) lines.AppendLine(r); }
  }
  if (monos>0 || lines.Length>0) { sb.AppendLine(PathOf(go)+"  (monos="+monos+")"); sb.Append(lines.ToString()); }
}
var hdr = "# wiring dump of [GameLoop] in scene '"+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"' ("+tag+")\n";
hdr += "TOTAL comps="+totalComps+" monos="+totalMono+" sceneRefs="+sceneRefs+" sameChildRefs="+sameChild+" crossChildRefs="+cross+" outsidePrefabRefs="+outRefs+" eventCalls="+evCalls+" missingScripts="+missing+"\n";
hdr += "-- top-level children of [GameLoop] (name, active, monobehaviours) --\n";
foreach (Transform c in rt) hdr += c.name+" active="+c.gameObject.activeSelf+" monos="+(monoByChild.ContainsKey(c.name)?monoByChild[c.name]:0)+"\n";
hdr += "<root> monos="+(monoByChild.ContainsKey("<root>")?monoByChild["<root>"]:0)+"\n";
hdr += "-- cross-child refs (source -> target) --\n";
foreach (var kv in matrix.OrderByDescending(k=>k.Value)) hdr += kv.Key+" x"+kv.Value+"\n";
hdr += "-- monobehaviour counts by type --\n";
foreach (var kv in perType.OrderByDescending(k=>k.Value)) hdr += kv.Key+" x"+kv.Value+"\n";
hdr += "-- outside targets --\n";
foreach (var kv in outTargets.OrderByDescending(k=>k.Value)) hdr += kv.Key+" x"+kv.Value+"\n";
var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Docs/baseline/managers"));
System.IO.Directory.CreateDirectory(dir);
var file = tag == "baseline" ? "wiring_dump.txt" : "wiring_dump." + tag + ".txt";
System.IO.File.WriteAllText(System.IO.Path.Combine(dir, file), hdr+"\n-- per object --\n"+sb.ToString());
return hdr.Substring(0, System.Math.Min(hdr.IndexOf("-- monobehaviour counts"), hdr.Length));
