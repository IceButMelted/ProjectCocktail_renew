using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// Load scene by name
    /// </summary>
    public void LoadScene(string sceneName)
    {
        SceneLoaderBridge.TargetScene = sceneName;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Scene name is empty!");
            return;
        }

        SceneManager.LoadScene("LoadingScene");
    }

    public void ReloadCurrentScene()
    {
        SceneLoaderBridge.TargetScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene("LoadingScene");
    }
}