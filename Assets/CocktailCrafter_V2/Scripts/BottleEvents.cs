using UnityEngine;

// Lives next to the bottle Animator so the pour clip's events can reach PourController.
public class BottleEvents : MonoBehaviour
{
    public PourController pour;

    public void StartStream() => pour.OnStartStream();

    public void StopStream() => pour.OnStopStream();
}
