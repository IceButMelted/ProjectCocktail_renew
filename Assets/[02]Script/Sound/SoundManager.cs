using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Plays every sound through MainMixer. Volumes are mixer exposed params (SoundSettings),
/// per-clip gain stays in SoundData. One persistent instance; scene duplicates self-destruct.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private SoundData m_SoundData;
    [SerializeField] private SoundSettings m_SoundSettings;
    [SerializeField] private string m_BGMStart;
    [SerializeField, Min(1)] private int m_MaxSFXCount = 5;

    [Header("Optional UI (0-1 sliders, may stay empty)")]
    [SerializeField] private Slider m_SliderMasterVolume;
    [SerializeField] private Slider m_SliderMusicVolume;
    [SerializeField] private Slider m_SliderAmbientVolume;
    [SerializeField] private Slider m_SliderMasterSFXVolume;
    [SerializeField] private Slider m_SliderSFXVolume;
    [SerializeField] private Slider m_SliderUIVolume;
    [SerializeField] private Slider m_SliderVoiceVolume;

    // BGM / Ambient each own two sources so a new clip can fade in while the old one fades out.
    private class LoopChannel
    {
        public AudioSource A, B;
        public AudioSource Active;
        public SoundEntry Current;
    }

    private readonly Dictionary<string, SoundEntry> m_Ambient = new();
    private readonly Dictionary<string, SoundEntry> m_Bgm = new();
    private readonly Dictionary<string, SoundEntry> m_Sfx = new();
    private readonly Dictionary<string, SoundEntry> m_UiSfx = new();
    private readonly Dictionary<string, SoundEntry> m_Voice = new();

    private readonly LoopChannel m_BgmChannel = new();
    private readonly LoopChannel m_AmbientChannel = new();
    private readonly Dictionary<AudioSource, Coroutine> m_Fades = new();
    private readonly Dictionary<string, AudioSource> m_LoopingSfx = new();
    private readonly HashSet<string> m_Warned = new();

    private AudioMixerGroup m_SfxGroup;
    private AudioSource m_UiSfxSrc;
    private AudioSource m_VoiceSrc;
    private AudioSource[] m_SfxPool;
    private int m_SfxOldest;    // ponytail: ring-buffer index, oldest = next to evict

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.PlayBGM(m_BGMStart);   // scene's own start track, same as the old per-scene Init
            enabled = false;                // keep Start() from running before the deferred Destroy
            Destroy(gameObject);
            return;
        }

        if (m_SoundSettings == null || m_SoundSettings.AudioMixer == null || m_SoundData == null)
        {
            Debug.LogError("[SoundManager] SoundData / SoundSettings / AudioMixer ยังไม่ได้ assign", this);
            enabled = false;
            return;
        }

        Instance = this;
        if (transform.parent != null) transform.SetParent(null);    // DontDestroyOnLoad needs a root
        DontDestroyOnLoad(gameObject);

        BuildSources();

        Register(m_Ambient, m_SoundData.ambients);
        Register(m_Bgm, m_SoundData.bgms);
        Register(m_Sfx, m_SoundData.effects);
        Register(m_UiSfx, m_SoundData.uiSfx);
        Register(m_Voice, m_SoundData.voices);

        Debug.Log($"[SoundManager] Init — "
            + $"Ambient:{m_Ambient.Count} | BGM:{m_Bgm.Count} | "
            + $"SFX:{m_Sfx.Count} | UiSFX:{m_UiSfx.Count} | Voice:{m_Voice.Count}");
    }

    private void Start()
    {
        InitialiseVolumes();
        PlayBGM(m_BGMStart);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    private void BuildSources()
    {
        var s = m_SoundSettings;
        var music = Group(s.MusicGroup);
        var ambient = Group(s.AmbientGroup);
        var ui = Group(s.UIGroup);
        var voice = Group(s.VoiceGroup);
        m_SfxGroup = Group(s.SFXGroup);

        m_BgmChannel.A = NewSource(music, true);
        m_BgmChannel.B = NewSource(music, true);
        m_BgmChannel.Active = m_BgmChannel.A;

        m_AmbientChannel.A = NewSource(ambient, true);
        m_AmbientChannel.B = NewSource(ambient, true);
        m_AmbientChannel.Active = m_AmbientChannel.A;

        m_UiSfxSrc = NewSource(ui);
        m_VoiceSrc = NewSource(voice);

        m_SfxPool = new AudioSource[m_MaxSFXCount];
        for (int i = 0; i < m_SfxPool.Length; i++)
            m_SfxPool[i] = NewSource(m_SfxGroup);
    }

    private AudioMixerGroup Group(string path)
    {
        var groups = m_SoundSettings.AudioMixer.FindMatchingGroups(path);
        if (groups.Length > 0) return groups[0];

        Debug.LogWarning($"[SoundManager] ไม่พบ mixer group '{path}'", this);
        return null;
    }

    private AudioSource NewSource(AudioMixerGroup group, bool loop = false)
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.loop = loop;
        src.outputAudioMixerGroup = group;
        return src;
    }

    private static void Register(Dictionary<string, SoundEntry> dict, List<SoundEntry> list)
    {
        foreach (var e in list)
        {
            if (e.clip == null || string.IsNullOrEmpty(e.id)) continue;
            dict[e.id] = e;
        }
    }

    // Empty id = "nothing to play" (silent). Unknown id = warn once, so typos don't fail silently.
    private SoundEntry Find(Dictionary<string, SoundEntry> dict, SoundChannel channel, string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (dict.TryGetValue(id, out var e)) return e;

        if (m_Warned.Add($"{channel}:{id}"))
            Debug.LogWarning($"[SoundManager] ไม่พบ {channel} id '{id}' ใน SoundData", this);
        return null;
    }

    // ── Volume (mixer exposed params, sliders are 0-1) ───────────────────────

    private void InitialiseVolumes()
    {
        var s = m_SoundSettings;
        ApplyVolume(s.MasterVolumeName, Load(s.MasterVolumeName, s.MasterVolume), m_SliderMasterVolume);
        ApplyVolume(s.MusicVolumeName, Load(s.MusicVolumeName, s.MusicVolume), m_SliderMusicVolume);
        ApplyVolume(s.AmbientVolumeName, Load(s.AmbientVolumeName, s.AmbientVolume), m_SliderAmbientVolume);
        ApplyVolume(s.MasterSFXVolumeName, Load(s.MasterSFXVolumeName, s.MasterSFXVolume), m_SliderMasterSFXVolume);
        ApplyVolume(s.SFXVolumeName, Load(s.SFXVolumeName, s.SFXVolume), m_SliderSFXVolume);
        ApplyVolume(s.UIVolumeName, Load(s.UIVolumeName, s.UIVolume), m_SliderUIVolume);
        ApplyVolume(s.VoiceVolumeName, Load(s.VoiceVolumeName, s.VoiceVolume), m_SliderVoiceVolume);
    }

    public void SetMasterVolume(float vol) => SetVolume(m_SoundSettings.MasterVolumeName, vol, m_SliderMasterVolume);
    public void SetMusicVolume(float vol) => SetVolume(m_SoundSettings.MusicVolumeName, vol, m_SliderMusicVolume);
    public void SetAmbientVolume(float vol) => SetVolume(m_SoundSettings.AmbientVolumeName, vol, m_SliderAmbientVolume);
    public void SetMasterSFXVolume(float vol) => SetVolume(m_SoundSettings.MasterSFXVolumeName, vol, m_SliderMasterSFXVolume);
    public void SetSFXVolume(float vol) => SetVolume(m_SoundSettings.SFXVolumeName, vol, m_SliderSFXVolume);
    public void SetUIVolume(float vol) => SetVolume(m_SoundSettings.UIVolumeName, vol, m_SliderUIVolume);
    public void SetVoiceVolume(float vol) => SetVolume(m_SoundSettings.VoiceVolumeName, vol, m_SliderVoiceVolume);

    private void SetVolume(string param, float vol, Slider slider)
    {
        vol = Mathf.Clamp01(vol);
        ApplyVolume(param, vol, slider);
        PlayerPrefs.SetFloat(PrefKey(param), vol);
    }

    private void ApplyVolume(string param, float vol, Slider slider)
    {
        vol = Mathf.Clamp01(vol);
        if (!m_SoundSettings.AudioMixer.SetFloat(param, ToDecibel(vol)))
            Debug.LogWarning($"[SoundManager] mixer ไม่มี exposed param '{param}'", this);

        if (slider != null) slider.SetValueWithoutNotify(vol);
    }

    private static float ToDecibel(float vol) => vol <= 0.0001f ? -80f : Mathf.Log10(vol) * 20f;
    private static string PrefKey(string param) => "Sound." + param;
    private static float Load(string param, float fallback) => PlayerPrefs.GetFloat(PrefKey(param), fallback);

    /// <summary>Global mute (AudioListener) — independent of the mixer volumes.</summary>
    public void SetMute(bool mute) => AudioListener.volume = mute ? 0f : 1f;

    // ── Fade / Crossfade ──────────────────────────────────────────────────────

    // New entry fades in on the idle source while the old one fades out; null entry = just fade out.
    private void Crossfade(LoopChannel ch, SoundEntry next, float duration)
    {
        if (next == ch.Current) return;     // already playing it — don't restart
        ch.Current = next;

        var outgoing = ch.Active;
        if (next != null)
        {
            var incoming = outgoing == ch.A ? ch.B : ch.A;
            if (!incoming.isPlaying) incoming.volume = 0f;  // still fading out from an earlier call → continue from its volume
            incoming.clip = next.clip;
            incoming.Play();
            FadeTo(incoming, next.volume, duration, false);
            ch.Active = incoming;
        }

        if (outgoing.isPlaying) FadeTo(outgoing, 0f, duration, true);
    }

    private void FadeTo(AudioSource src, float target, float duration, bool stopAtEnd)
    {
        if (m_Fades.TryGetValue(src, out var running)) StopCoroutine(running);
        m_Fades[src] = StartCoroutine(DoFade(src, target, duration, stopAtEnd));
    }

    private IEnumerator DoFade(AudioSource src, float target, float duration, bool stopAtEnd)
    {
        float start = src.volume;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)  // unscaled: works while paused (timeScale 0)
        {
            src.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        src.volume = target;
        if (stopAtEnd) src.Stop();
        m_Fades.Remove(src);
    }

    // ── Ambient ───────────────────────────────────────────────────────────────

    public void PlayAmbient(string id, float fade = 1f)
    {
        var e = Find(m_Ambient, SoundChannel.Ambient, id);
        if (e != null) Crossfade(m_AmbientChannel, e, fade);
    }

    public void StopAmbient(float fade = 1f) => Crossfade(m_AmbientChannel, null, fade);

    // ── BGM ───────────────────────────────────────────────────────────────────

    public void PlayBGM(string id, float fade = 1f)
    {
        var e = Find(m_Bgm, SoundChannel.BGM, id);
        if (e != null) Crossfade(m_BgmChannel, e, fade);
    }

    public void StopBGM(float fade = 1f) => Crossfade(m_BgmChannel, null, fade);

    public void PauseBGM() { m_BgmChannel.A.Pause(); m_BgmChannel.B.Pause(); }
    public void ResumeBGM() { m_BgmChannel.A.UnPause(); m_BgmChannel.B.UnPause(); }

    /// <summary>Change BGM and ambient together. null / empty id fades that channel out.</summary>
    public void TransitionTo(string bgmId, string ambientId, float duration = 1f)
    {
        if (string.IsNullOrEmpty(bgmId)) StopBGM(duration); else PlayBGM(bgmId, duration);
        if (string.IsNullOrEmpty(ambientId)) StopAmbient(duration); else PlayAmbient(ambientId, duration);
    }

    // ── SFX — 2D one-shot, capped at MaxSFXCount voices ──────────────────────

    public void PlaySFX(string id)
    {
        var e = Find(m_Sfx, SoundChannel.SFX, id);
        if (e == null) return;

        // Find a free slot; if none, evict the oldest (ring buffer)
        AudioSource slot = null;
        foreach (var src in m_SfxPool)
        {
            if (!src.isPlaying) { slot = src; break; }
        }
        if (slot == null)
        {
            slot = m_SfxPool[m_SfxOldest];
            slot.Stop();
            m_SfxOldest = (m_SfxOldest + 1) % m_SfxPool.Length;
        }

        slot.PlayOneShot(e.clip, e.volume);
    }

    // ── Loop SFX — 2D looping, one source per id ─────────────────────────────

    public void LoopSFX(string id)
    {
        if (m_LoopingSfx.ContainsKey(id)) return;   // already playing
        var e = Find(m_Sfx, SoundChannel.SFX, id);
        if (e == null) return;

        var go = new GameObject($"[SFX_Loop]{id}");
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.clip = e.clip;
        src.volume = e.volume;
        src.loop = true;
        src.spatialBlend = 0f;
        src.outputAudioMixerGroup = m_SfxGroup;
        src.Play();

        m_LoopingSfx[id] = src;
    }

    public void StopLoopSFX(string id)
    {
        if (!m_LoopingSfx.TryGetValue(id, out var src)) return;
        Destroy(src.gameObject);
        m_LoopingSfx.Remove(id);
    }

    public void StopAllLoopSFX()
    {
        foreach (var src in m_LoopingSfx.Values) Destroy(src.gameObject);
        m_LoopingSfx.Clear();
    }

    // ── UI SFX — 2D one-shot (button clicks, menu sounds, etc.) ──────────────

    public void PlayUiSFX(string id)
    {
        var e = Find(m_UiSfx, SoundChannel.UiSFX, id);
        if (e != null) m_UiSfxSrc.PlayOneShot(e.clip, e.volume);
    }

    // ── Voice — 2D one-shot / interruptible ──────────────────────────────────

    public void PlayVoice(string id)
    {
        var e = Find(m_Voice, SoundChannel.Voice, id);
        if (e != null) m_VoiceSrc.PlayOneShot(e.clip, e.volume);
    }

    /// <summary>Interrupt the current voice line.</summary>
    public void StopVoice() => m_VoiceSrc.Stop();
}
