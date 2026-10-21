using System.Text;
using SudokuRoguelike.Classes;
using SudokuRoguelike.Core;
using SudokuRoguelike.Data;
using SudokuRoguelike.Run;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuRoguelike.UI
{
    // [REQ: UI-STYLE-001] The garden path as an ink hand scroll (docs/design/layout-proposals.md, B): the floor's
    // painting blurred behind a hand scroll; on the paper the floor title, the floor marks, the route in ink (calm) and
    // vermilion (risk) through ensō nodes, the boss as a seal, and the route names and legend along the bottom; the run's
    // numbers on a paper tag; Save & Quit and Options as round buttons. The same frame on every floor.
    public sealed partial class InRunController
    {
        // The paper inside PathOverviewPanel (the panel spans 3–97 % of the screen; mockup px → (px/1920 − 0.03) / 0.94).
        private static readonly Vector2 PathPaperMin = new Vector2(0.060f, 0.128f);
        private static readonly Vector2 PathPaperMax = new Vector2(0.940f, 0.884f);
        private const float PathNodePadX = 70f;
        private const float PathNodePadY = 52f;

        private static readonly Color PathInk = new Color(0.16f, 0.13f, 0.10f, 1f);
        private static readonly Color PathVermilion = new Color(0.62f, 0.16f, 0.17f, 1f);

        private RectTransform _pathPaper;
        private RawImage _pathPaperWash;
        private Text _pathEyebrow;
        private Text _pathTitle;
        private Text _pathRouteLine;
        private GameObject _pathLegend;
        private Button _pathSaveQuitButton;
        private Button _pathOptionsButton;

        private static string PathBackgroundName(int floor) => floor switch
        {
            1 => "bg_koi_garden",
            2 => "bg_koi_terrace",
            3 => "bg_shrine_garden",
            4 => "bg_demon_summit",
            _ => "bg_garden_grove",
        };

        /// <summary>
        /// Node centre inside the scrolling content, inset so rings, labels and the seal stay on the paper; rounded to
        /// whole pixels so the node labels render sharp.
        /// </summary>
        private static Vector2 PathNodePos(RunNode node, float w, float h) => new Vector2(
            node.Type == NodeType.Boss
                ? Mathf.Round(w - PathBossInsetX)   // the seal sits at the end of the scroll, clear of the last gates
                : Mathf.Round(PathNodePadX + node.CanvasX * Mathf.Max(1f, w - 2f * PathNodePadX - PathBossInsetX * 0.6f)),
            Mathf.Round(PathNodePadY + (1f - node.CanvasY) * Mathf.Max(1f, h - 2f * PathNodePadY)));

        private const float PathBossInsetX = 96f;

        private void EnsurePathOverlayRoot()
        {
            if (_pathPanel == null) return;
            var panelRt = _pathPanel.GetComponent<RectTransform>();
            if (_pathOverlayRoot != null) return;

            // The floor's painting, blurred, fills the whole screen behind the scroll (Prev is used for the cross-fade).
            _floorBgPrev = ScrollUiKit.Bleed(panelRt, "FloorBackgroundPrev").gameObject.AddComponent<RawImage>();
            _floorBgPrev.color = new Color(1f, 1f, 1f, 0f);
            _floorBgPrev.raycastTarget = false;
            _floorBgRaw = ScrollUiKit.Bleed(panelRt, "FloorBackground").gameObject.AddComponent<RawImage>();
            _floorBgRaw.color = new Color(1f, 1f, 1f, 0.92f);
            _floorBgRaw.raycastTarget = false;
            _bgScrimImg = ScrollUiKit.Bleed(panelRt, "BackgroundScrim").gameObject.AddComponent<Image>();
            _bgScrimImg.color = new Color(0f, 0f, 0f, 0.55f);
            _bgScrimImg.raycastTarget = false;

            _pathPaper = ScrollUiKit.HandScroll(panelRt, "PathScroll", PathPaperMin, PathPaperMax, PathBackgroundName(0), true);
            _pathPaperWash = _pathPaper.Find("PaperWash")?.GetComponent<RawImage>();

            _pathEyebrow = ScrollUiKit.Label(_pathPaper, "PathEyebrow", string.Empty, 19, TextAnchor.MiddleLeft,
                ScrollUiKit.InkSoft, new Vector2(0.066f, 0.900f), new Vector2(0.60f, 0.935f), 13);
            _pathTitle = ScrollUiKit.Label(_pathPaper, "PathTitle", string.Empty, 44, TextAnchor.MiddleLeft,
                ScrollUiKit.Ink, new Vector2(0.066f, 0.825f), new Vector2(0.60f, 0.905f), 26);
            _pathRouteLine = ScrollUiKit.Label(_pathPaper, "PathRouteLine", string.Empty, 20, TextAnchor.MiddleLeft,
                ScrollUiKit.Ink, new Vector2(0.066f, 0.055f), new Vector2(0.60f, 0.105f), 13);
            _pathRouteLine.supportRichText = true;

            BuildPathStatsTag(panelRt);

            // The route scrolls sideways inside the paper when a floor is wider than the scroll.
            var scrollRt = ScrollUiKit.Area(_pathPaper, "PathScrollArea", new Vector2(0.02f, 0.11f), new Vector2(0.98f, 0.815f));
            _pathScrollRect = scrollRt.gameObject.AddComponent<ScrollRect>();
            _pathScrollRect.horizontal = true;
            _pathScrollRect.vertical = false;
            _pathScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _pathScrollRect.scrollSensitivity = 20f;
            var viewportGo = new GameObject("PathViewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollRt, false);
            _pathViewport = (RectTransform)viewportGo.transform;
            InRunUiFactory.StretchFill(_pathViewport);
            _pathScrollRect.viewport = _pathViewport;
            var contentGo = new GameObject("PathOverlay", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            _pathOverlayRoot = (RectTransform)contentGo.transform;
            _pathOverlayRoot.anchorMin = Vector2.zero;
            _pathOverlayRoot.anchorMax = new Vector2(0f, 1f);
            _pathOverlayRoot.pivot = Vector2.zero;
            _pathOverlayRoot.offsetMin = _pathOverlayRoot.offsetMax = Vector2.zero;
            _pathScrollRect.content = _pathOverlayRoot;
            contentGo.AddComponent<CanvasGroup>(); // used for the floor transition fade-in
            contentGo.AddComponent<Image>().color = Color.clear; // a drag on empty route space scrolls the route
            ScrollUiKit.SnapToPixels(contentGo, takesClicks: true); // crisp node labels; the nodes take clicks

            EnsureNodeTooltip();
        }

        // The run's numbers on a paper tag hanging over the scroll's top-right corner.
        private void BuildPathStatsTag(RectTransform panelRt)
        {
            var tag = ScrollUiKit.Fill(panelRt, "PathStatsBar", new Vector2(0.802f, 0.893f), new Vector2(0.998f, 1.000f),
                ScrollUiKit.Paper);
            tag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 1.2f);
            var shadow = tag.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(3f, -8f);
            var cord = ScrollUiKit.Fill(tag.transform, "Cord", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), ScrollUiKit.Gold);
            cord.rectTransform.offsetMin = new Vector2(-1f, 0f);
            cord.rectTransform.offsetMax = new Vector2(1f, 26f);

            _pathStatsBar = ScrollUiKit.Label(tag.transform, "PathStatsText", string.Empty, 19, TextAnchor.UpperLeft,
                ScrollUiKit.Ink, new Vector2(0.06f, 0.08f), new Vector2(0.76f, 0.92f), 12);
            _pathStatsBar.supportRichText = true;
            _pathStatsBar.lineSpacing = 1.05f;

            _goldIcon = ScrollUiKit.Icon(tag.transform, "GoldIcon", "economy/icon_sakura_coin",
                new Vector2(0.76f, 0.10f), new Vector2(0.86f, 0.40f), Color.white);
            _goldLabel = ScrollUiKit.Label(tag.transform, "GoldLabel", "0", 20, TextAnchor.MiddleLeft,
                PathVermilion, new Vector2(0.86f, 0.10f), new Vector2(0.99f, 0.40f), 12);
        }

        private void RefreshPathOverview()
        {
            var run = _map?.Run;
            if (run?.State == null || run.CurrentFloorGraph == null) return;
            var s = run.State;

            if (_pathStatsBar != null)
            {
                var sb = new StringBuilder();
                var classDef = ClassCatalog.GetDefinition(s.ClassId);
                sb.Append(LocalizationService.Format("InRun.Path.Tag.Class", "{0} · Level {1}",
                    LocalizationService.T(classDef?.Name ?? s.ClassId.ToString()), s.ClassLevel));
                var hpFrac = s.MaxHP > 0 ? (float)s.CurrentHP / s.MaxHP : 1f;
                var hpColor = hpFrac > 0.50f ? "#7d1f1f" : hpFrac > 0.25f ? "#b0651c" : "#c0281f";
                sb.Append('\n').Append(LocalizationService.Format("InRun.Path.Tag.Vitals",
                    "HP <color={0}>{1} / {2}</color>   Pencil <color=#7d1f1f>{3} / {4}</color>",
                    hpColor, s.CurrentHP, s.MaxHP, s.CurrentPencil, s.MaxPencil));
                var third = new StringBuilder();
                if (s.HasRelic && (s.HeldRelics?.Count ?? 0) > 0)
                    third.Append(LocalizationService.Format("InRun.Path.Tag.Relics", "Relics {0}", s.HeldRelics.Count));
                var floorEffect = _map?.GetPositiveFloorEffectLabel();
                if (!string.IsNullOrEmpty(floorEffect))
                {
                    var parenIdx = floorEffect.IndexOf(" (", System.StringComparison.Ordinal);
                    if (third.Length > 0) third.Append("   ");
                    third.Append(parenIdx > 0 ? floorEffect.Substring(0, parenIdx) : floorEffect);
                }
                if (third.Length > 0) sb.Append('\n').Append(third);
                _pathStatsBar.text = sb.ToString();
            }
            if (_goldLabel != null) _goldLabel.text = s.CurrentGold.ToString();

            // First pick: a vermilion note next to the floor title (P-1).
            if (_firstPickBand != null) Destroy(_firstPickBand);
            _firstPickBand = null;
            var isFirstPick = s.NodePath == null || s.NodePath.Count == 0;
            if (isFirstPick && _pathPaper != null)
            {
                var hint = ScrollUiKit.Label(_pathPaper, "FirstPickBand", T("InRun.Path.FirstPickHint"), 20,
                    TextAnchor.MiddleLeft, PathVermilion, new Vector2(0.066f, 0.785f), new Vector2(0.70f, 0.828f), 13,
                    FontStyle.Italic);
                _firstPickBand = hint.gameObject;
            }
            SlipFirstPickNote(_firstPickBand != null ? _firstPickBand.GetComponent<Text>() : null);

            RebuildPathCanvas(run, FloorThemeData.GetFloorTint(s.CurrentFloor));
        }

        // Per floor: the painting behind the scroll (cross-faded on a floor change): sharp with the washi window
        // (UI-STYLE-022), blurred with the opaque paper of High Contrast, which also shows it as a faint wash.
        private void ApplyPathFloorArt(int floor, bool isFloorAdvance)
        {
            var painting = Resources.Load<Texture2D>("background/" + PathBackgroundName(floor));
            var blurred = PathWashiOn ? painting : ScrollUiKit.BlurredCopy(painting);
            if (_floorBgRaw != null)
            {
                if (isFloorAdvance && blurred != null && _floorBgPrev != null && _floorBgRaw.texture != null)
                {
                    _floorBgPrev.texture = _floorBgRaw.texture;
                    _floorBgPrev.color = new Color(1f, 1f, 1f, 0.92f);
                    _floorBgRaw.texture = blurred;
                    _floorBgRaw.color = new Color(1f, 1f, 1f, 0f);
                    StartCoroutine(CrossFadeFloorBackground(0.6f));
                }
                else
                {
                    _floorBgRaw.texture = blurred;
                    _floorBgRaw.color = new Color(1f, 1f, 1f, blurred != null ? 0.92f : 0f);
                }
            }
            if (_pathPaperWash != null && painting != null) _pathPaperWash.texture = painting;
        }

        // Title, eyebrow and route names for the floor; the legend and the round buttons along the edges.
        private void RebuildPathChrome(RunDirector run, string calmName, string riskName)
        {
            var s = run.State;
            if (_pathEyebrow != null)
                _pathEyebrow.text = LocalizationService.Format("InRun.Path.Eyebrow", "Floor {0} of {1}",
                    s.CurrentFloor + 1, s.TotalFloors).ToUpperInvariant();
            if (_pathTitle != null) _pathTitle.text = FloorThemeData.GetFloorName(s.CurrentFloor);
            if (_pathRouteLine != null)
                _pathRouteLine.text = LocalizationService.Format("InRun.Path.RouteLine",
                    "Ink: <i>{0}</i>, calm  ·  <color=#9e2a2b>Vermilion: <i>{1}</i>, risk, more gold</color>",
                    calmName, riskName);
            RebuildFloorProgressStrip(run);
            RebuildPathLegend(run);
            ApplyPathWashi();
        }

        // Floor marks on the paper: ink = done, vermilion = this floor, an outline = still to come.
        private void RebuildFloorProgressStrip(RunDirector run)
        {
            if (_pathPaper == null || run?.State == null) return;
            var currentFloor = run.State.CurrentFloor;
            if (currentFloor == _lastBuiltFloorForStrip && _floorProgressStrip != null) return;
            if (_floorProgressStrip != null) Destroy(_floorProgressStrip.gameObject);

            _floorProgressStrip = ScrollUiKit.Area(_pathPaper, "FloorProgressBar", new Vector2(0.775f, 0.870f),
                new Vector2(0.934f, 0.918f));
            ScrollUiKit.Label(_floorProgressStrip, "FloorWord", T("InRun.Path.FloorMarks"), 16, TextAnchor.MiddleRight,
                ScrollUiKit.InkSoft, new Vector2(0f, 0f), new Vector2(0.24f, 1f), 11);
            var total = Mathf.Max(1, run.State.TotalFloors);
            for (var fi = 0; fi < total; fi++)
            {
                var center = new Vector2(0.32f + fi * (0.66f / Mathf.Max(1, total - 1)) * 0.98f, 0.5f);
                var mark = ScrollUiKit.Spot(_floorProgressStrip, $"FloorDot_{fi}", center, new Vector2(26f, 26f),
                    fi < currentFloor ? PathInk : fi == currentFloor ? PathVermilion : Color.clear, ScrollUiKit.Rounded(4f));
                if (fi > currentFloor)
                    ScrollUiKit.InkFrame(mark.transform, PathVermilion, 2f);
            }
            _lastBuiltFloorForStrip = currentFloor;
        }

        // Legend icons in the bottom line; Save & Quit (two-step) and Options as round buttons at the top left.
        private void RebuildPathLegend(RunDirector run)
        {
            if (_pathPanel == null || _pathPaper == null) return;
            DestroyNow(_pathLegend);
            var legend = ScrollUiKit.Area(_pathPaper, "PathLegend", new Vector2(0.60f, 0.050f), new Vector2(0.94f, 0.110f));
            _pathLegend = legend.gameObject;
            var entries = new (string Path, string LabelKey, string ObjectName)[]
            {
                ("node/icon_puzzle_tile",     "InRun.Path.Node.Puzzle", "Puzzle"),
                ("node/icon_elite_mask",      "InRun.Path.Node.Elite",  "Elite"),
                ("node/icon_market_stall",    "InRun.Path.Node.Shop",   "Shop"),
                ("node/icon_campfire_stones", "InRun.Path.Node.Rest",   "Rest"),
                ("node/icon_relic_pedestal",  "InRun.Path.Node.Relic",  "Relic"),
                ("node/icon_moss_trap",       "InRun.Path.Node.Cursed", "Cursed"),
            };
            for (var i = 0; i < entries.Length; i++)
            {
                var (path, key, objectName) = entries[i];
                var x0 = i / (float)entries.Length;
                var entry = ScrollUiKit.Area(legend, $"LegendEntry_{objectName}", new Vector2(x0, 0f),
                    new Vector2(x0 + 1f / entries.Length, 1f));
                ScrollUiKit.Icon(entry, "Icon", path, new Vector2(0f, 0.1f), new Vector2(0.32f, 0.9f), new Color(1f, 0.96f, 0.9f, 1f));
                ScrollUiKit.Label(entry, "Label", T(key), 15, TextAnchor.MiddleLeft, ScrollUiKit.InkSoft,
                    new Vector2(0.34f, 0f), new Vector2(1f, 1f), 10);
            }

            DestroyNow(_pathSaveQuitButton != null ? _pathSaveQuitButton.gameObject : null);
            DestroyNow(_pathOptionsButton != null ? _pathOptionsButton.gameObject : null);
            if (_saveQuitResetCo != null) { StopCoroutine(_saveQuitResetCo); _saveQuitResetCo = null; }
            _saveQuitPending = false;

            var panelRt = (RectTransform)_pathPanel.transform;
            var isTutorialQuit = run?.State?.TutorialMode ?? false;
            var saveQuit = ScrollUiKit.RoundButton(panelRt, "BtnSaveQuit", new Vector2(0.008f, 0.982f), 56f, "ui/icon_ink_save");
            _saveQuitImg = (Image)saveQuit.targetGraphic;
            _saveQuitLbl = ScrollUiKit.Label(saveQuit.transform, "Label",
                isTutorialQuit ? T("InRun.Path.Quit") : T("InRun.Path.SaveAndQuit"), 15, TextAnchor.UpperCenter,
                new Color(0.95f, 0.91f, 0.82f, 1f), new Vector2(-0.6f, -0.62f), new Vector2(1.6f, -0.05f), 10);
            if (isTutorialQuit)
            {
                saveQuit.onClick.AddListener(SaveAndQuit); // Custom Puzzle: one click, no confirm step
                _saveQuitImg = null;
                _saveQuitLbl = null;
            }
            else
            {
                saveQuit.onClick.AddListener(() =>
                {
                    if (!_saveQuitPending)
                    {
                        _saveQuitPending = true;
                        if (_saveQuitLbl != null) _saveQuitLbl.text = T("InRun.Path.ConfirmQuit");
                        if (_saveQuitImg != null) _saveQuitImg.color = new Color(0.86f, 0.42f, 0.36f, 1f);
                        if (_saveQuitResetCo != null) StopCoroutine(_saveQuitResetCo);
                        _saveQuitResetCo = StartCoroutine(ResetSaveQuitConfirm(3f));
                    }
                    else
                    {
                        if (_saveQuitResetCo != null) { StopCoroutine(_saveQuitResetCo); _saveQuitResetCo = null; }
                        _saveQuitPending = false;
                        SaveAndQuit();
                    }
                });
            }

            var options = ScrollUiKit.RoundButton(panelRt, "BtnPathOptions", new Vector2(0.048f, 0.982f), 56f, "ui/icon_stone_gear");
            ScrollUiKit.Label(options.transform, "Label", T("Options"), 15, TextAnchor.UpperCenter,
                new Color(0.95f, 0.91f, 0.82f, 1f), new Vector2(-0.6f, -0.62f), new Vector2(1.6f, -0.05f), 10);
            options.onClick.AddListener(ToggleOptionsPanel);
            // Automatic navigation: arrow keys lead from the buttons back down to the route.
            saveQuit.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            options.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            _pathSaveQuitButton = saveQuit;
            _pathOptionsButton = options;
        }

        // Destroy is deferred to the end of the frame; hide and rename first so lookups never find the old object.
        private static void DestroyNow(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            go.name += "_removed";
            Destroy(go);
        }

        // ── nodes and routes ──

        private Button CreateNodeButton(RectTransform parent, RunNode node, Vector2 pos, Color color,
            LevelConfig config = null, Color floorTint = default, bool bridgeWasUsed = false, bool hidden = false)
        {
            var isBoss = node.Type == NodeType.Boss;
            var size = isBoss ? 124f : node.Type == NodeType.ElitePuzzle || node.Type == NodeType.PreBoss ? 88f : 80f;
            var go = new GameObject($"Node_{node.Type}_{node.Index}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            var hit = go.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f); // invisible hit area; the glow below shows focus

            // Focus glow: gold behind the node when hovered or selected by keyboard or controller.
            var glow = ScrollUiKit.Fill(go.transform, "FocusGlow", new Vector2(-0.35f, -0.35f), new Vector2(1.35f, 1.35f),
                Color.white, ScrollUiKit.Glow());
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = glow;
            btn.colors = new ColorBlock
            {
                normalColor = new Color(1f, 0.82f, 0.45f, 0f),
                highlightedColor = new Color(1f, 0.82f, 0.45f, 0.75f),
                selectedColor = new Color(1f, 0.80f, 0.40f, 0.95f),
                pressedColor = new Color(1f, 0.72f, 0.30f, 1f),
                disabledColor = new Color(1f, 1f, 1f, 0f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };

            var risk = node.Route == RouteType.RiskRoute || node.Type == NodeType.Start;
            var routeColor = risk ? PathVermilion : PathInk;
            var faded = node.Visited ? 0.38f : 1f;
            string label;
            if (isBoss)
            {
                ScrollUiKit.Seal(go.transform, "BossSeal", new Vector2(0.5f, 0.5f), size,
                    "boss/icon_" + FloorThemeData.GetBossKey(node.Floor));
                label = FloorThemeData.GetBossName(node.Floor);
            }
            else
            {
                ScrollUiKit.Fill(go.transform, "PaperDisc", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f),
                    ScrollUiKit.Paper, ScrollUiKit.Disc());
                var ring = ScrollUiKit.Fill(go.transform, "Ring", Vector2.zero, Vector2.one,
                    new Color(routeColor.r, routeColor.g, routeColor.b, faded), ScrollUiKit.Enso());
                ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (node.Index * 47) % 360);
                var iconPath = hidden ? null : GetNodeIconPath(node.Type);
                if (!string.IsNullOrEmpty(iconPath))
                    ScrollUiKit.Icon(go.transform, "Icon", iconPath, new Vector2(0.20f, 0.20f), new Vector2(0.80f, 0.80f),
                        new Color(1f, 1f, 1f, node.Visited ? 0.45f : 1f));
                // Name and board size on the first line, the stars on a second, smaller line: busy floors stay readable.
                label = hidden ? "?" : GetNodeLabelShort(node);
                if (config != null && IsPuzzleNodeType(node.Type))
                    label += " " + Tf("InRun.Path.SizeLabel", config.BoardSize)
                        + (node.Visited ? string.Empty : "\n<size=15>" + new string('★', Mathf.Clamp(config.Stars, 1, 6)) + "</size>");
            }
            if (node.Index == _map?.Run?.State?.CurrentNodeIndex && node.Type == NodeType.Start)
                label = T("InRun.Path.YouAreHere");

            // The label sits under the node. A bridge tile on the calm (upper) route has the dotted bridge below it,
            // so its label goes above. Fixed even sizes keep the glyphs on whole pixels.
            var above = node.IsCrossLink && node.Route == RouteType.CalmRoute;
            var lbl = ScrollUiKit.Label(go.transform, "Label", label, isBoss ? 22 : 19,
                above ? TextAnchor.LowerCenter : TextAnchor.UpperCenter,
                isBoss ? PathVermilion : risk ? new Color(PathVermilion.r, PathVermilion.g, PathVermilion.b, faded)
                    : new Color(PathInk.r, PathInk.g, PathInk.b, faded),
                Vector2.zero, Vector2.zero, 12);
            lbl.supportRichText = true;
            lbl.lineSpacing = 0.9f;
            var lrt = lbl.rectTransform;
            lrt.anchorMin = lrt.anchorMax = above ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            lrt.pivot = above ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 1f);
            lrt.sizeDelta = new Vector2(220f, 52f);
            lrt.anchoredPosition = above ? new Vector2(0f, 2f) : new Vector2(0f, -2f);
            // Fixed size, no best fit: best fit measured the box before it got its size and rendered the glyphs too small.
            lbl.resizeTextForBestFit = false;
            lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            lbl.verticalOverflow = VerticalWrapMode.Overflow;
            PillUnderLabel(lbl, above);   // UI-STYLE-022: readable over the painting

            if (node.IsCrossLink && bridgeWasUsed)
                ScrollUiKit.Fill(go.transform, "CrossBadge", new Vector2(0.72f, 0.72f), new Vector2(1.06f, 1.06f),
                    PathInk, ScrollUiKit.SwapArrows());
            return btn;
        }

        // Calm = ink, risk = vermilion and a little wider; walked stretches are drawn a touch stronger.
        private static Color PathEdgeColor(bool calm, bool walked) =>
            calm ? new Color(PathInk.r, PathInk.g, PathInk.b, walked ? 0.95f : 0.80f)
                 : new Color(PathVermilion.r, PathVermilion.g, PathVermilion.b, walked ? 0.98f : 0.85f);

        private void DrawEdge(RectTransform parent, float w, float h, RunNode a, RunNode b, Color color, float thickness = 3f)
        {
            var pa = PathNodePos(a, w, h);
            var d = PathNodePos(b, w, h) - pa;
            var dist = d.magnitude;
            if (dist < 1f) return;
            var img = ScrollUiKit.Fill(parent, "Edge", Vector2.zero, Vector2.zero, color, ScrollUiKit.Stroke());
            var r = img.rectTransform;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(dist, thickness);
            r.anchoredPosition = pa + d * 0.5f;
            r.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        // The bridge between the two routes: an ink dotted line.
        private void DrawDottedEdge(RectTransform parent, float w, float h, RunNode a, RunNode b, Color color)
        {
            var pa = PathNodePos(a, w, h);
            var d = PathNodePos(b, w, h) - pa;
            var dist = d.magnitude;
            if (dist < 1f) return;
            var dir = d / dist;
            const float dot = 5f;
            const float step = 13f;
            for (var k = step * 0.5f; k < dist; k += step)
            {
                var img = ScrollUiKit.Fill(parent, "EdgeDot", Vector2.zero, Vector2.zero, color, ScrollUiKit.Disc());
                img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                img.rectTransform.sizeDelta = new Vector2(dot, dot);
                img.rectTransform.anchoredPosition = pa + dir * k;
            }
        }
    }
}
