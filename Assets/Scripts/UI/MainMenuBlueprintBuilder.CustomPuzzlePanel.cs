using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SudokuRoguelike.Boss;
using SudokuRoguelike.Bootstrap;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Run;
using SudokuRoguelike.Save;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Custom Puzzle (tutorial setup) panel. Split out of MainMenuBlueprintBuilder.cs per
    /// docs/controller-extraction-plan.md, following MainMenuBlueprintBuilder.ChallengePanels.cs.
    /// </summary>
    public sealed partial class MainMenuBlueprintBuilder
    {
        // ------------------------------------------------------------------------
        // Panel: Tutorial Setup
        // ------------------------------------------------------------------------

        private GameObject BuildTutorialSetupPanel(RectTransform root, MainMenuController mc,
            TutorialMenuController tutCtrl)
        {
            var panel = MakePanel("TutorialSetupPanel", root,
                new Vector2(0.14f, 0.05f), new Vector2(0.86f, 0.95f));
            var pr = GetPanelContent(panel);

            var title = BuildText("Title", pr, T("Custom Puzzle"), 28, TextAnchor.UpperCenter);
            SetRect(title.rectTransform,
                new Vector2(0.04f, 0.91f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

            // MM-3: helper that builds a [<] [value label] [>] selector row.
            // Returns the value Text and a setter to force-set the current index externally.
            Text BuildArrowSelector(string id, RectTransform parent,
                Vector2 aMin, Vector2 aMax, string[] options, int defaultIdx,
                System.Action<int> onChanged)
            {
                var idx = defaultIdx;

                var btnDec = BuildButton(id + "Dec", parent, "<", 16);
                SetRect(btnDec.GetComponent<RectTransform>(),
                    aMin, new Vector2(aMin.x + 0.06f, aMax.y), Vector2.zero, Vector2.zero);

                var valTxt = BuildText(id + "Val", parent,
                    options[idx], 13, TextAnchor.MiddleCenter);
                SetRect(valTxt.rectTransform,
                    new Vector2(aMin.x + 0.06f, aMin.y), new Vector2(aMax.x - 0.06f, aMax.y),
                    Vector2.zero, Vector2.zero);

                var btnInc = BuildButton(id + "Inc", parent, ">", 16);
                SetRect(btnInc.GetComponent<RectTransform>(),
                    new Vector2(aMax.x - 0.06f, aMin.y), aMax, Vector2.zero, Vector2.zero);

                btnDec.onClick.RemoveAllListeners();
                btnDec.onClick.AddListener(() =>
                {
                    idx = (idx + options.Length - 1) % options.Length;
                    valTxt.text = options[idx];
                    onChanged(idx);
                });
                btnInc.onClick.RemoveAllListeners();
                btnInc.onClick.AddListener(() =>
                {
                    idx = (idx + 1) % options.Length;
                    valTxt.text = options[idx];
                    onChanged(idx);
                });

                LinkHorizontalNav(btnDec, btnInc);
                return valTxt;
            }

            // Board Size
            var bsLabel = BuildText("BoardSizeLabel", pr, T("Board Size"), 15, TextAnchor.MiddleLeft);
            SetRect(bsLabel.rectTransform,
                new Vector2(0.04f, 0.84f), new Vector2(0.28f, 0.89f), Vector2.zero, Vector2.zero);
            BuildArrowSelector("BoardSize", pr,
                new Vector2(0.30f, 0.84f), new Vector2(0.96f, 0.89f),
                new[] { "5x5", "6x6", "7x7", "8x8", "9x9" }, 0,
                tutCtrl.SetBoardSizeIndex);

            // Stars
            var stLabel = BuildText("StarsLabel", pr, T("Difficulty"), 15, TextAnchor.MiddleLeft);
            SetRect(stLabel.rectTransform,
                new Vector2(0.04f, 0.77f), new Vector2(0.28f, 0.82f), Vector2.zero, Vector2.zero);
            BuildArrowSelector("Stars", pr,
                new Vector2(0.30f, 0.77f), new Vector2(0.96f, 0.82f),
                new[] { "1 Star (40%)", "2 Stars (50%)", "3 Stars (60%)",
                        "4 Stars (70%)", "5 Stars (80%)", "6 Stars (90%)", "7 Stars (100%)" }, 1,
                tutCtrl.SetStarsIndex);

            // Region Layout
            var rgLabel = BuildText("RegionLabel", pr, T("Region Layout"), 15, TextAnchor.MiddleLeft);
            SetRect(rgLabel.rectTransform,
                new Vector2(0.04f, 0.70f), new Vector2(0.28f, 0.75f), Vector2.zero, Vector2.zero);
            BuildArrowSelector("Region", pr,
                new Vector2(0.30f, 0.70f), new Vector2(0.96f, 0.75f),
                new[] { "Standard", "Alt Rectangular", "Jigsaw" }, 0,
                tutCtrl.SetRegionIndex);

            // -- Resource Mode (arrow row) --
            var rmLabel = BuildText("ResourceModeLabel", pr, T("Resource Mode"), 15, TextAnchor.MiddleLeft);
            rmLabel.fontStyle = FontStyle.Bold;
            rmLabel.color = TxtColor;
            SetRect(rmLabel.rectTransform,
                new Vector2(0.04f, 0.63f), new Vector2(0.28f, 0.68f), Vector2.zero, Vector2.zero);
            BuildArrowSelector("ResourceMode", pr,
                new Vector2(0.30f, 0.63f), new Vector2(0.96f, 0.68f),
                new[] { T("Free (∞ HP/Pencil)"), T("Class-Based") }, 0,
                tutCtrl.SetResourceModeIndex);

            // -- Class row (hidden by default in Free mode) --
            var classRow = EnsureRect("ClassRow", pr,
                new Vector2(0.04f, 0.57f), new Vector2(0.96f, 0.62f), Vector2.zero, Vector2.zero);
            var classLabel = BuildText("ClassLabel", classRow, T("Class:"), 15, TextAnchor.MiddleLeft);
            classLabel.color = TxtColor;
            SetRect(classLabel.rectTransform,
                new Vector2(0f, 0f), new Vector2(0.28f, 1f), Vector2.zero, Vector2.zero);

            var classNames = new[]
            {
                T("Number Freak"), T("Garden Monk"), T("Shrine Archivist"),
                T("Koi Gambler"), T("Stone Gardener"), T("Lantern Seer"),
                T("Reed Duelist"), T("Quiet Cartographer")
            };
            var classArrowLabel = BuildArrowSelector("Class", classRow,
                new Vector2(0.30f, 0f), new Vector2(1f, 1f),
                classNames, 0,
                tutCtrl.SetClassIndex);

            classRow.gameObject.SetActive(false);
            tutCtrl.ConfigureArrowClassRow(classRow);
            tutCtrl.SetClassArrowLabel(classArrowLabel);

            // -- Sudoku Modes label --
            var modsTitle = BuildText("ModsTitle", pr, T("Sudoku Modes (max 5)"), 15, TextAnchor.MiddleLeft);
            modsTitle.fontStyle = FontStyle.Bold;
            modsTitle.color = AccentColor;
            SetRect(modsTitle.rectTransform,
                new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.56f), Vector2.zero, Vector2.zero);

            // -- 6 modifier columns (30 total: 15 original + 15 extended) --
            var colW = 0.68f / 6f;

            RectTransform MakeCol(string colName, int idx)
            {
                var xMin = 0.02f + idx * colW;
                var xMax = xMin + colW - 0.005f;
                var col = EnsureRect(colName, pr,
                    new Vector2(xMin, 0.10f), new Vector2(xMax, 0.51f), Vector2.zero, Vector2.zero);
                var vl = EnsureComponent<VerticalLayoutGroup>(col.gameObject);
                vl.childAlignment = TextAnchor.UpperCenter;
                vl.spacing = 1f;
                vl.childControlWidth = true;
                vl.childControlHeight = false;
                vl.childForceExpandWidth = true;
                vl.childForceExpandHeight = false;
                vl.padding = new RectOffset(1, 1, 1, 1);
                return col;
            }

            void AddHeader(RectTransform col, string goName, string text)
            {
                var h = EnsureRect(goName, col, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var t = EnsureComponent<Text>(h.gameObject);
                t.text = text;
                t.font = GetBuiltInFont();
                t.fontSize = 10;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = AccentColor;
                t.fontStyle = FontStyle.Bold;
                EnsureComponent<LayoutElement>(h.gameObject).preferredHeight = 14f;
            }

            Toggle AddTgl(RectTransform col, string goName, string text)
            {
                var tgl = BuildToggle(goName, col, text);
                EnsureComponent<LayoutElement>(tgl.gameObject).preferredHeight = 16f;
                return tgl;
            }

            // Col 0: Global / Negative (original 2 + 6 extended = 8)
            var c0 = MakeCol("ColGlobal", 0);
            AddHeader(c0, "HdrGlobal", "Global/Neg");
            var noncons        = AddTgl(c0, "TglNoncons",        "Nonconsec.");
            var antiknight     = AddTgl(c0, "TglAntiknight",     "Anti-Knight");
            var antiking       = AddTgl(c0, "TglAntiking",       "Anti-King");
            var antibishop     = AddTgl(c0, "TglAntiBishop",     "Anti-Bishop");
            var nonconsecDiag  = AddTgl(c0, "TglNonconsecDiag",  "NcDiagonal");
            var distGe2        = AddTgl(c0, "TglDistGe2",        "Dist = 2");
            var entropy        = AddTgl(c0, "TglEntropy",        "Entropy");
            var modular        = AddTgl(c0, "TglModular",        "Modular Rgn");

            // Col 1: Line (original 6 + 3 extended = 9)
            var c1 = MakeCol("ColLine", 1);
            AddHeader(c1, "HdrLine", "Line");
            var german     = AddTgl(c1, "TglGerman",     "German");
            var dutch      = AddTgl(c1, "TglDutch",      "Dutch");
            var parity     = AddTgl(c1, "TglParity",     "Parity");
            var renban     = AddTgl(c1, "TglRenban",     "Renban");
            var palindrome = AddTgl(c1, "TglPalindrome", "Palindrome");
            var between    = AddTgl(c1, "TglBetween",    "Between");
            var consecLine = AddTgl(c1, "TglConsecLine", "Consecutive");
            var slowThermo = AddTgl(c1, "TglSlowThermo", "SlowThermo");
            var uniqueSet  = AddTgl(c1, "TglUniqueSet",  "UniqueSet");

            // Col 2: Thermo + Arrow
            var c2 = MakeCol("ColThermoArrow", 2);
            AddHeader(c2, "HdrThermoArrow", "Thermo/Arrow");
            var thermo = AddTgl(c2, "TglThermo", "Thermo");
            var arrow  = AddTgl(c2, "TglArrow",  "Arrow Sums");

            // Col 3: Dots (original 2 + 2 extended = 4)
            var c3 = MakeCol("ColDots", 3);
            AddHeader(c3, "HdrDots", "Dots");
            var diff       = AddTgl(c3, "TglDiff",       "Diff Kropki");
            var ratio      = AddTgl(c3, "TglRatio",      "Ratio Kropki");
            var fullKropki = AddTgl(c3, "TglFullKropki", "Full Kropki");
            var sumKropki  = AddTgl(c3, "TglSumKropki",  "Sum Dot");

            // Col 4: Pairs + Cages (original 1 + 2 extended = 3 + Killer)
            var c4 = MakeCol("ColPairs", 4);
            AddHeader(c4, "HdrPairs", "Pairs/Cage");
            var killer       = AddTgl(c4, "TglKiller",       "Killer");
            var greaterLess  = AddTgl(c4, "TglGreaterLess",  "Gt/LessThan");
            var xvPairs      = AddTgl(c4, "TglXVPairs",      "XV Pairs");

            // Col 5: Cell / Fog (original 2 + 2 extended = 4)
            var c5 = MakeCol("ColCell", 5);
            AddHeader(c5, "HdrCell", "Cell / Fog");
            var evenOdd      = AddTgl(c5, "TglEvenOdd",      "Even/Odd");
            var fog          = AddTgl(c5, "TglFog",          "Fog of War");
            var primeCells   = AddTgl(c5, "TglPrimeCells",   "Prime");
            var fortressCells= AddTgl(c5, "TglFortress",     "Fortress");

            // Bottom controls
            var startBtn = BuildButton("BtnTutStart", pr, T("Start Puzzle"), 15);
            SetRect(startBtn.GetComponent<RectTransform>(),
                new Vector2(0.02f, 0.01f), new Vector2(0.46f, 0.09f), Vector2.zero, Vector2.zero);
            startBtn.onClick.RemoveAllListeners();
            startBtn.onClick.AddListener(mc.StartTutorialGame);
            ApplyMenuButtonIcon(startBtn, "ui/icon_start_run");             // F-QA: dedicated start-run icon

            var backBtn = BuildButton("BtnTutBack", pr, T("Back"), 18);
            SetRect(backBtn.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.01f), new Vector2(0.90f, 0.09f), Vector2.zero, Vector2.zero);
            backBtn.onClick.RemoveAllListeners();
            backBtn.onClick.AddListener(mc.BackToMainMenu);
            ApplyMenuButtonIcon(backBtn, "ui/icon_back_leaf"); // F16: back/close uses dedicated back_leaf icon;

            // -- Selection summary (size, stars, regions, resources, chosen modes) above the tooltip --
            var summaryBg = EnsureRect("CustomSelectionSummaryBg", pr,
                new Vector2(0.72f, 0.35f), new Vector2(0.90f, 0.51f), Vector2.zero, Vector2.zero);
            EnsureComponent<Image>(summaryBg.gameObject).color = GamePalette.ShadowDark;
            var summaryRect = EnsureRect("CustomSelectionSummary", summaryBg,
                new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);
            var selectionSummary = EnsureComponent<Text>(summaryRect.gameObject);
            selectionSummary.font = GetBuiltInFont();
            selectionSummary.fontSize = 10;
            selectionSummary.color = TxtColor;
            selectionSummary.alignment = TextAnchor.UpperLeft;
            selectionSummary.horizontalOverflow = HorizontalWrapMode.Wrap;
            selectionSummary.verticalOverflow = VerticalWrapMode.Truncate;
            selectionSummary.resizeTextForBestFit = true;
            selectionSummary.resizeTextMinSize = 8;
            selectionSummary.resizeTextMaxSize = 10;
            selectionSummary.raycastTarget = false;

            // -- Modifier hover tooltip --
            var ttBg = EnsureRect("ModTooltipBg", pr,
                new Vector2(0.72f, 0.10f), new Vector2(0.90f, 0.33f), Vector2.zero, Vector2.zero);
            var ttImg = EnsureComponent<Image>(ttBg.gameObject);
            ttImg.color = GamePalette.ShadowDark;
            var ttTextRect = EnsureRect("ModTooltipText", ttBg,
                new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);
            var ttText = EnsureComponent<Text>(ttTextRect.gameObject);
            ttText.font = GetBuiltInFont();
            ttText.fontSize = 11;
            ttText.color = TxtColor;
            ttText.alignment = TextAnchor.UpperLeft;
            ttText.horizontalOverflow = HorizontalWrapMode.Wrap;
            ttText.verticalOverflow = VerticalWrapMode.Overflow;
            var defaultTooltip = T("Hover a Sudoku mode for details.");
            ttText.text = defaultTooltip;
            ttBg.gameObject.SetActive(true);

            // The tooltip is the modifier's rule text, the same (localized) text as on the boss gate and in the legend.
            void WireTip(Toggle tgl, BossModifierId id)
            {
                var desc = BossService.GetModifierDescription(id);
                var et = EnsureComponent<EventTrigger>(tgl.gameObject);
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => ttText.text = desc);
                et.triggers.Add(enter);
                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => ttText.text = defaultTooltip);
                et.triggers.Add(exit);
            }

            WireTip(german, BossModifierId.GermanWhispers);
            WireTip(dutch, BossModifierId.DutchWhispers);
            WireTip(parity, BossModifierId.ParityLines);
            WireTip(renban, BossModifierId.RenbanLines);
            WireTip(diff, BossModifierId.DifferenceKropki);
            WireTip(ratio, BossModifierId.RatioKropki);
            WireTip(killer, BossModifierId.KillerCages);
            WireTip(arrow, BossModifierId.ArrowSums);
            WireTip(fog, BossModifierId.FogOfWar);
            WireTip(palindrome, BossModifierId.Palindrome);
            WireTip(thermo, BossModifierId.Thermo);
            WireTip(between, BossModifierId.BetweenLines);
            WireTip(evenOdd, BossModifierId.EvenOdd);
            WireTip(noncons, BossModifierId.Nonconsecutive);
            WireTip(antiknight, BossModifierId.Antiknight);
            WireTip(antiking, BossModifierId.Antiking);
            WireTip(antibishop, BossModifierId.AntiBishop);
            WireTip(nonconsecDiag, BossModifierId.NonconsecDiagonal);
            WireTip(distGe2, BossModifierId.DistanceGe2);
            WireTip(entropy, BossModifierId.EntropyGlobal);
            WireTip(modular, BossModifierId.ModularRegions);
            WireTip(consecLine, BossModifierId.ConsecutiveLine);
            WireTip(slowThermo, BossModifierId.SlowThermo);
            WireTip(uniqueSet, BossModifierId.UniqueSetLine);
            WireTip(fullKropki, BossModifierId.FullKropki);
            WireTip(sumKropki, BossModifierId.SumKropki);
            WireTip(greaterLess, BossModifierId.GreaterLessThan);
            WireTip(xvPairs, BossModifierId.XVPairs);
            WireTip(primeCells, BossModifierId.PrimeCells);
            WireTip(fortressCells, BossModifierId.FortressCells);

            // Wire tutorial controller ? order matches TutorialMenuController.GetConfig() allModifiers (30 total)
            // MM-3: Pass null for the 3 dropdown params; size/stars/region are wired via SetXxxIndex above
            tutCtrl.Configure(null, null, null, startBtn,
                // Original 15: german, dutch, parity, renban, diff, ratio, killer, arrow, fog,
                //               palindrome, thermo, between, evenOdd, noncons, antiknight
                german, dutch, parity, renban, diff, ratio, killer, arrow, fog,
                palindrome, thermo, between, evenOdd, noncons, antiknight,
                // Extended 15
                antiking, antibishop, nonconsecDiag, distGe2, entropy, modular,
                consecLine, slowThermo, uniqueSet, fullKropki, sumKropki,
                greaterLess, xvPairs, primeCells, fortressCells);
            tutCtrl.SetSelectionSummaryText(selectionSummary);

            // Apply art background if the asset is available
            if (ApplyPanelBackground(panel.GetComponent<Image>(), "background/bg_tutorial_setup"))
            {
                var contentImg = pr.GetComponent<Image>();
                if (contentImg != null) contentImg.color = ContentScrimColor;
                var contentOutline = pr.GetComponent<Outline>();
                if (contentOutline != null) contentOutline.effectColor = Color.clear;

                ttText.color = TxtColor; // tooltip sits on ShadowDark bg ? keep light
                foreach (var btn in pr.GetComponentsInChildren<Button>(true))
                    StyleButtonForBackground(btn);
            }

            panel.SetActive(false);
            return panel;
        }
    }
}
