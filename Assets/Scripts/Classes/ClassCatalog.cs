using SudokuRoguelike.Core;

namespace SudokuRoguelike.Classes
{
    public sealed class ClassMeta
    {
        public int Tier;
        public ClassComplexity Complexity;
        public PlayerSkillBand SkillBand;
        public string PassiveDescription;
        public string Restriction;
    }

    public sealed class ClassSnapshot
    {
        public ClassId Id;
        public bool Playable;
        public int HP;
        public int Pencil;
        public int ItemSlots;
        public int RerollTokens;
        public bool CanBuyPencilMidLevel;
    }

    public static class ClassCatalog
    {
        public static ClassMeta GetMeta(ClassId id)
        {
            return id switch
            {
                ClassId.NumberFreak => new ClassMeta
                {
                    Tier = 1,
                    Complexity = ClassComplexity.Low,
                    SkillBand = PlayerSkillBand.Beginner,
                    PassiveDescription = "Balanced baseline class."
                },
                ClassId.GardenMonk => new ClassMeta
                {
                    Tier = 2,
                    Complexity = ClassComplexity.Low,
                    SkillBand = PlayerSkillBand.Early,
                    PassiveDescription = "Every 5 correct placements restore +1 HP.",
                    Restriction = "Cannot buy Pencil mid-level"
                },
                ClassId.ShrineArchivist => new ClassMeta
                {
                    Tier = 3,
                    Complexity = ClassComplexity.Medium,
                    SkillBand = PlayerSkillBand.Intermediate,
                    PassiveDescription = "First pencil use in each cell is free."
                },
                ClassId.KoiGambler => new ClassMeta
                {
                    Tier = 4,
                    Complexity = ClassComplexity.Medium,
                    SkillBand = PlayerSkillBand.Adaptive,
                    PassiveDescription = "25% chance wrong placement costs no HP; 25% chance correct placement grants +1 Gold."
                },
                ClassId.StoneGardener => new ClassMeta
                {
                    Tier = 5,
                    Complexity = ClassComplexity.High,
                    SkillBand = PlayerSkillBand.Advanced,
                    PassiveDescription = "First item used each level is not consumed."
                },
                ClassId.LanternSeer => new ClassMeta
                {
                    Tier = 6,
                    Complexity = ClassComplexity.High,
                    SkillBand = PlayerSkillBand.Expert,
                    PassiveDescription = "Boss modifiers are 20% weaker (intensity reduction)."
                },
                ClassId.ReedDuelist => new ClassMeta
                {
                    Tier = 7,
                    Complexity = ClassComplexity.High,
                    SkillBand = PlayerSkillBand.Expert,
                    PassiveDescription = "No-mistake, no-pencil puzzle clear grants +2 Pencil for the run."
                },
                ClassId.QuietCartographer => new ClassMeta
                {
                    Tier = 8,
                    Complexity = ClassComplexity.High,
                    SkillBand = PlayerSkillBand.Expert,
                    PassiveDescription = "After a perfect tile, preview board size and stars of unrevealed adjacent tiles."
                },
                _ => GetMeta(ClassId.NumberFreak)
            };
        }

        public static ClassSnapshot Build(ClassId id)
        {
            return id switch
            {
                ClassId.NumberFreak => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 10,
                    Pencil = 10,
                    ItemSlots = 2,
                    RerollTokens = 1,
                    CanBuyPencilMidLevel = true
                },
                ClassId.GardenMonk => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 14,
                    Pencil = 5,
                    ItemSlots = 1,
                    RerollTokens = 0,
                    CanBuyPencilMidLevel = false
                },
                ClassId.ShrineArchivist => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 8,
                    Pencil = 15,
                    ItemSlots = 2,
                    RerollTokens = 0,
                    CanBuyPencilMidLevel = true
                },
                ClassId.KoiGambler => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 9,
                    Pencil = 8,
                    ItemSlots = 2,
                    RerollTokens = 0,
                    CanBuyPencilMidLevel = true
                },
                ClassId.StoneGardener => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 11,
                    Pencil = 8,
                    ItemSlots = 3,
                    RerollTokens = 0,
                    CanBuyPencilMidLevel = true
                },
                ClassId.LanternSeer => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 7,
                    Pencil = 12,
                    ItemSlots = 2,
                    RerollTokens = 0,
                    CanBuyPencilMidLevel = true
                },
                ClassId.ReedDuelist => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 8,
                    Pencil = 7,
                    ItemSlots = 2,
                    RerollTokens = 1,
                    CanBuyPencilMidLevel = true
                },
                ClassId.QuietCartographer => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 9,
                    Pencil = 10,
                    ItemSlots = 2,
                    RerollTokens = 1,
                    CanBuyPencilMidLevel = true
                },
                _ => Build(ClassId.NumberFreak)
            };
        }
    }
}
