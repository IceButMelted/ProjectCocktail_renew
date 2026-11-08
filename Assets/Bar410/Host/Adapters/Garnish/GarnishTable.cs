using System.Collections.Generic;
using Bar410.WorldViews;
using UnityEngine;

namespace Bar410.Host
{
    /// <summary>
    /// Main-scene shell of the garnish table (on Garnish_Plate): the glass spawn, the pour, the picture of the
    /// glass and the customer seats (GarnishGlassView1/2/3, D6). UiSceneBindings hands these to GarnishUiHost.
    /// </summary>
    public sealed class GarnishTable : MonoBehaviour
    {
        [SerializeField] Transform _glassSpawn;
        [SerializeField] PourVisual _pour;
        [Tooltip("WorldViewSource on GarnishStageCam (the picture shown in the UI scene's GlassView).")]
        [SerializeField] WorldViewSource _glassView;
        [Tooltip("GarnishGlassView1/2/3, one per customer seat, in seat order (seat 0 = fallback).")]
        [SerializeField] Transform[] _seats;

        IGarnishTable _table;

        public IGarnishTable Table => _table ??= new GarnishTableCore(_glassSpawn, _pour);
        public WorldViewSource GlassView => _glassView;
        public IReadOnlyList<Transform> Seats => _seats;
    }
}
