using UnityEngine;
using static E_Cocktail;

/// <summary>
/// UI-friendly ingredient button for use with Unity's Button component (WorldSpace Canvas).
///
/// Set <see cref="Action"/> in the Inspector to choose what this button does.
/// Wire the Button's OnClick() to <see cref="Invoke"/> — one connection, no manual swapping.
///
/// Unlike IngredientButton (mesh/raycast), this component owns no visual logic —
/// the Button component handles all interaction and the Animator/Transition handles visuals.
/// </summary>
public class IngredientButtonUI : MonoBehaviour
{
    // ── Button Action Enum ────────────────────────────────
    public enum ButtonAction
    {
        None,
        AddMixer,
        AddAlcohol,
        AddLiqueur,
        SetShaking,
        SetMixing,
        AddIce,
        ResetShaker
    }
    [SerializeField] private ButtonAction _action;

    [SerializeField] private Mixer _mixer;
    [SerializeField] private BaseSpirit _alcohol;
    [SerializeField] private Liqueur _liqueur;

    // ── Private ───────────────────────────────────────────
    private CocktailShakerData _shaker;

    private void Awake()
        => _shaker = FindFirstObjectByType<CocktailShakerData>();

    // ── Single entry-point (wire this to Button.OnClick) ──

    /// <summary>
    /// Dispatches to the correct action based on <see cref="Action"/>.
    /// Wire only this method to the Button's OnClick() event.
    /// </summary>
    public void Invoke()
    {
        switch (_action)
        {
            case ButtonAction.AddMixer: AddMixer(); break;
            case ButtonAction.AddAlcohol: AddAlcohol(); break;
            case ButtonAction.AddLiqueur: AddLiqueur(); break;
            case ButtonAction.SetShaking: SetShaking(); break;
            case ButtonAction.SetMixing: SetMixing(); break;
            case ButtonAction.AddIce: AddIce(); break;
            case ButtonAction.ResetShaker: ResetShaker(); break;
        }
    }

    public void ActionBehavior() {
        switch (_action) {
            case ButtonAction.AddMixer: AddMixer(); break;
            case ButtonAction.AddAlcohol: AddAlcohol(); break;
            case ButtonAction.AddLiqueur: AddLiqueur(); break;
            case ButtonAction.SetShaking: SetShaking(); break;
            case ButtonAction.SetMixing: SetMixing(); break;
            case ButtonAction.AddIce: AddIce(); break;
            case ButtonAction.ResetShaker: ResetShaker(); break;
        }
    }

    // ── Individual methods (also available directly) ──────

    /// <summary>Add the assigned Mixer to the shaker.</summary>
    public void AddMixer()
    {
        _shaker.OnAddMixer?.Invoke(_mixer, 1);
    }

    /// <summary>Add the assigned Alcohol to the shaker.</summary>
    public void AddAlcohol()
    {
        _shaker.OnAddAlcohol?.Invoke(_alcohol, 1);
    }

    /// <summary>Add the assigned Liqueur to the shaker.</summary>
    public void AddLiqueur()
    {
        _shaker.OnAddLiqueur?.Invoke(_liqueur, 1);
    }

    /// <summary>Set preparation method to Shaking.</summary>
    public void SetShaking()
        => _shaker.SetMethod(Method.Shaking);

    /// <summary>Set preparation method to Mixing.</summary>
    public void SetMixing()
        => _shaker.SetMethod(Method.Stirring);

    /// <summary>Add ice to the shaker.</summary>
    public void AddIce()
    {
        //_shaker.SetIceAddIce();
        
    }

    public void AddIce(bool enable) { 
        _shaker.ToggleIce(enable);
    }


    /// <summary>Reset the shaker to empty state.</summary>
    public void ResetShaker()
        => _shaker.ResetShaker();
}