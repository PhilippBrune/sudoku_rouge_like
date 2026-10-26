using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Meta;
using SudokuRoguelike.Run;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// [REQ: UI-STYLE-024] Select Class · Who walks the garden (owner pick 2026-10-21): the classes as round seals over the
    /// class painting (locked ones dark with their unlock goal, META-UNLOCK-006; the Level 40 frame round a seal,
    /// META-XP-008), the chosen class on a hanging scroll with what the run starts with, and the run's settings on a strip
    /// (Harmony stones with the level's rule and the perk for a Garden Run, irregular puzzles, Back and Begin). Begin
    /// starts the run directly: the scroll shows what the confirmation window showed (owner 2026-10-21).
    /// The layout is built by MainMenuBlueprintBuilder.ClassSelect; MainMenuController owns the choice and the start.
    /// </summary>
    public sealed class ClassSelectScreen : MonoBehaviour
    {
        private static readonly Color SealPaper = new Color(0.94f, 0.89f, 0.78f, 1f);
        private static readonly Color SealLocked = new Color(0.29f, 0.24f, 0.19f, 1f);
        private static readonly Color Silhouette = new Color(0f, 0f, 0f, 0.5f);
        private static readonly Color GoalAmber = new Color(0.90f, 0.66f, 0.30f, 1f);
        private static readonly Color LevelGold = new Color(0.91f, 0.77f, 0.42f, 1f);
        private static readonly Color StoneReached = new Color(0.85f, 0.82f, 0.74f, 1f);
        private static readonly Color StoneLocked = new Color(0.45f, 0.42f, 0.37f, 0.45f);

        private readonly Dictionary<ClassId, Button> _seals = new Dictionary<ClassId, Button>();
        private readonly ClassGardenProgressionService _progression = new ClassGardenProgressionService();
        private RectTransform _scroll;
        private Text _eyebrow;
        private GameObject _harmonyArea;
        private Image[] _stones = Array.Empty<Image>();
        private Button _perk;
        private Text _perkName;
        private Text _modeInfo;
        private Text _beginLabel;
        private GameObject _abandonWarning;
        private ClassId _selected = ClassId.NumberFreak;

        /// <summary>LB/RB (PageUp/PageDown) step the Harmony level, Y (F) the perk. Set by MainMenuController.</summary>
        public Action<int> HarmonyStep;
        public Action PerkCycle;

        public void Configure(RectTransform scroll, Text eyebrow, GameObject harmonyArea, Image[] stones, Button perk, Text perkName,
            Text modeInfo, Text beginLabel, GameObject abandonWarning)
        {
            _scroll = scroll;
            _eyebrow = eyebrow;
            _harmonyArea = harmonyArea;
            _stones = stones ?? Array.Empty<Image>();
            _perk = perk;
            _perkName = perkName;
            _modeInfo = modeInfo;
            _beginLabel = beginLabel;
            _abandonWarning = abandonWarning;
        }

        public void RegisterSeal(ClassId id, Button seal) => _seals[id] = seal;

        private void Update()
        {
            if (_scroll == null || !_scroll.gameObject.activeInHierarchy) return;
            var (step, perk) = HarmonyHotkeys.Read();
            if (step != 0 && _harmonyArea != null && _harmonyArea.activeSelf) HarmonyStep?.Invoke(step);
            if (perk && _perk != null && _perk.interactable) PerkCycle?.Invoke();
        }

        // ── Seals ────────────────────────────────────────────────────────────

        /// <summary>Each seal: level and prestige stars and the Level 40 frame, or dark with the unlock goal.</summary>
        public void RefreshSeals(MetaProgressionState meta, Func<ClassId, bool> isUnlocked)
        {
            foreach (var pair in _seals)
            {
                var id = pair.Key;
                var seal = pair.Value;
                if (seal == null) continue;
                var unlocked = isUnlocked(id);
                var disc = seal.GetComponent<Image>();
                if (disc != null) disc.color = unlocked ? SealPaper : SealLocked;
                SetImageColor(seal.transform, "Icon", unlocked ? Color.white : Silhouette);
                var frame = seal.transform.Find("Frame");
                if (frame != null)
                {
                    var framed = unlocked && ClassFrames.IsUnlocked(meta, id);
                    frame.gameObject.SetActive(framed);
                    if (framed) frame.GetComponent<Image>().color = ClassFrames.Colour(id);
                }
                SetText(seal.transform, "Level", unlocked
                    ? LocalizationService.Format("MetaProgression.Plaque.Level", "Lv {0}", _progression.GetLevel(meta, id))
                    : ClassUnlockText.Goal(id, ClassUnlockService.GetProgress(meta, id)), unlocked ? LevelGold : GoalAmber);
                var tier = unlocked ? _progression.GetPrestigeTier(meta, id) : 0;
                SetText(seal.transform, "Stars", tier > 0 ? new string('★', tier) : string.Empty, ScrollUiKit.Vermilion);
            }
            MarkSelected();
        }

        private void MarkSelected()
        {
            foreach (var pair in _seals)
            {
                if (pair.Value == null) continue;
                var chosen = pair.Key == _selected;
                SetImageColor(pair.Value.transform, "Ring", chosen ? ScrollUiKit.Vermilion
                    : InkStampShop.SealRingColour(new Color(0.16f, 0.14f, 0.11f, 0.85f)));   // [REQ: DAILY-WALK-007]
                var glow = pair.Value.transform.parent != null ? pair.Value.transform.parent.Find("SealGlow_" + pair.Key) : null;
                if (glow != null) glow.GetComponent<Image>().color = new Color(1f, 0.82f, 0.48f, chosen ? 0.6f : 0f);
            }
        }

        // ── The chosen class on the scroll ───────────────────────────────────

        public void ShowClass(ClassId id, MetaProgressionState meta, bool unlocked)
        {
            _selected = id;
            MarkSelected();
            if (_scroll == null) return;
            ScrollUiKit.DestroyChildrenNow(_scroll, "ClassPage");
            var page = ScrollUiKit.Area(_scroll, "ClassPage", new Vector2(0.07f, 0.20f), new Vector2(0.93f, 0.955f));
            var def = ClassCatalog.GetDefinition(id);
            var level = unlocked ? _progression.GetLevel(meta, id) : 1;
            var tier = unlocked ? _progression.GetPrestigeTier(meta, id) : 0;

            ScrollUiKit.Label(page, "ChosenClassName", LocalizationService.T(def.Name, def.Name), 44, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0f, 0.87f), new Vector2(1f, 1f), 24);
            ScrollUiKit.Label(page, "ChosenClassSub", unlocked ? SubLine(meta, id, level, tier)
                    : LocalizationService.T("MetaProgression.Locked", "Locked") + " · " + ClassUnlockText.Goal(id, ClassUnlockService.GetProgress(meta, id)),
                19, TextAnchor.MiddleLeft, unlocked ? ScrollUiKit.InkSoft : ScrollUiKit.Vermilion, new Vector2(0f, 0.80f), new Vector2(1f, 0.87f), 12);
            BuildStats(page, def, id, level, tier);

            var slip = ScrollUiKit.Fill(page, "ClassPassive", new Vector2(0f, 0.36f), new Vector2(1f, 0.52f), new Color(0.94f, 0.89f, 0.78f, 1f));
            ScrollUiKit.Label(slip.transform, "Caption", LocalizationService.T("MetaProgression.Passive", "PASSIVE"), 14,
                TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.03f, 0.70f), new Vector2(0.97f, 0.95f));
            ScrollUiKit.Label(slip.transform, "Text", LocalizationService.T($"Class.Passive.{def.Id}", def.PassiveDescription), 19,
                TextAnchor.UpperLeft, ScrollUiKit.Ink, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.70f), 12);

            BuildNext(page, id, level);
        }

        private string SubLine(MetaProgressionState meta, ClassId id, int level, int tier)
        {
            string line;
            if (level >= XpTable.MaxLevel)
                line = LocalizationService.Format("MetaProgression.MaxLevel", "Level {0} reached", XpTable.MaxLevel);
            else
            {
                var into = Mathf.Max(0, _progression.GetTotalXp(meta, id) - XpTable.CumulativeXpForLevel(level));
                line = LocalizationService.Format("ClassSelect.Sub", "Level {0} of {1} · {2:N0} / {3:N0} XP", level, XpTable.MaxLevel,
                    into, Mathf.Max(1, XpTable.XpToNextLevel(level)));
            }
            if (tier > 0)
                line += " · " + LocalizationService.Format("ClassSelect.Prestige", "{0} prestige", new string('★', tier));
            return line;
        }

        // What the run starts with: HP, Pencil, bag, relics and gold, each "base (+bonus)" (REQ-CLASS-103).
        private static void BuildStats(RectTransform page, ClassDefinition def, ClassId id, int level, int tier)
        {
            var bonus = RunArchetypeService.GetLevelBonuses(id, level);
            var relics = RunArchetypeService.GetStartingRelicCount(id, level);
            var gold = bonus.Gold + ClassGardenProgressionService.PrestigeStartingGold(tier);
            var stats = new (string Icon, string Caption, int Value, int Bonus)[]
            {
                ("ui/icon_flame_stone", LocalizationService.T("HP", "HP"), def.BaseHP + bonus.Hp, bonus.Hp),
                ("ui/icon_flow_ribbon", LocalizationService.T("Pencil", "Pencil"), def.BasePencil + bonus.Pencil, bonus.Pencil),
                ("items/icon_solver", LocalizationService.T("ClassSelect.Bag", "Bag"), def.BaseItemSlots + bonus.Slots, bonus.Slots),
                ("ui/icon_petal_orb", LocalizationService.T("Relics", "Relics"), relics, relics - def.BaseStartingRelics),
                ("economy/icon_sakura_coin", LocalizationService.T("Gold", "Gold"), gold, gold)
            };
            const float gap = 0.022f;
            var width = (1f - gap * (stats.Length - 1)) / stats.Length;
            for (var i = 0; i < stats.Length; i++)
            {
                var x = i * (width + gap);
                var box = ScrollUiKit.Fill(page, $"Stat_{i}", new Vector2(x, 0.56f), new Vector2(x + width, 0.78f),
                    new Color(0.94f, 0.89f, 0.78f, 1f));
                ScrollUiKit.Icon(box.transform, "Icon", stats[i].Icon, new Vector2(0.34f, 0.66f), new Vector2(0.66f, 0.94f), Color.white);
                ScrollUiKit.Label(box.transform, "Caption", stats[i].Caption, 16, TextAnchor.MiddleCenter, ScrollUiKit.InkSoft,
                    new Vector2(0.02f, 0.44f), new Vector2(0.98f, 0.66f), 11);
                var value = stats[i].Bonus > 0
                    ? $"{stats[i].Value} <size=17><color=#9E2A2B>(+{stats[i].Bonus})</color></size>"
                    : stats[i].Value.ToString();
                var label = ScrollUiKit.Label(box.transform, "Value", value, 30, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                    new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.46f), 14);
                label.supportRichText = true;
            }
        }

        // The next two rewards of the level path, and the Level 30 item once it is the class's (META-XP-007).
        private static void BuildNext(RectTransform page, ClassId id, int level)
        {
            ScrollUiKit.Label(page, "NextCaption", LocalizationService.T("ClassSelect.Next", "NEXT"), 14, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(0f, 0.27f), new Vector2(1f, 0.33f));
            var lines = new List<string>();
            foreach (var (at, unlock) in ClassCatalog.GetAllUnlocks(id))
            {
                if (at <= level) continue;
                lines.Add(LocalizationService.Format("ClassSelect.NextLine", "Lv {0} · <color=#9E2A2B>{1}</color>", at,
                    ClassUnlockText.Localize(unlock)));
                if (lines.Count == 2) break;
            }
            if (level >= ClassCatalog.StartingItemLevel)
            {
                var (type, rarity) = ClassCatalog.GetStartingItem(id);
                var name = (rarity == ItemRarity.Rare ? LocalizationService.T("Item.Rarity.Rare", "Rare") + " " : string.Empty)
                    + SudokuRoguelike.Items.ItemService.GetItemName(type);
                lines.Add(LocalizationService.Format("ClassSelect.StartsWith", "Starts with: <color=#9E2A2B>{0}</color>", name));
            }
            if (lines.Count == 0) lines.Add(LocalizationService.T("ClassSelect.AllReached", "Every level reward reached."));
            var text = ScrollUiKit.Label(page, "NextText", string.Join("\n", lines), 20, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0f, 0f), new Vector2(1f, 0.27f), 12);
            text.supportRichText = true;
            text.lineSpacing = 1.2f;
        }

        // ── The run's settings ───────────────────────────────────────────────

        /// <summary>The strip for the mode: Harmony stones and perk for a Garden Run, a line on the mode otherwise; Begin, or
        /// "Abandon & Begin" with the warning when a saved run would end (REQ-RUN-101).</summary>
        public void ShowRunSettings(GameMode mode, SpiritTrialsTier tier, int level, int maxUnlocked, HarmonyPerkId perk,
            string perkName, bool abandonsSavedRun)
        {
            if (_eyebrow != null)
                _eyebrow.text = (mode switch
                {
                    GameMode.EndlessZen => LocalizationService.T("ClassSelect.Eyebrow.EndlessZen", "Endless Zen"),
                    GameMode.SpiritTrials => LocalizationService.T("ClassSelect.Eyebrow.SpiritTrials", "Spirit Trials"),
                    GameMode.IncenseWalk => LocalizationService.T("ClassSelect.Eyebrow.IncenseWalk", "Incense Walk"),
                    _ => LocalizationService.T("ClassSelect.Eyebrow.GardenRun", "Garden Run")
                }).ToUpperInvariant();
            var garden = mode == GameMode.GardenRun;
            if (_harmonyArea != null) _harmonyArea.SetActive(garden);
            if (_modeInfo != null)
            {
                _modeInfo.gameObject.SetActive(!garden);
                _modeInfo.text = mode == GameMode.SpiritTrials
                    ? LocalizationService.Format("ClassSelect.Mode.SpiritTrials", "Spirit Trials · {0}: one timed puzzle under rising pressure.",
                        LocalizationService.T($"SpiritTrials.Tier.{tier}", tier.ToString()))
                    : mode == GameMode.IncenseWalk
                        ? LocalizationService.T("ClassSelect.Mode.IncenseWalk")
                        : LocalizationService.T("ClassSelect.Mode.EndlessZen", "Endless Zen: one 9×9 after another until your HP runs out.");
            }
            for (var i = 0; i < _stones.Length; i++)
                if (_stones[i] != null)
                    _stones[i].color = i == level ? ScrollUiKit.Vermilion : i <= maxUnlocked ? StoneReached : StoneLocked;
            if (_perk != null)
            {
                _perk.interactable = level >= 4;
                if (_perkName != null)
                    _perkName.text = level >= 4 ? perkName : LocalizationService.T("ClassSelect.PerkLocked", "from Harmony IV");
            }
            if (_beginLabel != null)
                _beginLabel.text = abandonsSavedRun
                    ? LocalizationService.T("RunConfirm.AbandonAndBegin", "Abandon & Begin")
                    : LocalizationService.T("ClassSelect.Begin", "Begin the run");
            if (_abandonWarning != null) _abandonWarning.SetActive(abandonsSavedRun);
        }

        private static void SetText(Transform parent, string child, string text, Color color)
        {
            var found = parent.Find(child);
            var label = found != null ? found.GetComponent<Text>() : null;
            if (label == null) return;
            label.text = text;
            label.color = color;
        }

        private static void SetImageColor(Transform parent, string child, Color color)
        {
            var found = parent.Find(child);
            var image = found != null ? found.GetComponent<Image>() : null;
            if (image != null) image.color = color;
        }
    }
}
