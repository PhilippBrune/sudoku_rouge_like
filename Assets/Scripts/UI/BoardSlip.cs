using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: SEED-BOARD-003] A Steam board on a paper slip with two bookmarks on its top edge, All and Friends (RM-101, the
    /// owner's pick A of docs/design/layout-proposals/seed_board, 2026-11-04). A click, Y on the pad or F on the keyboard
    /// turns it. All: the players, the place and the top rows with the player's own row when it is not among them.
    /// Friends: the friends on the board, the place among them and their rows with the player's.
    /// </summary>
    public sealed class BoardSlip : MonoBehaviour
    {
        private const string Soft = "<size=16><color=#6B5F4C>{0}</color></size>";
        private Text _text;
        private Image _allTab, _friendsTab;
        private Text _allLabel, _friendsLabel;
        private BoardStanding _standing;
        private string _headerKey;
        private int _rows;
        private bool _friends, _answered;

        public bool FriendsShown => _friends;

        /// <param name="headerKey">The All side's header, "{0}" the number of players ("DailyWalk.Board.Header").</param>
        /// <param name="rows">The top rows shown (3; 1 where the slip is short).</param>
        public static BoardSlip Build(Transform parent, string name, Vector2 min, Vector2 max, string headerKey, int rows = 3)
        {
            var paper = ScrollUiKit.Fill(parent, name, min, max, ScrollUiKit.Paper);
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -10f);
            var slip = paper.gameObject.AddComponent<BoardSlip>();
            slip._headerKey = headerKey;
            slip._rows = rows;
            slip._text = ScrollUiKit.Label(paper.transform, "BoardText", LocalizationService.T("DailyWalk.Board.Loading"), 19,
                TextAnchor.UpperLeft, ScrollUiKit.Ink, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.93f), 12);
            slip._text.supportRichText = true;
            (slip._allTab, slip._allLabel) = slip.Tab(paper.transform, "TabAll", 0.05f, 0.30f, "Board.Tab.All", false);
            (slip._friendsTab, slip._friendsLabel) = slip.Tab(paper.transform, "TabFriends", 0.32f, 0.66f, "Board.Tab.Friends", true);
            slip.Render();
            return slip;
        }

        // A bookmark above the slip's top edge, 34 px tall; the chosen one has the paper's colour.
        private (Image, Text) Tab(Transform paper, string name, float left, float right, string key, bool friends)
        {
            var tab = ScrollUiKit.Fill(paper, name, new Vector2(left, 1f), new Vector2(right, 1f), ScrollUiKit.Paper);
            tab.rectTransform.offsetMax = new Vector2(0f, 34f);
            tab.raycastTarget = true;
            var button = tab.gameObject.AddComponent<Button>();
            button.targetGraphic = tab;
            button.onClick.AddListener(() => ShowFriends(friends));
            var label = ScrollUiKit.Label(tab.transform, "Label", LocalizationService.T(key), 18, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                Vector2.zero, Vector2.one, 11);
            label.supportRichText = true;   // the key's hint is smaller
            return (tab, label);
        }

        public void Show(BoardStanding standing)
        {
            _standing = standing;
            _answered = true;
            Render();
        }

        public void ShowFriends(bool friends)
        {
            _friends = friends;
            Render();
        }

        private void Update()
        {
            if (FeedbackScreen.IsOpen) return;   // [REQ: FEEDBACK-001] keys go to the feedback form
            if (PadButtons.Pressed(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.F)) ShowFriends(!_friends);
        }

        private void Render()
        {
            if (_text == null) return;
            var keyHint = InputDeviceTracker.UsingPad ? "Y" : "F";
            _friendsLabel.text = $"{LocalizationService.T("Board.Tab.Friends")}  <size=14>{keyHint}</size>";
            Paint(_allTab, _allLabel, !_friends);
            Paint(_friendsTab, _friendsLabel, _friends);
            _text.text = !_answered ? LocalizationService.T("DailyWalk.Board.Loading")
                : _friends ? FriendsText(_standing, _rows + 1) : AllText(_standing, _headerKey, _rows);
        }

        private static void Paint(Image tab, Text label, bool chosen)
        {
            tab.color = chosen ? ScrollUiKit.Paper : new Color(0.36f, 0.27f, 0.20f, 1f);
            label.color = chosen ? ScrollUiKit.Ink : new Color(0.91f, 0.86f, 0.75f, 1f);
        }

        /// <summary>The All side: the header with the players, the place (and among friends), the top rows and the player's
        /// own row when it is not among them; without Steam a line that says so.</summary>
        public static string AllText(BoardStanding standing, string headerKey, int rows = 3)
        {
            if (standing == null)
                return string.Format(Soft, LocalizationService.T("GameModes.Daily.BoardHeader").ToUpperInvariant()) + "\n\n"
                       + LocalizationService.T("DailyWalk.Board.Offline");
            var lines = new List<string> { string.Format(Soft, LocalizationService.Format(headerKey, "{0}", standing.Entries).ToUpperInvariant()) };
            if (standing.Rank > 0)
            {
                lines.Add($"<size={PlaceSize(rows)}>{LocalizationService.Format("DailyWalk.Board.Place", "Place {0} of {1}", standing.Rank, standing.Entries)}</size>");
                if (standing.Friends > 1 && rows > 1)
                    lines.Add(LocalizationService.Format("DailyWalk.Board.Friends", "{0} of {1} among friends", standing.FriendRank, standing.Friends));
            }
            else lines.Add(LocalizationService.T("DailyWalk.Board.NotOnIt"));
            for (var i = 0; i < standing.Top.Count && i < rows; i++)
                lines.Add($"{i + 1}. {standing.Top[i].Name}  <color=#3A3228>{Number(standing.Top[i].Score)}</color>{Clock(standing.TopSeconds, i)}");
            if (standing.Rank > System.Math.Min(rows, standing.Top.Count))
                lines.Add($"<color=#9E2A2B>{standing.Rank}. {LocalizationService.T("DailyWalk.Board.You")}</color>");
            return string.Join("\n", lines);
        }

        /// <summary>The Friends side: how many friends are on the board, the place among them, their rows (the player's in
        /// vermilion).</summary>
        public static string FriendsText(BoardStanding standing, int rows = 4)
        {
            if (standing == null)
                return string.Format(Soft, LocalizationService.T("Board.Friends.Title").ToUpperInvariant()) + "\n\n"
                       + LocalizationService.T("DailyWalk.Board.Offline");
            var lines = new List<string>
            {
                string.Format(Soft, LocalizationService.Format("Board.Friends.Header", "Among friends · {0} on the board", standing.Friends).ToUpperInvariant())
            };
            if (standing.FriendTop.Count == 0)
            {
                lines.Add(LocalizationService.T("Board.Friends.None"));
                return string.Join("\n", lines);
            }
            if (standing.FriendRank > 0)
                lines.Add($"<size={PlaceSize(rows - 1)}>{LocalizationService.Format("DailyWalk.Board.Place", "Place {0} of {1}", standing.FriendRank, standing.Friends)}</size>");
            for (var i = 0; i < standing.FriendTop.Count && i < rows; i++)
            {
                var (name, score) = standing.FriendTop[i];
                var row = $"{i + 1}. {name ?? LocalizationService.T("DailyWalk.Board.You")}  <color=#3A3228>{Number(score)}</color>{Clock(standing.FriendTopSeconds, i)}";
                lines.Add(name == null ? $"<color=#9E2A2B>{row}</color>" : row);
            }
            if (standing.FriendRank > System.Math.Min(rows, standing.FriendTop.Count))
                lines.Add($"<color=#9E2A2B>{standing.FriendRank}. {LocalizationService.T("DailyWalk.Board.You")}</color>");
            return string.Join("\n", lines);
        }

        // A short slip (one row, the run end) gets a smaller place line.
        private static int PlaceSize(int rows) => rows <= 1 ? 26 : 40;

        private static string Number(int value) => value.ToString("N0", LocalizationService.Culture);

        // [REQ: SEED-BOARD-004] The walk's time beside its score, where the board keeps one.
        public static string Clock(List<int> seconds, int index) =>
            seconds == null || index >= seconds.Count || seconds[index] <= 0 ? string.Empty
                : $"  <size=16><color=#6B5F4C>{seconds[index] / 60}:{seconds[index] % 60:00}</color></size>";
    }
}
