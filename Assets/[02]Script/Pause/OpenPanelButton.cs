using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Opens one pause-UI layer directly, skipping the pause menu (MainMenu's Settings button). A button outside the
/// Pause scene can't reference its layers, so this asks <see cref="PauseManager"/> by key. Does nothing when the
/// scene has no PauseManager.
/// </summary>
[RequireComponent(typeof(Button))]
public class OpenPanelButton : MonoBehaviour
{
    [Tooltip("Key of the PausePanel to open (its Key field, or its GameObject name when empty).")]
    [SerializeField] private string m_Key = "Settings";

    private void OnEnable() => GetComponent<Button>().onClick.AddListener(Open);
    private void OnDisable() => GetComponent<Button>().onClick.RemoveListener(Open);

    private void Open() => PauseManager.OpenPanel(m_Key);
}
