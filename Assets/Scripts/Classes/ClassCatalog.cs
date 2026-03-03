using SudokuRoguelike.Core;

namespace SudokuRoguelike.Classes
{
    public sealed class ClassMeta
    {
        public int Tier;
        public ClassComplexity Complexity;
        public PlayerSkillBand SkillBand;
        public string PassiveDescription;
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
                    PassiveDescription = "Every 5 correct placements restore +1 HP."
                },
                ClassId.ShrineArchivist => new ClassMeta
                {
                    Tier = 3,
                    Complexity = ClassComplexity.Medium,
                    SkillBand = PlayerSkillBand.Intermediate,
                    PassiveDescription = "First pencil per cell is free."
                },
                ClassId.KoiGambler => new ClassMeta
                {
                    Tier = 4,
                    Complexity = ClassComplexity.Medium,
                    SkillBand = PlayerSkillBand.Adaptive,
                    PassiveDescription = "25% wrong input ignores HP loss; 25% correct grants +1 Gold."
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
                    PassiveDescription = "Boss modifiers are 20% weaker."
                },
                ClassId.ZenMaster => new ClassMeta
                {
                    Tier = 0,
                    Complexity = ClassComplexity.Medium,
                    SkillBand = PlayerSkillBand.Advanced,
                    PassiveDescription = "Locked preview class."
                },
                ClassId.ChaosMonk => new ClassMeta
                {
                    Tier = 7,
                    Complexity = ClassComplexity.High,
                    SkillBand = PlayerSkillBand.Expert,
                    PassiveDescription = "Curses empower rewards and grant adaptive shields."
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
                ClassId.ZenMaster => new ClassSnapshot
                {
                    Id = id,
                    Playable = false,
                    HP = 8,
                    Pencil = 15,
                    ItemSlots = 1,
                    RerollTokens = 0,
                    CanBuyPencilMidLevel = true
                },
                ClassId.GardenMonk => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 14,
                    Pencil = 5,
                    ItemSlots = 1,
                    RerollTokens = 1,
                    CanBuyPencilMidLevel = false
                },
                ClassId.ShrineArchivist => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 8,
                    Pencil = 15,
                    ItemSlots = 2,
                    RerollTokens = 1,
                    CanBuyPencilMidLevel = true
                },
                ClassId.KoiGambler => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 9,
                    Pencil = 8,
                    ItemSlots = 2,
                    RerollTokens = 1,
                    CanBuyPencilMidLevel = true
                },
                ClassId.LanternSeer => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 7,
                    Pencil = 12,
                    ItemSlots = 2,
                    RerollTokens = 1,
                    CanBuyPencilMidLevel = true
                },
                ClassId.StoneGardener => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 11,
                    Pencil = 8,
                    ItemSlots = 3,
                    RerollTokens = 1,
                    CanBuyPencilMidLevel = true
                },
                ClassId.ChaosMonk => new ClassSnapshot
                {
                    Id = id,
                    Playable = true,
                    HP = 8,
                    Pencil = 9,
                    ItemSlots = 2,
                    RerollTokens = 2,
                    CanBuyPencilMidLevel = true
                },
                _ => Build(ClassId.NumberFreak)
            };
        }
    }
}
