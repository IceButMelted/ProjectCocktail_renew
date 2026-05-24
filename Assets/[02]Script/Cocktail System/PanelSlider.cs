// ============================================================
//  PanelSlider.cs — Moves a UI panel in and out of the screen.
//
//  SOLID — S (Single Responsibility):
//    This class owns exactly one concern: sliding a RectTransform
//    in a given direction and reporting when a finish condition
//    is reached.  No game state, no MonoBehaviour lifecycle,
//    no knowledge of what game is being played.
//
//  SOLID — O (Open / Closed):
//    New finish conditions (e.g. "stop at center") are added as
//    a new Slide() overload — existing callers are never broken.
//
//  Usage:
//    var slider = new PanelSlider(myRectTransform, 800f);
//    // In Update:
//    bool done = slider.Slide(Direction.Up, SlideFinishCondition.FullyIn);
// ============================================================

using UnityEngine;
using static E_Cocktail;

// ── Finish Condition ───────────────────────────────────────

/// <summary>When to consider a slide operation complete.</summary>
public enum SlideFinishCondition
{
    /// <summary>Panel is fully inside its parent bounds.</summary>
    FullyIn,

    /// <summary>Panel no longer overlaps its parent bounds.</summary>
    FullyOut
}

// ── Slider ─────────────────────────────────────────────────

/// <summary>
/// Pure C# utility — no MonoBehaviour overhead.
/// Construct once in Awake, call Slide() every frame.
/// </summary>
public class PanelSlider
{
    // ── State ──────────────────────────────────────────────

    private readonly RectTransform _panel;
    private readonly float _speed;

    // ── Constructor ────────────────────────────────────────

    /// <param name="panel">The UI panel to move.</param>
    /// <param name="speed">Pixels per second.</param>
    public PanelSlider(RectTransform panel, float speed)
    {
        _panel = panel;
        _speed = speed;
    }

    // ── Public API ─────────────────────────────────────────

    /// <summary>
    /// Moves the panel one step toward <paramref name="dir"/>.
    /// Returns true once the <paramref name="finishCondition"/> is satisfied.
    /// </summary>
    public bool Slide(Direction dir, SlideFinishCondition finishCondition)
    {
        _panel.anchoredPosition += ToVector(dir) * _speed * Time.deltaTime;
        var (fullyIn, fullyOut) = CheckBoundary();
        return finishCondition == SlideFinishCondition.FullyIn ? fullyIn : fullyOut;
    }

    /// <summary>
    /// Moves the panel toward <paramref name="dir"/> until it crosses
    /// <paramref name="targetPosition"/>, then snaps to it.
    /// Returns true on the frame the target is crossed.
    /// </summary>
    public bool Slide(Direction dir, Vector2 targetPosition)
    {
        Vector2 before = _panel.anchoredPosition;
        _panel.anchoredPosition += ToVector(dir) * _speed * Time.deltaTime;
        Vector2 after  = _panel.anchoredPosition;

        bool crossed = dir switch
        {
            Direction.Left  => before.x >= targetPosition.x && after.x <= targetPosition.x,
            Direction.Right => before.x <= targetPosition.x && after.x >= targetPosition.x,
            Direction.Up    => before.y <= targetPosition.y && after.y >= targetPosition.y,
            Direction.Down  => before.y >= targetPosition.y && after.y <= targetPosition.y,
            _               => false
        };

        if (crossed)
        {
            Vector2 snapped = after;
            if (dir is Direction.Left  or Direction.Right) snapped.x = targetPosition.x;
            if (dir is Direction.Up    or Direction.Down)  snapped.y = targetPosition.y;
            _panel.anchoredPosition = snapped;
        }

        return crossed;
    }

    // ── Private Helpers ────────────────────────────────────

    private static Vector2 ToVector(Direction dir) => dir switch
    {
        Direction.Left  => Vector2.left,
        Direction.Right => Vector2.right,
        Direction.Up    => Vector2.up,
        Direction.Down  => Vector2.down,
        _               => Vector2.zero
    };

    private Rect GetPanelRectInParent()
    {
        Vector3[] corners = new Vector3[4];
        _panel.GetWorldCorners(corners);

        var parent = _panel.parent as RectTransform;
        for (int i = 0; i < 4; i++)
            corners[i] = parent.InverseTransformPoint(corners[i]);

        return new Rect(
            corners[0].x, corners[0].y,
            corners[2].x - corners[0].x,
            corners[2].y - corners[0].y);
    }

    private Rect GetParentRect()
        => (_panel.parent as RectTransform).rect;

    private (bool fullyIn, bool fullyOut) CheckBoundary()
    {
        Rect panel  = GetPanelRectInParent();
        Rect screen = GetParentRect();

        bool fullyIn  = panel.xMin >= screen.xMin && panel.xMax <= screen.xMax &&
                        panel.yMin >= screen.yMin && panel.yMax <= screen.yMax;
        bool fullyOut = !panel.Overlaps(screen);

        return (fullyIn, fullyOut);
    }
}
