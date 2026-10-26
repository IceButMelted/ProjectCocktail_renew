using System;
using UnityEngine;

public enum TextSpeed { Slow, Normal, Fast }
public enum TextDisplay { Character, Word, Instant }

/// <summary>
/// Designer defaults for dialogue presentation. Never written at runtime.
/// Read it only through <see cref="DialogueSettings"/>.
/// </summary>
[CreateAssetMenu(menuName = "Bar410/Dialogue Settings")]
public class SO_DialogueSettings : ScriptableObject
{
    // Single source of truth for dialogue presentation.
    [Header("Auto Play")]
    [Min(0f)] public float autoPlayBaseDelay = 1f;      // seconds after the line finishes typing
    [Min(0f)] public float autoPlayPerCharDelay = 0.03f; // extra seconds per visible character

    // Defaults the player can override from the Settings menu (stored in PlayerPrefs).
    [Header("Text")]
    public TextSpeed defaultSpeed = TextSpeed.Normal;
    public TextDisplay defaultDisplay = TextDisplay.Character;
    [Tooltip("Text Animator typewriter speed multiplier, indexed by TextSpeed (Slow, Normal, Fast).")]
    public float[] speedMultiplier = { 0.5f, 1f, 2f };

    public float AutoPlayDelay(int charCount) => autoPlayBaseDelay + autoPlayPerCharDelay * charCount;
}

/// <summary>The only door to dialogue settings. Set once by DialogueControls.Awake.</summary>
public static class DialogueSettings
{
    const string SpeedKey = "dlg.speed";
    const string DisplayKey = "dlg.display";

    static SO_DialogueSettings _defaults;

    /// <summary>Fired after the player changes Speed or Display.</summary>
    public static event Action Changed;

    public static void Init(SO_DialogueSettings defaults) => _defaults = defaults;

    public static float AutoPlayDelay(int charCount) => _defaults != null ? _defaults.AutoPlayDelay(charCount) : 1f;

    public static TextSpeed Speed =>
        (TextSpeed)ClampedPref(SpeedKey, _defaults != null ? (int)_defaults.defaultSpeed : (int)TextSpeed.Normal, Enum.GetValues(typeof(TextSpeed)).Length);

    public static TextDisplay Display =>
        (TextDisplay)ClampedPref(DisplayKey, _defaults != null ? (int)_defaults.defaultDisplay : (int)TextDisplay.Character, Enum.GetValues(typeof(TextDisplay)).Length);

    /// <summary>Text Animator typewriter speed multiplier for the current Speed.</summary>
    public static float SpeedMultiplier
    {
        get
        {
            var table = _defaults != null ? _defaults.speedMultiplier : null;
            int i = (int)Speed;
            return table != null && i < table.Length ? table[i] : 1f;
        }
    }

    public static void SetSpeed(TextSpeed value) => Save(SpeedKey, (int)value);
    public static void SetDisplay(TextDisplay value) => Save(DisplayKey, (int)value);

    static int ClampedPref(string key, int fallback, int count) => Mathf.Clamp(PlayerPrefs.GetInt(key, fallback), 0, count - 1);

    static void Save(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
