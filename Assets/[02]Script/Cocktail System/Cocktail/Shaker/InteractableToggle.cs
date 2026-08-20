// ============================================================
//  InteractableToggle.cs — The one place that knows what
//  "enable this object for the player" actually means.
//
//  Plan bug B4: three separate loops did this, each touching a
//  DIFFERENT subset of components —
//    CocktailShakerData.SetIngredientActive   6 component types
//    CocktailShakerData.SetBookUiActive       7 component types
//    CocktailSystemManager.EnableButtonInYarn 4 component types,
//                                             and it ran straight
//                                             after SetIngredientActive,
//                                             setting the same objects twice.
//  Adding a new interactable component used to mean remembering all
//  three; now it means editing Apply().
// ============================================================

using UnityEngine;

public static class InteractableToggle
{
    /// <summary>
    /// Enables or disables every interaction component present on <paramref name="target"/>.
    /// Missing components are simply skipped — objects are free to carry any subset.
    /// </summary>
    public static void Apply(GameObject target, bool interactable)
    {
        if (target == null) return;

        if (target.TryGetComponent<Interactable_2_5DObject>(out var flat)) flat.Interactable = interactable;
        if (target.TryGetComponent<Interactable_3DObject>(out var solid)) solid.Interactable = interactable;
        if (target.TryGetComponent<DragableObject>(out var drag)) drag.Interactable = interactable;
        if (target.TryGetComponent<ScaleOnHover>(out var scale)) scale.Interactable = interactable;
        if (target.TryGetComponent<HoverTooltip>(out var tooltip)) tooltip.Interactable = interactable;
        if (target.TryGetComponent<UIPointerSound>(out var sound)) sound.Interactable = interactable;
        if (target.TryGetComponent<BookUI_V2>(out var book)) book.SetActive(interactable);
    }
}
