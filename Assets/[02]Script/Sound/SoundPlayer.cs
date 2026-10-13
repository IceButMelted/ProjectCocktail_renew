using UnityEngine;

/// <summary>
/// Put on any GameObject and call Play() / Stop() from a UnityEvent
/// (Button.onClick, GameFlowHooks state hooks, ...) — no code needed.
/// </summary>
public class SoundPlayer : MonoBehaviour
{
    [SerializeField] private SoundChannel _channel = SoundChannel.SFX;
    [SerializeField, SoundId(nameof(_channel))] private string _id;
    [SerializeField] private bool _playOnEnable;

    [Tooltip("Crossfade / fade-out time (BGM and Ambient only)")]
    [SerializeField, Min(0f)] private float _fade = 1f;

    private void OnEnable()
    {
        if (_playOnEnable) Play();
    }

    public void Play()
    {
        var sm = SoundManager.Instance;
        if (sm == null || string.IsNullOrEmpty(_id)) return;

        switch (_channel)
        {
            case SoundChannel.BGM: sm.PlayBGM(_id, _fade); break;
            case SoundChannel.Ambient: sm.PlayAmbient(_id, _fade); break;
            case SoundChannel.SFX: sm.PlaySFX(_id); break;
            case SoundChannel.UiSFX: sm.PlayUiSFX(_id); break;
            case SoundChannel.Voice: sm.PlayVoice(_id); break;
        }
    }

    /// <summary>BGM / Ambient fade out, Voice is interrupted. One-shot SFX / UiSFX can't be stopped.</summary>
    public void Stop()
    {
        var sm = SoundManager.Instance;
        if (sm == null) return;

        switch (_channel)
        {
            case SoundChannel.BGM: sm.StopBGM(_fade); break;
            case SoundChannel.Ambient: sm.StopAmbient(_fade); break;
            case SoundChannel.Voice: sm.StopVoice(); break;
        }
    }
}
