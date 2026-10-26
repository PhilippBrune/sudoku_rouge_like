using System;
using System.Collections.Generic;
using System.Text;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Owns the game-over / victory screen.
    /// </summary>
    public sealed partial class EndScreenViewController : MonoBehaviour
    {
        private RunMapController _map;
        private GameObject _gameOverPanel;
        private Text _gameOverSummary;
        private Text _gameOverDetails;
        private Button _gameOverBackBtn;
        private bool _gameOverShown;
        private GameObject _bossDefeatedScreen;
        private bool _offeringTaken;

        public Action OnReturnToMenuPressed;
        /// <summary>[REQ: RUN-AGAIN-001] Walk again: the next run with the same class, mode and Harmony.</summary>
        public Action OnWalkAgainPressed;

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

        /// <summary>
        /// [REQ: REQ-BOSS-107] [REQ: UI-STYLE-005] After a non-final boss: the offering table (layout-proposals.md, B).
        /// A banner names the defeated boss, a paper tag shows this fight's numbers, and the relic choice sits on the same
        /// screen: one offering per relic on a plinth with its own Take button, plus "Leave them". Without offerings a
        /// single Continue button advances. The screen covers the whole canvas and does not use the game-over panel, so
        /// the end-screen save rules and shortcuts (which clear a finished run) cannot fire here.
        /// </summary>
        public void ShowBossCleared(RunDirector run, IReadOnlyList<RelicInstance> offerings,
            Action<RelicInstance> onTake, Action onContinue)
        {
            HideBossCleared();
            var root = _gameOverPanel != null ? _gameOverPanel.transform.parent : transform;
            var state = run?.State;
            var floor = state?.CurrentFloor ?? 0;
            _offeringTaken = false;

            var screen = ScrollUiKit.Fill(root, BossDefeatedScreenName, Vector2.zero, Vector2.one, Color.black);
            screen.raycastTarget = true; // nothing behind the screen takes clicks
            _bossDefeatedScreen = screen.gameObject;
            _bossDefeatedScreen.transform.SetAsLastSibling();
            ScrollUiKit.Painting(screen.transform, "Backdrop", "bg_boss_cleared", new Color(0.62f, 0.60f, 0.56f, 1f));
            ScrollUiKit.Fill(screen.transform, "FloorShade", Vector2.zero, new Vector2(1f, 0.45f), new Color(0f, 0f, 0f, 0.35f));

            BuildDefeatBanner(screen.transform, floor, state?.TotalFloors ?? 5);
            BuildStatsTag(screen.transform, run);
            ScrollUiKit.Icon(screen.transform, "BossMask", "boss/icon_" + FloorThemeData.GetBossKey(floor),
                new Vector2(0.839f, 0.634f), new Vector2(0.898f, 0.755f), new Color(0.50f, 0.50f, 0.52f, 1f));
            ScrollUiKit.Label(screen.transform, "MaskCaption", T("InRun.BossDefeated.MaskStill"), 18,
                TextAnchor.MiddleCenter, new Color(0.92f, 0.89f, 0.82f, 1f), new Vector2(0.78f, 0.590f), new Vector2(0.96f, 0.624f), 12);

            var hasOfferings = offerings != null && offerings.Count > 0;
            ScrollUiKit.Label(screen.transform, "OfferingPrompt",
                T(hasOfferings ? "InRun.BossDefeated.Prompt" : "InRun.BossDefeated.NoOfferings"), 28,
                TextAnchor.MiddleCenter, new Color(0.96f, 0.93f, 0.85f, 1f), new Vector2(0.22f, 0.533f), new Vector2(0.78f, 0.569f), 18);

            if (!hasOfferings)
            {
                var continueBtn = ScrollUiKit.RimButton(screen.transform, "BossClearedContinueBtn", new Vector2(0.42f, 0.30f),
                    new Vector2(0.58f, 0.36f), T("InRun.BossDefeated.Onward"), 24, ScrollUiKit.Gold, OfferingFocus, OfferingText);
                continueBtn.onClick.AddListener(() => Advance(onContinue));
                FadeInAndSelect(continueBtn);
                return;
            }

            var takes = new List<Button>();
            for (var i = 0; i < offerings.Count; i++)
            {
                var relic = offerings[i];
                var center = 0.5f + (i - (offerings.Count - 1) * 0.5f) * 0.266f;
                var take = ScrollUiKit.Offering(screen.transform, i, relic, center, T("InRun.BossDefeated.Take"));
                take.onClick.AddListener(() => Advance(() => onTake?.Invoke(relic)));
                takes.Add(take);
            }

            ScrollUiKit.Label(screen.transform, "BossDefeatedNext",
                F("InRun.BossDefeated.Next", FloorThemeData.GetFloorName(floor + 1)), 18, TextAnchor.MiddleCenter,
                new Color(0.80f, 0.74f, 0.62f, 1f), new Vector2(0.15f, 0.058f), new Vector2(0.85f, 0.086f), 12);
            var leave = ScrollUiKit.RimButton(screen.transform, "BtnLeaveOfferings", new Vector2(0.45f, 0.010f),
                new Vector2(0.55f, 0.048f), T("InRun.BossDefeated.Leave"), 16, new Color(0.62f, 0.55f, 0.42f, 1f),
                OfferingFocus, OfferingText);
            leave.onClick.AddListener(() => Advance(onContinue));

            for (var i = 0; i < takes.Count; i++)
                takes[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? takes[i - 1] : null,
                    selectOnRight = i < takes.Count - 1 ? takes[i + 1] : null,
                    selectOnDown = leave
                };
            leave.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = takes[takes.Count / 2] };
            FadeInAndSelect(takes[takes.Count / 2]);
        }

        public bool IsBossClearedShown => _bossDefeatedScreen != null && _bossDefeatedScreen.activeSelf;

        private static readonly Color OfferingFocus = ScrollUiKit.OfferingFocus;
        private static readonly Color OfferingText = ScrollUiKit.OfferingText;
        private const string BossDefeatedScreenName = "BossDefeatedScreen";

        private void BuildDefeatBanner(Transform screen, int floor, int totalFloors)
        {
            ScrollUiKit.Banner(screen, "BossDefeatedBanner", new Vector2(0.318f, 0.844f), new Vector2(0.682f, 0.958f),
                "BossDefeatedEyebrow", F("InRun.BossDefeated.Eyebrow", floor + 1, totalFloors, FloorThemeData.GetFloorName(floor)),
                "BossDefeatedTitle", F("InRun.BossDefeated.Title", FloorThemeData.GetBossName(floor)));
        }

        // The fight's and the whole floor's numbers on a paper tag hanging from a string (REQ-BOSS-107 stats beside the
        // offerings): one column for the boss puzzle, one for every puzzle of the floor including it (0.2.0 playtest).
        private void BuildStatsTag(Transform screen, RunDirector run)
        {
            var tag = ScrollUiKit.PaperTag(screen, "BossClearedStats", new Vector2(0.050f, 0.560f), new Vector2(0.250f, 0.785f),
                1.5f, 46f);

            ScrollUiKit.Label(tag.transform, "Header", T("InRun.BossDefeated.StatsHeader").ToUpperInvariant(), 16,
                TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.07f, 0.81f), new Vector2(0.55f, 0.95f), 11);
            ScrollUiKit.Label(tag.transform, "ColumnBoss", T("InRun.BossDefeated.Column.Boss"), 15, TextAnchor.MiddleRight,
                ScrollUiKit.InkSoft, new Vector2(0.55f, 0.81f), new Vector2(0.74f, 0.95f), 10);
            ScrollUiKit.Label(tag.transform, "ColumnFloor", T("InRun.BossDefeated.Column.Floor"), 15, TextAnchor.MiddleRight,
                ScrollUiKit.InkSoft, new Vector2(0.74f, 0.81f), new Vector2(0.93f, 0.95f), 10);
            var boss = BuildBossClearedStats(run);
            var floor = BuildFloorStats(run);
            var rows = new (string key, int boss, int floor)[]
            {
                ("InRun.BossDefeated.Stat.Mistakes", boss.Mistakes, floor.Mistakes),
                ("InRun.BossDefeated.Stat.HpLost", boss.HpLost, floor.HpLost),
                ("InRun.BossDefeated.Stat.ItemsUsed", boss.ItemsUsed, floor.ItemsUsed),
                ("InRun.BossDefeated.Stat.PencilMarks", boss.PencilMarks, floor.PencilMarks)
            };
            for (var k = 0; k < rows.Length; k++)
            {
                var top = 0.79f - k * 0.175f;
                ScrollUiKit.Label(tag.transform, $"Stat_{k}_Label", T(rows[k].key), 20, TextAnchor.MiddleLeft,
                    ScrollUiKit.Ink, new Vector2(0.07f, top - 0.16f), new Vector2(0.58f, top), 12);
                ScrollUiKit.Label(tag.transform, $"Stat_{k}_Value", rows[k].boss.ToString(), 20, TextAnchor.MiddleRight,
                    ScrollUiKit.Vermilion, new Vector2(0.58f, top - 0.16f), new Vector2(0.74f, top), 12);
                ScrollUiKit.Label(tag.transform, $"Stat_{k}_Floor", rows[k].floor.ToString(), 20, TextAnchor.MiddleRight,
                    ScrollUiKit.Vermilion, new Vector2(0.74f, top - 0.16f), new Vector2(0.93f, top), 12);
            }
        }

        // One choice only: the first Take, Leave or Continue locks the screen, so a double click cannot advance twice.
        // If the next step throws, the screen unlocks again instead of trapping the player behind disabled buttons.
        private void Advance(Action next)
        {
            if (_offeringTaken) return;
            _offeringTaken = true;
            SetBossDefeatedButtonsInteractable(false);
            try
            {
                next?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _offeringTaken = false;
                SetBossDefeatedButtonsInteractable(true);
            }
        }

        private void SetBossDefeatedButtonsInteractable(bool interactable)
        {
            if (_bossDefeatedScreen == null) return;
            foreach (var button in _bossDefeatedScreen.GetComponentsInChildren<Button>())
                button.interactable = interactable;
        }

        private void FadeInAndSelect(Button first)
        {
            var group = _bossDefeatedScreen.AddComponent<CanvasGroup>();
            if (isActiveAndEnabled)
            {
                group.alpha = 0f;
                StartCoroutine(AnimationHelper.FadeIn(group, AnimationHelper.MenuPanelDuration));
            }
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null) eventSystem.SetSelectedGameObject(first.gameObject);
        }

        public readonly struct BossClearedStatsSnapshot
        {
            public BossClearedStatsSnapshot(int mistakes, int hpLost, int itemsUsed, int pencilMarks)
            {
                Mistakes = mistakes;
                HpLost = hpLost;
                ItemsUsed = itemsUsed;
                PencilMarks = pencilMarks;
            }

            public int Mistakes { get; }
            public int HpLost { get; }
            public int ItemsUsed { get; }
            public int PencilMarks { get; }
        }

        public static BossClearedStatsSnapshot BuildBossClearedStats(RunDirector run)
        {
            var level = run?.CurrentLevelState;
            return new BossClearedStatsSnapshot(
                level?.Mistakes ?? 0,
                level?.HpLost ?? 0,
                level?.ItemsUsedThisLevel ?? 0,
                level?.PencilMarksUsed ?? 0);
        }

        /// <summary>Every solved puzzle of the current floor, the boss fight included (it is completed before this screen).</summary>
        public static BossClearedStatsSnapshot BuildFloorStats(RunDirector run)
        {
            var tally = run?.State?.FloorTally;
            return tally == null
                ? BuildBossClearedStats(run)
                : new BossClearedStatsSnapshot(tally.Mistakes, tally.HpLost, tally.ItemsUsed, tally.PencilMarks);
        }

        /// <summary>Removes the boss-defeated screen. Only call from a player-triggered action or a new end screen.</summary>
        public void HideBossCleared() => ClearBossClearedDynamicContent();

        public void ShowGameOver(RunDirector run, bool victory)
        {
            // Seasonal challenge: show score result instead of normal end screen
            if (run?.State?.IsSeasonalChallenge == true)
            {
                ShowSeasonalResult(run, victory);
                return;
            }

            ClearBossClearedDynamicContent();
            UseRunEndScroll(false);

            // Panels hidden by InRunController before calling ShowGameOver
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
                InRunUiFactory.SelectFirstInteractable(_gameOverPanel);
            }

            // Load the correct background art (placeholder added in BuildGameOverPanel)
            if (_gameOverPanel != null)
            {
                var bgImg = _gameOverPanel.transform.Find("PanelBackground")?.GetComponent<RawImage>();
                if (bgImg != null)
                {
                    var tex = Resources.Load<Texture2D>(victory ? "background/bg_victory" : "background/bg_defeat");
                    bgImg.texture = tex;
                    bgImg.color   = new Color(1f, 1f, 1f, tex != null ? 0.85f : 0f);
                }
            }

            ConfigureOutcomeButton(victory);

            // Dynamic title
            if (_gameOverSummary != null)
            {
                _gameOverSummary.text = victory ? T("InRun.End.Title.Victory") : T("InRun.End.Title.Defeat");
                _gameOverSummary.color = victory
                    ? GamePalette.WinGold
                    : InRunUiFactory.CursedTitleRed;
            }

            // Snapshot the profile before persisting: the level-up, the best floor and what the run moved (RM-111, RM-112).
            var classId = run.State.ClassId;
            var snapshotSave = new SaveFileService(SaveProfileService.ActiveSlot);
            var preEnvelope = snapshotSave.HasSaveFile() ? snapshotSave.Load() : null;
            var preXp = preEnvelope != null ? GetClassTotalXpFromSave(preEnvelope, classId) : 0;
            var preLevel = XpTable.DeriveLevel(preXp);
            var preMeta = preEnvelope?.MetaProgress ?? new MetaProgressionState();   // a profile's first run: from nothing
            var preBestFloor = new SudokuRoguelike.Meta.ClassGardenProgressionService().GetEntry(preMeta, classId)?.BestFloor ?? 0;

            var result = _map.BuildRunResult(victory, 0, 0);
            var newUnlocks = PersistResult(result, run);

            if (victory && result?.Mode == SudokuRoguelike.Core.GameMode.GardenRun && result.XpEarned > 0)
                SudokuRoguelike.Meta.SteamLeaderboardService.SubmitRunScore(result.XpEarned);

            // Load updated XP after persist; fallback to in-memory calculation
            var postSave = new SaveFileService(SaveProfileService.ActiveSlot);
            var postEnvelope = postSave.HasSaveFile() ? postSave.Load() : null;
            var postXp = postEnvelope != null
                ? GetClassTotalXpFromSave(postEnvelope, classId) : preXp + (result?.XpEarned ?? 0);
            var postLevel = XpTable.DeriveLevel(postXp);
            var postXpInto = postXp - XpTable.CumulativeXpForLevel(postLevel);
            var postXpToNext = postLevel < 40 ? XpTable.XpToNextLevel(postLevel) : 0;

            // [REQ: UI-STYLE-016] A Garden Run ends on the run-end scroll; the other modes keep the text layout.
            if (run.State.Mode == GameMode.GardenRun && !run.State.TutorialMode)
            {
                var floorReached = victory ? run.State.TotalFloors : run.State.CurrentFloor + 1;
                RunLengthLog.Record(postSave, run.State, victory, floorReached, Time.realtimeSinceStartup);
                ShowRunEndScroll(new RunEndSummary
                {
                    Victory = victory, State = run.State, Result = result, PreLevel = preLevel, PostLevel = postLevel,
                    XpInto = postXpInto, XpToNext = postXpToNext, NewUnlocks = newUnlocks,
                    Unlocked = postEnvelope?.MetaProgress?.UnlockedClasses,
                    SoClose = RunEndInsights.SoClose(victory, floorReached, run.CurrentLevelConfig?.IsBoss == true, CellsLeft(run),
                        preBestFloor, ClassLabel(classId)),
                    Moved = RunEndInsights.Moved(preMeta, postEnvelope?.MetaProgress, run.State, victory)
                });
                return;
            }

            if (_gameOverDetails != null)
            {
                var sb = new StringBuilder();

                // Run summary
                var className = ClassLabel(classId);
                sb.AppendLine(ParkNarrativeService.GetRunEndFlavor(victory, classId));
                sb.AppendLine();
                sb.AppendLine(F("InRun.End.Detail.ClassFloor", className, run.State.Depth));
                sb.AppendLine(F("InRun.End.Detail.HpGold", run.State.CurrentHP, run.State.MaxHP, run.State.CurrentGold));

                // XP earned this run
                var xpEarned = result?.XpEarned ?? 0;
                sb.AppendLine(F("InRun.End.Detail.XpEarned", xpEarned));
                sb.AppendLine();

                // Level-up notification
                if (preLevel != postLevel)
                    sb.AppendLine(F("InRun.End.Detail.LevelUp", preLevel, postLevel));

                // XP progress bar toward next level
                if (postLevel < 40)
                {
                    var barLen = 16;
                    var filled = postXpToNext > 0
                        ? Math.Min(barLen, (int)(postXpInto * barLen / (float)postXpToNext))
                        : 0;
                    var bar = new string('=', filled) + new string('-', barLen - filled);
                    sb.AppendLine(F("InRun.End.Detail.ClassLevel", className, postLevel));
                    sb.AppendLine(F("InRun.End.Detail.XpBar", bar, postXpInto, postXpToNext));
                    sb.AppendLine(F("InRun.End.Detail.ToLevel", postLevel + 1));
                }
                else
                {
                    sb.AppendLine(F("InRun.End.Detail.LevelMax", className));
                }

                // Run score
                if (result != null && result.RunScore > 0)
                {
                    sb.AppendLine();
                    var presenter = new EndScreenPresenter();
                    sb.AppendLine(presenter.BuildRunScoreBreakdown(result));
                }

                // New class unlocks
                if (newUnlocks != null && newUnlocks.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine(T("InRun.End.Detail.NewUnlockHeader"));
                    foreach (var cid in newUnlocks)
                        sb.AppendLine(F("InRun.End.Detail.ClassUnlocked", ClassLabel(cid)));
                }

                _gameOverDetails.text = sb.ToString().TrimEnd();
            }
        }

        private void ShowSeasonalResult(RunDirector run, bool completed)
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);

            var now = DateTime.Today;
            var level = run.CurrentLevelState;
            var state = run.State;

            var cellsCorrect = level?.CorrectPlacements ?? 0;
            var mistakes = level?.Mistakes ?? 0;
            var pencilLeft = state.CurrentPencil;
            var score = SeasonalChallengeService.CalculateScore(cellsCorrect, mistakes, pencilLeft, state.MaxPencil);
            var grade = SeasonalChallengeService.GetGrade(score, completed);
            var theme = SeasonalChallengeService.GetThemeName(now.Year, now.Month);

            // Persist personal best
            var save = new SaveFileService(SaveProfileService.ActiveSlot);
            var envelope = save.HasSaveFile() ? save.Load() : new SaveFileEnvelope();
            if (envelope.SeasonalChallenge == null) envelope.SeasonalChallenge = new SeasonalChallengeState();
            var prevBest = envelope.SeasonalChallenge.GetBest(now.Year, now.Month);

            var hpRemaining = state.CurrentHP;
            var pencilUsed = Math.Max(0, state.MaxPencil - state.CurrentPencil);
            var timeSeconds = Mathf.RoundToInt(state.TotalRunSeconds);
            if (completed)
                envelope.SeasonalChallenge.SetBest(now.Year, now.Month, score, hpRemaining, pencilUsed, timeSeconds);

            // Award Ink Stamps: +2 for S grade, +1 for first-ever monthly completion (regardless of grade).
            // [REQ: SEASONAL-STAMP-001]
            if (completed)
            {
                if (envelope.DailyGoals == null) envelope.DailyGoals = new DailyGoalState();
                var isFirstCompletion = prevBest == 0;
                if (isFirstCompletion)
                    envelope.DailyGoals.TotalInkStamps += 1;
                if (grade == "S")
                    envelope.DailyGoals.TotalInkStamps += 2;
            }

            envelope.ActiveSeasonalRunState = null;
            envelope.ActiveSeasonalPuzzle = null;
            envelope.Mastery ??= new MasteryAchievementState();
            SteamAchievementService.EvaluateModeAchievements(envelope.Mastery, GameMode.SeasonalChallenge, completed);   // [REQ: ACHIEVE-MODE-001]
            save.Save(envelope);
            RunLengthLog.Record(save, state, completed, 1, Time.realtimeSinceStartup);   // [REQ: RUN-LENGTH-001]

            // [REQ: UI-STYLE-026] The month's result on the scroll: the grade large, the score against the month's best.
            var newBest = completed && score > prevBest;
            var end = new ModeEndSummary
            {
                Painting = "bg_monthly_walk",
                Good = completed,
                Eyebrow = F("InRun.End.Mode.Monthly.Eyebrow", theme),
                Title = completed ? F("InRun.End.Mode.Monthly.Title", grade) : T("InRun.End.Mode.Monthly.Unfinished"),
                Closing = completed ? T("InRun.End.Mode.Monthly.Closing") : T("InRun.End.Mode.Monthly.ClosingUnfinished"),
                RecordHeader = T("InRun.End.Mode.Monthly.Record"),
                RecordValue = grade,
                RecordLine = F("InRun.End.Mode.Monthly.ScoreLine", Number(score)) + "\n" + BestLine(newBest, prevBest, Number(prevBest),
                    T("InRun.End.Mode.Monthly.FirstAttempt")),
                NewBest = newBest && prevBest > 0,
                Again = RunAgain.KindOf(state)
            };
            end.Rows.Add((T("InRun.End.Mode.Score"), Number(score)));
            end.Rows.Add((T("InRun.End.Mode.CellsCorrect"), Number(cellsCorrect)));
            end.Rows.Add((T("InRun.End.Mode.Mistakes"), Number(mistakes)));
            end.Rows.Add((T("InRun.End.Mode.PencilLeft"), $"{pencilLeft} / {state.MaxPencil}"));
            end.Rows.Add((T("InRun.End.Mode.Time"), Clock(timeSeconds)));
            if (!completed) AddNearMiss(end, run);
            if (completed && prevBest == 0) end.Notes.Add(T("InRun.End.Mode.Monthly.StampFirst"));
            if (completed && grade == "S") end.Notes.Add(T("InRun.End.Mode.Monthly.StampS"));
            end.Notes.Add(SeasonalChallengeService.GetCountdownLabel(DateTime.Today));
            ShowModeEndScroll(end);
        }

        public void ShowSpiritTrialsResult(RunDirector run, bool completed)
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);

            var state = run?.State;
            var level = run?.CurrentLevelState;
            if (state == null)
            {
                UseRunEndScroll(false);
                return;
            }

            var tier       = state.SpiritTrialsTier;
            var tierName   = SpiritTierLabel(tier);
            var seconds    = state.TotalRunSeconds;
            var modCount   = run?.CurrentLevelConfig?.ActiveModifiers?.Count ?? 0;
            var cells      = level?.CorrectPlacements ?? 0;
            var pencilUsed = level?.PencilMarksUsed   ?? 0;
            var mistakes   = level?.Mistakes          ?? 0;

            var score = completed
                ? SpiritTrialsService.CalculateScore(tier, seconds, modCount, cells, pencilUsed, mistakes)
                : 0;

            // Persist personal best per tier
            var save     = new SaveFileService(SaveProfileService.ActiveSlot);
            var envelope = save.HasSaveFile() ? save.Load() : new SaveFileEnvelope();
            if (envelope.Statistics == null) envelope.Statistics = new ProfileStats();
            var stats = envelope.Statistics;

            int prevBest = tier switch
            {
                SpiritTrialsTier.Apprentice  => stats.TrialsBestScore_Apprentice,
                SpiritTrialsTier.Adept       => stats.TrialsBestScore_Adept,
                SpiritTrialsTier.Master      => stats.TrialsBestScore_Master,
                _                            => stats.TrialsBestScore_Grandmaster,
            };

            envelope.Mastery ??= new MasteryAchievementState();
            var achievementsBefore = envelope.Mastery.UnlockedAchievements?.Count ?? 0;
            SteamAchievementService.EvaluateModeAchievements(envelope.Mastery, GameMode.SpiritTrials, completed, tier: tier);   // [REQ: ACHIEVE-MODE-001]
            var newAchievement = (envelope.Mastery.UnlockedAchievements?.Count ?? 0) > achievementsBefore;
            if (completed && score > prevBest)
            {
                switch (tier)
                {
                    case SpiritTrialsTier.Apprentice:  stats.TrialsBestScore_Apprentice  = score; break;
                    case SpiritTrialsTier.Adept:       stats.TrialsBestScore_Adept       = score; break;
                    case SpiritTrialsTier.Master:      stats.TrialsBestScore_Master      = score; break;
                    default:                           stats.TrialsBestScore_Grandmaster = score; break;
                }
            }
            if ((completed && score > prevBest) || newAchievement)
                new ProfileService(save).UpdateStats(envelope, stats);
            RunLengthLog.Record(save, state, completed, 1, Time.realtimeSinceStartup);   // [REQ: RUN-LENGTH-001]

            // [REQ: UI-STYLE-026] The trial's result on the scroll: the score large against the tier's best.
            var newBest = completed && score > prevBest;
            var end = new ModeEndSummary
            {
                Painting = "bg_menu_trials_results",
                Good = completed,
                Eyebrow = F("InRun.End.Mode.Trials.Eyebrow", tierName),
                Title = completed ? T("InRun.End.Mode.Trials.Passed") : T("InRun.End.Mode.Trials.Failed"),
                Closing = completed ? T("InRun.End.Mode.Trials.Closing") : T("InRun.End.Trials.AwaitAgain"),
                RecordHeader = F("InRun.End.Mode.Trials.Record", tierName),
                RecordValue = completed ? Number(score) : "\u2013",
                RecordLine = completed
                    ? BestLine(newBest, prevBest, Number(prevBest), T("InRun.End.Mode.Trials.FirstCompletion"))
                    : prevBest > 0 ? F("InRun.End.Mode.Best", Number(prevBest)) : T("InRun.End.Mode.Trials.NoScore"),
                NewBest = newBest && prevBest > 0,
                Again = RunAgain.KindOf(state)
            };
            if (!completed) AddNearMiss(end, run);
            if (completed)
            {
                end.Rows.Add((T("InRun.End.Mode.Score"), Number(score)));
                end.Rows.Add((T("InRun.End.Mode.Time"), Clock(seconds)));
            }
            end.Rows.Add((T("InRun.End.Mode.CellsFilled"), Number(cells)));
            end.Rows.Add((T("InRun.End.Mode.PencilMarks"), Number(pencilUsed)));
            end.Rows.Add((T("InRun.End.Mode.Mistakes"), Number(mistakes)));
            if (modCount > 0) end.Rows.Add((T("InRun.End.Mode.Modifiers"), "+" + modCount));
            ShowModeEndScroll(end);
        }

        public void ShowEndlessZenResult(RunDirector run)
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);

            var state   = run?.State;
            var level   = run?.CurrentLevelState;
            var depth   = state?.Depth ?? 0;
            var seconds = state?.TotalRunSeconds ?? 0f;
            var mistakes = run?.GetAnalytics()?.TotalMistakes ?? level?.Mistakes ?? 0;

            // Persist depth + session count
            var save     = new SaveFileService(SaveProfileService.ActiveSlot);
            var envelope = save.HasSaveFile() ? save.Load() : new SaveFileEnvelope();
            if (envelope.Statistics == null) envelope.Statistics = new ProfileStats();
            var stats   = envelope.Statistics;
            var prevBest = stats.HighestEndlessDepth;
            var isNewBest = depth > prevBest;

            if (isNewBest) stats.HighestEndlessDepth = depth;
            stats.TotalZenSessions += 1;
            envelope.Mastery ??= new MasteryAchievementState();
            SteamAchievementService.EvaluateModeAchievements(envelope.Mastery, GameMode.EndlessZen, false, endlessDepth: depth);   // [REQ: ACHIEVE-MODE-001]
            new ProfileService(save).UpdateStats(envelope, stats);
            if (state != null) RunLengthLog.Record(save, state, false, depth, Time.realtimeSinceStartup);   // [REQ: RUN-LENGTH-001]

            // [REQ: UI-STYLE-026] The dive on the scroll: the depth large against the deepest dive.
            var end = new ModeEndSummary
            {
                Painting = "bg_menu_zen_results",
                Good = true,
                Eyebrow = T("Endless Zen"),
                Title = F("InRun.End.Mode.Zen.Title", depth),
                Closing = T("InRun.End.Mode.Zen.Closing"),
                RecordHeader = T("InRun.End.Mode.Zen.Record"),
                RecordValue = Number(Math.Max(depth, prevBest)),
                RecordLine = prevBest <= 0 ? T("InRun.End.Mode.Zen.FirstSession")
                    : isNewBest ? F("InRun.End.Mode.NewBestLine", Number(prevBest)) : F("InRun.End.Mode.Zen.ThisTime", Number(depth)),
                NewBest = isNewBest && prevBest > 0,
                Again = RunAgain.KindOf(state)
            };
            end.Rows.Add((T("InRun.End.Mode.Zen.Depth"), Number(depth)));
            end.Rows.Add((T("InRun.End.Mode.Time"), Clock(seconds)));
            end.Rows.Add((T("InRun.End.Mode.Mistakes"), Number(mistakes)));
            end.Rows.Add((T("InRun.End.Mode.Zen.Sessions"), Number(stats.TotalZenSessions)));
            ShowModeEndScroll(end);
        }

        // [REQ: RUN-END-NEAR-001] A lost trial or walk: how many cells were left.
        private static void AddNearMiss(ModeEndSummary end, RunDirector run)
        {
            var left = CellsLeft(run);
            if (left > 0)
                end.Notes.Insert(0, LocalizationService.FormatPlural(left, "InRun.End.Near.CellsOne", "InRun.End.Near.CellsMany",
                    "So close: {0} cell from the end.", "So close: {0} cells from the end."));
        }

        // "New best (before: …)", "Your best: …" or the first-attempt line.
        private static string BestLine(bool newBest, int prevBest, string prevText, string first) =>
            prevBest <= 0 ? first : newBest ? F("InRun.End.Mode.NewBestLine", prevText) : F("InRun.End.Mode.Best", prevText);

        private static string T(string key) => LocalizationService.T(key);

        // [REQ: RUN-END-NEAR-001] The empty cells of the board the run ended on.
        private static int CellsLeft(RunDirector run)
        {
            var board = run?.CurrentBoard;
            if (board == null) return 0;
            var left = 0;
            for (var r = 0; r < board.Size; r++)
            for (var c = 0; c < board.Size; c++)
                if (board.Cells[r, c] == 0) left++;
            return left;
        }

        private void ClearBossClearedDynamicContent()
        {
            if (_bossDefeatedScreen == null) return;
            _bossDefeatedScreen.SetActive(false);
            UnityEngine.Object.Destroy(_bossDefeatedScreen);
            _bossDefeatedScreen = null;
        }

        private void ConfigureOutcomeButton(bool victory)
        {
            if (_gameOverBackBtn == null) return;
            InRunUiFactory.ApplyActionIcon(_gameOverBackBtn, victory ? UiAction.Confirm : UiAction.Back, compact: true);
            var label = _gameOverBackBtn.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
                label.text = T("Back");
        }

        private static string F(string key, params object[] args) =>
            LocalizationService.Format(key, key, args);

        private static string ClassLabel(ClassId classId)
        {
            var def = ClassCatalog.GetDefinition(classId);
            return def != null ? LocalizationService.T(def.Name, def.Name) : classId.ToString();
        }

        private static string SpiritTierLabel(SpiritTrialsTier tier) =>
            LocalizationService.T($"SpiritTrials.Tier.{tier}", tier.ToString());

        private static int GetClassTotalXpFromSave(SaveFileEnvelope envelope, ClassId classId)
        {
            var entries = envelope?.MetaProgress?.GardenProgression?.ClassEntries;
            if (entries == null) return 0;
            for (var i = 0; i < entries.Count; i++)
                if (entries[i].ClassId == classId) return entries[i].TotalXp;
            return 0;
        }

        private List<ClassId> PersistResult(RunResult result, RunDirector run)
        {
            if (result == null || result.TutorialMode) return null;
            if (run?.State?.IsSeasonalChallenge == true) return null; // seasonal: no progression
            if (result.DisableProgressionRewards || result.Mode != GameMode.GardenRun) return null;

            if (run != null)
            {
                var analytics = run.GetAnalytics();
                result.ItemsUsedThisRun = analytics?.TotalItemsUsed
                    ?? run.CurrentLevelState?.ItemsUsedThisLevel ?? 0;
                result.AcquiredRelic = run.State.HasRelic;
                if (analytics != null)
                    result.ClearedStageNoPencilNoHpLoss = analytics.TotalMistakes == 0 && !analytics.PencilEverUsed;
            }

            var save = new SaveFileService(SaveProfileService.ActiveSlot);
            var profile = new ProfileService(save);
            var newUnlocks = profile.RecordRunAndGetNewUnlocks(result, run?.State);
            // ActiveRunState is cleared inside RecordRunAndGetNewUnlocks to avoid a race condition
            // where a second async save would overwrite the meta progress from the first save.

            return newUnlocks;
        }
    }
}
