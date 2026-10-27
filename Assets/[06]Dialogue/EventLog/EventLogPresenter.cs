using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using YarnSpinner.Custom;

/// <summary>
/// Records every dialogue line and chosen option of the day. It never awaits in
/// RunLineAsync, so it does not slow the DialogueRunner down.
/// </summary>
public class EventLogPresenter : DialoguePresenterBase
{
    [SerializeField] OptionPresenterCocktailProject _options;   // source of OptionChosen
    [SerializeField] string _playerName = "Player";
    [SerializeField] string _silentText = "*Silent*";

    [Header("Name colours")]
    [SerializeField] SO_CustomerRoster _roster;
    [SerializeField] Color _playerNameColor = Color.white;
    [SerializeField] Color _defaultNameColor = Color.white;

    [Header("UI")]
    [SerializeField] GameObject _panel;              // Panel_EventLog
    [SerializeField] ScrollRect _scroll;
    [SerializeField] RectTransform _content;         // Scroll View/Viewport/Content
    [SerializeField] GameObject _rowWithName;        // prefab
    [SerializeField] GameObject _rowWithoutName;     // prefab
    [SerializeField] GameObject _rowChoice;          // prefab, TMP_Text only: the option the player picked
    [SerializeField] Button _btnToggle;              // BTN_EventLog
    [SerializeField] Button _btnOpen;                // BTN_OpenEventLog
    [SerializeField] Button _btnClose;               // BTN_CloseEventLog

    [SerializeField] Button _btnBlocker;             // full-screen click catcher behind the panel: swallows clicks and closes the log

    static readonly Regex Tags = new Regex("<[^>]*>");   // Text Animator tags such as <wave>

    readonly List<EventLogEntry> _entries = new List<EventLogEntry>();
    readonly List<GameObject> _rows = new List<GameObject>();
    string _lastSpeaker;   // null = next named line shows its name

    public IReadOnlyList<EventLogEntry> Entries => _entries;

    void Awake()
    {
        if (_panel != null) _panel.SetActive(false);
    }

    void OnEnable()
    {
        if (_btnToggle != null) _btnToggle.onClick.AddListener(TogglePanel);
        if (_btnBlocker != null) _btnBlocker.onClick.AddListener(TogglePanel);
        if (_btnOpen != null) _btnOpen.onClick.AddListener(TogglePanel);
        if (_btnClose != null) _btnClose.onClick.AddListener(TogglePanel);
        if (_options != null) _options.OptionChosen += OnOptionChosen;
    }

    void OnDisable()
    {
        if (_btnToggle != null) _btnToggle.onClick.RemoveListener(TogglePanel);
        if (_btnBlocker != null) _btnBlocker.onClick.RemoveListener(TogglePanel);
        if (_btnOpen != null) _btnOpen.onClick.RemoveListener(TogglePanel); 
        if (_btnClose != null) _btnClose.onClick.RemoveListener(TogglePanel);
        if (_options != null) _options.OptionChosen -= OnOptionChosen;
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        _lastSpeaker = null;
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync() => YarnTask.CompletedTask;

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        Add(line.CharacterName ?? "", line.TextWithoutCharacterName.Text);
        return YarnTask.CompletedTask;
    }

    void OnOptionChosen(DialogueOption option, bool timedOut)
    {
        Add(_playerName, timedOut ? _silentText : option.Line.TextWithoutCharacterName.Text, true);
    }

    void Add(string speaker, string text, bool isChoice = false)
    {
        // A loaded save replays lines it already restored.
        if (SceneLoaderBridge.IsSilentReplay) return;

        bool showName = !isChoice && !string.IsNullOrEmpty(speaker) && speaker != _lastSpeaker;
        _lastSpeaker = isChoice ? null : speaker;   // a choice row has no name, so the next named line shows its own

        var entry = new EventLogEntry { Speaker = speaker, Text = Tags.Replace(text, ""), IsChoice = isChoice, ShowName = showName };
        _entries.Add(entry);
        Render(entry);
    }

    void Render(EventLogEntry entry)
    {
        if (_content == null) return;

        var template = entry.IsChoice ? _rowChoice : entry.ShowName ? _rowWithName : _rowWithoutName;
        var row = Instantiate(template, _content);
        if (entry.ShowName)
        {
            var nameText = row.transform.Find("TMP_Name").GetComponent<TMP_Text>();
            nameText.text = entry.Speaker;
            nameText.color = NameColor(entry.Speaker);
        }
        row.transform.Find("TMP_Text").GetComponent<TMP_Text>().text = entry.Text;
        _rows.Add(row);
    }

    Color NameColor(string speaker)
    {
        if (speaker == _playerName) return _playerNameColor;
        return _roster != null && _roster.TryGetNameColor(speaker, out var color) ? color : _defaultNameColor;
    }

    void TogglePanel()
    {
        _panel.SetActive(!_panel.activeSelf);
        if (!_panel.activeSelf) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        _scroll.verticalNormalizedPosition = 0f;
    }

    [YarnCommand("eventlog_clear")]
    public static void Yarn_Clear()
    {
        if (!SceneLoaderBridge.IsSilentReplay) FindFirstObjectByType<EventLogPresenter>()?.Clear();
    }

    public List<EventLogEntry> Snapshot() => new List<EventLogEntry>(_entries);

    public void Restore(IEnumerable<EventLogEntry> entries)
    {
        Clear();
        foreach (var entry in entries)
        {
            _entries.Add(entry);
            Render(entry);
        }
        _lastSpeaker = null;
    }

    public void Clear()
    {
        _entries.Clear();
        foreach (var row in _rows) Destroy(row);
        _rows.Clear();
        _lastSpeaker = null;
    }
}
