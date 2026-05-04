using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;
using static E_Cocktail;

public class CharacterData : MonoBehaviour
{
    [SerializedDictionary("Character", "Favorite Drink")]
    public SerializedDictionary<NPCName, List<TypeOfCocktail>> NPC_Favorite_Drink = new SerializedDictionary<NPCName, List<TypeOfCocktail>>();
      
    
}
