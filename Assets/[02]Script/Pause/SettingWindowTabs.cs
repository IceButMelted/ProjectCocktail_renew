using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tab bar of the Settings page: each button shows its own panel and hides the others.
/// Every time the Settings page opens it starts on <see cref="m_DefaultTab"/>.
/// The tab panels must not carry a PausePanel, or Esc would close a tab instead of Settings.
/// </summary>
public class SettingWindowTabs : MonoBehaviour
{
    [System.Serializable]
    private struct Tab
    {
        public Button Button;
        public GameObject Panel;
    }

    [SerializeField] private List<Tab> m_Tabs = new();
    [SerializeField, Min(0)] private int m_DefaultTab;

    private readonly List<UnityEngine.Events.UnityAction> m_Listeners = new();

    private void OnEnable()
    {
        for (int i = 0; i < m_Tabs.Count; i++)
        {
            int index = i;
            UnityEngine.Events.UnityAction show = () => Show(index);
            m_Listeners.Add(show);
            if (m_Tabs[i].Button != null) m_Tabs[i].Button.onClick.AddListener(show);
        }
        Show(m_DefaultTab);
    }

    private void OnDisable()
    {
        for (int i = 0; i < m_Tabs.Count && i < m_Listeners.Count; i++)
            if (m_Tabs[i].Button != null) m_Tabs[i].Button.onClick.RemoveListener(m_Listeners[i]);
        m_Listeners.Clear();
    }

    public void Show(int index)
    {
        for (int i = 0; i < m_Tabs.Count; i++)
            if (m_Tabs[i].Panel != null) m_Tabs[i].Panel.SetActive(i == index);
    }
}
