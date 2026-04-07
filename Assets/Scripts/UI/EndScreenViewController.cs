using System;
using System.Text;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Owns the game-over / victory screen.
    /// </summary>
    public sealed class EndScreenViewController : MonoBehaviour
    {
        private RunMapController _map;
        private GameObject _gameOverPanel;
        private Text _gameOverSummary;
        private Text _gameOverDetails;
        private Button _gameOverBackBtn;
        private bool _gameOverShown;

        public Action OnReturnToMenuPressed;

        public bool IsShown => _gameOverShown;
        public void ResetShown() => _gameOverShown = false;

        public void Configure(RunMapController map, GameObject gameOverPanel,
            Text goSummary, Text goDetails, Button goBack)
        {
            _map = map;
            _gameOverPanel = gameOverPanel;
            _gameOverSummary = goSummary;
            _gameOverDetails = goDetails;
            _gameOverBackBtn = goBack;

            if (_gameOverBackBtn != null)
            {
                _gameOverBackBtn.onClick.RemoveAllListeners();
                _gameOverBackBtn.onClick.AddListener(() => OnReturnToMenuPressed?.Invoke());
            }
        }

        public void CheckAndShow(RunDirector run)
        {
            if (_gameOverShown) return;
            if (run == null || !run.IsPlayerDead) return;
            _gameOverShown = true;
            ShowGameOver(run, false);
        }

        public void ShowGameOver(RunDirector run, bool victory)
        {
            // Panels hidden by InRunController before calling ShowGameOver
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);

            // Dynamic title
            if (_gameOverSummary != null)
            {
                _gameOverSummary.text = victory ? "Victory!" : "Defeat";
                _gameOverSummary.color = victory
                    ? new Color(0.85f, 0.75f, 0.20f, 1f)
                    : new Color(0.90f, 0.30f, 0.20f, 1f);
            }

            // Snapshot XP before persisting so we can show level-up
            var classId = run.State.ClassId;
            var snapshotSave = new SaveFileService();
            var preXp = snapshotSave.HasSaveFile()
                ? GetClassTotalXpFromSave(snapshotSave.Load(), classId) : 0;
            var preLevel = XpTable.DeriveLevel(preXp);

            var result = _map.BuildRunResult(victory, 0, 0);
            PersistResult(result, run);

            // Load updated XP after persist; fallback to in-memory calculation
            var postSave = new SaveFileService();
            var postXp = postSave.HasSaveFile()
                ? GetClassTotalXpFromSave(postSave.Load(), classId) : preXp + (result?.XpEarned ?? 0);
            var postLevel = XpTable.DeriveLevel(postXp);
            var postXpInto = postXp - XpTable.CumulativeXpForLevel(postLevel);
            var postXpToNext = postLevel < 40 ? XpTable.XpToNextLevel(postLevel) : 0;

            if (_gameOverDetails != null)
            {
                var sb = new StringBuilder();

                // Run summary
                sb.AppendLine($"Class: {classId}   Floor: {run.State.Depth}");
                sb.AppendLine($"HP: {run.State.CurrentHP}/{run.State.MaxHP}   Gold: {run.State.CurrentGold}");

                // XP earned this run
                var xpEarned = result?.XpEarned ?? 0;
                sb.AppendLine($"XP Earned This Run: +{xpEarned}");
                sb.AppendLine();

                // Level-up notification
                if (preLevel != postLevel)
                    sb.AppendLine($"  Level Up!  {preLevel}  \u2192  {postLevel}");

                // XP progress bar toward next level
                if (postLevel < 40)
                {
                    var barLen = 16;
                    var filled = postXpToNext > 0
                        ? Math.Min(barLen, (int)(postXpInto * barLen / (float)postXpToNext))
                        : 0;
                    var bar = new string('=', filled) + new string('-', barLen - filled);
                    sb.AppendLine($"{classId}  Lv {postLevel}");
                    sb.AppendLine($"[{bar}]  {postXpInto} / {postXpToNext} XP");
                    sb.AppendLine($"(to Level {postLevel + 1})");
                }
                else
                {
                    sb.AppendLine($"{classId}  Level MAX  (Lv 40)");
                }

                // Run score
                if (result != null && result.RunScore > 0)
                {
                    sb.AppendLine();
                    var presenter = new EndScreenPresenter();
                    sb.AppendLine(presenter.BuildRunScoreBreakdown(result));
                }

                _gameOverDetails.text = sb.ToString().TrimEnd();
            }
        }

        private static int GetClassTotalXpFromSave(SaveFileEnvelope envelope, ClassId classId)
        {
            var entries = envelope?.MetaProgress?.GardenProgression?.ClassEntries;
            if (entries == null) return 0;
            for (var i = 0; i < entries.Count; i++)
                if (entries[i].ClassId == classId) return entries[i].TotalXp;
            return 0;
        }

        private void PersistResult(RunResult result, RunDirector run)
        {
            if (result == null || result.TutorialMode) return;

            if (run != null)
            {
                result.ItemsUsedThisRun = run.GetAnalytics()?.TotalItemsUsed
                    ?? run.CurrentLevelState?.ItemsUsedThisLevel ?? 0;
                result.AcquiredRelic = run.State.HasRelic;
            }

            var profile = new ProfileService(new SaveFileService());
            profile.RecordRunAndGetNewUnlocks(result);

            var save = new SaveFileService();
            if (save.HasSaveFile())
            {
                var envelope = save.Load();
                envelope.ActiveRunState = null;
                envelope.ActivePuzzle = null;
                save.Save(envelope);
            }
        }
    }
}
