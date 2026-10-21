using System.Globalization;
using System.Threading;
using NUnit.Framework;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Tests
{
    // [REQ: LOC-NUM-001] Numbers in texts follow the language the player chose, not the PC's region (GAP-065: on a German
    // Windows the English Game Modes screen said "XP x1,0").
    [TestFixture]
    [Requirement("LOC-NUM-001")]
    public sealed class LocalizedNumberTests
    {
        private CultureInfo _savedCulture;

        [SetUp]
        public void SetUp()
        {
            _savedCulture = Thread.CurrentThread.CurrentCulture;
        }

        [TearDown]
        public void TearDown()
        {
            Thread.CurrentThread.CurrentCulture = _savedCulture;
            LocalizationService.SetLanguage(LanguageOption.English);
        }

        [Test]
        public void English_OnAGermanPc_UsesADecimalPointAndCommaGroups()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            LocalizationService.SetLanguage(LanguageOption.English);

            Assert.AreEqual("XP x1.5 · 50,000", LocalizationService.Format("Test.NoSuchKey", "XP x{0:0.0} · {1:N0}", 1.5f, 50000));
        }

        [Test]
        public void German_OnAnEnglishPc_UsesADecimalCommaAndPointGroups()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            LocalizationService.SetLanguage(LanguageOption.German);

            Assert.AreEqual("1,5 · 50.000", LocalizationService.Format("Test.NoSuchKey", "{0:0.0} · {1:N0}", 1.5f, 50000));
        }

        [Test]
        public void French_GroupsWithTheNoBreakSpaceTheFontHas()
        {
            LocalizationService.SetLanguage(LanguageOption.French);

            var text = LocalizationService.Format("Test.NoSuchKey", "{0:N0}", 50000);

            Assert.AreEqual("50 000", text, "U+00A0, not the narrow U+202F the game font lacks");
        }

        [TestCase(LanguageOption.English, "1.5")]
        [TestCase(LanguageOption.German, "1,5")]
        [TestCase(LanguageOption.French, "1,5")]
        [TestCase(LanguageOption.Spanish, "1,5")]
        [TestCase(LanguageOption.BrazilianPortuguese, "1,5")]
        public void EveryLanguage_HasItsDecimalSeparator(LanguageOption language, string expected)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            LocalizationService.SetLanguage(language);

            Assert.AreEqual(expected, 1.5f.ToString("0.0", LocalizationService.Culture));
        }

        [Test]
        public void FormatPlural_FollowsTheLanguageToo()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            LocalizationService.SetLanguage(LanguageOption.English);

            Assert.AreEqual("1,234 points", LocalizationService.FormatPlural(1234, "Test.One", "Test.Many", "{0:N0} point", "{0:N0} points"));
        }
    }
}
