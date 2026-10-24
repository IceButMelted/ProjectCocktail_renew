using System;
using UnityEngine;

/// <summary>
/// Flat silhouette NPC. Root walks along a lane and yaws toward the camera;
/// the child visual bobs up and down. Pooled by NpcCrowdSpawner.
/// </summary>
public class NpcWalker : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] private Transform visual;
    [SerializeField] private Renderer visualRenderer;
    [SerializeField] private float bobHeight = 0.05f;

    private Vector3 from;
    private Vector3 to;
    private float speed;
    private float bobFrequency;
    private float bobPhase;
    private float progress;
    private float pathLength;
    private float visualBaseY;
    private float alpha = 1f;
    private float fadeSpeed;
    private Color baseColor = Color.black;
    private MaterialPropertyBlock block;
    private Transform cam;
    private Action<NpcWalker> onFinished;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        visualBaseY = visual.localPosition.y;
        if (visualRenderer.sharedMaterial.HasProperty(BaseColorId))
            baseColor = visualRenderer.sharedMaterial.GetColor(BaseColorId);
    }

    public void Begin(Vector3 from, Vector3 to, float speed, float bobFrequency,
        float startProgress, Transform cam, Action<NpcWalker> onFinished)
    {
        this.from = from;
        this.to = to;
        this.speed = speed;
        this.bobFrequency = bobFrequency;
        this.cam = cam;
        this.onFinished = onFinished;
        pathLength = Mathf.Max(Vector3.Distance(from, to), 0.01f);
        progress = startProgress;
        bobPhase = UnityEngine.Random.value * Mathf.PI * 2f;
        fadeSpeed = 0f;
        gameObject.SetActive(true);
        SetAlpha(1f);
        Tick(0f);
    }

    public void FadeOut(float duration)
    {
        fadeSpeed = duration > 0f ? 1f / duration : float.MaxValue;
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    private void Tick(float dt)
    {
        progress += speed * dt / pathLength;
        transform.position = Vector3.Lerp(from, to, progress);

        float t = Time.time * bobFrequency * Mathf.PI * 2f + bobPhase;
        Vector3 local = visual.localPosition;
        local.y = visualBaseY + Mathf.Abs(Mathf.Sin(t)) * bobHeight;
        visual.localPosition = local;

        if (cam != null)
        {
            Vector3 away = transform.position - cam.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(away);
        }

        if (fadeSpeed > 0f)
            SetAlpha(alpha - fadeSpeed * dt);

        if (progress >= 1f || alpha <= 0f)
            Finish();
    }

    private void SetAlpha(float value)
    {
        alpha = Mathf.Clamp01(value);
        Color c = baseColor;
        c.a *= alpha;
        block.SetColor(BaseColorId, c);
        visualRenderer.SetPropertyBlock(block);
    }

    private void Finish()
    {
        gameObject.SetActive(false);
        onFinished?.Invoke(this);
    }
}
