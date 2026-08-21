using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

/// <summary>
/// Scene-side owner of the cocktail session: which repositories are in play, who is being
/// served, and the services that answer questions about the drink.
///
/// The rules themselves live in Cocktail/Domain (pure, testable) and the session state in
/// Cocktail/Session. This class wires them to the scene and to Yarn — nothing more.
/// </summary>
public partial class CocktailSystemManager : MonoBehaviour
{
    // Inspector Fields
    [Header("Cocktail Repository")]
    [Tooltip("Assign an SO_CocktailList asset — or any IDrinkRepository implementation.")]
    [SerializeField] private SO_CocktailList _normalCocktailRepository;

    [Tooltip("Optional. Story/unlock cocktails. Searchable by name, but never randomly ordered.")]
    [SerializeField] private SO_CocktailList _specialCocktailRepository;

    [Header("Customer Preferences")]
    [Tooltip("Preferred source (GDD §19.1). Falls back to the CharacterData component when empty.")]
    [SerializeField] private SO_CustomerRoster _customerRoster;

    [Header("Cocktail References")]
    [Tooltip("The drink in the glass. Preferred — assign this once the scene has been migrated.")]
    [SerializeField] private ShakerContents _shakerContents;

    [Tooltip("Ingredient buttons to lock while the player may not pour.")]
    [SerializeField] private IngredientButtonGroup _ingredientButtons;

    [Tooltip("Optional. Only used by CocktailShaker-based scenes.")]
    public CocktailShaker _cocktailShaker;

    [Tooltip("LEGACY compatibility shim. Leave empty in migrated scenes — see the manual setup doc.")]
    public CocktailShakerData _cocktailShakerData;

    // ── Shaker access ──────────────────────────────────────
    // Resolved lazily, not in Awake: in an unmigrated scene the real components are created
    // by CocktailShakerData.Awake, and component Awake order on one GameObject is not
    // guaranteed, so reading them eagerly can pick up a null.

    /// <summary>The drink in the glass, whichever way this scene is wired.</summary>
    private ShakerContents Contents
    {
        get
        {
            if (_shakerContents != null) return _shakerContents;
            if (_cocktailShakerData != null) _shakerContents = _cocktailShakerData.Contents;
            return _shakerContents;
        }
    }

    /// <summary>Ingredient buttons, whichever way this scene is wired.</summary>
    private IngredientButtonGroup IngredientButtons
    {
        get
        {
            if (_ingredientButtons != null) return _ingredientButtons;
            if (_cocktailShakerData != null) _ingredientButtons = _cocktailShakerData.IngredientGroup;
            return _ingredientButtons;
        }
    }

    // ── Character data ─────────────────────────────────────
    // Declared here, next to the Awake() that fills it. It is read from the Yarn partial,
    // but a field written in one file and declared in another is a trap for the next reader.
    private CharacterData _characterData;

    // ── Repositories (as interfaces) ───────────────────────

    /// <summary>Everything searchable by name — normal + special (plan §4.7).</summary>
    private IDrinkRepository _lookup;

    /// <summary>
    /// Pool that random orders draw from. Normal recipes only: a special cocktail must not
    /// surface before the story reaches it, so it is reachable by name and nothing else.
    /// </summary>
    private IDrinkRepository _randomPool;

    private IReadOnlyList<S_Drink> _allDrinks;

    // ── Session ────────────────────────────────────────────

    /// <summary>The order in progress. Single source of truth for target, result and payout.</summary>
    public DrinkOrderContext Order { get; } = new DrinkOrderContext();

    public OrderService Orders { get; private set; }
    public DrinkScoringService Scoring { get; private set; }

    /// <summary>Legacy accessor. Prefer <see cref="Order"/>.</summary>
    public S_Drink TargetCocktail
    {
        get => Order.Target;
        set => Order.BeginOrder(Order.Customer, OrderMode.FixedByName, value,
                                value != null ? AlcoholClassifier.Resolve(value) : TypeOfCocktail.None);
    }

    // Unity Lifecycle

    private void Awake()
    {
        _characterData = GetComponent<CharacterData>();

        // The composite skips null sources, so an unassigned special list is harmless and
        // an unassigned normal list reports itself instead of throwing later (bug B11).
        _lookup = new CompositeDrinkRepository(_normalCocktailRepository, _specialCocktailRepository);
        _randomPool = _normalCocktailRepository;
        _allDrinks = _lookup.GetDrinks();

        if (_allDrinks == null || _allDrinks.Count == 0)
            Debug.LogError("[CocktailSystemManager] No cocktail recipes loaded — assign a Cocktail Repository.", this);

        ICustomerPreferences preferences = _customerRoster != null
            ? (ICustomerPreferences)_customerRoster
            : _characterData;

        if (preferences == null)
            Debug.LogError("[CocktailSystemManager] No customer preferences — assign a Customer Roster " +
                           "or add a CharacterData component.", this);

        Orders = new OrderService(_lookup, _randomPool, preferences);
        Scoring = new DrinkScoringService(_lookup);
    }

    /// <summary>
    /// This method is USE ON BUTTON
    /// Serve the drink to the customer and update the Yarn variable with the result
    /// </summary>
    public void ServeDrink()
    {
        if (!Order.HasOrder)
        {
            Debug.LogWarning("[CocktailSystemManager] ServeDrink called with no order in progress.");
            return;
        }

        UpdateVariableInYarnTrigger();
    }

    /// <summary>
    /// This method is USE ON BUTTON
    /// This method use to reset the cocktail in shaker.
    /// </summary>
    public void ResetCocktail()
    {
        if (Contents == null)
        {
            Debug.LogWarning("[CocktailSystemManager] No ShakerContents assigned — nothing to reset.", this);
            return;
        }

        Contents.Clear();
        if (IngredientButtons != null) IngredientButtons.SetInteractable(true);
    }

    // Cocktail — Public API

    /// <summary>Picks a uniformly random cocktail as the current target.</summary>
    public S_Drink RandomCocktail()
    {
        var drink = _randomPool != null ? _randomPool.GetRandom() : null;
        if (drink != null)
            Order.BeginOrder(Order.Customer, OrderMode.RandomByPreferenceName, drink, AlcoholClassifier.Resolve(drink));

        return drink;
    }

    /// <summary>Picks a random cocktail of a specific type as the current target.</summary>
    public S_Drink RandomCocktail(TypeOfCocktail type)
    {
        var drink = _randomPool != null ? _randomPool.GetRandom(type) : null;
        if (drink != null)
            Order.BeginOrder(Order.Customer, OrderMode.RandomByPreferenceName, drink, AlcoholClassifier.Resolve(drink));

        return drink;
    }

    /// <summary>
    /// GDD §18 — scores what is in the shaker against the current order and records the
    /// result, payout and relationship change on <see cref="Order"/>.
    /// </summary>
    public Satisfaction CalculateSatisfaction()
        => Contents == null ? Satisfaction.None : Scoring.Score(Order, Contents.CurrentCocktail);

    /// <summary>GDD §18.1 — what the customer paid for the drink just scored.</summary>
    public float LastPayout => Order.Payout;

    public string GetTargetName() => Order.TargetName;

    /// <summary>Derives the identity of whatever is in the shaker and refreshes visuals.</summary>
    public void UpdateCocktailInShaker()
    {
        if (Contents == null)
        {
            Debug.LogWarning("[CocktailSystemManager] No ShakerContents assigned — cannot update the drink.", this);
            return;
        }

        Contents.UpdateIdentity(_allDrinks);
    }

    /// <summary>Editor / debug helper — picks a random target without returning it.</summary>
    public void RandomCocktailForDebug() => RandomCocktail();

    [ContextMenu("DebugTargetCocktail")]
    public void DebugTargetCocktail() => Debug.Log(DrinkFormatter.GetCocktailInfo(Order.Target));

    [ContextMenu("DebugCurrentCocktail")]
    public void DebugCurrentCocktail()
        => Debug.Log(Contents == null ? "(no ShakerContents)" : DrinkFormatter.GetCocktailInfo(Contents.CurrentCocktail));

    [ContextMenu("DebugLastMatch")]
    public void DebugLastMatch() => Debug.Log(DrinkFormatter.DescribeMatch(Order.Match));
}
