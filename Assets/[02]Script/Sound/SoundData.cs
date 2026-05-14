using UnityEngine;
using System;
using System.Collections.Generic;

// ── SoundEntry — 1 รายการเสียง ────────────────────────
[Serializable]
public class SoundEntry
{
    public string id;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;  // volume เฉพาะของ clip นี้
}

// ── SoundData — ScriptableObject ─────────────────────
// Create → Sound → SoundData แล้วลาก clip ใน Inspector
[CreateAssetMenu(fileName = "SoundData",
                 menuName = "Sound/SoundData")]
public class SoundData : ScriptableObject
{
    [Header("Ambient (loop)")]
    public List<SoundEntry> ambients = new();

    [Header("BGM (loop)")]
    public List<SoundEntry> bgms = new();

    [Header("Effect (one-shot)")]
    public List<SoundEntry> effects = new();

    // ── Editor helper: ตรวจ id ซ้ำ ──────────────────────
#if UNITY_EDITOR
    void OnValidate()
    {
        CheckDuplicates(ambients, "Ambient");
        CheckDuplicates(bgms, "BGM");
        CheckDuplicates(effects, "Effect");
    }

    void CheckDuplicates(List<SoundEntry> list, string label)
    {
        var seen = new HashSet<string>();
        foreach (var e in list)
        {
            if (string.IsNullOrEmpty(e.id)) continue;
            if (!seen.Add(e.id))
                Debug.LogWarning($"[SoundData] {label} id '{e.id}' ซ้ำ!");
        }
    }
#endif
}