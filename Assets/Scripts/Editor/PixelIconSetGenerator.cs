#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SudokuRoguelike.EditorTools
{
    /// <summary>
    /// Procedural 64×64 pixel-art icon generator.
    /// Run: Tools > Run of the Nine > Generate Pixel Icon Set
    /// </summary>
    public static class PixelIconSetGenerator
    {
        private const int S = 64;

        // ── Palette ──────────────────────────────────────────────
        private static readonly Color BgDark      = new(0.10f, 0.16f, 0.12f, 1f);
        private static readonly Color CreamWhite  = new(0.92f, 0.88f, 0.78f, 1f);
        private static readonly Color CalmGreen   = new(0.30f, 0.60f, 0.36f, 1f);
        private static readonly Color DarkGreen   = new(0.16f, 0.38f, 0.22f, 1f);
        private static readonly Color MossGreen   = new(0.26f, 0.46f, 0.24f, 1f);
        private static readonly Color JadeGreen   = new(0.25f, 0.72f, 0.55f, 1f);
        private static readonly Color StoneGray   = new(0.52f, 0.55f, 0.56f, 1f);
        private static readonly Color LightGray   = new(0.72f, 0.75f, 0.76f, 1f);
        private static readonly Color LanternGold = new(0.86f, 0.72f, 0.30f, 1f);
        private static readonly Color BrightGold  = new(0.96f, 0.86f, 0.22f, 1f);
        private static readonly Color KoiOrange   = new(0.90f, 0.48f, 0.16f, 1f);
        private static readonly Color SoftRed     = new(0.78f, 0.24f, 0.22f, 1f);
        private static readonly Color CrimsonRed  = new(0.82f, 0.12f, 0.16f, 1f);
        private static readonly Color PastelBlue  = new(0.40f, 0.70f, 0.90f, 1f);
        private static readonly Color DeepBlue    = new(0.20f, 0.45f, 0.78f, 1f);
        private static readonly Color WoodBrown   = new(0.52f, 0.36f, 0.18f, 1f);
        private static readonly Color CopperCol   = new(0.72f, 0.46f, 0.20f, 1f);
        private static readonly Color InkBlack    = new(0.08f, 0.08f, 0.10f, 1f);
        private static readonly Color SilkPink    = new(0.92f, 0.72f, 0.80f, 1f);
        private static readonly Color PorcelainW  = new(0.96f, 0.94f, 0.90f, 1f);
        private static readonly Color VioletPurp  = new(0.64f, 0.28f, 0.86f, 1f);
        private static readonly Color TealCyan    = new(0.18f, 0.82f, 0.72f, 1f);

        private static Color BorderFor(string cat) => cat switch
        {
            "relic_t1"     => StoneGray,
            "relic_t2"     => CalmGreen,
            "relic_t3"     => PastelBlue,
            "relic_t4"     => LanternGold,
            "relic_legend" => BrightGold,
            "item_normal"  => StoneGray,
            "item_rare"    => CalmGreen,
            "item_epic"    => VioletPurp,
            "class"        => CalmGreen,
            "ui"           => LanternGold,
            "modifier"     => PastelBlue,
            "boss"         => SoftRed,
            _              => StoneGray,
        };

        // ── Entry ────────────────────────────────────────────────

        private sealed class IE
        {
            public string Id;   // snake_case, maps to icon_<id>.png
            public string Cat;  // category
            public IE(string id, string cat) { Id = id; Cat = cat; }
        }

        // ── Menu Entry ───────────────────────────────────────────

        [MenuItem("Tools/Run of the Nine/Generate Pixel Icon Set")]
        public static void GenerateIconSet()
        {
            var dir = "Assets/Resources/GeneratedIcons";
            Directory.CreateDirectory(dir);

            var entries = BuildEntries();
            foreach (var e in entries)
            {
                var tex = DrawIcon(e);
                File.WriteAllBytes(Path.Combine(dir, "icon_" + e.Id + ".png"), tex.EncodeToPNG());
            }

            AssetDatabase.Refresh();
            ConfigureSprites(dir);
            Debug.Log($"[PixelIcons] Generated {entries.Count} icons in {dir}");
        }

        // ── Icon List ─────────────────────────────────────────────

        private static List<IE> BuildEntries() => new List<IE>
        {
            // ─ Items (tiered) ─
            new("solver",              "item_normal"),
            new("finder",              "item_normal"),
            new("ink_well",            "item_normal"),
            new("meditation_stone",    "item_normal"),
            new("wind_chime",          "item_normal"),
            new("pattern_scroll",      "item_normal"),
            new("koi_reflection",      "item_normal"),
            new("lantern_of_clarity",  "item_normal"),
            // ─ Items (unique normal) ─
            new("garden_rake",         "item_normal"),
            new("offering_bowl",       "item_normal"),
            new("pruning_shears",      "item_normal"),
            new("zen_sand_sifter",     "item_normal"),
            // ─ Items (unique rare) ─
            new("ginkgo_leaf",         "item_rare"),
            new("rice_paper_umbrella", "item_rare"),
            new("temple_incense",      "item_rare"),
            // ─ Items (unique epic) ─
            new("koi_dragon_scale",    "item_epic"),
            new("golden_kintsugi_jar", "item_epic"),
            new("silk_fan",            "item_epic"),

            // ─ Relics T1 ─
            new("smooth_pebble",        "relic_t1"),
            new("cracked_teacup",       "relic_t1"),
            new("wooden_comb",          "relic_t1"),
            new("moss_token",           "relic_t1"),
            // ─ Relics T2 ─
            new("koi_reflection_relic", "relic_t2"),
            new("monk_charm",           "relic_t2"),
            new("copper_tortoise",      "relic_t2"),
            new("jade_hairpin",         "relic_t2"),
            new("stone_sundial",        "relic_t2"),
            // ─ Relics T3 ─
            new("sakura_seal",          "relic_t3"),
            new("crimson_fan",          "relic_t3"),
            new("wisteria_branch",      "relic_t3"),
            new("paper_crane",          "relic_t3"),
            new("porcelain_mask",       "relic_t3"),
            // ─ Relics T4 ─
            new("transmuted_sigil",     "relic_t4"),
            new("phoenix_feather",      "relic_t4"),
            new("spirit_lantern",       "relic_t4"),
            new("moonstone_compass",    "relic_t4"),
            // ─ Relics Legendary ─
            new("golden_root",          "relic_legend"),
            new("silent_grid",          "relic_legend"),
            new("shifting_garden",      "relic_legend"),
            new("eternal_lotus",        "relic_legend"),
            new("dragons_eye",          "relic_legend"),

            // ─ Classes ─
            new("spin_coin",            "class"),
            new("enlightenment_tree",   "class"),
            new("scroll_graph",         "class"),
            new("golden_koi",           "class"),
            new("moss_stone",           "class"),
            new("garden_lantern",       "class"),
            new("bamboo_scroll",        "class"),
            new("compass_of_order",     "class"),

            // ─ UI ─
            new("bud",                  "ui"),
            new("golden_bloom",         "ui"),
            new("triple_chest",         "ui"),
            new("stone_gear",           "ui"),
            new("language_scroll",      "ui"),
            new("torii_lock",           "ui"),
            new("infinite_lotus",       "ui"),
            new("temple_seal",          "ui"),

            // ─ Modifiers ─
            new("fog_cloud",            "modifier"),
            new("arrow_sum",            "modifier"),
            new("german_whisper",       "modifier"),
            new("dutch_whisper",        "modifier"),
            new("parity_line",          "modifier"),
            new("renban_chain",         "modifier"),
            new("killer_cage",          "modifier"),
            new("white_dot",            "modifier"),
            new("black_dot",            "modifier"),
            new("thermo_line",          "modifier"),
            new("palindrome_line",      "modifier"),
            new("between_line",         "modifier"),
            new("even_odd",             "modifier"),
            new("nonconsecutive",       "modifier"),
            new("antiknight",           "modifier"),

            // ─ Nodes / Boss ─
            new("demon_mask",           "boss"),
            new("elite_mask",           "node"),
            new("market_stall",         "node"),
            new("campfire_stones",      "node"),
            new("relic_pedestal",       "node"),
            new("stone_altar",          "node"),
        };

        // ════════════════════════════════════════════════════════
        // Draw Dispatch
        // ════════════════════════════════════════════════════════

        private static Texture2D DrawIcon(IE e)
        {
            var t = MakeTex(BgDark);
            DrawBorder(t, BorderFor(e.Cat), 3);

            switch (e.Id)
            {
                // ── Items ──────────────────────────────────────
                case "solver":
                    // Bold checkmark
                    DrawLine(t, 12, 28, 26, 46, 4, CalmGreen);
                    DrawLine(t, 24, 46, 52, 16, 4, CalmGreen);
                    DrawDot(t, 32, 32, 5, new Color(0.4f,0.8f,0.5f,0.35f));
                    break;

                case "finder":
                    // Magnifying glass
                    DrawCircleOutline(t, 28, 34, 14, 3, CalmGreen);
                    DrawLine(t, 37, 25, 53, 12, 4, LightGray);
                    DrawDot(t, 28, 34, 4, new Color(CalmGreen.r,CalmGreen.g,CalmGreen.b,0.4f));
                    break;

                case "ink_well":
                    // Jar with liquid
                    Rect(t, 18, 10, 28, 6, StoneGray);   // neck
                    Rect(t, 14, 16, 36, 32, StoneGray);  // body outline
                    Rect(t, 16, 18, 32, 28, BgDark);     // body fill (hollow)
                    Rect(t, 16, 32, 32, 14, InkBlack);   // ink fill
                    DrawLine(t, 16, 32, 48, 32, 2, PastelBlue); // ink level
                    break;

                case "meditation_stone":
                    // Smooth oval stone with concentric oval lines
                    DrawOval(t, 32, 36, 20, 14, StoneGray);
                    DrawOvalOutline(t, 32, 36, 14, 9, 1, LightGray);
                    DrawOvalOutline(t, 32, 36, 7, 4, 1, LightGray);
                    DrawDot(t, 32, 36, 2, CreamWhite);
                    break;

                case "wind_chime":
                    // 4 hanging tubes from a bar
                    Rect(t, 10, 14, 44, 4, WoodBrown);   // bar
                    for (var i = 0; i < 4; i++)
                    {
                        var x = 14 + i * 11;
                        Rect(t, x, 18, 4, 14 + i * 2, CreamWhite);
                        DrawDot(t, x + 2, 33 + i * 2, 3, LanternGold);
                    }
                    break;

                case "pattern_scroll":
                    // Rolled scroll
                    DrawOval(t, 12, 32, 8, 20, WoodBrown);
                    DrawOval(t, 52, 32, 8, 20, WoodBrown);
                    Rect(t, 12, 14, 40, 36, CreamWhite);
                    // Lines on scroll
                    for (var y = 20; y <= 42; y += 5)
                        Rect(t, 18, y, 28, 2, StoneGray);
                    break;

                case "koi_reflection":
                    DrawKoiShape(t, KoiOrange, CalmGreen);
                    break;

                case "lantern_of_clarity":
                    DrawLanternShape(t, CalmGreen, CreamWhite);
                    break;

                case "garden_rake":
                    // Rake: handle + head + teeth
                    DrawLine(t, 32, 8, 32, 48, 3, WoodBrown);   // handle
                    Rect(t, 12, 46, 40, 5, WoodBrown);           // head
                    for (var i = 0; i < 5; i++)
                        Rect(t, 14 + i * 8, 50, 3, 7, WoodBrown); // teeth
                    break;

                case "offering_bowl":
                    // Bowl shape: two arcs + base
                    DrawArcBottom(t, 32, 36, 20, CalmGreen, 3);
                    DrawLine(t, 12, 36, 52, 36, 3, CalmGreen);  // rim
                    Rect(t, 24, 52, 16, 4, CalmGreen);           // base
                    DrawDot(t, 32, 30, 6, new Color(0.3f,0.8f,0.5f,0.3f)); // glow inside
                    break;

                case "pruning_shears":
                    // Crossed blades (scissors)
                    DrawLine(t, 14, 50, 50, 14, 5, StoneGray);
                    DrawLine(t, 50, 50, 14, 14, 5, StoneGray);
                    DrawDot(t, 32, 32, 5, WoodBrown); // pivot
                    DrawDot(t, 32, 32, 2, CreamWhite);
                    break;

                case "zen_sand_sifter":
                    // Circle with dot-grid inside
                    DrawCircleOutline(t, 32, 32, 22, 2, StoneGray);
                    for (var gy = 0; gy < 4; gy++)
                        for (var gx = 0; gx < 4; gx++)
                        {
                            var px = 18 + gx * 8;
                            var py = 18 + gy * 8;
                            var dx = px - 32; var dy = py - 32;
                            if (dx*dx+dy*dy <= 20*20)
                                DrawDot(t, px, py, 2, LightGray);
                        }
                    break;

                case "ginkgo_leaf":
                    DrawGinkgoLeafShape(t, CalmGreen, DarkGreen);
                    break;

                case "rice_paper_umbrella":
                    DrawUmbrellaShape(t, CreamWhite, WoodBrown);
                    break;

                case "temple_incense":
                    // Stick + smoke
                    Rect(t, 30, 20, 4, 36, WoodBrown);       // stick
                    DrawDot(t, 32, 56, 5, StoneGray);         // holder
                    // Smoke wisps
                    for (var i = 0; i < 5; i++)
                    {
                        var sy = 16 - i * 3;
                        var sx = 32 + (i % 2 == 0 ? -3 : 3);
                        DrawDot(t, sx, sy, 3, new Color(0.7f, 0.7f, 0.75f, 0.6f));
                    }
                    break;

                case "koi_dragon_scale":
                    // Diamond scale grid 4×4
                    for (var row = 0; row < 4; row++)
                        for (var col = 0; col < 4; col++)
                        {
                            var cx = 16 + col * 10 + (row % 2) * 5;
                            var cy = 16 + row * 10;
                            DrawDiamond(t, cx, cy, 5, 3, KoiOrange);
                            DrawDot(t, cx, cy, 1, BrightGold);
                        }
                    break;

                case "golden_kintsugi_jar":
                    // Oval jar with gold cracks
                    DrawOval(t, 32, 34, 18, 22, CreamWhite);
                    DrawOvalOutline(t, 32, 34, 18, 22, 2, BgDark);
                    // Gold crack lines
                    DrawLine(t, 32, 14, 20, 36, 1, BrightGold);
                    DrawLine(t, 32, 14, 42, 50, 1, BrightGold);
                    DrawLine(t, 20, 36, 38, 44, 1, BrightGold);
                    DrawDot(t, 32, 14, 3, BrightGold);
                    break;

                case "silk_fan":
                    DrawFanShape(t, SilkPink, BrightGold);
                    break;

                // ── Relics T1 ──────────────────────────────────

                case "smooth_pebble":
                    DrawOval(t, 32, 36, 20, 15, StoneGray);
                    DrawOvalOutline(t, 32, 36, 10, 7, 1, LightGray);
                    DrawDot(t, 32, 36, 2, CreamWhite);
                    break;

                case "cracked_teacup":
                    DrawArcBottom(t, 32, 40, 18, CreamWhite, 3);
                    DrawLine(t, 14, 40, 50, 40, 2, CreamWhite);       // rim
                    DrawLine(t, 50, 40, 52, 32, 2, CreamWhite);        // handle outer
                    DrawLine(t, 50, 48, 52, 40, 2, CreamWhite);        // handle inner
                    DrawLine(t, 52, 32, 52, 48, 2, CreamWhite);
                    // crack
                    DrawLine(t, 32, 40, 26, 52, 1, SoftRed);
                    DrawLine(t, 26, 52, 30, 58, 1, SoftRed);
                    break;

                case "wooden_comb":
                    // Base rectangle, then teeth upward
                    Rect(t, 10, 44, 44, 8, WoodBrown);
                    for (var i = 0; i < 8; i++)
                        Rect(t, 12 + i * 5, 26, 3, 18, WoodBrown);
                    break;

                case "moss_token":
                    DrawCircleOutline(t, 32, 32, 20, 3, MossGreen);
                    DrawDot(t, 32, 32, 12, new Color(MossGreen.r, MossGreen.g, MossGreen.b, 0.5f));
                    for (var i = 0; i < 6; i++)
                    {
                        var ang = i * Mathf.PI / 3f;
                        DrawDot(t, 32 + (int)(8 * Mathf.Cos(ang)), 32 + (int)(8 * Mathf.Sin(ang)), 2, CalmGreen);
                    }
                    break;

                // ── Relics T2 ──────────────────────────────────

                case "koi_reflection_relic":
                    DrawKoiShape(t, JadeGreen, CalmGreen);
                    DrawLine(t, 10, 42, 54, 42, 1, new Color(0.4f, 0.7f, 0.9f, 0.5f)); // water
                    break;

                case "monk_charm":
                    DrawCircleOutline(t, 32, 32, 20, 2, LanternGold);
                    // Inner pattern
                    for (var i = 0; i < 6; i++)
                    {
                        var ang = i * Mathf.PI / 3f;
                        DrawLine(t, 32, 32,
                            32 + (int)(14 * Mathf.Cos(ang)), 32 + (int)(14 * Mathf.Sin(ang)),
                            1, CalmGreen);
                    }
                    DrawDot(t, 32, 32, 5, LanternGold);
                    DrawDot(t, 32, 32, 2, BrightGold);
                    // Hole at top
                    DrawDot(t, 32, 12, 3, BgDark);
                    Rect(t, 30, 6, 4, 6, StoneGray);
                    break;

                case "copper_tortoise":
                    // Hexagon shell
                    DrawHexagon(t, 32, 34, 18, CopperCol, 3);
                    DrawHexagon(t, 32, 34, 10, new Color(0.6f,0.35f,0.12f,0.6f), 2);
                    DrawDot(t, 32, 34, 4, CopperCol);
                    // Head + legs
                    DrawOval(t, 32, 16, 6, 5, CopperCol);
                    DrawLine(t, 14, 28, 18, 38, 2, CopperCol);
                    DrawLine(t, 50, 28, 46, 38, 2, CopperCol);
                    break;

                case "jade_hairpin":
                    // U-shape pin
                    DrawLine(t, 20, 12, 20, 46, 4, JadeGreen);
                    DrawLine(t, 44, 12, 44, 46, 4, JadeGreen);
                    DrawArcBottom(t, 32, 46, 12, JadeGreen, 4);
                    DrawDot(t, 20, 12, 6, BrightGold); // gem top left
                    DrawDot(t, 44, 12, 6, BrightGold); // gem top right
                    DrawDot(t, 32, 58, 5, JadeGreen);  // bottom gem
                    break;

                case "stone_sundial":
                    DrawCircleOutline(t, 32, 36, 20, 3, StoneGray);
                    DrawDot(t, 32, 36, 4, LanternGold); // center
                    // Hour marks (8 rays)
                    for (var i = 0; i < 8; i++)
                    {
                        var ang = i * Mathf.PI / 4f;
                        DrawLine(t,
                            32 + (int)(10 * Mathf.Cos(ang)), 36 + (int)(10 * Mathf.Sin(ang)),
                            32 + (int)(18 * Mathf.Cos(ang)), 36 + (int)(18 * Mathf.Sin(ang)),
                            2, LanternGold);
                    }
                    // Gnomon shadow
                    DrawLine(t, 32, 36, 44, 20, 2, SoftRed);
                    break;

                // ── Relics T3 ──────────────────────────────────

                case "sakura_seal":
                    DrawSakuraShape(t, SilkPink, BrightGold);
                    break;

                case "crimson_fan":
                    DrawFanShape(t, CrimsonRed, new Color(0.95f,0.7f,0.6f,1f));
                    break;

                case "wisteria_branch":
                    // Branch + hanging clusters
                    DrawLine(t, 16, 12, 48, 28, 3, WoodBrown);
                    DrawLine(t, 32, 20, 32, 52, 2, WoodBrown);
                    DrawLine(t, 22, 16, 22, 44, 2, WoodBrown);
                    DrawLine(t, 42, 24, 42, 50, 2, WoodBrown);
                    // Grape clusters
                    for (var i = 0; i < 4; i++)
                    {
                        var bx = 22 + i * 7;
                        var by = 44 + (i % 2) * 4;
                        DrawDot(t, bx, by,     3, SilkPink);
                        DrawDot(t, bx, by + 5, 3, VioletPurp);
                        DrawDot(t, bx, by + 10, 3, SilkPink);
                    }
                    break;

                case "paper_crane":
                    DrawCraneShape(t, CreamWhite, StoneGray);
                    break;

                case "porcelain_mask":
                    DrawMaskShape(t, PorcelainW, InkBlack);
                    break;

                // ── Relics T4 ──────────────────────────────────

                case "transmuted_sigil":
                    // Star / alchemical symbol
                    DrawStar(t, 32, 32, 20, 8, LanternGold);
                    DrawCircleOutline(t, 32, 32, 14, 1, BrightGold);
                    DrawDot(t, 32, 32, 4, BrightGold);
                    break;

                case "phoenix_feather":
                    DrawPhoenixFeatherShape(t);
                    break;

                case "spirit_lantern":
                    DrawLanternShape(t, BrightGold, new Color(1f,0.95f,0.6f,1f));
                    DrawDot(t, 32, 34, 8, new Color(1f, 0.95f, 0.5f, 0.4f)); // glow
                    break;

                case "moonstone_compass":
                    DrawCompassShape(t, PastelBlue, CreamWhite);
                    break;

                // ── Relics Legendary ───────────────────────────

                case "golden_root":
                    DrawGoldenRootShape(t);
                    break;

                case "silent_grid":
                    DrawSudokuGrid(t, BrightGold);
                    break;

                case "shifting_garden":
                    DrawSpiral(t, 32, 32, BrightGold, CalmGreen);
                    break;

                case "eternal_lotus":
                    DrawLotusShape(t, BrightGold, CalmGreen);
                    break;

                case "dragons_eye":
                    DrawDragonsEyeShape(t);
                    break;

                // ── Classes ────────────────────────────────────

                case "spin_coin":
                    DrawCircleOutline(t, 32, 32, 20, 3, LanternGold);
                    DrawDot(t, 32, 32, 10, new Color(LanternGold.r,LanternGold.g,LanternGold.b,0.5f));
                    DrawLine(t, 32, 12, 32, 52, 1, BrightGold);
                    DrawLine(t, 12, 32, 52, 32, 1, BrightGold);
                    DrawDot(t, 32, 32, 4, BrightGold);
                    break;

                case "enlightenment_tree":
                    DrawTreeShape(t, DarkGreen, CalmGreen, MossGreen);
                    break;

                case "scroll_graph":
                    DrawOval(t, 12, 32, 8, 22, WoodBrown);
                    DrawOval(t, 52, 32, 8, 22, WoodBrown);
                    Rect(t, 12, 12, 40, 40, CreamWhite);
                    // Graph lines inside
                    DrawLine(t, 18, 44, 24, 30, 2, CalmGreen);
                    DrawLine(t, 24, 30, 32, 36, 2, CalmGreen);
                    DrawLine(t, 32, 36, 38, 22, 2, CalmGreen);
                    DrawLine(t, 38, 22, 44, 28, 2, CalmGreen);
                    Rect(t, 18, 44, 26, 2, StoneGray); // x axis
                    break;

                case "golden_koi":
                    DrawKoiShape(t, BrightGold, LanternGold);
                    break;

                case "moss_stone":
                    DrawOval(t, 32, 36, 22, 16, StoneGray);
                    DrawOvalOutline(t, 32, 36, 14, 10, 1, LightGray);
                    // Moss patches
                    DrawDot(t, 26, 28, 4, MossGreen);
                    DrawDot(t, 38, 30, 3, MossGreen);
                    DrawDot(t, 22, 36, 3, MossGreen);
                    DrawDot(t, 32, 26, 3, MossGreen);
                    break;

                case "garden_lantern":
                    DrawLanternShape(t, LanternGold, BrightGold);
                    break;

                case "bamboo_scroll":
                    // Bamboo segments + scroll roll
                    for (var seg = 0; seg < 4; seg++)
                        Rect(t, 14, 12 + seg * 12, 10, 10, DarkGreen);
                    DrawOval(t, 48, 32, 10, 22, WoodBrown);
                    Rect(t, 24, 14, 24, 36, CreamWhite);
                    for (var li = 0; li < 5; li++)
                        Rect(t, 26, 18 + li * 6, 20, 1, StoneGray);
                    break;

                case "compass_of_order":
                    DrawCompassShape(t, LanternGold, CreamWhite);
                    break;

                // ── UI ─────────────────────────────────────────

                case "bud":
                    DrawLine(t, 32, 54, 32, 28, 3, CalmGreen);   // stem
                    DrawOval(t, 32, 24, 10, 14, DarkGreen);       // outer petals
                    DrawDot(t, 32, 22, 7, CalmGreen);             // bud tip
                    DrawDot(t, 28, 30, 5, CalmGreen);             // left leaf
                    DrawDot(t, 36, 30, 5, CalmGreen);             // right leaf
                    break;

                case "golden_bloom":
                    DrawSakuraShape(t, BrightGold, LanternGold);
                    DrawDot(t, 32, 32, 5, BrightGold);
                    break;

                case "triple_chest":
                    // 3 overlapping chests
                    DrawChest(t, 8,  38, 18, 14, StoneGray);
                    DrawChest(t, 24, 32, 18, 14, LanternGold);
                    DrawChest(t, 40, 38, 18, 14, CalmGreen);
                    break;

                case "stone_gear":
                    DrawGearShape(t, StoneGray, LightGray);
                    break;

                case "language_scroll":
                    DrawOval(t, 12, 32, 8, 22, WoodBrown);
                    DrawOval(t, 52, 32, 8, 22, WoodBrown);
                    Rect(t, 12, 12, 40, 40, CreamWhite);
                    // Text lines
                    for (var li = 0; li < 6; li++)
                        Rect(t, 18, 18 + li * 5, 12 + (li % 2) * 8, 2, StoneGray);
                    break;

                case "torii_lock":
                    DrawToriiShape(t, SoftRed, WoodBrown);
                    break;

                case "infinite_lotus":
                    DrawLotusShape(t, CalmGreen, JadeGreen);
                    // Infinity symbol overlay
                    DrawLine(t, 22, 32, 28, 26, 2, BrightGold);
                    DrawLine(t, 28, 26, 36, 38, 2, BrightGold);
                    DrawLine(t, 36, 38, 42, 32, 2, BrightGold);
                    DrawLine(t, 22, 32, 28, 38, 2, BrightGold);
                    DrawLine(t, 28, 38, 36, 26, 2, BrightGold);
                    DrawLine(t, 36, 26, 42, 32, 2, BrightGold);
                    break;

                case "temple_seal":
                    DrawSakuraShape(t, CreamWhite, StoneGray);
                    DrawCircleOutline(t, 32, 32, 20, 2, StoneGray);
                    break;

                // ── Modifiers ──────────────────────────────────

                case "fog_cloud":
                    DrawDot(t, 26, 36, 12, new Color(0.7f,0.8f,0.85f,0.9f));
                    DrawDot(t, 38, 34, 10, new Color(0.75f,0.82f,0.88f,0.9f));
                    DrawDot(t, 32, 30, 10, new Color(0.8f,0.85f,0.90f,0.95f));
                    DrawDot(t, 32, 36, 8,  new Color(0.85f,0.88f,0.92f,1f));
                    break;

                case "arrow_sum":
                    DrawCircleOutline(t, 20, 32, 10, 2, CalmGreen);
                    DrawLine(t, 30, 32, 52, 32, 2, CalmGreen);
                    DrawLine(t, 52, 32, 44, 24, 2, CalmGreen);
                    DrawLine(t, 52, 32, 44, 40, 2, CalmGreen);
                    DrawLine(t, 18, 28, 22, 36, 1, BrightGold); // digit in circle
                    break;

                case "german_whisper":
                    DrawLine(t, 12, 48, 24, 28, 3, new Color(0.22f,0.72f,0.35f,1f));
                    DrawLine(t, 24, 28, 40, 42, 3, new Color(0.22f,0.72f,0.35f,1f));
                    DrawLine(t, 40, 42, 52, 16, 3, new Color(0.22f,0.72f,0.35f,1f));
                    DrawDot(t, 12, 48, 5, CreamWhite);
                    DrawDot(t, 52, 16, 5, CreamWhite);
                    break;

                case "dutch_whisper":
                    DrawLine(t, 12, 48, 24, 28, 3, TealCyan);
                    DrawLine(t, 24, 28, 40, 42, 3, TealCyan);
                    DrawLine(t, 40, 42, 52, 16, 3, TealCyan);
                    DrawDot(t, 12, 48, 5, CreamWhite);
                    DrawDot(t, 52, 16, 5, CreamWhite);
                    break;

                case "parity_line":
                    // Blue/orange alternating dots on a line
                    DrawLine(t, 12, 40, 52, 24, 2, LightGray);
                    for (var pi = 0; pi < 5; pi++)
                    {
                        var px = 12 + pi * 9;
                        var py = 40 - pi * 3;
                        DrawDot(t, px, py, 4, pi % 2 == 0 ? PastelBlue : KoiOrange);
                    }
                    break;

                case "renban_chain":
                    // Violet line — ascending consecutive sequence
                    DrawLine(t, 8, 40, 56, 22, 3, VioletPurp);
                    // 4 circles: ascending size+brightness = consecutive group
                    var renX = new[] { 12, 24, 38, 52 };
                    var renY = new[] { 39, 34, 29, 23 };
                    var renR = new[] { 5, 6, 7, 8 };
                    for (var ri = 0; ri < 4; ri++)
                    {
                        var fade = 0.55f + ri * 0.15f;
                        DrawDot(t, renX[ri], renY[ri], renR[ri],
                            new Color(0.92f * fade, 0.72f * fade, 0.80f * fade, 1f));
                        DrawDot(t, renX[ri], renY[ri], renR[ri] - 2, VioletPurp);
                        // Tick marks: 1 tick for 1st, 2 for 2nd, etc.
                        for (var ti = 0; ti <= ri; ti++)
                            Rect(t, renX[ri] - ri + ti * 2, renY[ri] - 2, 1, 4, CreamWhite);
                    }
                    break;

                case "killer_cage":
                    // Dashed cage outline
                    for (var di = 0; di < 40; di += 4)
                    {
                        if ((di / 4) % 2 == 0)
                        {
                            if (di < 40) Rect(t, 14 + di, 14, 3, 2, LanternGold);
                            if (di < 40) Rect(t, 14, 14 + di, 2, 3, LanternGold);
                            if (di < 40) Rect(t, 14 + di, 52, 3, 2, LanternGold);
                            if (di < 40) Rect(t, 52, 14 + di, 2, 3, LanternGold);
                        }
                    }
                    // Sum digit in corner
                    Rect(t, 14, 14, 14, 10, new Color(LanternGold.r,LanternGold.g,LanternGold.b,0.4f));
                    break;

                case "white_dot":
                    DrawDot(t, 32, 32, 14, CreamWhite);
                    DrawDot(t, 32, 32, 8, BgDark);
                    break;

                case "black_dot":
                    DrawDot(t, 32, 32, 14, InkBlack);
                    DrawDot(t, 32, 32, 3, new Color(0.3f,0.3f,0.35f,1f));
                    break;

                case "thermo_line":
                    DrawDot(t, 16, 44, 10, new Color(0.85f,0.30f,0.20f,1f)); // bulb
                    DrawDot(t, 16, 44, 5, new Color(1f,0.5f,0.3f,1f));
                    DrawLine(t, 16, 34, 48, 20, 6, new Color(0.70f,0.70f,0.72f,1f)); // tube
                    DrawLine(t, 16, 34, 48, 20, 4, new Color(0.85f,0.40f,0.30f,0.7f)); // heat color
                    break;

                case "palindrome_line":
                    DrawLine(t, 10, 32, 54, 32, 3, VioletPurp);
                    // Mirror dots
                    DrawDot(t, 16, 32, 6, SilkPink);
                    DrawDot(t, 48, 32, 6, SilkPink);
                    DrawDot(t, 24, 32, 5, VioletPurp);
                    DrawDot(t, 40, 32, 5, VioletPurp);
                    DrawDot(t, 32, 32, 4, CreamWhite);
                    break;

                case "between_line":
                    DrawLine(t, 10, 32, 54, 32, 2, LightGray);
                    DrawDot(t, 10, 32, 8, CreamWhite);  // large white circle L
                    DrawDot(t, 10, 32, 5, BgDark);
                    DrawDot(t, 54, 32, 8, CreamWhite);  // large white circle R
                    DrawDot(t, 54, 32, 5, BgDark);
                    // middle constraint markers
                    DrawDot(t, 24, 32, 3, LanternGold);
                    DrawDot(t, 40, 32, 3, LanternGold);
                    break;

                case "even_odd":
                    Rect(t, 12, 14, 18, 18, new Color(0.35f,0.65f,0.90f,0.7f)); // even square
                    DrawDot(t, 44, 23, 9, new Color(0.90f,0.55f,0.20f,0.7f));   // odd circle
                    Rect(t, 12, 38, 18, 12, new Color(0.35f,0.65f,0.90f,0.5f));
                    DrawDot(t, 44, 44, 7, new Color(0.90f,0.55f,0.20f,0.5f));
                    break;

                case "nonconsecutive":
                    // Grid with X marks between cells
                    Rect(t, 10, 20, 18, 18, StoneGray);
                    Rect(t, 36, 20, 18, 18, StoneGray);
                    Rect(t, 10, 42, 18, 18, StoneGray);
                    Rect(t, 36, 42, 18, 18, StoneGray);
                    DrawLine(t, 28, 24, 36, 32, 2, SoftRed);
                    DrawLine(t, 36, 24, 28, 32, 2, SoftRed);
                    break;

                case "antiknight":
                    // Chess knight move indicator
                    DrawDot(t, 20, 20, 8, new Color(0.8f,0.7f,0.5f,0.8f));
                    DrawLine(t, 20, 20, 44, 28, 2, SoftRed);  // knight L-move
                    DrawLine(t, 44, 28, 44, 44, 2, SoftRed);
                    DrawDot(t, 44, 44, 8, new Color(0.8f,0.7f,0.5f,0.8f));
                    // X over arrival
                    DrawLine(t, 38, 38, 50, 50, 2, SoftRed);
                    DrawLine(t, 50, 38, 38, 50, 2, SoftRed);
                    break;

                // ── Nodes / Boss ───────────────────────────────

                case "demon_mask":
                    DrawMaskShape(t, SoftRed, InkBlack);
                    // Horns
                    DrawLine(t, 22, 16, 18, 6,  3, SoftRed);
                    DrawLine(t, 42, 16, 46, 6,  3, SoftRed);
                    break;

                case "elite_mask":
                    DrawMaskShape(t, LanternGold, InkBlack);
                    break;

                case "market_stall":
                    // Awning + stall body
                    Rect(t, 8,  16, 48, 6,  SoftRed);    // awning strip
                    Rect(t, 8,  22, 48, 28, CreamWhite); // body
                    Rect(t, 12, 26, 12, 8,  StoneGray);  // item 1
                    Rect(t, 28, 26, 12, 8,  LanternGold);// item 2
                    DrawLine(t, 8, 16, 8, 50, 3, WoodBrown);
                    DrawLine(t, 56, 16, 56, 50, 3, WoodBrown);
                    break;

                case "campfire_stones":
                    // Stones + flame
                    DrawDot(t, 22, 44, 8, StoneGray);
                    DrawDot(t, 42, 44, 8, StoneGray);
                    DrawDot(t, 32, 46, 8, LightGray);
                    // Flames
                    DrawDot(t, 32, 34, 8, KoiOrange);
                    DrawDot(t, 28, 28, 5, SoftRed);
                    DrawDot(t, 36, 30, 5, LanternGold);
                    DrawDot(t, 32, 24, 4, BrightGold);
                    break;

                case "relic_pedestal":
                    // Column + relic on top
                    Rect(t, 24, 38, 16, 18, StoneGray);  // column
                    Rect(t, 18, 36, 28, 4, LightGray);   // capital
                    DrawDot(t, 32, 26, 10, LanternGold); // relic glow
                    DrawDot(t, 32, 26, 6, BrightGold);
                    DrawDot(t, 32, 26, 3, CreamWhite);
                    break;

                case "stone_altar":
                    Rect(t, 10, 44, 44, 8, StoneGray);    // base
                    Rect(t, 16, 32, 32, 12, LightGray);   // top slab
                    Rect(t, 20, 28, 24, 4, StoneGray);    // stone block
                    // Rune marks
                    DrawLine(t, 24, 36, 24, 42, 1, new Color(0.4f,0.6f,0.9f,0.8f));
                    DrawLine(t, 32, 34, 32, 42, 1, new Color(0.4f,0.6f,0.9f,0.8f));
                    DrawLine(t, 40, 36, 40, 42, 1, new Color(0.4f,0.6f,0.9f,0.8f));
                    break;

                default:
                    // Generic fallback
                    DrawDot(t, 32, 32, 14, CalmGreen);
                    DrawDot(t, 32, 32, 6, BrightGold);
                    break;
            }

            t.Apply();
            return t;
        }

        // ════════════════════════════════════════════════════════
        // Reusable Shape Drawers
        // ════════════════════════════════════════════════════════

        private static void DrawKoiShape(Texture2D t, Color body, Color fin)
        {
            // Body: oval
            DrawOval(t, 30, 34, 14, 20, body);
            // Head
            DrawDot(t, 30, 16, 8, body);
            DrawDot(t, 33, 14, 3, CreamWhite); // eye
            DrawDot(t, 33, 14, 1, InkBlack);
            // Tail fin
            DrawLine(t, 30, 52, 18, 58, 3, fin);
            DrawLine(t, 30, 52, 42, 58, 3, fin);
            DrawLine(t, 30, 52, 30, 60, 2, fin);
            // Dorsal fin
            DrawLine(t, 30, 22, 40, 18, 2, fin);
            DrawLine(t, 40, 18, 38, 26, 2, fin);
            // Scales
            DrawDot(t, 30, 28, 3, new Color(body.r*0.7f, body.g*0.7f, body.b*0.7f, 0.7f));
            DrawDot(t, 30, 38, 3, new Color(body.r*0.7f, body.g*0.7f, body.b*0.7f, 0.7f));
        }

        private static void DrawLanternShape(Texture2D t, Color bodyColor, Color lightColor)
        {
            // Hanging hook
            DrawLine(t, 32, 6, 32, 14, 2, StoneGray);
            // Top cap
            Rect(t, 24, 14, 16, 4, bodyColor);
            // Body ribs (oval sections)
            DrawOval(t, 32, 32, 16, 22, bodyColor);
            DrawLine(t, 16, 24, 48, 24, 1, new Color(bodyColor.r*.7f,bodyColor.g*.7f,bodyColor.b*.7f,1f));
            DrawLine(t, 16, 32, 48, 32, 1, new Color(bodyColor.r*.7f,bodyColor.g*.7f,bodyColor.b*.7f,1f));
            DrawLine(t, 16, 40, 48, 40, 1, new Color(bodyColor.r*.7f,bodyColor.g*.7f,bodyColor.b*.7f,1f));
            // Bottom cap
            Rect(t, 24, 52, 16, 4, bodyColor);
            DrawLine(t, 32, 56, 32, 60, 2, StoneGray);
            // Light glow
            DrawDot(t, 32, 32, 8, new Color(lightColor.r, lightColor.g, lightColor.b, 0.55f));
            DrawDot(t, 32, 32, 4, lightColor);
        }

        private static void DrawGinkgoLeafShape(Texture2D t, Color light, Color dark)
        {
            // Stem
            DrawLine(t, 32, 56, 32, 40, 3, WoodBrown);
            // Fan ribs
            for (var ri = 0; ri < 7; ri++)
            {
                var ang = -Mathf.PI * 0.75f + ri * Mathf.PI * 0.25f;
                DrawLine(t, 32, 40, 32 + (int)(22 * Mathf.Cos(ang)), 40 + (int)(22 * Mathf.Sin(ang)), 2, dark);
            }
            // Arc connecting tips
            for (var a = 0; a < 360; a += 2)
            {
                var rad = a * Mathf.Deg2Rad;
                if (a < 180) continue; // bottom half only (fan top)
                var px = 32 + (int)(22 * Mathf.Cos(rad - Mathf.PI / 2f));
                var py = 40 + (int)(22 * Mathf.Sin(rad - Mathf.PI / 2f));
                if (px >= 0 && px < S && py >= 0 && py < S)
                    DrawDot(t, px, py, 2, light);
            }
            // Fill interior with lighter shade
            for (var fy = 20; fy < 40; fy++)
                for (var fx = 14; fx < 50; fx++)
                {
                    var dx = fx - 32; var dy = fy - 40;
                    if (dx*dx + dy*dy < 22*22 && fy < 40)
                        if (t.GetPixel(fx, fy) == BgDark)
                            t.SetPixel(fx, fy, new Color(light.r, light.g, light.b, 0.45f));
                }
        }

        private static void DrawUmbrellaShape(Texture2D t, Color canopy, Color pole)
        {
            // Pole
            DrawLine(t, 32, 56, 32, 22, 3, pole);
            // Canopy arc (filled half circle)
            for (var a = 180; a <= 360; a += 2)
            {
                var rad = a * Mathf.Deg2Rad;
                var r = 22;
                for (var ir = 2; ir < r; ir++)
                {
                    var px = 32 + (int)(ir * Mathf.Cos(rad));
                    var py = 28 + (int)(ir * Mathf.Sin(rad));
                    if (px >= 0 && px < S && py >= 0 && py < S)
                        t.SetPixel(px, py, canopy);
                }
            }
            // Ribs
            for (var ri = 0; ri < 7; ri++)
            {
                var ang = Mathf.PI + ri * Mathf.PI / 6f;
                DrawLine(t, 32, 28, 32 + (int)(22 * Mathf.Cos(ang)), 28 + (int)(22 * Mathf.Sin(ang)), 1, new Color(pole.r,pole.g,pole.b,0.6f));
            }
            // Tip
            DrawDot(t, 38, 56, 4, pole);
        }

        private static void DrawFanShape(Texture2D t, Color main, Color trim)
        {
            // Handle
            DrawLine(t, 32, 56, 26, 40, 3, WoodBrown);
            // Fan ribs from handle top
            for (var ri = 0; ri < 9; ri++)
            {
                var ang = -Mathf.PI * 0.75f + ri * Mathf.PI * 0.1875f;
                DrawLine(t, 26, 40, 26 + (int)(24 * Mathf.Cos(ang)), 40 + (int)(24 * Mathf.Sin(ang)), 2, main);
            }
            // Arc at tip
            for (var a = -135; a <= -45; a += 3)
            {
                var rad = a * Mathf.Deg2Rad;
                var px = 26 + (int)(24 * Mathf.Cos(rad));
                var py = 40 + (int)(24 * Mathf.Sin(rad));
                DrawDot(t, px, py, 3, trim);
            }
            // Mid arc
            for (var a = -130; a <= -50; a += 4)
            {
                var rad = a * Mathf.Deg2Rad;
                var px = 26 + (int)(16 * Mathf.Cos(rad));
                var py = 40 + (int)(16 * Mathf.Sin(rad));
                DrawDot(t, px, py, 2, new Color(main.r*.8f, main.g*.8f, main.b*.8f, 0.8f));
            }
        }

        private static void DrawSakuraShape(Texture2D t, Color petal, Color center)
        {
            // 5 petals radiating from center
            for (var pi = 0; pi < 5; pi++)
            {
                var ang = pi * Mathf.PI * 2f / 5f - Mathf.PI / 2f;
                var px = (int)(32 + 14 * Mathf.Cos(ang));
                var py = (int)(32 + 14 * Mathf.Sin(ang));
                DrawOval(t, px, py, 7, 5, petal);
            }
            DrawDot(t, 32, 32, 6, center);
            DrawDot(t, 32, 32, 3, new Color(center.r, center.g, center.b, 0.5f));
        }

        private static void DrawCraneShape(Texture2D t, Color body, Color shadow)
        {
            // Body (triangle pointing right)
            DrawLine(t, 12, 38, 40, 28, 4, body);
            DrawLine(t, 40, 28, 38, 42, 4, body);
            DrawLine(t, 12, 38, 38, 42, 3, body);
            // Wings
            DrawLine(t, 24, 34, 14, 20, 3, body);
            DrawLine(t, 14, 20, 36, 24, 2, shadow);
            DrawLine(t, 24, 34, 46, 22, 3, body);
            // Head
            DrawDot(t, 44, 26, 5, body);
            DrawDot(t, 48, 22, 3, SoftRed); // red crown
            DrawLine(t, 46, 28, 52, 30, 2, body); // beak
            // Leg
            DrawLine(t, 26, 42, 22, 54, 2, shadow);
            break_stub();
        }

        private static void break_stub() {} // no-op, avoids compiler complaint in switch

        private static void DrawMaskShape(Texture2D t, Color face, Color detail)
        {
            DrawOval(t, 32, 34, 22, 26, face);
            // Eye holes
            DrawOval(t, 22, 28, 7, 5, detail);
            DrawOval(t, 42, 28, 7, 5, detail);
            // Nose
            DrawLine(t, 30, 36, 34, 36, 2, detail);
            DrawLine(t, 30, 36, 28, 40, 1, detail);
            DrawLine(t, 34, 36, 36, 40, 1, detail);
            // Mouth
            DrawArcBottom(t, 32, 44, 8, detail, 2);
            // Ear holes
            DrawDot(t, 10, 34, 4, detail);
            DrawDot(t, 54, 34, 4, detail);
        }

        private static void DrawPhoenixFeatherShape(Texture2D t)
        {
            // Quill
            DrawLine(t, 32, 56, 32, 10, 3, CreamWhite);
            // Plume left
            for (var i = 0; i < 6; i++)
            {
                var y = 18 + i * 6;
                DrawLine(t, 32, y, 32 - 6 - i * 2, y + 4, 2, KoiOrange);
                DrawDot(t, 32 - 8 - i * 2, y + 5, 3, new Color(1f, 0.6f + i * 0.05f, 0.2f, 1f));
            }
            // Plume right
            for (var i = 0; i < 6; i++)
            {
                var y = 18 + i * 6;
                DrawLine(t, 32, y, 32 + 6 + i * 2, y + 4, 2, BrightGold);
                DrawDot(t, 32 + 8 + i * 2, y + 5, 3, new Color(1f, 0.8f + i * 0.02f, 0.3f, 1f));
            }
            DrawDot(t, 32, 10, 5, BrightGold); // tip glow
        }

        private static void DrawCompassShape(Texture2D t, Color ring, Color needle)
        {
            DrawCircleOutline(t, 32, 32, 22, 3, ring);
            DrawCircleOutline(t, 32, 32, 14, 1, new Color(ring.r,ring.g,ring.b,0.4f));
            // Cardinal marks
            DrawLine(t, 32, 11, 32, 16, 2, CreamWhite); // N
            DrawLine(t, 32, 48, 32, 53, 2, CreamWhite); // S
            DrawLine(t, 11, 32, 16, 32, 2, CreamWhite); // W
            DrawLine(t, 48, 32, 53, 32, 2, CreamWhite); // E
            // Needle
            DrawLine(t, 32, 32, 32, 14, 3, SoftRed);   // north red
            DrawLine(t, 32, 32, 32, 50, 3, needle);    // south
            DrawLine(t, 32, 32, 22, 42, 2, needle);    // west
            DrawDot(t, 32, 32, 4, CreamWhite);
        }

        private static void DrawGoldenRootShape(Texture2D t)
        {
            // Trunk
            DrawLine(t, 32, 12, 32, 36, 5, WoodBrown);
            // Roots spreading
            DrawLine(t, 32, 36, 14, 52, 3, BrightGold);
            DrawLine(t, 32, 36, 20, 56, 2, LanternGold);
            DrawLine(t, 32, 36, 32, 58, 3, BrightGold);
            DrawLine(t, 32, 36, 44, 56, 2, LanternGold);
            DrawLine(t, 32, 36, 50, 52, 3, BrightGold);
            // Root tips glow
            DrawDot(t, 14, 52, 4, BrightGold);
            DrawDot(t, 50, 52, 4, BrightGold);
            DrawDot(t, 32, 58, 5, BrightGold);
            // Canopy
            DrawDot(t, 32, 16, 14, new Color(CalmGreen.r, CalmGreen.g, CalmGreen.b, 0.8f));
            DrawDot(t, 22, 20, 8, DarkGreen);
            DrawDot(t, 42, 20, 8, DarkGreen);
        }

        private static void DrawSudokuGrid(Texture2D t, Color line)
        {
            // 3x3 thick grid (like 9x9 sudoku boxes)
            for (var gi = 0; gi <= 3; gi++)
            {
                var x = 10 + gi * 14;
                var thick = (gi % 3 == 0) ? 2 : 1;
                Rect(t, x, 10, thick, 42, line);
                Rect(t, 10, x, 42, thick, line);
            }
            // Center accent
            DrawDot(t, 32, 32, 4, new Color(line.r, line.g, line.b, 0.5f));
        }

        private static void DrawSpiral(Texture2D t, int cx, int cy, Color inner, Color outer)
        {
            for (var step = 0; step < 200; step++)
            {
                var angle = step * 0.2f;
                var r = step * 0.08f;
                var px = cx + (int)(r * Mathf.Cos(angle));
                var py = cy + (int)(r * Mathf.Sin(angle));
                if (px < 4 || py < 4 || px >= S - 4 || py >= S - 4) continue;
                var blend = step / 200f;
                var c = Color.Lerp(inner, outer, blend);
                DrawDot(t, px, py, Mathf.Max(1, 3 - (int)(step / 60f)), c);
            }
        }

        private static void DrawLotusShape(Texture2D t, Color petal, Color center)
        {
            // Outer petals (8)
            for (var li = 0; li < 8; li++)
            {
                var ang = li * Mathf.PI / 4f;
                var px = (int)(32 + 16 * Mathf.Cos(ang));
                var py = (int)(32 + 16 * Mathf.Sin(ang));
                DrawOval(t, px, py, 6, 9, petal);
            }
            // Inner petals (4)
            for (var li = 0; li < 4; li++)
            {
                var ang = li * Mathf.PI / 2f + Mathf.PI / 4f;
                var px = (int)(32 + 9 * Mathf.Cos(ang));
                var py = (int)(32 + 9 * Mathf.Sin(ang));
                DrawOval(t, px, py, 5, 7, new Color(petal.r*.9f,petal.g*.9f,petal.b*.85f,1f));
            }
            // Center
            DrawDot(t, 32, 32, 6, center);
            DrawDot(t, 32, 32, 3, BrightGold);
        }

        private static void DrawDragonsEyeShape(Texture2D t)
        {
            // Outer eye (almond shape)
            DrawOval(t, 32, 32, 24, 14, new Color(0.80f, 0.20f, 0.10f, 0.9f));
            DrawOvalOutline(t, 32, 32, 24, 14, 2, SoftRed);
            // Iris
            DrawDot(t, 32, 32, 10, new Color(0.55f, 0.25f, 0.70f, 1f));
            // Pupil (vertical slit)
            Rect(t, 30, 22, 4, 20, InkBlack);
            DrawDot(t, 32, 32, 3, new Color(0.1f, 0.05f, 0.05f, 1f));
            // Light reflection
            DrawDot(t, 28, 26, 2, CreamWhite);
            // Scales around eye
            for (var si = 0; si < 6; si++)
            {
                var ang = si * Mathf.PI / 3f;
                DrawDot(t, 32 + (int)(20 * Mathf.Cos(ang)), 32 + (int)(20 * Mathf.Sin(ang)), 3, new Color(0.7f,0.2f,0.1f,0.8f));
            }
        }

        private static void DrawTreeShape(Texture2D t, Color dark, Color mid, Color light)
        {
            // Trunk
            Rect(t, 28, 40, 8, 18, WoodBrown);
            // Three canopy layers
            DrawDot(t, 32, 36, 12, mid);
            DrawDot(t, 24, 30, 8, dark);
            DrawDot(t, 40, 30, 8, dark);
            DrawDot(t, 32, 22, 14, light);
            DrawDot(t, 32, 16, 8, mid);
        }

        private static void DrawChest(Texture2D t, int x, int y, int w, int h, Color c)
        {
            Rect(t, x, y + h / 2, w, h / 2, c);
            Rect(t, x, y, w, h / 2, new Color(c.r * 0.8f, c.g * 0.8f, c.b * 0.8f, 1f));
            DrawLine(t, x, y + h / 2, x + w, y + h / 2, 1, InkBlack);
            DrawDot(t, x + w / 2, y + h / 2, 2, BrightGold); // latch
        }

        private static void DrawGearShape(Texture2D t, Color body, Color highlight)
        {
            DrawDot(t, 32, 32, 18, body);
            DrawDot(t, 32, 32, 10, BgDark);  // center hole
            DrawDot(t, 32, 32, 5, body);
            // Teeth (8)
            for (var ti = 0; ti < 8; ti++)
            {
                var ang = ti * Mathf.PI / 4f;
                Rect(t,
                    32 + (int)(16 * Mathf.Cos(ang)) - 3,
                    32 + (int)(16 * Mathf.Sin(ang)) - 3,
                    6, 6, highlight);
            }
        }

        private static void DrawToriiShape(Texture2D t, Color beam, Color post)
        {
            // Two vertical posts
            Rect(t, 16, 18, 5, 38, post);
            Rect(t, 43, 18, 5, 38, post);
            // Top beam
            Rect(t, 10, 14, 44, 5, beam);
            // Second beam
            Rect(t, 16, 24, 32, 4, beam);
            // Kasagi end curves (simplified as dots)
            DrawDot(t, 10, 14, 5, beam);
            DrawDot(t, 54, 14, 5, beam);
        }

        // ════════════════════════════════════════════════════════
        // Primitive Helpers
        // ════════════════════════════════════════════════════════

        private static Texture2D MakeTex(Color fill)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var px = new Color[S * S];
            for (var i = 0; i < px.Length; i++) px[i] = fill;
            t.SetPixels(px);
            return t;
        }

        private static void DrawBorder(Texture2D t, Color c, int thick)
        {
            for (var i = 0; i < thick; i++)
            {
                for (var x = 0; x < S; x++)
                {
                    t.SetPixel(x, i, c);
                    t.SetPixel(x, S - 1 - i, c);
                }
                for (var y = 0; y < S; y++)
                {
                    t.SetPixel(i, y, c);
                    t.SetPixel(S - 1 - i, y, c);
                }
            }
        }

        private static void DrawDot(Texture2D t, int cx, int cy, int r, Color c)
        {
            for (var y = cy - r; y <= cy + r; y++)
                for (var x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || y < 0 || x >= S || y >= S) continue;
                    if ((x-cx)*(x-cx)+(y-cy)*(y-cy) <= r*r)
                        t.SetPixel(x, y, c);
                }
        }

        private static void DrawOval(Texture2D t, int cx, int cy, int rx, int ry, Color c)
        {
            for (var y = cy - ry; y <= cy + ry; y++)
                for (var x = cx - rx; x <= cx + rx; x++)
                {
                    if (x < 0 || y < 0 || x >= S || y >= S) continue;
                    var dx = (float)(x - cx) / rx;
                    var dy = (float)(y - cy) / ry;
                    if (dx*dx + dy*dy <= 1f)
                        t.SetPixel(x, y, c);
                }
        }

        private static void DrawOvalOutline(Texture2D t, int cx, int cy, int rx, int ry, int thick, Color c)
        {
            for (var y = cy - ry - thick; y <= cy + ry + thick; y++)
                for (var x = cx - rx - thick; x <= cx + rx + thick; x++)
                {
                    if (x < 0 || y < 0 || x >= S || y >= S) continue;
                    var dx = (float)(x - cx) / rx;
                    var dy = (float)(y - cy) / ry;
                    var dist = Mathf.Sqrt(dx*dx + dy*dy);
                    if (dist >= 1f - (float)thick/rx && dist <= 1f + (float)thick/rx)
                        t.SetPixel(x, y, c);
                }
        }

        private static void DrawCircleOutline(Texture2D t, int cx, int cy, int r, int thick, Color c)
        {
            DrawOvalOutline(t, cx, cy, r, r, thick, c);
        }

        private static void DrawLine(Texture2D t, int x0, int y0, int x1, int y1, int thick, Color c)
        {
            var dx = x1 - x0; var dy = y1 - y0;
            var steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
            if (steps == 0) { DrawDot(t, x0, y0, thick / 2, c); return; }
            for (var i = 0; i <= steps; i++)
            {
                var px = x0 + (int)(dx * i / (float)steps);
                var py = y0 + (int)(dy * i / (float)steps);
                DrawDot(t, px, py, thick / 2, c);
            }
        }

        private static void Rect(Texture2D t, int x, int y, int w, int h, Color c)
        {
            for (var ry = y; ry < y + h; ry++)
                for (var rx = x; rx < x + w; rx++)
                {
                    if (rx < 0 || ry < 0 || rx >= S || ry >= S) continue;
                    t.SetPixel(rx, ry, c);
                }
        }

        private static void DrawArcBottom(Texture2D t, int cx, int cy, int r, Color c, int thick)
        {
            // Draw bottom half of circle (bowl shape)
            for (var a = 0; a <= 180; a += 2)
            {
                var rad = a * Mathf.Deg2Rad;
                for (var ti = 0; ti < thick; ti++)
                {
                    var pr = r - ti;
                    var px = cx + (int)(pr * Mathf.Cos(rad));
                    var py = cy + (int)(pr * Mathf.Sin(rad));
                    if (px >= 0 && py >= 0 && px < S && py < S)
                        t.SetPixel(px, py, c);
                }
            }
        }

        private static void DrawHexagon(Texture2D t, int cx, int cy, int r, Color c, int thick)
        {
            for (var i = 0; i < 6; i++)
            {
                var a0 = (i * 60 - 30) * Mathf.Deg2Rad;
                var a1 = ((i + 1) * 60 - 30) * Mathf.Deg2Rad;
                DrawLine(t,
                    cx + (int)(r * Mathf.Cos(a0)), cy + (int)(r * Mathf.Sin(a0)),
                    cx + (int)(r * Mathf.Cos(a1)), cy + (int)(r * Mathf.Sin(a1)),
                    thick, c);
            }
        }

        private static void DrawDiamond(Texture2D t, int cx, int cy, int rx, int ry, Color c)
        {
            for (var y = cy - ry; y <= cy + ry; y++)
                for (var x = cx - rx; x <= cx + rx; x++)
                {
                    if (x < 0 || y < 0 || x >= S || y >= S) continue;
                    if (Mathf.Abs(x - cx) / (float)rx + Mathf.Abs(y - cy) / (float)ry <= 1f)
                        t.SetPixel(x, y, c);
                }
        }

        private static void DrawStar(Texture2D t, int cx, int cy, int rOut, int rIn, Color c)
        {
            for (var i = 0; i < 8; i++)
            {
                var ang0 = i * Mathf.PI / 4f;
                var ang1 = ang0 + Mathf.PI / 8f;
                DrawLine(t, cx, cy,
                    cx + (int)(rOut * Mathf.Cos(ang0)), cy + (int)(rOut * Mathf.Sin(ang0)), 3, c);
                DrawLine(t, cx, cy,
                    cx + (int)(rIn * Mathf.Cos(ang1)), cy + (int)(rIn * Mathf.Sin(ang1)), 2, new Color(c.r*0.7f,c.g*0.7f,c.b*0.7f,1f));
            }
        }

        // ════════════════════════════════════════════════════════
        // Import Configuration
        // ════════════════════════════════════════════════════════

        private static void ConfigureSprites(string dir)
        {
            foreach (var f in Directory.GetFiles(dir, "icon_*.png", SearchOption.TopDirectoryOnly))
            {
                var path = f.Replace("\\", "/");
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.filterMode = FilterMode.Point;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
        }
    }
}
#endif
