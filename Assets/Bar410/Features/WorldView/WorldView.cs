using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bar410.WorldViews
{
    /// <summary>
    /// On the RawImage (UI scene): shows a handle's texture, relays clicks into the world and owns raycast blocking.
    /// Hide() clears both raycastTarget and blocksRaycasts so a hidden view never eats clicks.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class WorldView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] CanvasGroup _group;   // optional

        RawImage _image;
        WorldViewHandle _handle;

        public bool IsShown { get; private set; }

        void Awake() { _image = GetComponent<RawImage>(); ApplyShown(false); }

        public void Bind(WorldViewHandle h)
        {
            if (_handle != null && _handle != h) Unbind();
            _handle = h;
            if (_image == null) _image = GetComponent<RawImage>();
            if (h != null) _image.texture = h.Texture;
        }

        public void Unbind() { Hide(); _handle = null; }

        public void Show()
        {
            if (_handle == null) { Debug.LogWarning("[WorldView] Show before Bind", this); return; }
            ApplyShown(true);
            _handle.SetRendering(true);
        }

        public void Hide()
        {
            ApplyShown(false);
            _handle?.SetRendering(false);
        }

        void ApplyShown(bool shown)
        {
            IsShown = shown;
            if (_image == null) _image = GetComponent<RawImage>();
            _image.raycastTarget = shown;
            if (_group != null) _group.blocksRaycasts = shown;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!IsShown || _handle == null) return;
            var rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var local)) return;
            var n = ToViewport(Rect.PointToNormalized(rect.rect, local), _image.uvRect);
            if (_handle.TryPick(n, out var hit))
                ExecuteEvents.ExecuteHierarchy(hit, e, ExecuteEvents.pointerClickHandler);
        }

        /// <summary>The image may show only part of the texture (GarnishManager.RevealServe animates uvRect).</summary>
        public static Vector2 ToViewport(Vector2 normalized, Rect uv) =>
            new Vector2(uv.x + normalized.x * uv.width, uv.y + normalized.y * uv.height);
    }
}
