using UnityEngine;

public class SaveSettingData
{
    [Header("Sound Settings")]
    public float MasterVolume = 1;
    public float BGMVolume = 1;
    public float AmbientVolume = 1;
    public float SFXVolume = 1;
    public float UiSFXVolume = 1;
    public float VoiceVolume = 1;

    [Header("Text Settings")]
    public int TextSpeed;

    [Header("Graphic Settings")]
    public int qualityLevel = 2;      // Unity's QualitySettings index
    public bool fullscreen = true;
    public int resolutionIndex = 0;
}
