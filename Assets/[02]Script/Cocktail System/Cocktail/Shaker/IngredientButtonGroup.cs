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

    /// <summary>Enables or disables only one component type across every member.</summary>
    public void EnableOnlyDragDrop(bool enable) {
        for (int i = 0; i < _members.Count; i++)
            InteractableToggle.ApplyOnlyDragDrop(_members[i], enable);
    }
    public void EnableOnlyHoverTooltip(bool enable) {
        for (int i = 0; i < _members.Count; i++)
            InteractableToggle.ApplyOnlyHoverTooltip(_members[i], enable);
    }
    public void EnableOnlyScaleOnHover(bool enable) {
        for (int i = 0; i < _members.Count; i++)
            InteractableToggle.ApplyOnlyScaleOnHover(_members[i], enable);
    }
    public void EnableOnlyUIPointerSound(bool enable, bool? canPlayUp = null) {
        for (int i = 0; i < _members.Count; i++)
            InteractableToggle.ApplyOnlyUIPointerSound(_members[i], enable, canPlayUp);
    }
    public void EnableOnlyBookUI(bool enable) {
        for (int i = 0; i < _members.Count; i++)
            InteractableToggle.ApplyOnlyBookUI(_members[i], enable);
    }
    public void EnableOnlyInteractable_2_5DObject(bool enable) {
        for (int i = 0; i < _members.Count; i++)
            InteractableToggle.ApplyOnlyInteractable_2_5DObject(_members[i], enable);
    }
    public void EnableOnlyInteractable_3DObject(bool enable) {
        for (int i = 0; i < _members.Count; i++)
            InteractableToggle.ApplyOnlyInteractable_3DObject(_members[i], enable);
    }
    public void EnableOnlyBottleIngredientSource(bool enable) {
        for (int i = 0; i < _members.Count; i++)
            InteractableToggle.ApplyOnlyBottleIngredientSource(_members[i], enable);
    }

    ///--- Enable/Disable on this Phase------

    /// <summary>Level 1 Prepare: drag-and-drop bar layout on, pouring off. See InteractableToggle.ApplyPrepareBarPhase.</summary>
    public void EnableInteractablePrepareBarPhase()
    {
        IsInteractable = true;
        for (int i = 0; i < _members.Count; i++)
            InteractableToggle.ApplyPrepareBarPhase(_members[i]);
    }

    /// <summary>Level 3 AddIngredient: pouring (click or bottle-drag) on. See InteractableToggle.ApplyPrepareDrinksPhase.</summary>
    public void EnableInteractablePrepareDrinksPhase()
    {
        IsInteractable = true;
        for (int i = 0; i < _members.Count; i++)
            InteractableToggle.ApplyPrepareDrinksPhase(_members[i]);
    }

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
