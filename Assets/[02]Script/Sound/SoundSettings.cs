using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Default volumes (0-1) + names of the exposed AudioMixer params / group paths used by SoundManager.
/// Never written at runtime — the live values are saved to PlayerPrefs by SoundManager.
/// </summary>
[CreateAssetMenu(
    menuName = "Sound/SoundSettings",
    fileName = "SoundSettings"
)]
public class SoundSettings : ScriptableObject
{
    public AudioMixer AudioMixer;

    [Header("Master Volume")]
    [Range(0f, 1f)]
    public float MasterVolume = 1f;

    public string MasterVolumeName = "MasterVolume";

    [Header("Music Volume")]
    [Range(0f, 1f)]
    public float MusicVolume = 0.8f;

    public string MusicVolumeName = "MusicVolume";
    public string MusicGroup = "Master/Music";

    [Header("Ambient Volume")]
    [Range(0f, 1f)]
    public float AmbientVolume = 0.5f;

    public string AmbientVolumeName = "AmbientVolume";
    public string AmbientGroup = "Master/Ambient";

    [Header("Master SFX Volume")]
    [Range(0f, 1f)]
    public float MasterSFXVolume = 1f;

    public string MasterSFXVolumeName = "MasterSFXVolume";

    [Header("SFX Volume")]
    [Range(0f, 1f)]
    public float SFXVolume = 1f;

    public string SFXVolumeName = "SFXVolume";
    public string SFXGroup = "Master/SFXMaster/SFX";

    [Header("UI Volume")]
    [Range(0f, 1f)]
    public float UIVolume = 1f;

    public string UIVolumeName = "UIVolume";
    public string UIGroup = "Master/SFXMaster/Ui";

    [Header("Voice Volume")]
    [Range(0f, 1f)]
    public float VoiceVolume = 1f;

    public string VoiceVolumeName = "VoiceVolume";
    public string VoiceGroup = "Master/Voice";
}
