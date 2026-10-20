using System;
using System.Collections.Generic;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-013] The rest node as tea bowls on a low table in the rest painting (layout-proposals.md, "Rest"):
    // a banner naming the floor, the run's HP, Pencil and reroll tokens on a paper tag, the curses on the player on
    // another, and one paper slip per choice over its bowl with what it gives and the value after it. Choosing a bowl
    // lifts its slip and names it on the Drink button; choosing it again (click, key 1–4 or A) drinks it.
    // [REQ: CURSE-ACQUIRE-005] Heal, fresh ink (+4 Pencil) and a reroll token; [REQ: CURSE-ACQUIRE-004] Cleanse appears
    // only while a curse is active and lifts the oldest one.
    public sealed partial class RewardViewController
    {
        private sealed class RestChoice
        {
            public string Title;
            public string Text;
            public string After;
            public string Icon;
            public string Status;
            public Action Apply;
        }

        private readonly List<RestChoice> _restChoices = new List<RestChoice>();
        private readonly List<Button> _restBowls = new List<Button>();
        private Button _restDrink;
        private int _selectedRest = -1;
        private bool _restTaken;

        // Slip and bowl rows (fractions of the canvas, measured on the 1920×1080 mockup).
        private const float SlipBottom = 0.4167f, SlipTop = 0.6944f, SlipLift = 0.012f, ChoiceWidth = 0.1719f, ChoiceStep = 0.2083f;

        public bool IsRestOpen => _restPanel != null && _restPanel.activeSelf;

        public void ShowRestChoicePanel()
        {
            var run = _map?.Run;
            if (run?.State == null || _pathPanel == null) return;
            CloseRestScene();
            BuildRestChoices(run);
            _selectedRest = -1;
            _restTaken = false;

            var state = run.State;
            var screen = ScrollUiKit.Scene(_pathPanel, "RestChoicePanel", "bg_rest", new Color(0.60f, 0.60f, 0.56f, 1f));
            _restPanel = screen.gameObject;
            ScrollUiKit.Fill(screen.transform, "FloorShade", Vector2.zero, new Vector2(1f, 0.33f), new Color(0f, 0f, 0f, 0.30f));

            ScrollUiKit.Banner(screen.transform, "RestBanner", new Vector2(0.3177f, 0.8444f), new Vector2(0.6823f, 0.9574f),
                "RestEyebrow", F("InRun.BossDefeated.Eyebrow", state.CurrentFloor + 1, state.TotalFloors,
                    FloorThemeData.GetFloorName(state.CurrentFloor)),
                "RestTitle", T("InRun.Rest.SceneTitle"));
            BuildRestRunTag(screen.transform, state);
            BuildRestCurseTag(screen.transform, run);

            var prompt = ScrollUiKit.Label(screen.transform, "RestPrompt", T("InRun.Rest.Prompt"), 32, TextAnchor.MiddleCenter,
                ScrollUiKit.PaperText, new Vector2(0.25f, 0.744f), new Vector2(0.75f, 0.786f), 20);
            AddShadow(prompt);
            var sub = ScrollUiKit.Label(screen.transform, "RestPromptSub", T("InRun.Rest.PromptSub"), 19, TextAnchor.MiddleCenter,
                new Color(0.90f, 0.86f, 0.76f, 1f), new Vector2(0.25f, 0.720f), new Vector2(0.75f, 0.745f), 13);
            AddShadow(sub);

            BuildTeaTable(screen.transform);
            _restBowls.Clear();
            for (var i = 0; i < _restChoices.Count; i++)
                _restBowls.Add(BuildBowl(screen.transform, i, _restChoices.Count));

            _restDrink = ScrollUiKit.PillButton(screen.transform, "BtnRestDrink", new Vector2(0.40f, 0.0796f), new Vector2(0.60f, 0.1333f),
                T("InRun.Rest.PickOne"), 24, ScrollUiKit.Vermilion, Color.white, 24f);
            _restDrink.interactable = false;
            _restDrink.onClick.AddListener(() => { if (_selectedRest >= 0) DrinkRest(_selectedRest); });
            var hint = ScrollUiKit.Label(screen.transform, "RestHint", T("InRun.Rest.Hint"), 19, TextAnchor.MiddleCenter,
                new Color(0.86f, 0.82f, 0.70f, 1f), new Vector2(0.2f, 0.040f), new Vector2(0.8f, 0.070f), 12);
            AddShadow(hint);

            for (var i = 0; i < _restBowls.Count; i++)
                _restBowls[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? _restBowls[i - 1] : null,
                    selectOnRight = i < _restBowls.Count - 1 ? _restBowls[i + 1] : null,
                    selectOnDown = _restDrink
                };
            _restDrink.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = _restBowls[0] };

            ActivePanel = _restPanel;
            FadeInPanel(_restPanel);
            EventSystem.current?.SetSelectedGameObject(_restBowls[0].gameObject);
        }

        /// <summary>Keys 1–4 choose a bowl; the same key again drinks it.</summary>
        public void SelectRestByIndex(int index)
        {
            if (!IsRestOpen || index < 0 || index >= _restBowls.Count) return;
            EventSystem.current?.SetSelectedGameObject(_restBowls[index].gameObject);
            OnBowlClicked(index);
        }

        private void BuildRestChoices(RunDirector run)
        {
            var state = run.State;
            _restChoices.Clear();
            var heal = run.GetRestHealAmount();
            _restChoices.Add(new RestChoice
            {
                Title = T("InRun.Rest.Choice.Heal"), Text = F("InRun.Rest.Choice.HealText", heal),
                After = F("InRun.Rest.After.Hp", state.CurrentHP, Math.Min(state.MaxHP, state.CurrentHP + heal)),
                Icon = "ui/icon_petal_orb", Status = F("InRun.Rest.Status.Healed", heal), Apply = run.AcceptRestHeal
            });
            _restChoices.Add(new RestChoice
            {
                Title = T("InRun.Rest.Choice.Pencil"), Text = F("InRun.Rest.Choice.PencilText", RunDirector.RestPencilBoost),
                After = F("InRun.Rest.After.Pencil", state.CurrentPencil,
                    Math.Min(state.MaxPencil, state.CurrentPencil + RunDirector.RestPencilBoost)),
                Icon = "ui/icon_ink_save", Status = T("InRun.Rest.Status.Pencil"), Apply = run.AcceptRestPencilBoost
            });
            _restChoices.Add(new RestChoice
            {
                Title = T("InRun.Rest.Choice.Reroll"), Text = T("InRun.Rest.Choice.RerollText"),
                After = F("InRun.Rest.After.Tokens", state.RerollTokens, state.RerollTokens + 1),
                Icon = "ui/icon_reroll_arrow", Status = T("InRun.Rest.Status.Reroll"), Apply = run.AcceptRestRerollShop
            });
            var curses = run.GetActiveCurses();
            if (curses.Count > 0)
            {
                var oldest = CurseService.GetLocalizedName(curses[0]);
                _restChoices.Add(new RestChoice
                {
                    Title = T("InRun.Rest.Choice.Cleanse"), Text = F("InRun.Rest.Choice.CleanseText", oldest),
                    After = F("InRun.Rest.After.Curses", curses.Count, curses.Count - 1),
                    Icon = "cursed/icon_" + CurseService.GetIconName(curses[0].Id),
                    Status = F("InRun.Rest.Status.Cleansed", oldest), Apply = () => run.AcceptRestCurseRemoval()
                });
            }
        }

        private static float ChoiceCentre(int index, int count) => 0.5f + (index - (count - 1) * 0.5f) * ChoiceStep;

        // A paper slip pinned above a tea bowl; the whole slot is one button.
        private Button BuildBowl(Transform screen, int index, int count)
        {
            var choice = _restChoices[index];
            var centre = ChoiceCentre(index, count);
            var slot = ScrollUiKit.Area(screen, $"RestChoice_{index}", new Vector2(centre - ChoiceWidth * 0.5f, 0.29f),
                new Vector2(centre + ChoiceWidth * 0.5f, SlipTop + SlipLift + 0.02f));
            var hit = slot.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = slot.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            ScrollUiKit.Spot(screen, $"RestGlow_{index}", new Vector2(centre, 0.365f), new Vector2(320f, 150f),
                new Color(1f, 0.85f, 0.50f, 0f), ScrollUiKit.Glow());
            var bowl = ScrollUiKit.Spot(screen, $"RestBowl_{index}", new Vector2(centre, 0.365f), new Vector2(170f, 84f),
                Color.white, ScrollUiKit.TeaBowl());
            bowl.raycastTarget = false;

            var slip = ScrollUiKit.Fill(screen, $"RestSlip_{index}", new Vector2(centre - ChoiceWidth * 0.5f, SlipBottom),
                new Vector2(centre + ChoiceWidth * 0.5f, SlipTop), ScrollUiKit.Paper);
            var shadow = slip.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(3f, -6f);
            ScrollUiKit.Spot(slip.transform, "Pin", new Vector2(0.5f, 1f), new Vector2(16f, 16f), ScrollUiKit.Vermilion, ScrollUiKit.Disc());
            ScrollUiKit.Icon(slip.transform, "Icon", choice.Icon, new Vector2(0.33f, 0.62f), new Vector2(0.67f, 0.93f), Color.white);
            ScrollUiKit.Label(slip.transform, "Title", choice.Title, 32, TextAnchor.MiddleCenter, ScrollUiKit.Ink,
                new Vector2(0.06f, 0.44f), new Vector2(0.94f, 0.60f), 18);
            ScrollUiKit.Label(slip.transform, "Text", choice.Text, 20, TextAnchor.UpperCenter, ScrollUiKit.InkSoft,
                new Vector2(0.07f, 0.17f), new Vector2(0.93f, 0.43f), 13);
            ScrollUiKit.Label(slip.transform, "After", choice.After, 18, TextAnchor.MiddleCenter, ScrollUiKit.Vermilion,
                new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.14f), 12);
            if (index < 9) ScrollUiKit.KeyBadge(screen, $"RestKey_{index}", new Vector2(centre, 0.304f), index + 1, 30f);

            var idx = index;
            button.onClick.AddListener(() => OnBowlClicked(idx));
            slot.SetAsLastSibling();   // above the slip and bowl, so the whole slot takes the click
            return button;
        }

        private static void BuildTeaTable(Transform screen)
        {
            var top = ScrollUiKit.Fill(screen, "RestTable", new Vector2(0.0938f, 0.259f), new Vector2(0.9063f, 0.361f),
                ScrollUiKit.Wood, ScrollUiKit.Rounded(10f));
            var shadow = top.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -14f);
            ScrollUiKit.Fill(top.transform, "Edge", new Vector2(0f, 0f), new Vector2(1f, 0.18f), new Color(0.29f, 0.18f, 0.09f, 1f));
            ScrollUiKit.Fill(screen, "RestTableLegLeft", new Vector2(0.1146f, 0.148f), new Vector2(0.1354f, 0.259f), ScrollUiKit.WoodDark);
            ScrollUiKit.Fill(screen, "RestTableLegRight", new Vector2(0.8646f, 0.148f), new Vector2(0.8854f, 0.259f), ScrollUiKit.WoodDark);
        }

        private void BuildRestRunTag(Transform screen, RunState state)
        {
            var tag = ScrollUiKit.PaperTag(screen, "RestRun", new Vector2(0.0573f, 0.8056f), new Vector2(0.2083f, 0.963f), -1.5f, 20f);
            ScrollUiKit.Label(tag.transform, "Header", T("InRun.Relic.YourRun").ToUpperInvariant(), 16, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.95f), 11);
            var rows = new (string key, string value)[]
            {
                ("InRun.Reward.Row.Hp", $"{state.CurrentHP} / {state.MaxHP}"),
                ("InRun.Reward.Row.Pencil", $"{state.CurrentPencil} / {state.MaxPencil}"),
                ("InRun.Rest.Row.Rerolls", state.RerollTokens.ToString())
            };
            for (var k = 0; k < rows.Length; k++)
            {
                var top = 0.74f - k * 0.235f;
                ScrollUiKit.ValueRow(tag.transform, $"Row_{k}", new Vector2(0.08f, top - 0.2f), new Vector2(0.92f, top),
                    T(rows[k].key), rows[k].value, 20, ScrollUiKit.Vermilion);
            }
        }

        private void BuildRestCurseTag(Transform screen, RunDirector run)
        {
            var curses = run.GetActiveCurses();
            if (curses.Count == 0) return;
            var tag = ScrollUiKit.PaperTag(screen, "RestCurses", new Vector2(0.7813f, 0.861f), new Vector2(0.9427f, 0.963f), 1.5f, 20f);
            ScrollUiKit.Label(tag.transform, "Header", T(curses.Count > 1 ? "InRun.Rest.CursesOn" : "InRun.Rest.CurseOn").ToUpperInvariant(),
                16, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.92f), 11);
            var shown = Mathf.Min(curses.Count, 2);
            for (var i = 0; i < shown; i++)
            {
                var x = 0.06f + i * 0.47f;
                ScrollUiKit.Icon(tag.transform, $"CurseIcon_{i}", "cursed/icon_" + CurseService.GetIconName(curses[i].Id),
                    new Vector2(x, 0.12f), new Vector2(x + 0.14f, 0.60f), Color.white);
                ScrollUiKit.Label(tag.transform, $"CurseName_{i}", CurseService.GetLocalizedName(curses[i]), 18, TextAnchor.MiddleLeft,
                    ScrollUiKit.Ink, new Vector2(x + 0.15f, 0.12f), new Vector2(x + 0.46f, 0.60f), 11);
            }
        }

        private void OnBowlClicked(int index)
        {
            if (_restTaken || index < 0 || index >= _restChoices.Count) return;
            if (_selectedRest == index)
            {
                DrinkRest(index);
                return;
            }
            _selectedRest = index;
            for (var i = 0; i < _restChoices.Count; i++)
            {
                var slip = _restPanel.transform.Find($"RestSlip_{i}") as RectTransform;
                if (slip != null)
                {
                    var lift = i == index ? SlipLift : 0f;
                    var centre = ChoiceCentre(i, _restChoices.Count);
                    slip.anchorMin = new Vector2(centre - ChoiceWidth * 0.5f, SlipBottom + lift);
                    slip.anchorMax = new Vector2(centre + ChoiceWidth * 0.5f, SlipTop + lift);
                    var ring = slip.GetComponent<Outline>();   // Unity null, not ??
                    if (ring == null) ring = slip.gameObject.AddComponent<Outline>();
                    ring.effectColor = ScrollUiKit.Vermilion;
                    ring.effectDistance = new Vector2(4f, -4f);
                    ring.enabled = i == index;
                }
                var glow = _restPanel.transform.Find($"RestGlow_{i}")?.GetComponent<Image>();
                if (glow != null) glow.color = new Color(1f, 0.85f, 0.50f, i == index ? 0.55f : 0f);
            }
            var label = _restDrink != null ? _restDrink.transform.Find("Label")?.GetComponent<Text>() : null;
            if (label != null) label.text = F("InRun.Rest.Drink", _restChoices[index].Title);
            if (_restDrink != null) _restDrink.interactable = true;
        }

        // One choice only: a double click cannot drink twice. If the choice throws, the table stays usable.
        private void DrinkRest(int index)
        {
            if (_restTaken || index < 0 || index >= _restChoices.Count) return;
            _restTaken = true;
            var choice = _restChoices[index];
            try
            {
                choice.Apply?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _restTaken = false;
                return;
            }
            OnStatusChanged?.Invoke(choice.Status);
            CloseRestScene();
        }
    }
}
