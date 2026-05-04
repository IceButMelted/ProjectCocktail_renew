using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static E_Cocktail;

public class CocktailSystemManager : MonoBehaviour
{
    [Header("Cocktail Lists")]
    [SerializeField] private SO_CocktailList _normalCocktailList;
    [SerializeField] private SO_CocktailList _specialCocktailList;

    [Header("Fallback")]
    [SerializeField] private Texture2D _failCocktailTexture;

    [Header("References")]
    public CocktailShaker cocktailShaker;

    // ── Private State ────────────────────────────────────
    private List<S_Drink> _normalDrinks = new List<S_Drink>();
    private S_Drink       _targetCocktail;

    // ── Unity ────────────────────────────────────────────
    private void Start()
    {
        // Cache runtime list once
        foreach (var so in _normalCocktailList.cocktails)
            _normalDrinks.Add(so.CocktailInfos);

        RandomCocktail(TypeOfCocktail.LowAlcohol);
        Debug.Log("[CocktailSystem] Target set:\n" + _targetCocktail.GetOfCocktailInfo());
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
        {
            UpdateCocktailInShaker();
            Debug.Log(cocktailShaker.currentCocktail.GetOfCocktailInfo());
        }
#endif
    }

    // ── Public API ───────────────────────────────────────

    /// <summary>Pick a random cocktail (any type) as the current target.</summary>
    public S_Drink RandomCocktail()
    {
        int idx = Random.Range(0, _normalCocktailList.cocktails.Count);
        _targetCocktail = _normalCocktailList.cocktails[idx].CocktailInfos;
        return _targetCocktail;
    }

    /// <summary>Pick a random cocktail of a specific type as the current target.</summary>
    public S_Drink RandomCocktail(TypeOfCocktail type)
    {
        var matches = _normalCocktailList.cocktails
            .Where(c => c.CocktailInfos.GetTypeOfAlcohol() == type)
            .ToList();

        if (matches.Count == 0)
        {
            Debug.LogWarning($"[CocktailSystem] No cocktails of type {type}. Falling back to random.");
            return RandomCocktail();
        }

        _targetCocktail = matches[Random.Range(0, matches.Count)].CocktailInfos;
        return _targetCocktail;
    }

    public Satisfaction CalculateSatisfaction()
        => _targetCocktail.CalculateSatisfaction(cocktailShaker.currentCocktail);

    public string GetTargetName() => _targetCocktail.Name;

    /// <summary>Derive identity of whatever is currently in the shaker and update visuals.</summary>
    public void UpdateCocktailInShaker()
    {
        var current = cocktailShaker.currentCocktail;
        current.UpdateTypeOfAlcohol(_normalDrinks);
        current.UpdateName(_normalDrinks);
        current.UpdatePrice(_normalDrinks);

        Texture2D tex = current.GetCocktailTexture(_normalDrinks) ?? _failCocktailTexture;
        //cocktailShaker.SetBTNSprite(tex, tex, tex);
    }

    // ── Debug Helpers ─────────────────────────────────────
    /// <summary>Editor/debug helper — sets a random target without returning it.</summary>
    public void RandomCocktailForDebug() => RandomCocktail();
}
