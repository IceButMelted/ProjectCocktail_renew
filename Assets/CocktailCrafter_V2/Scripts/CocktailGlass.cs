using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CocktailGlass : MonoBehaviour
{
    [Header("Assigns")]
    public SpriteRenderer liquidRenderer;
    public SpriteMask liquidMask;
    public EdgeCollider2D innerCollider;
    public Transform iceRoot;
    public Rigidbody2D icePrefab;
    public LiquidSettings settings;

    [Header("Markers (drag in Scene view)")]
    public Transform innerBottom;
    public Transform innerTop;
    public Transform fillLine;
    public Transform iceSpawnLeft;
    public Transform iceSpawnRight;

    [Header("Ice")]
    public int iceCount = 4;
    public float iceDropInterval = 0.25f;
    [Tooltip("Random size multiplier applied on top of the ice prefab scale.")]
    public Vector2 iceScaleRange = new Vector2(0.8f, 1.2f);
    [Tooltip("Time to grow from 0 to full size. Ice holds still until fully grown, then drops.")]
    public float iceGrowDuration = 0.15f;

    [Header("Surface Spring")]
    [Range(8, 256)] public int surfacePoints = 64;
    public float stiffness = 15f;
    public float damping = 1.5f;
    [Tooltip("How fast ripples travel to neighbouring points.")]
    public float spread = 1500f;
    [Tooltip("Impulse falloff width, in surface points.")]
    public float impulseWidth = 2f;

    [Header("Surface Slosh (left-right tilt)")]
    [Tooltip("Higher = faster rocking.")]
    public float sloshStiffness = 25f;
    public float sloshDamping = 1.2f;

    [Header("Wave Limits")]
    [Tooltip("Fraction of the free space a wave may use before it gets scaled down.")]
    [Range(0.1f, 1f)] public float headroomUsage = 0.9f;
    [Tooltip("Inspector warns when InnerTop - FillLine is smaller than this.")]
    public float minHeadroom = 0.15f;

    // 0 = InnerBottom, 1 = FillLine
    public float Level01 { get; set; }
    public float LevelY => Mathf.Lerp(innerBottom.position.y, fillLine.position.y, Level01);
    public float SurfaceCenterY => LevelY + SampleHeight(0.5f);
    public Vector3 SurfaceCenter => new Vector3(innerBottom.position.x, SurfaceCenterY, innerBottom.position.z);
    public float InnerBottomY => innerBottom.position.y;
    public float Headroom => innerTop.position.y - fillLine.position.y;
    public bool IsDroppingIce { get; private set; }

    const float SubStep = 1f / 120f;

    static readonly int ColorTopId = Shader.PropertyToID("_ColorTop");
    static readonly int ColorBottomId = Shader.PropertyToID("_ColorBottom");
    static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    static readonly int InnerBottomYId = Shader.PropertyToID("_InnerBottomY");
    static readonly int InnerTopYId = Shader.PropertyToID("_InnerTopY");
    static readonly int LevelYId = Shader.PropertyToID("_LevelY");
    static readonly int SurfaceAxisId = Shader.PropertyToID("_SurfaceAxis");
    static readonly int SurfaceMinXId = Shader.PropertyToID("_SurfaceMinX");
    static readonly int SurfaceMaxXId = Shader.PropertyToID("_SurfaceMaxX");
    static readonly int SurfaceTexId = Shader.PropertyToID("_SurfaceTex");

    // Physics2D only works on the world XY plane. Ice is simulated in a hidden,
    // unrotated copy of the glass interior and mirrored into the real glass, so
    // the glass can face any direction.
    static int proxyCount;

    struct Ice
    {
        public Rigidbody2D body;  // in the proxy
        public Transform visual;  // under iceRoot
    }

    readonly List<Ice> ices = new List<Ice>();
    Transform proxyRoot;
    Transform staging;

    float[] spring, velocity, tilt, heights;
    Texture2D surfaceTex;
    Material mat;
    float innerMinX, innerMaxX; // glass-local width of the interior
    float slosh, sloshVelocity; // surface height at the right edge; left edge is the opposite
    float accumulator;
    bool springActive;
    bool texDirty = true;

    void Awake()
    {
        spring = new float[surfacePoints];
        velocity = new float[surfacePoints];
        tilt = new float[surfacePoints];
        heights = new float[surfacePoints];

        surfaceTex = new Texture2D(surfacePoints, 1, TextureFormat.RFloat, false, true)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        mat = liquidRenderer.material;
        mat.SetTexture(SurfaceTexId, surfaceTex);

        CreateIceProxy();
    }

    void OnDestroy()
    {
        Destroy(surfaceTex);
        Destroy(mat);
        if (proxyRoot) Destroy(proxyRoot.gameObject);
    }

    void CreateIceProxy()
    {
        proxyRoot = new GameObject(name + " (Ice Proxy)").transform;
        proxyRoot.position = new Vector3(2000f + 20f * proxyCount++, 2000f, 0f);
        // Unit scale: ice simulates in glass-local units whatever the glass's world size. A glass
        // scaled far down would otherwise put the ice below Physics2D's contact offset and it
        // would stack with visible gaps.

        staging = new GameObject("Staging").transform;
        staging.SetParent(proxyRoot, false);
        staging.gameObject.SetActive(false);

        // Interior collider in glass-local space
        Vector2[] points = InnerPointsLocal();
        innerMinX = float.MaxValue;
        innerMaxX = float.MinValue;
        foreach (Vector2 p in points)
        {
            innerMinX = Mathf.Min(innerMinX, p.x);
            innerMaxX = Mathf.Max(innerMaxX, p.x);
        }

        var col = new GameObject("InnerCollider").AddComponent<EdgeCollider2D>();
        col.transform.SetParent(proxyRoot, false);
        col.points = points;
        col.edgeRadius = innerCollider.edgeRadius;
        innerCollider.enabled = false;
    }

    Vector2[] InnerPointsLocal()
    {
        Vector2[] src = innerCollider.points;
        var points = new Vector2[src.Length];
        for (int i = 0; i < src.Length; i++)
            points[i] = transform.InverseTransformPoint(innerCollider.transform.TransformPoint(src[i] + innerCollider.offset));
        return points;
    }

    public void SetMenu(CocktailMenu menu)
    {
        mat.SetColor(ColorTopId, menu.top);
        mat.SetColor(ColorBottomId, menu.bottom);
    }

    // Kicks the spring at the middle of the glass (pour stream landing).
    public void Impulse(float strength)
    {
        float center = (surfacePoints - 1) * 0.5f;
        for (int i = 0; i < surfacePoints; i++)
        {
            float d = (i - center) / impulseWidth;
            velocity[i] -= strength * Mathf.Exp(-d * d);
        }
        springActive = true;
    }

    // Rocks the surface left-right. Kicks follow the current swing so repeated
    // kicks keep it rocking; from rest the side the stream flows toward rises.
    // direction > 0 means toward the glass's right.
    public void Slosh(float strength, float direction)
    {
        float push = Mathf.Abs(sloshVelocity) > 0.01f ? Mathf.Sign(sloshVelocity) : Mathf.Sign(direction);
        sloshVelocity += strength * push;
        springActive = true;
    }

    public void DropIce()
    {
        if (!IsDroppingIce) StartCoroutine(DropIceRoutine());
    }

    public void ResetGlass()
    {
        StopAllCoroutines();
        IsDroppingIce = false;
        foreach (Ice ice in ices)
        {
            Destroy(ice.body.gameObject);
            Destroy(ice.visual.gameObject);
        }
        ices.Clear();
        Level01 = 0f;
        System.Array.Clear(spring, 0, surfacePoints);
        System.Array.Clear(velocity, 0, surfacePoints);
        System.Array.Clear(tilt, 0, surfacePoints);
        slosh = sloshVelocity = 0f;
        springActive = true;
    }

    IEnumerator DropIceRoutine()
    {
        IsDroppingIce = true;
        for (int i = 0; i < iceCount; i++)
        {
            Vector3 local = transform.InverseTransformPoint(Vector3.Lerp(iceSpawnLeft.position, iceSpawnRight.position, Random.value));
            local.z = 0f;
            Quaternion angle = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Rigidbody2D body = Instantiate(icePrefab, proxyRoot.TransformPoint(local), angle, proxyRoot);
            foreach (Renderer r in body.GetComponentsInChildren<Renderer>()) r.enabled = false;

            // Visual copy without physics, built while inactive so it never joins the real Physics2D world
            GameObject visual = Instantiate(icePrefab.gameObject, staging);
            foreach (Collider2D c in visual.GetComponentsInChildren<Collider2D>()) DestroyImmediate(c);
            foreach (Rigidbody2D rb in visual.GetComponentsInChildren<Rigidbody2D>()) DestroyImmediate(rb);
            visual.transform.SetParent(iceRoot, false);

            var ice = new Ice { body = body, visual = visual.transform };
            ices.Add(ice);
            SyncIce(ice);
            StartCoroutine(GrowIce(body, body.transform.localScale * Random.Range(iceScaleRange.x, iceScaleRange.y)));
            yield return new WaitForSeconds(iceDropInterval);
        }
        IsDroppingIce = false;
    }

    IEnumerator GrowIce(Rigidbody2D body, Vector3 fullScale)
    {
        body.simulated = false;
        for (float e = 0f; e < iceGrowDuration; e += Time.deltaTime)
        {
            // Ease-out-back: slight overshoot so it pops in
            float t = e / iceGrowDuration - 1f;
            body.transform.localScale = fullScale * (1f + 2.70158f * t * t * t + 1.70158f * t * t);
            yield return null;
        }
        body.transform.localScale = fullScale;
        body.simulated = true;
    }

    void SyncIce(Ice ice)
    {
        Transform b = ice.body.transform;
        Vector3 local = proxyRoot.InverseTransformPoint(b.position);
        local.z = 0f;
        ice.visual.SetPositionAndRotation(transform.TransformPoint(local), transform.rotation * Quaternion.Euler(0f, 0f, b.eulerAngles.z));
        ice.visual.localScale = b.localScale;
    }

    // Read the glass after Animator/Timeline/tweens have moved it this frame.
    void LateUpdate()
    {
        foreach (Ice ice in ices) SyncIce(ice);

        if (springActive) StepSpring();

        float levelY = LevelY;
        float up = innerTop.position.y - levelY;
        float down = levelY - innerBottom.position.y;
        float maxH = 0f, minH = 0f;
        for (int i = 0; i < surfacePoints; i++)
        {
            float h = spring[i] + tilt[i];
            heights[i] = h;
            maxH = Mathf.Max(maxH, h);
            minH = Mathf.Min(minH, h);
        }

        // Soft scale keeps the wave shape; hard clamp is only a safety net.
        float scale = 1f;
        if (maxH > up * headroomUsage) scale = Mathf.Min(scale, up * headroomUsage / maxH);
        if (-minH > down * headroomUsage) scale = Mathf.Min(scale, down * headroomUsage / -minH);
        for (int i = 0; i < surfacePoints; i++) heights[i] = Mathf.Clamp(heights[i] * scale, -down, up);

        if (texDirty)
        {
            surfaceTex.SetPixelData(heights, 0);
            surfaceTex.Apply(false);
            texDirty = springActive;
        }

        // Shader finds a pixel's place on the surface by projecting it onto the glass's right axis
        Vector3 axis = transform.right;
        mat.SetVector(SurfaceAxisId, axis);
        mat.SetFloat(SurfaceMinXId, Vector3.Dot(transform.TransformPoint(new Vector3(innerMinX, 0f, 0f)), axis));
        mat.SetFloat(SurfaceMaxXId, Vector3.Dot(transform.TransformPoint(new Vector3(innerMaxX, 0f, 0f)), axis));
        mat.SetFloat(OpacityId, settings.liquidOpacity);
        mat.SetFloat(InnerBottomYId, innerBottom.position.y);
        mat.SetFloat(InnerTopYId, innerTop.position.y);
        mat.SetFloat(LevelYId, levelY);
    }

    void StepSpring()
    {
        accumulator = Mathf.Min(accumulator + Time.deltaTime, 0.1f);
        while (accumulator >= SubStep)
        {
            accumulator -= SubStep;
            for (int i = 0; i < surfacePoints; i++)
            {
                float left = spring[Mathf.Max(i - 1, 0)];
                float right = spring[Mathf.Min(i + 1, surfacePoints - 1)];
                float accel = -stiffness * spring[i] - damping * velocity[i] + spread * (left + right - 2f * spring[i]);
                velocity[i] += accel * SubStep;
            }
            for (int i = 0; i < surfacePoints; i++) spring[i] += velocity[i] * SubStep;

            sloshVelocity += (-sloshStiffness * slosh - sloshDamping * sloshVelocity) * SubStep;
            slosh += sloshVelocity * SubStep;
        }

        for (int i = 0; i < surfacePoints; i++)
            tilt[i] = slosh * (2f * i / (surfacePoints - 1f) - 1f);

        bool settled = Mathf.Abs(slosh) < 1e-4f && Mathf.Abs(sloshVelocity) < 1e-4f;
        for (int i = 0; i < surfacePoints && settled; i++)
            settled = Mathf.Abs(spring[i]) < 1e-4f && Mathf.Abs(velocity[i]) < 1e-4f;
        if (settled)
        {
            System.Array.Clear(spring, 0, surfacePoints);
            System.Array.Clear(velocity, 0, surfacePoints);
            System.Array.Clear(tilt, 0, surfacePoints);
            slosh = sloshVelocity = 0f;
            springActive = false;
        }
        texDirty = true;
    }

    float SampleHeight(float u)
    {
        float x = u * (surfacePoints - 1);
        int i = Mathf.FloorToInt(x);
        int j = Mathf.Min(i + 1, surfacePoints - 1);
        return Mathf.Lerp(heights[i], heights[j], x - i);
    }

    // Drawn in glass-local space so the gizmo follows the glass rotation
    void OnDrawGizmos()
    {
        if (!innerBottom || !innerTop || !fillLine) return;

        float minX = -1f, maxX = 1f;
        if (innerCollider && innerCollider.points.Length > 0)
        {
            minX = float.MaxValue;
            maxX = float.MinValue;
            foreach (Vector2 p in InnerPointsLocal())
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
            }
        }

        Gizmos.matrix = transform.localToWorldMatrix;
        float bottomY = transform.InverseTransformPoint(innerBottom.position).y;
        float topY = transform.InverseTransformPoint(innerTop.position).y;
        float fillY = transform.InverseTransformPoint(fillLine.position).y;

        Gizmos.color = Headroom < minHeadroom ? new Color(1f, 0f, 0f, 0.35f) : new Color(0f, 1f, 0.4f, 0.2f);
        Gizmos.DrawCube(new Vector3((minX + maxX) * 0.5f, (topY + fillY) * 0.5f, 0f), new Vector3(maxX - minX, Mathf.Abs(topY - fillY), 0.01f));

        DrawLine(minX, maxX, bottomY, Color.yellow);
        DrawLine(minX, maxX, topY, Color.yellow);
        DrawLine(minX, maxX, fillY, Color.cyan);

        if (iceSpawnLeft && iceSpawnRight)
        {
            Vector3 left = transform.InverseTransformPoint(iceSpawnLeft.position);
            Vector3 right = transform.InverseTransformPoint(iceSpawnRight.position);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawWireSphere(left, 0.04f);
            Gizmos.DrawWireSphere(right, 0.04f);
        }
        Gizmos.matrix = Matrix4x4.identity;
    }

    static void DrawLine(float minX, float maxX, float y, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(new Vector3(minX, y, 0f), new Vector3(maxX, y, 0f));
    }
}
