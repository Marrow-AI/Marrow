namespace RedBlueGames.Tools.TextTyper.Tests
{
    using UnityEngine;
    using UnityEditor;
    using NUnit.Framework;

    public class RichTextTagTests
    {
        [Test]
        public void Constructor_OpeningTag_Parses()
        {
            //Arrange
            var tag = "<b>";

            //Act
            var richTextTag = new RichTextTag(tag);

            //Assert
            Assert.AreEqual(tag, richTextTag.TagText);
            Assert.AreEqual("b", richTextTag.TagType);
            Assert.IsFalse(richTextTag.IsClosingTag);
            Assert.AreEqual("</b>", richTextTag.ClosingTagText);
            Assert.AreEqual(string.Empty, richTextTag.Parameter);
        }

        [Test]
        public void Constructor_TagAndParameter_Parses()
        {
            //Arrange
            var tag = "<color=#FFFFFFFF>";

            //Act
            var richTextTag = new RichTextTag(tag);

            //Assert
            Assert.AreEqual(tag, richTextTag.TagText);
            Assert.AreEqual("color", richTextTag.TagType);
            Assert.IsFalse(richTextTag.IsClosingTag);
            Assert.AreEqual("</color>", richTextTag.ClosingTagText);
            Assert.AreEqual("#FFFFFFFF", richTextTag.Parameter);
        }

        [Test]
        public void Constructor_ClosingTag_Parses()
        {
            //Arrange
            var tag = "</color>";

            //Act
            var richTextTag = new RichTextTag(tag);

            //Assert
            Assert.AreEqual(tag, richTextTag.TagText);
            Assert.AreEqual("color", richTextTag.TagType);
            Assert.IsTrue(richTextTag.IsClosingTag);
            Assert.AreEqual("</color>", richTextTag.ClosingTagText);
            Assert.AreEqual(string.Empty, richTextTag.Parameter);
        }
    }
}