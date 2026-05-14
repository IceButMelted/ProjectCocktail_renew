using UnityEngine;
using System.Collections.Generic;

public static class ManagerSound
{
    public static float VolumeAmbient = 0.5f;
    public static float VolumeBGM = 0.8f;
    public static float VolumeEffect = 1.0f;

    private static readonly Dictionary<string, SoundEntry> _ambient = new();
    private static readonly Dictionary<string, SoundEntry> _bgm = new();
    private static readonly Dictionary<string, SoundEntry> _effect = new();
    private static readonly Dictionary<string, AudioSource> _loopingSfx = new();

    private static AudioSource _ambientSrc;
    private static AudioSource _bgmSrc;
    private static AudioSource _sfxSrc;     // 2D one-shot
    private static SoundRunner _runner;

    // ── Init ─────────────────────────────────────────────
    // เรียกครั้งเดียวตอนเริ่มเกม ส่ง SoundData เข้ามา
    public static void Init(SoundData data)
    {
        if (_runner != null) return;

        var go = new GameObject("[ManagerSound]");
        Object.DontDestroyOnLoad(go);

        _runner = go.AddComponent<SoundRunner>();
        _ambientSrc = go.AddComponent<AudioSource>();
        _bgmSrc = go.AddComponent<AudioSource>();
        _sfxSrc = go.AddComponent<AudioSource>();

        // 2D ทั้งหมด
        _ambientSrc.spatialBlend = 0f;
        _bgmSrc.spatialBlend = 0f;
        _sfxSrc.spatialBlend = 0f;

        _ambientSrc.loop = true;
        _bgmSrc.loop = true;

        RegisterList(_ambient, data.ambients);
        RegisterList(_bgm, data.bgms);
        RegisterList(_effect, data.effects);

        Debug.Log($"[ManagerSound] registered: "
            + $"{_ambient.Count} ambient, "
            + $"{_bgm.Count} bgm, "
            + $"{_effect.Count} effect");
    }

    static void RegisterList(Dictionary<string, SoundEntry> dict,
                              List<SoundEntry> list)
    {
        foreach (var e in list)
        {
            if (e.clip == null || string.IsNullOrEmpty(e.id)) continue;
            dict[e.id] = e;
        }
    }

    // ── Ambient ───────────────────────────────────────────
    public static void PlayAmbient(string id, float fade = 1f)
    {
        if (!_ambient.TryGetValue(id, out var e)) return;
        _runner.Fade(_ambientSrc, e.clip, e.volume * VolumeAmbient, fade);
    }

    public static void StopAmbient(float fade = 1f)
        => _runner.FadeOut(_ambientSrc, fade);

    // ── BGM ───────────────────────────────────────────────
    public static void PlayBGM(string id, float fade = 1f)
    {
        if (!_bgm.TryGetValue(id, out var e)) return;
        _runner.Fade(_bgmSrc, e.clip, e.volume * VolumeBGM, fade);
    }

    public static void StopBGM(float fade = 1f)
        => _runner.FadeOut(_bgmSrc, fade);

    public static void PauseBGM() => _bgmSrc?.Pause();
    public static void ResumeBGM() => _bgmSrc?.UnPause();

    // ── Effect (2D one-shot) ──────────────────────────────
    // PlayOneShot เล่นซ้อนกันได้ ไม่สร้าง GameObject
    public static void PlayEffect(string id)
    {
        if (_runner == null)
        {
            Debug.LogWarning("[ManagerSound] ยังไม่ได้ Init()");
            return;
        }
        if (!_effect.TryGetValue(id, out var e)) return;
        _sfxSrc.PlayOneShot(e.clip, e.volume * VolumeEffect);
    }

    // ── Loop Effect (2D) ──────────────────────────────────
    public static void LoopEffect(string id)
    {
        if (_runner == null)
        {
            Debug.LogWarning("[ManagerSound] ยังไม่ได้ Init()");
            return;
        }
        if (_loopingSfx.ContainsKey(id)) return;   // เล่นอยู่แล้ว ไม่เปิดซ้ำ
        if (!_effect.TryGetValue(id, out var e)) return;

        var go = new GameObject($"[SFX_Loop]{id}");
        Object.DontDestroyOnLoad(go);
        var src = go.AddComponent<AudioSource>();
        src.clip = e.clip;
        src.volume = e.volume * VolumeEffect;
        src.loop = true;
        src.spatialBlend = 0f;  // 2D
        src.Play();

        _loopingSfx[id] = src;
    }

    public static void StopLoopEffect(string id)
    {
        if (!_loopingSfx.TryGetValue(id, out var src)) return;
        Object.Destroy(src.gameObject);
        _loopingSfx.Remove(id);
    }

    public static void StopAllLoopEffect()
    {
        foreach (var src in _loopingSfx.Values)
            Object.Destroy(src.gameObject);
        _loopingSfx.Clear();
    }

    // ── Mute ──────────────────────────────────────────────
    public static void SetMute(bool mute)
        => AudioListener.volume = mute ? 0f : 1f;
}