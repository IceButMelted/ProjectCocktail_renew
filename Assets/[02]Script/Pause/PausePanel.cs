using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put on the root of every pause-UI layer (menu, Settings, Save/Load). While a panel is enabled it counts as open:
/// <see cref="PauseManager"/> freezes the game, Esc steps back through the layers, and the Canvas on this
/// object (if any) is sorted above everything else, stacked by open order.
/// </summary>
public class PausePanel : MonoBehaviour
{
    /// <summary>Just above HoverTooltip's canvas (9999).</summary>
    public const int SortBase = 10000;

    private static readonly List<PausePanel> s_Open = new();

    [Tooltip("The pause menu itself (base layer of the Pause scene). Esc closes the whole pause instead of hiding this panel alone.")]
    [SerializeField] private bool m_IsBase;

    [Tooltip("Music/Ambient play at full volume while this panel is open (Settings, so volume changes are audible).")]
    [SerializeField] private bool m_ReleaseMusicDuck;

    [Tooltip("Name PauseManager.OpenPanel(key) uses to open this layer directly (e.g. Settings from MainMenu). Empty = the GameObject name.")]
    [SerializeField] private string m_Key;

    public bool IsBase => m_IsBase;

    public bool Matches(string key) =>
        string.Equals(string.IsNullOrEmpty(m_Key) ? name : m_Key, key, System.StringComparison.OrdinalIgnoreCase);

    public static bool AnyOpen => s_Open.Count > 0;

    public static bool AnyReleasesDuck
    {
        get
        {
            foreach (var p in s_Open)
                if (p.m_ReleaseMusicDuck) return true;
            return false;
        }
    }

    // For Button.onClick in the Inspector (same-scene references work, unlike PauseManager).
    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    private void OnEnable()
    {
        s_Open.Add(this);
        if (TryGetComponent(out Canvas canvas)) canvas.sortingOrder = SortBase + s_Open.Count;
        PauseManager.Refresh();
    }

    private void OnDisable()
    {
        s_Open.Remove(this);
        PauseManager.Refresh();
    }

    /// <summary>Hides the most recently opened non-base panel. False when there is none.</summary>
    public static bool CloseTopSub()
    {
        for (int i = s_Open.Count - 1; i >= 0; i--)
        {
            if (s_Open[i].m_IsBase) continue;
            s_Open[i].Hide();
            return true;
        }
        return false;
    }

    /// <summary>Hides every non-base panel so the next open starts from the base layer.</summary>
    public static void CloseAllSub()
    {
        foreach (var p in s_Open.ToArray())
            if (!p.m_IsBase) p.Hide();
    }
}
