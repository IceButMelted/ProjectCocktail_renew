using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TMPro.TextMeshProUGUI loadingText;

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 5f;

    private float _displayProgress = 0f;

    void Start()
    {
        LoadScene(SceneLoaderBridge.TargetScene);
    }

    public void LoadScene(string sceneName, System.Action onComplete = null)
    {
        StartCoroutine(LoadAsync(sceneName, onComplete));
    }

    private IEnumerator LoadAsync(string sceneName, System.Action onComplete = null)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            _displayProgress = Mathf.Lerp(
                _displayProgress,
                targetProgress,
                Time.deltaTime * smoothSpeed
            );

            loadingBar.value = _displayProgress;

            if (loadingText != null)
                loadingText.text = $"{Mathf.RoundToInt(_displayProgress * 100f)}%";

            if (_displayProgress >= 0.99f)
            {
                yield return new WaitForSeconds(0.3f);

                // Fire callback BEFORE activating the scene
                onComplete?.Invoke();

                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    public void ReloadCurrentScene()
    {
        SceneLoaderBridge.TargetScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene("LoadingScene");
    }
}