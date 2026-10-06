using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Pauses the game (timeScale 0 + SoundManager.SetPaused) while any <see cref="PausePanel"/> is enabled, and owns
/// the additive Pause scene (preloaded, kept hidden). Scene-scoped singleton: duplicates destroy themselves and
/// there is no DontDestroyOnLoad, so timeScale 0 can never outlive its scene.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    /// <summary>True while time is frozen by an open pause panel.</summary>
    public static bool IsPaused => Instance != null && Instance.m_Paused;

    [Tooltip("Freeze time and pause SFX/Voice while a panel is open. Off in MainMenu (Settings opens without freezing anything).")]
    [SerializeField] private bool m_PauseTime = true;

    [Tooltip("Additive UI scene preloaded and kept hidden (must be in Build Settings). Empty = no pause scene, Esc only steps back through open panels.")]
    [SerializeField] private string m_PauseScene = "PauseScene";

    [Tooltip("Esc opens the pause menu when nothing is open. Off in MainMenu, where Esc only closes what is open.")]
    [SerializeField] private bool m_EscOpensPauseMenu = true;

    private readonly List<GameObject> m_Roots = new();      // Pause-scene roots that contain a PausePanel (toggled on open/close)
    private readonly List<PausePanel> m_Panels = new();     // every layer in the Pause scene (for OpenPanel by key)
    private readonly List<PausePanel> m_Bases = new();      // base layers, shown on every open
    private bool m_Open;        // pause scene roots are active
    private bool m_Direct;      // opened straight to one layer (MainMenu Settings): closing it closes the whole scene
    private bool m_Paused;      // we set timeScale 0
    private bool m_Preloading;  // panels enabled by the scene load must not pause the game

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;                // keep Start() from running before the deferred Destroy
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(m_PauseScene)) StartCoroutine(Preload());
        Apply();                            // panels that were already enabled before this Awake
    }

    private void Update()
    {
        // Direct mode has no base layer: once its layer is hidden (Back button or Esc) hide the backdrop too.
        if (m_Direct && m_Open && !PausePanel.AnyOpen) CloseInternal();

        if (!EscapePressed()) return;

        if (PausePanel.CloseTopSub()) return;           // Settings / Save-Load step back first
        if (m_Open) CloseInternal();
        else if (m_EscOpensPauseMenu && m_Roots.Count > 0) OpenInternal();     // pause scene is ready
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;

        if (m_Paused)
        {
            Time.timeScale = 1f;
            SoundManager.Instance?.SetPaused(false);
        }
    }

    // ── Public API (null-safe: scenes without a PauseManager just do nothing) ─

    public static void Open() { if (Instance != null) Instance.OpenInternal(); }
    public static void Close() { if (Instance != null) Instance.CloseInternal(); }

    /// <summary>Opens one layer of the Pause scene directly, without the pause menu (e.g. "Settings" from MainMenu).</summary>
    public static void OpenPanel(string key) { if (Instance != null) Instance.OpenPanelInternal(key); }

    /// <summary>Called by <see cref="PausePanel"/> whenever a panel is enabled or disabled.</summary>
    public static void Refresh() { if (Instance != null) Instance.Apply(); }

    // ── Internals ─────────────────────────────────────────────────────────────

    private void OpenInternal()
    {
        if (m_Open || m_Roots.Count == 0) return;
        m_Open = true;
        m_Direct = false;
        foreach (var go in m_Roots) go.SetActive(true);
        foreach (var p in m_Bases) p.Show();            // every layer starts hidden, so the base must be shown
    }

    private void OpenPanelInternal(string key)
    {
        if (m_Open || m_Roots.Count == 0) return;

        var target = m_Panels.Find(p => p.Matches(key));
        if (target == null)
        {
            Debug.LogWarning($"[PauseManager] No PausePanel with key '{key}' in '{m_PauseScene}'.", this);
            return;
        }

        m_Open = true;
        m_Direct = true;
        foreach (var go in m_Roots) go.SetActive(true);
        target.Show();                                  // bases stay hidden: skip the pause menu
    }

    private void CloseInternal()
    {
        if (!m_Open) return;
        m_Open = false;
        m_Direct = false;
        PausePanel.CloseAllSub();           // next open starts from the base layer
        foreach (var go in m_Roots) go.SetActive(false);
    }

    private void Apply()
    {
        bool paused = !m_Preloading && m_PauseTime && PausePanel.AnyOpen;
        if (paused != m_Paused)
        {
            m_Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        // Idempotent; also re-evaluates the music duck when a Settings panel opens or closes while paused.
        SoundManager.Instance?.SetPaused(paused, !PausePanel.AnyReleasesDuck);
    }

    private IEnumerator Preload()
    {
        m_Preloading = true;

        var scene = SceneManager.GetSceneByName(m_PauseScene);
        if (!scene.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(m_PauseScene, LoadSceneMode.Additive);
            if (op == null)
            {
                m_Preloading = false;
                Debug.LogError($"[PauseManager] Scene '{m_PauseScene}' can't be loaded — add it to Build Settings.", this);
                yield break;
            }
            yield return op;
            scene = SceneManager.GetSceneByName(m_PauseScene);
        }

        foreach (var go in scene.GetRootGameObjects())
        {
            var panels = go.GetComponentsInChildren<PausePanel>(true);
            foreach (var p in panels)
            {
                m_Panels.Add(p);
                if (p.IsBase) m_Bases.Add(p);
                p.Hide();                               // every layer starts closed whatever state it was saved in; Open() shows the base
            }

            // One canvas for the whole scene: sort it above everything (HoverTooltip is 9999); layers stack by sibling order.
            if (go.TryGetComponent(out Canvas canvas)) canvas.sortingOrder = PausePanel.SortBase;

            go.SetActive(false);                        // roots without a PausePanel (a test EventSystem, a light…) stay off for good
            if (panels.Length > 0) m_Roots.Add(go);
        }

        m_Preloading = false;
        Apply();
    }

    private static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
