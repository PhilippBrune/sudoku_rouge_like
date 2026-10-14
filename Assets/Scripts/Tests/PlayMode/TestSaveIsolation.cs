using System;
using System.IO;
using NUnit.Framework;
using SudokuRoguelike.Save;

// No namespace: NUnit applies a namespace-less [SetUpFixture] to every test in the assembly.
// [REQ: REQ-SAVE-102] Automated tests never read or write the player's real save folder (GAP-010).
[SetUpFixture]
public sealed class PlayModeTestSaveIsolation
{
    private string _root;

    [OneTimeSetUp]
    public void RedirectSaveRoot()
    {
        SaveFileService.FlushSharedPendingWrites();
        _root = Path.Combine(Path.GetTempPath(), "sudoku_rouge_like_tests", "playmode_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        SaveFileService.RootDirectoryOverride = _root;
        SudokuRoguelike.Platform.PlatformServiceRegistry.SteamworksCallsEnabled = false; // no Steam client in test runs
    }

    [OneTimeTearDown]
    public void RestoreSaveRoot()
    {
        SaveFileService.FlushSharedPendingWrites();
        SaveFileService.RootDirectoryOverride = null;
        SudokuRoguelike.Platform.PlatformServiceRegistry.SteamworksCallsEnabled = true;
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch (IOException)
        {
            // A locked temp file must not fail the run; the OS temp cleanup removes it later.
        }
    }
}
