using NUnit.Framework;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    [Requirement("CREDITS-COMMUNITY-001")]
    public sealed class CommunityCreditsTests
    {
        [Test]
        public void Names_AreSortedAndTranslatorsShowTheirLanguage()
        {
            var credits = CommunityCredits.Parse(
                "{\"discord\":\"https://discord.gg/x\",\"playtesters\":[\"zoe\",\"Ana\",\"\"]," +
                "\"translators\":[{\"name\":\"Jean\",\"language\":\"fr\"},{\"name\":\"Bea\",\"language\":\"\"}]}");
            Assert.IsTrue(credits.HasNames);
            Assert.AreEqual("Ana, zoe", credits.PlaytesterLine());
            Assert.AreEqual("Bea, Jean (fr)", credits.TranslatorLine());
            Assert.AreEqual("https://discord.gg/x", credits.discord);
        }

        [Test]
        public void EmptyOrMissingData_GivesNoNames()
        {
            Assert.IsFalse(CommunityCredits.Parse("").HasNames);
            Assert.IsFalse(CommunityCredits.Parse("{\"playtesters\":[]}").HasNames);
        }

        [Test]
        public void TheShippedFile_Loads_AndLinksTheDiscord()
        {
            Assert.NotNull(Resources.Load<TextAsset>(CommunityCredits.ResourcePath), "Resources/credits_community.json");
            StringAssert.StartsWith("https://discord.gg/", CommunityCredits.Load().discord);
        }
    }
}
