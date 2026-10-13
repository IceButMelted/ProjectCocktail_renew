using System.Collections;
using UnityEngine;

// The bottle pose comes from one AnimationClip whose StartStream / StopStream
// events define how long the stream flows. Code anchors this object to the
// glass and owns the stream and the liquid timing.
public class PourController : MonoBehaviour
{
    [Header("Assigns")]
    [Tooltip("Holds the Animator and BottleEvents. Hidden while not pouring.")]
    public Transform bottle;
    public Animator bottleAnimator;
    public Transform bottleMouth;
    [Tooltip("World-space LineRenderer, sorted between Ice and Liquid.")]
    public LineRenderer stream;

    [Header("Animation")]
    [Tooltip("Animator state name. Must match the clip name so its events can be read.")]
    public string pourState = "BottlePour";

    [Header("Placement")]
    [Tooltip("Before each pour this object moves to the glass InnerTop + offset (in the glass's local axes) " +
             "and takes the glass rotation. The clip's Bottle keys are relative to this object, " +
             "so one clip fits glasses of any height and facing.")]
    public Vector3 glassOffset = new Vector3(4f, -5.52f, 0f);

    [Header("Stream")]
    [Tooltip("Stream fall speed along its curve (units/s).")]
    public float fallSpeed = 8f;
    [Tooltip("How far the stream shoots out of the mouth before bending down, as a fraction of its length.")]
    [Range(0f, 1f)] public float streamReach = 0.35f;
    public float streamWidth = 0.08f;
    [Range(4, 64)] public int streamPoints = 16;

    [Header("Bottle Fade")]
    [Tooltip("Fade in over the start of the clip.")]
    public float fadeInDuration = 0.3f;
    [Tooltip("Fade out over the end of the clip.")]
    public float fadeOutDuration = 0.3f;

    [Header("Ripples")]
    public float impulseInterval = 0.12f;
    [Tooltip("Up-down ripple from the stream landing. 0 = off.")]
    public float impulseStrength = 1.5f;
    [Tooltip("Left-right rocking kick per impulse. 0 = off.")]
    public float sloshStrength = 0.15f;

    public bool IsPouring { get; private set; }

    CocktailGlass glass;
    SpriteRenderer[] bottleRenderers;
    Color[] bottleColors;
    Coroutine streamRoutine;
    Coroutine tailRoutine;
    float streamWindow;

    void Awake()
    {
        stream.enabled = false;
        stream.useWorldSpace = true;
        stream.positionCount = streamPoints;
        bottleRenderers = bottle.GetComponentsInChildren<SpriteRenderer>(true);
        bottleColors = new Color[bottleRenderers.Length];
        for (int i = 0; i < bottleRenderers.Length; i++) bottleColors[i] = bottleRenderers[i].color;
        SetBottleAlpha(0f);
    }

    // Authoring helper: arrange this object next to a glass in the scene, then run this
    // to store that arrangement as the offset.
    [ContextMenu("Capture Glass Offset From Nearest Glass")]
    void CaptureGlassOffset()
    {
        CocktailGlass nearest = null;
        float best = float.MaxValue;
        foreach (CocktailGlass g in FindObjectsByType<CocktailGlass>(FindObjectsSortMode.None))
        {
            float d = (g.innerTop.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; nearest = g; }
        }
        if (nearest == null) { Debug.LogWarning("No CocktailGlass in the scene.", this); return; }

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Capture Glass Offset");
#endif
        glassOffset = Quaternion.Inverse(nearest.transform.rotation) * (transform.position - nearest.innerTop.position);
        Debug.Log($"Glass offset set to {glassOffset} from '{nearest.name}'.", this);
    }

    public void Pour(CocktailGlass target, CocktailMenu menu)
    {
        if (!IsPouring) StartCoroutine(PourRoutine(target, menu));
    }

    IEnumerator PourRoutine(CocktailGlass target, CocktailMenu menu)
    {
        IsPouring = true;
        glass = target;
        glass.SetMenu(menu);
        Color c = new Color(menu.bottom.r, menu.bottom.g, menu.bottom.b, 1f);
        stream.startColor = c;
        stream.endColor = c;
        stream.startWidth = streamWidth;
        stream.endWidth = streamWidth * 0.6f;
        streamWindow = ReadStreamWindow();
        Quaternion glassRotation = glass.transform.rotation;
        transform.SetPositionAndRotation(glass.innerTop.position + glassRotation * glassOffset, glassRotation);

        // Clip events call OnStartStream / OnStopStream while it plays
        bottleAnimator.Play(pourState, 0, 0f);
        bottleAnimator.Update(0f);
        while (true)
        {
            AnimatorStateInfo state = bottleAnimator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(pourState) || state.normalizedTime >= 1f) break;

            // Fade in over the start of the clip, out over its end
            float t = state.normalizedTime * state.length;
            float fadeIn = fadeInDuration > 0f ? t / fadeInDuration : 1f;
            float fadeOut = fadeOutDuration > 0f ? (state.length - t) / fadeOutDuration : 1f;
            SetBottleAlpha(Mathf.Clamp01(Mathf.Min(fadeIn, fadeOut)));
            yield return null;
        }

        OnStopStream();
        while (tailRoutine != null) yield return null;

        SetBottleAlpha(0f);
        glass = null;
        IsPouring = false;
    }

    // Abandons the pour where it is. Call before destroying or resetting the glass mid-pour.
    public void Cancel()
    {
        StopAllCoroutines();
        streamRoutine = tailRoutine = null;
        stream.enabled = false;
        SetBottleAlpha(0f);
        glass = null; // the clip keeps playing hidden; its StartStream event is ignored without a glass
        IsPouring = false;
    }

    public void OnStartStream()
    {
        if (glass != null && streamRoutine == null) streamRoutine = StartCoroutine(StreamRoutine());
    }

    public void OnStopStream()
    {
        if (streamRoutine == null) return;
        StopCoroutine(streamRoutine);
        streamRoutine = null;
        glass.Level01 = 1f;
        tailRoutine = StartCoroutine(TailRoutine());
    }

    IEnumerator StreamRoutine()
    {
        stream.enabled = true;

        // Stream falls along its curve until it reaches the surface
        float fallTime = CurveLength() / fallSpeed;
        for (float e = 0f; e < fallTime; e += Time.deltaTime)
        {
            DrawStream(0f, e / fallTime);
            yield return null;
        }

        // Liquid rises for the rest of the StartStream -> StopStream window
        float riseTime = streamWindow - fallTime;
        if (riseTime <= 0f)
        {
            Debug.LogWarning($"{pourState}: StopStream is too close to StartStream for the stream to land ({fallTime:F2}s fall). Move StopStream later.", this);
            riseTime = 0.01f;
        }

        float startLevel = glass.Level01;
        float nextImpulse = 0f;
        for (float e = 0f; e < riseTime; e += Time.deltaTime)
        {
            glass.Level01 = Mathf.Lerp(startLevel, 1f, e / riseTime);
            DrawStream(0f, 1f);
            if (e >= nextImpulse)
            {
                KickSurface();
                nextImpulse += impulseInterval;
            }
            yield return null;
        }

        glass.Level01 = 1f;
        while (true)
        {
            DrawStream(0f, 1f);
            yield return null;
        }
    }

    // The last of the stream leaves the mouth and falls into the glass.
    // The curve is frozen at the stop moment so it does not swing with the bottle.
    IEnumerator TailRoutine()
    {
        CurvePoints(out Vector3 p0, out Vector3 p1, out _);
        float fallTime = CurveLength() / fallSpeed;
        float nextImpulse = 0f;
        for (float e = 0f; e < fallTime; e += Time.deltaTime)
        {
            CurvePoints(out _, out _, out Vector3 p2);
            DrawCurve(p0, p1, p2, e / fallTime, 1f);
            if (e >= nextImpulse)
            {
                KickSurface();
                nextImpulse += impulseInterval;
            }
            yield return null;
        }

        stream.enabled = false;
        tailRoutine = null;
    }

    void SetBottleAlpha(float alpha)
    {
        for (int i = 0; i < bottleRenderers.Length; i++)
        {
            Color c = bottleColors[i];
            c.a *= alpha;
            bottleRenderers[i].color = c;
            bottleRenderers[i].enabled = alpha > 0f;
        }
    }

    // Stream ripples plus a left-right kick toward where the stream is flowing
    void KickSurface()
    {
        CurvePoints(out _, out Vector3 p1, out Vector3 p2);
        glass.Impulse(impulseStrength);
        glass.Slosh(sloshStrength, Vector3.Dot(p2 - p1, glass.transform.right));
    }

    float ReadStreamWindow()
    {
        float start = -1f, stop = -1f;
        foreach (AnimationClip clip in bottleAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name != pourState) continue;
            foreach (AnimationEvent ev in clip.events)
            {
                if (ev.functionName == nameof(BottleEvents.StartStream)) start = ev.time;
                else if (ev.functionName == nameof(BottleEvents.StopStream)) stop = ev.time;
            }
        }

        if (start < 0f || stop <= start)
        {
            Debug.LogError($"Clip '{pourState}' needs a StartStream event followed by a StopStream event.", this);
            return 0f;
        }
        return stop - start;
    }

    // Quadratic Bezier: out of the mouth along the bottle's tilt, down to the glass centre
    void CurvePoints(out Vector3 p0, out Vector3 p1, out Vector3 p2)
    {
        p0 = bottleMouth.position;
        p2 = glass.SurfaceCenter;
        p1 = p0 + bottleMouth.up * (Vector3.Distance(p0, p2) * streamReach);
    }

    static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    float CurveLength()
    {
        CurvePoints(out Vector3 p0, out Vector3 p1, out Vector3 p2);
        float length = 0f;
        Vector3 prev = p0;
        for (int i = 1; i < streamPoints; i++)
        {
            Vector3 p = Bezier(p0, p1, p2, i / (streamPoints - 1f));
            length += Vector3.Distance(prev, p);
            prev = p;
        }
        return length;
    }

    void DrawStream(float from, float to)
    {
        CurvePoints(out Vector3 p0, out Vector3 p1, out Vector3 p2);
        DrawCurve(p0, p1, p2, from, to);
    }

    void DrawCurve(Vector3 p0, Vector3 p1, Vector3 p2, float from, float to)
    {
        for (int i = 0; i < streamPoints; i++)
            stream.SetPosition(i, Bezier(p0, p1, p2, Mathf.Lerp(from, to, i / (streamPoints - 1f))));
    }
}
