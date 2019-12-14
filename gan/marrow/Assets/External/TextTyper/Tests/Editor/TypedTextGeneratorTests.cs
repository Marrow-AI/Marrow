namespace RedBlueGames.Tools.TextTyper.Tests
{
    using UnityEditor;
    using UnityEngine;
    using NUnit.Framework;

    public class TypedTextGeneratorTests
    {
        [Test]
        public void GetText_EmptyString_ReturnsEmptyAndCompleted()
        {
            var textToType = string.Empty;
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 8);

            var expectedText = textToType;

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
            Assert.AreEqual(char.MinValue, generatedText.LastPrintedChar);
            Assert.AreEqual(true, generatedText.IsComplete);
            Assert.AreEqual(0.0, generatedText.Delay);
        }

        [Test]
        public void GetText_OnlyUnityRichTextTags_ReturnsEmptyAndCompleted()
        {
            var textToType = "<b><i></i></b>";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 1);

            var expectedText = textToType;

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
            Assert.AreEqual(char.MinValue, generatedText.LastPrintedChar);
            Assert.AreEqual(true, generatedText.IsComplete);
            Assert.AreEqual(0.0, generatedText.Delay);
        }

        [Test]
        public void GetText_OnlyCustomRichTextTags_ReturnsEmptyAndCompleted()
        {
            var textToType = "<delay=5></delay>";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 1);

            var expectedText = string.Empty;

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
            Assert.AreEqual(char.MinValue, generatedText.LastPrintedChar);
            Assert.AreEqual(true, generatedText.IsComplete);
            Assert.AreEqual(0.0, generatedText.Delay);
        }

        [Test]
        public void GetText_VisibleCharIndexOutOfBounds_ReturnsFullString()
        {
            var textToType = "Hello world";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 9999);

            var expectedText = textToType;

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void GetText_NoTagsFirstChar_ShowsFirstChar()
        {
            var textToType = "Hello world";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 0);

            var expectedText = string.Concat("H", RichTextTag.ClearColorTag, "ello world", RichTextTag.ClearColorTag.ClosingTagText);

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void GetText_NoTagsLastChar_ReturnsFullString()
        {
            var textToType = "Hello world";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, textToType.Length - 1);

            var expectedText = textToType;

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void GetText_NoTagsNextToLastChar_ShowsAllButLast()
        {
            var textToType = "Hello world";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, textToType.Length - 2);

            var expectedText = string.Concat("Hello worl", RichTextTag.ClearColorTag, "d", RichTextTag.ClearColorTag.ClosingTagText);

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void GetText_BeforeTagsAtEnd_CorrectlyHidesTags()
        {
            var textToType = "Hello <b>world</b>";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 1);

            var expectedText = string.Concat("He", RichTextTag.ClearColorTag, "llo <b>world</b>", RichTextTag.ClearColorTag.ClosingTagText);

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void GetText_MiddleOfTag_HidesTags()
        {
            var textToType = "Hello <size=40>world</size>";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 8);

            var expectedText = string.Concat(
                                   "Hello <size=40>wor</size>",
                                   RichTextTag.ClearColorTag,
                                   "ld",
                                   RichTextTag.ClearColorTag.ClosingTagText);

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void GetText_AfterTags_ShowsTags()
        {
            var textToType = "<size=40>Hello</size> world";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 8);

            var expectedText = string.Concat(
                                   "<size=40>Hello</size> wor",
                                   RichTextTag.ClearColorTag,
                                   "ld",
                                   RichTextTag.ClearColorTag.ClosingTagText);

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void GetText_AllUnityTags_PrintsCorrectly()
        {
            var textToType = "<size=40><b><i><color=red>Hello world</color></i></b></size>";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 8);

            var expectedText = string.Concat(
                                   "<size=40><b><i><color=red>Hello wor</color></i></b></size>",
                                   RichTextTag.ClearColorTag,
                                   "ld",
                                   RichTextTag.ClearColorTag.ClosingTagText);

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void GetText_BeforeColorTags_HidesAllTags()
        {
            var textToType = "Hello <color=green>world</color>";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 3);

            var expectedText = string.Concat(
                                   "Hell",
                                   RichTextTag.ClearColorTag,
                                   "o world",
                                   RichTextTag.ClearColorTag.ClosingTagText);

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void GetText_AfterColorTags_ShowsAllTags()
        {
            var textToType = "<color=red>Hello</color> world";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 8);

            var expectedText = string.Concat(
                                   "<color=red>Hello</color> wor",
                                   RichTextTag.ClearColorTag,
                                   "ld",
                                   RichTextTag.ClearColorTag.ClosingTagText);

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void GetText_MiddleOfColorTags_PrintsCorrectly()
        {
            var textToType = "<color=red>Hello</color> <color=green>world</color>";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 8);

            var expectedText = string.Concat(
                                   "<color=red>Hello</color> <color=green>wor</color>",
                                   RichTextTag.ClearColorTag,
                                   "ld",
                                   RichTextTag.ClearColorTag.ClosingTagText);

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void GetText_ExactlyOnColorTagEnd_PrintsCorrectly()
        {
            var textToType = "<color=red>Hello</color> <color=green>world</color>";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 4);

            var expectedText = string.Concat(
                                   "<color=red>Hello</color>",
                                   RichTextTag.ClearColorTag,
                                   " world",
                                   RichTextTag.ClearColorTag.ClosingTagText);

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void GetText_ExactlyOnColorTagStart_PrintsCorrectly()
        {
            var textToType = "<color=red>Hello</color> <color=green>world</color>";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 6);

            var expectedText = string.Concat(
                                   "<color=red>Hello</color> <color=green>w</color>",
                                   RichTextTag.ClearColorTag,
                                   "orld",
                                   RichTextTag.ClearColorTag.ClosingTagText);

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void GetText_IncludeCustomTags_RemovesCustomTags()
        {
            var textToType = "<delay=0.5>Hello</delay> world";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 8);

            var expectedText = string.Concat(
                                   "Hello wor",
                                   RichTextTag.ClearColorTag,
                                   "ld",
                                   RichTextTag.ClearColorTag.ClosingTagText);

            Assert.AreEqual(expectedText, generatedText.TextToPrint);
        }

        [Test]
        public void Delay_DelayTagIsActive_ReturnsCorrectDelay()
        {
            var textToType = "<delay=0.5>Hello</delay> world";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 4);

            Assert.AreEqual(0.5f, generatedText.Delay);
        }

        [Test]
        public void Delay_DelayTagIsNotActive_ReturnsNoDelay()
        {
            var textToType = "<delay=0.5>Hello</delay> world";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 8);

            Assert.AreEqual(0.0, generatedText.Delay);
        }

        [Test]
        public void LastPrintedCharacter_FirstLetter_ReturnsFirst()
        {
            var textToType = "Hello <b>world</b>";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 0);

            Assert.AreEqual('H', generatedText.LastPrintedChar);
        }

        [Test]
        public void LastPrintedCharacter_SecondLetter_ReturnsSecond()
        {
            var textToType = "Hello <b>world</b>";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 1);

            Assert.AreEqual('e', generatedText.LastPrintedChar);
        }

        [Test]
        public void LastPrintedCharacter_JustAfterTag_ReturnsChar()
        {
            var textToType = "<b>Hello</b> world";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 5);

            Assert.AreEqual(' ', generatedText.LastPrintedChar);
        }

        [Test]
        public void LastPrintedCharacter_LastCharacterIsTag_ReturnsNonTagChar()
        {
            var textToType = "Hello <b>world</b>";
            var generator = new TypedTextGenerator();
            var generatedText = generator.GetTypedTextAt(textToType, 11);

            Assert.AreEqual('d', generatedText.LastPrintedChar);
        }
    }
}