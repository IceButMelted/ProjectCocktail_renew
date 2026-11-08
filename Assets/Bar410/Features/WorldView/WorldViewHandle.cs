using UnityEngine;

// Namespace is Bar410.WorldViews (not Bar410.WorldView): a namespace equal to a type name breaks unqualified lookups (Contracts_Final 2).
namespace Bar410.WorldViews
{
    /// <summary>
    /// Runtime reference to a main-scene render source that a UI scene can show. One owner per handle, no ref-counting.
    /// A destroyed camera (main scene unloaded) makes SetRendering/TryPick no-ops that return false, with one warning.
    /// </summary>
    public sealed class WorldViewHandle
    {
        bool _warned;

        public WorldViewHandle(string id, Camera camera, RenderTexture texture, LayerMask clickMask)
        { Id = id; Camera = camera; Texture = texture; ClickMask = clickMask; }

        public string Id { get; }
        public Camera Camera { get; }
        public RenderTexture Texture { get; }
        public LayerMask ClickMask { get; }

        public bool IsRendering => Alive() && Camera.enabled;

        public void SetRendering(bool on) { if (Alive()) Camera.enabled = on; }

        /// <summary>Raycast through the picture. Only ClickMask layers are hit (old relay hit everything).</summary>
        public bool TryPick(Vector2 viewportPoint, out GameObject hit)
        {
            hit = null;
            if (!Alive()) return false;
            var ray = Camera.ViewportPointToRay(viewportPoint);
            if (!Physics.Raycast(ray, out var info, Camera.farClipPlane, ClickMask)) return false;
            hit = info.collider.gameObject;
            return true;
        }

        bool Alive()
        {
            if (Camera != null) return true;
            if (!_warned) { _warned = true; Debug.LogWarning($"[WorldView] camera of '{Id}' is destroyed"); }
            return false;
        }
    }
}
