using System;
using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

/// <summary>
/// Scene-side owner of the cocktail session: active repositories, who's being served,
/// and the services answering questions about the drink.
/// Rules live in Cocktail/Domain (pure, testable), session state in Cocktail/Session.
/// This class just wires them to the scene and Yarn.
/// </summary>
public partial class CocktailSystemManager : MonoBehaviour
{
    // Inspector Fields
    [Header("Cocktail Repository")]
    [Tooltip("Assign an SO_CocktailList asset — or any IDrinkRepository implementation.")]
    [SerializeField] private SO_CocktailList _normalCocktailRepository;

    [Tooltip("Optional. Story/unlock cocktails. Searchable by name, but never randomly ordered.")]
    [SerializeField] private SO_CocktailList _specialCocktailRepository;

    [Header("Cocktail References")]
    [Tooltip("The drink in the glass. Preferred — assign this once the scene has been migrated.")]
    [SerializeField] private ShakerContents _shakerContents;

    [Tooltip("Ingredient buttons to lock while the player may not pour.")]
    [SerializeField] private IngredientButtonGroup _ingredientButtons;

    // ── Shaker access ──────────────────────────────────────

    /// <summary>The drink in the glass.</summary>
    private ShakerContents Contents => _shakerContents;

    /// <summary>Ingredient buttons.</summary>
    private IngredientButtonGroup IngredientButtons => _ingredientButtons;

    // ── Customer preferences ───────────────────────────────
    // Who likes what belongs to the customers (TalkToCustomerManager); this manager is handed it
    // through UsePreferences and only passes it on to the order service.
    private ICustomerPreferences _preferences;

    // ── Repositories (as interfaces) ───────────────────────

    /// <summary>Everything searchable by name — normal + special (plan §4.7).</summary>
    private IDrinkRepository _lookup;

    /// <summary>
    /// Pool random orders draw from. Normal recipes only — a special cocktail must not
    /// surface before the story reaches it, so it's reachable by name only.
    /// </summary>
    private IDrinkRepository _randomPool;

    private IReadOnlyList<S_Drink> _allDrinks;

    // ── Session ────────────────────────────────────────────

    /// <summary>The order in progress. Single source of truth for target, result and payout.</summary>
    public DrinkOrderContext Order { get; } = new DrinkOrderContext();

    public OrderService Orders { get; private set; }
    public DrinkScoringService Scoring { get; private set; }

    /// <summary>
    /// Raised once per order, right after it has been scored (synchronously, inside
    /// <see cref="ServeDrink"/>). Whoever keeps the dialogue's variables writes the result there.
    /// <c>wait_for_task</c> waits on <see cref="DrinkOrderContext.IsScored"/>, so a subscriber has
    /// finished before the dialogue resumes.
    /// </summary>
    public event Action<DrinkOrderContext> OrderScored;

    /// <summary>Raised when <c>Reset_Variable</c> starts a new customer loop (the order is already cleared).</summary>
    public event Action OrderCleared;

    // Unity Lifecycle

    private void Awake()
    {
        // Composite skips null sources: unassigned special list is harmless, unassigned
        // normal list reports itself instead of throwing later (bug B11).
        _lookup = new CompositeDrinkRepository(_normalCocktailRepository, _specialCocktailRepository);
        _randomPool = _normalCocktailRepository;
        _allDrinks = _lookup.GetDrinks();

        if (_allDrinks == null || _allDrinks.Count == 0)
            Debug.LogError("[CocktailSystemManager] No cocktail recipes loaded — assign a Cocktail Repository.", this);

        Orders = new OrderService(_lookup, _randomPool, _preferences);
        Scoring = new DrinkScoringService(_lookup);

        // The bottles pour into the shaker this manager owns; hand it over once here instead of
        // letting each bottle search the scene on its first pour.
        if (_ingredientButtons != null) _ingredientButtons.BindShaker(_shakerContents);
    }

    /// <summary>
    /// Hands over whoever knows the customers' favourite drinks. Safe to call before or after
    /// Awake; the order service is rebuilt with it.
    /// </summary>
    public void UsePreferences(ICustomerPreferences preferences)
    {
        _preferences = preferences;
        if (_lookup != null) Orders = new OrderService(_lookup, _randomPool, _preferences);
    }

    /// <summary>
    /// Scores the drink and publishes the result (<see cref="OrderScored"/>). Idempotent: an order is committed
    /// once, so a second call (a double press, a Yarn command after the button) does nothing
    /// instead of applying the relationship change again.
    /// </summary>
    public void ServeDrink()
    {
        if (!Order.HasOrder)
        {
            Debug.LogWarning("[CocktailSystemManager] ServeDrink called with no order in progress.");
            return;
        }

        if (Order.IsScored) return;

        UpdateVariableInYarnTrigger();
    }

    /// <summary>Button hook. Resets the cocktail in the shaker.</summary>
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
    /// GDD §18 — scores shaker contents against the current order; records result,
    /// payout and relationship change on <see cref="Order"/>.
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

    /// <summary>
    /// Fallback for testing without dialogue: only randomizes a target if nothing was placed
    /// yet (e.g. by a real Order_Cocktail_* Yarn call). Never overwrites a dialogue-driven order.
    /// </summary>
    public void RandomCocktailIfNoOrder()
    {
        if (!Order.HasOrder) RandomCocktail();
    }

    [ContextMenu("DebugTargetCocktail")]
    public void DebugTargetCocktail() => Debug.Log(DrinkFormatter.GetCocktailInfo(Order.Target));

    [ContextMenu("DebugCurrentCocktail")]
    public void DebugCurrentCocktail()
        => Debug.Log(Contents == null ? "(no ShakerContents)" : DrinkFormatter.GetCocktailInfo(Contents.CurrentCocktail));

    [ContextMenu("DebugLastMatch")]
    public void DebugLastMatch() => Debug.Log(DrinkFormatter.DescribeMatch(Order.Match));
}
