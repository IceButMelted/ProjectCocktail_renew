using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// The chapter / day title shown when the bar opens: fades in, holds, fades out. Placeholder look;
/// restyle freely, FlowWiring only calls <see cref="Show"/>. Never blocks clicks.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ChapterTitleUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    [SerializeField, Min(0f)] private float _fadeIn = 0.5f;
    [SerializeField, Min(0f)] private float _hold = 1.5f;
    [SerializeField, Min(0f)] private float _fadeOut = 0.5f;

    private CanvasGroup _group;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        _group.interactable = false;
    }

    private void OnDestroy() => _group.DOKill();

    /// <summary>True from <see cref="Show"/> until the title has faded out.</summary>
    public bool IsPlaying => _group != null && DOTween.IsTweening(_group);

    public void Show(string text)
    {
        if (_label != null) _label.text = text;

        _group.DOKill();
        _group.alpha = 0f;
        DOTween.Sequence().SetTarget(_group)
            .Append(_group.DOFade(1f, _fadeIn))
            .AppendInterval(_hold)
            .Append(_group.DOFade(0f, _fadeOut));
    }
}
