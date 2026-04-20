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

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadAsync(sceneName));
    }

    private IEnumerator LoadAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            // Unity loads from 0 > 0.9
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // Smooth the bar
            _displayProgress = Mathf.Lerp(
                _displayProgress,
                targetProgress,
                Time.deltaTime * smoothSpeed
            );

            loadingBar.value = _displayProgress;

            if (loadingText != null)
                loadingText.text = $"{Mathf.RoundToInt(_displayProgress * 100f)}%";

            // When fully loaded
            if (_displayProgress >= 0.99f)
            {
                yield return new WaitForSeconds(0.3f); // small polish delay
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}