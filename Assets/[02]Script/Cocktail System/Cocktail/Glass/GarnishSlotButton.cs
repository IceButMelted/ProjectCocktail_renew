// ============================================================
//  GarnishSlotButton.cs — Click detector for one of a placed
//  glass's 2 garnish slots. Sits alongside the slot's SpriteRenderer
//  (which keeps showing whatever garnish item is placed there) —
//  clicking it only selects the slot as the active target for the
//  next Garnish Item button press. Does not apply anything itself
//  and never touches the sprite, so it can't stomp the content
//  sprite the way a hover/click sprite-swap button would.
// ============================================================

using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class GarnishSlotButton : MonoBehaviour, IPointerClickHandler
{
    public event Action Clicked;

    public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();
}
