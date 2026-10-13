// ============================================================
//  PourVisual.cs — The game's one door into CocktailCrafter_V2's
//  PourController: "pour this colour from this sprite into that
//  container, and tell me when it is done".
//
//  Sits on the PourController object. The container is any
//  CocktailGlass (a serving glass today, the shaker later), so the
//  callers never touch PourController or CocktailMenu themselves.
// ============================================================

using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PourController))]
public class PourVisual : MonoBehaviour
{
    [Tooltip("Shows whatever is pouring. Sprites are drawn upright with the mouth at the top centre, so the stream starts there for every sprite.")]
    [SerializeField] private SpriteRenderer _source;

    [Tooltip("Optional. Hidden for the length of the pour — the resting object the pouring sprite stands in for.")]
    [SerializeField] private Renderer _hideWhilePouring;

    private PourController _pour;
    private CocktailMenu _menu;
    private Coroutine _routine;

    public bool IsPouring => _routine != null;

    private void Awake()
    {
        _pour = GetComponent<PourController>();
        _menu = ScriptableObject.CreateInstance<CocktailMenu>();
        PlaceMouth();
    }

    private void OnDestroy() => Destroy(_menu);

    /// <summary>
    /// Pours into <paramref name="target"/>. Waits for its ice to land first. Ignored while another
    /// pour is running. <paramref name="source"/> null keeps the sprite already on the prefab.
    /// </summary>
    public void Pour(CocktailGlass target, Color top, Color bottom, Sprite source, Action onComplete)
    {
        if (IsPouring || target == null) return;

        _menu.top = top;
        _menu.bottom = bottom;
        if (source != null && _source != null)
        {
            _source.sprite = source;
            PlaceMouth();
        }

        _routine = StartCoroutine(PourRoutine(target, onComplete));
    }

    /// <summary>Drops the running pour without completing it. Call before its container is destroyed.</summary>
    public void Cancel()
    {
        if (_routine == null) return;

        StopCoroutine(_routine);
        _routine = null;
        _pour.Cancel();
        ShowResting(true);
    }

    private IEnumerator PourRoutine(CocktailGlass target, Action onComplete)
    {
        while (target != null && target.IsDroppingIce) yield return null;

        if (target != null)
        {
            ShowResting(false);
            _pour.Pour(target, _menu);
            while (_pour.IsPouring) yield return null;
            ShowResting(true);
        }

        _routine = null;
        if (target != null) onComplete?.Invoke();
    }

    private void PlaceMouth()
    {
        if (_source == null || _source.sprite == null) return;

        Bounds b = _source.sprite.bounds;
        _pour.bottleMouth.position = _source.transform.TransformPoint(new Vector3(b.center.x, b.max.y, 0f));
    }

    private void ShowResting(bool visible)
    {
        if (_hideWhilePouring != null) _hideWhilePouring.enabled = visible;
    }
}
