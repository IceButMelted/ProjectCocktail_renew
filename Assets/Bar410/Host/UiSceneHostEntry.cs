using Bar410.UiScenes;
using UnityEngine;

namespace Bar410.Host
{
    /// <summary>The UiSceneEntry of the Minigame_UI and Garnish_UI scenes: registers the host component set in the Inspector.</summary>
    public sealed class UiSceneHostEntry : UiSceneEntry
    {
        [Tooltip("MinigameUiHost or GarnishUiHost in this scene.")]
        [SerializeField] MonoBehaviour _host;

        protected override object Host => _host;
    }
}
