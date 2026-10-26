using System;
using System.Collections.Generic;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: UI-STYLE-028] [REQ: DAILY-WALK-003] The XP that waits for a class: the sum, the eight class seals on a red
    /// cord (a locked class dark), the chosen one lifted with a vermilion ring, the XP stroke and "Lv 4 → 6" for it, and
    /// the button that gives it. The Daily Walk's result and Game Modes (while XP still waits) use it.
    /// </summary>
    public sealed class DailyWalkGiveView : MonoBehaviour
    {
        private const float SealSize = 74f, SealStep = 0.122f, CordY = 0.55f;
        private static readonly Color SealPaper = new Color(0.94f, 0.89f, 0.78f, 1f);
        private static readonly Color SealLocked = new Color(0.29f, 0.24f, 0.19f, 1f);
        private static readonly Color StrokeWas = new Color(0.72f, 0.54f, 0.17f, 1f);
        private static readonly Color StrokeAdd = new Color(0.94f, 0.82f, 0.50f, 1f);

        private readonly Dictionary<ClassId, Button> _seals = new Dictionary<ClassId, Button>();
        private readonly ClassGardenProgressionService _garden = new ClassGardenProgressionService();
        private MetaProgressionState _meta;
        private int _xp;
        private ClassId _chosen;
        private Func<ClassId, bool> _give;
        private Action _back;
        private Text _xpText, _sum, _line;
        private RectTransform _was, _add;
        private Button _giveButton;
        private bool _given;

        /// <summary>The panel at <paramref name="min"/>–<paramref name="max"/> on the screen and the give button below it.
        /// <paramref name="give"/> commits the XP (false: nothing was given); <paramref name="back"/> leaves afterwards.</summary>
        public static DailyWalkGiveView Build(Transform screen, Vector2 min, Vector2 max, Vector2 buttonMin, Vector2 buttonMax,
            int xp, string sum, MetaProgressionState meta, Func<ClassId, bool> give, Action back)
        {
            var panel = ScrollUiKit.Fill(screen, "GivePanel", min, max, new Color(0.08f, 0.06f, 0.04f, 0.78f), ScrollUiKit.Rounded(14f));
            var rim = panel.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(0.79f, 0.64f, 0.31f, 0.5f);
            rim.effectDistance = new Vector2(1.5f, -1.5f);
            var view = panel.gameObject.AddComponent<DailyWalkGiveView>();
            view._meta = meta ?? new MetaProgressionState();
            view._xp = xp;
            view._give = give;
            view._back = back;
            var t = panel.transform;
            ScrollUiKit.Label(t, "GiveHeader", Tr("DailyWalk.Give.Header").ToUpperInvariant(), 16, TextAnchor.MiddleLeft,
                new Color(0.80f, 0.74f, 0.61f, 1f), new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.95f), 11);
            view._xpText = ScrollUiKit.Label(t, "GiveXp", Fr("DailyWalk.Give.Xp", xp), 44, TextAnchor.MiddleLeft, ScrollUiKit.Gold,
                new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.88f), 24);
            view._sum = ScrollUiKit.Label(t, "GiveSum", sum, 17, TextAnchor.UpperLeft, new Color(0.80f, 0.74f, 0.61f, 1f),
                new Vector2(0.05f, 0.64f), new Vector2(0.95f, 0.75f), 11);
            view.BuildCord(t);
            var track = ScrollUiKit.Fill(t, "XpTrack", new Vector2(0.05f, 0.27f), new Vector2(0.95f, 0.335f),
                new Color(1f, 1f, 1f, 0.12f), ScrollUiKit.Rounded(14f));
            view._was = ScrollUiKit.Fill(track.transform, "Was", Vector2.zero, Vector2.zero, StrokeWas, ScrollUiKit.Rounded(14f)).rectTransform;
            view._add = ScrollUiKit.Fill(track.transform, "Add", Vector2.zero, Vector2.zero, StrokeAdd, ScrollUiKit.Rounded(14f)).rectTransform;
            view._line = ScrollUiKit.Label(t, "GiveLine", string.Empty, 26, TextAnchor.MiddleLeft, ScrollUiKit.PaperText,
                new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.25f), 14);
            view._giveButton = ScrollUiKit.PillButton(screen, "BtnGiveXp", buttonMin, buttonMax, string.Empty, 24, ScrollUiKit.Vermilion,
                Color.white, 30f);
            view._giveButton.onClick.AddListener(view.Give);
            view.Choose(view.DefaultClass());
            view.WireNavigation();
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events != null && view._seals.TryGetValue(view._chosen, out var first)) events.SetSelectedGameObject(first.gameObject);
            return view;
        }

        /// <summary>The class shown, for screenshots and tests.</summary>
        public ClassId Chosen => _chosen;

        // The eight seals in catalog order on a red cord; focus or the pointer chooses, a click gives.
        private void BuildCord(Transform t)
        {
            ScrollUiKit.Fill(t, "Cord", new Vector2(0.02f, CordY + 0.105f), new Vector2(0.98f, CordY + 0.112f), ScrollUiKit.Vermilion);
            var all = ClassCatalog.GetAll();
            for (var i = 0; i < all.Length; i++)
            {
                var id = all[i].Id;
                var unlocked = ClassUnlockService.GetProgress(_meta, id).Unlocked;
                var x = 0.5f + (i - (all.Length - 1) * 0.5f) * SealStep;
                var disc = ScrollUiKit.Spot(t, "Seal_" + id, new Vector2(x, CordY), new Vector2(SealSize, SealSize),
                    unlocked ? SealPaper : SealLocked, ScrollUiKit.Disc());
                disc.raycastTarget = true;
                var icon = ScrollUiKit.Icon(disc.transform, "Icon", "class/icon_" + ClassCatalog.GetIconName(id),
                    new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f), unlocked ? Color.white : new Color(0f, 0f, 0f, 0.45f));
                icon.raycastTarget = false;
                ScrollUiKit.Label(t, "SealLevel_" + id, unlocked ? Fr("DailyWalk.Give.Level", _garden.GetLevel(_meta, id)) : Tr("DailyWalk.Give.Locked"),
                    15, TextAnchor.UpperCenter, unlocked ? ScrollUiKit.Gold : new Color(0.62f, 0.56f, 0.48f, 1f),
                    new Vector2(x - SealStep * 0.5f, CordY - 0.15f), new Vector2(x + SealStep * 0.5f, CordY - 0.08f), 10);
                var button = disc.gameObject.AddComponent<Button>();
                button.targetGraphic = disc;
                var colors = ScrollUiKit.StateColors(Color.white);
                colors.disabledColor = Color.white;
                button.colors = colors;
                button.interactable = unlocked;
                button.onClick.AddListener(() => Choose(id));   // the button gives, so a stray click gives nothing
                disc.gameObject.AddComponent<FocusNotifier>().OnFocus = () => Choose(id);
                var hover = disc.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                entry.callback.AddListener(_ => { if (button.interactable) Choose(id); });
                hover.triggers.Add(entry);
                _seals[id] = button;
            }
        }

        // The first unlocked class with the lowest level: the walk's XP helps the class that needs it most.
        private ClassId DefaultClass()
        {
            var best = ClassId.NumberFreak;
            var bestLevel = int.MaxValue;
            foreach (var pair in _seals)
            {
                if (!pair.Value.interactable) continue;
                var level = _garden.GetLevel(_meta, pair.Key);
                if (level < bestLevel) { best = pair.Key; bestLevel = level; }
            }
            return best;
        }

        public void Choose(ClassId id)
        {
            if (_given || !_seals.TryGetValue(id, out var chosen) || !chosen.interactable) return;
            _chosen = id;
            foreach (var pair in _seals)
            {
                var rt = (RectTransform)pair.Value.transform;
                var lifted = pair.Key == id;
                rt.anchoredPosition = new Vector2(0f, lifted ? 14f : 0f);
                rt.localScale = Vector3.one * (lifted ? 1.12f : 1f);
                var ring = pair.Value.transform.Find("Ring");
                if (lifted && ring == null)
                {
                    var r = ScrollUiKit.Fill(pair.Value.transform, "Ring", Vector2.zero, Vector2.one, ScrollUiKit.Vermilion, ScrollUiKit.Ring());
                    r.rectTransform.offsetMin = new Vector2(-7f, -7f);
                    r.rectTransform.offsetMax = new Vector2(7f, 7f);
                }
                else if (!lifted && ring != null) ScrollUiKit.DestroyNow(ring.gameObject);
            }
            ShowStroke();
            SetButtonLabel(Fr("DailyWalk.Give.Button", _xp, ClassName(id)));
            _giveButton.interactable = _xp > 0;
        }

        // The XP stroke for the chosen class: its progress in the level now, and the walk's XP on top.
        private void ShowStroke()
        {
            var total = _garden.GetTotalXp(_meta, _chosen);
            var level = XpTable.DeriveLevel(total);
            var after = XpTable.DeriveLevel(total + _xp);
            var into = total - XpTable.CumulativeXpForLevel(level);
            var span = level < XpTable.MaxLevel ? XpTable.XpToNextLevel(level) : 1;
            var was = level < XpTable.MaxLevel ? Mathf.Clamp01(into / (float)span) : 1f;
            var add = after > level ? 1f : Mathf.Clamp01((into + _xp) / (float)span);
            SetFill(_was, 0f, was);
            SetFill(_add, was, Mathf.Max(was, add));
            _line.text = after > level
                ? Fr("DailyWalk.Give.LevelUp", ClassName(_chosen), level, after)
                : Fr("DailyWalk.Give.LevelSame", ClassName(_chosen), level);
        }

        private static void SetFill(RectTransform fill, float from, float to)
        {
            fill.anchorMin = new Vector2(from, 0f);
            fill.anchorMax = new Vector2(to, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
        }

        private void Give()
        {
            if (_given)
            {
                _back?.Invoke();
                return;
            }
            if (_xp <= 0 || _give == null || !_give(_chosen)) return;
            _given = true;
            ShowStroke();
            _sum.text = Fr("DailyWalk.Give.Given", _xp, ClassName(_chosen));
            _xpText.text = Fr("DailyWalk.Give.Xp", _xp);
            foreach (var seal in _seals.Values) seal.interactable = false;
            SetButtonLabel(Tr("InRun.End.Scroll.Back"));
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events != null) events.SetSelectedGameObject(_giveButton.gameObject);
            WireNavigation();
        }

        private void SetButtonLabel(string text)
        {
            var label = _giveButton.transform.Find("Label");
            if (label != null) label.GetComponent<Text>().text = text;
        }

        // Left and right along the seals, down to the give button; up from it to the chosen seal.
        private void WireNavigation()
        {
            var open = new List<Button>();
            foreach (var seal in _seals.Values)
                if (seal.interactable) open.Add(seal);
            for (var i = 0; i < open.Count; i++)
                open[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? open[i - 1] : null,
                    selectOnRight = i + 1 < open.Count ? open[i + 1] : null,
                    selectOnDown = _giveButton
                };
            _giveButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = !_given && _seals.TryGetValue(_chosen, out var chosen) ? chosen : null
            };
        }

        private static string ClassName(ClassId id)
        {
            var def = ClassCatalog.GetDefinition(id);
            return LocalizationService.T(def.Name, def.Name);
        }

        private static string Tr(string key) => LocalizationService.T(key, key);
        private static string Fr(string key, params object[] args) => LocalizationService.Format(key, key, args);
    }
}
