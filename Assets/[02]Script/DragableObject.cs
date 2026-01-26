using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DragableObject : MonoBehaviour
{
    public bool CanPlaced = false;
    private bool _beingDrags = false;
    public bool BeingDrags
    {
        get { return _beingDrags; }
        set { _beingDrags = value; }
    }

    public int numbersObjectOverlaying = 0;
    private Vector3 _pastLocation;
    public Vector3 PastLocation
    {
        get { return _pastLocation; }
        set { _pastLocation = value; }
    }

    // Define the half extents (half size) of the overlap box in the Inspector
    public Vector3 boxHalfExtents = new Vector3(1f, 1f, 1f);
    // Use a LayerMask to filter which objects to detect
    public LayerMask detectLayerMask;
    private Collider colliderOfObject;

    [Header("Debugging Zone")]
    public bool DebugDraw = false;   

    private void Awake()
    {
        _pastLocation = gameObject.transform.position;
        colliderOfObject = GetComponent<Collider>();
    }

    void FixedUpdate()
    {
        //BeingDrags = true; //for check code before attach to other code

        if (!BeingDrags)
        {
            return;
        }
        CheckForCollisions();
    }

    void CheckForCollisions()
    {
        // Call OverlapBox at the current object's position and rotation
        Collider[] hitColliders = Physics.OverlapBox(
            transform.position,
            boxHalfExtents,
            transform.rotation,
            detectLayerMask
        );

        //not count it self as overlap opject

        // Process the colliders found
        if (hitColliders.Length - 1 > 0)
        {
            CanPlaced = false;
            Debug.Log("Overlap detected with " + hitColliders.Length + " colliders!");
            foreach (Collider hitCollider in hitColliders)
            {
                // Access information about the overlapping object
                Debug.Log("Colliding with: " + hitCollider.gameObject.name);
            }
        }
        else { 
            CanPlaced = true;
        }

        numbersObjectOverlaying = hitColliders.Length - 1 ;
    }

    // Optional: Draw the box in the scene view for debugging purposes
    void OnDrawGizmos()
    {
        if(!DebugDraw) { return; }
        Gizmos.color = CanPlaced ? Color.green :Color.red;
        // The Gizmos matrix is used to apply the object's rotation to the drawn cube
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2); // Gizmo size is full size, not half
    }

}