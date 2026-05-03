using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ParallaxLayer
{
    public RectTransform target;
    [Range(0f, 100f)]
    public float strength = 20f;

    [HideInInspector] public Vector2 startPos;
}

public class UIParallaxMouse : MonoBehaviour
{
    [Header("Parallax Layers")]
    public List<ParallaxLayer> layers = new List<ParallaxLayer>();

    [Header("Global Settings")]
    public float smoothSpeed = 5f;
    public bool invertX = false;
    public bool invertY = false;


    private Vector2 _screenCenter;

    void Start()
    {
        _screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // Store original positions
        foreach (var layer in layers)
        {
            if (layer.target != null)
                layer.startPos = layer.target.anchoredPosition;
        }
    }

    void Update()
    {
        Vector2 mousePos = Input.mousePosition;

        // Normalize mouse position to [-1, 1]
        Vector2 normalized = (mousePos - _screenCenter) / _screenCenter;

        if (invertX) normalized.x *= -1;
        if (invertY) normalized.y *= -1;

        foreach (var layer in layers)
        {
            if (layer.target == null) continue;

            Vector2 offset = new Vector2(
                normalized.x * layer.strength,
                normalized.y * layer.strength
            );

            Vector2 targetPos = layer.startPos + offset;

            // Smooth movement
            layer.target.anchoredPosition = Vector2.Lerp(
                layer.target.anchoredPosition,
                targetPos,
                Time.deltaTime * smoothSpeed
            );
        }
    }
}