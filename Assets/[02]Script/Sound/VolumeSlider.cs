using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives one SoundManager volume from the Slider on this object. Pick the channel in the Inspector — no scene
/// references, so the same prefab works in MainMenu and in the additive Pause scene (SoundManager is DontDestroyOnLoad).
/// The slider needs Min 0 / Max 1 / Whole Numbers off. Without a SoundManager the slider is disabled.
/// </summary>
[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    [SerializeField] private VolumeChannel m_Channel;

    private Slider m_Slider;

    private void Awake() => m_Slider = GetComponent<Slider>();

    // OnEnable, not Start: the page is hidden and re-shown, and the value must be re-read from SoundManager each time.
    private void OnEnable()
    {
        var sm = SoundManager.Instance;
        m_Slider.interactable = sm != null;
        if (sm == null) return;

        m_Slider.SetValueWithoutNotify(sm.GetVolume(m_Channel));    // no notify: don't write PlayerPrefs just by opening the page
        m_Slider.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDisable() => m_Slider.onValueChanged.RemoveListener(OnValueChanged);

    private void OnValueChanged(float value) => SoundManager.Instance?.SetVolume(m_Channel, value);
}
