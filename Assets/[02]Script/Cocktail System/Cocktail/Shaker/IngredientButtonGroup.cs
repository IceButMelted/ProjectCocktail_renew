// ============================================================
//  IngredientButtonGroup.cs — A named set of objects the player
//  may or may not interact with right now.
//
//  Put one on the ingredient shelf and one on the recipe book;
//  the old code kept two hand-maintained lists on CocktailShakerData
//  plus two near-identical loops to walk them.
//
//  BarSetupBridge (plan §6.1) rebuilds the ingredient group from
//  whatever the player actually placed on the bar during
//  PrepareBarPhase, which is why the roster is settable at runtime.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class IngredientButtonGroup : MonoBehaviour
{
    [Tooltip("Objects this group enables and disables together.")]
    [SerializeField] private List<GameObject> _members = new List<GameObject>();

    [Tooltip("Interactable state applied on Awake.")]
    [SerializeField] private bool _startInteractable = true;

    /// <summary>Current roster. Read-only — use <see cref="SetRoster"/> to replace it.</summary>
    public IReadOnlyList<GameObject> Members => _members;

    /// <summary>Last state passed to <see cref="SetInteractable"/>.</summary>
    public bool IsInteractable { get; private set; } = true;

    private void Awake() => SetInteractable(_startInteractable);

    /// <summary>Enables or disables every member.</summary>
    public void SetInteractable(bool interactable)
    {
        IsInteractable = interactable;

        for (int i = 0; i < _members.Count; i++)
            InteractableToggle.Apply(_members[i], interactable);
    }

    /// <summary>Inspector/UnityEvent-friendly aliases.</summary>
    public void Enable() => SetInteractable(true);
    public void Disable() => SetInteractable(false);

    /// <summary>
    /// Replaces the roster, applying the current interactable state to the new members.
    /// Used by BarSetupBridge once the player has finished laying out the bar.
    /// </summary>
    public void SetRoster(IEnumerable<GameObject> members)
    {
        _members.Clear();
        if (members != null) _members.AddRange(members);

        SetInteractable(IsInteractable);
    }

    public void Add(GameObject member)
    {
        if (member == null || _members.Contains(member)) return;

        _members.Add(member);
        InteractableToggle.Apply(member, IsInteractable);
    }

    public void Remove(GameObject member) => _members.Remove(member);
}
