using UnityEngine;
using Yarn.Unity;

public class PH_Thx : MonoBehaviour
{
    [YarnCommand("DisplayThx")]
    public static void DisplayThx()
    {
        var instance = FindAnyObjectByType<PH_Thx>(FindObjectsInactive.Include);

        if (instance != null)
            instance.gameObject.SetActive(true);
        else
            Debug.LogWarning("[PH_Thx] No instance found in scene.");
    }
}