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

            await Typewriter.RunTypewriter(text, token.HurryUpToken).SuppressCancellationThrow();

            if (autoAdvance)
            {
                await YarnTask.Delay((int)(autoAdvanceDelay * 1000), token.NextContentToken).SuppressCancellationThrow();
            }
            else
            {
                await YarnTask.WaitUntilCanceled(token.NextContentToken).SuppressCancellationThrow();
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
