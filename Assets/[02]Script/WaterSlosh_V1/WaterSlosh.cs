using UnityEngine;

public class WaterSlosh : MonoBehaviour
{
    [Header("Assigns")]
    public GameObject waterSprite;
    public GameObject iceSprite;

    [Header("Water Settings")]
    public float waterLevel = 0.5f;
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
        matInstance.SetFloat("_WaterLevel", waterLevel);

        iceSprite.SetActive(withIce);
    }
}