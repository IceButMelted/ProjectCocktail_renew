// ============================================================
//  ShakerTooltip.cs — Supplies the hover tooltip text for the shaker.
//  One method, one reason to change.
// ============================================================

using UnityEngine;

[RequireComponent(typeof(ShakerContents))]
public class ShakerTooltip : MonoBehaviour, ITooltipProvider
{
    private ShakerContents _contents;

    private void Awake() => _contents = GetComponent<ShakerContents>();

    public string GetTooltipText()
        => _contents == null ? string.Empty : DrinkFormatter.GetCocktailIngredient(_contents.CurrentCocktail);
}
