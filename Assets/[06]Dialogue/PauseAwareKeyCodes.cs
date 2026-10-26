using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Like Yarn's LineAdvancerInput.KeyCodes, but ignores input while <see cref="PauseManager.IsPaused"/>
/// (Update keeps running at timeScale 0). Hurry up and next line each take a list of keys, and a press
/// of either starts <see cref="inputCooldown"/> so fast presses cannot skip lines.
/// Use with LineAdvancer Input Mode = External.
/// </summary>
[RequireComponent(typeof(LineAdvancer))]
public class PauseAwareKeyCodes : MonoBehaviour, ILineAdvancerInput
{
    [SerializeField] LineAdvancer lineAdvancer;

    public LineAdvancer LineAdvancer { get => lineAdvancer; set => lineAdvancer = value; }

    public List<KeyCode> hurryUpLineKeyCodes = new List<KeyCode>() { KeyCode.Space };
    public List<KeyCode> nextLineKeyCodes = new List<KeyCode>() { KeyCode.Return, KeyCode.KeypadEnter };

    [Tooltip("Seconds after a hurry-up or next-line press before another one counts. Unscaled time.")]
    [Min(0f)] public float inputCooldown = 0.25f;

    public KeyCode hurryUpOptionsKeyCode = KeyCode.Space;
    public KeyCode cancelDialogueKeyCode = KeyCode.None;

    float _nextInputTime;

    void Update()
    {
        if (lineAdvancer == null || PauseManager.IsPaused) return;

        if (InputSystemAvailability.GetKeyDown(hurryUpOptionsKeyCode)) lineAdvancer.OnInputHurryUpOptions();
        if (InputSystemAvailability.GetKeyDown(cancelDialogueKeyCode)) lineAdvancer.OnInputCancelDialogue();

        if (Time.unscaledTime < _nextInputTime) return;

        // One line action per frame, so a key in both lists does not fire twice.
        if (AnyDown(hurryUpLineKeyCodes)) lineAdvancer.OnInputHurryUpLines();
        else if (AnyDown(nextLineKeyCodes)) lineAdvancer.OnInputNextContent();
        else return;

        _nextInputTime = Time.unscaledTime + inputCooldown;
    }

    static bool AnyDown(List<KeyCode> keys)
    {
        foreach (var keyCode in keys)
            if (InputSystemAvailability.GetKeyDown(keyCode)) return true;
        return false;
    }

    public void OnDialogueStarted() { }

    public void OnDialogueComplete() { }
}
