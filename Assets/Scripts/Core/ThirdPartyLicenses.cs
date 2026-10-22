using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SudokuRoguelike.Core
{
    /// <summary>
    /// [REQ: LEGAL-LICENSE-001] The licence texts that must travel with the game (GAP-068, docs/legal/legal-check-2026-10.md):
    /// the Roboto font's attribution and the Apache 2.0 text, the MIT licence of Steamworks.NET. They ship twice: as
    /// TextAssets in Resources/Legal, shown on the Credits screen, and as files in the build's Licenses folder (copied by the
    /// editor build step, LicenseFilesBuildStep). The sources are the copies in docs/legal/third-party.
    /// </summary>
    public static class ThirdPartyLicenses
    {
        public const string SourceFolder = "docs/legal/third-party";
        public const string BuildFolderName = "Licenses";

        /// <summary>One licence: the Credits role key, its Resources path and its source file in docs/legal/third-party.</summary>
        public readonly struct Entry
        {
            public readonly string RoleKey;
            public readonly string ResourcePath;
            public readonly string SourceFile;

            public Entry(string roleKey, string resourcePath, string sourceFile)
            {
                RoleKey = roleKey;
                ResourcePath = resourcePath;
                SourceFile = sourceFile;
            }
        }

        public static readonly IReadOnlyList<Entry> All = new[]
        {
            new Entry("Credits.Role.License.RobotoNotice", "Legal/Roboto-ATTRIBUTION", "Roboto-ATTRIBUTION.txt"),
            new Entry("Credits.Role.License.Apache", "Legal/APACHE-2.0", "APACHE-2.0.txt"),
            new Entry("Credits.Role.License.Steamworks", "Legal/Steamworks.NET-LICENSE", "Steamworks.NET-LICENSE.md")
        };

        /// <summary>The licence text bundled with the game, or an empty string when the resource is missing.</summary>
        public static string Load(Entry entry)
        {
            var asset = Resources.Load<TextAsset>(entry.ResourcePath);
            return asset != null ? asset.text.Replace("\r\n", "\n").TrimEnd() : string.Empty;
        }

        /// <summary>Copies every licence file from <paramref name="sourceFolder"/> into the build's Licenses folder.</summary>
        /// <returns>The files copied.</returns>
        public static int CopyToBuild(string sourceFolder, string buildFolder)
        {
            var target = Path.Combine(buildFolder, BuildFolderName);
            Directory.CreateDirectory(target);
            var copied = 0;
            foreach (var entry in All)
            {
                var source = Path.Combine(sourceFolder, entry.SourceFile);
                if (!File.Exists(source))
                    throw new FileNotFoundException("A licence text the build must carry is missing (GAP-068).", source);
                File.Copy(source, Path.Combine(target, entry.SourceFile), true);
                copied++;
            }
            return copied;
        }
    }
}
