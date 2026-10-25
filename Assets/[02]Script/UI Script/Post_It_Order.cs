// ============================================================
//  Post_It_Order.cs — The order note (Panel - OrderDirnks).
//
//  When an order is placed the note pops open: it grows out of its
//  corner, then the text fades in. After a moment it folds away and
//  a small button takes its place. Pressing the button unfolds the
//  note again; pressing the note folds it back.
//
//  The button only exists while the player is making the drink
//  (SetAvailable, driven by FlowWiring). The pop-up at order time
//  shows regardless, since the order is placed during the talk.
//
//  It always sits in the top-left corner.
// ============================================================

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Post_It_Order : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI OrderText;

    [Header("Parts")]
    [Tooltip("The full note. Grows from, and folds back into, its pivot corner.")]
    [SerializeField] private RectTransform _panel;

    [Tooltip("On the note: pressing it folds the note into the button.")]
    [SerializeField] private Button _btnClose;

    [Tooltip("The folded note. Pressing it unfolds the note.")]
    [SerializeField] private Button _btnOpen;

    [Header("Timing")]
    [Tooltip("Seconds the note stays open after an order pops it up.")]
    [SerializeField] private float autoHideDuration = 3f;
    [SerializeField] private float _scaleDuration = 0.25f;
    [SerializeField] private float _textFadeDuration = 0.2f;

    private enum Shape { Hidden, Button, Panel }

    private bool _hasOrder;
    private bool _available;
    private bool _open;
    private bool _dirty;
    private Shape _shown = Shape.Hidden;
    private Coroutine _morph;
    private Coroutine _autoFold;

    private void Awake()
    {
        if (_btnOpen != null) _btnOpen.onClick.AddListener(Unfold);
        if (_btnClose != null) _btnClose.onClick.AddListener(Fold);

        SetScale(_panel, 0f);
        if (_btnOpen != null) SetScale(_btnOpen.transform, 0f);
        if (OrderText != null) OrderText.alpha = 0f;

        PinTopLeft();
    }

    private void OnDestroy()
    {
        if (_btnOpen != null) _btnOpen.onClick.RemoveListener(Unfold);
        if (_btnClose != null) _btnClose.onClick.RemoveListener(Fold);
    }

    // ── In ─────────────────────────────────────────────────

    /// <summary>An order was placed: writes it on the note and pops the note open for a moment.</summary>
    public void SetPostItOrderText(string Order)
    {
        if (OrderText != null) OrderText.text = Order;
        _hasOrder = true;

        // Replaying a save re-runs the order commands; the note should not pop up for those.
        if (!SceneLoaderBridge.IsSilentReplay)
        {
            _open = true;
            CancelAutoFold();
            _autoFold = StartCoroutine(AutoFold());
        }

        _dirty = true;
    }

    /// <summary>The order is over (served and scored): nothing is shown until the next one.</summary>
    public void ClearOrder()
    {
        _hasOrder = false;
        _open = false;
        CancelAutoFold();
        _dirty = true;
    }

    /// <summary>Whether the folded button may be on screen — true only while the drink is being made.</summary>
    public void SetAvailable(bool available)
    {
        _available = available;
        _dirty = true;
    }

    private void Unfold()
    {
        CancelAutoFold(); // opened by hand: it stays until folded by hand
        _open = true;
        _dirty = true;
    }

    private void Fold()
    {
        CancelAutoFold();
        _open = false;
        _dirty = true;
    }

    // ── Shape ──────────────────────────────────────────────

    // Resolved once per frame, so a step change that turns the button off and straight back on
    // (Prepare Drinks → Garnish) never makes it flicker.
    private void LateUpdate()
    {
        if (!_dirty) return;
        _dirty = false;

        Shape wanted = !_hasOrder ? Shape.Hidden
            : _open ? Shape.Panel
            : _available ? Shape.Button
            : Shape.Hidden;

        if (wanted == _shown) return;
        _shown = wanted;

        if (_morph != null) StopCoroutine(_morph);
        _morph = StartCoroutine(Morph(wanted));
    }

    // Always from wherever the parts are now, so an interrupted change carries on smoothly.
    private IEnumerator Morph(Shape to)
    {
        Transform button = _btnOpen != null ? _btnOpen.transform : null;

        if (to != Shape.Panel)
        {
            yield return FadeText(0f);
            yield return ScaleTo(_panel, 0f);
        }

        if (to != Shape.Button) yield return ScaleTo(button, 0f);

        if (to == Shape.Panel)
        {
            yield return ScaleTo(_panel, 1f);
            yield return FadeText(1f);
        }

        if (to == Shape.Button) yield return ScaleTo(button, 1f);

        _morph = null;
    }

    private IEnumerator ScaleTo(Transform target, float to)
    {
        if (target == null) yield break;

        float from = target.localScale.x;
        if (Mathf.Approximately(from, to)) yield break;

        for (float e = 0f; e < _scaleDuration; e += Time.deltaTime)
        {
            float t = e / _scaleDuration;
            // Growing overshoots a little (ease-out-back) so it pops; shrinking just eases.
            float eased = to > from ? 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f) : t * t;
            SetScale(target, Mathf.LerpUnclamped(from, to, eased));
            yield return null;
        }
        SetScale(target, to);
    }

    private IEnumerator FadeText(float to)
    {
        if (OrderText == null) yield break;

        float from = OrderText.alpha;
        if (Mathf.Approximately(from, to)) yield break;

        for (float e = 0f; e < _textFadeDuration; e += Time.deltaTime)
        {
            OrderText.alpha = Mathf.Lerp(from, to, e / _textFadeDuration);
            yield return null;
        }
        OrderText.alpha = to;
    }

    // A part folded to nothing is switched off, so it cannot catch a click.
    private static void SetScale(Transform target, float scale)
    {
        if (target == null) return;

        target.localScale = new Vector3(scale, scale, 1f);
        target.gameObject.SetActive(scale > 0f);
    }

    // The root and both parts are pinned to the top-left corner, so they grow out of it.
    // The gap to the corner is taken from the authored position.
    private void PinTopLeft()
    {
        var corner = new Vector2(0f, 1f);
        Vector2 authored = ((RectTransform)transform).anchoredPosition;

        Pin((RectTransform)transform, corner, new Vector2(Mathf.Abs(authored.x), -Mathf.Abs(authored.y)));
        Pin(_panel, corner, Vector2.zero);
        if (_btnOpen != null) Pin((RectTransform)_btnOpen.transform, corner, Vector2.zero);
    }

    private static void Pin(RectTransform rect, Vector2 corner, Vector2 position)
    {
        if (rect == null) return;

        rect.anchorMin = rect.anchorMax = rect.pivot = corner;
        rect.anchoredPosition = position;
    }

    private IEnumerator AutoFold()
    {
        yield return new WaitForSeconds(autoHideDuration);
        _autoFold = null;
        _open = false;
        _dirty = true;
    }

    private void CancelAutoFold()
    {
        if (_autoFold != null) StopCoroutine(_autoFold);
        _autoFold = null;
    }
}
