using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Phases a Hub UI can be shown in. Mirrors the flow's steps; the flow itself is not referenced.</summary>
[Flags]
public enum HubPhase { None = 0, BeforeOpen = 1, Talking = 2, AddIngredient = 4, Minigame = 8, Garnish = 16, Serve = 32, Close = 64 }

/// <summary>
/// Shows and hides Hub UI (buttons on Canvas_HUD) per Phase, from a table set in the Inspector.
/// It is a CanvasGroup gate: outside a row's ticked Phases the target fades out and is not clickable.
/// It never calls SetActive, so the target's own scripts can still open and close it inside the allowed Phases.
/// UI that is not in the table is always shown. System UI (Garnish, Minigame, Post-it, Dialogue) stays with its own Manager.
/// Knows only <see cref="HubPhase"/>; FlowWiring is what calls <see cref="SetPhase"/>.
/// </summary>
public class HubManager : MonoBehaviour
{
    [Serializable]
    class Row
    {
        [TableColumnWidth(160, Resizable = false), Required, SceneObjectsOnly]
        public GameObject target;

        [EnumToggleButtons, HideLabel]
        public HubPhase shownIn;

        //[TableColumnWidth(160)]
        //public string note;

        [NonSerialized] public CanvasGroup group;
        [NonSerialized] public bool warned;
    }

    [Title("Rows")]
    [InfoBox("Hub UI that only shows or hides by Phase. UI not listed here is always shown. " +
             "Garnish / Minigame / Post-it UI is controlled by its own Manager - don't add it here.")]
    [TableList(AlwaysExpanded = true, ShowIndexLabels = false)]
    [ValidateInput(nameof(NoDuplicateTargets), "The same target appears in more than one row")]
    [SerializeField] List<Row> rows = new List<Row>();

    [PropertySpace(8)]
    [Tooltip("Seconds for a fade in or out. Unscaled time.")]
    [SerializeField, Min(0f)] float fadeDuration = 0.25f;

    [ShowInInspector, ReadOnly, HideInEditorMode]
    public HubPhase CurrentPhase { get; private set; }

    Coroutine _fade;

    bool NoDuplicateTargets(List<Row> list)
        => list == null || list.Where(r => r.target != null).Select(r => r.target).Distinct().Count()
                           == list.Count(r => r.target != null);

    /// <summary>Fades every row to its on/off state for this Phase.</summary>
    public void SetPhase(HubPhase phase)
    {
        CurrentPhase = phase;
        if (_fade != null) StopCoroutine(_fade);

        // FlowWiring can call this before our Awake, so the groups are resolved here, lazily.
        var live = new List<(CanvasGroup group, bool on)>();
        foreach (var row in rows)
        {
            var group = GroupOf(row);
            if (group != null) live.Add((group, (row.shownIn & phase) != 0));
        }

        if (!isActiveAndEnabled || fadeDuration <= 0f)
        {
            foreach (var (group, on) in live) Apply(group, on ? 1f : 0f, on);
            _fade = null;
            return;
        }
        _fade = StartCoroutine(Fade(live));
    }

    IEnumerator Fade(List<(CanvasGroup group, bool on)> live)
    {
        var from = live.Select(l => l.group.alpha).ToArray();
        foreach (var (group, _) in live) Apply(group, group.alpha, false); // no clicks mid-fade

        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            for (int i = 0; i < live.Count; i++)
                Apply(live[i].group, Mathf.Lerp(from[i], live[i].on ? 1f : 0f, t / fadeDuration), false);
            yield return null;
        }
        foreach (var (group, on) in live) Apply(group, on ? 1f : 0f, on);
        _fade = null;
    }

    static void Apply(CanvasGroup group, float alpha, bool interactive)
    {
        group.alpha = alpha;
        group.interactable = interactive;
        group.blocksRaycasts = interactive;
    }

    CanvasGroup GroupOf(Row row)
    {
        if (row.target == null)
        {
            if (!row.warned) Debug.LogWarning("[HubManager] A row has no target and is skipped.", this);
            row.warned = true;
            return null;
        }
        if (row.group == null && !row.target.TryGetComponent(out row.group))
            row.group = row.target.AddComponent<CanvasGroup>();
        return row.group;
    }

#if UNITY_EDITOR
    // Edit-mode preview. Every change goes through Undo, so Ctrl+Z reverts it.
    [Button, HorizontalGroup("Preview"), HideInPlayMode]
    void PreviewPhase(HubPhase phase)
    {
        foreach (var row in rows)
            if (row.target != null) PreviewRow(row.target, (row.shownIn & phase) != 0 ? 1f : 0f);
    }

    [Button("Show all"), HorizontalGroup("Preview"), HideInPlayMode]
    void PreviewReset()
    {
        foreach (var row in rows)
            if (row.target != null) PreviewRow(row.target, 1f);
    }

    static void PreviewRow(GameObject target, float alpha)
    {
        if (!target.TryGetComponent(out CanvasGroup group))
        {
            if (alpha >= 1f) return; // nothing to show, so don't add a component for it
            group = Undo.AddComponent<CanvasGroup>(target);
        }
        Undo.RecordObject(group, "Hub preview");
        group.alpha = alpha;
    }
#endif
}
