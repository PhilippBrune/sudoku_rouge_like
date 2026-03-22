using UnityEngine;

namespace SudokuRoguelike.Data
{
    public static class FloorThemeData
    {
        public static readonly FloorTheme[] Themes =
        {
            new()
            {
                Name = "Cherry Blossom Garden",
                JapaneseName = "Sakura-en",
                BgColor = new Color(0.92f, 0.78f, 0.82f, 1f),
                PathColor = new Color(0.85f, 0.55f, 0.65f, 0.80f),
                AccentColor = new Color(1f, 0.72f, 0.78f, 1f),
                TileColor = new Color(0.18f, 0.22f, 0.16f, 0.92f),
                BossGateColor = new Color(0.45f, 0.15f, 0.15f, 1f)
            },
            new()
            {
                Name = "Bamboo Forest",
                JapaneseName = "Take-no-mori",
                BgColor = new Color(0.20f, 0.38f, 0.22f, 1f),
                PathColor = new Color(0.55f, 0.72f, 0.35f, 0.80f),
                AccentColor = new Color(0.82f, 0.75f, 0.30f, 1f),
                TileColor = new Color(0.12f, 0.20f, 0.10f, 0.92f),
                BossGateColor = new Color(0.40f, 0.18f, 0.10f, 1f)
            },
            new()
            {
                Name = "Zen Rock Garden",
                JapaneseName = "Karesansui",
                BgColor = new Color(0.60f, 0.58f, 0.52f, 1f),
                PathColor = new Color(0.78f, 0.76f, 0.70f, 0.80f),
                AccentColor = new Color(0.45f, 0.58f, 0.40f, 1f),
                TileColor = new Color(0.18f, 0.18f, 0.16f, 0.92f),
                BossGateColor = new Color(0.35f, 0.20f, 0.15f, 1f)
            },
            new()
            {
                Name = "Koi Pond Garden",
                JapaneseName = "Koi-no-niwa",
                BgColor = new Color(0.15f, 0.38f, 0.48f, 1f),
                PathColor = new Color(0.25f, 0.60f, 0.65f, 0.80f),
                AccentColor = new Color(0.90f, 0.52f, 0.18f, 1f),
                TileColor = new Color(0.10f, 0.18f, 0.22f, 0.92f),
                BossGateColor = new Color(0.42f, 0.15f, 0.12f, 1f)
            },
            new()
            {
                Name = "Mountain Temple",
                JapaneseName = "Yama-no-shinden",
                BgColor = new Color(0.12f, 0.10f, 0.14f, 1f),
                PathColor = new Color(0.40f, 0.32f, 0.28f, 0.80f),
                AccentColor = new Color(0.90f, 0.65f, 0.20f, 1f),
                TileColor = new Color(0.08f, 0.08f, 0.10f, 0.92f),
                BossGateColor = new Color(0.50f, 0.12f, 0.10f, 1f)
            }
        };

        public static FloorTheme Get(int floorIndex)
        {
            var clamped = Mathf.Clamp(floorIndex, 0, Themes.Length - 1);
            return Themes[clamped];
        }
    }

    public sealed class FloorTheme
    {
        public string Name;
        public string JapaneseName;
        public Color BgColor;
        public Color PathColor;
        public Color AccentColor;
        public Color TileColor;
        public Color BossGateColor;
    }
}
