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

        //this blew for disable other component that's not inherited from Intaractable_2_5DObject or Interactable_3DObject, so we need to add a separate check for it
        if (target.TryGetComponent<BottleIngredientSource>(out var bottle)) bottle.enabled = interactable; 
    }

    public static void ApplyOnlyDragDrop(GameObject target, bool interactable)
    {
        if (target == null) return;
        if (target.TryGetComponent<DragableObject>(out var drag)) drag.Interactable = interactable;
    }

    public static void ApplyOnlyHoverTooltip(GameObject target, bool interactable)
    {
        if (target == null) return;
        if (target.TryGetComponent<HoverTooltip>(out var tooltip)) tooltip.Interactable = interactable;
    }

    public static void ApplyOnlyScaleOnHover(GameObject target, bool interactable)
    {
        if (target == null) return;
        if (target.TryGetComponent<ScaleOnHover>(out var scale)) scale.Interactable = interactable;
    }

    /// <summary>
    /// Sets pointer-sound interactability. Pass <paramref name="canPlayUp"/> only when the
    /// pointer-up sound needs to end up different from <paramref name="interactable"/> (e.g.
    /// muted specifically while everything else stays on) — leave it null to let the
    /// Interactable setter's own OnInteractableChanged hook decide it as usual.
    /// </summary>
    public static void ApplyOnlyUIPointerSound(GameObject target, bool interactable, bool? canPlayUp = null)
    {
        if (target == null) return;
        if (!target.TryGetComponent<UIPointerSound>(out var sound)) return;

        sound.Interactable = interactable;
        if (canPlayUp.HasValue) sound.SetCanPlayUp(canPlayUp.Value);
    }

    public static void ApplyOnlyBookUI(GameObject target, bool interactable)
    {
        if (target == null) return;
        if (target.TryGetComponent<BookUI_V2>(out var book)) book.SetActive(interactable);
    }

    public static void ApplyOnlyBottleIngredientSource(GameObject target, bool enable)
    {
        if (target == null) return;
        if (target.TryGetComponent<BottleIngredientSource>(out var bottle)) bottle.enabled = enable;
    }

    /// <summary>
    /// Turns a DragableFruitTraySlot's drag-hijack on or off — only meaningful on an ingredient
    /// that doubles as a fruit tray (e.g. Mixer-LemonJuice (1)); a no-op on anything else.
    /// </summary>
    public static void ApplyOnlyFruitTraySlot(GameObject target, bool enable)
    {
        if (target == null) return;
        if (target.TryGetComponent<DragableFruitTraySlot>(out var tray)) tray.SetHijackEnabled(enable);
    }

    public static void ApplyOnlyInteractable_2_5DObject(GameObject target, bool interactable)
    {
        if (target == null) return;
        if (target.TryGetComponent<Interactable_2_5DObject>(out var flat)) flat.Interactable = interactable;
    }

    public static void ApplyOnlyInteractable_3DObject(GameObject target, bool interactable)
    {
        if (target == null) return;
        if (target.TryGetComponent<Interactable_3DObject>(out var solid)) solid.Interactable = interactable;
    }

    /// <summary>
    /// Level 1 Prepare: bar-layout dragging and hover feedback stay on; pouring (click or
    /// bottle-drag) is off, and the release sound is muted since repositioning fires
    /// pointer-up constantly. One pass per target instead of "enable everything, then undo
    /// a few" — that shape is exactly what let DragableObject slip through un-reasoned-about
    /// before; a single explicit end-state per phase removes that whole class of mistake.
    /// </summary>
    public static void ApplyPrepareBarPhase(GameObject target)
    {
        if (target == null) return;

        ApplyOnlyInteractable_2_5DObject(target, false);
        ApplyOnlyInteractable_3DObject(target, true);
        ApplyOnlyDragDrop(target, true);
        ApplyOnlyScaleOnHover(target, true);
        ApplyOnlyHoverTooltip(target, true);
        ApplyOnlyUIPointerSound(target, true, canPlayUp: false);
        ApplyOnlyBookUI(target, true);
        ApplyOnlyBottleIngredientSource(target, false);
        ApplyOnlyFruitTraySlot(target, false);
    }

    /// <summary>
    /// Level 3 AddIngredient: pouring (click or bottle-drag) is on. DragableObject stays on
    /// here too — it is the same input listener BottleIngredientSource's own drag detection
    /// rides on (OnPointerDown/OnDrag both gate on DragableObject.Interactable), not a
    /// separate bar-layout switch, so turning it off would silently break pouring rather than
    /// just locking repositioning. BottleIngredientSource.OnDragEnded already forces the
    /// bottle back to its own spot on every release regardless, so leaving DragableObject on
    /// does not let bottles actually relocate outside Prepare.
    /// </summary>
    public static void ApplyPrepareDrinksPhase(GameObject target)
    {
        if (target == null) return;

        ApplyOnlyInteractable_2_5DObject(target, true);
        ApplyOnlyInteractable_3DObject(target, true);
        ApplyOnlyDragDrop(target, true);
        ApplyOnlyScaleOnHover(target, true);
        ApplyOnlyHoverTooltip(target, true);
        ApplyOnlyUIPointerSound(target, true);
        ApplyOnlyBookUI(target, true);
        ApplyOnlyBottleIngredientSource(target, true);
        ApplyOnlyFruitTraySlot(target, true);
    }
}
