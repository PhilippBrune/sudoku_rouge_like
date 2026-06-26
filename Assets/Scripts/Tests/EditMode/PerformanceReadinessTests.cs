using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public sealed class PerformanceReadinessTests
    {
        private const string PerformanceBudgetPath = "docs/performance-runtime-budget.md";
        private const string PerformanceProfilerResultsPath = "docs/performance-profiler-results.md";
        private const string InRunControllerPath = "Assets/Scripts/UI/InRunController.cs";
        private const string RunAudioControllerPath = "Assets/Scripts/UI/RunAudioController.cs";
        private const string PathAmbientParticlesPath = "Assets/Scripts/UI/PathAmbientParticles.cs";

        [Test]
        public void RuntimePerformanceBudget_DocumentsTrackedHotspotsAndProfilerScenarios()
        {
            var budget = File.ReadAllText(PerformanceBudgetPath);

            StringAssert.Contains("Runtime performance status: needs Unity Profiler verification.", budget);
            StringAssert.Contains("Per-frame GC allocations during stable puzzle play", budget);
            StringAssert.Contains("Save/profile disk I/O in `Update()`/`LateUpdate()`", budget);
            StringAssert.Contains("InRunController.cs", budget);
            StringAssert.Contains("RunAudioController.cs", budget);
            StringAssert.Contains("PathAmbientParticles.cs", budget);
            StringAssert.Contains("GeneratedUiRebuildTests", budget);
            StringAssert.Contains("Boss gate display", budget);
            StringAssert.Contains("Language switch to German", budget);
        }

        [Test]
        public void RunAudioControllerUpdate_DoesNotLoadSaveOrProfileDataFromDisk()
        {
            var source = File.ReadAllText(RunAudioControllerPath);
            var updateBody = ExtractMethodBody(source, "Update");

            StringAssert.Contains("OptionsController.LiveAudio", updateBody);
            Assert.That(updateBody, Does.Not.Contain("SaveFileService"));
            Assert.That(updateBody, Does.Not.Contain("ProfileService"));
            Assert.That(updateBody, Does.Not.Contain("HasSaveFile("));
            Assert.That(updateBody, Does.Not.Contain(".Load("));
            Assert.That(updateBody, Does.Not.Contain("ApplyEnvelope"));
        }

        [Test]
        public void RunAudioControllerStart_DoesNotAutoPlayDefaultPathLoop()
        {
            var source = File.ReadAllText(RunAudioControllerPath);
            var startBody = ExtractMethodBody(source, "Start");
            var setContextBody = ExtractMethodBody(source, "SetContext");
            var stopAllBody = ExtractMethodBody(source, "StopAll");

            StringAssert.Contains("if (_contextRequested)", startBody);
            Assert.That(startBody, Does.Not.Contain("SetContext(_context); // apply any context set before Start completed"));
            StringAssert.Contains("_contextRequested = true;", setContextBody);
            StringAssert.Contains("_contextRequested = false;", stopAllBody);
        }

        [Test]
        public void UpdateLoopHotspotInventory_RemainsExplicit()
        {
            var hotspots = new Dictionary<string, string[]>
            {
                [InRunControllerPath] = new[] { "Update", "LateUpdate" },
                [RunAudioControllerPath] = new[] { "Update" },
                [PathAmbientParticlesPath] = new[] { "Update" }
            };

            foreach (var pair in hotspots)
            {
                var source = File.ReadAllText(pair.Key);
                foreach (var methodName in pair.Value)
                {
                    Assert.DoesNotThrow(
                        () => ExtractMethodBody(source, methodName),
                        $"{pair.Key} should keep {methodName}() visible in the hotspot inventory until it is refactored or formally waived.");
                }
            }
        }

        [Test]
        public void ProfilerResultsLedger_KeepsReleaseCaptureGateExplicit()
        {
            var ledger = File.ReadAllText(PerformanceProfilerResultsPath);

            StringAssert.Contains("REL-009", ledger);
            StringAssert.Contains("Unity Profiler captures", ledger);
            StringAssert.Contains("blocked until captures are recorded and reviewed", ledger);
            StringAssert.Contains("PERF-CAP-001", ledger);
            StringAssert.Contains("PERF-CAP-007", ledger);
            StringAssert.Contains("Do not mark performance release-ready", ledger);
            StringAssert.Contains("Last Capture Attempt", ledger);
            StringAssert.Contains("PlayMode-unity.log", ledger);
            StringAssert.Contains("Unity licensing client", ledger);
            StringAssert.Contains("com.unity.editor.headless", ledger);
            StringAssert.Contains("TestResults/PlayMode-results.xml", ledger);
            StringAssert.Contains("Last Validation Attempt", ledger);
            StringAssert.Contains("Library/ScriptAssemblies/UnityEngine.UI.dll", ledger);
        }

        private static string ExtractMethodBody(string source, string methodName)
        {
            var signature = $"private void {methodName}(";
            var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
            if (signatureIndex < 0)
                throw new InvalidOperationException($"Could not find method signature: {signature}");

            var openBrace = source.IndexOf('{', signatureIndex);
            if (openBrace < 0)
                throw new InvalidOperationException($"Could not find method body for: {methodName}");

            var depth = 0;
            for (var i = openBrace; i < source.Length; i++)
            {
                if (source[i] == '{')
                {
                    depth++;
                    continue;
                }

                if (source[i] != '}')
                    continue;

                depth--;
                if (depth == 0)
                    return source.Substring(openBrace + 1, i - openBrace - 1);
            }

            throw new InvalidOperationException($"Could not parse method body for: {methodName}");
        }
    }
}
