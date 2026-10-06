// ============================================================
//  InteractableToggle.cs — the one place that knows what
//  "enable this object for the player" means.
//
//  Bug B4: three separate loops did this, each touching a different
//  component subset — SetIngredientActive (6 types), SetBookUiActive
//  (7 types), EnableButtonInYarn (4 types, run right after
//  SetIngredientActive, double-setting the same objects).
//  Adding a component used to mean updating all three; now just Apply().
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public static class InteractableToggle
{
    /// <summary>
    /// Enables/disables every interaction component on <paramref name="target"/>.
    /// Missing components are skipped — any subset is fine.
    /// </summary>
    public static void Apply(GameObject target, bool interactable)
    {
        if (target == null) return;

        if (target.TryGetComponent<Button>(out var button)) button.interactable = interactable;
        if (target.TryGetComponent<Interactable_2_5DObject>(out var flat)) flat.Interactable = interactable;
        if (target.TryGetComponent<Interactable_3DObject>(out var solid)) solid.Interactable = interactable;
        if (target.TryGetComponent<DragableObject>(out var drag)) drag.Interactable = interactable;
        if (target.TryGetComponent<ScaleOnHover>(out var scale)) scale.Interactable = interactable;
        if (target.TryGetComponent<HoverTooltip>(out var tooltip)) tooltip.Interactable = interactable;
        if (target.TryGetComponent<UIPointerSound>(out var sound)) sound.Interactable = interactable;
        if (target.TryGetComponent<BookUI_V2>(out var book)) book.SetActive(interactable);

        // Separate check: BottleIngredientSource doesn't inherit Interactable_2_5DObject/3DObject.
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

    public static void ApplyOnlyButton(GameObject target, bool interactable)
    {
        if (target == null) return;
        if (target.TryGetComponent<Button>(out var button)) button.interactable = interactable;
    }

    /// <summary>
    /// Sets pointer-sound interactability. Pass <paramref name="canPlayUp"/> only to make
    /// pointer-up differ from <paramref name="interactable"/> (e.g. muted while rest stays on);
    /// leave null to let Interactable's OnInteractableChanged hook decide as usual.
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
    /// Toggles a DragableFruitTraySlot's drag-hijack — meaningful only on an ingredient that
    /// doubles as a fruit tray (e.g. Mixer-LemonJuice (1)); no-op otherwise.
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
    /// Level 3 AddIngredient: pouring (click or bottle-drag) on. DragableObject stays on too
    /// — BottleIngredientSource's drag detection gates on DragableObject.Interactable
    /// (OnPointerDown/OnDrag), not a separate switch, so disabling it would silently break
    /// pouring, not just lock repositioning. BottleIngredientSource.OnDragEnded forces the
    /// bottle back to its spot on every release regardless, so it can't relocate outside Prepare.
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
        //ApplyOnlyBookUI(target, true);
        ApplyOnlyButton(target, true);

        ApplyOnlyBottleIngredientSource(target, true);
        ApplyOnlyFruitTraySlot(target, true);
    }

    //this migh be use Apply(flase) instead
    public static void ApplyAddIngredientFull(GameObject target) {

        if(target == null) return;

        ApplyOnlyInteractable_2_5DObject(target, false);
        ApplyOnlyInteractable_3DObject(target, false);
        ApplyOnlyDragDrop(target, false);
        ApplyOnlyScaleOnHover(target, false);
        ApplyOnlyHoverTooltip(target, false);
        ApplyOnlyUIPointerSound(target, false);
        //ApplyOnlyBookUI(target, true);
        ApplyOnlyButton(target, false);

        ApplyOnlyBottleIngredientSource(target, false);
        ApplyOnlyFruitTraySlot(target, false);


    }
}
