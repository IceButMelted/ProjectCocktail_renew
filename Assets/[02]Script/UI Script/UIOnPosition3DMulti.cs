using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extended version of UIOnPosition3D that supports 2–4 reference points.
///
/// The order of <see cref="_refPoints"/> defines placement priority.
/// Each point's screen position relative to the midpoint determines its side
/// (Up / Down / Left / Right). The first candidate that fits on screen wins;
/// the last is the fallback.
///
/// 0–1 points — delegates to base class.
/// </summary>
public class UIOnPosition3DMulti : UIOnPosition3D
{
    [Header("Reference Points")]
    [Tooltip("2–4 world-space transforms. List order = placement priority. 0–1 falls back to base-class behaviour.")]
    [SerializeField] private List<Transform> _refPoints = new();

    [Header("Shift Gap")]
    [Tooltip("Extra pixel gap added on top of the auto half-size shift.")]
    [SerializeField] private float _shiftGap = 8f;

    // ── Public API ────────────────────────────────────────

    public List<Transform> RefPoints { get => _refPoints; set => _refPoints = value; }
    public float ShiftGap { get => _shiftGap; set => _shiftGap = value; }

    // ── Pre-allocated buffers (zero per-frame GC) ─────────

    private readonly List<Vector3> _screenPts = new(4);
    private readonly List<Vector2> _candidates = new(4);

    // ── Unity ─────────────────────────────────────────────

    protected override void LateUpdate()
    {
        if (_refPoints == null || _refPoints.Count <= 1)
        {
            base.LateUpdate();
            return;
        }

        if (UICanvas == null) return;

        // ── Collect valid screen points ───────────────────
        _screenPts.Clear();
        foreach (Transform t in _refPoints)
            if (t != null) _screenPts.Add(Cam.WorldToScreenPoint(t.position));

        if (_screenPts.Count == 0) return;

        // ── Midpoint ──────────────────────────────────────
        Vector3 mid = Vector3.zero;
        for (int i = 0; i < _screenPts.Count; i++) mid += _screenPts[i];
        mid /= _screenPts.Count;

        bool isBehind = mid.z < 0f;
        gameObject.SetActive(!isBehind);
        if (isBehind) return;

        // ── Build candidates ordered by ref-point sequence ─
        // Each point's position relative to the midpoint determines its side.
        // Duplicate sides are skipped so we never test the same position twice.
        Vector2 halfSize = RectTr.rect.size * 0.5f;
        float shiftH = halfSize.x + _shiftGap;
        float shiftV = halfSize.y + _shiftGap;

        _candidates.Clear();
        foreach (Vector3 pt in _screenPts)
        {
            float dx = pt.x - mid.x;
            float dy = pt.y - mid.y;
            Vector2 candidate = Mathf.Abs(dx) >= Mathf.Abs(dy)
                ? new Vector2(mid.x + (dx >= 0f ? shiftH : -shiftH), mid.y)
                : new Vector2(mid.x, mid.y + (dy >= 0f ? shiftV : -shiftV));

            if (!_candidates.Contains(candidate))
                _candidates.Add(candidate);
        }

        if (_candidates.Count == 0)
            _candidates.Add(new Vector2(mid.x, mid.y + shiftV));

        // ── Pick first on-screen candidate ────────────────
        Vector2 chosen = _candidates[_candidates.Count - 1];
        foreach (Vector2 c in _candidates)
        {
            if (IsOnScreen(c, halfSize)) { chosen = c; break; }
        }

        RectTr.anchoredPosition = ScreenToCanvasPosition(new Vector3(chosen.x, chosen.y, mid.z))
                                + ScreenOffset;
    }
}