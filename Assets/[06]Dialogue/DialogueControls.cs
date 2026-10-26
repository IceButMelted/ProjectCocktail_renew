using System;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using YarnSpinner.Custom;

/// <summary>
/// Owns the dialogue HUD buttons: AutoPlay toggle and Skip-to-order (with its warning popup).
/// Presenters read the static state; they never talk to the buttons.
/// </summary>
public class DialogueControls : MonoBehaviour
{
    [SerializeField] SO_DialogueSettings settings;
    [SerializeField] DialogueRunner runner;

    [Header("AutoPlay")]
    [SerializeField] Button autoPlayButton;
    [SerializeField] Color autoPlayOffColor = Color.white;
    [SerializeField] Color autoPlayOnColor = new Color(1f, 0.82f, 0.35f, 1f);

    [Header("Skip")]
    [SerializeField] Button skipButton;
    [SerializeField] CanvasGroup skipWarning;
    [SerializeField] Button blocker;
    [SerializeField] Button cancelSkip;
    [SerializeField] Button confirmSkip;

    public static bool AutoPlay { get; private set; }
    public static bool IsSkipping { get; private set; }

    /// <summary>Id of the line the presenter is showing (own copy: SceneLoaderBridge.CurrentLineId is the save system's).</summary>
    public static string CurrentLineId { get; private set; }

    static event Action StateChanged;
    static DialogueRunner s_runner;

    /// <summary>Ends the line on screen (used by AutoPlay).</summary>
    public static void RequestNextLine() => s_runner?.RequestNextLine();

    void Awake()
    {
        s_runner = runner;
        AutoPlay = false;
        IsSkipping = false;
        CurrentLineId = null;
        DialogueSettings.Init(settings);
        SetWarningVisible(false);
        RefreshAutoPlayVisual();
        RefreshSkipButton();
    }

    void OnEnable()
    {
        autoPlayButton.onClick.AddListener(ToggleAutoPlay);
        skipButton.onClick.AddListener(OnSkipClicked);
        confirmSkip.onClick.AddListener(OnConfirmSkip);
        cancelSkip.onClick.AddListener(HideWarning);
        blocker.onClick.AddListener(HideWarning);
        StateChanged += RefreshSkipButton;
        runner.onDialogueComplete.AddListener(OnDialogueComplete);
    }

    void OnDisable()
    {
        autoPlayButton.onClick.RemoveListener(ToggleAutoPlay);
        skipButton.onClick.RemoveListener(OnSkipClicked);
        confirmSkip.onClick.RemoveListener(OnConfirmSkip);
        cancelSkip.onClick.RemoveListener(HideWarning);
        blocker.onClick.RemoveListener(HideWarning);
        StateChanged -= RefreshSkipButton;
        runner.onDialogueComplete.RemoveListener(OnDialogueComplete);
    }

    /// <summary>Called by the line presenter every time a line begins.</summary>
    public static void OnLineStarted(string lineId)
    {
        CurrentLineId = lineId;
        if (!IsSkipping) StateChanged?.Invoke();
    }

    /// <summary>Ends a skip (order line reached, order taken, or dialogue over).</summary>
    public static void StopSkip()
    {
        if (!IsSkipping) return;
        IsSkipping = false;
        StateChanged?.Invoke();
    }

    void ToggleAutoPlay()
    {
        AutoPlay = !AutoPlay;
        RefreshAutoPlayVisual();
    }

    void RefreshAutoPlayVisual()
    {
        if (autoPlayButton != null && autoPlayButton.image != null)
            autoPlayButton.image.color = AutoPlay ? autoPlayOnColor : autoPlayOffColor;
    }

    void OnSkipClicked()
    {
        if (!SkipScan.FindOrderAhead(runner, out bool hasOptions)) return;
        if (hasOptions) SetWarningVisible(true);
        else BeginSkip();
    }

    void OnConfirmSkip()
    {
        SetWarningVisible(false);
        BeginSkip();
    }

    void HideWarning() => SetWarningVisible(false);

    // Showing the panel enables its PausePanel, which freezes the game until it is hidden again.
    void SetWarningVisible(bool visible)
    {
        skipWarning.alpha = 1f;
        skipWarning.interactable = true;
        skipWarning.blocksRaycasts = true;
        skipWarning.gameObject.SetActive(visible);
    }

    void BeginSkip()
    {
        IsSkipping = true;
        StateChanged?.Invoke();
        runner.RequestNextLine(); // ends the line on screen
        FindFirstObjectByType<OptionPresenterCocktailProject>()?.ChooseFirstAvailable(); // options already open
    }

    void OnDialogueComplete()
    {
        StopSkip();
        skipButton.interactable = false;
    }

    void RefreshSkipButton()
    {
        skipButton.interactable = !IsSkipping && SkipScan.FindOrderAhead(runner, out _);
    }
}
