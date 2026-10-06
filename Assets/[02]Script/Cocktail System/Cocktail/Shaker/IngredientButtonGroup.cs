// ============================================================
//  IngredientButtonGroup.cs — A named set of objects the player
//  may or may not interact with right now.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class IngredientButtonGroup : MonoBehaviour
{
    [Tooltip("Objects this group enables and disables together.")]
    [SerializeField] private List<GameObject> _members = new List<GameObject>();

    [Tooltip("Interactable state applied on Awake.")]
    [SerializeField] private bool _startInteractable = true;

    private void Awake() => SetInteractable(_startInteractable);

    /// <summary>Enables or disables every member.</summary>
    public void SetInteractable(bool interactable)
    {
        for (int i = 0; i < _members.Count; i++)
            InteractableToggle.Apply(_members[i], interactable);
    }

    public void Enable() => SetInteractable(true);
    public void Disable() => SetInteractable(false);

    /// <summary>Level 3 AddIngredient: pouring (click or bottle-drag) on. See InteractableToggle.ApplyPrepareDrinksPhase.</summary>
    public void EnableInteractablePrepareDrinksPhase()
    {
        for (int i = 0; i < _members.Count; i++)
            InteractableToggle.ApplyPrepareDrinksPhase(_members[i]);
    }
}
