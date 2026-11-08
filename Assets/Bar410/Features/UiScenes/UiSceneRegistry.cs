using UnityEngine;

namespace Bar410.UiScenes
{
    public enum UiSceneId { Minigame, Garnish }

    public interface IUiSceneRegistry
    {
        void Register(UiSceneId id, object host);
        void Unregister(UiSceneId id, object host);
    }

    /// <summary>
    /// The one static handshake a UI scene can reach without seeing GameRoot (Assembly-CSharp).
    /// GameRoot calls Set in Awake and Clear in OnDestroy; UiSceneEntry registers through Current.
    /// </summary>
    public static class UiSceneRegistry
    {
        public static IUiSceneRegistry Current { get; private set; }

        /// <summary>A second Set with another instance logs an error and is ignored.</summary>
        public static void Set(IUiSceneRegistry registry)
        {
            if (Current != null && Current != registry)
            {
                Debug.LogError("[UiScenes] UiSceneRegistry.Set called twice; the second registry is ignored.");
                return;
            }
            Current = registry;
        }

        /// <summary>Clear with a different instance is a no-op.</summary>
        public static void Clear(IUiSceneRegistry registry)
        {
            if (Current == registry) Current = null;
        }

        // Enter Play Mode options may skip the domain reload; never carry a registry into the next play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;
    }
}
