using SudokuRoguelike.Core;
using SudokuRoguelike.Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: CUSTOM-HELP-001] [REQ: CUSTOM-SEED-001] [REQ: CUSTOM-CODE-001] [REQ: UI-STYLE-037] RM-209 (0.3.0 playtest): a "?"
    // beside each Board field opens a slip that says what it does; under the Board paper the puzzle's seed (typed or
    // rolled) and a share code that holds the whole setup, with Copy and Paste.
    public sealed partial class CustomPuzzleScreen
    {
        private enum HelpTopic { None = -1, Size, Difficulty, Regions, Resources }

        // Row centres on the settings slip (BuildSettingsSlip) and the slip's place on the screen.
        private static readonly float[] HelpRowCentre = { 0.834f, 0.696f, 0.578f, 0.376f };
        private const float SlipBottom = 0.4333f, SlipHeight = 0.3843f;

        private Image _helpSlip;
        private HelpTopic _helpPinned = HelpTopic.None, _helpShown = HelpTopic.None;
        private InputField _seedField;
        private Text _codeText, _codeMessage;

        private void BuildHelp(Transform slip)
        {
            HelpButton(slip, "HelpSize", new Vector2(0.935f, HelpRowCentre[0]), HelpTopic.Size);
            HelpButton(slip, "HelpDifficulty", new Vector2(0.935f, HelpRowCentre[1]), HelpTopic.Difficulty);
            HelpButton(slip, "HelpRegions", new Vector2(0.935f, HelpRowCentre[2] + 0.002f), HelpTopic.Regions);
            HelpButton(slip, "HelpResources", new Vector2(0.935f, HelpRowCentre[3] + 0.002f), HelpTopic.Resources);
            // Keyboard and pad: the row's own controls open its help while they have the focus.
            FollowFocus(slip, HelpTopic.Size, "SizeDec", "SizeInc");
            FollowFocus(slip, HelpTopic.Difficulty, "StarsDec", "StarsInc");
            FollowFocus(slip, HelpTopic.Regions, "RegionSeg_0", "RegionSeg_1", "RegionSeg_2");
            FollowFocus(slip, HelpTopic.Resources, "ResourceSeg_0", "ResourceSeg_1", "ClassPrev", "ClassNext");
        }

        private void HelpButton(Transform parent, string name, Vector2 centre, HelpTopic topic)
        {
            var disc = ScrollUiKit.Spot(parent, name, centre, new Vector2(26f, 26f), Color.white, ScrollUiKit.Disc());
            disc.raycastTarget = true;
            var button = disc.gameObject.AddComponent<Button>();
            button.targetGraphic = disc;
            button.colors = ScrollUiKit.StateColors(new Color(0.55f, 0.42f, 0.23f, 1f));
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ScrollUiKit.Label(disc.transform, "Glyph", "?", 17, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one);
            button.onClick.AddListener(() =>
            {
                _helpPinned = _helpPinned == topic ? HelpTopic.None : topic;
                ShowHelp(_helpPinned);
            });
            AddTrigger(disc.gameObject, EventTriggerType.PointerEnter, () => ShowHelp(topic));
            AddTrigger(disc.gameObject, EventTriggerType.PointerExit, () => ShowHelp(_helpPinned));
        }

        private void FollowFocus(Transform slip, HelpTopic topic, params string[] controls)
        {
            foreach (var name in controls)
            {
                var control = FindDeep(slip, name);
                if (control == null) continue;
                AddTrigger(control.gameObject, EventTriggerType.Select, () => { if (InputDeviceTracker.UsingPad) ShowHelp(topic); });
                AddTrigger(control.gameObject, EventTriggerType.Deselect, () => ShowHelp(_helpPinned));
            }
        }

        private void ShowHelp(HelpTopic topic)
        {
            if (_helpSlip != null) ScrollUiKit.DestroyNow(_helpSlip.gameObject);
            _helpSlip = null;
            _helpShown = topic;
            if (topic == HelpTopic.None || _screen == null) return;

            var rowY = SlipBottom + HelpRowCentre[(int)topic] * SlipHeight;
            var top = Mathf.Min(0.93f, rowY + 0.06f);
            var height = topic == HelpTopic.Difficulty ? 0.36f : 0.22f;
            _helpSlip = ScrollUiKit.PaperTag(_screen, "CustomHelp", new Vector2(0.245f, top - height), new Vector2(0.52f, top), 0f);
            _helpSlip.raycastTarget = false;
            var edge = ScrollUiKit.Fill(_helpSlip.transform, "Edge", Vector2.zero, new Vector2(0.025f, 1f), ScrollUiKit.Vermilion);
            edge.raycastTarget = false;

            // The eyebrow and the title keep their height in screen terms (0.026 and 0.040) whatever the slip's height.
            var key = "CustomPuzzle.Help." + topic;
            var eyebrowBottom = 1f - 0.026f / height;
            var titleBottom = eyebrowBottom - 0.040f / height;
            var textBottom = topic == HelpTopic.Difficulty ? 0.47f : 0.06f;
            ScrollUiKit.Label(_helpSlip.transform, "HelpEyebrow", T(key + ".Eyebrow", topic.ToString()).ToUpperInvariant(), 14,
                TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.07f, eyebrowBottom), new Vector2(0.96f, 0.985f), 9);
            ScrollUiKit.Label(_helpSlip.transform, "HelpTitle", T(key + ".Title", topic.ToString()), 24, TextAnchor.MiddleLeft,
                ScrollUiKit.Vermilion, new Vector2(0.07f, titleBottom), new Vector2(0.96f, eyebrowBottom), 14);
            ScrollUiKit.Label(_helpSlip.transform, "HelpBody", T(key + ".Body", string.Empty), 17, TextAnchor.UpperLeft, ScrollUiKit.Ink,
                new Vector2(0.07f, textBottom), new Vector2(0.96f, titleBottom - 0.005f), 11);
            if (topic == HelpTopic.Difficulty) DrawStarBars(_helpSlip.transform);
        }

        // One bar per star count, as long as the share of empty cells; the chosen one in vermilion.
        private void DrawStarBars(Transform parent)
        {
            for (var stars = 1; stars <= MaxStars; stars++)
            {
                var top = 0.45f - (stars - 1) * 0.062f;
                var on = stars == _setup.GetStars();
                var ink = on ? ScrollUiKit.Vermilion : new Color(0.72f, 0.54f, 0.17f, 1f);
                ScrollUiKit.Label(parent, $"Stars_{stars}", new string('★', stars), 13, TextAnchor.MiddleLeft, ink,
                    new Vector2(0.07f, top - 0.055f), new Vector2(0.30f, top), 8);
                var empty = Tutorial.TutorialModeService.GetMissingRange(stars).min;
                var bar = ScrollUiKit.Fill(parent, $"Bar_{stars}", new Vector2(0.31f, top - 0.04f),
                    new Vector2(0.31f + 0.46f * empty, top - 0.015f), on ? ScrollUiKit.Vermilion : ScrollUiKit.Moss, ScrollUiKit.Rounded(6f));
                bar.raycastTarget = false;
                ScrollUiKit.Label(parent, $"Share_{stars}", $"{Mathf.RoundToInt(empty * 100f)} %", 13, TextAnchor.MiddleLeft,
                    ScrollUiKit.InkSoft, new Vector2(0.79f, top - 0.055f), new Vector2(0.98f, top), 8);
            }
        }

        // ── seed and share code ──

        private void BuildSeedPaper(RectTransform screen)
        {
            var paper = ScrollUiKit.PaperTag(screen, "CustomSeedPaper", new Vector2(0.0458f, 0.165f), new Vector2(0.2354f, 0.415f), -0.6f).transform;
            Header(paper, "SeedHeader", "CustomPuzzle.Seed", "Seed", 0.905f);

            var go = new GameObject("CustomSeedInput", typeof(RectTransform), typeof(Image), typeof(GamepadKeyboardInputField));   // DECK-KEYBOARD-001
            go.transform.SetParent(paper, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.08f, 0.66f);
            rt.anchorMax = new Vector2(0.60f, 0.84f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var box = go.GetComponent<Image>();
            box.sprite = ScrollUiKit.Rounded(10f);
            box.type = Image.Type.Sliced;
            box.color = new Color(1f, 0.98f, 0.94f, 1f);
            ScrollUiKit.InkFrame(go.transform, new Color(0.33f, 0.29f, 0.24f, 0.45f), 1.5f);
            var text = ScrollUiKit.Label(go.transform, "Text", string.Empty, 21, TextAnchor.MiddleLeft, ScrollUiKit.Ink,
                new Vector2(0.06f, 0f), new Vector2(0.95f, 1f));
            text.supportRichText = false;
            _seedField = go.GetComponent<InputField>();
            _seedField.textComponent = text;
            _seedField.contentType = InputField.ContentType.IntegerNumber;
            _seedField.characterLimit = 9;
            _seedField.navigation = new Navigation { mode = Navigation.Mode.None };   // GAP-078: a focused field takes every key
            _seedField.onEndEdit.AddListener(value =>
            {
                if (int.TryParse(value, out var typed) && typed > 0) _setup.SetSeed(typed);
                else _seedField.text = _setup.Seed.ToString();
            });
            Pill(paper, "BtnSeedRandom", new Vector2(0.64f, 0.66f), new Vector2(0.94f, 0.84f), T("CustomPuzzle.Seed.Random", "Random"),
                () => _setup.RollSeed());

            Header(paper, "CodeHeader", "CustomPuzzle.Code", "Share code", 0.585f);
            var codeBox = ScrollUiKit.Fill(paper, "CodeBox", new Vector2(0.08f, 0.38f), new Vector2(0.94f, 0.54f),
                new Color(1f, 0.98f, 0.94f, 1f), ScrollUiKit.Rounded(10f));
            ScrollUiKit.InkFrame(codeBox.transform, new Color(0.33f, 0.29f, 0.24f, 0.45f), 1.5f);
            _codeText = ScrollUiKit.Label(codeBox.transform, "CodeText", string.Empty, 17, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                new Vector2(0.03f, 0f), new Vector2(0.97f, 1f), 10);
            Pill(paper, "BtnCodeCopy", new Vector2(0.08f, 0.19f), new Vector2(0.40f, 0.34f), T("CustomPuzzle.Code.Copy", "Copy"), CopyCode);
            Pill(paper, "BtnCodePaste", new Vector2(0.44f, 0.19f), new Vector2(0.94f, 0.34f), T("CustomPuzzle.Code.Paste", "Paste a code"),
                PasteCode);
            _codeMessage = ScrollUiKit.Label(paper, "CodeMessage", T("CustomPuzzle.Code.Note", string.Empty), 13, TextAnchor.UpperLeft,
                ScrollUiKit.InkSoft, new Vector2(0.08f, 0.01f), new Vector2(0.95f, 0.17f), 9);
        }

        private static void Pill(Transform parent, string name, Vector2 min, Vector2 max, string label, System.Action onClick)
        {
            var button = ScrollUiKit.PillButton(parent, name, min, max, label, 17, new Color(0.80f, 0.60f, 0.25f, 1f), ScrollUiKit.Ink, 16f);
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            button.onClick.AddListener(() => onClick());
        }

        private void RefreshSeedAndCode()
        {
            if (_helpShown == HelpTopic.Difficulty) ShowHelp(HelpTopic.Difficulty);   // the chosen stars' bar
            if (_seedField != null && !_seedField.isFocused) _seedField.text = _setup.Seed.ToString();
            if (_codeText != null) _codeText.text = CustomPuzzleCode.Encode(_setup.GetConfig());
        }

        private void CopyCode()
        {
            GUIUtility.systemCopyBuffer = CustomPuzzleCode.Encode(_setup.GetConfig());
            _codeMessage.text = T("CustomPuzzle.Code.Copied", "Copied.");
        }

        // [REQ: CUSTOM-CODE-001] The clipboard is outside input: only a whole, valid code of this version is taken.
        private void PasteCode()
        {
            if (!CustomPuzzleCode.TryDecode(GUIUtility.systemCopyBuffer, out var read))
            {
                _codeMessage.text = T("CustomPuzzle.Code.Invalid", "The clipboard holds no share code.");
                return;
            }
            _codeMessage.text = _setup.TryApply(read, out var reason) ? T("CustomPuzzle.Code.Read", "Code read.") : reason;
        }

        private static void AddTrigger(GameObject target, EventTriggerType type, System.Action action)
        {
            var trigger = target.GetComponent<EventTrigger>();
            if (trigger == null) trigger = target.AddComponent<EventTrigger>();   // not ??: Unity's fake null
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var hit = FindDeep(child, name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
