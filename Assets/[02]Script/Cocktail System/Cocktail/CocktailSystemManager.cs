using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

public partial class CocktailSystemManager : MonoBehaviour
{
    // Inspector Fields
    [Header("Cocktail Repository")]
    [Tooltip("Assign an SO_CocktailList asset — or any IDrinkRepository implementation.")]
    [SerializeField] private SO_CocktailList _normalCocktailRepository;

    [Tooltip("Optional. Story/unlock cocktails. Searchable by name, but never randomly ordered.")]
    [SerializeField] private SO_CocktailList _specialCocktailRepository;

    [Header("Cocktail References")]
    public CocktailShakerData _cocktailShakerData;
    public CocktailShaker _cocktailShaker;

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

    // ── Cocktail ──
    private S_Drink _targetCocktail;
    public S_Drink TargetCocktail
    {
        get => _targetCocktail;
        set => _targetCocktail = value;
    }

    // Unity Lifecycle

    private void Awake()
    {
        _characterData = GetComponent<CharacterData>();
        if (_characterData == null)
            Debug.LogError("[CocktailSystemManager] CharacterData not found in Component.", this);

        // Depend on the interfaces, not the concrete assets.
        // The composite skips null sources, so an unassigned special list is harmless and
        // an unassigned normal list reports itself instead of throwing later (bug B11).
        _lookup = new CompositeDrinkRepository(_normalCocktailRepository, _specialCocktailRepository);
        _randomPool = _normalCocktailRepository;

        _allDrinks = _lookup.GetDrinks();

        if (_allDrinks == null || _allDrinks.Count == 0)
            Debug.LogError("[CocktailSystemManager] No cocktail recipes loaded — assign a Cocktail Repository.", this);
    }

    /// <summary>
    /// This method is USE ON BUTTON
    /// Serve the drink to the customer and update the Yarn variable with the result
    /// </summary>
    public void ServeDrink()
    {
        if (_targetCocktail == null)
        {
            Debug.LogWarning("[CocktailSystemManager] ServeDrink called with no target cocktail set.");
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
        _cocktailShakerData.ResetShaker();
        _cocktailShakerData.ResetCocktailData();
    }

    // Cocktail — Public API

    /// <summary>Picks a uniformly random cocktail as the current target.</summary>
    public S_Drink RandomCocktail()
    {
        _targetCocktail = _randomPool != null ? _randomPool.GetRandom() : null;
        return _targetCocktail;
    }

    /// <summary>Picks a random cocktail of a specific type as the current target.</summary>
    public S_Drink RandomCocktail(TypeOfCocktail type)
    {
        _targetCocktail = _randomPool != null ? _randomPool.GetRandom(type) : null;
        return _targetCocktail;
    }

    /// <summary>
    /// GDD §18 — how satisfied the customer is with what is in the shaker right now.
    ///
    /// Deviation is measured against the ORDERED recipe; the identity of what the player
    /// actually made comes from a separate best-match search. See the note on
    /// <see cref="DrinkDeviation.MatchAgainst"/> for why.
    /// </summary>
    public Satisfaction CalculateSatisfaction()
    {
        var served = _cocktailShakerData.CurrentCocktail;

        if (_targetCocktail == null || served == null)
        {
            Debug.LogWarning("[CocktailSystemManager] CalculateSatisfaction with no target or no drink.");
            return Satisfaction.None;
        }

        var orderMatch = DrinkDeviation.MatchAgainst(served, _targetCocktail);
        var identity = DrinkDeviation.FindBestMatch(served, _allDrinks);

        TypeOfCocktail servedType = identity.IsRecognised
            ? AlcoholClassifier.Resolve(identity.Recipe)
            : AlcoholClassifier.Compute(served);
        TypeOfCocktail orderedType = AlcoholClassifier.Resolve(_targetCocktail);

        // Cached so the Yarn layer can write $type_of_cocktail with a real value (bug B1)
        // and so pricing can tell Fail (a) from Fail (b) (GDD §18.1).
        _servedType = servedType;
        _lastOrderMatch = orderMatch;

        return SatisfactionEvaluator.Evaluate(orderMatch, servedType, orderedType);
    }

    /// <summary>GDD §18.1 — what the customer pays for the drink just evaluated.</summary>
    public float CalculatePayout(Satisfaction result) => PricingRules.Payout(_lastOrderMatch, result);

    public string GetTargetName() => _targetCocktail != null ? _targetCocktail.Name : string.Empty;

    /// <summary>Derives the identity of whatever is in the shaker and refreshes visuals.</summary>
    public void UpdateCocktailInShaker() => _cocktailShakerData.UpdateCocktailInShaker(_allDrinks);

    /// <summary>Editor / debug helper — picks a random target without returning it.</summary>
    public void RandomCocktailForDebug() => RandomCocktail();

    [ContextMenu("DebugTargetCocktail")]
    public void DebugTargetCocktail() => Debug.Log(DrinkFormatter.GetCocktailInfo(TargetCocktail));

    [ContextMenu("DebugCurrentCocktail")]
    public void DebugCurrentCocktail() => Debug.Log(DrinkFormatter.GetCocktailInfo(_cocktailShakerData.CurrentCocktail));

    [ContextMenu("DebugLastMatch")]
    public void DebugLastMatch() => Debug.Log(DrinkFormatter.DescribeMatch(_lastOrderMatch));
}
