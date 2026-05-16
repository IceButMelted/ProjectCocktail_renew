using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

public class CharacterData : MonoBehaviour
{
    [SerializedDictionary("Character", "Favorite Drink")]
    public SerializedDictionary<NPC_Name, List<TypeOfCocktail>> NPC_Favorite_Drink = new SerializedDictionary<NPC_Name, List<TypeOfCocktail>>();
      
    
}
