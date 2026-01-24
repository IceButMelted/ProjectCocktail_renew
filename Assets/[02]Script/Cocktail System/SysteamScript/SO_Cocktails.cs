using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_CocktailMaker", menuName = "Scriptable Objects/SO_CocktailMaker")]
public class SO_Cocktails : ScriptableObject
{
    public GameObject CocktailGameObject;
    public S_Recipe CocktailInfos;

}