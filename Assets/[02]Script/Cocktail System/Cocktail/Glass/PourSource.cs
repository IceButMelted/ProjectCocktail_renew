// ============================================================
//  PourSource.cs — Marker only, no logic.
//
//  Added alongside the mixing vessel's (CocktailShaker) existing
//  DragableObject so GlassPlacementZone.CanPlace can tell "a glass is
//  arriving" from "the mixing vessel is arriving to pour" apart.
//  GarnishFlowBridge is what actually reacts to the drop.
// ============================================================

using UnityEngine;

public class PourSource : MonoBehaviour
{
}
