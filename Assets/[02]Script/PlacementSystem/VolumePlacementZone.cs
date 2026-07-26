using UnityEngine;

/// <summary>
/// The item is centered on TWO axes and free to translate along only the
/// volume's LONGEST axis (e.g. sliding along a shelf's length), clamped so
/// it never pokes out of the volume's bounds.
///
/// Enable _snapToCenter to lock the item to the exact center of the volume
/// on all three axes instead — useful for slots that only ever hold one
/// item in one spot (e.g. a single potion holder).
///
/// Setup:
///   - Attach to an empty GameObject with a BoxCollider (set isTrigger = true).
///   - Size/position the BoxCollider to match the interior of your shelf/cubby/etc.
///   - Put that collider on the layer assigned to N_InputManager._zoneLayer.
///   - By default the longest local axis of the BoxCollider is auto-picked as
///     the free/translate axis. Untick _autoDetectTranslateAxis to force a
///     specific axis instead.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class VolumePlacementZone : PlacementZoneBase
{
    private enum Axis { X, Y, Z }
    private enum PositionPlacement { CenterInVolume, BottomInVolume, CeilingInVolume, CenterItemToCenterVolume  }

    [Header("Volume Zone")]
    [Tooltip("If true, the longest local axis of the BoxCollider is auto-picked as the free/translate axis. The other two axes are locked to center.")]
    [SerializeField] private bool _autoDetectTranslateAxis = true;
    [SerializeField] private Axis _manualTranslateAxis = Axis.X;
    [SerializeField] private PositionPlacement _positionPlacement = PositionPlacement.CenterInVolume;

    private BoxCollider _volume;
    private Axis _translateAxis;

    private void Awake()
    {
        _volume = GetComponent<BoxCollider>();
        _translateAxis = _autoDetectTranslateAxis ? FindLongestAxis(_volume.size) : _manualTranslateAxis;
    }

    public override Vector3 GetClampedPosition(RaycastHit hit, Bounds itemBounds)
    {
        if (_positioningMode == PositioningMode.SnapToCenter)
            return transform.TransformPoint(_volume.center);

        // Hit point -> zone local space (relative to the BoxCollider's own center).
        Vector3 local = transform.InverseTransformPoint(hit.point) - _volume.center;

        Vector3 halfVolume = _volume.size * 0.5f;
        Vector3 halfItem = Abs(transform.InverseTransformVector(itemBounds.extents));

        // Only the picked axis reads from the hit point; the other two are
        // locked to zero (center). This is what makes the result immune to
        // WHICH face of the box the raycast happened to hit — a hit on a
        // side face still gives a valid coordinate along the long axis,
        // whereas its coordinates on the other two axes are meaningless
        // (just wherever the ray clipped that face's edge) and are now ignored.
        float x = 0f, y = 0f, z = 0f;

        switch (_translateAxis)
        {
            case Axis.X:
                x = Mathf.Clamp(local.x, -halfVolume.x + halfItem.x, halfVolume.x - halfItem.x);
                break;
            case Axis.Y:
                y = Mathf.Clamp(local.y, -halfVolume.y + halfItem.y, halfVolume.y - halfItem.y);
                break;
            default: // Axis.Z
                z = Mathf.Clamp(local.z, -halfVolume.z + halfItem.z, halfVolume.z - halfItem.z);
                break;
        }

        Vector3 clampedLocal = new Vector3(x, y, z);

        // Only override the Y axis if it ISN'T the free/translate axis —
        // otherwise this would silently cancel out the sliding we just did above.
        if (_translateAxis != Axis.Y)
        {
            switch (_positionPlacement)
            {
                case PositionPlacement.CenterInVolume:
                    clampedLocal.y = 0f;
                    break;

                case PositionPlacement.CeilingInVolume:
                    // Rest against the ceiling of the volume, pulled down by the
                    // item's own half-height so it doesn't poke out through the top.
                    clampedLocal.y = 2 * (-halfItem.y) + halfVolume.y;
                    break;

                case PositionPlacement.BottomInVolume:
                    // Rest on the floor of the volume, pushed up by the item's
                    // own half-height so it doesn't poke out through the bottom.
                    clampedLocal.y = -halfVolume.y;
                    break;
                case PositionPlacement.CenterItemToCenterVolume:
                    
                    clampedLocal.y = - halfItem.y;
                    break;
            }
        }

        clampedLocal += _volume.center;
        return transform.TransformPoint(clampedLocal);
    }

    private static Axis FindLongestAxis(Vector3 size)
    {
        if (size.x >= size.y && size.x >= size.z) return Axis.X;
        if (size.z >= size.x && size.z >= size.y) return Axis.Z;
        return Axis.Y;
    }

    /// <summary>
    /// No extra offset — GetClampedPosition already returns the exact pivot
    /// position (it centers the item's own extents inside the volume), so
    /// this must NOT stack another bottom offset on top like the base class
    /// does for surfaces, or the item ends up pushed out of the volume.
    /// </summary>
    public override Vector3 GetPivotOffset(float bottomOffset) => Vector3.zero;

    private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    private void OnDrawGizmosSelected()
    {
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc == null) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(bc.center, bc.size);
    }
}