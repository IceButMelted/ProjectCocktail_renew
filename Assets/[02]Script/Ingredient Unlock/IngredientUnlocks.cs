// ============================================================
//  IngredientUnlocks.cs — the one place that knows which ingredients
//  the player has. The bar, the orders, the recipe book and Yarn all ask here.
//
//  IsUnlocked(x) = (row.Day > 0 && $day >= row.Day) || storyUnlocked.Contains(x)
//  An ingredient with no row in the table is unlocked. Unlocks are permanent.
//
//  Day unlocks are computed from $day (already saved with Yarn). Only story
//  unlocks (<<unlock_ingredient>>) and the "already announced" set are stored.
//
//  Scene-scoped singleton (Current). Knows no other manager; FlowWiring hands
//  its answers to the bar, the orders and the popups.
//  Plan: Docs/Bar410_IngredientUnlock_Plan.md
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class IngredientUnlocks : MonoBehaviour
{
    /// <summary>The instance of the loaded scene, or null. Set in Awake, cleared in OnDestroy.</summary>
    public static IngredientUnlocks Current { get; private set; }

    [SerializeField] private SO_IngredientUnlockTable _table;
    [Tooltip("Where $day is read from.")]
    [SerializeField] private DialogueRunner _dialogueRunner;

    /// <summary>A story unlock or a load changed what is unlocked. $day changes are not reported here.</summary>
    public event System.Action Changed;

    private readonly HashSet<string> _storyUnlocked = new HashSet<string>();
    private readonly HashSet<string> _notified = new HashSet<string>();
    private YarnVariableSync _variables;

    private YarnVariableSync Variables => _variables ??= new YarnVariableSync(_dialogueRunner);

    private void Awake()
    {
        if (Current != null && Current != this)
        {
            Debug.LogWarning($"[IngredientUnlocks] A second instance on '{name}' is being destroyed.", this);
            Destroy(this);
            return;
        }

        Current = this;

        // New game: the day-1 ingredients are the starting kit, so no popup announces them.
        if (_table != null)
            foreach (var row in _table.Rows)
                if (row.unlockDay == 1) _notified.Add(row.id.Key);
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    // ── Questions ──────────────────────────────────────────

    public bool IsUnlocked(string ingredient)
    {
        if (_table == null || !_table.TryGet(ingredient, out var row)) return true;
        if (_storyUnlocked.Contains(ingredient)) return true;

        return row.unlockDay > 0 && Variables.ReadDay() >= row.unlockDay;
    }

    /// <summary>True when every ingredient of the recipe is unlocked: the player can make it.</summary>
    public bool IsRecipeComplete(S_Drink drink)
    {
        if (drink == null) return false;

        foreach (var a in drink.AlcoholList)
            if (a.Type != E_Cocktail.BaseSpirit.None && !IsUnlocked(a.Type.ToString())) return false;
        foreach (var l in drink.LiqueurList)
            if (l.Type != E_Cocktail.Liqueur.None && !IsUnlocked(l.Type.ToString())) return false;
        foreach (var m in drink.MixerList)
            if (m.Type != E_Cocktail.Mixer.None && !IsUnlocked(m.Type.ToString())) return false;

        return true;
    }

    // ── Story unlock ───────────────────────────────────────

    /// <summary>Unlocks for good. Calling again does nothing. An unknown name is an error, not silence.</summary>
    public void Unlock(string ingredient)
    {
        if (TryResolve(ingredient, out var name) && _storyUnlocked.Add(name)) Changed?.Invoke();
    }

    /// <summary>The enum name for what a writer typed (any letter case). Logs an error and returns false for a name that is not an ingredient.</summary>
    private bool TryResolve(string ingredient, out string name)
    {
        foreach (var candidate in SO_IngredientUnlockTable.AllNames)
        {
            if (!string.Equals(candidate, ingredient, System.StringComparison.OrdinalIgnoreCase)) continue;

            name = candidate;
            return true;
        }

        Debug.LogError($"[IngredientUnlocks] '{ingredient}' is not an ingredient (use the enum name, e.g. LemonJuice).", this);
        name = null;
        return false;
    }

    /// <summary>For Yarn: <see cref="IsUnlocked"/> after checking the name is real (a typo is an error, not a silent false).</summary>
    private bool IsUnlockedByName(string ingredient)
        => TryResolve(ingredient, out var name) && IsUnlocked(name);

    // ── Announcements ──────────────────────────────────────

    /// <summary>
    /// Table rows of the ingredients unlocked but not announced yet, in table order (display name and
    /// icon for the popup). Marks them announced, so a second call returns an empty list.
    /// </summary>
    public List<SO_IngredientUnlockTable.Row> TakeNewUnlocks()
    {
        var rows = new List<SO_IngredientUnlockTable.Row>();
        if (_table == null) return rows;

        foreach (var row in _table.Rows)
        {
            if (_notified.Contains(row.id.Key) || !IsUnlocked(row.id.Key)) continue;

            _notified.Add(row.id.Key);
            rows.Add(row);
        }

        return rows;
    }

    // ── Save ───────────────────────────────────────────────

    public SaveUnlockData Snapshot() => new SaveUnlockData
    {
        StoryUnlocked = new List<string>(_storyUnlocked),
        Notified = new List<string>(_notified),
    };

    /// <summary>Replaces both sets with the saved ones (no merge).</summary>
    public void Restore(SaveUnlockData data)
    {
        if (data == null) return;

        _storyUnlocked.Clear();
        _storyUnlocked.UnionWith(data.StoryUnlocked ?? new List<string>());
        _notified.Clear();
        _notified.UnionWith(data.Notified ?? new List<string>());
        Changed?.Invoke();
    }

    // ── Yarn ───────────────────────────────────────────────
    // Static because Yarn calls them with no target object; they reach the live instance through Current.

    [YarnCommand("unlock_ingredient")]
    public static void UnlockIngredientCommand(string ingredient)
    {
        if (Current == null) { Debug.LogError("[IngredientUnlocks] No instance in the scene — unlock_ingredient ignored."); return; }
        Current.Unlock(ingredient);
    }

    [YarnFunction("ingredient_unlocked")]
    public static bool IngredientUnlockedFunction(string ingredient)
    {
        if (Current == null) { Debug.LogError("[IngredientUnlocks] No instance in the scene — ingredient_unlocked returns false."); return false; }
        return Current.IsUnlockedByName(ingredient);
    }
}
