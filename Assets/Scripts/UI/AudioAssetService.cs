using System;
using System.Collections.Generic;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    public enum AudioClipId
    {
        MusicMenuLoop,
        MusicPuzzleLoop,
        MusicShopLoop,
        MusicRestLoop,
        MusicFloor0,
        MusicFloor1,
        MusicFloor2,
        MusicFloor3,
        MusicFloor4,
        MusicBossLayer0,
        MusicBossLayer1,
        MusicBossLayer2,
        MusicBossLayer3,
        MusicBossLayer4,
        SfxWrongPlacement,
        SfxPuzzleSolved,
        SfxShopPurchase,
        SfxShopReroll,
        SfxItemUse,
        SfxRewardClaim,
        SfxPathAdvance,
        SfxCorrectPlacement,
        SfxCellSelect,
        SfxPencilToggle,
        SfxRestHeal,
        SfxGameOver,
        SfxRelicPickup,
        SfxCombo5,
        SfxCombo10,
        SfxComboBreak,
        SfxHpLoss,
        SfxHpCritical,
        SfxPencilDepleted,
        SfxGoldGain,
        SfxFogReveal,
        SfxModifierActivate,
        SfxBossGateOpen,
        SfxFloorTransition,
        SfxLevelUp,
        SfxAchievement,
        SfxXpBarFill,
        SfxVictory,
        SfxRunVictoryFanfare,
        UiButtonHover,
        UiButtonClick,
        UiMenuOpen,
        UiMenuClose,
        UiItemReward,
        UiRelicReveal
    }

    public enum AudioAssetCategory
    {
        Music,
        Sfx,
        Ui
    }

    public sealed class AudioAssetInfo
    {
        public AudioAssetInfo(
            AudioClipId id,
            string resourcePath,
            AudioAssetCategory category,
            bool loop,
            bool releaseRequired,
            string usage)
        {
            Id = id;
            ResourcePath = resourcePath;
            Category = category;
            Loop = loop;
            ReleaseRequired = releaseRequired;
            Usage = usage;
        }

        public AudioClipId Id { get; }
        public string ResourcePath { get; }
        public AudioAssetCategory Category { get; }
        public bool Loop { get; }
        public bool ReleaseRequired { get; }
        public string Usage { get; }
    }

    public static class AudioAssetService
    {
        private static readonly AudioAssetInfo[] ClipInfo =
        {
            new AudioAssetInfo(AudioClipId.MusicMenuLoop, "audio/music/menu_loop", AudioAssetCategory.Music, true, true, "Main menu ambience."),
            new AudioAssetInfo(AudioClipId.MusicPuzzleLoop, "audio/music/puzzle_loop", AudioAssetCategory.Music, true, true, "Fallback run puzzle loop before floor music is selected."),
            new AudioAssetInfo(AudioClipId.MusicShopLoop, "audio/music/shop_loop", AudioAssetCategory.Music, true, true, "Shop encounter loop."),
            new AudioAssetInfo(AudioClipId.MusicRestLoop, "audio/music/rest_loop", AudioAssetCategory.Music, true, true, "Rest encounter loop."),
            new AudioAssetInfo(AudioClipId.MusicFloor0, "audio/music/floor_00_bamboo_courtyard", AudioAssetCategory.Music, true, true, "Floor 1 puzzle/path loop."),
            new AudioAssetInfo(AudioClipId.MusicFloor1, "audio/music/floor_01_moss_garden", AudioAssetCategory.Music, true, true, "Floor 2 puzzle/path loop."),
            new AudioAssetInfo(AudioClipId.MusicFloor2, "audio/music/floor_02_koi_terrace", AudioAssetCategory.Music, true, true, "Floor 3 puzzle/path loop."),
            new AudioAssetInfo(AudioClipId.MusicFloor3, "audio/music/floor_03_stone_lantern_walk", AudioAssetCategory.Music, true, true, "Floor 4 puzzle/path loop."),
            new AudioAssetInfo(AudioClipId.MusicFloor4, "audio/music/floor_04_shrine_summit", AudioAssetCategory.Music, true, true, "Floor 5 puzzle/path loop."),
            new AudioAssetInfo(AudioClipId.MusicBossLayer0, "audio/music/boss_layer_00", AudioAssetCategory.Music, true, true, "Floor 1 boss overlay."),
            new AudioAssetInfo(AudioClipId.MusicBossLayer1, "audio/music/boss_layer_01", AudioAssetCategory.Music, true, true, "Floor 2 boss overlay."),
            new AudioAssetInfo(AudioClipId.MusicBossLayer2, "audio/music/boss_layer_02", AudioAssetCategory.Music, true, true, "Floor 3 boss overlay."),
            new AudioAssetInfo(AudioClipId.MusicBossLayer3, "audio/music/boss_layer_03", AudioAssetCategory.Music, true, true, "Floor 4 boss overlay."),
            new AudioAssetInfo(AudioClipId.MusicBossLayer4, "audio/music/boss_layer_04", AudioAssetCategory.Music, true, true, "Floor 5 boss overlay."),
            new AudioAssetInfo(AudioClipId.SfxWrongPlacement, "audio/sfx/wrong_placement", AudioAssetCategory.Sfx, false, true, "Invalid number placement."),
            new AudioAssetInfo(AudioClipId.SfxPuzzleSolved, "audio/sfx/puzzle_solved", AudioAssetCategory.Sfx, false, true, "Puzzle completion."),
            new AudioAssetInfo(AudioClipId.SfxShopPurchase, "audio/sfx/shop_purchase", AudioAssetCategory.Sfx, false, true, "Shop purchase confirmation."),
            new AudioAssetInfo(AudioClipId.SfxShopReroll, "audio/sfx/shop_reroll", AudioAssetCategory.Sfx, false, true, "Shop reroll."),
            new AudioAssetInfo(AudioClipId.SfxItemUse, "audio/sfx/item_use", AudioAssetCategory.Sfx, false, true, "Item activation."),
            new AudioAssetInfo(AudioClipId.SfxRewardClaim, "audio/sfx/reward_claim", AudioAssetCategory.Sfx, false, true, "Reward claim."),
            new AudioAssetInfo(AudioClipId.SfxPathAdvance, "audio/sfx/path_advance", AudioAssetCategory.Sfx, false, true, "Map node/path movement."),
            new AudioAssetInfo(AudioClipId.SfxCorrectPlacement, "audio/sfx/correct_placement", AudioAssetCategory.Sfx, false, true, "Correct number placement."),
            new AudioAssetInfo(AudioClipId.SfxCellSelect, "audio/sfx/cell_select", AudioAssetCategory.Sfx, false, true, "Grid cell selection."),
            new AudioAssetInfo(AudioClipId.SfxPencilToggle, "audio/sfx/pencil_toggle", AudioAssetCategory.Sfx, false, true, "Pencil mode toggle."),
            new AudioAssetInfo(AudioClipId.SfxRestHeal, "audio/sfx/rest_heal", AudioAssetCategory.Sfx, false, true, "Rest encounter healing."),
            new AudioAssetInfo(AudioClipId.SfxGameOver, "audio/sfx/game_over", AudioAssetCategory.Sfx, false, true, "Run failure stinger."),
            new AudioAssetInfo(AudioClipId.SfxRelicPickup, "audio/sfx/relic_pickup", AudioAssetCategory.Sfx, false, true, "Relic pickup."),
            new AudioAssetInfo(AudioClipId.SfxCombo5, "audio/sfx/combo_05", AudioAssetCategory.Sfx, false, true, "Five-combo milestone."),
            new AudioAssetInfo(AudioClipId.SfxCombo10, "audio/sfx/combo_10", AudioAssetCategory.Sfx, false, true, "Ten-combo milestone."),
            new AudioAssetInfo(AudioClipId.SfxComboBreak, "audio/sfx/combo_break", AudioAssetCategory.Sfx, false, true, "Combo break."),
            new AudioAssetInfo(AudioClipId.SfxHpLoss, "audio/sfx/hp_loss", AudioAssetCategory.Sfx, false, true, "Health loss."),
            new AudioAssetInfo(AudioClipId.SfxHpCritical, "audio/sfx/hp_critical", AudioAssetCategory.Sfx, false, true, "Critical health warning."),
            new AudioAssetInfo(AudioClipId.SfxPencilDepleted, "audio/sfx/pencil_depleted", AudioAssetCategory.Sfx, false, true, "No pencil marks remaining."),
            new AudioAssetInfo(AudioClipId.SfxGoldGain, "audio/sfx/gold_gain", AudioAssetCategory.Sfx, false, true, "Gold gain."),
            new AudioAssetInfo(AudioClipId.SfxFogReveal, "audio/sfx/fog_reveal", AudioAssetCategory.Sfx, false, true, "Fog tile reveal."),
            new AudioAssetInfo(AudioClipId.SfxModifierActivate, "audio/sfx/modifier_activate", AudioAssetCategory.Sfx, false, true, "Boss modifier activation."),
            new AudioAssetInfo(AudioClipId.SfxBossGateOpen, "audio/sfx/boss_gate_open", AudioAssetCategory.Sfx, false, true, "Boss gate opening."),
            new AudioAssetInfo(AudioClipId.SfxFloorTransition, "audio/sfx/floor_transition", AudioAssetCategory.Sfx, false, true, "Floor transition."),
            new AudioAssetInfo(AudioClipId.SfxLevelUp, "audio/sfx/level_up", AudioAssetCategory.Sfx, false, true, "Class level-up."),
            new AudioAssetInfo(AudioClipId.SfxAchievement, "audio/sfx/achievement", AudioAssetCategory.Sfx, false, true, "Achievement unlock."),
            new AudioAssetInfo(AudioClipId.SfxXpBarFill, "audio/sfx/xp_bar_fill", AudioAssetCategory.Sfx, false, true, "Post-run XP fill."),
            new AudioAssetInfo(AudioClipId.SfxVictory, "audio/sfx/victory", AudioAssetCategory.Sfx, false, true, "Short victory confirmation."),
            new AudioAssetInfo(AudioClipId.SfxRunVictoryFanfare, "audio/sfx/run_victory_fanfare", AudioAssetCategory.Sfx, false, true, "Final run victory fanfare."),
            new AudioAssetInfo(AudioClipId.UiButtonHover, "audio/ui/button_hover", AudioAssetCategory.Ui, false, true, "Button hover."),
            new AudioAssetInfo(AudioClipId.UiButtonClick, "audio/ui/button_click", AudioAssetCategory.Ui, false, true, "Button click."),
            new AudioAssetInfo(AudioClipId.UiMenuOpen, "audio/ui/menu_open", AudioAssetCategory.Ui, false, true, "Menu panel open."),
            new AudioAssetInfo(AudioClipId.UiMenuClose, "audio/ui/menu_close", AudioAssetCategory.Ui, false, true, "Menu panel close."),
            new AudioAssetInfo(AudioClipId.UiItemReward, "audio/ui/item_reward", AudioAssetCategory.Ui, false, true, "Item reward reveal."),
            new AudioAssetInfo(AudioClipId.UiRelicReveal, "audio/ui/relic_reveal", AudioAssetCategory.Ui, false, true, "Relic reveal.")
        };

        private static readonly Dictionary<AudioClipId, AudioAssetInfo> InfoById = BuildInfoById();
        private static readonly Dictionary<AudioClipId, AudioClip> AuthoredCache = new Dictionary<AudioClipId, AudioClip>();
        private static readonly HashSet<AudioClipId> MissingAuthored = new HashSet<AudioClipId>();
        private static readonly string[] MenuMusicStyleKeys =
        {
            "Audio.MenuMusicStyle.Garden",
            "Audio.MenuMusicStyle.Bamboo",
            "Audio.MenuMusicStyle.Rest"
        };

        public static IReadOnlyList<AudioAssetInfo> RequiredClips => ClipInfo;
        public static IReadOnlyList<string> MenuMusicStyleLocalizationKeys => MenuMusicStyleKeys;

        public static AudioClip GetClip(AudioClipId id, Func<AudioClip> proceduralFallbackFactory)
        {
            if (TryLoadAuthoredClip(id, out var authoredClip))
                return authoredClip;

            return proceduralFallbackFactory?.Invoke();
        }

        public static AudioClip GetFloorLoop(int floorIndex, Func<AudioClip> proceduralFallbackFactory)
        {
            return GetClip(GetFloorLoopId(floorIndex), proceduralFallbackFactory);
        }

        public static AudioClip GetBossLayer(int floorIndex, Func<AudioClip> proceduralFallbackFactory)
        {
            return GetClip(GetBossLayerId(floorIndex), proceduralFallbackFactory);
        }

        public static AudioClipId GetFloorLoopId(int floorIndex)
        {
            if (floorIndex <= 0) return AudioClipId.MusicFloor0;
            if (floorIndex == 1) return AudioClipId.MusicFloor1;
            if (floorIndex == 2) return AudioClipId.MusicFloor2;
            if (floorIndex == 3) return AudioClipId.MusicFloor3;
            return AudioClipId.MusicFloor4;
        }

        public static AudioClipId GetBossLayerId(int floorIndex)
        {
            if (floorIndex <= 0) return AudioClipId.MusicBossLayer0;
            if (floorIndex == 1) return AudioClipId.MusicBossLayer1;
            if (floorIndex == 2) return AudioClipId.MusicBossLayer2;
            if (floorIndex == 3) return AudioClipId.MusicBossLayer3;
            return AudioClipId.MusicBossLayer4;
        }

        public static int NormalizeMenuMusicStyleIndex(int styleIndex)
        {
            if (styleIndex < 0) return 0;
            if (styleIndex >= MenuMusicStyleKeys.Length) return 0;
            return styleIndex;
        }

        public static AudioClipId GetMenuMusicStyleLoopId(int styleIndex)
        {
            switch (NormalizeMenuMusicStyleIndex(styleIndex))
            {
                case 1:
                    return AudioClipId.MusicFloor0;
                case 2:
                    return AudioClipId.MusicRestLoop;
                default:
                    return AudioClipId.MusicMenuLoop;
            }
        }

        public static bool TryGetInfo(AudioClipId id, out AudioAssetInfo info)
        {
            return InfoById.TryGetValue(id, out info);
        }

        public static bool TryLoadAuthoredClip(AudioClipId id, out AudioClip clip)
        {
            if (AuthoredCache.TryGetValue(id, out clip))
                return clip != null;

            if (MissingAuthored.Contains(id))
            {
                clip = null;
                return false;
            }

            if (!InfoById.TryGetValue(id, out var info))
            {
                MissingAuthored.Add(id);
                clip = null;
                return false;
            }

            clip = Resources.Load<AudioClip>(info.ResourcePath);
            if (clip == null)
            {
                MissingAuthored.Add(id);
                return false;
            }

            AuthoredCache[id] = clip;
            return true;
        }

        public static void ResetCacheForTests()
        {
            AuthoredCache.Clear();
            MissingAuthored.Clear();
        }

        private static Dictionary<AudioClipId, AudioAssetInfo> BuildInfoById()
        {
            var result = new Dictionary<AudioClipId, AudioAssetInfo>();
            foreach (var info in ClipInfo)
                result[info.Id] = info;
            return result;
        }
    }
}
