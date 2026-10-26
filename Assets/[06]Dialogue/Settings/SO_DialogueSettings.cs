using UnityEngine;

/// <summary>
/// Designer defaults for dialogue presentation. Never written at runtime.
/// Read it only through <see cref="DialogueSettings"/>.
/// </summary>
[CreateAssetMenu(menuName = "Bar410/Dialogue Settings")]
public class SO_DialogueSettings : ScriptableObject
{
    // Single source of truth for dialogue presentation.
    // TODO: move text speed (Text Animator typewriter) and display type here.
    [Header("Auto Play")]
    [Min(0f)] public float autoPlayBaseDelay = 1f;      // seconds after the line finishes typing
    [Min(0f)] public float autoPlayPerCharDelay = 0.03f; // extra seconds per visible character

    public float AutoPlayDelay(int charCount) => autoPlayBaseDelay + autoPlayPerCharDelay * charCount;
}

/// <summary>The only door to dialogue settings. Set once by DialogueControls.Awake.</summary>
public static class DialogueSettings
{
    static SO_DialogueSettings _defaults;

    public static void Init(SO_DialogueSettings defaults) => _defaults = defaults;

    public static float AutoPlayDelay(int charCount) => _defaults != null ? _defaults.AutoPlayDelay(charCount) : 1f;

    // FUTURE (Settings menu): player overrides + PlayerPrefs + `event Action Changed` live here.
}
