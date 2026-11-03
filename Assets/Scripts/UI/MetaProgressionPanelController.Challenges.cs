using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Core;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Meta;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-043] [REQ: CLASS-CHALLENGE-002] Class challenges on the garden of classes (RM-116, owner's pick A): three
    // seals at the foot of every plaque, red when met, and the chosen class's challenges as the scroll's second page; Y (F
    // on the keyboard) or a click on the tabs turns between the level path and the challenges.
    public sealed partial class MetaProgressionPanelController
    {
        private static readonly Color SealOpen = new Color(0.48f, 0.16f, 0.12f, 0.9f);
        private static readonly Color TabIdle = new Color(0.29f, 0.23f, 0.16f, 1f);
        private static readonly Color TabText = new Color(0.91f, 0.86f, 0.75f, 1f);

        private bool _challengesPage;

        /// <summary>True while the scroll shows the challenges page.</summary>
        public bool ShowingChallenges => _challengesPage;

        /// <summary>Turns the scroll between the level path and the challenges of the chosen class.</summary>
        public void TogglePage() => ShowPage(!_challengesPage);

        public void ShowPage(bool challenges)
        {
            _challengesPage = challenges;
            Select(_selected, true);
        }

        // The three seals at the foot of a plaque: a red disc when met, an ink ring when open.
        private void BuildSeals(Transform board, ClassId id)
        {
            for (var i = 0; i < ClassChallenges.PerClass; i++)
            {
                var met = ClassChallenges.IsMet(_meta, id, i);
                var seal = ScrollUiKit.Spot(board, $"Seal_{i}", new Vector2(0.34f + i * 0.16f, 0.085f), new Vector2(22f, 22f),
                    met ? ScrollUiKit.Vermilion : SealOpen, met ? ScrollUiKit.Disc() : ScrollUiKit.Enso());
                seal.raycastTarget = false;
            }
        }

        // The tabs above the scroll; the pad turns them with Y, so they keep out of its navigation.
        private void BuildPageTabs()
        {
            ScrollUiKit.DestroyChildrenNow(_scroll, "PageTabs");
            var tabs = ScrollUiKit.Area(_scroll, "PageTabs", new Vector2(0.50f, 1.012f), new Vector2(0.97f, 1.062f));
            var hint = ScrollUiKit.Label(tabs, "Key", DeviceHint.For("MetaProgression.Tab.Key"), 16, TextAnchor.MiddleRight, TabText,
                new Vector2(0f, 0f), new Vector2(0.10f, 1f));
            DeviceHint.Attach(hint, "MetaProgression.Tab.Key");
            AddTab(tabs, "TabPath", T("MetaProgression.Tab.Path", "Path"), new Vector2(0.12f, 0f), new Vector2(0.44f, 1f), !_challengesPage,
                () => ShowPage(false));
            AddTab(tabs, "TabChallenges", T("MetaProgression.Tab.Challenges", "Challenges"), new Vector2(0.46f, 0f), new Vector2(1f, 1f),
                _challengesPage, () => ShowPage(true));
        }

        private static void AddTab(RectTransform tabs, string name, string label, Vector2 min, Vector2 max, bool on, Action click)
        {
            var tab = ScrollUiKit.PillButton(tabs, name, min, max, label, 18, on ? ScrollUiKit.Paper : TabIdle,
                on ? ScrollUiKit.Ink : TabText, 8f);
            tab.navigation = new Navigation { mode = Navigation.Mode.None };
            tab.onClick.AddListener(() => click());
        }

        // The challenges page: the class's name, how many seals it has, and each challenge with its seal, rule and state.
        private void BuildChallengesPage(RectTransform page, ClassId id)
        {
            var def = ClassCatalog.GetDefinition(id);
            ScrollUiKit.Label(page, "ClassName", T(def.Name, def.Name), 44, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0f, 0.905f), new Vector2(1f, 1f), 24);
            ScrollUiKit.Label(page, "ClassSub", F("MetaProgression.Challenges.Sub", "Challenges · {0} of {1} seals",
                    ClassChallenges.MetCount(_meta, id), ClassChallenges.PerClass), 19, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0f, 0.86f), new Vector2(1f, 0.905f), 13);
            for (var i = 0; i < ClassChallenges.PerClass; i++)
            {
                var top = 0.82f - i * 0.215f;
                var row = ScrollUiKit.Area(page, $"Challenge_{i}", new Vector2(0f, top - 0.2f), new Vector2(1f, top));
                var metOn = ClassChallenges.MetOn(_meta, id, i);
                var seal = ScrollUiKit.Spot(row, "Seal", new Vector2(0.06f, 0.62f), new Vector2(54f, 54f),
                    metOn != null ? ScrollUiKit.Vermilion : SealOpen, metOn != null ? ScrollUiKit.Disc() : ScrollUiKit.Enso());
                seal.raycastTarget = false;
                ScrollUiKit.Label(row, "Name", ClassChallenges.Name(id, i), 24, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                    new Vector2(0.15f, 0.70f), new Vector2(1f, 0.96f), 16);
                ScrollUiKit.Label(row, "Rule", ClassChallenges.Description(id, i), 18, TextAnchor.UpperLeft, ScrollUiKit.InkSoft,
                    new Vector2(0.15f, 0.38f), new Vector2(1f, 0.70f), 13);
                ScrollUiKit.Label(row, "State", metOn != null ? F("MetaProgression.Challenges.MetOn", "Met on {0}", Day(metOn))
                        : T("MetaProgression.Challenges.NotYet", "Not yet"), 17, TextAnchor.MiddleLeft, ScrollUiKit.Vermilion,
                    new Vector2(0.15f, 0.19f), new Vector2(0.62f, 0.38f), 13);
                ScrollUiKit.Label(row, "Steam", T("MetaProgression.Challenges.Steam", "STEAM ACHIEVEMENT"), 14, TextAnchor.MiddleRight,
                    ScrollUiKit.InkSoft, new Vector2(0.62f, 0.19f), new Vector2(1f, 0.38f), 13);
                ScrollUiKit.Fill(row, "Line", new Vector2(0f, 0.02f), new Vector2(1f, 0.026f), new Color(0.24f, 0.18f, 0.12f, 0.2f))
                    .raycastTarget = false;
            }
        }

        // "14 October 2026" in the game's language; a date the save cannot read stays as it is.
        private static string Day(string iso) =>
            DateTime.TryParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
                ? day.ToString("d MMMM yyyy", LocalizationService.Culture) : iso;
    }
}
