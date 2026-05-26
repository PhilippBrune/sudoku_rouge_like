using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SudokuRoguelike.UI;
using UnityEngine;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class AudioAssetServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            AudioAssetService.ResetCacheForTests();
        }

        [Test]
        public void RequiredClipCatalog_CoversEveryAudioClipId()
        {
            foreach (AudioClipId id in Enum.GetValues(typeof(AudioClipId)))
            {
                Assert.IsTrue(
                    AudioAssetService.TryGetInfo(id, out _),
                    $"{id} is missing from the authored audio catalog.");
            }
        }

        [Test]
        public void RequiredClipCatalog_UsesStableResourcePaths()
        {
            var ids = new HashSet<AudioClipId>();
            var paths = new HashSet<string>();

            foreach (var info in AudioAssetService.RequiredClips)
            {
                Assert.IsTrue(ids.Add(info.Id), $"{info.Id} is duplicated.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(info.ResourcePath), $"{info.Id} has no Resources path.");
                Assert.IsTrue(info.ResourcePath.StartsWith("audio/", StringComparison.Ordinal), $"{info.Id} is outside Assets/Resources/audio.");
                Assert.IsFalse(info.ResourcePath.Contains("\\"), $"{info.Id} uses backslashes in its Resources path.");
                Assert.IsTrue(string.IsNullOrEmpty(Path.GetExtension(info.ResourcePath)), $"{info.Id} Resources path should omit file extension.");
                Assert.IsTrue(paths.Add(info.ResourcePath), $"{info.ResourcePath} is assigned to more than one clip.");
                Assert.IsTrue(Enum.IsDefined(typeof(AudioAssetCategory), info.Category), $"{info.Id} has an invalid category.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(info.Usage), $"{info.Id} has no usage description.");
            }
        }

        [Test]
        public void ReleaseRequiredClips_HaveAuthoredAssetFilesAndMetas()
        {
            var extensions = new[] { ".wav", ".mp3", ".ogg", ".aiff", ".flac" };

            foreach (var info in AudioAssetService.RequiredClips)
            {
                if (!info.ReleaseRequired)
                    continue;

                var resourcePath = info.ResourcePath.Replace('/', Path.DirectorySeparatorChar);
                string assetPath = null;
                foreach (var extension in extensions)
                {
                    var candidate = Path.Combine("Assets", "Resources", resourcePath + extension);
                    if (File.Exists(candidate))
                    {
                        assetPath = candidate;
                        break;
                    }
                }

                Assert.IsNotNull(assetPath, $"{info.Id} has no authored clip file for Resources path {info.ResourcePath}.");
                Assert.IsTrue(File.Exists(assetPath + ".meta"), $"{assetPath} is missing its Unity .meta file.");
                Assert.Greater(new FileInfo(assetPath).Length, 44, $"{assetPath} is not a valid non-empty audio file.");

                if (Path.GetExtension(assetPath).Equals(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    using var stream = File.OpenRead(assetPath);
                    using var reader = new BinaryReader(stream);
                    Assert.AreEqual("RIFF", new string(reader.ReadChars(4)), $"{assetPath} does not start with RIFF.");
                    stream.Position = 8;
                    Assert.AreEqual("WAVE", new string(reader.ReadChars(4)), $"{assetPath} is not a WAVE file.");
                }
            }
        }

        [Test]
        public void GetClip_UnknownCatalogId_UsesProceduralFallback()
        {
            AudioAssetService.ResetCacheForTests();
            var fallbackCalled = false;
            var fallback = AudioClip.Create("fallback_audio_test", 1, 1, 22050, false);

            try
            {
                var resolved = AudioAssetService.GetClip((AudioClipId)(-1000), () =>
                {
                    fallbackCalled = true;
                    return fallback;
                });

                Assert.IsTrue(fallbackCalled);
                Assert.AreSame(fallback, resolved);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fallback);
            }
        }

        [Test]
        public void FloorAndBossHelpers_ClampToKnownAudioIds()
        {
            Assert.AreEqual(AudioClipId.MusicFloor0, AudioAssetService.GetFloorLoopId(-1));
            Assert.AreEqual(AudioClipId.MusicFloor0, AudioAssetService.GetFloorLoopId(0));
            Assert.AreEqual(AudioClipId.MusicFloor4, AudioAssetService.GetFloorLoopId(99));

            Assert.AreEqual(AudioClipId.MusicBossLayer0, AudioAssetService.GetBossLayerId(-1));
            Assert.AreEqual(AudioClipId.MusicBossLayer0, AudioAssetService.GetBossLayerId(0));
            Assert.AreEqual(AudioClipId.MusicBossLayer4, AudioAssetService.GetBossLayerId(99));
        }

        [Test]
        public void MenuMusicStyles_MapToAuthoredLoopIdsAndLocalizationKeys()
        {
            Assert.AreEqual(3, AudioAssetService.MenuMusicStyleLocalizationKeys.Count);
            Assert.AreEqual("Audio.MenuMusicStyle.Garden", AudioAssetService.MenuMusicStyleLocalizationKeys[0]);
            Assert.AreEqual("Audio.MenuMusicStyle.Bamboo", AudioAssetService.MenuMusicStyleLocalizationKeys[1]);
            Assert.AreEqual("Audio.MenuMusicStyle.Rest", AudioAssetService.MenuMusicStyleLocalizationKeys[2]);

            Assert.AreEqual(0, AudioAssetService.NormalizeMenuMusicStyleIndex(-1));
            Assert.AreEqual(2, AudioAssetService.NormalizeMenuMusicStyleIndex(2));
            Assert.AreEqual(0, AudioAssetService.NormalizeMenuMusicStyleIndex(99));

            Assert.AreEqual(AudioClipId.MusicMenuLoop, AudioAssetService.GetMenuMusicStyleLoopId(0));
            Assert.AreEqual(AudioClipId.MusicFloor0, AudioAssetService.GetMenuMusicStyleLoopId(1));
            Assert.AreEqual(AudioClipId.MusicRestLoop, AudioAssetService.GetMenuMusicStyleLoopId(2));
            Assert.AreEqual(AudioClipId.MusicMenuLoop, AudioAssetService.GetMenuMusicStyleLoopId(99));
        }

        [Test]
        public void MenuMusicController_InitializeRestoresMissingAudioSource()
        {
            var go = new GameObject("MenuMusicControllerTest");

            try
            {
                var controller = go.AddComponent<MenuMusicController>();
                var source = go.GetComponent<AudioSource>();
                if (source != null)
                    UnityEngine.Object.DestroyImmediate(source);

                Assert.IsNull(go.GetComponent<AudioSource>());

                Assert.DoesNotThrow(() => controller.Initialize());

                var restored = go.GetComponent<AudioSource>();
                Assert.IsNotNull(restored);
                Assert.IsTrue(restored.loop);
                Assert.IsFalse(restored.playOnAwake);
                Assert.AreEqual(0f, restored.spatialBlend);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
