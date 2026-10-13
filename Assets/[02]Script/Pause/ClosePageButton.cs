using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Closes the whole pause UI (Resume). A button inside the Pause scene can't reference PauseManager
/// (it lives in another scene), so this does the call. To step back one layer inside the Pause scene,
/// point the button at <see cref="PausePanel.Hide"/> instead.
/// </summary>
[RequireComponent(typeof(Button))]
public class ClosePageButton : MonoBehaviour
{
    private void OnEnable() => GetComponent<Button>().onClick.AddListener(PauseManager.Close);
    private void OnDisable() => GetComponent<Button>().onClick.RemoveListener(PauseManager.Close);
}
