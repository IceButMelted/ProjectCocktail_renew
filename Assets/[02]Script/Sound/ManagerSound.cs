using UnityEngine;
using System.Collections.Generic;

public static class ManagerSound
{
    //  Volume Controls  (0.0 – 1.0)
    //  ┌─────────────────────────────────────────────────────────────────────┐
    //  │  Final Volume = MasterVolume × ChannelVolume × clip.volume          │
    //  └─────────────────────────────────────────────────────────────────────┘
    public static float MasterVolume = 1.0f;
    public static float BGMVolume = 0.8f;
    public static float AmbientVolume = 0.5f;
    public static float SFXVolume = 1.0f;
    public static float UiSFXVolume = 1.0f;
    public static float VoiceVolume = 1.0f;

    // ── Equation helpers ──────────────────────────────────────────────────────
    static float FinalBGM(float clip) => Mathf.Clamp01(MasterVolume * BGMVolume * clip);
    static float FinalAmbient(float clip) => Mathf.Clamp01(MasterVolume * AmbientVolume * clip);
    static float FinalSFX(float clip) => Mathf.Clamp01(MasterVolume * SFXVolume * clip);
    static float FinalUiSFX(float clip) => Mathf.Clamp01(MasterVolume * UiSFXVolume * clip);
    static float FinalVoice(float clip) => Mathf.Clamp01(MasterVolume * VoiceVolume * clip);

    // ── Sound Registries ──────────────────────────────────────────────────────
    private static readonly Dictionary<string, SoundEntry> _ambient = new();
    private static readonly Dictionary<string, SoundEntry> _bgm = new();
    private static readonly Dictionary<string, SoundEntry> _sfx = new();
    private static readonly Dictionary<string, SoundEntry> _uiSfx = new();
    private static readonly Dictionary<string, SoundEntry> _voice = new();

    // Stores (source + entry) so SetSFXVolume can refresh looping sources
    private static readonly Dictionary<string, (AudioSource src, SoundEntry entry)> _loopingSfx = new();

    // Track current looping entries so SetBGMVolume / SetAmbientVolume can refresh them live
    private static SoundEntry _currentBGM;
    private static SoundEntry _currentAmbient;

    // ── Audio Sources ─────────────────────────────────────────────────────────
    private static AudioSource _ambientSrc;
    private static AudioSource _bgmSrc;
    private static AudioSource _uiSfxSrc;    // 2D one-shot UI SFX
    private static AudioSource _voiceSrc;    // 2D one-shot / interruptible voice
    private static SoundRunner _runner;

    // ── SFX Pool ──────────────────────────────────────────────────────────────
    public static int MaxSFXCount = 5;       // adjust freely at runtime
    private static AudioSource[] _sfxPool;
    private static int _sfxOldest = 0;       // ponytail: ring-buffer index, oldest = next to evict


    //  Init  — call once at game start, pass SoundData in

    public static void Init(SoundData data)
    {
        if (_runner != null) return;

        var go = new GameObject("[ManagerSound]");
        Object.DontDestroyOnLoad(go);

        _runner = go.AddComponent<SoundRunner>();
        _ambientSrc = go.AddComponent<AudioSource>();
        _bgmSrc = go.AddComponent<AudioSource>();
        _uiSfxSrc = go.AddComponent<AudioSource>();
        _voiceSrc = go.AddComponent<AudioSource>();

        // Build SFX pool
        _sfxPool = new AudioSource[MaxSFXCount];
        for (int i = 0; i < MaxSFXCount; i++)
        {
            _sfxPool[i] = go.AddComponent<AudioSource>();
            _sfxPool[i].spatialBlend = 0f;
        }
        _sfxOldest = 0;

        // All non-pool sources are 2D
        foreach (var src in new[] { _ambientSrc, _bgmSrc, _uiSfxSrc, _voiceSrc })
            src.spatialBlend = 0f;

        _ambientSrc.loop = true;
        _bgmSrc.loop = true;

        RegisterList(_ambient, data.ambients);
        RegisterList(_bgm, data.bgms);
        RegisterList(_sfx, data.effects);
        RegisterList(_uiSfx, data.uiSfx);
        RegisterList(_voice, data.voices);

        Debug.Log($"[ManagerSound] Init — "
            + $"Ambient:{_ambient.Count} | BGM:{_bgm.Count} | "
            + $"SFX:{_sfx.Count} | UiSFX:{_uiSfx.Count} | Voice:{_voice.Count}");
    }

    static void RegisterList(Dictionary<string, SoundEntry> dict, List<SoundEntry> list)
    {
        foreach (var e in list)
        {
            if (e.clip == null || string.IsNullOrEmpty(e.id)) continue;
            dict[e.id] = e;
        }
    }


    //  Volume Setters — wire these to your UI sliders
    //  Each setter applies the equation instantly to currently-playing sources.

    #region Set Volume Helpers
    /// <summary>Master fader — refreshes ALL channels immediately.</summary>
    public static void SetMasterVolume(float v)
    {
        MasterVolume = Mathf.Clamp01(v);
        RefreshVolumes();
    }

    /// <summary>BGM channel — refreshes the looping BGM source immediately.</summary>
    public static void SetBGMVolume(float v)
    {
        BGMVolume = Mathf.Clamp01(v);
        if (_bgmSrc != null && _currentBGM != null)
            _bgmSrc.volume = FinalBGM(_currentBGM.volume);
    }

    /// <summary>Ambient channel — refreshes the looping ambient source immediately.</summary>
    public static void SetAmbientVolume(float v)
    {
        AmbientVolume = Mathf.Clamp01(v);
        if (_ambientSrc != null && _currentAmbient != null)
            _ambientSrc.volume = FinalAmbient(_currentAmbient.volume);
    }

    /// <summary>SFX channel — refreshes all active looping SFX sources immediately.
    /// One-shot SFX already fired cannot be updated (fire-and-forget).</summary>
    public static void SetSFXVolume(float v)
    {
        SFXVolume = Mathf.Clamp01(v);
        foreach (var (src, entry) in _loopingSfx.Values)
            src.volume = FinalSFX(entry.volume);
    }

    /// <summary>UI SFX channel volume (applied on next PlayUiSFX call).</summary>
    public static void SetUiSFXVolume(float v) => UiSFXVolume = Mathf.Clamp01(v);

    /// <summary>Voice channel volume (applied on next PlayVoice call).</summary>
    public static void SetVoiceVolume(float v) => VoiceVolume = Mathf.Clamp01(v);
    #endregion

    /// <summary>Refreshes volume on ALL currently-playing looping sources.
    /// Call this after loading saved volume preferences.</summary>
    public static void RefreshVolumes()
    {
        if (_ambientSrc != null && _currentAmbient != null)
            _ambientSrc.volume = FinalAmbient(_currentAmbient.volume);

        if (_bgmSrc != null && _currentBGM != null)
            _bgmSrc.volume = FinalBGM(_currentBGM.volume);

        foreach (var (src, entry) in _loopingSfx.Values)
            src.volume = FinalSFX(entry.volume);
    }



    //  Ambient
    public static void PlayAmbient(string id, float fade = 1f)
    {
        if (!_ambient.TryGetValue(id, out var e)) return;
        _currentAmbient = e;
        _runner.Fade(_ambientSrc, e.clip, FinalAmbient(e.volume), fade);
    }

    public static void StopAmbient(float fade = 1f)
    {
        _currentAmbient = null;
        _runner.FadeOut(_ambientSrc, fade);
    }


    //  BGM
    public static void PlayBGM(string id, float fade = 1f)
    {
        if (!_bgm.TryGetValue(id, out var e)) return;
        _currentBGM = e;
        _runner.Fade(_bgmSrc, e.clip, FinalBGM(e.volume), fade);
    }

    public static void StopBGM(float fade = 1f)
    {
        _currentBGM = null;
        _runner.FadeOut(_bgmSrc, fade);
    }

    public static void PauseBGM() => _bgmSrc?.Pause();
    public static void ResumeBGM() => _bgmSrc?.UnPause();


    //  SFX  — 2D one-shot, capped at MaxSFXCount voices
    public static void PlaySFX(string id)
    {
        if (!CheckInit("PlaySFX")) return;
        if (!_sfx.TryGetValue(id, out var e)) return;

        // Find a free slot; if none, evict the oldest (ring buffer)
        AudioSource slot = null;
        for (int i = 0; i < _sfxPool.Length; i++)
        {
            if (!_sfxPool[i].isPlaying) { slot = _sfxPool[i]; break; }
        }
        if (slot == null)
        {
            slot = _sfxPool[_sfxOldest];
            slot.Stop();
            _sfxOldest = (_sfxOldest + 1) % _sfxPool.Length;
        }

        slot.PlayOneShot(e.clip, FinalSFX(e.volume));
    }

    // Backward-compat alias
    public static void PlayEffect(string id) => PlaySFX(id);


    //  Loop SFX  — 2D looping (creates own GameObject)
    public static void LoopSFX(string id)
    {
        if (!CheckInit("LoopSFX")) return;
        if (_loopingSfx.ContainsKey(id)) return;      // already playing
        if (!_sfx.TryGetValue(id, out var e)) return;

        var go = new GameObject($"[SFX_Loop]{id}");
        Object.DontDestroyOnLoad(go);
        var src = go.AddComponent<AudioSource>();
        src.clip = e.clip;
        src.volume = FinalSFX(e.volume);
        src.loop = true;
        src.spatialBlend = 0f;
        src.Play();

        _loopingSfx[id] = (src, e);
    }

    public static void StopLoopSFX(string id)
    {
        if (!_loopingSfx.TryGetValue(id, out var pair)) return;
        Object.Destroy(pair.src.gameObject);
        _loopingSfx.Remove(id);
    }

    public static void StopAllLoopSFX()
    {
        foreach (var (src, _) in _loopingSfx.Values)
            Object.Destroy(src.gameObject);
        _loopingSfx.Clear();
    }

    // Backward-compat aliases
    public static void LoopEffect(string id) => LoopSFX(id);
    public static void StopLoopEffect(string id) => StopLoopSFX(id);
    public static void StopAllLoopEffect() => StopAllLoopSFX();


    //  UI SFX  — 2D one-shot (button clicks, menu sounds, etc.)
    public static void PlayUiSFX(string id)
    {
        if (!CheckInit("PlayUiSFX")) return;
        if (!_uiSfx.TryGetValue(id, out var e)) return;
        _uiSfxSrc.PlayOneShot(e.clip, FinalUiSFX(e.volume));
    }

    //  Voice  — 2D one-shot / interruptible
    public static void PlayVoice(string id)
    {
        if (!CheckInit("PlayVoice")) return;
        if (!_voice.TryGetValue(id, out var e)) return;
        _voiceSrc.PlayOneShot(e.clip, FinalVoice(e.volume));
    }

    /// <summary>Interrupt the current voice line.</summary>
    public static void StopVoice() => _voiceSrc?.Stop();


    //  Global Mute  (toggles AudioListener — affects everything)
    public static void SetMute(bool mute)
        => AudioListener.volume = mute ? 0f : 1f;

    // ── Internal ─────────────────────────────────────────────────────────────
    static bool CheckInit(string caller)
    {
        if (_runner != null) return true;
        Debug.LogWarning($"[ManagerSound] {caller}: ยังไม่ได้ Init()");
        return false;
    }
}