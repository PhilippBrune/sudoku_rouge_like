using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: RUN-HISTORY-002] The main menu's Recent Runs slip (RM-230, the owner's pick C): the last five runs with class,
    /// result and seed; a row opens the run's card with its last board, the last wrong digit, items, relics and "Walk seed",
    /// which opens Select Class with the seed filled in. The slip sits on the path beside the menu; there is no room for it
    /// under the run card, where the mockup had it. Without a finished run it stays hidden.
    /// </summary>
    public sealed class RecentRunsSlip : MonoBehaviour
    {
        private const float BoardSide = 540f;
        private static readonly Color Won = new Color(0.18f, 0.42f, 0.23f, 1f);
        private static readonly Color RowOn = new Color(0.91f, 0.77f, 0.42f, 0.42f);
        private static GameObject _card;
        private static int _closedFrame = -1;

        private MainMenuController _menu;
        private RectTransform _rows;
        private readonly List<Button> _buttons = new List<Button>();
        private Button _openRow;

        /// <summary>The card is up, or closed this frame: the main menu leaves B, Esc and A alone.</summary>
        public static bool CardOpen => _card != null || Time.frameCount == _closedFrame;

        internal static RecentRunsSlip Build(RectTransform parent, MainMenuController menu, Vector2 min, Vector2 max)
        {
            var paper = ScrollUiKit.Fill(parent, "RecentRunsSlip", min, max, ScrollUiKit.Paper);
            paper.raycastTarget = true;
            paper.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.55f);
            ScrollUiKit.Fill(paper.transform, "Band", new Vector2(0f, 0.982f), Vector2.one, ScrollUiKit.Vermilion);
            ScrollUiKit.Label(paper.transform, "RecentTitle", T("RecentRuns.Title").ToUpperInvariant(), 15, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.97f), 10);
            var slip = paper.gameObject.AddComponent<RecentRunsSlip>();
            slip._menu = menu;
            slip._rows = ScrollUiKit.Area(paper.transform, "Rows", new Vector2(0.02f, 0.025f), new Vector2(0.98f, 0.885f));
            return slip;
        }

        /// <summary>Shows the runs of the save (MainMenuHomeWidgetsController, whenever the main menu is shown).</summary>
        public void Refresh(SaveFileEnvelope envelope)
        {
            ScrollUiKit.DestroyChildrenNow(_rows);
            _buttons.Clear();
            var runs = envelope?.RecentRuns ?? new List<RecentRun>();
            gameObject.SetActive(runs.Count > 0);
            for (var i = 0; i < runs.Count && i < Run.RecentRuns.MaxEntries; i++)
                _buttons.Add(BuildRow(i, runs[i]));
            WireNavigation();
        }

        private Button BuildRow(int index, RecentRun run)
        {
            var step = 1f / Run.RecentRuns.MaxEntries;
            var row = ScrollUiKit.Fill(_rows, $"RecentRun_{index}", new Vector2(0f, 1f - (index + 1) * step), new Vector2(1f, 1f - index * step), Color.white);
            row.raycastTarget = true;
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row;
            var colors = ScrollUiKit.StateColors(new Color(1f, 1f, 1f, 0f));
            colors.highlightedColor = colors.selectedColor = RowOn;
            colors.pressedColor = RowOn;
            button.colors = colors;
            button.onClick.AddListener(() => OpenCard(run, button));

            if (ClassCatalog.IsClass(run.ClassId))
                ScrollUiKit.Icon(row.transform, "Icon", "class/icon_" + ClassCatalog.GetIconName(run.ClassId), new Vector2(0.015f, 0.08f), new Vector2(0.105f, 0.92f), Color.white);
            ScrollUiKit.Label(row.transform, "Name", RunName(run), 18, TextAnchor.LowerLeft, ScrollUiKit.Ink, new Vector2(0.125f, 0.50f), new Vector2(0.66f, 0.98f), 11);
            ScrollUiKit.Label(row.transform, "Meta", MetaLine(run), 14, TextAnchor.UpperLeft, ScrollUiKit.InkSoft, new Vector2(0.125f, 0.04f), new Vector2(0.72f, 0.50f), 9);
            ScrollUiKit.Label(row.transform, "Result", Result(run), 15, TextAnchor.LowerRight, run.Victory ? Won : ScrollUiKit.Vermilion,
                new Vector2(0.60f, 0.50f), new Vector2(0.985f, 0.98f), 10);
            ScrollUiKit.Label(row.transform, "Seed", run.Seed.ToString(CultureInfo.InvariantCulture), 15, TextAnchor.UpperRight, ScrollUiKit.Ink,
                new Vector2(0.72f, 0.04f), new Vector2(0.985f, 0.50f), 10);
            return button;
        }

        // Up and down along the rows, left to New Run, right and back to the Daily Walk.
        private void WireNavigation()
        {
            var column = transform.parent;
            var daily = column.Find("DailyWidget/BtnDailyWidget")?.GetComponent<Button>();
            var start = column.parent.Find("BtnStart")?.GetComponent<Button>();
            for (var i = 0; i < _buttons.Count; i++)
                _buttons[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? _buttons[i - 1] : null,
                    selectOnDown = i + 1 < _buttons.Count ? _buttons[i + 1] : null,
                    selectOnLeft = start,
                    selectOnRight = daily
                };
            if (daily == null) return;
            var nav = daily.navigation;
            nav.selectOnLeft = _buttons.Count > 0 ? _buttons[0] : start;
            daily.navigation = nav;
        }

        private void Update()
        {
            if (_card == null || FeedbackScreen.IsOpen) return;
            if (PadButtons.Pressed(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape)) { CloseCard(); return; }
            if (PadButtons.ConfirmPressed())
                EventSystem.current?.currentSelectedGameObject?.GetComponent<Button>()?.onClick.Invoke();
        }

        private void OpenCard(RecentRun run, Button row)
        {
            CloseCard();
            _openRow = row;
            var dim = ScrollUiKit.Fill(CardParent, "RecentRunCard", Vector2.zero, Vector2.one, new Color(0.03f, 0.02f, 0.01f, 0.72f));
            dim.raycastTarget = true;
            _card = dim.gameObject;
            var paper = ScrollUiKit.Fill(dim.transform, "Paper", new Vector2(0.17f, 0.12f), new Vector2(0.83f, 0.88f), ScrollUiKit.Paper);
            paper.raycastTarget = true;
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -12f);
            var t = paper.transform;
            ScrollUiKit.Fill(t, "Band", new Vector2(0f, 0.992f), Vector2.one, ScrollUiKit.Vermilion);
            var eyebrow = string.Join(" · ", When(run), RunName(run), ModeLine(run), Run.RunLengthLog.FormatDuration(run.ActiveSeconds));
            ScrollUiKit.Label(t, "CardEyebrow", eyebrow.ToUpperInvariant(), 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.03f, 0.92f), new Vector2(0.97f, 0.97f), 10);
            var hasBoard = run.BoardSize > 0;
            ScrollUiKit.Label(t, "CardTitle", hasBoard ? F("RecentRuns.Card.LastBoard", "{0}: the last board", Result(run)) : Result(run), 36,
                TextAnchor.MiddleLeft, ScrollUiKit.Ink, new Vector2(0.03f, 0.83f), new Vector2(0.97f, 0.92f), 20);
            if (hasBoard) DrawBoard(t, run);

            var infoMin = hasBoard ? 0.50f : 0.03f;
            ScrollUiKit.Label(t, "CardInfo", Details(run), 20, TextAnchor.UpperLeft, ScrollUiKit.Ink, new Vector2(infoMin, 0.34f), new Vector2(0.97f, 0.80f), 13);
            if (hasBoard && run.WrongRow >= 0)
            {
                var note = ScrollUiKit.Fill(t, "CardMistake", new Vector2(infoMin, 0.235f), new Vector2(0.97f, 0.315f), ScrollUiKit.Vermilion, ScrollUiKit.Rounded(8f));
                ScrollUiKit.Label(note.transform, "Text", F("RecentRuns.Card.Mistake", "The last mistake: a {0}, it was {1}", run.WrongDigit, run.RightDigit), 18,
                    TextAnchor.MiddleCenter, Color.white, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f), 11);
            }

            var close = ScrollUiKit.PillButton(t, "BtnRecentClose", new Vector2(0.79f, 0.05f), new Vector2(0.97f, 0.135f), T("RecentRuns.Card.Close"), 22,
                ScrollUiKit.Wood, ScrollUiKit.PaperText, 22f);
            close.onClick.AddListener(CloseCard);
            Button walk = null;
            if (CanWalk(run))
            {
                walk = ScrollUiKit.PillButton(t, "BtnRecentWalk", new Vector2(0.50f, 0.05f), new Vector2(0.77f, 0.135f),
                    F("RecentRuns.Card.Walk", "Walk seed {0}", run.Seed), 22, ScrollUiKit.Vermilion, Color.white, 22f);
                walk.onClick.AddListener(() => { CloseCard(); _menu.WalkSeed(run.Seed); });
                walk.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = close };
                close.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = walk };
            }
            EventSystem.current?.SetSelectedGameObject((walk != null ? walk : close).gameObject);
        }

        // The dim layer covers the whole menu, not just the column the slip hangs on.
        private Transform CardParent => transform.GetComponentInParent<Canvas>().rootCanvas.transform;

        private void CloseCard()
        {
            if (_card == null) return;
            ScrollUiKit.DestroyNow(_card);
            _card = null;
            _closedFrame = Time.frameCount;
            if (_openRow != null && _openRow.gameObject.activeInHierarchy) EventSystem.current?.SetSelectedGameObject(_openRow.gameObject);
        }

        private void OnDisable()
        {
            if (_card != null) { ScrollUiKit.DestroyNow(_card); _card = null; }
        }

        private static void DrawBoard(Transform paper, RecentRun run)
        {
            var area = ScrollUiKit.Fill(paper, "CardBoard", new Vector2(0.04f, 0.09f), new Vector2(0.04f, 0.09f), ScrollUiKit.Ink);
            area.rectTransform.pivot = Vector2.zero;
            area.rectTransform.sizeDelta = new Vector2(BoardSide, BoardSide);
            var n = run.BoardSize;
            var font = Mathf.RoundToInt(BoardSide / n * 0.5f);
            for (var r = 0; r < n; r++)
            for (var c = 0; c < n; c++)
            {
                var i = r * n + c;
                var wrong = r == run.WrongRow && c == run.WrongCol;
                var shade = wrong ? new Color(0.95f, 0.78f, 0.73f, 1f) : (run.Regions[i] - '0') % 2 == 1 ? ScrollUiKit.PaperDeep : ScrollUiKit.Paper;
                var cell = ScrollUiKit.Fill(area.transform, $"Cell_{r}_{c}", new Vector2((float)c / n, 1f - (r + 1f) / n), new Vector2((c + 1f) / n, 1f - (float)r / n), shade);
                cell.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
                cell.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
                var digit = wrong ? run.WrongDigit : run.Digits[i] - '0';
                if (digit > 0)
                    ScrollUiKit.Label(cell.transform, "Digit", digit.ToString(CultureInfo.InvariantCulture), font, TextAnchor.MiddleCenter, wrong ? ScrollUiKit.Vermilion : ScrollUiKit.Ink,
                        Vector2.zero, Vector2.one, 0, run.Givens[i] == '1' ? FontStyle.Bold : FontStyle.Normal);
                if (wrong)
                    ScrollUiKit.Fill(cell.transform, "Ring", new Vector2(-0.22f, -0.22f), new Vector2(1.22f, 1.22f), ScrollUiKit.Vermilion, ScrollUiKit.Enso());
            }
        }

        private static bool CanWalk(RecentRun run) => !run.MonthlyWalk
            && MainMenuController.TryParseSeed(run.Seed.ToString(CultureInfo.InvariantCulture), out _);

        private static string RunName(RecentRun run) => run.DailyWalk ? T("Daily Walk") : run.MonthlyWalk ? T("Monthly Walk") : ClassCatalog.DisplayName(run.ClassId);

        private static string Result(RecentRun run) => run.Victory ? T("RecentRuns.Won") : F("RecentRuns.Lost", "Lost on Floor {0}", run.FloorReached);

        private static string ModeLine(RecentRun run) => run.DailyWalk || run.MonthlyWalk ? RunName(run) : $"{T("Garden Run")} H{run.HarmonyLevel}";

        private static string MetaLine(RecentRun run)
        {
            var parts = new List<string> { When(run) };
            if (!run.DailyWalk && !run.MonthlyWalk) parts.Add(ModeLine(run));
            parts.Add(Run.RunLengthLog.FormatDuration(run.ActiveSeconds));
            return string.Join(" · ", parts);
        }

        /// <summary>"today 21:40", "yesterday" or "5 Nov", in the game's language.</summary>
        private static string When(RecentRun run)
        {
            if (!DateTime.TryParse(run.EndedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var ended)) return string.Empty;
            var local = ended.ToLocalTime();
            if (local.Date == DateTime.Today) return F("RecentRuns.Today", "today {0}", local.ToString("HH:mm", CultureInfo.InvariantCulture));
            return local.Date == DateTime.Today.AddDays(-1) ? T("RecentRuns.Yesterday") : local.ToString("d MMM", LocalizationService.Culture);
        }

        private static string Details(RecentRun run)
        {
            var lines = new List<string>();
            if (run.BoardSize > 0)
                lines.Add(F("RecentRuns.Card.Where", "{0}, tile {1}, a {2}×{2} {3}.", FloorThemeData.GetFloorName(run.Garden), run.Tile, run.BoardSize,
                    new string('★', Mathf.Clamp(run.Stars, 1, 7))));
            if (run.Rules.Count > 0) lines.Add(F("RecentRuns.Card.Rules", "Rules: {0}.", string.Join(", ", run.Rules.Select(BossService.GetModifierName))));
            if (run.Items.Count > 0) lines.Add(F("RecentRuns.Card.Items", "Items: {0}.", string.Join(", ", run.Items.Select(ItemService.GetItemName))));
            if (run.Relics.Count > 0) lines.Add(F("RecentRuns.Card.Relics", "Relics: {0}.", string.Join(", ", run.Relics.Select(RelicService.GetRelicName))));
            if (CanWalk(run)) lines.Add("\n" + T("RecentRuns.Card.SeedNote"));
            return string.Join("\n", lines);
        }

        private static string T(string key) => LocalizationService.T(key);

        private static string F(string key, string fallback, params object[] args) => LocalizationService.Format(key, fallback, args);
    }
}
