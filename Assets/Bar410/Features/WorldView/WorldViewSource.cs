using UnityEngine;

namespace Bar410.WorldViews
{
    /// <summary>Main-scene side of a handle. Builds it in Awake and switches the camera off (WorldView owns camera.enabled from then on).</summary>
    public sealed class WorldViewSource : MonoBehaviour
    {
        [SerializeField] string _id;
        [SerializeField] Camera _camera;
        [Tooltip("Optional: the camera's targetTexture is used when empty.")]
        [SerializeField] RenderTexture _texture;
        [Tooltip("Layers a click may hit through this view. Default: Garnish layer only (set in the Inspector).")]
        [SerializeField] LayerMask _clickMask;

        public WorldViewHandle Handle { get; private set; }

        void Awake()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null) { Debug.LogError($"[WorldView] '{_id}' has no camera", this); return; }
            var tex = _texture != null ? _texture : _camera.targetTexture;
            if (tex == null) Debug.LogWarning($"[WorldView] '{_id}' has no target texture", this);
            if (_camera.TryGetComponent<AudioListener>(out _)) Debug.LogWarning($"[WorldView] '{_id}' camera has an AudioListener", this);
            if (_camera.TryGetComponent<Unity.Cinemachine.CinemachineBrain>(out _)) Debug.LogWarning($"[WorldView] '{_id}' camera has a CinemachineBrain", this);
            _camera.enabled = false;
            Handle = new WorldViewHandle(_id, _camera, tex, _clickMask);
        }

        /// <summary>Render once at preload so shaders compile before the first real Show.</summary>
        public void Warm()
        {
            if (_camera == null) return;
            bool was = _camera.enabled;
            _camera.Render();
            _camera.enabled = was;
        }
    }
}
