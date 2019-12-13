namespace RedBlueGames.Tools.TextTyper
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    /// <summary>
    /// Typed text generator is used to create TypedText results given a text that should be printed one character
    /// at a time, up to the specified character.
    /// </summary>
    public sealed class TypedTextGenerator
    {
        private static readonly List<string> UnityTagTypes = new List<string> { "b", "i", "size", "color" };
        private static readonly List<string> CustomTagTypes = new List<string> { "delay" };

        /// <summary>
        /// Gets Completed TypedText from the specified text string.
        /// </summary>
        /// <returns>The completed text, as it should display in Unity.</returns>
        /// <param name="text">Text to complete.</param>
        public TypedText GetCompletedText(string text)
        {
            var printText = RemoveCustomTags(text);

            var typedText = new TypedText();
            typedText.TextToPrint = printText;
            typedText.Delay = 0.0f;
            typedText.LastPrintedChar = printText[printText.Length - 1];
            typedText.IsComplete = true;

            return typedText;
        }

        /// <summary>
        /// Gets the typed text at the specified visibleCharacterIndex. This is the text that should be written
        /// to the Text component.
        /// </summary>
        /// <returns>The <see cref="TypedText"/> generated at the specified visible character index.</returns>
        /// <param name="text">Text to parse.</param>
        /// <param name="visibleCharacterIndex">Visible character index (ignores tags).</param>
        public TypedText GetTypedTextAt(string text, int visibleCharacterIndex)
        {
            var textAsSymbolList = CreateSymbolListFromText(text);

            // Split the text into shown and hide strings based on the actual visible characters
            int printedCharCount = 0;
            var shownText = string.Empty;
            var hiddenText = string.Empty;
            var lastVisibleCharacter = char.MinValue;
            foreach (var symbol in textAsSymbolList)
            {
                if (printedCharCount <= visibleCharacterIndex)
                {
                    shownText += symbol.Text;

                    // Keep track of the visible characters that have been printed
                    if (!symbol.IsTag)
                    {
                        lastVisibleCharacter = symbol.Character.ToCharArray()[0];
                    }
                }
                else
                {
                    hiddenText += symbol.Text;
                }

                if (!symbol.IsTag)
                {
                    printedCharCount++;
                }
            }

            var activeTags = GetActiveTagsInSymbolList(textAsSymbolList, visibleCharacterIndex);

            // Remove closing tags for active tags from hidden text (move to before color hide tag)
            foreach (var activeTag in activeTags)
            {
                hiddenText = RemoveFirstOccurance(hiddenText, activeTag.ClosingTagText);
            }

            // Remove all color tags from hidden text so that they don't cause it to be shown
            // ex: <color=clear>This should <color=red>be clear</color></color> will show 'be clear" in red
            hiddenText = RichTextTag.RemoveTagsFromString(hiddenText, "color");

            // Add the hidden text, provided there is text to hide
            if (!string.IsNullOrEmpty(hiddenText))
            {
                var hiddenTag = RichTextTag.ClearColorTag;
                hiddenText = hiddenText.Insert(0, hiddenTag.TagText);
                hiddenText = hiddenText.Insert(hiddenText.Length, hiddenTag.ClosingTagText);
            }

            // Add back in closing tags in reverse order
            for (int i = 0; i < activeTags.Count; ++i)
            {
                hiddenText = hiddenText.Insert(0, activeTags[i].ClosingTagText);
            }

            // Remove all custom tags since Unity will display them when printed (it doesn't recognize them as rich text tags)
            var printText = shownText + hiddenText;
            foreach (var customTag in CustomTagTypes)
            {
                printText = RichTextTag.RemoveTagsFromString(printText, customTag);
            }

            // Calculate Delay, if active
            var delay = 0.0f;
            foreach (var activeTag in activeTags)
            {
                if (activeTag.TagType == "delay")
                {
                    try
                    {
                        delay = activeTag.IsOpeningTag ? float.Parse(activeTag.Parameter) : 0.0f;
                    }
                    catch (System.FormatException e)
                    {
                        var warning = string.Format(
                                          "TypedTextGenerator found Invalid paramter format in tag [{0}]. " +
                                          "Parameter [{1}] does not parse to a float. Exception: {2}",
                                          activeTag,
                                          activeTag.Parameter,
                                          e);
                        Debug.Log(warning);
                        delay = 0.0f;
                    }
                }
            }

            var typedText = new TypedText();
            typedText.TextToPrint = printText;
            typedText.Delay = delay;
            typedText.LastPrintedChar = lastVisibleCharacter;
            typedText.IsComplete = string.IsNullOrEmpty(hiddenText);

            return typedText;
        }

        private static List<RichTextTag> GetActiveTagsInSymbolList(List<TypedTextSymbol> symbolList, int visibleCharacterPosition)
        {
            var activeTags = new List<RichTextTag>();
            int printableCharacterCount = 0;
            foreach (var symbol in symbolList)
            {
                if (symbol.IsTag)
                {
                    if (symbol.Tag.IsOpeningTag)
                    {
                        activeTags.Add(symbol.Tag);
                    }
                    else
                    {
                        var poppedTag = activeTags[activeTags.Count - 1];
                        if (poppedTag.TagType != symbol.Tag.TagType)
                        {
                            var errorMessage = string.Format(
                                                   "TypedTextGenerator Popped TagType [{0}] that did not match last outstanding tagType [{1}]" +
                                                   ". Unity only respects tags that are added and closed as a stack.",
                                                   poppedTag.TagType,
                                                   symbol.Tag.TagType);
                            Debug.LogError(errorMessage);
                        }

                        activeTags.RemoveAt(activeTags.Count - 1);
                    }
                }
                else
                {
                    printableCharacterCount++;

                    // Finished when we've passed the visibleCharacter (non-Tag) position
                    if (printableCharacterCount > visibleCharacterPosition)
                    {
                        break;
                    }
                }
            }

            return activeTags;
        }

        private static List<TypedTextSymbol> CreateSymbolListFromText(string text)
        {
            var symbolList = new List<TypedTextSymbol>();
            int parsedCharacters = 0;
            while (parsedCharacters < text.Length)
            {
                TypedTextSymbol symbol = null;

                // Check for tags
                var remainingText = text.Substring(parsedCharacters, text.Length - parsedCharacters);
                if (RichTextTag.StringStartsWithTag(remainingText))
                {
                    var tag = RichTextTag.ParseNext(remainingText);
                    symbol = new TypedTextSymbol(tag);
                }
                else
                {
                    symbol = new TypedTextSymbol(remainingText.Substring(0, 1));
                }

                parsedCharacters += symbol.Length;
                symbolList.Add(symbol);
            }

            return symbolList;
        }

        private static char GetLastVisibleCharInSymbolList(List<TypedTextSymbol> symbolList)
        {
            for (int i = symbolList.Count - 1; i >= 0; --i)
            {
                var symbol = symbolList[i];
                if (!symbol.IsTag)
                {
                    return symbol.Character.ToCharArray()[0];
                }
            }

            return char.MinValue;
        }

        private static string RemoveAllTags(string textWithTags)
        {
            string textWithoutTags = textWithTags;
            textWithoutTags = RemoveUnityTags(textWithoutTags);
            textWithoutTags = RemoveCustomTags(textWithoutTags);

            return textWithoutTags;
        }

        private static string RemoveCustomTags(string textWithTags)
        {
            string textWithoutTags = textWithTags;
            foreach (var customTag in CustomTagTypes)
            {
                textWithoutTags = RichTextTag.RemoveTagsFromString(textWithoutTags, customTag);
            }

            return textWithoutTags;
        }

        private static string RemoveUnityTags(string textWithTags)
        {
            string textWithoutTags = textWithTags;
            foreach (var unityTag in UnityTagTypes)
            {
                textWithoutTags = RichTextTag.RemoveTagsFromString(textWithoutTags, unityTag);
            }

            return textWithoutTags;
        }

        private static string RemoveFirstOccurance(string source, string searchString)
        {
            var index = source.IndexOf(searchString);
            if (index >= 0)
            {
                return source.Remove(index, searchString.Length);
            }
            else
            {
                return source;
            }
        }

        /// <summary>
        /// TypedText represents results from the TypedTextGenerator
        /// </summary>
        public class TypedText
        {
            /// <summary>
            /// Gets or sets the text to print to the Text component. This is what is visible to the user.
            /// </summary>
            /// <value>The text to print.</value>
            public string TextToPrint { get; set; }

            /// <summary>
            /// Gets or sets the desired Delay based on the delay tags in the typed text.
            /// </summary>
            /// <value>The delay.</value>
            public float Delay { get; set; }

            /// <summary>
            /// Gets or sets the last printed char.
            /// </summary>
            /// <value>The last printed char.</value>
            public char LastPrintedChar { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this instance is complete and has printed all its characters)
            /// </summary>
            /// <value><c>true</c> if this instance is complete; otherwise, <c>false</c>.</value>
            public bool IsComplete { get; set; }
        }

        private class TypedTextSymbol
        {
            public TypedTextSymbol(string character)
            {
                this.Character = character;
            }

            public TypedTextSymbol(RichTextTag tag)
            {
                this.Tag = tag;
            }

            public string Character { get; private set; }

            public RichTextTag Tag { get; private set; }

            public int Length
            {
                get
                {
                    return this.Text.Length;
                }
            }

            public string Text
            {
                get
                {
                    if (this.IsTag)
                    {
                        return this.Tag.TagText;
                    }
                    else
                    {
                        return this.Character;
                    }
                }
            }

            public bool IsTag
            {
                get
                {
                    return this.Tag != null;
                }
            }
        }
    }
}