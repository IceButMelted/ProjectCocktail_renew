using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static E_Cocktail;
using UnityEngine.Events;

public class CocktailSystemManager : MonoBehaviour
{
    [SerializeField] private SO_CocktailList normalCocktailList;
    [SerializeField] private SO_CocktailList specialCocktailList;
    private List<S_Drink> NormalCocktails = new List<S_Drink>();
    private S_Drink targetcocktail = default(S_Drink);

    [SerializeField] public CocktailShaker cocktailShaker;

    public UnityEvent OnApplyCocktail;

    private void Awake()
    {
        
    }

    private void Start()
    {
        RandomCocktail();
        Debug.Log("All randome\n" + targetcocktail.GetOfCocktailInfo());

        RandomCocktail(E_Cocktail.TypeOfCocktail.LowAlcohol);
        Debug.Log("Specific type\n" + targetcocktail.GetOfCocktailInfo());

        foreach (SO_Cocktails s in normalCocktailList.cocktails)
        {
            S_Drink drink = s.CocktailInfos;
            NormalCocktails.Add(drink);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            UpdateCocktailInShaker();
            Debug.Log(cocktailShaker.currentCocktail.GetOfCocktailInfo());
        }
    }

    public S_Drink RandomCocktail()
    {
        int randomIndex = Random.Range(0, normalCocktailList.cocktails.Count);
        targetcocktail = normalCocktailList.cocktails[randomIndex].CocktailInfos;
        return targetcocktail;
    }

    public S_Drink RandomCocktail(E_Cocktail.TypeOfCocktail _typeCocktail)
    {
        // Get all cocktails matching the type
        var matchingCocktails = normalCocktailList.cocktails
            .Where(c => c.CocktailInfos.GetTypeOfAlcohol() == _typeCocktail)
            .ToList();

        // If no matching cocktails found, return default or log warning
        if (matchingCocktails.Count == 0)
        {
            Debug.LogWarning($"No cocktails found of type: {_typeCocktail}");
            return default(S_Drink); // or return RandomCocktail() for fallback
        }

        // Pick random from matching cocktails
        int randomIndex = Random.Range(0, matchingCocktails.Count);
        targetcocktail = matchingCocktails[randomIndex].CocktailInfos;
        return targetcocktail;
    }

    public Satisfaction CalculateSatisfaction() {
        return targetcocktail.CalculateSatisfaction(cocktailShaker.currentCocktail);
    }

    public void UpdateCocktailInShaker() {
        cocktailShaker.currentCocktail.UpdateTypeOfAlcohol(NormalCocktails);
        cocktailShaker.currentCocktail.UpdateName(NormalCocktails);
        cocktailShaker.currentCocktail.UpdatePrice(NormalCocktails);
        Texture2D newShakerSprite = cocktailShaker.currentCocktail.GetCocktailTexture(NormalCocktails) as Texture2D;
        if (newShakerSprite != null ) 
            cocktailShaker.SetBTNSprite(newShakerSprite,newShakerSprite,newShakerSprite);
    }
    
}
