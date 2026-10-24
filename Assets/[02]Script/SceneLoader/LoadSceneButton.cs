using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A button that loads a scene through the loading scene when it is pressed. The button owns what
/// it does, so nothing else has to know which scene "end shift" or "thanks" leads to.
/// </summary>
[RequireComponent(typeof(Button))]
public class LoadSceneButton : MonoBehaviour
{
    [Tooltip("Found on this object or a parent when left empty.")]
    [SerializeField] private SceneLoader _loader;
    [SerializeField] private string _scene = "MainMenu";

    private Button _button;

    private void Awake()
    {
        if (_loader == null) _loader = GetComponentInParent<SceneLoader>(true);
        _button = GetComponent<Button>();
        _button.onClick.AddListener(Load);
    }

    private void OnDestroy()
    {
        if (_button != null) _button.onClick.RemoveListener(Load);
    }

    private void Load()
    {
        if (_loader == null)
        {
            Debug.LogWarning("[LoadSceneButton] No SceneLoader on this object or a parent.", this);
            return;
        }

        _loader.LoadScene(_scene);
    }
}
