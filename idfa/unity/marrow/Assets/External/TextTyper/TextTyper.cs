namespace RedBlueGames.Tools.TextTyper
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    /// <summary>
    /// Type text component types out Text one character at a time. Heavily adapted from synchrok's GitHub project.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TextTyper : MonoBehaviour
    {
        /// <summary>
        /// The print delay setting. Could make this an option some day, for fast readers.
        /// </summary>
        private const float PrintDelaySetting = 0.02f;

        // Characters that are considered punctuation in this language. TextTyper pauses on these characters
        // a bit longer by default. Could be a setting sometime since this doesn't localize.
        private readonly List<char> punctutationCharacters = new List<char>
        {
            '.',
            ',',
            '!',
            '?'
        };

        [SerializeField]
        [Tooltip("Event that's called when the text has finished printing.")]
        private UnityEvent printCompleted = new UnityEvent();

        [SerializeField]
        [Tooltip("Event called when a character is printed. Inteded for audio callbacks.")]
        private CharacterPrintedEvent characterPrinted = new CharacterPrintedEvent();

        private TextMeshProUGUI textComponent;
        private string printingText;
        private float defaultPrintDelay;
        private Coroutine typeTextCoroutine;

        /// <summary>
        /// Gets the PrintCompleted callback event.
        /// </summary>
        /// <value>The print completed callback event.</value>
        public UnityEvent PrintCompleted
        {
            get
            {
                return this.printCompleted;
            }
        }

        /// <summary>
        /// Gets the CharacterPrinted event, which includes a string for the character that was printed.
        /// </summary>
        /// <value>The character printed event.</value>
        public CharacterPrintedEvent CharacterPrinted
        {
            get
            {
                return this.characterPrinted;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="TextTyper"/> is currently printing text.
        /// </summary>
        /// <value><c>true</c> if printing; otherwise, <c>false</c>.</value>
        public bool IsTyping
        {
            get
            {
                return this.typeTextCoroutine != null;
            }
        }

        private TextMeshProUGUI TextComponent
        {
            get
            {
                if (this.textComponent == null)
                {
                    this.textComponent = this.GetComponent<TextMeshProUGUI>();
                }

                return this.textComponent;
            }
        }

        /// <summary>
        /// Types the text into the Text component character by character, using the specified (optional) print delay per character.
        /// </summary>
        /// <param name="text">Text to type.</param>
        /// <param name="printDelay">Print delay (in seconds) per character.</param>
        public void TypeText(string text, float printDelay = -1)
        {
            this.Cleanup();

            this.defaultPrintDelay = printDelay > 0 ? printDelay : PrintDelaySetting;
            this.printingText = text;

            this.typeTextCoroutine = this.StartCoroutine(this.TypeTextCharByChar(text));
        }

        /// <summary>
        /// Skips the typing to the end.
        /// </summary>
        public void Skip()
        {
            this.Cleanup();

            var generator = new TypedTextGenerator();
            var typedText = generator.GetCompletedText(this.printingText);
            this.TextComponent.text = typedText.TextToPrint;

            this.OnTypewritingComplete();
        }

        /// <summary>
        /// Determines whether this instance is skippable.
        /// </summary>
        /// <returns><c>true</c> if this instance is skippable; otherwise, <c>false</c>.</returns>
        public bool IsSkippable()
        {
            // For now there's no way to configure this. Just make sure it's currently typing.
            return this.IsTyping;
        }

        private void Cleanup()
        {
            if (this.typeTextCoroutine != null)
            {
                this.StopCoroutine(this.typeTextCoroutine);
                this.typeTextCoroutine = null;
            }
        }

        private IEnumerator TypeTextCharByChar(string text)
        {
            this.TextComponent.text = string.Empty;

            var generator = new TypedTextGenerator();
            TypedTextGenerator.TypedText typedText;
            int printedCharCount = 0;
            do
            {
                typedText = generator.GetTypedTextAt(text, printedCharCount);
                this.TextComponent.text = typedText.TextToPrint;
                this.OnCharacterPrinted(typedText.LastPrintedChar.ToString());

                ++printedCharCount;

                var delay = typedText.Delay > 0 ? typedText.Delay : this.GetPrintDelayForCharacter(typedText.LastPrintedChar);
                yield return new WaitForSeconds(delay);
            }
            while (!typedText.IsComplete);

            this.typeTextCoroutine = null;
            this.OnTypewritingComplete();
        }

        private float GetPrintDelayForCharacter(char characterToPrint)
        {
            // Then get the default print delay for the current character
            float punctuationDelay = this.defaultPrintDelay * 8.0f;
            if (this.punctutationCharacters.Contains(characterToPrint))
            {
                return punctuationDelay;
            }
            else
            {
                return this.defaultPrintDelay;
            }
        }

        private void OnCharacterPrinted(string printedCharacter)
        {
            if (this.CharacterPrinted != null)
            {
                this.CharacterPrinted.Invoke(printedCharacter);
            }
        }

        private void OnTypewritingComplete()
        {
            if (this.PrintCompleted != null)
            {
                this.PrintCompleted.Invoke();
            }
        }

        /// <summary>
        /// Event that signals a Character has been printed to the Text component.
        /// </summary>
        [System.Serializable]
        public class CharacterPrintedEvent : UnityEvent<string>
        {
        }
    }
}
