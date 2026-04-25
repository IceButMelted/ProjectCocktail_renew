/*
 * BubbleOptionsPresenter.cs
 * Based on OptionsPresenter.cs from YarnSpinner-Unity (#current branch)
 * Yarn Spinner is licensed under the terms found in LICENSE.md.
 */

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using TMPro;
using Yarn.Unity.Attributes;

#nullable enable

namespace Yarn.Unity
{
    [HelpURL("https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/dialogue-view/options-list-view")]
    public sealed class BubbleOptionsPresenter : DialoguePresenterBase
    {
        // ── Core refs ─────────────────────────────────────────────────────

        [SerializeField] CanvasGroup? canvasGroup;

        [MustNotBeNull]
        [SerializeField] BubbleOptionItem? optionViewPrefab;

        List<BubbleOptionItem> optionViews = new List<BubbleOptionItem>();

        // ── Last-line display ─────────────────────────────────────────────

        [Space]
        [SerializeField] bool showsLastLine;

        [ShowIf(nameof(showsLastLine))]
        [Indent]
        [MustNotBeNullWhen(nameof(showsLastLine))]
        [SerializeField] TMP_Text? lastLineText;

        [ShowIf(nameof(showsLastLine))]
        [Indent]
        [SerializeField] GameObject? lastLineContainer;

        [ShowIf(nameof(showsLastLine))]
        [Indent]
        [SerializeField] TMP_Text? lastLineCharacterNameText;

        [ShowIf(nameof(showsLastLine))]
        [Indent]
        [SerializeField] GameObject? lastLineCharacterNameContainer;

        LocalizedLine? lastSeenLine;

        private const string TruncateLastLineMarkupName = "lastline";

        // ── Standard options settings ─────────────────────────────────────

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

        // ── Bubble layout settings ────────────────────────────────────────

        [Space]
        [Header("Bubble Layout")]

        [Tooltip("Fraction of the parent width each bubble occupies (0-1).")]
        [SerializeField, Range(0.2f, 0.9f)]
        private float bubbleWidthFraction = 0.55f;

        [Tooltip("Horizontal inset from the anchored edge (pixels).")]
        [SerializeField]
        private float horizontalPadding = 24f;

        [Tooltip("Vertical offset from the bottom of the parent (pixels).")]
        [SerializeField]
        private float verticalBaseOffset = 0f;

        [Tooltip("Gap between stacked bubbles (pixels).")]
        [SerializeField]
        private float bubbleSpacing = 12f;

        [Tooltip("Flip the bubble Image on X for right-side bubbles.")]
        [SerializeField]
        private bool mirrorRightBubbles = true;

        // ── Lifecycle ─────────────────────────────────────────────────────

        private void Start()
        {
            // ── DEBUG: confirm Start() runs and prefab is assigned ────────
            Debug.Log($"[BubbleOptionsPresenter] Start()" +
                      $" | prefab={(optionViewPrefab != null ? optionViewPrefab.name : "NULL")}" +
                      $" | canvasGroup={(canvasGroup != null ? canvasGroup.name : "NULL")}");

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (lastLineContainer == null && lastLineText != null)
                lastLineContainer = lastLineText.gameObject;

            if (lastLineCharacterNameContainer == null && lastLineCharacterNameText != null)
                lastLineCharacterNameContainer = lastLineCharacterNameText.gameObject;
        }

        public override YarnTask OnDialogueStartedAsync()
        {
            Debug.Log("[BubbleOptionsPresenter] OnDialogueStartedAsync()");

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            Debug.Log("[BubbleOptionsPresenter] OnDialogueCompleteAsync()");

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            return YarnTask.CompletedTask;
        }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            Debug.Log($"[BubbleOptionsPresenter] RunLineAsync() | line: {line.Text.Text}");

            if (showsLastLine)
                lastSeenLine = line;

            return YarnTask.CompletedTask;
        }

        // ── Main options logic ────────────────────────────────────────────

        public override async YarnTask<DialogueOption?> RunOptionsAsync(
            DialogueOption[] dialogueOptions,
            LineCancellationToken cancellationToken)
        {
            Debug.Log($"[BubbleOptionsPresenter] RunOptionsAsync() called with {dialogueOptions.Length} options.");

            // ── CHECKPOINT 1: log every option's availability ─────────────
            for (int i = 0; i < dialogueOptions.Length; i++)
                Debug.Log($"  Option[{i}]: '{dialogueOptions[i].Line.TextWithoutCharacterName.Text}'" +
                          $" IsAvailable={dialogueOptions[i].IsAvailable}");

            // ── Skip entirely if nothing is selectable ────────────────────
            bool anyAvailable = false;
            foreach (var option in dialogueOptions)
                if (option.IsAvailable) { anyAvailable = true; break; }

            if (!anyAvailable)
            {
                Debug.LogWarning("[BubbleOptionsPresenter] No available options — returning null early.");
                return null;
            }

            // ── CHECKPOINT 2: prefab null check ───────────────────────────
            if (optionViewPrefab == null)
            {
                Debug.LogError("[BubbleOptionsPresenter] optionViewPrefab is NULL! " +
                               "Assign the BubbleOptionItem prefab in the Inspector.");
                return null;
            }

            // ── Grow the pool if needed ───────────────────────────────────
            Debug.Log($"[BubbleOptionsPresenter] Pool size before grow: {optionViews.Count}" +
                      $" | need: {dialogueOptions.Length}");

            while (dialogueOptions.Length > optionViews.Count)
            {
                var newView = CreateNewOptionView();
                optionViews.Add(newView);
                Debug.Log($"[BubbleOptionsPresenter] Created pool item → parent: {newView.transform.parent?.name}" +
                          $" | active: {newView.gameObject.activeSelf}");
            }

            // ── Completion plumbing ───────────────────────────────────────
            var selectedOptionCompletionSource =
                new YarnTaskCompletionSource<DialogueOption?>();

            var completionCancellationSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken.NextContentToken);

            async YarnTask CancelSourceWhenDialogueCancelled()
            {
                await YarnTask.WaitUntilCanceled(completionCancellationSource.Token);
                if (cancellationToken.IsNextContentRequested == true)
                    selectedOptionCompletionSource.TrySetResult(null);
            }
            CancelSourceWhenDialogueCancelled().Forget();

            // ── Hide all views first ──────────────────────────────────────
            foreach (var view in optionViews)
                view.gameObject.SetActive(false);

            float cumulativeY = verticalBaseOffset;

            // ── CHECKPOINT 3: configure and position each view ────────────
            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                var option = dialogueOptions[i];

                if (!option.IsAvailable && !showUnavailableOptions)
                {
                    Debug.Log($"[BubbleOptionsPresenter] Skipping option[{i}] — unavailable.");
                    continue;
                }

                var optionView = optionViews[i];
                optionView.gameObject.SetActive(true);

                optionView.Option = option;
                optionView.OnOptionSelected = selectedOptionCompletionSource;
                optionView.completionToken = completionCancellationSource.Token;

                bool isLeft = (i % 2 == 0);
                var rt = optionView.GetComponent<RectTransform>();

                // ── CHECKPOINT 4: log RectTransform state before anchor ────
                Debug.Log($"[BubbleOptionsPresenter] Option[{i}] before anchor" +
                          $" | parent: {rt.transform.parent?.name}" +
                          $" | parentRect: {(rt.parent as RectTransform)?.rect}" +
                          $" | sizeDelta: {rt.sizeDelta}" +
                          $" | isLeft: {isLeft}");

                ApplyBubbleAnchor(rt, isLeft, cumulativeY);

                // ── CHECKPOINT 5: log after anchor ─────────────────────────
                Debug.Log($"[BubbleOptionsPresenter] Option[{i}] after anchor" +
                          $" | anchorMin: {rt.anchorMin}" +
                          $" | anchorMax: {rt.anchorMax}" +
                          $" | pivot: {rt.pivot}" +
                          $" | anchoredPos: {rt.anchoredPosition}" +
                          $" | sizeDelta: {rt.sizeDelta}");

                if (mirrorRightBubbles)
                    MirrorBubbleGraphic(optionView, isLeft);

                cumulativeY += rt.sizeDelta.y + bubbleSpacing;
            }

            // ── Handle initial highlight ──────────────────────────────────
            int optionIndexToSelect = -1;
            for (int i = 0; i < optionViews.Count; i++)
            {
                var view = optionViews[i];
                if (!view.isActiveAndEnabled) continue;
                if (view.IsHighlighted) { optionIndexToSelect = i; break; }
                if (optionIndexToSelect == -1) optionIndexToSelect = i;
            }
            if (optionIndexToSelect > -1)
                optionViews[optionIndexToSelect].Select();

            // ── Last-line display ─────────────────────────────────────────
            if (lastLineContainer != null)
            {
                if (lastSeenLine != null && showsLastLine)
                {
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
                                lastLineCharacterNameText.text = lastSeenLine.CharacterName;
                        }
                    }
                    else
                    {
                        line = lastSeenLine.TextWithoutCharacterName;
                    }

                    var lineText = line.Text;

                    if (line.TryGetAttributeWithName(TruncateLastLineMarkupName, out var markup))
                        if (markup.Position <= lineText.Length)
                            lineText = "..." + lineText.Substring(markup.Position);

                    if (lastLineText != null)
                        lastLineText.text = lineText;

                    lastLineContainer.SetActive(true);
                }
                else
                {
                    lastLineContainer.SetActive(false);
                }
            }

            // ── CHECKPOINT 6: about to fade in ────────────────────────────
            Debug.Log($"[BubbleOptionsPresenter] About to fade in." +
                      $" | useFadeEffect={useFadeEffect}" +
                      $" | canvasGroup={(canvasGroup != null ? $"alpha={canvasGroup.alpha}" : "NULL")}");

            if (useFadeEffect && canvasGroup != null)
                await Effects.FadeAlphaAsync(canvasGroup, 0, 1, fadeUpDuration,
                    cancellationToken.HurryUpToken);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1; // force visible even if fade was skipped
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            Debug.Log("[BubbleOptionsPresenter] Fade in complete — waiting for selection.");

            // ── Wait for the player to pick an option ─────────────────────
            var completedTask = await selectedOptionCompletionSource.Task;
            completionCancellationSource.Cancel();

            Debug.Log($"[BubbleOptionsPresenter] Option selected: " +
                      $"'{completedTask?.Line.TextWithoutCharacterName.Text ?? "null"}'");

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            // ── Fade out ──────────────────────────────────────────────────
            if (useFadeEffect && canvasGroup != null)
                await Effects.FadeAlphaAsync(canvasGroup, 1, 0, fadeDownDuration,
                    cancellationToken.HurryUpToken);

            foreach (var optionView in optionViews)
                optionView.gameObject.SetActive(false);

            await YarnTask.Yield();

            if (cancellationToken.NextContentToken.IsCancellationRequested)
                return await DialogueRunner.NoOptionSelected;

            return completedTask;
        }

        // ── Private helpers ───────────────────────────────────────────────

        private BubbleOptionItem CreateNewOptionView()
        {
            var optionView = Instantiate(optionViewPrefab);

            if (optionView == null)
                throw new System.InvalidOperationException(
                    $"Instantiate returned null — {nameof(optionViewPrefab)} prefab is broken.");

            var targetTransform = canvasGroup != null
                ? canvasGroup.transform
                : this.transform;

            optionView.transform.SetParent(targetTransform, false);
            optionView.transform.SetAsLastSibling();
            optionView.gameObject.SetActive(false);

            return optionView;
        }

        private void ApplyBubbleAnchor(RectTransform rt, bool anchorLeft, float yOffset)
        {
            float parentWidth = 0f;
            if (rt.parent is RectTransform parentRT)
                parentWidth = parentRT.rect.width;

            // If parentWidth is 0 the Canvas rect hasn't been calculated yet —
            // fall back to Screen.width as a safe approximation.
            if (parentWidth <= 0f)
            {
                Debug.LogWarning("[BubbleOptionsPresenter] Parent RectTransform width is 0 " +
                                 "— falling back to Screen.width. Check your Canvas setup.");
                parentWidth = Screen.width;
            }

            float bubbleWidth = parentWidth * bubbleWidthFraction;

            if (anchorLeft)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.sizeDelta = new Vector2(bubbleWidth, rt.sizeDelta.y);
                rt.anchoredPosition = new Vector2(horizontalPadding, yOffset);
            }
            else
            {
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.sizeDelta = new Vector2(bubbleWidth, rt.sizeDelta.y);
                rt.anchoredPosition = new Vector2(-horizontalPadding, yOffset);
            }
        }

        private void MirrorBubbleGraphic(BubbleOptionItem optionView, bool isLeft)
        {
            var s = optionView.transform.localScale;

            if (isLeft)
            {
                optionView.transform.localScale = new Vector3(Mathf.Abs(s.x), s.y, s.z);
            }
            else
            {
                optionView.transform.localScale = new Vector3(-Mathf.Abs(s.x), s.y, s.z);

                foreach (var tmp in optionView.GetComponentsInChildren<TMP_Text>())
                {
                    var ts = tmp.transform.localScale;
                    tmp.transform.localScale = new Vector3(-Mathf.Abs(ts.x), ts.y, ts.z);
                }
            }
        }
    }
}