// ============================================================
//  FruitTraySlot.cs — One discrete fruit-piece position on a tray
//  (e.g. LimeJuice, LemonJuice — the Mixer values presented as fruit
//  instead of a bottle). Same respawn pattern as GlassShelfSlot: keeps
//  a fresh FruitPieceInstance ready to be dragged, respawning whenever
//  the previous piece is consumed.
// ============================================================

using UnityEngine;
using static E_Cocktail;

public class FruitTraySlot : MonoBehaviour
{
    [SerializeField] private Mixer _fruitType;
    [SerializeField] private GameObject _piecePrefab;

    private void Awake() => SpawnReplacement();

    /// <summary>
    /// Instantiates a fresh FruitPieceInstance at this slot. Called on Awake and again by
    /// FruitPieceInstance once the previous piece has been consumed (delivered or dropped short).
    /// </summary>
    public void SpawnReplacement()
    {
        if (_piecePrefab == null)
        {
            Debug.LogWarning($"[FruitTraySlot] '{name}' has no piece prefab assigned.", this);
            return;
        }

        var instance = Instantiate(_piecePrefab, transform.position, transform.rotation);
        var piece = instance.GetComponent<FruitPieceInstance>();

        if (piece == null)
        {
            Debug.LogWarning($"[FruitTraySlot] '{_piecePrefab.name}' has no FruitPieceInstance component.", this);
            return;
        }

        piece.Initialize(_fruitType, this);
    }
}
