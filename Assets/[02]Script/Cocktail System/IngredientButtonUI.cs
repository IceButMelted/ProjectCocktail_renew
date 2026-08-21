using UnityEngine;
using UnityEngine.Events;
using static E_Cocktail;

/// <summary>
/// One ingredient / method / ice button. Set <see cref="ButtonAction"/> in the Inspector to
/// choose what it does, then wire the click to <see cref="Invoke"/> — one connection, no
/// manual swapping. Works from a UI Button as well as from Interactable_2_5DObject.OnClicked.
///
/// Talks to <see cref="ShakerContents"/>, the refactored owner of the live drink. It used to
/// talk to the CocktailShakerData shim, which the migrated scenes no longer have — the lookup
/// returned null and every pour threw. Scenes that still carry the shim keep their old
/// behaviour: see <see cref="Legacy"/>.
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

    [Header("Shaker")]
    [Tooltip("Leave empty to find the one in the scene on first use.")]
    [SerializeField] private ShakerContents _shaker;

    [Header("Events")]
    [Tooltip("Fired only when the pour actually landed — not when the glass is already full (GDD §15, 10 units).")]
    public UnityEvent OnPoured = new UnityEvent();

    [Tooltip("Fired when the pour was refused because the glass has no room left.")]
    public UnityEvent OnRejected = new UnityEvent();

    // ── Shaker resolution ─────────────────────────────────

    /// <summary>
    /// Resolved on first use, not in Awake: in unmigrated scenes ShakerContents is added by
    /// the CocktailShakerData shim during ITS Awake, and script execution order is not fixed.
    /// </summary>
    private ShakerContents Shaker
    {
        get
        {
            if (_shaker != null) return _shaker;

            _shaker = FindFirstObjectByType<ShakerContents>(FindObjectsInactive.Include);
            if (_shaker == null && Legacy != null) _shaker = Legacy.Contents;
            if (_shaker == null)
                Debug.LogWarning($"[IngredientButtonUI] No ShakerContents in the scene — '{name}' does nothing.", this);

            return _shaker;
        }
    }

    /// <summary>
    /// The compatibility shim, when the scene still has one. Its ingredient UnityEvents are
    /// authored per scene (pour animation, sounds, the add itself), so where it exists those
    /// events stay the single path — calling ShakerContents directly as well would pour twice.
    /// </summary>
    private CocktailShakerData Legacy
        => _legacy != null ? _legacy : _legacy = FindFirstObjectByType<CocktailShakerData>(FindObjectsInactive.Include);

    private CocktailShakerData _legacy;

    // ── Single entry-point (wire this to the click) ───────

    /// <summary>Dispatches to the action picked in the Inspector.</summary>
    public void Invoke()
    {
        switch (_action)
        {
            case ButtonAction.AddMixer: AddMixer(); break;
            case ButtonAction.AddAlcohol: AddAlcohol(); break;
            case ButtonAction.AddLiqueur: AddLiqueur(); break;
            case ButtonAction.SetShaking: SetShaking(); break;
            case ButtonAction.SetMixing: SetMixing(); break;
            case ButtonAction.AddIce: AddIce(true); break;
            case ButtonAction.ResetShaker: ResetShaker(); break;
        }
    }

    // ── Individual methods (also available directly) ──────

    /// <summary>Add the assigned Mixer to the shaker.</summary>
    public void AddMixer()
    {
        if (Legacy != null) { Legacy.OnAddMixer?.Invoke(_mixer, 1); return; }
        Pour(() => Shaker.TryToAddMixer(_mixer, 1));
    }

    /// <summary>Add the assigned Alcohol to the shaker.</summary>
    public void AddAlcohol()
    {
        if (Legacy != null) { Legacy.OnAddAlcohol?.Invoke(_alcohol, 1); return; }
        Pour(() => Shaker.TryToAddAlcohol(_alcohol, 1));
    }

    /// <summary>Add the assigned Liqueur to the shaker.</summary>
    public void AddLiqueur()
    {
        if (Legacy != null) { Legacy.OnAddLiqueur?.Invoke(_liqueur, 1); return; }
        Pour(() => Shaker.TryToAddLiqueur(_liqueur, 1));
    }

    /// <summary>Set preparation method to Shaking.</summary>
    public void SetShaking() => Shaker?.SetMethod(Method.Shaking);

    /// <summary>Set preparation method to Mixing (Method.Stirring).</summary>
    public void SetMixing() => Shaker?.SetMethod(Method.Stirring);

    /// <summary>Add or remove ice. The no-argument overload had an empty body and was deleted.</summary>
    public void AddIce(bool enable) => Shaker?.ToggleIce(enable);

    /// <summary>
    /// Empty the shaker. With the shim present this raises its OnResetedCocktail chain exactly
    /// as before; without it, the drink is cleared and ShakerContents.Cleared carries the news.
    /// </summary>
    public void ResetShaker()
    {
        if (Legacy != null) { Legacy.ResetShaker(); return; }
        Shaker?.Clear();
    }

    // ── Internal ──────────────────────────────────────────

    /// <summary>
    /// Runs the add and reports whether it landed. DrinkBuilder silently refuses a pour that
    /// would break the 10-unit ceiling, so the part count is what tells the two apart.
    /// </summary>
    private void Pour(System.Action add)
    {
        var shaker = Shaker;
        if (shaker == null) return;

        int before = shaker.TotalParts;
        add();

        if (shaker.TotalParts != before) OnPoured?.Invoke();
        else OnRejected?.Invoke();
    }
}
