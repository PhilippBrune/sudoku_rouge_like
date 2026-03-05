#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SudokuRoguelike.EditorTools
{
    public static class PixelIconSetGenerator
    {
        private const int IconSize = 64;
        private const int AtlasSize = 2048;
        private const int AtlasGrid = 16;
        private const int AtlasCell = AtlasSize / AtlasGrid;

        private static readonly Color CalmGreen = new(0.22f, 0.36f, 0.25f, 1f);
        private static readonly Color StoneGray = new(0.40f, 0.43f, 0.44f, 1f);
        private static readonly Color LanternGold = new(0.86f, 0.72f, 0.30f, 1f);
        private static readonly Color SoftRed = new(0.72f, 0.24f, 0.22f, 1f);
        private static readonly Color PastelBlue = new(0.40f, 0.70f, 0.85f, 1f);

        [MenuItem("Tools/Run of the Nine/Generate Pixel Icon Set")]
        public static void GenerateIconSet()
        {
            var outputDir = "Assets/Resources/GeneratedIcons";
            var atlasPath = "Assets/Resources/GeneratedIcons/RunOfTheNine_IconAtlas_2048.png";
            Directory.CreateDirectory(outputDir);

            var entries = BuildIconEntries();
            var atlas = NewTexture(AtlasSize, AtlasSize, new Color(0.06f, 0.12f, 0.09f, 0f));

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var icon = BuildIcon(entry);
                var filePath = Path.Combine(outputDir, entry.FileName + ".png");
                File.WriteAllBytes(filePath, icon.EncodeToPNG());

                var gridX = i % AtlasGrid;
                var gridY = i / AtlasGrid;
                BlitIconToAtlas(atlas, icon, gridX, gridY);
            }

            File.WriteAllBytes(atlasPath, atlas.EncodeToPNG());
            File.WriteAllText(Path.Combine(outputDir, "RunOfTheNine_IconMap.csv"), BuildMappingCsv(entries));

            AssetDatabase.Refresh();
            ConfigureSpriteImportSettings(outputDir, atlasPath);
            Debug.Log($"Generated {entries.Count} icons + atlas at {outputDir}");
        }

        private static void ConfigureSpriteImportSettings(string outputDir, string atlasPath)
        {
            var pngFiles = Directory.GetFiles(outputDir, "*.png", SearchOption.TopDirectoryOnly);
            for (var i = 0; i < pngFiles.Length; i++)
            {
                var path = pngFiles[i].Replace("\\", "/");
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            var atlasImporter = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
            if (atlasImporter != null)
            {
                atlasImporter.textureType = TextureImporterType.Default;
                atlasImporter.filterMode = FilterMode.Point;
                atlasImporter.mipmapEnabled = false;
                atlasImporter.alphaIsTransparency = true;
                atlasImporter.SaveAndReimport();
            }
        }

        private static List<IconEntry> BuildIconEntries()
        {
            return new List<IconEntry>
            {
                // Items
                E("Tea Cup", "items"), E("Moss Stone", "items"), E("Bamboo Scroll", "items"), E("Rice Bowl", "items"), E("Pebble", "items"),
                E("Jade Amulet", "items"), E("Wind Bell", "items"), E("Garden Lantern", "items"), E("Water Basin", "items"),
                E("Golden Koi", "items"), E("Sacred Bell", "items"), E("Temple Seal", "items"), E("Sun Medallion", "items"),
                E("Enlightenment Tree", "legendary"), E("Spirit Dragon Coin", "legendary"), E("Infinite Lotus", "legendary"),
                E("Broken Mask", "cursed"), E("Withered Flower", "cursed"), E("Blood Ink Brush", "cursed"), E("Fog Stone", "cursed"),

                // Classes
                E("Coin Sakura", "class"), E("Moss Tree", "class"), E("Flowing Wind", "class"), E("Geometric Seal", "class"), E("Fractured Lantern", "class"),

                // Map Nodes
                E("Elite Mask", "node"), E("Market Stall", "node"), E("Campfire Stones", "node"), E("Relic Pedestal", "node"), E("Triple Chest", "node"),
                E("Stone Altar", "node"), E("Moss Trap", "node"), E("Engraved Stone", "node"), E("Demon Mask", "boss"),

                // System/UI
                E("Sakura Coin", "ui"), E("Petal Orb", "ui"), E("Flame Stone", "ui"), E("Cracked Tile", "ui"), E("Flow Ribbon", "ui"),
                E("Golden Bloom", "ui"), E("Torii Lock", "ui"), E("Scroll Stamp", "ui"), E("Ink Save", "ui"), E("Stone Gear", "ui"),
                E("Language Scroll", "ui"), E("Temple Bell", "ui"), E("Framed Garden", "ui"),

                // Boss Modifiers
                E("Fog Cloud", "modifier"), E("Arrow Sum", "modifier"), E("Green Whisper", "modifier"), E("Orange Whisper", "modifier"),
                E("Parity Line", "modifier"), E("Renban Chain", "modifier"), E("Killer Cage", "modifier"), E("White Dot", "modifier"),
                E("Black Dot", "modifier"), E("Segmented Blue Line", "modifier"), E("RGB Circle", "modifier"),

                // Run Economy
                E("Spin Coin", "economy"), E("Empty Slot", "economy"), E("Iron Latch", "economy"), E("Swap Arrows", "economy"),
                E("Golden Bonsai", "economy"), E("Melting Relic", "economy"), E("Shattered Mask", "economy"),

                // Meta
                E("Bud", "meta"), E("Full Sakura", "meta"), E("Golden Sakura", "meta"), E("Halo Bloom", "meta"), E("Stacked Flame", "meta"),
                E("Infinity Stone", "meta"), E("Petal Hourglass", "meta"), E("Scroll Graph", "meta"), E("Corrupted Path", "meta"), E("Dual Mask", "meta")
            };
        }

        private static IconEntry E(string label, string category)
        {
            return new IconEntry
            {
                Label = label,
                Category = category,
                FileName = "icon_" + label.ToLowerInvariant().Replace(" ", "_")
            };
        }

        private static Texture2D BuildIcon(IconEntry entry)
        {
            var tex = NewTexture(IconSize, IconSize, new Color(0.10f, 0.16f, 0.12f, 1f));
            var hash = Mathf.Abs(entry.Label.GetHashCode());
            var border = BorderFor(entry.Category);

            DrawBorder(tex, border);
            DrawInnerPattern(tex, hash, AccentFor(entry.Category));
            DrawCornerMark(tex, entry.Category);

            tex.Apply();
            return tex;
        }

        private static Color BorderFor(string category)
        {
            if (category == "legendary") return LanternGold;
            if (category == "cursed") return SoftRed;
            if (category == "boss") return new Color(0.80f, 0.18f, 0.16f, 1f);
            if (category == "modifier") return PastelBlue;
            if (category == "class") return new Color(0.40f, 0.68f, 0.45f, 1f);
            return StoneGray;
        }

        private static Color AccentFor(string category)
        {
            if (category == "ui") return LanternGold;
            if (category == "economy") return new Color(0.78f, 0.69f, 0.30f, 1f);
            if (category == "meta") return new Color(0.72f, 0.78f, 0.45f, 1f);
            return CalmGreen;
        }

        private static void DrawBorder(Texture2D tex, Color color)
        {
            for (var x = 0; x < IconSize; x++)
            {
                for (var t = 0; t < 3; t++)
                {
                    tex.SetPixel(x, t, color);
                    tex.SetPixel(x, IconSize - 1 - t, color);
                }
            }

            for (var y = 0; y < IconSize; y++)
            {
                for (var t = 0; t < 3; t++)
                {
                    tex.SetPixel(t, y, color);
                    tex.SetPixel(IconSize - 1 - t, y, color);
                }
            }
        }

        private static void DrawInnerPattern(Texture2D tex, int hash, Color color)
        {
            var center = IconSize / 2;
            var r = 8 + (hash % 12);
            for (var y = center - r; y <= center + r; y++)
            {
                for (var x = center - r; x <= center + r; x++)
                {
                    if (x < 4 || y < 4 || x >= IconSize - 4 || y >= IconSize - 4)
                    {
                        continue;
                    }

                    var dx = x - center;
                    var dy = y - center;
                    var distSq = dx * dx + dy * dy;
                    if (distSq <= r * r)
                    {
                        tex.SetPixel(x, y, Color.Lerp(color, LanternGold, (hash % 7) / 10f));
                    }
                }
            }

            var bars = 2 + (hash % 4);
            for (var i = 0; i < bars; i++)
            {
                var y = 12 + (i * 10);
                for (var x = 10; x < IconSize - 10; x++)
                {
                    if (((x + hash + i) % 3) == 0)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void DrawCornerMark(Texture2D tex, string category)
        {
            if (category == "legendary")
            {
                DrawDot(tex, IconSize - 8, IconSize - 8, 3, LanternGold);
                DrawDot(tex, 8, IconSize - 8, 3, LanternGold);
            }
            else if (category == "cursed")
            {
                for (var i = 0; i < 8; i++)
                {
                    tex.SetPixel(IconSize - 12 + i, 6 + i, SoftRed);
                }
            }
            else if (category == "modifier")
            {
                DrawDot(tex, IconSize - 8, 8, 2, PastelBlue);
            }
        }

        private static void DrawDot(Texture2D tex, int cx, int cy, int r, Color color)
        {
            for (var y = cy - r; y <= cy + r; y++)
            {
                for (var x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || y < 0 || x >= tex.width || y >= tex.height)
                    {
                        continue;
                    }

                    var dx = x - cx;
                    var dy = y - cy;
                    if ((dx * dx) + (dy * dy) <= r * r)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void BlitIconToAtlas(Texture2D atlas, Texture2D icon, int gridX, int gridY)
        {
            var startX = gridX * AtlasCell + ((AtlasCell - IconSize) / 2);
            var startY = gridY * AtlasCell + ((AtlasCell - IconSize) / 2);

            for (var y = 0; y < IconSize; y++)
            {
                for (var x = 0; x < IconSize; x++)
                {
                    atlas.SetPixel(startX + x, AtlasSize - 1 - (startY + y), icon.GetPixel(x, y));
                }
            }

            atlas.Apply();
        }

        private static Texture2D NewTexture(int width, int height, Color fill)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = fill;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static string BuildMappingCsv(List<IconEntry> entries)
        {
            var lines = new List<string> { "index,label,category,file" };
            for (var i = 0; i < entries.Count; i++)
            {
                lines.Add($"{i},{entries[i].Label},{entries[i].Category},{entries[i].FileName}.png");
            }

            return string.Join("\n", lines);
        }

        private sealed class IconEntry
        {
            public string Label;
            public string Category;
            public string FileName;
        }
    }
}
#endif
