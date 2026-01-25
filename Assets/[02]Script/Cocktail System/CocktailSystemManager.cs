using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class CocktailSystemManager : MonoBehaviour
{
    [SerializeField] private SO_CocktailList normalCocktailList;
    [SerializeField] private SO_CocktailList specialCocktailList;
    private S_Recipe targetcocktail = default(S_Recipe);

    private void Start()
    {
        RandomCocktail();
        Debug.Log("All randome\n" + targetcocktail.GetCocktailInfo());

        RandomCocktail(E_Cocktail.TypeOfCocktail.LowAlcohol);
        Debug.Log("Specific type\n" + targetcocktail.GetCocktailInfo());
    }

    public S_Recipe RandomCocktail()
    {
        int randomIndex = Random.Range(0, normalCocktailList.cocktails.Count);
        targetcocktail = normalCocktailList.cocktails[randomIndex].CocktailInfos;
        return targetcocktail;
    }

    public S_Recipe RandomCocktail(E_Cocktail.TypeOfCocktail _typeCocktail)
    {
        // Get all cocktails matching the type
        var matchingCocktails = normalCocktailList.cocktails
            .Where(c => c.CocktailInfos.typeOfCocktail == _typeCocktail)
            .ToList();

        // If no matching cocktails found, return default or log warning
        if (matchingCocktails.Count == 0)
        {
            Debug.LogWarning($"No cocktails found of type: {_typeCocktail}");
            return default(S_Recipe); // or return RandomCocktail() for fallback
        }

        // Pick random from matching cocktails
        int randomIndex = Random.Range(0, matchingCocktails.Count);
        targetcocktail = matchingCocktails[randomIndex].CocktailInfos;
        return targetcocktail;
    }
}