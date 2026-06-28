/*
 * OptionPresenterType2.cs
 * Based on OptionsPresenter.cs from YarnSpinner-Unity (current branch)
 * https://github.com/YarnSpinnerTool/YarnSpinner-Unity.git#current
 *
 * Timeout support based on:
 * https://docs.yarnspinner.dev/yarn-spinner-for-unity/samples/make-options-timeout
 *
 * Features:
 *   - Single CanvasGroup ref (optionPresenterCanvasGroup) for show/hide — no Transform anchors.
 *   - Fade In / Fade Out (adjustable durations).
 *   - Tick box to show unavailable options (greyed out).
 *   - Last-line display (character name + body).
 *   - Timeout: ONLY activates when ALL options end with "?" AND one is tagged #fallback.
 *     The "?" is stripped from the displayed button label automatically.
 *
 * Yarn script usage (timeout ON — every option ends with "?"):
 *   -> Option A?
 *   -> Option B?
 *   -> This is chosen automatically? #fallback
 *
 * Yarn script usage (timeout OFF — no "?" suffix):
 *   -> Option A
 *   -> Option B
 *   -> This will NOT auto-select even though it has #fallback
 */

using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Unity;

namespace YarnSpinner.Custom
{
    public class OptionPresenterType2 : DialoguePresenterBase
    {
        // ── Option Item ───────────────────────────────────────────────────────

        [Header("Option Item Prefab")]
        [Tooltip("Prefab with OptionType2Item + Button + TMP_Text attached.")]
        [SerializeField] private OptionType2Item optionItemPrefab;

        // ── Canvas Group ──────────────────────────────────────────────────────

        [Header("Options Canvas Group")]
        [Tooltip("CanvasGroup wrapping the entire options UI. " +
                 "Option items are spawned as children of this transform.")]
        [SerializeField] private CanvasGroup optionPresenterCanvasGroup;

        // ── Fade ──────────────────────────────────────────────────────────────

        [Header("Fade Settings")]
        [Tooltip("Enable fade-in / fade-out transitions.")]
        [SerializeField] private bool useFadeEffect = true;

        [Tooltip("Seconds for fade-IN when options appear.")]
        [SerializeField] private float fadeInDuration = 0.25f;

        [Tooltip("Seconds for fade-OUT after an option is selected.")]
        [SerializeField] private float fadeOutDuration = 0.15f;

        // ── Unavailable Options ───────────────────────────────────────────────

        [Header("Unavailable Options")]
        [Tooltip("Show options marked as unavailable (IsAvailable == false) " +
                 "greyed-out and non-interactable.")]
        [SerializeField] private bool showUnavailableOptions = false;

        // ── Timeout ───────────────────────────────────────────────────────────

        [Header("Timeout")]
        [Tooltip("TimeoutBar component that shows the countdown. " +
                 "Assign the GameObject that holds the TimeoutBar script. " +
                 "Leave empty to disable the bar visual (timeout still works).")]
        [SerializeField] private TimeoutBar timedBar;

        [Tooltip("Seconds the player has to pick an option before the #fallback is chosen.\n" +
                 "Only active when options end with '?' AND one option is tagged #fallback.")]
        [SerializeField] public float autoSelectDuration = 10f;

        // ── Last Line ─────────────────────────────────────────────────────────

        [Header("Last Line Display")]
        [Tooltip("Show the last spoken line while the player chooses.")]
        [SerializeField] private bool showsLastLine = true;
        

        [SerializeField] private TMP_Text lastLineText;
        [SerializeField] private GameObject lastLineContainer;
        [SerializeField] private TMP_Text lastLineCharacterNameText;
        [SerializeField] private GameObject lastLineCharacterNameContainer;

        [Header("Target")]
        [SerializeField] private CanvasGroup bubblePresneter;
        // ── Constants ─────────────────────────────────────────────────────────

        /// <summary>Yarn metadata tag that marks the hidden fallback option.</summary>
        private const string FallbackMetadataTag = "fallback";

        // ── Internal Timeout Type ─────────────────────────────────────────────

        private enum TimeoutOptionType { None, HiddenFallback }

        // ── State ─────────────────────────────────────────────────────────────

        private readonly List<OptionType2Item> _pool = new List<OptionType2Item>();
        private string _lastLineBody = string.Empty;
        private string _lastLineCharacterName = string.Empty;

        // ── Canvas Group Helpers ──────────────────────────────────────────────

        private void ResetCanvasGroup()
        {
            if (optionPresenterCanvasGroup == null) return;
            optionPresenterCanvasGroup.alpha = 0f;
            optionPresenterCanvasGroup.interactable = false;
            optionPresenterCanvasGroup.blocksRaycasts = false;
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            ResetCanvasGroup();
            HideLastLine();
            if (timedBar != null) timedBar.gameObject.SetActive(false);
        }

        // ── DialoguePresenterBase ─────────────────────────────────────────────

        public override YarnTask OnDialogueStartedAsync()
        {
            ResetCanvasGroup();
            HideLastLine();
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            _lastLineBody = string.Empty;          
            _lastLineCharacterName = string.Empty;

            DeactivateAllOptions();
            ResetCanvasGroup();
            HideLastLine();
            if (timedBar != null) timedBar.gameObject.SetActive(false);
            return YarnTask.CompletedTask;
        }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            _lastLineCharacterName = line.CharacterName ?? string.Empty;
            
            _lastLineBody = line.TextWithoutCharacterName.Text ?? string.Empty;

            return YarnTask.CompletedTask;
        }

        [System.Obsolete]
        public override async YarnTask<DialogueOption> RunOptionsAsync(
                                    DialogueOption[] dialogueOptions,
                                    CancellationToken cancellationToken)
        {
            // ── SAVE/LOAD: silent replay — auto-select without showing UI 
            // Must be the very first thing in the method, before any guard checks.
            if (SceneLoaderBridge.IsSilentReplay)
            {
                // Dequeue the saved choice for this option block
                if (SceneLoaderBridge.ReplayOptionQueue.Count > 0)
                {
                    int savedId = SceneLoaderBridge.ReplayOptionQueue.Dequeue();
                    foreach (var opt in dialogueOptions)
                        if (opt.DialogueOptionID == savedId && opt.IsAvailable)
                            return opt;
                }
                // Fallback: first available option (saves from older versions without choice data)
                foreach (var opt in dialogueOptions)
                    if (opt.IsAvailable) return opt;
                return await DialogueRunner.NoOptionSelected;
            }
            // ── END SAVE/LOAD

            // ── Guards 
            if (optionItemPrefab == null)
            {
                Debug.LogWarning("[OptionPresenterType2] optionItemPrefab is not assigned.");
                return await DialogueRunner.NoOptionSelected;
            }
            if (optionPresenterCanvasGroup == null)
            {
                Debug.LogWarning("[OptionPresenterType2] optionPresenterCanvasGroup is not assigned.");
                return await DialogueRunner.NoOptionSelected;
            }

            // ── Gate 1: Check if this option set uses the "?" timeout convention ──
            // Timeout only activates when at least one option text ends with "?".
            // Options WITHOUT "?" never trigger timeout, even if #fallback is present.
            bool hasTimeoutMarker = false;
            foreach (var opt in dialogueOptions)
            {
                string raw = opt.Line.TextWithoutCharacterName.Text ?? string.Empty;
                if (raw.TrimEnd().EndsWith("?"))
                {
                    hasTimeoutMarker = true;
                    break;
                }
            }

            // ── Gate 2: Scan for #fallback metadata ───────────────────────────
            TimeoutOptionType timeoutType = TimeoutOptionType.None;
            DialogueOption fallbackOpt = null;
            int fallbackCount = 0;

            foreach (var opt in dialogueOptions)
            {
                foreach (var meta in opt.Line.Metadata)
                {
                    if (meta != FallbackMetadataTag) continue;

                    // Unavailable fallbacks are ignored
                    if (!opt.IsAvailable) continue;

                    fallbackOpt = opt;
                    fallbackCount++;
                    break;
                }
            }

            // Timeout requires BOTH: "?" marker on options AND a valid #fallback
            if (hasTimeoutMarker && fallbackOpt != null)
            {
                if (fallbackCount > 1)
                {
                    Debug.LogError("[OptionPresenterType2] More than one option is tagged #fallback. Only one is allowed.");
                    return await DialogueRunner.NoOptionSelected;
                }
                timeoutType = TimeoutOptionType.HiddenFallback;
            }
            else if (!hasTimeoutMarker && fallbackOpt != null)
            {
                // #fallback exists but no "?" — treat as a normal visible option, no timeout.
                Debug.Log("[OptionPresenterType2] #fallback found but no '?' suffix on options — timeout disabled. " +
                          "Fallback will be shown as a normal button.");
                fallbackOpt = null; // don't hide it from the player
            }

            // ── Check at least one visible option exists ───────────────────────
            bool anyVisible = false;
            foreach (var opt in dialogueOptions)
            {
                if (!opt.IsAvailable) continue;
                // Hidden fallback is represented by the bar, not a button
                if (timeoutType == TimeoutOptionType.HiddenFallback &&
                    fallbackOpt != null &&
                    opt.DialogueOptionID == fallbackOpt.DialogueOptionID) continue;
                anyVisible = true;
                break;
            }
            if (!anyVisible)
            {
                // Only invisible/fallback options — auto-select the fallback if present
                if (fallbackOpt != null) return fallbackOpt;
                return await DialogueRunner.NoOptionSelected;
            }

            // ── Last line ─────────────────────────────────────────────────────
            if (showsLastLine && _lastLineCharacterName.ToLower() != "Player".ToLower())
                ShowLastLine(_lastLineCharacterName, _lastLineBody);
            else
                HideLastLine();

            // ── Pool management ───────────────────────────────────────────────
            while (dialogueOptions.Length > _pool.Count)
                _pool.Add(CreatePooledItem());

            // ── Completion + cancellation sources ─────────────────────────────
            var selectionSource = new YarnTaskCompletionSource<DialogueOption>();
            var completionCancelSrc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Monitor outer cancellation: if the runner cancels us, resolve with null
            async YarnTask WatchExternalCancel()
            {
                await YarnTask.WaitUntilCanceled(completionCancelSrc.Token);
                if (cancellationToken.IsCancellationRequested)
                    selectionSource.TrySetResult(null);
            }
            WatchExternalCancel().Forget();

            // ── Configure option items ────────────────────────────────────────
            // Pass hasTimeoutMarker so items know to strip the trailing "?" from their label.
            int activeCount = 0;
            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                var opt = dialogueOptions[i];

                // Skip unavailable (unless showUnavailableOptions)
                if (!opt.IsAvailable && !showUnavailableOptions) continue;

                // Skip the hidden fallback — it's represented by the bar, not a button
                if (timeoutType == TimeoutOptionType.HiddenFallback &&
                    fallbackOpt != null &&
                    opt.DialogueOptionID == fallbackOpt.DialogueOptionID) continue;

                var item = _pool[activeCount];
                item.Configure(opt, showUnavailableOptions, selectionSource,
                               completionCancelSrc.Token, stripTrailingQuestionMark: hasTimeoutMarker);
                item.gameObject.SetActive(true);
                activeCount++;
            }

            // Deactivate unused pool slots
            for (int i = activeCount; i < _pool.Count; i++)
                _pool[i].gameObject.SetActive(false);

            // ── Timeout bar ───────────────────────────────────────────────────
            if (timedBar != null)
            {
                bool needsBar = (timeoutType != TimeoutOptionType.None);
                timedBar.gameObject.SetActive(needsBar);
                if (needsBar)
                {
                    //timedBar.ResetBar();
                    timedBar.gameObject.SetActive(true);
                    timedBar.transform.parent?.SetAsLastSibling();
                }
            }

            // ── Fade IN ───────────────────────────────────────────────────────
            optionPresenterCanvasGroup.interactable = false;
            optionPresenterCanvasGroup.blocksRaycasts = false;

            if (useFadeEffect && fadeInDuration > 0f)
                await Effects.FadeAlphaAsync(optionPresenterCanvasGroup, 0f, 1f, fadeInDuration, cancellationToken);
            else
                optionPresenterCanvasGroup.alpha = 1f;

            optionPresenterCanvasGroup.interactable = true;
            optionPresenterCanvasGroup.blocksRaycasts = true;

            // ── Kick off timeout bar shrink ───────────────────────────────────
            if (timeoutType == TimeoutOptionType.HiddenFallback && fallbackOpt != null)
                RunTimeout(selectionSource, fallbackOpt, completionCancelSrc.Token).Forget();

            // ── Wait for selection ────────────────────────────────────────────
            var selected = await selectionSource.Task;

            // ── SAVE/LOAD: record the choice for potential save
            // Stored in SessionOptionChoices so SaveLoadManager can snapshot it on save.
            // SessionOptionChoices is cleared automatically on every node start.
            if (selected != null)
                SceneLoaderBridge.SessionOptionChoices.Add(selected.DialogueOptionID);
            // ── END SAVE/LOAD 

            // ── Clean up 
            completionCancelSrc.Cancel();

            optionPresenterCanvasGroup.interactable = false;
            optionPresenterCanvasGroup.blocksRaycasts = false;

            if (useFadeEffect && fadeOutDuration > 0f)
                await Effects.FadeAlphaAsync(optionPresenterCanvasGroup, 1f, 0f, fadeOutDuration, cancellationToken);
            else
                optionPresenterCanvasGroup.alpha = 0f;

            DeactivateAllOptions();
            HideLastLine();
            if (timedBar != null) timedBar.gameObject.SetActive(false);

            // If outer dialogue was cancelled, signal no selection
            if (cancellationToken.IsCancellationRequested)
                return await DialogueRunner.NoOptionSelected;

            return selected;
        }

        // ── Timeout Runner ────────────────────────────────────────────────────

        private async YarnTask RunTimeout(
            YarnTaskCompletionSource<DialogueOption> source,
            DialogueOption fallback,
            CancellationToken cancellationToken)
        {
            if (timedBar != null)
                await timedBar.Shrink(autoSelectDuration, cancellationToken);
            else
            {
                // No bar — just wait manually
                float elapsed = 0f;
                while (elapsed < autoSelectDuration && !cancellationToken.IsCancellationRequested)
                {
                    elapsed += Time.deltaTime;
                    await YarnTask.Yield();
                }
            }

            if (!cancellationToken.IsCancellationRequested)
                source.TrySetResult(fallback);
        }

        // ── Pool Helpers ──────────────────────────────────────────────────────

        private OptionType2Item CreatePooledItem()
        {
            var item = Instantiate(optionItemPrefab, optionPresenterCanvasGroup.transform);
            item.gameObject.SetActive(false);
            return item;
        }

        private void DeactivateAllOptions()
        {
            foreach (var item in _pool)
                if (item != null) item.gameObject.SetActive(false);
        }

        // ── Last Line Helpers ─────────────────────────────────────────────────

        private void ShowLastLine(string characterName, string body)
        {
            if (lastLineText != null) lastLineText.text = body;
            if (lastLineContainer != null)
            {
                lastLineContainer.SetActive(true);
                lastLineCharacterNameContainer.GetComponent<RectTransform>().position = bubblePresneter.GetComponent<RectTransform>().position;
            }

            bool hasName = !string.IsNullOrEmpty(characterName);
            if (lastLineCharacterNameText != null)
                lastLineCharacterNameText.text = hasName ? characterName : string.Empty;
            if (lastLineCharacterNameContainer != null)
                lastLineCharacterNameContainer.SetActive(hasName);
        }

        private void HideLastLine()
        {
            if (lastLineContainer != null) lastLineContainer.SetActive(false);
            if (lastLineCharacterNameContainer != null) lastLineCharacterNameContainer.SetActive(false);
            if (lastLineText != null) lastLineText.text = string.Empty;
            if (lastLineCharacterNameText != null) lastLineCharacterNameText.text = string.Empty;
        }
    }
}