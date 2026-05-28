// ============================================================
//  PanelSlider.cs — Static utility for sliding UI panels.
//
//  SOLID — S (Single Responsibility):
//    Owns exactly one concern: sliding a RectTransform toward a
//    pre-computed target and reporting when it arrives.
//    No game state, no MonoBehaviour lifecycle.
//
//  SOLID — O (Open / Closed):
//    New finish conditions → one new case in ComputeTarget.
//    New easing modes     → one new case in EasingMath.Evaluate.
//    Existing callers never change.
//
//  Session model:
//    Every panel owns a SlideSession value.  BeginSession() fires
//    geometry ONCE (GetWorldCorners / InverseTransformPoint) and
//    stores start, end, direction, and easing.  Tick() runs every
//    frame with zero geometry overhead.
//
//  Usage — Update loop:
//    bool done = PanelSlider.Slide(ref _session, panel,
//                    Direction.Up, SlideFinishCondition.FullyIn);
//
//  Usage — coroutine:
//    yield return StartCoroutine(
//        PanelSlider.SlideRoutine(panel, Direction.Up,
//                    SlideFinishCondition.FullyIn,
//                    EasingConfig.Bounce(0.5f)));
// ============================================================

using System.Collections;
using UnityEngine;
using static E_Cocktail;

/// <summary>
/// Mutable session state — one per animated panel.
/// Callers declare a field of this type; PanelSlider reads and
/// writes it via ref parameters.
/// </summary>
public struct SlideSession
{
    internal bool Active;
    internal Direction Dir;
    internal Vector2 Start;
    internal Vector2 End;
    internal EasingConfig Easing;   // null → linear velocity
    internal float Elapsed;         // only advanced by TickEased
}

/// <summary>
/// Pure static utility — no MonoBehaviour, no instance needed.
/// Declare a <see cref="SlideSession"/> per panel and pass it by ref.
/// </summary>
public static class PanelSlider
{
    // ═══════════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Call every frame. Starts a new session on the first call,
    /// then ticks it until the target is reached.
    /// Returns true on the frame the slide completes.
    /// </summary>
    public static bool Slide(ref SlideSession session, RectTransform panel,
                             Direction dir, SlideFinishCondition condition,
                             float speed, EasingConfig easing = null)
    {
        if (!session.Active)
            BeginSession(ref session, panel, dir, ComputeTarget(panel, dir, condition), easing);

        return Tick(ref session, panel, speed);
    }

    /// <summary>
    /// Call every frame with an explicit target position.
    /// Restarts the session if the target changes mid-flight.
    /// Returns true on the frame the slide completes.
    /// </summary>
    public static bool Slide(ref SlideSession session, RectTransform panel,
                             Direction dir, Vector2 targetPosition,
                             float speed, EasingConfig easing = null)
    {
        if (!session.Active || session.End != targetPosition)
            BeginSession(ref session, panel, dir, targetPosition, easing);

        return Tick(ref session, panel, speed);
    }

    /// <summary>
    /// Coroutine version — begins a fresh session and yields until complete.
    /// </summary>
    public static IEnumerator SlideRoutine(SlideSession session, RectTransform panel,
                                           Direction dir, SlideFinishCondition condition,
                                           float speed, EasingConfig easing = null)
    {
        BeginSession(ref session, panel, dir, ComputeTarget(panel, dir, condition), easing);
        while (!Tick(ref session, panel, speed)) yield return null;
    }

    /// <summary>Coroutine version with an explicit target position.</summary>
    public static IEnumerator SlideRoutine(SlideSession session, RectTransform panel,
                                           Direction dir, Vector2 targetPosition,
                                           float speed, EasingConfig easing = null)
    {
        BeginSession(ref session, panel, dir, targetPosition, easing);
        while (!Tick(ref session, panel, speed)) yield return null;
    }

    /// <summary>
    /// Instantly snaps the panel to the position defined by <paramref name="condition"/>
    /// without requiring a <see cref="Direction"/>. The condition name already encodes
    /// which edge moves where (e.g. <c>LeftEdgeToCenter</c>, <c>TopEdgeToTopBound</c>).
    /// <para>
    /// <b>Not supported:</b> <see cref="SlideFinishCondition.FullyIn"/> and
    /// <see cref="SlideFinishCondition.FullyOut"/> are inherently directional —
    /// use <see cref="SnapTo(RectTransform, Direction, SlideFinishCondition)"/> for those.
    /// </para>
    /// </summary>
    public static void SnapTo(RectTransform panel, SlideFinishCondition condition)
    {
        if (condition is SlideFinishCondition.FullyIn or SlideFinishCondition.FullyOut)
        {
            Debug.LogError($"[PanelSlider] SnapTo({condition}) requires a Direction — " +
                           "FullyIn/FullyOut are ambiguous without one. " +
                           "Use SnapTo(panel, dir, condition) instead.");
            return;
        }

        if (!TryGetRects(panel, out Rect panelRect, out Rect screen))
            return;

        panel.anchoredPosition = SnapTarget(condition, panelRect, screen, panel.anchoredPosition);
    }

    /// <inheritdoc cref="SnapTo(RectTransform, SlideFinishCondition)"/>
    /// <param name="session">In-flight session to reset alongside the snap.</param>
    public static void SnapTo(ref SlideSession session,
                               RectTransform panel, SlideFinishCondition condition)
    {
        SnapTo(panel, condition);
        session = default;
    }

    // ═══════════════════════════════════════════════════════
    //  Progress Queries
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Returns how far the slide has progressed as a 0–1 value.
    /// <para>• Eased sessions  → based on <see cref="SlideSession.Elapsed"/> / Duration.</para>
    /// <para>• Linear sessions → based on distance travelled vs total distance.</para>
    /// <para>Returns 1 when the session is inactive (already finished or never started).</para>
    /// </summary>
    public static float Progress(in SlideSession session, RectTransform panel)
    {
        if (!session.Active) return 1f;

        if (session.Easing != null)
        {
            // Eased: time-based progress (pre-easing curve)
            return Mathf.Clamp01(session.Elapsed / session.Easing.Duration);
        }

        // Linear: spatial progress along the dominant axis
        float total = Vector2.Distance(session.Start, session.End);
        if (total < Mathf.Epsilon) return 1f;

        float travelled = Vector2.Distance(session.Start, panel.anchoredPosition);
        return Mathf.Clamp01(travelled / total);
    }

    /// <summary>
    /// Returns seconds remaining in the slide.
    /// <para>• Eased sessions  → exact: <c>Duration - Elapsed</c>.</para>
    /// <para>• Linear sessions → estimated from current speed (<paramref name="speed"/> required).</para>
    /// <para>Returns 0 when the session is inactive.</para>
    /// </summary>
    public static float TimeRemaining(in SlideSession session, RectTransform panel, float speed = 0f)
    {
        if (!session.Active) return 0f;

        if (session.Easing != null)
            return Mathf.Max(0f, session.Easing.Duration - session.Elapsed);

        // Linear: distance left / speed
        if (speed <= 0f)
        {
            Debug.LogWarning("[PanelSlider] TimeRemaining for a linear session requires speed > 0.");
            return 0f;
        }

        float remaining = Vector2.Distance(panel.anchoredPosition, session.End);
        return remaining / speed;
    }

    // ═══════════════════════════════════════════════════════
    //  Session
    // ═══════════════════════════════════════════════════════

    private static void BeginSession(ref SlideSession session, RectTransform panel,
                                     Direction dir, Vector2 target, EasingConfig easing)
    {
        session.Dir = dir;
        session.Start = panel.anchoredPosition;
        session.End = target;
        session.Easing = easing;
        session.Elapsed = 0f;
        session.Active = true;
    }

    private static bool Tick(ref SlideSession session, RectTransform panel, float speed)
        => session.Easing != null
            ? TickEased(ref session, panel)
            : TickLinear(ref session, panel, speed);

    // ── Linear tick ────────────────────────────────────────

    private static bool TickLinear(ref SlideSession session, RectTransform panel, float speed)
    {
        Vector2 before = panel.anchoredPosition;
        panel.anchoredPosition += ToVector(session.Dir) * speed * Time.deltaTime;
        Vector2 after = panel.anchoredPosition;

        bool crossed = session.Dir switch
        {
            Direction.Left => before.x >= session.End.x && after.x <= session.End.x,
            Direction.Right => before.x <= session.End.x && after.x >= session.End.x,
            Direction.Up => before.y <= session.End.y && after.y >= session.End.y,
            Direction.Down => before.y >= session.End.y && after.y <= session.End.y,
            _ => false,
        };

        if (!crossed) return false;

        Vector2 snapped = after;
        if (session.Dir is Direction.Left or Direction.Right) snapped.x = session.End.x;
        else snapped.y = session.End.y;

        panel.anchoredPosition = snapped;
        session.Active = false;
        return true;
    }

    // ── Eased tick ─────────────────────────────────────────

    private static bool TickEased(ref SlideSession session, RectTransform panel)
    {
        session.Elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(session.Elapsed / session.Easing.Duration);
        float easedT = EasingMath.Evaluate(session.Easing, t);

        panel.anchoredPosition = Vector2.LerpUnclamped(session.Start, session.End, easedT);

        if (t < 1f) return false;

        panel.anchoredPosition = session.End;
        session.Active = false;
        return true;
    }

    // ═══════════════════════════════════════════════════════
    //  Target Computation
    // ═══════════════════════════════════════════════════════

    private static Vector2 ComputeTarget(RectTransform panel,
                                         Direction dir, SlideFinishCondition condition)
    {
        if (!TryGetRects(panel, out Rect panelRect, out Rect screen))
            return panel.anchoredPosition;

        Vector2 pos = panel.anchoredPosition;

        return condition switch
        {
            SlideFinishCondition.FullyIn => FullyInTarget(dir, panelRect, screen, pos),
            SlideFinishCondition.FullyOut => FullyOutTarget(dir, panelRect, screen, pos),
            _ => SnapTarget(condition, panelRect, screen, pos),
        };
    }

    private static Vector2 FullyInTarget(Direction dir, Rect panel, Rect screen, Vector2 pos)
        => dir switch
        {
            Direction.Up => new Vector2(pos.x, pos.y + (screen.yMin - panel.yMin)),
            Direction.Down => new Vector2(pos.x, pos.y + (screen.yMax - panel.yMax)),
            Direction.Left => new Vector2(pos.x + (screen.xMax - panel.xMax), pos.y),
            Direction.Right => new Vector2(pos.x + (screen.xMin - panel.xMin), pos.y),
            _ => pos,
        };

    private static Vector2 FullyOutTarget(Direction dir, Rect panel, Rect screen, Vector2 pos)
        => dir switch
        {
            Direction.Up => new Vector2(pos.x, pos.y + (screen.yMax - panel.yMin)),
            Direction.Down => new Vector2(pos.x, pos.y + (screen.yMin - panel.yMax)),
            Direction.Left => new Vector2(pos.x + (screen.xMin - panel.xMax), pos.y),
            Direction.Right => new Vector2(pos.x + (screen.xMax - panel.xMin), pos.y),
            _ => pos,
        };

    private static Vector2 SnapTarget(SlideFinishCondition condition,
                                      Rect panel, Rect screen, Vector2 pos)
    {
        float dx = 0f, dy = 0f;

        switch (condition)
        {
            case SlideFinishCondition.LeftEdgeToLeftBound: dx = screen.xMin - panel.xMin; break;
            case SlideFinishCondition.RightEdgeToRightBound: dx = screen.xMax - panel.xMax; break;
            case SlideFinishCondition.TopEdgeToTopBound: dy = screen.yMax - panel.yMax; break;
            case SlideFinishCondition.BottomEdgeToBottomBound: dy = screen.yMin - panel.yMin; break;

            case SlideFinishCondition.CenterToCenter:
                dx = screen.center.x - panel.center.x;
                dy = screen.center.y - panel.center.y; break;
            case SlideFinishCondition.LeftEdgeToCenter: dx = screen.center.x - panel.xMin; break;
            case SlideFinishCondition.RightEdgeToCenter: dx = screen.center.x - panel.xMax; break;
            case SlideFinishCondition.TopEdgeToCenter: dy = screen.center.y - panel.yMax; break;
            case SlideFinishCondition.BottomEdgeToCenter: dy = screen.center.y - panel.yMin; break;

            case SlideFinishCondition.BottomEdgeToTopBound: dy = screen.yMax - panel.yMin; break;
            case SlideFinishCondition.TopEdgeToBottomBound: dy = screen.yMin - panel.yMax; break;
            case SlideFinishCondition.LeftEdgeToRightBound: dx = screen.xMax - panel.xMin; break;
            case SlideFinishCondition.RightEdgeToLeftBound: dx = screen.xMin - panel.xMax; break;
        }

        return new Vector2(pos.x + dx, pos.y + dy);
    }

    // ═══════════════════════════════════════════════════════
    //  Geometry Helpers
    // ═══════════════════════════════════════════════════════

    private static bool TryGetRects(RectTransform panel, out Rect panelRect, out Rect screen)
    {
        panelRect = screen = Rect.zero;

        if (panel.parent is not RectTransform parentRT)
        {
            Debug.LogError("[PanelSlider] Panel has no RectTransform parent.");
            return false;
        }

        Vector3[] corners = new Vector3[4];
        panel.GetWorldCorners(corners);
        for (int i = 0; i < 4; i++)
            corners[i] = parentRT.InverseTransformPoint(corners[i]);

        panelRect = new Rect(corners[0].x, corners[0].y,
                             corners[2].x - corners[0].x,
                             corners[2].y - corners[0].y);
        screen = parentRT.rect;
        return true;
    }

    private static Vector2 ToVector(Direction dir) => dir switch
    {
        Direction.Left => Vector2.left,
        Direction.Right => Vector2.right,
        Direction.Up => Vector2.up,
        Direction.Down => Vector2.down,
        _ => Vector2.zero,
    };
}