using UnityEngine;

/// <summary>
/// One walking lane for menu NPCs. Place the Start/End points off-screen
/// (leave margin for the parallax camera sway). NPCs walk either direction.
/// </summary>
public class NpcLane : MonoBehaviour
{
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    public Vector3 Start => startPoint.position;
    public Vector3 End => endPoint.position;
    public bool IsValid => startPoint != null && endPoint != null;

    private void OnDrawGizmos()
    {
        if (!IsValid) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(Start, End);
        Gizmos.DrawWireSphere(Start, 0.15f);
        Gizmos.DrawWireSphere(End, 0.15f);
    }
}
