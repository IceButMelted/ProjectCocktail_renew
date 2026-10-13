using UnityEngine;

[CreateAssetMenu(menuName = "Cocktail/Menu")]
public class CocktailMenu : ScriptableObject
{
    [Tooltip("Gradient top colour. Only RGB is used.")]
    public Color top = Color.magenta;

    [Tooltip("Gradient bottom colour and pour stream colour. Only RGB is used.")]
    public Color bottom = Color.green;
}
