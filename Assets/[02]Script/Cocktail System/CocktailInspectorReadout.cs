// ============================================================
//  CocktailInspectorReadout.cs — Inspector-only debug readout.
//  Shows the customer's target, what's currently in the shaker,
//  and what that mix resolves to against the recipe database.
//  Pure visualisation — reads state, never changes it.
// ============================================================

using UnityEngine;

public class CocktailInspectorReadout : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CocktailSystemManager _cocktail;
    [SerializeField] private ShakerContents _shakerContents;

    [Header("Target — what the customer ordered")]
    [TextArea(2, 6)]
    [SerializeField] private string _targetInfo;

    [Header("Shaker — what's currently in the glass")]
    [TextArea(4, 10)]
    [SerializeField] private string _shakerInfo;

    [Header("Resolved — what the mix turns out to be")]
    [TextArea(2, 4)]
    [SerializeField] private string _resolvedInfo;

    private void Reset()
    {
        if (_cocktail == null) _cocktail = GetComponent<CocktailSystemManager>();
    }

    private void Update()
    {
        _targetInfo = _cocktail != null
            ? DrinkFormatter.GetCocktailInfo(_cocktail.Order.Target)
            : "(no CocktailSystemManager assigned)";

        _shakerInfo = _shakerContents != null
            ? DrinkFormatter.GetCocktailInfo(_shakerContents.CurrentCocktail)
            : "(no ShakerContents assigned)";

        _resolvedInfo = _shakerContents != null
            ? DrinkFormatter.DescribeMatch(_shakerContents.LastMatch)
            : "(no ShakerContents assigned)";
    }
}
