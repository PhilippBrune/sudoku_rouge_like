using System.IO;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class StorePlatformAssetReadinessTests
    {
        private const string ChecklistPath = "docs/release/store-platform-asset-checklist.md";
        private const string ManifestPath = "docs/release/platform-release-manifest.md";

        [Test]
        public void StorePlatformAssetChecklist_TracksRequiredDeliverables()
        {
            var checklist = File.ReadAllText(ChecklistPath);

            Assert.That(checklist, Does.Contain("ASSET-006"));
            Assert.That(checklist, Does.Contain("Steam capsule"));
            Assert.That(checklist, Does.Contain("library hero"));
            Assert.That(checklist, Does.Contain("app icon"));
            Assert.That(checklist, Does.Contain("controller glyph"));
            Assert.That(checklist, Does.Contain("trailer"));
            Assert.That(checklist, Does.Contain("RELEASE_BLOCKER"));
        }

        [Test]
        public void PlatformManifest_LinksStorePlatformAssetChecklist()
        {
            var manifest = File.ReadAllText(ManifestPath);

            Assert.That(manifest, Does.Contain("PRG-STORE-ASSETS"));
            Assert.That(manifest, Does.Contain("`docs/release/store-platform-asset-checklist.md`"));
            Assert.That(manifest, Does.Contain("Store capsule, header, library art, and platform icon package are missing."));
        }

        [Test]
        public void StorePlatformRuntimeAssets_AreNotAccidentallyClaimedPresent()
        {
            Assert.IsFalse(ExistsAny(
                "Assets",
                "store_capsule.png",
                "store_header.png",
                "library_hero.png",
                "app_icon.png",
                "controller_glyphs.png"),
                "Store/platform assets exist now; update the release checklist and platform manifest exit criteria.");
        }

        private static bool ExistsAny(string root, params string[] fileNames)
        {
            if (!Directory.Exists(root))
                return false;

            foreach (var fileName in fileNames)
            {
                foreach (var path in Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories))
                {
                    if (!path.EndsWith(".meta"))
                        return true;
                }
            }

            return false;
        }
    }
}
