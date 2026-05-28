/*
 * BubbleOptionsPresenter.cs
 * Based on OptionsPresenter.cs from YarnSpinner-Unity (current branch)
 *
 * Shows up to 4 option bubbles anchored to inspector-assigned Transform points.
 *
 * Also displays the LAST SPOKEN LINE while options are visible, using:
 *   lastLineText                 – TMP_Text for the line body
 *   lastLineContainer            – GameObject shown/hidden around the line display
 *   lastLineCharacterNameText    – TMP_Text for the character name
 *   lastLineCharacterNameContainer – GameObject shown/hidden when name exists
 *
 * The last line is received via RunLineAsync (called by the DialogueRunner just
 * before RunOptionsAsync). We cache it and display it while waiting for a choice.
 *
 * Option anchors:
 *   optionAnchors[0] → position for option 1
 *   optionAnchors[1] → position for option 2
 *   optionAnchors[2] → position for option 3
 *   optionAnchors[3] → position for option 4
 */

using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Unity;

namespace YarnSpinner.Custom
{
    public class BubbleOptionsPresenter : DialoguePresenterBase
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Option Prefab")]
        [Tooltip("Prefab with BubbleOptionItem + Button + TMP_Text.")]
        [SerializeField] private BubbleOptionItem optionItemPrefab;

        [Header("Anchor Points (max 4)")]
        [Tooltip("Empty GameObjects whose positions place each option bubble. " +
                 "Index 0 = first option, Index 1 = second option, Index 2 = third option, Index 3 = fourth option.")]
        [SerializeField] private Transform[] optionAnchors = new Transform[4];
        [Header("Options Container")]
        [Tooltip("Parent RectTransform under which option items are spawned.")]
        [SerializeField] private RectTransform optionsContainer;

        [Header("Last Line Display")]
        [Tooltip("TMP_Text that shows the body of the last spoken line " +
                 "while the player is choosing an option.")]
        [SerializeField] private TMP_Text lastLineText;

        [Tooltip("GameObject that wraps the last-line display. " +
                 "Shown when options appear, hidden otherwise.")]
        [SerializeField] private GameObject lastLineContainer;

        [Tooltip("TMP_Text that shows the character name of the last spoken line.")]
        [SerializeField] private TMP_Text lastLineCharacterNameText;

        [Tooltip("GameObject that wraps the character name label. " +
                 "Shown only when the last line has a non-empty character name.")]
        [SerializeField] private GameObject lastLineCharacterNameContainer;

        [Header("Presenter GameObject")]
        [Tooltip("Root GameObject for the BubblePresenterGameObject. To Set Last Line Container to BubblePresenterGameObject place")]
        [SerializeField] private GameObject bubblePresenterGameObject;
        [Tooltip("Root GameObject for the FacllBackGameObject. To set Last Line Container to FallBackGameObject")]
        [SerializeField] private GameObject fallBackGameObject;

        // ── State ─────────────────────────────────────────────────────────────

        private BubbleOptionItem[] _activeItems = new BubbleOptionItem[4];
        private YarnTaskCompletionSource<DialogueOption> _completionSource;

        // Cache the last received line so we can display it during options
        private string _lastLineBody          = string.Empty;
        private string _lastLineCharacterName = string.Empty;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            HideLastLine();
        }

        // ── DialoguePresenterBase ─────────────────────────────────────────────

        public override YarnTask OnDialogueStartedAsync()
        {
            HideLastLine();
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            ClearOptions();
            HideLastLine();
            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// Cache the line text and character name so we can show them
        /// while the options are being presented.
        /// We return immediately — we are NOT responsible for displaying the
        /// running line (BubblePresenter does that). We just remember it.
        /// </summary>
        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            _lastLineCharacterName = line.CharacterName ?? string.Empty;
            _lastLineBody          = line.TextWithoutCharacterName.Text ?? string.Empty;
            return YarnTask.CompletedTask;
        }

        [System.Obsolete]
        public override async YarnTask<DialogueOption> RunOptionsAsync(
            DialogueOption[] dialogueOptions,
            CancellationToken cancellationToken)
        {
            // Guard checks
            if (optionItemPrefab == null)
            {
                Debug.LogWarning("[BubbleOptionsPresenter] optionItemPrefab is not set.");
                return null;
            }

            if (optionsContainer == null)
            {
                Debug.LogWarning("[BubbleOptionsPresenter] optionsContainer is not set.");
                return null;
            }

            // ── Show the last spoken line ─────────────────────────────────
            ShowLastLine(_lastLineCharacterName, _lastLineBody);

            // ── Clear any leftover option buttons ──────────────────────────
            ClearOptions();

            // ── Spawn option buttons (max 4) ───────────────────────────────
            int count = Mathf.Min(dialogueOptions.Length, 4);
            _completionSource = new YarnTaskCompletionSource<DialogueOption>();

            for (int i = 0; i < count; i++)
            {
                var item = Instantiate(optionItemPrefab, optionsContainer);
                item.Configure(dialogueOptions[i]);
                item.OnOptionSelected = OnOptionSelected;

                if (i < optionAnchors.Length && optionAnchors[i] != null)
                    item.transform.position = optionAnchors[i].position;
                //CentreOnAnchor(item.GetComponent<RectTransform>(), optionAnchors[i]);

                _activeItems[i] = item;
                item.gameObject.SetActive(true);
            }

            // ── Wait for player choice or external cancellation ────────────
            DialogueOption result = null;

            using (cancellationToken.Register(() =>
                _completionSource?.TrySetCanceled()))
            {
                try
                {
                    result = await _completionSource.Task;
                }
                catch (System.OperationCanceledException)
                {
                    // Dialogue interrupted externally
                }
            }

            // ── Clean up ───────────────────────────────────────────────────
            ClearOptions();
            HideLastLine();

            return result;
        }

        // ── Last Line Helpers ─────────────────────────────────────────────────

        /// <summary>
        /// Populate and show the last-line UI fields.
        /// </summary>
        private void ShowLastLine(string characterName, string body)
        {
            // Line body
            if (lastLineText != null)
                lastLineText.text = body;

            if (lastLineContainer != null)
                lastLineContainer.SetActive(true);

            // Character name — show container only when name is non-empty
            bool hasName = !string.IsNullOrEmpty(characterName);

            if (lastLineCharacterNameText != null)
                lastLineCharacterNameText.text = hasName ? characterName : string.Empty;

            if (lastLineCharacterNameContainer != null)
                lastLineCharacterNameContainer.SetActive(hasName);

            //Set Last Line Container to persenter place
            if (bubblePresenterGameObject != null)
                lastLineContainer.transform.position = bubblePresenterGameObject.transform.position;
            else if (fallBackGameObject != null)
                lastLineContainer.transform.position = fallBackGameObject.transform.position;
            else
                Debug.LogWarning("[BubbleOptionsPresenter] No presenterGameObject or fallBackGameObject assigned for last line container position.");
        }

        /// <summary>
        /// Hide all last-line UI fields and clear the text.
        /// </summary>
        private void HideLastLine()
        {
            if (lastLineContainer != null)
                lastLineContainer.SetActive(false);

            if (lastLineCharacterNameContainer != null)
                lastLineCharacterNameContainer.SetActive(false);

            if (lastLineText != null)
                lastLineText.text = string.Empty;

            if (lastLineCharacterNameText != null)
                lastLineCharacterNameText.text = string.Empty;
        }

        // ── Option Helpers ────────────────────────────────────────────────────

        /// Note: NOT USE IN THIS VERSION. We just set the option item position to the anchor's world position.
        /// <summary>
        /// Note: NOT USE IN THIS VERSION. We just set the option item position to the anchor's world position.
        /// Centre <paramref name="rect"/> on the world position of <paramref name="anchor"/>.
        /// Works for Screen Space – Overlay and Screen Space – Camera canvases.
        /// </summary>
        private static void CentreOnAnchor(RectTransform rect, Transform anchor)
        {
            if (rect == null || anchor == null) return;

            var canvas = rect.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, anchor.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, cam, out Vector2 localPoint);

            rect.pivot      = new Vector2(0.5f, 0.5f);
            rect.anchorMin  = Vector2.zero;
            rect.anchorMax  = Vector2.zero;
            rect.anchoredPosition = localPoint;
        }

        private void OnOptionSelected(DialogueOption option)
        {
            _completionSource?.TrySetResult(option);
        }

        private void ClearOptions()
        {
            for (int i = 0; i < _activeItems.Length; i++)
            {
                if (_activeItems[i] != null)
                {
                    Destroy(_activeItems[i].gameObject);
                    _activeItems[i] = null;
                }
            }
        }
    }
}
