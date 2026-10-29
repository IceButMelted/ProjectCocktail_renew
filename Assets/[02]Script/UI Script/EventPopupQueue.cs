using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The day's event popups (BeforeOpen › Event). Anything can <see cref="Enqueue"/> one; FlowWiring
/// calls <see cref="ShowAll"/> when Event is entered and finishes Event on <see cref="AllClosed"/>.
/// One popup at a time, the player closes each. Fades in on the first and out after the last;
/// AllClosed is raised once the fade-out is over, so Event (and the dialogue waiting on it) goes on
/// only when the panel is gone. Placeholder look; restyle freely.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class EventPopupQueue : MonoBehaviour
{
    private struct Popup
    {
        public string Title;
        public string Body;
        public IReadOnlyList<Sprite> Images;
    }

    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _body;
    [Tooltip("The first image slot. A popup with more images gets copies of it under the same parent, so give " +
             "the parent a Horizontal Layout Group. Hidden when a popup has no image.")]
    [SerializeField] private Image _image;
    [SerializeField] private Button _close;
    [Tooltip("UiSFX Sound ID (SoundManager) played each time a popup shows. Empty = silent.")]
    [SerializeField] private string _showSoundId;
    [Tooltip("Seconds to fade in the first popup and fade out after the last (unscaled time). 0 = instant.")]
    [SerializeField, Min(0f)] private float _fadeSeconds = 0.25f;

    /// <summary>The last queued popup was closed and the panel has faded out.</summary>
    public event Action AllClosed;

    private readonly Queue<Popup> _queue = new Queue<Popup>();
    private CanvasGroup _group;
    private bool _showing;
    private readonly List<Image> _extraImages = new List<Image>();

    public bool HasPending => _queue.Count > 0;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        SetInteractive(false);
        _group.alpha = 0f;
        if (_close != null) _close.onClick.AddListener(Next);
    }

    private void OnDestroy()
    {
        if (_close != null) _close.onClick.RemoveListener(Next);
        _group.DOKill();
    }

    public void Enqueue(string title, string body, Sprite image = null)
        => Enqueue(title, body, image != null ? new[] { image } : null);

    /// <summary>A popup with several images side by side (e.g. one icon per unlocked ingredient).</summary>
    public void Enqueue(string title, string body, IReadOnlyList<Sprite> images)
        => _queue.Enqueue(new Popup { Title = title, Body = body, Images = images });

    /// <summary>Shows the queued popups one by one. Raises <see cref="AllClosed"/> at once when none are queued.</summary>
    public void ShowAll()
    {
        if (_showing) return;
        _showing = true;
        Next();
    }

    private void Next()
    {
        if (!_showing) return;

        if (_queue.Count == 0)
        {
            _showing = false;
            SetInteractive(false);
            // Nothing was shown (alpha 0): finish at once. Otherwise finish when the fade-out is over.
            _group.DOKill();
            if (_group.alpha <= 0f || _fadeSeconds <= 0f) { _group.alpha = 0f; AllClosed?.Invoke(); }
            else _group.DOFade(0f, _fadeSeconds).SetUpdate(true).OnComplete(() => AllClosed?.Invoke());
            return;
        }

        var popup = _queue.Dequeue();
        if (_title != null) _title.text = popup.Title;
        if (_body != null) _body.text = popup.Body;
        ShowImages(popup.Images);

        SetInteractive(true);
        _group.DOKill();
        if (_fadeSeconds > 0f) _group.DOFade(1f, _fadeSeconds).SetUpdate(true);
        else _group.alpha = 1f;
        if (!string.IsNullOrEmpty(_showSoundId)) SoundManager.Instance?.PlayUiSFX(_showSoundId);
        // Controller: the close button takes focus so Submit closes the popup.
        if (_close != null) EventSystem.current?.SetSelectedGameObject(_close.gameObject);
    }

    /// <summary>One image slot per sprite: <see cref="_image"/> first, then copies of it; unused slots hidden.</summary>
    private void ShowImages(IReadOnlyList<Sprite> sprites)
    {
        if (_image == null) return;

        int count = sprites?.Count ?? 0;
        while (_extraImages.Count < count - 1)
            _extraImages.Add(Instantiate(_image, _image.transform.parent));

        for (int i = 0; i <= _extraImages.Count; i++)
        {
            var slot = i == 0 ? _image : _extraImages[i - 1];
            slot.gameObject.SetActive(i < count);
            if (i < count) slot.sprite = sprites[i];
        }
    }

    private void SetInteractive(bool on)
    {
        _group.blocksRaycasts = on;
        _group.interactable = on;
    }

    [ContextMenu("Test: queue two popups")]
    private void QueueTestPopups()
    {
        Enqueue("New ingredient", "Lemon Juice is now on the bar.");
        Enqueue("Event", "A second popup.");
    }
}
