using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SudokuRoguelike.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace SudokuRoguelike.Tests
{
    // [REQ: REQ-AUDIO-100] [REQ: REQ-AUDIO-101] GAP-030: every release-required clip loads as a real AudioClip with
    // audio data (the 1.0 files had no RIFF header). Listening is still a manual check.
    [TestFixture]
    [Requirement("REQ-AUDIO-100", "REQ-AUDIO-101")]
    public sealed class AudioClipPlayModeTests
    {
        [UnityTest]
        public IEnumerator ReleaseClips_LoadWithAudioData()
        {
            AudioAssetService.ResetCacheForTests();
            var problems = new List<string>();
            var checkedCount = 0;

            foreach (var info in AudioAssetService.RequiredClips)
            {
                if (!info.ReleaseRequired) continue;
                checkedCount++;

                if (!AudioAssetService.TryLoadAuthoredClip(info.Id, out var clip))
                {
                    problems.Add($"{info.Id}: not loadable from Resources/{info.ResourcePath}");
                    continue;
                }
                if (clip.loadState != AudioDataLoadState.Loaded)
                    clip.LoadAudioData();
                for (var frame = 0; frame < 120 && clip.loadState == AudioDataLoadState.Loading; frame++)
                    yield return null;

                if (clip.loadState != AudioDataLoadState.Loaded)
                    problems.Add($"{info.Id}: audio data state {clip.loadState}");
                if (clip.length <= 0.05f || clip.samples <= 0)
                    problems.Add($"{info.Id}: empty clip ({clip.length:F2} s)");
                if (info.Loop && clip.length < 5f)
                    problems.Add($"{info.Id}: music loop only {clip.length:F1} s");
            }

            Assert.Greater(checkedCount, 0, "no release clips registered");
            CollectionAssert.IsEmpty(problems);
        }

        // AUDIO-MUSIC-003 / GAP-045: the boss puzzle plays the floor's boss track instead of the floor track. Before,
        // StartBossLayer existed but nothing called it, so boss fights never had boss music.
        [UnityTest]
        [Requirement("AUDIO-MUSIC-003")]
        public IEnumerator BossPuzzle_ReplacesTheFloorTrackWithTheBossTrack_AndLeavingEndsIt()
        {
            var host = new GameObject("RunAudioTest", typeof(AudioSource));
            host.AddComponent<RunAudioController>();
            yield return null;
            yield return null;
            var audio = Object.FindFirstObjectByType<RunAudioController>();
            Assert.NotNull(audio, "a run audio controller exists");

            try
            {
                audio.SetMode(SudokuRoguelike.Core.GameMode.GardenRun);
                audio.SetFloorIndex(1);
                audio.SetContext(RunAudioController.Context.Puzzle);
                Assert.AreSame(audio.GetFloorTrack(1), audio.CurrentMusicTarget, "puzzle plays the floor track");

                audio.StartBossLayer();
                Assert.IsTrue(audio.IsBossMusicActive);
                Assert.AreSame(audio.GetBossTrack(1), audio.CurrentMusicTarget, "boss puzzle switches to the boss track");
                yield return new WaitForSecondsRealtime(1.3f);
                Assert.AreSame(audio.GetBossTrack(1), audio.GetComponent<AudioSource>().clip,
                    "the boss track plays on the music source, not as a layer on top");

                audio.SetContext(RunAudioController.Context.Path);
                Assert.IsFalse(audio.IsBossMusicActive, "leaving the puzzle ends the boss music");
            }
            finally
            {
                audio.StopAll();
                if (host != null) Object.Destroy(host);
            }
        }

        // AUDIO-MUSIC-005 / 007: optional soundtrack tracks without a file keep today's music.
        [UnityTest]
        [Requirement("AUDIO-MUSIC-005", "AUDIO-MUSIC-007")]
        public IEnumerator OptionalTracks_WithoutFiles_KeepTheExistingMusic()
        {
            var host = new GameObject("RunAudioOptionalTest", typeof(AudioSource));
            host.AddComponent<RunAudioController>();
            var menuHost = new GameObject("MenuMusicTest", typeof(AudioSource));
            var menu = menuHost.AddComponent<MenuMusicController>();
            yield return null;
            yield return null;
            var audio = Object.FindFirstObjectByType<RunAudioController>();

            try
            {
                audio.SetFloorIndex(2);
                audio.SetMode(SudokuRoguelike.Core.GameMode.EndlessZen);
                audio.SetContext(RunAudioController.Context.Puzzle);
                if (!AudioAssetService.TryLoadAuthoredClip(AudioClipId.MusicEndlessZen, out _))
                    Assert.AreSame(audio.GetFloorTrack(2), audio.CurrentMusicTarget, "no Endless Zen file: the floor track plays");

                menu.Initialize();
                var before = menuHost.GetComponent<AudioSource>().clip;
                menu.SetCreditsActive(true);
                if (!AudioAssetService.TryLoadAuthoredClip(AudioClipId.MusicCredits, out _))
                {
                    Assert.IsFalse(menu.CreditsTrackActive);
                    Assert.AreSame(before, menuHost.GetComponent<AudioSource>().clip, "no credits file: the menu track stays");
                }
                menu.SetCreditsActive(false);
            }
            finally
            {
                audio.SetMode(SudokuRoguelike.Core.GameMode.GardenRun);
                audio.StopAll();
                Object.Destroy(host);
                Object.Destroy(menuHost);
            }
        }
    }
}
