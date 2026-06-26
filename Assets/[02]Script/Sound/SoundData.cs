using UnityEngine;
using System;
using System.Collections.Generic;

// ── SoundEntry — 1 sound entry ────────────────────────────────────────────────
[Serializable]
public class SoundEntry
{
    public string id;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;   // Per-clip volume (multiplied with channel + master)
}

// ── SoundData — ScriptableObject ─────────────────────────────────────────────
// Create → Sound → SoundData  then drag clips in Inspector
[CreateAssetMenu(fileName = "SoundData", menuName = "Sound/SoundData")]
public class SoundData : ScriptableObject
{
    [Header("Ambient  (loop)")]
    public List<SoundEntry> ambients = new();

    [Header("BGM  (loop)")]
    public List<SoundEntry> bgms = new();

    [Header("SFX  (one-shot / loop)")]
    public List<SoundEntry> effects = new();

    [Header("UI SFX  (one-shot)")]
    public List<SoundEntry> uiSfx = new();

    [Header("Voice  (one-shot)")]
    public List<SoundEntry> voices = new();

    // ── Editor: warn on duplicate ids ────────────────────────────────────────
#if UNITY_EDITOR
    void OnValidate()
    {
        CheckDuplicates(ambients, "Ambient");
        CheckDuplicates(bgms, "BGM");
        CheckDuplicates(effects, "SFX");
        CheckDuplicates(uiSfx, "UiSFX");
        CheckDuplicates(voices, "Voice");
    }

    void CheckDuplicates(List<SoundEntry> list, string label)
    {
        var seen = new HashSet<string>();
        foreach (var e in list)
        {
            if (string.IsNullOrEmpty(e.id)) continue;
            if (!seen.Add(e.id))
                Debug.LogWarning($"[SoundData] {label} id '{e.id}' Dup!");
        }
    }
#endif
}