using UnityEngine;

public class DebugPositionOnscren : MonoBehaviour
{
    public RectTransform uiElement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void Update()
    {
        // 1. Get the world position of the GameObject
        Vector3 worldPos = transform.position;

        // 2. Convert it to screen space using the Main Camera
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);


        // Result: screenPos.x and screenPos.y are the pixel coordinates.
        // screenPos.z is the distance from the camera to the object.
        Debug.Log("Screen Position: " + screenPos);
    }
}
