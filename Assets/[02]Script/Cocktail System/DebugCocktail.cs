using TMPro;
using UnityEngine;

public class DebugCocktail : MonoBehaviour
{
    public CocktailShaker cocktailShaker;
    public CocktailSystemManager cocktailSystemManager;

    public TextMeshProUGUI targetCocktail;
    public TextMeshProUGUI currentCocktail;
    public TextMeshProUGUI customerSatification;

    private void Update(){
        targetCocktail.text = "Target Cocktail :" + cocktailSystemManager.GetTargetName();
        
        currentCocktail.text = "Current in Shaker :\n" +cocktailShaker.currentCocktail.GetOfCocktailInfo();
    }

    public void UpdateSatification() {
        customerSatification.text = "Customer Satification" + cocktailSystemManager.CalculateSatisfaction();
    }



}
