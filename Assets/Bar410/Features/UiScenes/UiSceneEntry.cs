using UnityEngine;
using UnityEngine.EventSystems;

namespace Bar410.UiScenes
{
    /// <summary>
    /// One per UI scene, on an object that stays active (Awake never runs on inactive objects; hide with
    /// Canvas.enabled / CanvasGroup instead of SetActive). Awake switches off any EventSystem, AudioListener,
    /// CinemachineBrain or Camera in its scene (the main scene owns those), then registers its host through
    /// <see cref="UiSceneRegistry.Current"/>. With no registry the scene runs standalone (<see cref="OnStandalone"/>).
    /// </summary>
    public abstract class UiSceneEntry : MonoBehaviour
    {
        [SerializeField] UiSceneId _id;

        IUiSceneRegistry _registeredWith;

        public UiSceneId Id => _id;

        /// <summary>The object the main scene talks to (MinigameUiHost, GarnishUiHost).</summary>
        protected abstract object Host { get; }

        protected virtual void Awake()
        {
            DisableForbidden();

            var registry = UiSceneRegistry.Current;
            if (registry == null)
            {
                Debug.Log($"[UiScenes] '{_id}' has no registry: running standalone.", this);
                OnStandalone();
                return;
            }
            registry.Register(_id, Host);
            _registeredWith = registry;
        }

        protected virtual void OnDestroy() => _registeredWith?.Unregister(_id, Host);

        /// <summary>Dev hook when the scene is opened on its own (no GameRoot).</summary>
        protected virtual void OnStandalone() { }

        // Disabled, not destroyed: components that require them (input modules, URP camera data) block Destroy.
        void DisableForbidden()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                Disable<EventSystem>(root);
                Disable<AudioListener>(root);
                Disable<Unity.Cinemachine.CinemachineBrain>(root);
                Disable<Camera>(root);
            }
        }

        void Disable<T>(GameObject root) where T : Behaviour
        {
            foreach (var c in root.GetComponentsInChildren<T>(true))
            {
                if (!c.enabled) continue;
                Debug.LogError($"[UiScenes] UI scene '{gameObject.scene.name}' has a {typeof(T).Name} on '{c.name}'; disabled (the main scene owns it).", c);
                c.enabled = false;
            }
        }
    }
}
