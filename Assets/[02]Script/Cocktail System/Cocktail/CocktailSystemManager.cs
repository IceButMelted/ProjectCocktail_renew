using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

public partial class CocktailSystemManager : MonoBehaviour
{

    // Inspector Fields
    [Header("Cocktail Repository")]
    [Tooltip("Assign an SO_CocktailList asset — or any IDrinkRepository implementation.")]
    [SerializeField] private SO_CocktailList _normalCocktailRepository;
    [SerializeField] private SO_CocktailList _specialCocktailRepository;

    [Header("Fallback")]
    [SerializeField] private Sprite _failCocktailSprite;

    [Header("Cocktail References")]
    public CocktailShakerData _cocktailShakerData;
    public CocktailShaker _cocktailShaker;


    // ── Repositories (as interfaces)
    private IDrinkRepository _normalRepository;

    // ── Cocktail ──
    private IReadOnlyList<S_Drink> _normalDrinks;
    private S_Drink _targetCocktail;
    public S_Drink TargetCocktail { 
        get => _targetCocktail;
        set => _targetCocktail = value;
    }

    // Unity Lifecycle

    private void Awake()
    {
        _characterData = GetComponent<CharacterData>();
        if (_characterData == null)
            Debug.LogError("[CocktailSystemManager] CharacterData not found in Component.");

        // Depend on the interface, not the concrete type.
        _normalRepository = _normalCocktailRepository;
    }

    private void Start()
    {
        // Cache the read-only list once — no .ToDrink() copy needed;
        // S_Drink IS the data now.
        _normalDrinks = _normalRepository.GetDrinks();
    }

    private void Update()
    {
//#if UNITY_EDITOR
//        if (Input.GetKeyDown(KeyCode.P))
//        {
//            UpdateCocktailInShaker();
//            Debug.Log(DrinkUtility.GetCocktailInfo(_cocktailShakerData.CurrentCocktail));
//        }
//        if (Input.GetKeyDown(KeyCode.E)) { ServeDrink(); Debug.Log("[CocktailSystemManager] Served via E key."); }
//        if (Input.GetKeyDown(KeyCode.A)) SetSatisfactionPerfect();
//        if (Input.GetKeyDown(KeyCode.S)) SetSatisfactionAcceptable();
//        if (Input.GetKeyDown(KeyCode.D)) SetSatisfactionFail();
//#endif
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
    public void ResetCocktail() {
        _cocktailShakerData.ResetShaker();
        _cocktailShakerData.ResetCocktailData();
    }

    // Cocktail — Public API
    /// <summary>Picks a uniformly random cocktail as the current target.</summary>
    public S_Drink RandomCocktail()
    {
        _targetCocktail = _normalRepository.GetRandom();
        //TargetCocktail = _targetCocktail;
        return _targetCocktail;
    }

    /// <summary>Picks a random cocktail of a specific type as the current target.</summary>
    public S_Drink RandomCocktail(TypeOfCocktail type)
    {
        _targetCocktail = _normalRepository.GetRandom(type);
        //TargetCocktail = _targetCocktail;
        return _targetCocktail;
    }

    /// <summary>
    /// Calculates how well the shaker's current cocktail matches the target.
    /// Delegates comparison to DrinkUtility — no algorithm here.
    /// </summary>
    public Satisfaction CalculateSatisfaction()
        => DrinkUtility.CalculateSatisfaction(_cocktailShakerData.CurrentCocktail, _targetCocktail);

    public string GetTargetName() => _targetCocktail != null ? _targetCocktail.Name : string.Empty;

    /// <summary>
    /// Derives the identity of whatever is in the shaker and refreshes visuals.
    /// Used during debug (P key) and can be called externally.
    /// </summary>
    public void UpdateCocktailInShaker()
    {
        //S_Drink current = _cocktailShakerData.CurrentCocktail;
        //DrinkUtility.UpdateTypeOfAlcohol(current, _normalDrinks);
        //DrinkUtility.UpdateName(current, _normalDrinks);
        //DrinkUtility.UpdatePrice(current, _normalDrinks);
        //DrinkUtility.UpdateColorInGlass(current, _normalDrinks);

        _cocktailShakerData.UpdateCocktailInShaker(_normalDrinks);
        //Sprite sprite = DrinkUtility.GetCocktailSprite(current, _normalDrinks)
        //                ?? _failCocktailSprite;
        //_cocktailShaker.SetBTNSprite(sprite, sprite, sprite);
    }

    /// <summary>Editor / debug helper — picks a random target without returning it.</summary>
    public void RandomCocktailForDebug() => RandomCocktail();



    [ContextMenu("DebugTargetCocktail")]
    public void DebugTargetCocktail() => Debug.Log(DrinkUtility.GetCocktailInfo(TargetCocktail));
    [ContextMenu("DebugCurrentCocktail")]
    public void DebugCurrentCocktail() => Debug.Log(DrinkUtility.GetCocktailInfo(_cocktailShakerData.CurrentCocktail));
}