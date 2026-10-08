using System.IO;
using System.Text.RegularExpressions;

namespace SudokuRoguelike.Tests
{
    /// <summary>
    /// Reads documents for phrase assertions. Collapses whitespace so a check does not depend on
    /// how a Markdown paragraph happens to be wrapped (GAP-010 triage: re-wrapped docs broke
    /// several readiness tests although the asserted sentences were still present).
    /// </summary>
    internal static class DocText
    {
        private const string EnglishCatalogPath = "Assets/Resources/Localization/en.json";

        public static string Read(string path) => Normalize(File.ReadAllText(path));

        /// <summary>
        /// In-game text lives in the localization catalogs; returns the English catalog as one
        /// normalized string (JSON "\n" escapes count as whitespace).
        /// </summary>
        public static string EnglishCatalog() => Normalize(File.ReadAllText(EnglishCatalogPath).Replace("\\n", " "));

        private static string Normalize(string text) => Regex.Replace(text, @"\s+", " ");
    }
}
