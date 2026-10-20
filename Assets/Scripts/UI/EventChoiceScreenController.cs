using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-014] The event node as a short hand scroll above the garden path (layout-proposals.md, "Event"):
    // HP, gold and pencil over the scroll, the event's title and story on the left, the choices as paper strips on the
    // right with their cost and gain. A choice the player cannot pay for is greyed with the reason. Choosing a strip
    // marks it and names it on the main button; choosing it again (click, key 1–3 or A) takes it. The result line then
    // shows what happened and the button becomes Continue (click, A, or keys 1–3 a moment later), which closes the scene.
    // If no choice can be paid, the button offers to walk on without one.
    // GAP-052: the old event panel was built under the full-screen path, so events opened invisibly.
    public sealed class EventChoiceScreenController : MonoBehaviour
    {
        private RunMapController _runMap;
        private GameObject _pathPanel;
        private GameObject _scene;
        private RunEvent _currentEvent;
        private readonly List<Button> _strips = new List<Button>();
        private readonly List<bool> _affordable = new List<bool>();
        private Button _mainButton;
        private Text _resultText;
        private int _selected = -1;
        private bool _chosen;
        private float _chosenAt;
        private const float KeyContinueDelay = 0.35f;   // a quick second tap must not skip the result line

        public GameObject ActivePanel { get; private set; }

        public void Bind(RunMapController runMap)
        {
            _runMap = runMap;
        }

        /// <summary>Kept for the blueprint; the scene is built above the path (see <see cref="SetSceneHost"/>).</summary>
        public void Configure(GameObject panel, Text prompt, Text result, Transform optionsContainer)
        {
            if (panel != null) panel.SetActive(false);
        }

        /// <summary>The garden path panel: the event scene sits right after it, so it covers the path.</summary>
        public void SetSceneHost(GameObject pathPanel)
        {
            _pathPanel = pathPanel;
        }

        public void OpenEvent()
        {
            if (_runMap == null || _pathPanel == null) return;
            CloseEvent();
            _currentEvent = _runMap.OpenEventNode();
            if (_currentEvent == null) return;
            BuildScene(_runMap.Run?.State);
            ActivePanel = _scene;
            var first = _strips.Find(s => s.interactable);
            if (first != null) EventSystem.current?.SetSelectedGameObject(first.gameObject);
        }

        public void CloseEvent()
        {
            ActivePanel = null;
            ScrollUiKit.DestroyNow(_scene);
            _scene = null;
            _strips.Clear();
            _affordable.Clear();
            _currentEvent = null;
            _selected = -1;
            _chosen = false;
        }

        /// <summary>Keys 1–3 choose a strip; the same key again takes it. After a choice they continue.</summary>
        public void SelectChoiceByIndex(int index)
        {
            if (_scene == null) return;
            if (_chosen)
            {
                if (Time.unscaledTime - _chosenAt >= KeyContinueDelay) Continue();
                return;
            }
            if (index < 0 || index >= _strips.Count || !_affordable[index]) return;
            EventSystem.current?.SetSelectedGameObject(_strips[index].gameObject);
            OnStripClicked(index);
        }

        // ── scene ──

        private void BuildScene(RunState state)
        {
            var screen = ScrollUiKit.Scene(_pathPanel, "EventScene", "bg_event", new Color(0.55f, 0.55f, 0.52f, 1f));
            _scene = screen.gameObject;
            if (state != null) BuildPurse(screen.transform, state);

            var paper = ScrollUiKit.HandScroll(screen.transform, "EventScroll", new Vector2(0.091f, 0.176f), new Vector2(0.909f, 0.843f),
                "bg_event", true);
            ScrollUiKit.Label(paper, "EventEyebrow", T("InRun.Event.Eyebrow", "AN ENCOUNTER ON THE PATH").ToUpperInvariant(), 17,
                TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.05f, 0.86f), new Vector2(0.46f, 0.90f), 12);
            ScrollUiKit.Label(paper, "EventTitle", _currentEvent.Title, 58, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.05f, 0.73f), new Vector2(0.46f, 0.86f), 30);
            ScrollUiKit.Label(paper, "EventStory", _currentEvent.Description, 25, TextAnchor.UpperLeft, new Color(0.23f, 0.20f, 0.16f, 1f),
                new Vector2(0.05f, 0.40f), new Vector2(0.46f, 0.71f), 16);
            ScrollUiKit.Icon(paper, "EventIcon", "node/icon_event_scroll", new Vector2(0.05f, 0.07f), new Vector2(0.20f, 0.38f), Color.white);
            BuildStamp(paper, T("InRun.Event.Seal", "Garden tale"));
            ScrollUiKit.Fill(paper, "EventDivider", new Vector2(0.495f, 0.09f), new Vector2(0.497f, 0.91f), new Color(0.62f, 0.16f, 0.17f, 0.35f));

            ScrollUiKit.Label(paper, "EventQuestion", T("InRun.Event.Question", "WHAT DO YOU DO?").ToUpperInvariant(), 20,
                TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.53f, 0.85f), new Vector2(0.95f, 0.90f), 13);
            var options = _currentEvent.Options;
            for (var i = 0; i < options.Count && i < 4; i++)
                _strips.Add(BuildStrip(paper, i, options[i], state));

            _resultText = ScrollUiKit.Label(paper, "EventResult", string.Empty, 22, TextAnchor.MiddleLeft, ScrollUiKit.Vermilion,
                new Vector2(0.53f, 0.07f), new Vector2(0.95f, 0.15f), 14);

            _mainButton = ScrollUiKit.PillButton(screen.transform, "BtnEventMain", new Vector2(0.43f, 0.06f), new Vector2(0.62f, 0.115f),
                T("InRun.Event.PickOne", "Choose what to do"), 24, ScrollUiKit.Vermilion, Color.white, 24f);
            _mainButton.interactable = false;
            ScrollUiKit.Label(screen.transform, "EventHint", T("InRun.Event.Hint", "1–3 to choose · the same key again takes it"), 17,
                TextAnchor.MiddleCenter, ScrollUiKit.PaperText, new Vector2(0.30f, 0.015f), new Vector2(0.70f, 0.05f), 11);
            _mainButton.onClick.AddListener(() =>
            {
                if (_chosen) Continue();
                else if (_selected >= 0) Take(_selected);
            });

            // The D-pad moves between the strips that can be taken, then down to the button.
            var payable = _strips.FindAll(s => s.interactable);
            for (var i = 0; i < payable.Count; i++)
                payable[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? payable[i - 1] : null,
                    selectOnDown = i < payable.Count - 1 ? payable[i + 1] : _mainButton
                };
            _mainButton.navigation = new Navigation
                { mode = Navigation.Mode.Explicit, selectOnUp = payable.Count > 0 ? payable[payable.Count - 1] : null };

            // Every event has a free choice today; should one not, the player walks on instead of being stuck.
            if (payable.Count == 0)
            {
                _chosen = true;
                SetMainLabel(T("InRun.Event.Continue", "Continue"));
                _mainButton.interactable = true;
            }
        }

        private void BuildPurse(Transform screen, RunState state)
        {
            var line = $"{F("InRun.BossDefeated.Eyebrow", state.CurrentFloor + 1, state.TotalFloors, FloorThemeData.GetFloorName(state.CurrentFloor))}";
            var floor = ScrollUiKit.Label(screen, "EventFloor", line, 22, TextAnchor.MiddleLeft, ScrollUiKit.PaperText,
                new Vector2(0.078f, 0.905f), new Vector2(0.40f, 0.950f), 14);
            Shade(floor);
            var stats = new (string icon, string text)[]
            {
                ("ui/icon_petal_orb", F("InRun.Event.Purse.Hp", state.CurrentHP, state.MaxHP)),
                ("economy/icon_sakura_coin", F("InRun.Event.Purse.Gold", state.CurrentGold)),
                ("ui/icon_ink_save", F("InRun.Event.Purse.Pencil", state.CurrentPencil))
            };
            for (var i = 0; i < stats.Length; i++)
            {
                var x = 0.42f + i * 0.13f;
                ScrollUiKit.Icon(screen, $"EventPurseIcon_{i}", stats[i].icon, new Vector2(x, 0.905f), new Vector2(x + 0.022f, 0.950f), Color.white);
                Shade(ScrollUiKit.Label(screen, $"EventPurse_{i}", stats[i].text, 22, TextAnchor.MiddleLeft, ScrollUiKit.PaperText,
                    new Vector2(x + 0.026f, 0.905f), new Vector2(x + 0.125f, 0.950f), 14));
            }
        }

        // A square vermilion stamp, slightly turned.
        private static void BuildStamp(RectTransform paper, string text)
        {
            var stamp = ScrollUiKit.Fill(paper, "EventStamp", new Vector2(0.31f, 0.14f), new Vector2(0.39f, 0.32f),
                new Color(0.70f, 0.20f, 0.15f, 0.85f), ScrollUiKit.Rounded(10f));
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            var inner = ScrollUiKit.Fill(stamp.transform, "Inner", Vector2.zero, Vector2.one, new Color(0.95f, 0.91f, 0.82f, 1f), ScrollUiKit.Rounded(8f));
            inner.rectTransform.offsetMin = new Vector2(5f, 5f);
            inner.rectTransform.offsetMax = new Vector2(-5f, -5f);
            ScrollUiKit.Label(stamp.transform, "Text", text, 24, TextAnchor.MiddleCenter, new Color(0.70f, 0.20f, 0.15f, 0.9f),
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), 13);
        }

        private Button BuildStrip(RectTransform paper, int index, EventOption option, RunState state)
        {
            var top = 0.82f - index * 0.20f;
            var strip = ScrollUiKit.Fill(paper, $"EventOption_{index}", new Vector2(0.53f, top - 0.17f), new Vector2(0.95f, top),
                new Color(0.98f, 0.95f, 0.88f, 1f));
            strip.raycastTarget = true;
            var border = strip.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.82f, 0.72f, 0.54f, 1f);
            border.effectDistance = new Vector2(2f, -2f);
            var button = strip.gameObject.AddComponent<Button>();
            button.targetGraphic = strip;
            button.colors = ScrollUiKit.StateColors(Color.white);

            var why = Affordability(option, state);
            var affordable = why == null;
            _affordable.Add(affordable);
            button.interactable = affordable;

            var badge = ScrollUiKit.KeyBadge(strip.transform, "KeyBadge", new Vector2(0.06f, 0.5f), index + 1, 36f);
            ScrollUiKit.SnapToPixels(badge.gameObject);   // the strips sit between pixels; keep the digit crisp
            ScrollUiKit.Label(strip.transform, "Label", option.Label, 28, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.13f, affordable ? 0.48f : 0.56f), new Vector2(0.78f, 0.94f), 16);
            ScrollUiKit.Label(strip.transform, "Effect", option.EffectDescription, 20, TextAnchor.MiddleLeft, GainColor(option),
                new Vector2(0.13f, affordable ? 0.10f : 0.32f), new Vector2(0.78f, affordable ? 0.48f : 0.56f), 13);
            if (!affordable)
                ScrollUiKit.Label(strip.transform, "Why", why, 17, TextAnchor.MiddleLeft, ScrollUiKit.Vermilion,
                    new Vector2(0.13f, 0.06f), new Vector2(0.95f, 0.32f), 11);
            var cost = CostText(option);
            var pill = ScrollUiKit.Pill(strip.transform, "Cost", new Vector2(0.80f, 0.34f), new Vector2(0.97f, 0.66f), cost,
                new Color(0.94f, 0.89f, 0.78f, 1f), cost == T("InRun.Event.Free", "free") ? ScrollUiKit.Ink : ScrollUiKit.Vermilion, 17);
            pill.raycastTarget = false;
            if (!affordable)
            {
                var group = strip.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0.55f;
            }
            var idx = index;
            button.onClick.AddListener(() => OnStripClicked(idx));
            return button;
        }

        /// <summary>Null when the player can pay for the option; otherwise the reason. HP costs never block (they stop at 1).</summary>
        private static string Affordability(EventOption option, RunState state)
        {
            if (state == null) return null;
            if (option.GoldCost > state.CurrentGold)
                return F("InRun.Event.NeedGold", option.GoldCost, state.CurrentGold);
            if (option.PencilCost > state.CurrentPencil)
                return F("InRun.Event.NeedPencil", option.PencilCost, state.CurrentPencil);
            return null;
        }

        private static string CostText(EventOption option)
        {
            var parts = new List<string>();
            if (option.GoldCost > 0) parts.Add(F("RunEvent.Result.GoldCost", option.GoldCost));
            if (option.HpCost > 0) parts.Add(F("RunEvent.Result.HpCost", option.HpCost));
            if (option.PencilCost > 0) parts.Add(F("RunEvent.Result.PencilCost", option.PencilCost));
            return parts.Count > 0 ? string.Join(" ", parts) : T("InRun.Event.Free", "free");
        }

        // Gains in green, a cost-only or curse line in red, "nothing happens" in ink.
        private static Color GainColor(EventOption option)
        {
            if (!string.IsNullOrEmpty(option.CurseId)) return ScrollUiKit.Vermilion;
            var gains = option.GoldGain > 0 || option.HpGain > 0 || option.PencilGain > 0 || option.MaxHpGain > 0 || option.GrantRandomItem;
            return gains ? new Color(0.18f, 0.42f, 0.23f, 1f) : ScrollUiKit.InkSoft;
        }

        private void OnStripClicked(int index)
        {
            if (_chosen || index < 0 || index >= _strips.Count || !_affordable[index]) return;
            if (_selected == index)
            {
                Take(index);
                return;
            }
            _selected = index;
            for (var i = 0; i < _strips.Count; i++)
            {
                var outline = _strips[i].GetComponent<Outline>();
                if (outline == null) continue;
                outline.effectColor = i == index ? ScrollUiKit.Vermilion : new Color(0.82f, 0.72f, 0.54f, 1f);
                outline.effectDistance = i == index ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
            }
            SetMainLabel(_currentEvent.Options[index].Label);
            _mainButton.interactable = true;
        }

        // One choice only. If resolving it throws, the strips stay usable.
        private void Take(int index)
        {
            if (_chosen || _runMap == null) return;
            _chosen = true;
            _chosenAt = Time.unscaledTime;
            string summary;
            try
            {
                summary = _runMap.ChooseEventOption(index);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                _chosen = false;
                return;
            }
            if (_resultText != null)
                _resultText.text = string.IsNullOrEmpty(summary) ? T("RunEvent.Result.Done", "Done.") : summary;
            foreach (var strip in _strips) strip.interactable = false;
            SetMainLabel(T("InRun.Event.Continue", "Continue"));
            _mainButton.interactable = true;
            EventSystem.current?.SetSelectedGameObject(_mainButton.gameObject);

            // Refresh the curse panel now that state may have changed (e.g. Ink Peddler gave a curse)
            var flowCtrl = GetComponentInParent<InRunUiFlowController>(true);
            flowCtrl?.RefreshRuntimePanels();
        }

        private void Continue()
        {
            var flowCtrl = GetComponentInParent<InRunUiFlowController>(true);
            if (flowCtrl != null) flowCtrl.OnEventClosed();
            else CloseEvent();
        }

        private void SetMainLabel(string text)
        {
            var label = _mainButton != null ? _mainButton.transform.Find("Label")?.GetComponent<Text>() : null;
            if (label != null) label.text = text;
        }

        private static void Shade(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(1f, -2f);
        }

        private static string T(string key, string fallback) => LocalizationService.T(key, fallback);

        private static string F(string key, params object[] args) => LocalizationService.Format(key, key, args);
    }
}
