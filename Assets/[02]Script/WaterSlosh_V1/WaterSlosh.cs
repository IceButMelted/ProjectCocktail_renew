using UnityEngine;
using System.Collections;

public class WaterSlosh : MonoBehaviour
{
    [Header("Assigns")]
    public GameObject waterSprite;
    public GameObject iceSprite;

    [Header("Water Settings")]
    [Range(0f,.95f)]public float waterLevel = 0.5f;
    public bool withIce = false;
    

    [Header("Material Settings")]
    public Color waterColorTop = Color.magenta;
    public Color waterColorBottom = Color.green;
    public float speed = 10f;
    public float frequency = 15f;

    [Header("Slosh Settings")]
    public float sloshAmount = 0.05f;
    public float sloshDecay = 2f;
    public float velocityMultiplier = 0.1f;

    private Rigidbody2D rb;
    private float currentAmplitude;
    private Vector3 lastPosition;
    private Material matInstance;

    private Coroutine _fillCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lastPosition = transform.position;

        matInstance = waterSprite.GetComponent<Renderer>().material;

        matInstance.SetFloat("_WaterLevel", waterLevel);
        matInstance.SetFloat("_Speed", speed);
        matInstance.SetFloat("_Frequency", frequency);
        matInstance.SetColor("_WaterColorTop", waterColorTop);
        matInstance.SetColor("_WaterColorBottom", waterColorBottom);

        waterSprite.SetActive(true);
        iceSprite.SetActive(withIce);
    }

    void Update()
    {
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;

        float impact = Mathf.Abs(delta.x) + Mathf.Abs(delta.z);
        impact *= velocityMultiplier * 100f;
        currentAmplitude = Mathf.Max(currentAmplitude, impact * sloshAmount);
        currentAmplitude = Mathf.Lerp(currentAmplitude, 0f, Time.deltaTime * sloshDecay);

        matInstance.SetFloat("_Amplitude", currentAmplitude);

    }

    private void OnValidate()
    {
        matInstance = waterSprite.GetComponent<Renderer>().material;

        if (matInstance != null)
        {
            matInstance.SetFloat("_WaterLevel", waterLevel);
            matInstance.SetFloat("_Speed", speed);
            matInstance.SetFloat("_Frequency", frequency);
            matInstance.SetColor("_WaterColorTop", waterColorTop);
            matInstance.SetColor("_WaterColorBottom", waterColorBottom);
            iceSprite.SetActive(withIce);
        }
    }

    public void StartFilling()
    { 
        _fillCoroutine = StartCoroutine(FillWater());
    }

    public void UpdateColor() {
        matInstance.SetColor("_WaterColorTop", waterColorTop);
        matInstance.SetColor("_WaterColorBottom", waterColorBottom);
    }

    private IEnumerator FillWater()
    {
        while (waterLevel < 0.94f)
        {
            waterLevel += Time.deltaTime;
            waterLevel = Mathf.Clamp(waterLevel, 0f, 0.95f);
            matInstance.SetFloat("_WaterLevel", waterLevel);
            yield return null;
        }
        waterLevel = 0.94f;
        matInstance.SetFloat("_WaterLevel", waterLevel);
    }

    public void StopFilling()
    {
        if (_fillCoroutine != null)
        {
            StopCoroutine(_fillCoroutine);
            _fillCoroutine = null;
        }
    }

    public void FinishFilling() {
        waterLevel = 0.94f;
    }

    public void ResetGlass() {
        waterLevel = 0f;
        iceSprite.SetActive(false);
    }

    public void AddIce(bool active)
    {
        iceSprite.SetActive (active);
    }
}