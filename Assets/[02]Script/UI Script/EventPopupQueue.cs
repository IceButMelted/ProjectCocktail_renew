using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The day's event popups (BeforeOpen › Event). Anything can <see cref="Enqueue"/> one; FlowWiring
/// calls <see cref="ShowAll"/> when Event is entered and finishes Event on <see cref="AllClosed"/>.
/// One popup at a time, the player closes each. Placeholder look; restyle freely.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class EventPopupQueue : MonoBehaviour
{
    private struct Popup
    {
        public string Title;
        public string Body;
        public Sprite Image;
    }

    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _body;
    [Tooltip("Hidden when a popup has no image.")]
    [SerializeField] private Image _image;
    [SerializeField] private Button _close;

    /// <summary>The last queued popup was closed.</summary>
    public event Action AllClosed;

    private readonly Queue<Popup> _queue = new Queue<Popup>();
    private CanvasGroup _group;
    private bool _showing;

    public bool HasPending => _queue.Count > 0;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        SetVisible(false);
        if (_close != null) _close.onClick.AddListener(Next);
    }

    private void OnDestroy()
    {
        if (_close != null) _close.onClick.RemoveListener(Next);
    }

    public void Enqueue(string title, string body, Sprite image = null)
        => _queue.Enqueue(new Popup { Title = title, Body = body, Image = image });

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
            SetVisible(false);
            AllClosed?.Invoke();
            return;
        }

        var popup = _queue.Dequeue();
        if (_title != null) _title.text = popup.Title;
        if (_body != null) _body.text = popup.Body;
        if (_image != null)
        {
            _image.sprite = popup.Image;
            _image.gameObject.SetActive(popup.Image != null);
        }

        SetVisible(true);
        // Controller: the close button takes focus so Submit closes the popup.
        if (_close != null) EventSystem.current?.SetSelectedGameObject(_close.gameObject);
    }

    private void SetVisible(bool visible)
    {
        _group.alpha = visible ? 1f : 0f;
        _group.blocksRaycasts = visible;
        _group.interactable = visible;
    }

    [ContextMenu("Test: queue two popups")]
    private void QueueTestPopups()
    {
        Enqueue("New ingredient", "Lemon Juice is now on the bar.");
        Enqueue("Event", "A second popup.");
    }
}
