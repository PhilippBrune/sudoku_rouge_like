using System;
using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Owns the boss gate modifier-choice panel and the cursed-node choice panel.
    /// </summary>
    public sealed class BossGateViewController : MonoBehaviour
    {
        private RunMapController _map;

        private GameObject _bossGatePanel;
        private bool _awaitingBossGate;
        private List<BossModifierId> _bossGateOptions;
        private List<BossModifierId> _selectedBossMods;
        private int _bossPicksRequired;
        private Text _bossGateTitle;

        public Action<List<BossModifierId>> OnModsConfirmed;

        public bool IsAwaiting => _awaitingBossGate;

        public void Configure(RunMapController map)
        {
            _map = map;
            _bossGateOptions = new List<BossModifierId>();
            _selectedBossMods = new List<BossModifierId>();
        }

        public void ShowBossGateChoice()
        {
            if (_awaitingBossGate) return;
            var run = _map?.Run;
            if (run == null) return;

            _bossGateOptions.Clear();
            _selectedBossMods.Clear();
            var choices = run.RollBossModifierChoices();
            if (choices != null) _bossGateOptions.AddRange(choices);
            if (_bossGateOptions.Count == 0) return;

            run.GetBossModifierCounts(out _, out _bossPicksRequired);
            _awaitingBossGate = true;
            BuildBossGatePanel();
        }

        public void ShowCursedNodePanel(GameObject pathPanel, RunNode node,
            Action<LevelConfig> onAccept, Action<LevelConfig> onDecline)
        {
            var run = _map?.Run;
            if (run == null) return;

            // Build a preview of what the cursed level would look like
            var cursedConfig = run.BuildCursedLevelConfig(node.Index);
            var extraMod = cursedConfig.ActiveModifiers.Count > 0
                ? InRunUiFactory.FormatModName(cursedConfig.ActiveModifiers[cursedConfig.ActiveModifiers.Count - 1])
                : "?";

            if (pathPanel == null) return;
            var panel = new GameObject("CursedChoicePanel", typeof(RectTransform));
            panel.transform.SetParent(pathPanel.transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.25f);
            rt.anchorMax = new Vector2(0.85f, 0.75f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.18f, 0.06f, 0.06f, 0.96f);

            InRunUiFactory.CreateText(panel.transform, "Title", "Cursed Tile!", 18, TextAnchor.UpperCenter,
                new Color(0.90f, 0.30f, 0.20f, 1f)).rectTransform.SetRect(0.05f, 0.80f, 0.95f, 0.97f);

            InRunUiFactory.CreateText(panel.transform, "Desc",
                $"Extra modifier: {extraMod}\n+50% Gold & XP if you clear it.\nFail to complete? Normal penalties apply.",
                12, TextAnchor.MiddleCenter, new Color(0.95f, 0.85f, 0.75f, 1f))
                .rectTransform.SetRect(0.05f, 0.45f, 0.95f, 0.78f);

            var btnAccept = InRunUiFactory.CreatePanelButton(panel.transform, "BtnAccept",
                new Vector2(0.05f, 0.20f), new Vector2(0.45f, 0.40f), "Accept Curse");
            btnAccept.onClick.AddListener(() =>
            {
                Object.Destroy(panel);
                onAccept?.Invoke(cursedConfig);
            });

            var btnDecline = InRunUiFactory.CreatePanelButton(panel.transform, "BtnDecline",
                new Vector2(0.55f, 0.20f), new Vector2(0.95f, 0.40f), "Decline");
            btnDecline.onClick.AddListener(() =>
            {
                var normalConfig = run.BuildLevelConfig(false, false, node.Index);
                Object.Destroy(panel);
                onDecline?.Invoke(normalConfig);
            });
        }

        private void BuildBossGatePanel()
        {
            if (_bossGatePanel != null) Object.Destroy(_bossGatePanel);
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            _bossGatePanel = new GameObject("BossGatePanel", typeof(RectTransform), typeof(Image));
            _bossGatePanel.transform.SetParent(canvas.transform, false);
            var pr = _bossGatePanel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.15f, 0.10f);
            pr.anchorMax = new Vector2(0.85f, 0.90f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;
            _bossGatePanel.GetComponent<Image>().color = InRunUiFactory.PanelBg;

            _bossGateTitle = InRunUiFactory.CreateText(_bossGatePanel.transform, "Title",
                $"Boss Gate \u2014 Pick {_bossPicksRequired} Modifiers (0/{_bossPicksRequired})",
                18, TextAnchor.MiddleCenter, InRunUiFactory.AccentGold);
            _bossGateTitle.rectTransform.anchorMin = new Vector2(0.05f, 0.90f);
            _bossGateTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.98f);
            _bossGateTitle.rectTransform.offsetMin = Vector2.zero;
            _bossGateTitle.rectTransform.offsetMax = Vector2.zero;

            var run2 = _map?.Run;
            var seenMods = run2?.State?.SeenBossModifiers;
            for (var i = 0; i < _bossGateOptions.Count; i++)
            {
                var mod = _bossGateOptions[i];
                var seen = seenMods != null && seenMods.Contains(mod);
                var col = i % 3; var row = i / 3;
                var xMin = 0.05f + col * 0.31f;
                var yMax = 0.85f - row * 0.25f;
                var labelText = seen ? $"{InRunUiFactory.FormatModName(mod)}\n{InRunUiFactory.GetModDesc(mod)}" : "???";
                var btn = InRunUiFactory.CreatePanelButton(_bossGatePanel.transform, $"Mod_{i}",
                    new Vector2(xMin, yMax - 0.22f), new Vector2(xMin + 0.28f, yMax),
                    labelText);
                InRunUiFactory.SetButtonIcon(btn, seen ? BossService.GetIconName(mod) : null, !seen);
                var captured = mod;
                btn.onClick.AddListener(() => ToggleBossModSelection(captured, btn));
            }

            // Confirm button (disabled until enough picks)
            var confirmBtn = InRunUiFactory.CreatePanelButton(_bossGatePanel.transform, "ConfirmBoss",
                new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.10f),
                "Confirm");
            confirmBtn.interactable = false;
            confirmBtn.onClick.AddListener(ConfirmBossGateAll);
            confirmBtn.gameObject.name = "ConfirmBossBtn";
        }

        private void ToggleBossModSelection(BossModifierId mod, Button btn)
        {
            if (_selectedBossMods.Contains(mod))
            {
                _selectedBossMods.Remove(mod);
                var colors = btn.colors;
                colors.normalColor = InRunUiFactory.BtnColor;
                btn.colors = colors;
            }
            else if (_selectedBossMods.Count < _bossPicksRequired)
            {
                _selectedBossMods.Add(mod);
                var colors = btn.colors;
                colors.normalColor = new Color(InRunUiFactory.AccentGold.r, InRunUiFactory.AccentGold.g, InRunUiFactory.AccentGold.b, 0.5f);
                btn.colors = colors;
            }

            // Update title
            if (_bossGateTitle != null)
                _bossGateTitle.text = $"Boss Gate \u2014 Pick {_bossPicksRequired} Modifiers ({_selectedBossMods.Count}/{_bossPicksRequired})";

            // Enable/disable confirm
            var confirmGo = _bossGatePanel?.transform.Find("ConfirmBossBtn");
            if (confirmGo != null)
            {
                var confirmBtn = confirmGo.GetComponent<Button>();
                if (confirmBtn != null)
                    confirmBtn.interactable = _selectedBossMods.Count >= _bossPicksRequired;
            }
        }

        private void ConfirmBossGateAll()
        {
            _awaitingBossGate = false;
            var run = _map?.Run;
            if (run != null) run.ChooseBossModifiers(new List<BossModifierId>(_selectedBossMods));
            if (_bossGatePanel != null) { Object.Destroy(_bossGatePanel); _bossGatePanel = null; }

            OnModsConfirmed?.Invoke(new List<BossModifierId>(_selectedBossMods));
        }
    }
}
