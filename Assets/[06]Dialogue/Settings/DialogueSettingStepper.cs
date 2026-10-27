using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One "◀ value ▶" row in a Settings page. Reads and writes only through <see cref="DialogueSettings"/>.
/// Holds no scene references, so the same row works in any scene.
/// </summary>
public class DialogueSettingStepper : MonoBehaviour
{
    public enum Target { Speed, Display }

    [SerializeField] Target target;
    [SerializeField] Button previous;
    [SerializeField] Button next;
    [SerializeField] TMP_Text valueLabel;
    [Tooltip("Text shown for each value, in enum order (Speed: Slow, Normal, Fast. Display: Character, Word, Instant).")]
    [SerializeField] string[] labels;

    Type EnumType => target == Target.Speed ? typeof(TextSpeed) : typeof(TextDisplay);
    int Count => Enum.GetValues(EnumType).Length;
    int Current => target == Target.Speed ? (int)DialogueSettings.Speed : (int)DialogueSettings.Display;

    void OnEnable()
    {
        Refresh();
        if (previous != null) previous.onClick.AddListener(StepBack);
        if (next != null) next.onClick.AddListener(StepForward);
    }

    void OnDisable()
    {
        if (previous != null) previous.onClick.RemoveListener(StepBack);
        if (next != null) next.onClick.RemoveListener(StepForward);
    }

    void StepBack() => Step(-1);
    void StepForward() => Step(1);

    void Step(int dir)
    {
        int index = (Current + dir + Count) % Count; // wraps: Fast + ▶ → Slow
        if (target == Target.Speed) DialogueSettings.SetSpeed((TextSpeed)index);
        else DialogueSettings.SetDisplay((TextDisplay)index);
        Refresh();
    }

    void Refresh()
    {
        if (valueLabel == null) return;
        int i = Current;
        valueLabel.text = labels != null && i < labels.Length && !string.IsNullOrEmpty(labels[i])
            ? labels[i]
            : Enum.GetName(EnumType, i);
    }
}
