using UnityEngine;
using static E_Cocktail;

/// <summary>
/// UI-friendly ingredient button for use with Unity's Button component (WorldSpace Canvas).
/// Assign each method to the Button's OnClick() event in the Inspector.
///
/// Unlike IngredientButton (mesh/raycast), this component owns no visual logic —
/// the Button component handles all interaction and the Animator/Transition handles visuals.
/// </summary>
public class IngredientButtonUI : MonoBehaviour
{
    [Header("Ingredient Settings")]
    [SerializeField] private Mixer   _mixer;
    [SerializeField] private BaseSpirit _alcohol;

    // ── Private ───────────────────────────────────────────
    private CocktailShakerData _shaker;

    private void Awake()
        => _shaker = FindFirstObjectByType<CocktailShakerData>();

    // ── Public Methods (wire these to Button.OnClick) ─────

    /// <summary>Add the assigned Mixer to the shaker.</summary>
    public void AddMixer()
    {
        _shaker.OnAddMixer?.Invoke(_mixer, 1);
        _shaker.OnAddIngredient?.Invoke();
    }

    /// <summary>Add the assigned Alcohol to the shaker.</summary>
    public void AddAlcohol()
    {
        _shaker.OnAddAlcohol?.Invoke(_alcohol, 1);
        _shaker.OnAddIngredient?.Invoke();
    }

    /// <summary>Set preparation method to Shaking.</summary>
    public void SetShaking()
        => _shaker.SetMethod(Method.Shaking);

    /// <summary>Set preparation method to Mixing.</summary>
    public void SetMixing()
        => _shaker.SetMethod(Method.Mixing);

    /// <summary>Add ice to the shaker. Disables this button after use (one-shot).</summary>
    public void AddIce()
    {
        _shaker.SetIceAddIce();
        
        GetComponent<Interactable3DObject>().Interactable = false;
    }

    /// <summary>Reset the shaker to empty state.</summary>
    public void ResetShaker()
        => _shaker.ResetShaker();
}
