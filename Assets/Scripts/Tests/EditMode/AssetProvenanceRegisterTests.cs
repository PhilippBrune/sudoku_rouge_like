using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class AssetProvenanceRegisterTests
    {
        private const string ResourcesRoot = "Assets/Resources";
        private const string RegisterPath = "docs/legal/asset-provenance-register.md";

        private static readonly Dictionary<string, HashSet<string>> RegisteredExtensions =
            new Dictionary<string, HashSet<string>>
            {
                { string.Empty, Set(".json") },
                { "audio", Set(".wav", ".mp3", ".ogg", ".aiff", ".flac") },
                { "background", Set(".png") },
                { "boss", Set(".png") },
                { "class", Set(".png") },
                { "cursed", Set(".png") },
                { "debuff", Set(".png") },
                { "economy", Set(".png") },
                { "Fonts", Set(".ttf", ".otf", ".ttc", ".fontsettings") },
                { "GeneratedIcons", Set(".csv") },
                { "items", Set(".png") },
                { "legendary", Set(".png") },
                { "meta", Set(".png") },
                { "modifier", Set(".png") },
                { "node", Set(".png") },
                { "relic", Set(".png") },
                { "ui", Set(".png") }
            };

        private static readonly string[] RegisteredPromptFiles =
        {
            "docs/art-source/background/AI_prompts.txt",
            "docs/art-source/background/AI_prompts_2.txt",
            "docs/art-source/background/background_generation_prompts.txt"
        };

        [Test]
        public void RuntimeResourceFiles_AreCoveredByRegisteredRootsAndExtensions()
        {
            foreach (var path in Directory.GetFiles(ResourcesRoot, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(path) == ".meta")
                    continue;

                var relative = Normalize(Path.GetRelativePath(ResourcesRoot, path));
                var root = GetRoot(relative);
                var extension = Path.GetExtension(path);

                Assert.IsTrue(
                    RegisteredExtensions.TryGetValue(root, out var allowedExtensions),
                    $"{relative} is under an unregistered runtime Resources root.");
                Assert.IsTrue(
                    allowedExtensions.Contains(extension),
                    $"{relative} uses unregistered extension '{extension}' for root '{root}'.");
            }
        }

        [Test]
        public void BackgroundPromptFiles_AreMovedOutOfRuntimeResourcesAndExplicitlyRegistered()
        {
            var runtimePromptFiles = Directory
                .GetFiles(Path.Combine(ResourcesRoot, "background"), "*.txt", SearchOption.TopDirectoryOnly);
            Assert.That(runtimePromptFiles, Is.Empty, "Prompt/source text files must not live under runtime Resources.");

            var actual = RegisteredPromptFiles
                .Where(File.Exists)
                .OrderBy(path => path)
                .ToArray();

            CollectionAssert.AreEquivalent(RegisteredPromptFiles, actual);
        }

        [Test]
        public void ProvenanceRegister_DocumentsEveryRegisteredRuntimeRoot()
        {
            var register = File.ReadAllText(RegisterPath);

            Assert.That(register, Does.Contain("`Assets/Resources/BillingMode.json`"));
            foreach (var root in RegisteredExtensions.Keys.Where(key => !string.IsNullOrEmpty(key)))
            {
                Assert.That(
                    register,
                    Does.Contain($"`Assets/Resources/{root}/`"),
                    $"Missing provenance register entry for Resources root '{root}'.");
            }
        }

        private static HashSet<string> Set(params string[] values)
        {
            return new HashSet<string>(values);
        }

        private static string Normalize(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string GetRoot(string relativePath)
        {
            var slash = relativePath.IndexOf('/');
            return slash < 0 ? string.Empty : relativePath.Substring(0, slash);
        }
    }
}
