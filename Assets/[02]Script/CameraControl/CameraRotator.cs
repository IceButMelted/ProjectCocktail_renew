using UnityEngine;
using System.Collections; 
using System; 

public class CameraRotator : MonoBehaviour
{
    // Public method to start the rotation sequence
    public void StartCameraRotation(Vector3 targetEulerAngles, float duration, Action onComplete = null)
    {
        StartCoroutine(RotateCamera(targetEulerAngles, duration, onComplete));
    }

    private IEnumerator RotateCamera(Vector3 targetEuler, float duration, Action onComplete)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(targetEuler);
        float timeElapsed = 0;

        while (timeElapsed < duration)
        {
            // Use Quaternion.Slerp for smooth rotation between start and end angles
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        // Ensure the final rotation is exactly the target rotation
        transform.rotation = targetRotation;

        // Call the optional callback function when finished
        onComplete?.Invoke();
    }
}
