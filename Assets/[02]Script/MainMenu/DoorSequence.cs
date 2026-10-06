using UnityEngine;

/// <summary>
/// Attach to the DOOR GameObject. Animation Events only reach components on the
/// same object as the Animator playing them, so this stays door-side.
/// </summary>
public class DoorSequence : MonoBehaviour
{
    [SerializeField] private Animator cameraAnimator; // Animator on Cam1

    // Animation Event: call at the frame the door is fully open.
    public void OnDoorOpened()
    {
        cameraAnimator.SetTrigger("PlayAnim");
    }
}