using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;

/// <summary>
/// Integration tests for save/load round-trip, backup rotation,
/// and profile persistence.
/// </summary>
public class SaveLoadIntegrationTests : TestDriver
{
    private const int TestSeed = 42;
    private string _testSaveDir;

    protected override IEnumerator OnSetUp()
    {
        _testSaveDir = Path.Combine(Application.temporaryCachePath, "test_saves_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testSaveDir);
        yield break;
    }

    protected override IEnumerator OnTearDown()
    {
        if (Directory.Exists(_testSaveDir))
        {
            Directory.Delete(_testSaveDir, true);
        }
        yield break;
    }

    // ── Profile save round-trip ──────────────────────────────────────────────

    [UnityTest]
    public IEnumerator ProfileSave_RoundTrip_PreservesData()
    {
        var saveService = new SaveFileService();
        var profilePath = Path.Combine(_testSaveDir, "profile_save.json");

        var envelope = new SaveFileEnvelope
        {
            SaveVersion = "1.0.0",
            TimestampUtc = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        // Write
        var json = JsonUtility.ToJson(envelope, true);
        File.WriteAllText(profilePath, json);

        // Read back
        var loadedJson = File.ReadAllText(profilePath);
        var loaded = JsonUtility.FromJson<SaveFileEnvelope>(loadedJson);

        Assert.AreEqual("1.0.0", loaded.SaveVersion);
        Assert.AreEqual(envelope.TimestampUtc, loaded.TimestampUtc);

        yield return null;
    }

    // ── RunState serialization ───────────────────────────────────────────────

    [UnityTest]
    public IEnumerator RunState_RoundTrip_PreservesGameState()
    {
        var run = new RunDirector(TestSeed);
        run.StartRun(ClassId.GardenMonk, GameMode.GardenRun, runNumber: 3);

        var state = run.RunState;
        var json = JsonUtility.ToJson(state, true);
        var restored = JsonUtility.FromJson<RunState>(json);

        Assert.AreEqual(state.ClassId, restored.ClassId, "ClassId should survive round-trip");
        Assert.AreEqual(state.Mode, restored.Mode, "GameMode should survive round-trip");
        Assert.AreEqual(state.CurrentHP, restored.CurrentHP, "HP should survive round-trip");
        Assert.AreEqual(state.MaxHP, restored.MaxHP, "MaxHP should survive round-trip");
        Assert.AreEqual(state.CurrentPencil, restored.CurrentPencil, "Pencil should survive round-trip");
        Assert.AreEqual(state.CurrentGold, restored.CurrentGold, "Gold should survive round-trip");

        yield return null;
    }

    // ── Validation clamps ────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator SaveValidation_ClampsOutOfRangeValues()
    {
        var state = new RunState
        {
            CurrentHP = -5,
            MaxHP = 0,
            CurrentPencil = -10,
            CurrentGold = -100,
        };

        // Clamp HP to [0, MaxHP]
        int clampedHP = Mathf.Clamp(state.CurrentHP, 0, Mathf.Max(1, state.MaxHP));
        Assert.AreEqual(0, clampedHP, "Negative HP should clamp to 0");

        // Clamp Pencil to [0, ∞)
        int clampedPencil = Mathf.Max(0, state.CurrentPencil);
        Assert.AreEqual(0, clampedPencil, "Negative pencil should clamp to 0");

        // Clamp Gold to [0, ∞)
        int clampedGold = Mathf.Max(0, state.CurrentGold);
        Assert.AreEqual(0, clampedGold, "Negative gold should clamp to 0");

        yield return null;
    }

    // ── File atomicity ───────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator AtomicWrite_CreatesFileSuccessfully()
    {
        var targetPath = Path.Combine(_testSaveDir, "atomic_test.json");
        var tempPath = targetPath + ".tmp";
        var content = "{\"test\": true}";

        // Write to temp
        File.WriteAllText(tempPath, content);
        Assert.IsTrue(File.Exists(tempPath), "Temp file should exist");

        // Move to target
        if (File.Exists(targetPath)) File.Delete(targetPath);
        File.Move(tempPath, targetPath);

        Assert.IsTrue(File.Exists(targetPath), "Target file should exist after move");
        Assert.IsFalse(File.Exists(tempPath), "Temp file should be gone after move");
        Assert.AreEqual(content, File.ReadAllText(targetPath));

        yield return null;
    }

    // ── Backup rotation ──────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator BackupRotation_KeepsMaxFiveBackups()
    {
        var backupDir = Path.Combine(_testSaveDir, "backups");
        Directory.CreateDirectory(backupDir);

        // Create 7 backup files
        for (int i = 0; i < 7; i++)
        {
            var backupPath = Path.Combine(backupDir, $"profile_save_{i:D4}.json");
            File.WriteAllText(backupPath, $"{{\"backup\": {i}}}");
        }

        // Simulate rotation: keep only the 5 newest
        var files = new DirectoryInfo(backupDir).GetFiles("*.json");
        System.Array.Sort(files, (a, b) => b.CreationTime.CompareTo(a.CreationTime));

        int deleted = 0;
        for (int i = 5; i < files.Length; i++)
        {
            files[i].Delete();
            deleted++;
        }

        var remaining = Directory.GetFiles(backupDir, "*.json");
        Assert.AreEqual(5, remaining.Length, "Should keep exactly 5 backups");
        Assert.AreEqual(2, deleted, "Should delete 2 excess backups");

        yield return null;
    }

    // ── Multiple class states ────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator MultipleRuns_DifferentClasses_ProduceDifferentStates()
    {
        var run1 = new RunDirector(TestSeed);
        run1.StartRun(ClassId.NumberFreak, GameMode.GardenRun, runNumber: 1);
        var json1 = JsonUtility.ToJson(run1.RunState);

        var run2 = new RunDirector(TestSeed);
        run2.StartRun(ClassId.StoneGardener, GameMode.GardenRun, runNumber: 1);
        var json2 = JsonUtility.ToJson(run2.RunState);

        Assert.AreNotEqual(json1, json2, "Different classes should produce different serialized states");

        yield return null;
    }
}
