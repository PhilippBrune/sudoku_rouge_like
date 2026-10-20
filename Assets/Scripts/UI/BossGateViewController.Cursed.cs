using System;
using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-015] The cursed tile as a sealed talisman in the curse painting (layout-proposals.md, "Cursed tile"):
    // the extra modifier with its icon and rule and the puzzle's size and stars on the talisman, the reward and the risk
    // on two paper tags, the curses already on the player on a third. Accept the curse, or decline and play the same
    // tile as a normal puzzle.
    // [REQ: MAP-NODE-CURSED-001] [REQ: MAP-NODE-CURSED-002] [REQ: MAP-NODE-CURSED-003]
    public sealed partial class BossGateViewController
    {
        private GameObject _cursedPanel;

        // The talisman and the tags (fractions of the canvas, measured on the 1920×1080 mockup).
        private static readonly Vector2 TalismanMin = new Vector2(0.3958f, 0.1852f), TalismanMax = new Vector2(0.6042f, 0.8889f);

        /// <summary>Removes the cursed-tile scene without a choice (a run that ends while it is open, or a test).</summary>
        public void CloseCursedNodePanel()
        {
            if (_cursedPanel == null) return;
            if (ActivePanel == _cursedPanel) ActivePanel = null;
            ScrollUiKit.DestroyNow(_cursedPanel);
            _cursedPanel = null;
        }

        public void ShowCursedNodePanel(GameObject pathPanel, RunNode node,
            Action<LevelConfig> onAccept, Action<LevelConfig> onDecline)
        {
            var run = _map?.Run;
            if (run?.State == null || pathPanel == null) return;
            CloseCursedNodePanel();

            var cursedConfig = run.BuildCursedLevelConfig(node.Index);
            var mods = cursedConfig.ActiveModifiers;
            BossModifierId? extra = mods.Count > 0 ? mods[mods.Count - 1] : (BossModifierId?)null;

            var screen = ScrollUiKit.Scene(pathPanel, "CursedChoicePanel", "bg_curse", new Color(0.62f, 0.62f, 0.58f, 1f));
            _cursedPanel = screen.gameObject;
            BuildTalisman(screen.transform, run.State, cursedConfig, extra);
            BuildCursedGainTag(screen.transform, cursedConfig);
            BuildCursedRiskTag(screen.transform);
            BuildCursedCurseTag(screen.transform, run);

            var accept = ScrollUiKit.PillButton(screen.transform, "BtnAccept", new Vector2(0.3516f, 0.0852f), new Vector2(0.4729f, 0.137f),
                T("InRun.Cursed.AcceptCurse"), 24, ScrollUiKit.Vermilion, Color.white, 28f);
            // "Skip" in the name lets B (gamepad cancel) press it, like the other scenes' skip buttons.
            var decline = ScrollUiKit.RimButton(screen.transform, "BtnDeclineSkip", new Vector2(0.4885f, 0.0852f), new Vector2(0.6484f, 0.137f),
                T("InRun.Cursed.DeclineNormal"), 24, ScrollUiKit.Gold, new Color(0.45f, 0.33f, 0.14f, 0.95f), ScrollUiKit.PaperText);
            var hint = ScrollUiKit.Label(screen.transform, "CursedHint", T("InRun.Cursed.Hint"), 19, TextAnchor.MiddleCenter,
                new Color(0.86f, 0.82f, 0.70f, 1f), new Vector2(0.30f, 0.042f), new Vector2(0.70f, 0.072f), 12);
            Shade(hint);

            accept.onClick.AddListener(() =>
            {
                CloseCursedNodePanel();
                onAccept?.Invoke(cursedConfig);
            });
            decline.onClick.AddListener(() =>
            {
                // [REQ: MAP-NODE-CURSED-003] Decline: standard puzzle built with no cursed multipliers or extra modifier
                var normalConfig = run.BuildNodeLevelConfig(node, node.Index);
                CloseCursedNodePanel();
                onDecline?.Invoke(normalConfig);
            });
            accept.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = decline };
            decline.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = accept };

            ActivePanel = _cursedPanel;
            FadeInPanel(_cursedPanel);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(accept.gameObject);
        }

        // The talisman: a tall paper slip pinned at the top, with a vermilion border, the extra rule and a seal.
        private static void BuildTalisman(Transform screen, RunState state, LevelConfig config, BossModifierId? extra)
        {
            var paper = ScrollUiKit.Fill(screen, "CursedTalisman", TalismanMin, TalismanMax, new Color(0.95f, 0.90f, 0.78f, 1f));
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -12f);
            var t = paper.transform;
            ScrollUiKit.Fill(t, "PaperLight", new Vector2(0f, 0.4f), Vector2.one, new Color(1f, 1f, 1f, 0.22f), ScrollUiKit.Glow());
            var border = ScrollUiKit.Area(t, "Border", Vector2.zero, Vector2.one);
            border.offsetMin = new Vector2(20f, 20f);
            border.offsetMax = new Vector2(-20f, -20f);
            ScrollUiKit.InkFrame(border, new Color(0.75f, 0.22f, 0.17f, 1f), 3f);
            ScrollUiKit.Spot(t, "Pin", new Vector2(0.5f, 1f), new Vector2(22f, 22f), new Color(0.24f, 0.15f, 0.08f, 1f), ScrollUiKit.Disc());

            ScrollUiKit.Label(t, "Eyebrow", F("InRun.Cursed.Eyebrow", state.CurrentFloor + 1).ToUpperInvariant(), 17, TextAnchor.MiddleCenter,
                ScrollUiKit.Vermilion, new Vector2(0.06f, 0.895f), new Vector2(0.94f, 0.945f), 11);
            ScrollUiKit.Label(t, "Title", T("InRun.Cursed.SceneTitle"), 54, TextAnchor.MiddleCenter, new Color(0.62f, 0.16f, 0.12f, 1f),
                new Vector2(0.06f, 0.80f), new Vector2(0.94f, 0.905f), 28);

            ScrollUiKit.Spot(t, "IconRing", new Vector2(0.5f, 0.697f), new Vector2(154f, 154f), new Color(0.80f, 0.42f, 0.36f, 1f), ScrollUiKit.Disc());
            ScrollUiKit.Spot(t, "IconDisc", new Vector2(0.5f, 0.697f), new Vector2(148f, 148f), new Color(0.92f, 0.78f, 0.70f, 1f), ScrollUiKit.Disc());
            var name = extra.HasValue ? BossService.GetModifierName(extra.Value) : "?";
            var rule = extra.HasValue ? BossService.GetModifierDescription(extra.Value) : string.Empty;
            if (extra.HasValue)
            {
                var iconName = BossService.GetIconName(extra.Value);
                if (string.IsNullOrEmpty(iconName))
                    Debug.LogWarning($"[BossGateViewController] No icon mapped for the cursed modifier: {extra.Value}");
                else
                    ScrollUiKit.Spot(t, "ModifierIcon", new Vector2(0.5f, 0.697f), new Vector2(112f, 112f), Color.white,
                        Resources.Load<Sprite>(BossService.GetIconFolder(extra.Value) + "/icon_" + iconName));
            }
            ScrollUiKit.Label(t, "ModifierName", name, 36, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                new Vector2(0.06f, 0.515f), new Vector2(0.94f, 0.575f), 20);
            ScrollUiKit.Label(t, "ModifierRule", rule, 21, TextAnchor.UpperCenter, new Color(0.23f, 0.20f, 0.16f, 1f),
                new Vector2(0.09f, 0.405f), new Vector2(0.91f, 0.508f), 13);
            ScrollUiKit.Label(t, "Puzzle", F("InRun.Cursed.Puzzle", config.BoardSize, new string('★', Mathf.Clamp(config.Stars, 1, 5))), 19,
                TextAnchor.MiddleCenter, ScrollUiKit.InkSoft, new Vector2(0.06f, 0.35f), new Vector2(0.94f, 0.40f), 12);

            var seal = ScrollUiKit.Fill(t, "Seal", new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.22f), new Color(0.72f, 0.22f, 0.16f, 1f),
                ScrollUiKit.Rounded(8f));
            seal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            var sealShadow = seal.gameObject.AddComponent<Shadow>();
            sealShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            sealShadow.effectDistance = new Vector2(2f, -4f);
            ScrollUiKit.Fill(seal.transform, "Mark", new Vector2(0.24f, 0.24f), new Vector2(0.76f, 0.76f), new Color(0.98f, 0.92f, 0.86f, 1f),
                ScrollUiKit.Enso());
        }

        private static void BuildCursedGainTag(Transform screen, LevelConfig config)
        {
            var tag = ScrollUiKit.PaperTag(screen, "CursedGain", new Vector2(0.1495f, 0.5167f), new Vector2(0.3292f, 0.7269f), 1.5f, 20f);
            var t = tag.transform;
            var green = new Color(0.20f, 0.42f, 0.22f, 1f);
            ScrollUiKit.Label(t, "Header", T("InRun.Cursed.IfCleared").ToUpperInvariant(), 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.07f, 0.82f), new Vector2(0.93f, 0.95f), 11);
            var goldBonus = Mathf.RoundToInt((config.CursedGoldMult - 1f) * 100f);
            var xpBonus = Mathf.RoundToInt((config.CursedXpMult - 1f) * 100f);
            if (goldBonus > 0 || xpBonus > 0)
            {
                ScrollUiKit.Label(t, "Gold", F("InRun.Cursed.GoldBonus", goldBonus), 34, TextAnchor.MiddleLeft, green,
                    new Vector2(0.07f, 0.60f), new Vector2(0.93f, 0.80f), 18);
                ScrollUiKit.Label(t, "Xp", F("InRun.Cursed.XpBonus", xpBonus), 34, TextAnchor.MiddleLeft, green,
                    new Vector2(0.07f, 0.40f), new Vector2(0.93f, 0.60f), 18);
            }
            else
            {
                // [REQ: HARMONY-FLAG-005] H9+: no bonus gold or XP.
                ScrollUiKit.Label(t, "NoBonus", T("InRun.Cursed.NoBonus"), 24, TextAnchor.MiddleLeft, ScrollUiKit.Vermilion,
                    new Vector2(0.07f, 0.40f), new Vector2(0.93f, 0.80f), 14);
            }
            ScrollUiKit.Label(t, "Score", T("InRun.Cursed.RunScore"), 20, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.07f, 0.06f), new Vector2(0.93f, 0.38f), 12);
        }

        private static void BuildCursedRiskTag(Transform screen)
        {
            var tag = ScrollUiKit.PaperTag(screen, "CursedRisk", new Vector2(0.6708f, 0.5324f), new Vector2(0.85f, 0.7269f), -1.5f, 20f);
            var t = tag.transform;
            ScrollUiKit.Label(t, "Header", T("InRun.Cursed.IfFailed").ToUpperInvariant(), 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.07f, 0.80f), new Vector2(0.93f, 0.94f), 11);
            ScrollUiKit.Label(t, "Penalty", T("InRun.Cursed.NormalPenalties"), 28, TextAnchor.MiddleLeft, ScrollUiKit.Vermilion,
                new Vector2(0.07f, 0.60f), new Vector2(0.93f, 0.80f), 16);
            ScrollUiKit.Label(t, "Text", T("InRun.Cursed.RiskText"), 20, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.07f, 0.06f), new Vector2(0.93f, 0.58f), 12);
        }

        private static void BuildCursedCurseTag(Transform screen, RunDirector run)
        {
            var curses = run.GetActiveCurses();
            if (curses.Count == 0) return;
            var tag = ScrollUiKit.PaperTag(screen, "CursedCurses", new Vector2(0.0573f, 0.3148f), new Vector2(0.2292f, 0.4093f), -0.5f, 20f);
            var t = tag.transform;
            ScrollUiKit.Label(t, "Header", T("InRun.Cursed.AlreadyOnYou").ToUpperInvariant(), 16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                new Vector2(0.07f, 0.62f), new Vector2(0.95f, 0.90f), 11);
            var shown = Mathf.Min(curses.Count, 2);
            for (var i = 0; i < shown; i++)
            {
                var x = 0.07f + i * 0.45f;
                ScrollUiKit.Icon(t, $"CurseIcon_{i}", "cursed/icon_" + CurseService.GetIconName(curses[i].Id),
                    new Vector2(x, 0.12f), new Vector2(x + 0.12f, 0.58f), Color.white);
                ScrollUiKit.Label(t, $"CurseName_{i}", CurseService.GetLocalizedName(curses[i]), 19, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                    new Vector2(x + 0.13f, 0.12f), new Vector2(x + 0.44f, 0.58f), 11);
            }
        }

        private static void Shade(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(1f, -2f);
        }
    }
}
