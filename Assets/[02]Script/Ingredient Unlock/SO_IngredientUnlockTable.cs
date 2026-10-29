// ============================================================
//  SO_IngredientUnlockTable.cs — which ingredient unlocks on which day.
//
//  One row per ingredient, picked as a typed IngredientId. Its Key is the enum name (Gin, LemonJuice, ...),
//  the same string Yarn uses in <<unlock_ingredient X>>. An ingredient with
//  no row is unlocked from the start. See IngredientUnlocks.
//  Plan: Docs/Bar410_IngredientUnlock_Plan.md
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using static E_Cocktail;

[CreateAssetMenu(fileName = "IngredientUnlockTable", menuName = "Bar410/Ingredient Unlock Table")]
public class SO_IngredientUnlockTable : ScriptableObject
{
    [Serializable]
    public class Row
    {
        [Tooltip("Pick the category, then the ingredient (BaseSpirit / Liqueur / Mixer).")]
        public IngredientId id;


        [TableColumnWidth(80, Resizable = false), Min(0)]
        [Tooltip("0 = only a Yarn <<unlock_ingredient>> unlocks it. N = unlocks by itself when $day >= N.")]
        public int unlockDay = 1;

        // ponytail: plain string until Localization exists; then this becomes a String Table key.
        [Tooltip("Shown in the unlock popup. Empty = the enum name.")]
        public string displayName;

        [PreviewField(40), TableColumnWidth(60, Resizable = false)]
        [Tooltip("Icon in the unlock popup, beside the other ingredients new that day. Empty = no icon for this one.")]
        public Sprite icon;

        public string DisplayOrName => string.IsNullOrEmpty(displayName) ? id.Key : displayName;
    }

    [TableList(AlwaysExpanded = true, ShowIndexLabels = false)]
    [ValidateInput(nameof(RowsAreValid))]
    [SerializeField] private List<Row> rows = new List<Row>();

    public IReadOnlyList<Row> Rows => rows;

    public bool TryGet(string ingredient, out Row row)
    {
        row = rows.FirstOrDefault(r => r.id.Key == ingredient);
        return row != null;
    }

    /// <summary>Every ingredient name the bar knows: the three enums without None.</summary>
    public static IEnumerable<string> AllNames
        => Enum.GetNames(typeof(BaseSpirit))
               .Concat(Enum.GetNames(typeof(Liqueur)))
               .Concat(Enum.GetNames(typeof(Mixer)))
               .Where(n => n != "None");

    /// <summary>Every ingredient the bar knows as an id, in enum order.</summary>
    public static IEnumerable<IngredientId> AllIds
        => Enum.GetValues(typeof(BaseSpirit)).Cast<BaseSpirit>().Where(v => v != BaseSpirit.None).Select(v => IngredientId.Of(v))
           .Concat(Enum.GetValues(typeof(Liqueur)).Cast<Liqueur>().Where(v => v != Liqueur.None).Select(v => IngredientId.Of(v)))
           .Concat(Enum.GetValues(typeof(Mixer)).Cast<Mixer>().Where(v => v != Mixer.None).Select(v => IngredientId.Of(v)));

    /// <summary>Ingredients that have no row (they count as unlocked from day 1).</summary>
    public List<IngredientId> MissingIds()
    {
        var have = new HashSet<string>(rows.Select(r => r.id.Key));
        return AllIds.Where(i => !have.Contains(i.Key)).ToList();
    }

    private bool RowsAreValid(List<Row> list, ref string message)
    {
        if (list == null) return true;
        if (list.Any(r => !r.id.IsValid)) { message = "A row has no ingredient picked"; return false; }
        if (list.Select(r => r.id.Key).Distinct().Count() != list.Count) { message = "The same ingredient appears in more than one row"; return false; }
        return true;
    }

#if UNITY_EDITOR
    [Button("Add Missing Ingredients", ButtonSizes.Large)]
    [Tooltip("Appends a row (Unlock Day 1) for every ingredient enum value that has none.")]
    private void AddMissingIngredients()
    {
        var missing = MissingIds();
        if (missing.Count == 0) { Debug.Log($"[{name}] No missing ingredients.", this); return; }

        UnityEditor.Undo.RecordObject(this, "Add Missing Ingredients");
        foreach (var id in missing) rows.Add(new Row { id = id });
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[{name}] Added {missing.Count} row(s): {string.Join(", ", missing.Select(i => i.Key))}", this);
    }
#endif
}
