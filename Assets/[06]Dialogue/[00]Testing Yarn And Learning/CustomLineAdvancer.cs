/*
 * CustomLineAdvancer.cs
 *
 * Based on Yarn Spinner 3.2's LineAdvancer.
 *
 * Ping-Pong mode: when hurryUpLineKeyCode == nextLineKeyCode (e.g. both Space):
 *   1st press → RequestHurryUpLine  (typewriter still running)
 *   2nd press → RequestNextLine     (line fully shown, waiting for player)
 *
 * When the two keys are different, behaviour is identical to the original.
 */

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity.Attributes;
using TMPro;

#nullable enable

namespace Yarn.Unity
{
    public sealed class CustomLineAdvancer : DialoguePresenterBase, IActionMarkupHandler
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [MustNotBeNull("CustomLineAdvancer needs a Dialogue Runner.")]
        [Tooltip("The dialogue runner that receives advance/cancel requests.")]
        [SerializeField] DialogueRunner? runner;

        [Tooltip("The presenter whose Typewriter notifies us when a line is fully visible.")]
        [SerializeField] DialoguePresenterBase? presenter;

        [Space]
        [Tooltip("Does repeatedly requesting a line advance cancel the line?")]
        public bool multiAdvanceIsCancel = false;

        [ShowIf(nameof(multiAdvanceIsCancel))]
        [Indent]
        [Label("Advance Count")]
        [Tooltip("Number of advance requests before the current line is cancelled.")]
        public int advanceRequestsBeforeCancellingLine = 2;

        // ── Input mode ───────────────────────────────────────────────────────

        public enum InputMode
        {
            InputActions,
            KeyCodes,
            None,
            LegacyInputAxes,
        }

        [Space]
        [MessageBox(sourceMethod: nameof(ValidateInputMode))]
        [SerializeField] InputMode inputMode = InputMode.KeyCodes;

        // NOTE: We use #if preprocessor directives directly here instead of
        // InputSystemAvailability.enableInputSystem / inputSystemInstalled /
        // enableLegacyInput, because those fields are marked 'internal' inside
        // Yarn Spinner's own assembly and are not visible from Assembly-CSharp.
        // The preprocessor symbols (ENABLE_INPUT_SYSTEM, USE_INPUTSYSTEM,
        // ENABLE_LEGACY_INPUT_MANAGER) are project-wide and always accessible.

        InputMode UsedInputMode
        {
            get
            {
                if (inputMode == InputMode.InputActions)
                {
#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
                    return InputMode.InputActions;
#else
                    // Input System not installed or not enabled — fall back to keys
                    return InputMode.KeyCodes;
#endif
                }
                return inputMode;
            }
        }

        private MessageBoxAttribute.Message ValidateInputMode()
        {
            if (inputMode == InputMode.None)
                return MessageBoxAttribute.Info(
                    $"Call directly:\n- {nameof(RequestLineHurryUp)}()\n" +
                    $"- {nameof(RequestNextLine)}()\n" +
                    $"- {nameof(RequestOptionHurryUp)}()\n" +
                    $"- {nameof(RequestDialogueCancellation)}()");

#if !ENABLE_LEGACY_INPUT_MANAGER
            if (inputMode == InputMode.LegacyInputAxes)
                return MessageBoxAttribute.Warning("Input Manager (Old) is not enabled.");
#endif

            if (inputMode == InputMode.InputActions)
            {
#if !USE_INPUTSYSTEM
                return MessageBoxAttribute.Warning("Install the Unity Input System package.\nFalling back to keyboard.");
#elif !ENABLE_INPUT_SYSTEM
                return MessageBoxAttribute.Warning("Unity Input System is not enabled.\nFalling back to keyboard.");
#endif
            }

            return MessageBoxAttribute.NoMessage;
        }

        // ── KeyCode inputs ───────────────────────────────────────────────────

        [ShowIf(nameof(UsedInputMode), InputMode.KeyCodes)]
        [Indent]
        [Tooltip("Hurry up key. Set equal to Next Line Key to enable ping-pong mode.")]
        [SerializeField] KeyCode hurryUpLineKeyCode = KeyCode.Space;

        [ShowIf(nameof(UsedInputMode), InputMode.KeyCodes)]
        [Indent]
        [Tooltip("Next line key. Set equal to Hurry Up Key to enable ping-pong mode.")]
        [SerializeField] KeyCode nextLineKeyCode = KeyCode.Space;

        [ShowIf(nameof(UsedInputMode), InputMode.KeyCodes)]
        [Indent]
        [SerializeField] KeyCode hurryUpOptionsKeyCode = KeyCode.Space;

        [ShowIf(nameof(UsedInputMode), InputMode.KeyCodes)]
        [Indent]
        [SerializeField] KeyCode cancelDialogueKeyCode = KeyCode.None;

        // ── Legacy axis inputs ───────────────────────────────────────────────

        [ShowIf(nameof(UsedInputMode), InputMode.LegacyInputAxes)]
        [Indent]
        [SerializeField] string? hurryUpLineAxis = "Jump";

        [ShowIf(nameof(UsedInputMode), InputMode.LegacyInputAxes)]
        [Indent]
        [SerializeField] string? nextLineAxis = "Cancel";

        [ShowIf(nameof(UsedInputMode), InputMode.LegacyInputAxes)]
        [Indent]
        [SerializeField] string? hurryUpOptionsAxis = "Jump";

        [ShowIf(nameof(UsedInputMode), InputMode.LegacyInputAxes)]
        [Indent]
        [SerializeField] string? cancelDialogueAxis = "";

        // ── Input System actions ─────────────────────────────────────────────

#if USE_INPUTSYSTEM
        [ShowIf(nameof(UsedInputMode), InputMode.InputActions)]
        [Indent]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? hurryUpLineAction;

        [ShowIf(nameof(UsedInputMode), InputMode.InputActions)]
        [Indent]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? nextLineAction;

        [ShowIf(nameof(UsedInputMode), InputMode.InputActions)]
        [Indent]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? hurryUpOptionsAction;

        [ShowIf(nameof(UsedInputMode), InputMode.InputActions)]
        [Indent]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? cancelDialogueAction;

        [Tooltip("Enable input actions when dialogue starts.")]
        [ShowIf(nameof(UsedInputMode), InputMode.InputActions)]
        [Indent]
        [SerializeField] bool enableActions = true;
#endif

        // ── Runtime state ────────────────────────────────────────────────────

        private int numberOfAdvancesThisLine = 0;
        private int frameContentReceived = 0;

        private enum PresentationStatus
        {
            Unknown,
            LineBegan,      // line arrived, typewriter running
            LineWaiting,    // typewriter finished, waiting for player input
            OptionsBegan,
            OptionsWaiting,
        }
        [SerializeField]private PresentationStatus status = PresentationStatus.Unknown;

        /// <summary>
        /// True when hurry-up and next-line share the same key/axis/action,
        /// enabling automatic ping-pong behaviour.
        /// </summary>
        private bool IsPingPongMode
        {
            get
            {
                switch (UsedInputMode)
                {
                    case InputMode.KeyCodes:
                        return hurryUpLineKeyCode != KeyCode.None
                            && hurryUpLineKeyCode == nextLineKeyCode;

                    case InputMode.LegacyInputAxes:
                        return !string.IsNullOrEmpty(hurryUpLineAxis)
                            && hurryUpLineAxis == nextLineAxis;

#if USE_INPUTSYSTEM
                    case InputMode.InputActions:
                        return hurryUpLineAction != null
                            && hurryUpLineAction == nextLineAction;
#endif
                    default:
                        return false;
                }
            }
        }

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Start()
        {
            if (runner == null || presenter == null) return;

            var list = new List<DialoguePresenterBase?>(runner.DialoguePresenters) { this };
            runner.DialoguePresenters = list;

            StartCoroutine(SubscribeToTypewriterNextFrame());
        }

        private System.Collections.IEnumerator SubscribeToTypewriterNextFrame()
        {
            yield return null; // wait one frame for presenter to finish Start()

            if (presenter == null) yield break;

            if (presenter.Typewriter == null)
            {
                Debug.LogError("Typewriter is still null after one frame — check your presenter setup.", this);
                yield break;
            }

            presenter.Typewriter.ActionMarkupHandlers.Add(this);
            Debug.Log("Typewriter subscription successful.", this);
        }

        // ── DialoguePresenterBase overrides ──────────────────────────────────

        public override YarnTask OnDialogueStartedAsync()
        {
#if USE_INPUTSYSTEM
            if (UsedInputMode == InputMode.InputActions)
            {
                if (enableActions)
                {
                    hurryUpLineAction?.action.Enable();
                    hurryUpOptionsAction?.action.Enable();
                    nextLineAction?.action.Enable();
                    cancelDialogueAction?.action.Enable();
                }
                if (hurryUpLineAction != null)
                    hurryUpLineAction.action.performed += OnHurryUpLinePerformed;
                if (hurryUpOptionsAction != null)
                    hurryUpOptionsAction.action.performed += OnHurryUpOptionsPerformed;
                // Only bind nextLineAction separately when NOT in ping-pong mode
                if (nextLineAction != null && !IsPingPongMode)
                    nextLineAction.action.performed += OnNextLinePerformed;
                if (cancelDialogueAction != null)
                    cancelDialogueAction.action.performed += OnCancelDialoguePerformed;
            }
#endif
            ResetLineTracking();
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
#if USE_INPUTSYSTEM
            if (UsedInputMode == InputMode.InputActions)
            {
                if (hurryUpLineAction != null)
                    hurryUpLineAction.action.performed -= OnHurryUpLinePerformed;
                if (hurryUpOptionsAction != null)
                    hurryUpOptionsAction.action.performed -= OnHurryUpOptionsPerformed;
                if (nextLineAction != null)
                    nextLineAction.action.performed -= OnNextLinePerformed;
                if (cancelDialogueAction != null)
                    cancelDialogueAction.action.performed -= OnCancelDialoguePerformed;
            }
#endif
            ResetLineTracking();
            return YarnTask.CompletedTask;
        }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            ResetLineTracking();
            status = PresentationStatus.LineBegan;
            frameContentReceived = Time.frameCount;
            return YarnTask.CompletedTask;
        }

        public override YarnTask<DialogueOption?> RunOptionsAsync(
            DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            ResetLineTracking();
            status = PresentationStatus.OptionsBegan;
            frameContentReceived = Time.frameCount;
            return DialogueRunner.NoOptionSelected;
        }

        // ── IActionMarkupHandler ─────────────────────────────────────────────

        public void OnPrepareForLine(MarkupParseResult line, TMP_Text text) { }

        public void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text) { }

        public YarnTask OnCharacterWillAppear(
            int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken)
            => YarnTask.CompletedTask;

        public void OnLineWillDismiss() { }

        public void OnLineDisplayComplete()
        {
            if (status == PresentationStatus.LineBegan)
                status = PresentationStatus.LineWaiting;
            else if (status == PresentationStatus.OptionsBegan)
                status = PresentationStatus.OptionsWaiting;
        }

        // ── Input System callbacks ───────────────────────────────────────────

#if USE_INPUTSYSTEM
        private void OnHurryUpLinePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (IsPingPongMode) HandleAdvanceKey();
            else RequestLineHurryUpInternal();
        }

        private void OnHurryUpOptionsPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
            => RequestOptionHurryUp();

        private void OnNextLinePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
            => RequestNextLine();

        private void OnCancelDialoguePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
            => RequestDialogueCancellation();
#endif

        // ── Update polling ───────────────────────────────────────────────────

        private void Update()
{
    switch (UsedInputMode)
    {
        case InputMode.KeyCodes:
            bool pingPong = IsPingPongMode; // evaluate ONCE per frame
            
            if (InputSystemAvailability.GetKeyDown(hurryUpLineKeyCode))
            {
                if (pingPong) HandleAdvanceKey();
                else RequestLineHurryUpInternal();
            }
            // Guard: only poll next key if it's truly a different key
            if (!pingPong && nextLineKeyCode != hurryUpLineKeyCode 
                && InputSystemAvailability.GetKeyDown(nextLineKeyCode))
                RequestNextLine();

            if (InputSystemAvailability.GetKeyDown(hurryUpOptionsKeyCode))
                RequestOptionHurryUp();
            if (InputSystemAvailability.GetKeyDown(cancelDialogueKeyCode))
                RequestDialogueCancellation();
            break;
        // ... rest unchanged
    }
}

        // ── Ping-pong core ───────────────────────────────────────────────────

        /// <summary>
        /// Single-key handler used in ping-pong mode.
        ///   LineBegan   → hurry up (typewriter still running)
        ///   LineWaiting → next line (text fully shown, waiting for player)
        /// </summary>
        private void HandleAdvanceKey()
        {
            // Don't fire on the same frame content arrived — prevents accidental
            // skips when the same key that confirmed an option also starts a line.
            if (frameContentReceived == Time.frameCount)
                return;

            switch (status)
            {
                case PresentationStatus.LineBegan:
                    RequestLineHurryUpInternal();
                    break;

                case PresentationStatus.LineWaiting:
                    RequestNextLine();
                    break;
            }
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        private void RequestLineHurryUpInternal()
        {
            if (frameContentReceived == Time.frameCount)
                return;

            if (status != PresentationStatus.LineBegan && status != PresentationStatus.LineWaiting)
                return;

            numberOfAdvancesThisLine++;

            if (multiAdvanceIsCancel && numberOfAdvancesThisLine >= advanceRequestsBeforeCancellingLine)
                RequestNextLine();
            else if (status == PresentationStatus.LineWaiting)
                RequestNextLine();
            else if (runner != null)
                runner.RequestHurryUpLine();
            else
                Debug.LogError($"{nameof(CustomLineAdvancer)} dialogue runner is null", this);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Hurry up the current line. Safe to call from external code or UI.</summary>
        public void RequestLineHurryUp()
        {
            numberOfAdvancesThisLine++;

            if (multiAdvanceIsCancel && numberOfAdvancesThisLine >= advanceRequestsBeforeCancellingLine)
                RequestNextLine();
            else if (runner != null)
                runner.RequestHurryUpLine();
            else
                Debug.LogError($"{nameof(CustomLineAdvancer)} dialogue runner is null", this);

            
        }

        /// <summary>Hurry up the current options.</summary>
        public void RequestOptionHurryUp()
        {
            if (frameContentReceived == Time.frameCount)
                return;

            if (runner == null)
            {
                Debug.LogError($"{nameof(CustomLineAdvancer)} dialogue runner is null", this);
                return;
            }

            if (status == PresentationStatus.OptionsBegan || status == PresentationStatus.OptionsWaiting)
                runner.RequestHurryUpOption();
        }

        /// <summary>Advance to the next line.</summary>
        public void RequestNextLine()
        {
            ResetLineTracking();
            if (runner != null)
                runner.RequestNextLine();
            else
                Debug.LogError($"{nameof(CustomLineAdvancer)} dialogue runner is null", this);
        }

        /// <summary>Stop the entire dialogue.</summary>
        public void RequestDialogueCancellation()
        {
            ResetLineTracking();
            if (runner != null)
                runner.Stop().Forget();
        }

        private void ResetLineTracking()
        {
            numberOfAdvancesThisLine = 0;
            status = PresentationStatus.Unknown;
        }
    }
}
