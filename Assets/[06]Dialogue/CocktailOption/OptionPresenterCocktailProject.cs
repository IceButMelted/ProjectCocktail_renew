/*
 * OptionPresenterCocktailProject.cs
 * Based on OptionsPresenter.cs from YarnSpinner-Unity (Yarn Spinner licence, see LICENSE.md).
 * Same behaviour as the stock presenter; add Cocktail Project features in the marked spots.
 */

using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Unity;
using Yarn.Unity.Attributes;

#nullable enable

namespace YarnSpinner.Custom
{
    /// <summary>
    /// Receives options from a <see cref="DialogueRunner"/>, and displays and
    /// manages a collection of <see cref="OptionItem"/> views for the user to choose from.
    /// </summary>
    public sealed class OptionPresenterCocktailProject : DialoguePresenterBase
    {
        [SerializeField] CanvasGroup? canvasGroup;

        [MustNotBeNull]
        [SerializeField] OptionItem? optionViewPrefab;

        [Tooltip("Parent for spawned option items. Empty = the CanvasGroup, else this object.")]
        [SerializeField] Transform? optionContainer;

        // Pool of OptionItem views so we can reuse them
        readonly List<OptionItem> optionViews = new List<OptionItem>();

        [Space]
        [SerializeField] bool showsLastLine;

        [ShowIf(nameof(showsLastLine))]
        [Indent]
        [MustNotBeNullWhen(nameof(showsLastLine))]
        [SerializeField] TextMeshProUGUI? lastLineText;

        [ShowIf(nameof(showsLastLine))]
        [Indent]
        [SerializeField] GameObject? lastLineContainer;

        [ShowIf(nameof(showsLastLine))]
        [Indent]
        [SerializeField] TextMeshProUGUI? lastLineCharacterNameText;

        [ShowIf(nameof(showsLastLine))]
        [Indent]
        [SerializeField] GameObject? lastLineCharacterNameContainer;

        LocalizedLine? lastSeenLine;

        /// <summary>Show options whose <see cref="OptionSet.Option.IsAvailable"/> is false.</summary>
        [Space]
        public bool showUnavailableOptions = false;

        [Group("Fade")]
        [Label("Fade UI")]
        public bool useFadeEffect = true;

        [Group("Fade")]
        [ShowIf(nameof(useFadeEffect))]
        public float fadeUpDuration = 0.25f;

        [Group("Fade")]
        [ShowIf(nameof(useFadeEffect))]
        public float fadeDownDuration = 0.1f;

        [Group("Timeout")]
        [Tooltip("Optional countdown bar. Timeout works without it.")]
        [SerializeField] TimeoutBar? timedBar;

        [Group("Timeout")]
        [Tooltip("Seconds before the option tagged #timeout (visible) or #SilentTimeout (hidden) is chosen automatically. 0 or less disables timeout.")]
        public float autoSelectDuration = 10f;

        private const string TruncateLastLineMarkupName = "lastline";
        private const string TimeoutMetadataTag = "timeout";
        private const string SilentTimeoutMetadataTag = "silenttimeout";

        void HideCanvas()
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            HideCanvas();
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueStartedAsync()
        {
            HideCanvas();
            return YarnTask.CompletedTask;
        }

        private void Start()
        {
            HideCanvas();

            if (lastLineContainer == null && lastLineText != null)
            {
                lastLineContainer = lastLineText.gameObject;
            }
            if (lastLineCharacterNameContainer == null && lastLineCharacterNameText != null)
            {
                lastLineCharacterNameContainer = lastLineCharacterNameText.gameObject;
            }
        }

        /// <summary>
        /// This view does not show lines; it stores the last one so it can be
        /// shown when options appear.
        /// </summary>
        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (showsLastLine)
            {
                lastSeenLine = line;
            }
            return YarnTask.CompletedTask;
        }

        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            // All unavailable: return null, the DialogueRunner handles it.
            bool anyAvailable = false;
            foreach (var option in dialogueOptions)
            {
                if (option.IsAvailable)
                {
                    anyAvailable = true;
                    break;
                }
            }
            if (!anyAvailable)
            {
                return null;
            }

            // Timeout: an available option tagged #timeout is hidden and auto-chosen when time runs out.
            DialogueOption? fallbackOption = null;
            bool fallbackHidden = false;
            if (autoSelectDuration > 0f)
            {
                foreach (var option in dialogueOptions)
                {
                    if (!option.IsAvailable) continue;
                    foreach (var meta in option.Line.Metadata)
                    {
                        bool silent = string.Equals(meta, SilentTimeoutMetadataTag, System.StringComparison.OrdinalIgnoreCase);
                        if (!silent && !string.Equals(meta, TimeoutMetadataTag, System.StringComparison.OrdinalIgnoreCase)) continue;
                        if (fallbackOption != null)
                        {
                            Debug.LogError($"[{nameof(OptionPresenterCocktailProject)}] More than one option is tagged #timeout/#SilentTimeout. Only one is allowed.");
                        }
                        else
                        {
                            fallbackOption = option;
                            fallbackHidden = silent;
                        }
                        break;
                    }
                }
            }

            while (dialogueOptions.Length > optionViews.Count)
            {
                optionViews.Add(CreateNewOptionView());
            }

            var selectedOptionCompletionSource = new YarnTaskCompletionSource<DialogueOption?>();

            // Cancelled when any item is selected, or when the whole view is cancelled.
            var completionCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.NextContentToken);

            async YarnTask CancelSourceWhenDialogueCancelled()
            {
                await YarnTask.WaitUntilCanceled(completionCancellationSource.Token);

                if (cancellationToken.IsNextContentRequested == true)
                {
                    // Runner no longer wants our result; bail out fast.
                    selectedOptionCompletionSource.TrySetResult(null);
                }
            }

            CancelSourceWhenDialogueCancelled().Forget();

            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                var optionView = optionViews[i];
                var option = dialogueOptions[i];

                if (option.IsAvailable == false && showUnavailableOptions == false)
                {
                    continue;
                }

                // #SilentTimeout has no button; the timer represents it.
                if (fallbackHidden && fallbackOption != null && option.DialogueOptionID == fallbackOption.DialogueOptionID)
                {
                    continue;
                }

                optionView.gameObject.SetActive(true);
                optionView.Option = option;

                optionView.OnOptionSelected = selectedOptionCompletionSource;
                optionView.completionToken = completionCancellationSource.Token;
            }

            // Select after all items are configured, otherwise two can look selected.
            // Prefer an already-highlighted item, else the first active one.
            int optionIndexToSelect = -1;
            for (int i = 0; i < optionViews.Count; i++)
            {
                var view = optionViews[i];
                if (!view.isActiveAndEnabled)
                {
                    continue;
                }

                if (view.IsHighlighted)
                {
                    optionIndexToSelect = i;
                    break;
                }

                if (optionIndexToSelect == -1)
                {
                    optionIndexToSelect = i;
                }
            }
            if (optionIndexToSelect > -1)
            {
                optionViews[optionIndexToSelect].Select();
            }

            UpdateLastLine();

            if (timedBar != null)
            {
                timedBar.gameObject.SetActive(fallbackOption != null);
            }

            if (useFadeEffect && canvasGroup != null)
            {
                await Effects.FadeAlphaAsync(canvasGroup, 0, 1, fadeUpDuration, cancellationToken.HurryUpToken);
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (fallbackOption != null)
            {
                RunTimeout(selectedOptionCompletionSource, fallbackOption, completionCancellationSource.Token).Forget();
            }

            var completedTask = await selectedOptionCompletionSource.Task;
            completionCancellationSource.Cancel();

            if (timedBar != null)
            {
                timedBar.gameObject.SetActive(false);
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (useFadeEffect && canvasGroup != null)
            {
                await Effects.FadeAlphaAsync(canvasGroup, 1, 0, fadeDownDuration, cancellationToken.HurryUpToken);
            }

            foreach (var optionView in optionViews)
            {
                optionView.gameObject.SetActive(false);
            }
            await YarnTask.Yield();

            // Cancelled: return without a selection.
            if (cancellationToken.NextContentToken.IsCancellationRequested)
            {
                return await DialogueRunner.NoOptionSelected;
            }

            return completedTask;
        }

        async YarnTask RunTimeout(YarnTaskCompletionSource<DialogueOption?> source, DialogueOption fallback, CancellationToken token)
        {
            if (timedBar != null)
            {
                await timedBar.Shrink(autoSelectDuration, token);
            }

            // Bar missing, or Shrink returned early: wait out the remainder manually.
            float elapsed = 0f;
            while (timedBar == null && elapsed < autoSelectDuration && !token.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                await YarnTask.Yield();
            }

            if (!token.IsCancellationRequested)
            {
                source.TrySetResult(fallback);
            }
        }

        void UpdateLastLine()
        {
            if (lastLineContainer == null) return;

            if (lastSeenLine == null || !showsLastLine)
            {
                lastLineContainer.SetActive(false);
                return;
            }

            // Show the nameplate only when a container exists and the line has a character.
            var line = lastSeenLine.Text;
            if (lastLineCharacterNameContainer != null)
            {
                if (string.IsNullOrWhiteSpace(lastSeenLine.CharacterName))
                {
                    lastLineCharacterNameContainer.SetActive(false);
                }
                else
                {
                    line = lastSeenLine.TextWithoutCharacterName;
                    lastLineCharacterNameContainer.SetActive(true);
                    if (lastLineCharacterNameText != null)
                    {
                        lastLineCharacterNameText.text = lastSeenLine.CharacterName;
                    }
                }
            }
            else
            {
                line = lastSeenLine.TextWithoutCharacterName;
            }

            var lineText = line.Text;
            // [lastline] markup: replace everything before the marker with "..."
            if (line.TryGetAttributeWithName(TruncateLastLineMarkupName, out var markup)
                && markup.Position <= lineText.Length)
            {
                lineText = "..." + lineText.Substring(markup.Position);
            }

            if (lastLineText != null)
            {
                lastLineText.text = lineText;
            }

            lastLineContainer.SetActive(true);
        }

        private OptionItem CreateNewOptionView()
        {
            var optionView = Instantiate(optionViewPrefab);

            if (optionView == null)
            {
                throw new System.InvalidOperationException($"Can't create new option view: {nameof(optionView)} is null");
            }

            var targetTransform = optionContainer != null ? optionContainer
                : canvasGroup != null ? canvasGroup.transform : this.transform;

            optionView.transform.SetParent(targetTransform, false);
            optionView.transform.SetAsLastSibling();
            optionView.gameObject.SetActive(false);

            return optionView;
        }
    }
}
