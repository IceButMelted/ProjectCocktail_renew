// ============================================================
//  GlassShelfSlot.cs — A shelf position offering one glass option.
//
//  Never itself dragged. Keeps a fresh PlacedGlassInstance sitting at
//  the slot ready to be picked up, respawning whenever the previous
//  instance leaves for a placement zone — the infinitely-reusable
//  drag origin, mirroring how ingredient bottles are always available,
//  just via respawn instead of snap-back (a placed glass actually
//  leaves the shelf for good).
// ============================================================

using UnityEngine;

public class GlassShelfSlot : MonoBehaviour
{
    [SerializeField] private SO_GlassOption _option;

    private void Awake() => SpawnReplacement();

    /// <summary>
    /// Instantiates a fresh PlacedGlassInstance at this slot. Called on Awake and again by
    /// PlacedGlassInstance.NotifyPlaced once the previous instance has left for a zone.
    /// </summary>
    public void SpawnReplacement()
    {
        if (_option == null || _option.PlacedPrefab == null)
        {
            Debug.LogWarning($"[GlassShelfSlot] '{name}' has no SO_GlassOption/PlacedPrefab assigned.", this);
            return;
        }

        var instance = Instantiate(_option.PlacedPrefab, transform.position, transform.rotation);
        var glass = instance.GetComponent<PlacedGlassInstance>();

        if (glass == null)
        {
            Debug.LogWarning($"[GlassShelfSlot] '{_option.PlacedPrefab.name}' has no PlacedGlassInstance component.", this);
            return;
        }

        glass.Initialize(_option, this);
    }
}
