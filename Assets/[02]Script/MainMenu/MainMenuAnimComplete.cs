using UnityEngine;

/// <summary>
/// Attach to the CAMERA GameObject (Cam1) — same reason as DoorSequence:
/// Animation Events only reach components on the object the clip is playing on.
/// </summary>
public class CameraZoomComplete : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private string nextSceneName;

    // Animation Event: call at the frame in Cam1's clip where the door should start opening.
    public void PlayDoorOpen()
    {
        doorAnimator.SetTrigger("Open");
    }

    // Animation Event: call at the last frame of the camera's zoom-in clip.
    public void OnAnimComplete()
    {
        sceneLoader.LoadScene(nextSceneName);
    }
}