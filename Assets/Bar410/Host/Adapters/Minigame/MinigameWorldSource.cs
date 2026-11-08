using Bar410.Contracts;
using Bar410.WorldViews;
using UnityEngine;
using static E_Cocktail;

namespace Bar410.Host
{
    /// <summary>What the minigame UI scene needs from the main scene for one game type.</summary>
    public sealed class MinigameWorld
    {
        public WorldViewHandle View;
        public IMinigameAnimation Animation;
    }

    /// <summary>IMinigameAnimation over the world-side AnimationMinigameController.</summary>
    public sealed class AmcAnimation : IMinigameAnimation
    {
        readonly AnimationMinigameController _amc;
        public AmcAnimation(AnimationMinigameController amc) { _amc = amc; }
        public void Play() { if (_amc != null) _amc.PlayAnimation(); }   // Unity null: the main scene may be gone
        public void Stop() { if (_amc != null) _amc.StopAnimation(); }
    }

    /// <summary>
    /// Main scene, one per minigame type: the render source and the animator the UI-scene minigame drives.
    /// UiSceneBindings collects these and binds them to MinigameUiHost once the UI scene registers.
    /// </summary>
    public sealed class MinigameWorldSource : MonoBehaviour
    {
        [SerializeField] Enum_MiniGameType _type;
        [SerializeField] WorldViewSource _view;
        [SerializeField] AnimationMinigameController _amc;

        public Enum_MiniGameType Type => _type;
        public WorldViewSource View => _view;

        public MinigameWorld Build() => new()
        {
            View = _view != null ? _view.Handle : null,
            Animation = _amc != null ? new AmcAnimation(_amc) : null,
        };
    }
}
