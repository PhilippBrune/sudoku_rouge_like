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
    }
}
