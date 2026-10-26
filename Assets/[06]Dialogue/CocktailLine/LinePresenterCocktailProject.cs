/*
 * LinePresenterCocktailProject.cs
 * Based on LinePresenter.cs from YarnSpinner-Unity (Yarn Spinner licence, see LICENSE.md).
 * Same behaviour as the stock presenter; add Cocktail Project features in the marked spots.
 */

using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;
using Yarn.Unity.Attributes;


#nullable enable

namespace YarnSpinner.Custom
{
    /// <summary>
    /// Presents lines of dialogue using Unity UI elements.
    /// </summary>
    public sealed class LinePresenterCocktailProject : DialoguePresenterBase
    {
        // The stock enum is internal to the Yarn assembly, so we keep our own.
        public enum TypewriterType
        {
            Instant, ByLetter, ByWord, Custom,
        }

        /// <summary>Holds the UI. Its alpha is animated when <see cref="useFadeEffect"/> is on.</summary>
        [Space]
        [MustNotBeNull]
        public CanvasGroup? canvasGroup;

        [MustNotBeNull]
        public TMP_Text? lineText;

        /// <summary>Only used when <see cref="characterNameText"/> is null.</summary>
        [Group("Character")]
        [Label("Shows Name In Line")]
        public bool showCharacterNameInLine = true;

        [Group("Character")]
        [Label("Name Field")]
        public TMP_Text? characterNameText = null;

        /// <summary>Object holding <see cref="characterNameText"/>; defaults to its own GameObject.</summary>
        [Group("Character")]
        public GameObject? characterNameContainer = null;

        [System.Serializable]
        public struct CharacterSpriteEntry
        {
            public string characterName;
            public Sprite characterSprite;
        }

        /// <summary>Image whose sprite is swapped to match the speaking character.</summary>
        [Group("Character Sprite")]
        public UnityEngine.UI.Image? characterImage;

        /// <summary>Matched against the line's character name. No match sets the sprite to null.</summary>
        [Group("Character Sprite")]
        public List<CharacterSpriteEntry> characterSprites = new List<CharacterSpriteEntry>();

        [Group("Fade")]
        [Label("Fade UI")]
        public bool useFadeEffect = true;

        [Group("Fade")]
        [ShowIf(nameof(useFadeEffect))]
        [Tooltip("Consecutive lines from the same character skip the fade out/in.")]
        public bool skipFadeForSameCharacter = true;

        string? lastCharacterName;
        CancellationTokenSource? fadeOutSource;

        [Group("Fade")]
        [ShowIf(nameof(useFadeEffect))]
        public float fadeUpDuration = 0.25f;

        [Group("Fade")]
        [ShowIf(nameof(useFadeEffect))]
        public float fadeDownDuration = 0.1f;

        /// <summary>
        /// If true, the line ends on its own after <see cref="autoAdvanceDelay"/>;
        /// otherwise it waits for the runner to request the next line.
        /// </summary>
        [Group("Automatically Advance Dialogue")]
        public bool autoAdvance = false;

        [Group("Automatically Advance Dialogue")]
        [ShowIf(nameof(autoAdvance))]
        [Label("Delay Before Advancing")]
        public float autoAdvanceDelay = 1f;

        [Group("Typewriter")]
        [SerializeField] TypewriterType typewriterStyle = TypewriterType.ByLetter;

        [Group("Typewriter")]
        [ShowIf(nameof(typewriterStyle), TypewriterType.ByLetter)]
        [Label("Letters per Second")]
        [Min(0)]
        public int lettersPerSecond = 60;

        [Group("Typewriter")]
        [ShowIf(nameof(typewriterStyle), TypewriterType.ByWord)]
        [Label("Words per Second")]
        [Min(0)]
        public int wordsPerSecond = 10;

        [Group("Typewriter")]
        [ShowIf(nameof(typewriterStyle), TypewriterType.Custom)]
        public InterfaceContainer<IAsyncTypewriter>? customTypewriter;

        // Player Settings (Display Type / Text Speed) are applied to the Text Animator typewriter
        // at the start of every line. They only take effect when the typewriter above is a TextAnimatorYarnTypewriter.
        [Group("Typewriter")]
        [Tooltip("Text Animator timings used for Display Type = Character.")]
        [SerializeField] Febucci.TextAnimatorForUnity.TypingsTimingsScriptableBase? characterTimings;

        [Group("Typewriter")]
        [Tooltip("Text Animator timings used for Display Type = Word.")]
        [SerializeField] Febucci.TextAnimatorForUnity.TypingsTimingsScriptableBase? wordTimings;

        /// <summary>Handlers for markers in the line (the pause handler is always added).</summary>
        [Group("Typewriter")]
        [Label("Event Handlers")]
        [SerializeField] List<ActionMarkupHandler> eventHandlers = new List<ActionMarkupHandler>();

        private List<IActionMarkupHandler> ActionMarkupHandlers
        {
            get
            {
                var handlers = new List<IActionMarkupHandler> { new PauseEventProcessor() };
                handlers.AddRange(eventHandlers);
                return handlers;
            }
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            StopFadeOut();
            lastCharacterName = null;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
            }
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueStartedAsync()
        {
            StopFadeOut();
            lastCharacterName = null;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
            }
            return YarnTask.CompletedTask;
        }

        // The box is hidden by fading it out, from several places here and in Yarn's own effects.
        // An invisible box must not catch clicks meant for the bar behind it, so the two are tied
        // together in this one spot instead of at every fade.
        private void LateUpdate()
        {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = canvasGroup.alpha > 0f;
        }

        private void Awake()
        {
            if (characterNameContainer == null && characterNameText != null)
            {
                characterNameContainer = characterNameText.gameObject;
            }

            switch (typewriterStyle)
            {
                case TypewriterType.Instant:
                    Typewriter = new InstantTypewriter()
                    {
                        ActionMarkupHandlers = ActionMarkupHandlers,
                        TextElement = this.lineText,
                    };
                    break;

                case TypewriterType.ByLetter:
                    Typewriter = new LetterTypewriter()
                    {
                        ActionMarkupHandlers = ActionMarkupHandlers,
                        TextElement = this.lineText,
                        CharactersPerSecond = this.lettersPerSecond,
                    };
                    break;

                case TypewriterType.ByWord:
                    Typewriter = new WordTypewriter()
                    {
                        ActionMarkupHandlers = ActionMarkupHandlers,
                        TextElement = this.lineText,
                        WordsPerSecond = this.wordsPerSecond,
                    };
                    break;

                case TypewriterType.Custom:
                    Typewriter = customTypewriter?.Interface;
                    if (Typewriter == null)
                    {
                        Debug.LogWarning("Typewriter mode is set to custom but there is no typewriter set.");
                    }
                    else
                    {
                        Typewriter.ActionMarkupHandlers.AddRange(ActionMarkupHandlers);
                        Typewriter.TextElement = this.lineText;
                    }
                    break;
            }
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (lineText == null)
            {
                Debug.LogError($"{nameof(LinePresenterCocktailProject)} does not have a text view. Skipping line {line.TextID} (\"{line.RawText}\")");
                return;
            }

            DialogueControls.OnLineStarted(line.TextID);

            // Skip-to-order: lines fly past unshown (the Event Log is its own presenter and still gets them),
            // until the line carrying the [order] markup, which plays normally.
            if (DialogueControls.IsSkipping)
            {
                if (line.Text.TryGetAttributeWithName("order", out _)) DialogueControls.StopSkip();
                else return;
            }

            // Same character as the previous line and the UI hasn't fully faded: keep it on screen.
            bool sameCharacter = skipFadeForSameCharacter
                && !string.IsNullOrWhiteSpace(line.CharacterName)
                && line.CharacterName == lastCharacterName
                && canvasGroup != null && canvasGroup.alpha > 0f;
            StopFadeOut();
            lastCharacterName = line.CharacterName;

            MarkupParseResult text;

            if (characterNameText == null)
            {
                text = showCharacterNameInLine ? line.Text : line.TextWithoutCharacterName;
            }
            else
            {
                text = line.TextWithoutCharacterName;

                // Name goes in its own box; hide the box when the line has no character.
                if (characterNameContainer != null)
                {
                    if (string.IsNullOrWhiteSpace(line.CharacterName))
                    {
                        characterNameContainer.SetActive(false);
                    }
                    else
                    {
                        characterNameContainer.SetActive(true);
                        characterNameText.text = line.CharacterName;
                    }
                }
            }

            if (characterImage != null)
            {
                // No match (or no character name) clears the sprite and hides the image (alpha 0).
                Sprite? found = null;
                foreach (var entry in characterSprites)
                {
                    if (entry.characterName == line.CharacterName)
                    {
                        found = entry.characterSprite;
                        break;
                    }
                }
                characterImage.sprite = found;

                var color = characterImage.color;
                color.a = found != null ? 1f : 0f;
                characterImage.color = color;
            }

            Typewriter ??= new InstantTypewriter()
            {
                ActionMarkupHandlers = this.ActionMarkupHandlers,
                TextElement = this.lineText,
            };

            ApplyTextSettings();
            Typewriter.PrepareForContent(text);

            if (canvasGroup != null)
            {
                if (sameCharacter)
                {
                    canvasGroup.alpha = 1;
                }
                else if (useFadeEffect)
                {
                    await Effects.FadeAlphaAsync(canvasGroup, 0, 1, fadeUpDuration, token.HurryUpToken);
                }
                else
                {
                    canvasGroup.alpha = 1;
                }
            }

            await RunTypewriterAsync(text, token.HurryUpToken);

            if (autoAdvance)
            {
                await YarnTask.Delay((int)(autoAdvanceDelay * 1000), token.NextContentToken).SuppressCancellationThrow();
            }
            else
            {
                // Wait for input. AutoPlay advances after the settings delay; the timer runs on
                // Time.deltaTime, so it freezes while the game is paused.
                float waited = 0f;
                while (!token.IsNextContentRequested)
                {
                    if (DialogueControls.AutoPlay)
                    {
                        waited += Time.deltaTime;
                        if (waited >= DialogueSettings.AutoPlayDelay(text.Text.Length))
                        {
                            DialogueControls.RequestNextLine();
                            break;
                        }
                    }
                    else waited = 0f;
                    await YarnTask.Yield();
                }
            }

            Typewriter.ContentWillDismiss();

            if (canvasGroup != null && useFadeEffect && skipFadeForSameCharacter)
            {
                // Fade out in the background so a following line from the same character can cancel it.
                fadeOutSource = new CancellationTokenSource();
                FadeOutInBackground(fadeOutSource).Forget();
                return;
            }

            if (canvasGroup != null)
            {
                if (useFadeEffect)
                {
                    await Effects.FadeAlphaAsync(canvasGroup, 1, 0, fadeDownDuration, token.HurryUpToken).SuppressCancellationThrow();
                }
                else
                {
                    canvasGroup.alpha = 0;
                }
            }

            Typewriter.ContentDidDismiss();
        }

        Yarn.Unity.Addons.TextAnimatorIntegration.TextAnimatorYarnTypewriter? textAnimatorTypewriter;

        // Re-applied every line: it is cheap, and it keeps a line that is typing from being changed mid-way.
        void ApplyTextSettings()
        {
            if (Typewriter is not Yarn.Unity.Addons.TextAnimatorIntegration.TextAnimatorYarnTypewriter ta) return;

            if (textAnimatorTypewriter != ta)
            {
                textAnimatorTypewriter = ta;
                // Text Animator resets its speed to 1x when typing starts, so the speed is set from this event.
                ta.onTypewriterStart.AddListener(ApplyTypewriterSpeed);
            }

            var timings = DialogueSettings.Display == TextDisplay.Word ? wordTimings : characterTimings;
            if (timings != null) ta.TimingSettings = timings;
        }

        void ApplyTypewriterSpeed()
        {
            if (textAnimatorTypewriter == null) return;
            textAnimatorTypewriter.SetTypewriterSpeed(DialogueSettings.SpeedMultiplier);
        }

        void OnDestroy()
        {
            if (textAnimatorTypewriter != null) textAnimatorTypewriter.onTypewriterStart.RemoveListener(ApplyTypewriterSpeed);
        }

        async YarnTask RunTypewriterAsync(MarkupParseResult text, CancellationToken hurryUp)
        {
            // Package bug: TextAnimatorYarnTypewriter only finishes a line from OnAfterWaitingCharacter of the
            // last character, which never fires with "Delays By Word". So in Word mode we also end the line
            // when Text Animator reports the text as shown (cancelling takes the normal hurry-up finish path).
            var display = DialogueSettings.Display;
            if (textAnimatorTypewriter == null || display == TextDisplay.Character)
            {
                await Typewriter!.RunTypewriter(text, hurryUp).SuppressCancellationThrow();
                return;
            }

            var ta = textAnimatorTypewriter;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(hurryUp);
            // Instant = hurry-up from the first frame: the typewriter's own skip path shows the whole line at once.
            if (display == TextDisplay.Instant) linked.Cancel();
            UnityEngine.Events.UnityAction onShown = linked.Cancel;
            ta.onTextShowed.AddListener(onShown);
            try
            {
                await Typewriter!.RunTypewriter(text, linked.Token).SuppressCancellationThrow();
            }
            finally
            {
                ta.onTextShowed.RemoveListener(onShown);
            }
        }

        async YarnTask FadeOutInBackground(CancellationTokenSource source)
        {
            var token = source.Token;
            float start = canvasGroup!.alpha;
            float elapsed = 0f;
            while (!token.IsCancellationRequested && elapsed < fadeDownDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, 0f, elapsed / fadeDownDuration);
                await YarnTask.Yield();
            }

            // Cancelled: the next line took over and already ran ContentDidDismiss.
            if (token.IsCancellationRequested) return;

            canvasGroup.alpha = 0f;
            fadeOutSource = null;
            Typewriter?.ContentDidDismiss();
        }

        /// <summary>Cancels a pending background fade-out and finishes the dismissal it skipped.</summary>
        void StopFadeOut()
        {
            if (fadeOutSource == null) return;
            fadeOutSource.Cancel();
            fadeOutSource = null;
            Typewriter?.ContentDidDismiss();
        }
    }
}
