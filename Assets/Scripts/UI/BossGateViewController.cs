using System;
using System.Collections.Generic;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;
using SudokuRoguelike.Economy;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

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
        public GameObject ActivePanel { get; private set; }

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
            if (choices != null)
            {
                for (var i = 0; i < choices.Count; i++)
                    if (!_bossGateOptions.Contains(choices[i]))
                        _bossGateOptions.Add(choices[i]);
            }
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
                ? BossService.GetModifierName(cursedConfig.ActiveModifiers[cursedConfig.ActiveModifiers.Count - 1])
                : "?";

            if (pathPanel == null) return;
            var panel = new GameObject("CursedChoicePanel", typeof(RectTransform));
            panel.transform.SetParent(pathPanel.transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.25f);
            rt.anchorMax = new Vector2(0.85f, 0.75f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var cursedPanelBase = InRunUiFactory.CursedPanelBg;
            panel.AddComponent<Image>().color = new Color(cursedPanelBase.r, cursedPanelBase.g, cursedPanelBase.b, 0.80f);
            // F23: bg_curse is a full-screen dark overlay behind this panel, not a board replacement.
            // It visually signals the cursed state while the board remains playable beneath.
            InRunUiFactory.AddPanelBackground(panel.transform, "bg_curse");

            var titleTxt = InRunUiFactory.CreateText(panel.transform, "Title", T("InRun.Cursed.Title"), 18, TextAnchor.UpperCenter,
                InRunUiFactory.CursedTitleRed);
            titleTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.80f);
            titleTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.97f);
            titleTxt.rectTransform.offsetMin = Vector2.zero;
            titleTxt.rectTransform.offsetMax = Vector2.zero;

            var descTxt = InRunUiFactory.CreateText(panel.transform, "Desc",
                F("InRun.Cursed.Description", extraMod),
                12, TextAnchor.MiddleCenter, InRunUiFactory.WarmIvory);
            descTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.45f);
            descTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.78f);
            descTxt.rectTransform.offsetMin = Vector2.zero;
            descTxt.rectTransform.offsetMax = Vector2.zero;

            var btnAccept = InRunUiFactory.CreateActionButton(panel.transform, "BtnAccept",
                new Vector2(0.05f, 0.20f), new Vector2(0.45f, 0.40f), T("InRun.Cursed.Accept"), UiAction.Accept);
            btnAccept.onClick.AddListener(() =>
            {
                ActivePanel = null;
                Object.Destroy(panel);
                onAccept?.Invoke(cursedConfig);
            });

            var btnDecline = InRunUiFactory.CreateActionButton(panel.transform, "BtnDecline",
                new Vector2(0.55f, 0.20f), new Vector2(0.95f, 0.40f), T("Decline"), UiAction.Decline);
            btnDecline.onClick.AddListener(() =>
            {
                // [REQ: MAP-NODE-CURSED-003] Decline: standard puzzle built with no cursed multipliers or extra modifier
                var normalConfig = run.BuildLevelConfig(false, false, node.Index);
                ActivePanel = null;
                Object.Destroy(panel);
                onDecline?.Invoke(normalConfig);
            });

            // Explicit horizontal D-pad nav between the two choice buttons
            var aNav = btnAccept.navigation;
            aNav.mode = Navigation.Mode.Explicit; aNav.selectOnRight = btnDecline;
            btnAccept.navigation = aNav;
            var dNav = btnDecline.navigation;
            dNav.mode = Navigation.Mode.Explicit; dNav.selectOnLeft = btnAccept;
            btnDecline.navigation = dNav;

            ActivePanel = panel;
            FadeInPanel(panel);
            InRunUiFactory.SelectFirstInteractable(panel);
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
            var bossGatePanelBg = InRunUiFactory.PanelBg;
            _bossGatePanel.GetComponent<Image>().color = new Color(bossGatePanelBg.r, bossGatePanelBg.g, bossGatePanelBg.b, 0.72f);
            InRunUiFactory.AddPanelBackground(_bossGatePanel.transform, "bg_boss_gate");

            _bossGateTitle = InRunUiFactory.CreateText(_bossGatePanel.transform, "Title",
                BossGateTitle(0),
                18, TextAnchor.MiddleCenter, InRunUiFactory.AccentGold);
            _bossGateTitle.rectTransform.anchorMin = new Vector2(0.05f, 0.90f);
            _bossGateTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.98f);
            _bossGateTitle.rectTransform.offsetMin = Vector2.zero;
            _bossGateTitle.rectTransform.offsetMax = Vector2.zero;

            // Boss avatar — floor-specific icon displayed in the top-left of the panel
            var bossAvatarName = (_map?.Run?.State?.CurrentFloor ?? 4) switch
            {
                0 => "grove_spirit",
                1 => "koi_warden",
                2 => "shrine_oni",
                3 => "stone_patriarch",
                _ => "demon_mask"
            };
            var avatarSpr = Resources.Load<Sprite>("boss/icon_" + bossAvatarName);
            if (avatarSpr != null)
            {
                var avatarGo = new GameObject("BossAvatar", typeof(RectTransform), typeof(Image));
                avatarGo.transform.SetParent(_bossGatePanel.transform, false);
                var avatarRt = avatarGo.GetComponent<RectTransform>();
                avatarRt.anchorMin = new Vector2(0.02f, 0.87f);
                avatarRt.anchorMax = new Vector2(0.13f, 0.99f);
                avatarRt.offsetMin = Vector2.zero;
                avatarRt.offsetMax = Vector2.zero;
                avatarGo.GetComponent<Image>().sprite = avatarSpr;
                avatarGo.GetComponent<Image>().preserveAspect = true;
            }

            var run2 = _map?.Run;
            var seenMods = run2?.State?.SeenBossModifiers;
            for (var i = 0; i < _bossGateOptions.Count; i++)
            {
                var mod = _bossGateOptions[i];
                var seen = seenMods != null && seenMods.Contains(mod);
                var col = i % 3; var row = i / 3;
                var xMin = 0.05f + col * 0.31f;
                var yMax = 0.85f - row * 0.25f;
                var labelText = seen ? $"{BossService.GetModifierName(mod)}\n{BossService.GetModifierDescription(mod)}" : "???";
                Debug.Log(
                    $"[ModifierDiscovery] Boss gate label: mod={mod}, seen={seen}, " +
                    $"runSeen={ModifierDiscoveryService.Describe(seenMods)}, label={(seen ? "revealed" : "???")}");
                var btn = InRunUiFactory.CreatePanelButton(_bossGatePanel.transform, $"Mod_{i}",
                    new Vector2(xMin, yMax - 0.22f), new Vector2(xMin + 0.28f, yMax),
                    labelText);
                var modIconName = seen ? BossService.GetIconName(mod) : "";
                if (seen && string.IsNullOrEmpty(modIconName))
                    Debug.LogWarning($"[BossGateViewController] No icon mapped for seen modifier: {mod}");
                InRunUiFactory.SetButtonIcon(btn, seen ? modIconName : "petal_orb", !seen, seen ? BossService.GetIconFolder(mod) : "ui"); // F-QA: debuff icons routed to debuff/ subfolder
                // Alt Constraint Symbols: overlay abbreviation text on the icon area for accessibility
                if (seen && OptionsController.LiveAccessibility != null
                    && OptionsController.LiveAccessibility.AlternativeConstraintSymbols)
                {
                    var abbr = BossService.GetConstraintAbbreviation(mod);
                    var abbrGo = new GameObject("AbbrLabel", typeof(RectTransform), typeof(Text));
                    abbrGo.transform.SetParent(btn.transform, false);
                    var abbrRt = abbrGo.GetComponent<RectTransform>();
                    abbrRt.anchorMin = new Vector2(0f, 0.08f);
                    abbrRt.anchorMax = new Vector2(0.30f, 0.92f);
                    abbrRt.offsetMin = abbrRt.offsetMax = Vector2.zero;
                    var abbrTxt = abbrGo.GetComponent<Text>();
                    abbrTxt.text = abbr;
                    abbrTxt.font = FontAssetService.GetFont();
                    abbrTxt.fontSize = 14;
                    abbrTxt.fontStyle = FontStyle.Bold;
                    abbrTxt.alignment = TextAnchor.MiddleCenter;
                    abbrTxt.color = GamePalette.WinGold;
                }
                var captured = mod;
                btn.onClick.AddListener(() => ToggleBossModSelection(captured, btn));
            }

            // Confirm button (disabled until enough picks).
            // Override disabledColor so the button stays visible when not yet interactable.
            var confirmBtn = InRunUiFactory.CreateActionButton(_bossGatePanel.transform, "ConfirmBoss",
                new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.10f),
                T("Confirm"), UiAction.Confirm);
            var confirmCols = confirmBtn.colors;
            confirmCols.disabledColor = InRunUiFactory.BtnDisabled;
            confirmBtn.colors = confirmCols;
            confirmBtn.interactable = false;
            confirmBtn.onClick.AddListener(ConfirmBossGateAll);
            confirmBtn.gameObject.name = "ConfirmBossBtn";

            // Wire explicit D-pad grid navigation (3-column layout)
            var modCount = _bossGateOptions.Count;
            for (var i = 0; i < modCount; i++)
            {
                var btnGo = _bossGatePanel.transform.Find($"Mod_{i}");
                if (btnGo == null) continue;
                var btn = btnGo.GetComponent<Button>();
                if (btn == null) continue;
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i % 3 < 2 && i + 1 < modCount)
                    nav.selectOnRight = _bossGatePanel.transform.Find($"Mod_{i + 1}")?.GetComponent<Button>();
                if (i % 3 > 0)
                    nav.selectOnLeft  = _bossGatePanel.transform.Find($"Mod_{i - 1}")?.GetComponent<Button>();
                nav.selectOnDown  = (i + 3 < modCount)
                    ? _bossGatePanel.transform.Find($"Mod_{i + 3}")?.GetComponent<Button>()
                    : confirmBtn;
                if (i >= 3)
                    nav.selectOnUp    = _bossGatePanel.transform.Find($"Mod_{i - 3}")?.GetComponent<Button>();
                btn.navigation = nav;
            }
            // Confirm: up goes back to the first button in the last row
            var lastRowFirst = (modCount / 3) * 3;
            var cNav = confirmBtn.navigation;
            cNav.mode = Navigation.Mode.Explicit;
            cNav.selectOnUp = _bossGatePanel.transform.Find($"Mod_{lastRowFirst}")?.GetComponent<Button>();
            confirmBtn.navigation = cNav;

            // WardingFlame: show "Remove Modifier" button if relic held and not yet used
            var run3 = _map?.Run;
            if (run3 != null && RelicService.HasRelicOfType(run3.State, RelicId.WardingFlame) && !run3.State.WardingFlameUsed)
            {
                var wardingBtn = InRunUiFactory.CreateActionButton(_bossGatePanel.transform, "BtnWardingFlame",
                    new Vector2(0.05f, 0.02f), new Vector2(0.33f, 0.10f), T("InRun.BossGate.WardingFlame"), UiAction.WardingFlame);
                wardingBtn.onClick.AddListener(() =>
                {
                    if (_bossGateOptions.Count > 0 && RelicService.TryWardingFlame(run3.State))
                    {
                        _bossGateOptions.RemoveAt(0); // remove first (lowest-tier) option
                        _selectedBossMods.RemoveAll(m => !_bossGateOptions.Contains(m));
                        Object.Destroy(_bossGatePanel);
                        _bossGatePanel = null;
                        BuildBossGatePanel();
                    }
                });
            }

            ActivePanel = _bossGatePanel;
            FadeInPanel(_bossGatePanel);
            InRunUiFactory.SelectFirstInteractable(_bossGatePanel);
        }

        private void ToggleBossModSelection(BossModifierId mod, Button btn)
        {
            // Use Image.color directly — btn.colors.normalColor is multiplied with the
            // dark base image color, making gold highlights invisible.
            var img = btn.GetComponent<Image>();
            if (_selectedBossMods.Contains(mod))
            {
                _selectedBossMods.Remove(mod);
                if (img != null) img.color = InRunUiFactory.BtnColor;
            }
            else if (_selectedBossMods.Count < _bossPicksRequired)
            {
                _selectedBossMods.Add(mod);
                if (img != null) img.color = GamePalette.AccentGold;
            }

            // Update title
            if (_bossGateTitle != null)
                _bossGateTitle.text = BossGateTitle(_selectedBossMods.Count);

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
            var confirmGo = _bossGatePanel?.transform.Find("ConfirmBossBtn");
            if (confirmGo != null)
            {
                StartCoroutine(FlashAndAdvance(confirmGo.GetComponent<Button>()));
            }
            else
            {
                FinishConfirm();
            }
        }

        private System.Collections.IEnumerator FlashAndAdvance(Button btn)
        {
            if (btn != null)
            {
                var img = btn.GetComponent<Image>();
                if (img != null) img.color = GamePalette.WinGold;
            }
            yield return new WaitForSecondsRealtime(0.3f);
            FinishConfirm();
        }

        private void FinishConfirm()
        {
            _awaitingBossGate = false;
            ActivePanel = null;
            var run = _map?.Run;
            if (run != null) run.ChooseBossModifiers(new List<BossModifierId>(_selectedBossMods));
            _map?.SaveNow();
            if (_bossGatePanel != null) { Object.Destroy(_bossGatePanel); _bossGatePanel = null; }

            OnModsConfirmed?.Invoke(new List<BossModifierId>(_selectedBossMods));
        }

        private void FadeInPanel(GameObject panel)
        {
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            StartCoroutine(AnimationHelper.FadeIn(cg, AnimationHelper.MenuPanelDuration));
        }

        private string BossGateTitle(int selectedCount) =>
            LocalizationService.Format(
                "InRun.BossGate.Title",
                "Boss Gate - Pick {0} Modifiers ({1}/{0})",
                _bossPicksRequired,
                selectedCount);

        private static string T(string key) => LocalizationService.T(key);

        private static string F(string key, params object[] args) =>
            LocalizationService.Format(key, key, args);
    }
}
