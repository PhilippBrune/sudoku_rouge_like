using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class ReservedArtManifestTests
    {
        private const string ReservedArtRoot = "Assets/_ReservedArt";
        private const string ManifestPath = "docs/reserved-art-manifest.md";
        private const string ProvenancePath = "docs/legal/asset-provenance-register.md";

        [Test]
        public void ReservedArtFiles_AreClassifiedInManifest()
        {
            var manifest = File.ReadAllText(ManifestPath);
            var files = Directory
                .GetFiles(ReservedArtRoot, "*", SearchOption.AllDirectories)
                .Where(path => Path.GetExtension(path) != ".meta")
                .Select(Normalize)
                .OrderBy(path => path)
                .ToArray();

            Assert.That(files.Length, Is.GreaterThan(0), "No reserved art files were found.");
            foreach (var path in files)
            {
                Assert.That(manifest, Does.Contain($"`{path}`"), $"{path} is missing from the reserved art manifest.");
            }
        }

        [Test]
        public void ReservedArtFiles_AreKeptOutsideRuntimeResources()
        {
            foreach (var path in Directory.GetFiles(ReservedArtRoot, "*", SearchOption.AllDirectories))
            {
                Assert.That(Normalize(path), Does.Not.Contain("/Resources/"), $"{path} should not be in runtime Resources.");
            }
        }

        [Test]
        public void ProvenanceRegister_LinksReservedArtManifest()
        {
            var provenance = File.ReadAllText(ProvenancePath);

            Assert.That(provenance, Does.Contain("`Assets/_ReservedArt/`"));
            Assert.That(provenance, Does.Contain("`docs/reserved-art-manifest.md`"));
            Assert.That(provenance, Does.Contain("not loaded through `Resources`"));
        }

        private static string Normalize(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
