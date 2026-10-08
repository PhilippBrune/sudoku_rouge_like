using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.UI
{
    // [REQ: REQ-RUN-101] In-run menu additions (GAP-020): the run seed, View Modifiers and Abandon Run.
    // The buttons are built by InRunUiBlueprintBuilder.BuildInGameOptionsPanel.
    public sealed partial class InRunController
    {
        private Text _abandonLabel;
        private bool _abandonArmed;
        private GameObject _modifierList;

        private void WireRunMenu()
        {
            var panel = _inGameOptionsPanel != null ? _inGameOptionsPanel.transform : null;
            if (panel == null) return;

            var modifiers = FindInPanel<Button>(panel, "BtnInGameOptionsModifiers");
            if (modifiers != null)
            {
                modifiers.onClick.RemoveAllListeners();
                modifiers.onClick.AddListener(ShowModifierList);
            }

            var abandon = FindInPanel<Button>(panel, "BtnInGameOptionsAbandon");
            if (abandon != null)
            {
                _abandonLabel = abandon.transform.Find("Label")?.GetComponent<Text>();
                abandon.onClick.RemoveAllListeners();
                abandon.onClick.AddListener(OnAbandonPressed);
            }
        }

        private void RefreshRunMenu()
        {
            var panel = _inGameOptionsPanel != null ? _inGameOptionsPanel.transform : null;
            var state = _map?.Run?.State;
            if (panel == null || state == null) return;

            var seed = FindInPanel<Text>(panel, "IGRunSeed");
            if (seed != null) seed.text = Tf("Seed: {0}", state.Seed);

            // Tutorials end through Save & Quit; they have no run result to record.
            var abandon = FindInPanel<Button>(panel, "BtnInGameOptionsAbandon");
            if (abandon != null) abandon.gameObject.SetActive(!state.TutorialMode);
            _abandonArmed = false;
            if (_abandonLabel != null) _abandonLabel.text = T("Abandon Run");
            if (_modifierList != null) Destroy(_modifierList);
        }

        // Two presses: the first asks for confirmation, the second ends the run as a defeat.
        private void OnAbandonPressed()
        {
            var run = _map?.Run;
            if (run?.State == null) return;
            if (!_abandonArmed)
            {
                _abandonArmed = true;
                if (_abandonLabel != null) _abandonLabel.text = T("Confirm: end this run?");
                return;
            }

            _abandonArmed = false;
            Debug.Log($"[InRunController] Abandon Run: floor {run.State.CurrentFloor + 1}, XP {run.GetTotalRunXp()}.");
            if (_inGameOptionsPanel != null) _inGameOptionsPanel.SetActive(false);
            ShowDefeatScreen(run);
        }

        private void ShowModifierList()
        {
            var panel = _inGameOptionsPanel != null ? _inGameOptionsPanel.transform : null;
            if (panel == null) return;
            if (_modifierList != null) Destroy(_modifierList);

            // A child of the options panel, so closing the options (Back / B) also closes the list.
            _modifierList = new GameObject("ModifierList", typeof(RectTransform), typeof(Image));
            _modifierList.transform.SetParent(panel, false);
            var rt = _modifierList.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            // Opaque: the options labels underneath showed through the panel colour (1.1 build smoke B13).
            var bg = InRunUiFactory.PanelBg;
            _modifierList.GetComponent<Image>().color = new Color(bg.r, bg.g, bg.b, 1f);

            var title = InRunUiFactory.CreateText(_modifierList.transform, "Title", T("Active Modifiers"), 22,
                TextAnchor.UpperCenter, InRunUiFactory.AccentGold);
            Place(title.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f));

            var body = InRunUiFactory.CreateText(_modifierList.transform, "Body", BuildModifierListText(), 14,
                TextAnchor.UpperLeft, InRunUiFactory.TextColor);
            body.supportRichText = true;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            Place(body.rectTransform, new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.86f));

            var back = InRunUiFactory.CreateActionButton(_modifierList.transform, "BtnModifierListBack",
                new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.10f), T("Back"), UiAction.Back);
            back.onClick.AddListener(() =>
            {
                Destroy(_modifierList);
                _modifierList = null;
                var reopen = FindInPanel<Button>(panel, "BtnInGameOptionsModifiers");
                if (reopen != null) EventSystem.current?.SetSelectedGameObject(reopen.gameObject);
            });
            EventSystem.current?.SetSelectedGameObject(back.gameObject);
        }

        // Floor modifiers, the current puzzle's modifiers and this floor's boss debuff, each with its rule text.
        private string BuildModifierListText()
        {
            var run = _map?.Run;
            var sb = new StringBuilder();
            void Add(BossModifierId id)
            {
                sb.Append("<b>").Append(BossService.GetModifierName(id)).Append("</b>  ");
                sb.AppendLine(BossService.GetModifierDescription(id));
            }

            var floorMods = run?.State?.ActiveFloorModifiers;
            if (floorMods != null && floorMods.Count > 0)
            {
                sb.AppendLine(T("Floor modifiers:"));
                foreach (var id in floorMods) Add(id);
                sb.AppendLine();
            }

            var puzzleMods = run?.CurrentLevelConfig?.ActiveModifiers;
            var extra = 0;
            if (puzzleMods != null && run.CurrentBoard != null)
                foreach (var id in puzzleMods)
                {
                    if (floorMods != null && floorMods.Contains(id)) continue;
                    if (extra++ == 0) sb.AppendLine(T("This puzzle:"));
                    Add(id);
                }

            if (extra == 0 && run?.GetBossDebuffForCurrentFloor() is { } debuff)
            {
                sb.AppendLine(T("This floor's boss:"));
                Add(debuff);
            }

            return sb.Length > 0 ? sb.ToString() : T("No modifiers are active.");
        }

        private static TComponent FindInPanel<TComponent>(Transform panel, string name) where TComponent : Component
        {
            foreach (var component in panel.GetComponentsInChildren<TComponent>(true))
                if (component.gameObject.name == name) return component;
            return null;
        }

        private static void Place(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
