using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bar410.UiScenes
{
    /// <summary>
    /// Loads the additive UI scenes and hands out their hosts. GameRoot owns one instance and publishes it with
    /// <see cref="UiSceneRegistry.Set"/> so each scene's <see cref="UiSceneEntry"/> can register.
    /// Protocol per id: LoadSceneAsync(Additive) → the active scene must not change → poll until the entry has
    /// registered (timeout → <see cref="TimeoutException"/>) → raise <see cref="Registered"/> → ready.
    /// Several waiters on the same id share one load by polling (an Awaitable is never awaited twice).
    /// Scenes are in Build Settings (name-based load). No explicit unload: they go with the main scene (Single load).
    /// </summary>
    public sealed class UiSceneLoader : IUiSceneRegistry
    {
        readonly IReadOnlyDictionary<UiSceneId, string> _sceneNames;
        readonly float _registerTimeout;
        readonly Dictionary<UiSceneId, object> _hosts = new();
        readonly HashSet<UiSceneId> _ready = new();
        readonly HashSet<UiSceneId> _loading = new();

        public UiSceneLoader(IReadOnlyDictionary<UiSceneId, string> sceneNames, float registerTimeoutSeconds = 10f)
        {
            _sceneNames = sceneNames;
            _registerTimeout = registerTimeoutSeconds;
        }

        /// <summary>Raised once per registration, after the scene loaded and before it counts as ready. Glue binds world objects here.</summary>
        public event Action<UiSceneId, object> Registered;

        public bool IsReady(UiSceneId id) => _ready.Contains(id);

        // ── IUiSceneRegistry ───────────────────────────────

        public void Register(UiSceneId id, object host)
        {
            if (host == null) { Debug.LogError($"[UiScenes] Register({id}) with a null host."); return; }
            if (_hosts.TryGetValue(id, out var existing) && existing != host)
            {
                Debug.LogError($"[UiScenes] '{id}' is already registered; the second registration is ignored.");
                return;
            }
            _hosts[id] = host;
        }

        public void Unregister(UiSceneId id, object host)
        {
            if (!_hosts.TryGetValue(id, out var existing) || existing != host) return;
            _hosts.Remove(id);
            _ready.Remove(id);
        }

        // ── Loading ────────────────────────────────────────

        /// <summary>Loads every known UI scene. Idempotent. Call at BeforeOpen.</summary>
        public async Awaitable PreloadAsync(CancellationToken ct)
        {
            foreach (var id in _sceneNames.Keys)
                await EnsureAsync(id, ct);
        }

        /// <summary>The host of a UI scene; loads it first if needed (slow path, warns).</summary>
        public async Awaitable<T> GetHostAsync<T>(UiSceneId id, CancellationToken ct) where T : class
        {
            if (!IsReady(id))
            {
                Debug.LogWarning($"[UiScenes] '{id}' was not preloaded; loading it now.");
                await EnsureAsync(id, ct);
            }
            if (_hosts[id] is T host) return host;
            throw new InvalidCastException($"[UiScenes] host of '{id}' is {_hosts[id].GetType().Name}, not {typeof(T).Name}");
        }

        async Awaitable EnsureAsync(UiSceneId id, CancellationToken ct)
        {
            if (_ready.Contains(id)) return;

            if (_loading.Contains(id))   // another caller is loading it: share that load
            {
                while (_loading.Contains(id)) await Awaitable.NextFrameAsync(ct);
                if (!_ready.Contains(id)) throw new InvalidOperationException($"[UiScenes] loading '{id}' failed (see the first caller's error)");
                return;
            }

            if (!_sceneNames.TryGetValue(id, out var sceneName))
                throw new ArgumentException($"[UiScenes] no scene name for '{id}'");

            _loading.Add(id);
            try
            {
                var active = SceneManager.GetActiveScene();
                if (!_hosts.ContainsKey(id) && !SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    if (op == null) throw new InvalidOperationException($"[UiScenes] cannot load '{sceneName}': is it in Build Settings?");
                    await Awaitable.FromAsyncOperation(op, ct);
                }

                // Lighting and probes come from the active scene: a UI scene must never become active.
                if (SceneManager.GetActiveScene() != active)
                {
                    Debug.LogError($"[UiScenes] loading '{sceneName}' changed the active scene; restoring '{active.name}'.");
                    SceneManager.SetActiveScene(active);
                }

                float deadline = Time.realtimeSinceStartup + _registerTimeout;
                while (!_hosts.ContainsKey(id))
                {
                    if (Time.realtimeSinceStartup > deadline)
                        throw new TimeoutException($"[UiScenes] '{sceneName}' loaded but no UiSceneEntry registered '{id}' within {_registerTimeout} s (is its root active? is UiSceneRegistry set?)");
                    await Awaitable.NextFrameAsync(ct);
                }

                Registered?.Invoke(id, _hosts[id]);
                _ready.Add(id);
            }
            finally { _loading.Remove(id); }
        }
    }
}
