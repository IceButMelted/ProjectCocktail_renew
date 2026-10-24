// ============================================================
//  IngredientButtonGroup.cs — A named set of objects the player
//  may or may not interact with right now.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class IngredientButtonGroup : MonoBehaviour
{
    [Tooltip("Objects locked together with the bottles that are NOT under this object, such as the " +
             "shaker and the recipe-book button. Every bottle (anything under this object with an " +
             "IngredientButtonUI) is picked up automatically, so a new bottle needs no entry here.")]
    [SerializeField] private List<GameObject> _members = new List<GameObject>();

    [Tooltip("Interactable state applied on Awake.")]
    [SerializeField] private bool _startInteractable = true;

    private List<GameObject> _resolved;

    /// <summary>The bottles under this object followed by the explicit extras, each once.</summary>
    private List<GameObject> Members
    {
        get
        {
            if (_resolved != null) return _resolved;

            _resolved = new List<GameObject>();
            var seen = new HashSet<GameObject>();

            foreach (var bottle in GetComponentsInChildren<IngredientButtonUI>(true))
                if (seen.Add(bottle.gameObject)) _resolved.Add(bottle.gameObject);

            foreach (var extra in _members)
                if (extra != null && seen.Add(extra)) _resolved.Add(extra);

            return _resolved;
        }
    }

    private void Awake() => SetInteractable(_startInteractable);

    /// <summary>
    /// Hands every bottle the ShakerContents it pours into, so none of them has to search the
    /// scene for it on its first pour. Called once by CocktailSystemManager.
    /// </summary>
    public void BindShaker(ShakerContents shaker)
    {
        if (shaker == null) return;

        foreach (var member in Members)
            if (member.TryGetComponent<IngredientButtonUI>(out var button)) button.Bind(shaker);
    }

    /// <summary>Enables or disables every member.</summary>
    public void SetInteractable(bool interactable)
    {
        var members = Members;
        for (int i = 0; i < members.Count; i++)
            InteractableToggle.Apply(members[i], interactable);
    }

    public void Enable() => SetInteractable(true);
    public void Disable() => SetInteractable(false);

    /// <summary>Level 3 AddIngredient: pouring (click or bottle-drag) on. See InteractableToggle.ApplyPrepareDrinksPhase.</summary>
    public void EnableInteractablePrepareDrinksPhase()
    {
        var members = Members;
        for (int i = 0; i < members.Count; i++)
            InteractableToggle.ApplyPrepareDrinksPhase(members[i]);
    }
}
