using System;
using UnityEngine;

/// <summary>
/// One playing sound with its own AudioSource (N2). Created by SoundManager.PlayVoiceHandle / PlaySFXHandle.
/// IsPlaying stays true while the source is paused; Ended fires exactly once (natural end, Stop, or teardown).
/// </summary>
public sealed class SoundHandle
{
    readonly AudioSource _src;
    bool _ended;

    internal SoundHandle(AudioSource src) { _src = src; }

    public bool IsPlaying => !_ended;
    public event Action Ended;

    public void Stop() => Finish();

    internal void Pause(bool paused)
    {
        if (_ended || _src == null) return;
        if (paused) _src.Pause(); else _src.UnPause();
    }

    /// <summary>Called by SoundManager each frame. Returns true when the handle is finished and can be dropped.</summary>
    internal bool Tick(bool paused)
    {
        if (_ended) return true;
        if (_src == null || (!paused && !_src.isPlaying)) Finish();
        return _ended;
    }

    internal void Finish()
    {
        if (_ended) return;
        _ended = true;
        if (_src != null) UnityEngine.Object.Destroy(_src.gameObject);
        Ended?.Invoke();
    }
}
