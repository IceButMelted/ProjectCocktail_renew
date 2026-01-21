using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SO_CocktailList", menuName = "Scriptable Objects/SO_CocktailList")]
public class SO_CocktailList : ScriptableObject
{
    public List<SO_Cocktails> cocktails;
}
