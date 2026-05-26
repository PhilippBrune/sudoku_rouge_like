using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class BackgroundAssetBudgetTests
    {
        private const string BackgroundRoot = "Assets/Resources/background";
        private const string BudgetDocPath = "docs/background-asset-budget.md";
        private const long MaxSingleBackgroundBytes = 10L * 1024L * 1024L;
        private const long MaxTotalBackgroundBytes = 150L * 1024L * 1024L;

        [Test]
        public void BackgroundPngSources_StayWithinTrackedReleaseBudget()
        {
            var files = Directory.GetFiles(BackgroundRoot, "*.png", SearchOption.TopDirectoryOnly);
            Assert.That(files.Length, Is.GreaterThan(0), "No runtime background PNGs were found.");

            foreach (var path in files)
            {
                var info = new FileInfo(path);
                Assert.LessOrEqual(
                    info.Length,
                    MaxSingleBackgroundBytes,
                    $"{info.Name} is above the per-background source budget.");
            }

            var total = files.Sum(path => new FileInfo(path).Length);
            Assert.LessOrEqual(total, MaxTotalBackgroundBytes, "Runtime background PNG sources exceed the tracked total budget.");
        }

        [Test]
        public void BackgroundImportSettings_AreReviewedForRuntimeUse()
        {
            foreach (var pngPath in Directory.GetFiles(BackgroundRoot, "*.png", SearchOption.TopDirectoryOnly))
            {
                var metaPath = pngPath + ".meta";
                Assert.IsTrue(File.Exists(metaPath), $"{pngPath} is missing its Unity .meta file.");

                var meta = File.ReadAllText(metaPath);
                Assert.That(meta, Does.Contain("isReadable: 0"), $"{Path.GetFileName(pngPath)} should not be CPU-readable at runtime.");
                Assert.That(meta, Does.Contain("enableMipMap: 0"), $"{Path.GetFileName(pngPath)} should not use mipmaps for fullscreen UI art.");
                Assert.That(meta, Does.Contain("maxTextureSize: 2048"), $"{Path.GetFileName(pngPath)} should keep the reviewed 2048 max texture cap.");
                Assert.That(meta, Does.Contain("textureCompression: 1"), $"{Path.GetFileName(pngPath)} should keep normal compression enabled.");
            }
        }

        [Test]
        public void BackgroundAssetBudgetDocument_RecordsThresholdsAndManualReview()
        {
            var doc = File.ReadAllText(BudgetDocPath);

            Assert.That(doc, Does.Contain("ASSET-004"));
            Assert.That(doc, Does.Contain("10 MB"));
            Assert.That(doc, Does.Contain("150 MB"));
            Assert.That(doc, Does.Contain("Unity import"));
            Assert.That(doc, Does.Contain("runtime backgrounds"));
        }
    }
}
