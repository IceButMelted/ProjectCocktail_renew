using UnityEngine;
using System.Collections;
using Yarn.Unity;
using static E_Cocktail;
using AYellowpaper.SerializedCollections;

public class WaterSlosh : MonoBehaviour
{
    [Header("Assigns")]
    public GameObject waterSprite;
    public GameObject iceSprite;
    public GameObject MaskingSprite;

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

    

    //private Variable
    private Rigidbody2D rb;
    private float currentAmplitude;
    private Vector3 lastPosition;
    private Material matInstance;

    private Coroutine _fillCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lastPosition = transform.position;
        // GetMat() will assign matInstance here too
        GetMat().SetFloat("_WaterLevel", waterLevel);
        GetMat().SetFloat("_Speed", speed);
        GetMat().SetFloat("_Frequency", frequency);
        GetMat().SetColor("_WaterColorTop", waterColorTop);
        GetMat().SetColor("_WaterColorBottom", waterColorBottom);
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

        GetMat()?.SetFloat("_Amplitude", currentAmplitude);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (waterSprite == null) return;
        var mat = GetMat();
        if (mat != null)
        {
            UpdateMatVariable();
            if (iceSprite != null) iceSprite.SetActive(withIce);
        }
    }
#endif

    public void StartFilling()
    { 
        _fillCoroutine = StartCoroutine(FillWater());
    }

    public void UpdateColor()
    {
        var mat = GetMat();
        if (mat == null) return; // safe guard
        mat.SetColor("_WaterColorTop", waterColorTop);
        mat.SetColor("_WaterColorBottom", waterColorBottom);
    }

    private Material GetMat()
    {
        if (waterSprite == null) return null;

        var renderer = waterSprite.GetComponent<Renderer>();
        if (renderer == null) return null;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Don't instance a material in edit mode — edit the shared asset directly.
            return renderer.sharedMaterial;
        }
#endif

        if (matInstance == null)
            matInstance = renderer.material;

        return matInstance;
    }

    private IEnumerator FillWater()
    {
        while (waterLevel < 0.94f)
        {
            waterLevel += Time.deltaTime;
            waterLevel = Mathf.Clamp(waterLevel, 0f, 0.95f);
            UpdateMatVariable();
            yield return null;
        }
        waterLevel = 0.94f;
        UpdateMatVariable();
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
        UpdateMatVariable();
        iceSprite.SetActive(false);
    }

    public void AddIce(bool active)
    {
        iceSprite.SetActive (active);
    }

    public void UpdateMatVariable()
    {
        var mat = GetMat();
        if (mat == null) return;
        mat.SetFloat("_WaterLevel", waterLevel);
        mat.SetFloat("_Speed", speed);
        mat.SetFloat("_Frequency", frequency);
        mat.SetColor("_WaterColorTop", waterColorTop);
        mat.SetColor("_WaterColorBottom", waterColorBottom);
    }

    public void UpdateVisual(Sprite IceSprite, Sprite GlassSprite, Sprite WaterSprite) { 
        waterSprite.GetComponent<SpriteRenderer>().sprite = WaterSprite;
        this.GetComponent<SpriteRenderer>().sprite = GlassSprite;
        iceSprite.GetComponent<SpriteRenderer>().sprite = IceSprite;
        MaskingSprite.GetComponent<SpriteMask>().sprite = WaterSprite;
    }
    
}