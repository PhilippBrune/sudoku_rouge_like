using System.Collections.Generic;
using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// Provides floor-themed flavor text for each node type.
    /// All text is keyed by (NodeType, floorIndex 0-4).
    /// Display: 2-3 lines at the top of the node screen, fading after about 4 seconds.
    /// </summary>
    public static class ParkNarrativeService
    {
        private static readonly string[] FloorEntryLines =
        {
            "The bamboo gate opens with a dry clack. Raked sand curves ahead, each path asking for a steadier hand.",
            "Moss softens the stones around the koi pond. Lantern light gathers in the water.",
            "Wooden bridges cross black water. Orange koi turn below like living brushstrokes.",
            "Stone lanterns wake one by one. The gravel path glows amber under drifting leaves.",
            "The torii at the summit waits in mist. Every bell note sounds closer than it should."
        };

        private static readonly string[][] PuzzleLines =
        {
            new[] { "You step between bamboo shadows. The grid settles like raked sand.", "A clean path opens. You breathe and solve." },
            new[] { "Koi circle beneath the bridge. Their turns mark a quiet rhythm.", "Numbers fall into place like stones in moss." },
            new[] { "Water reflects the board in broken fragments.", "You follow the pattern until the surface stills." },
            new[] { "Lantern light divides the path into warm squares.", "Each correct digit feels like a wick catching flame." },
            new[] { "Mist curls around the shrine steps.", "The last garden asks for patience, not speed." }
        };

        private static readonly string[][] ElitePuzzleLines =
        {
            new[] { "A narrow service path slips behind the bamboo screen.", "The spirits watch the shortcut closely." },
            new[] { "The moss path breaks into slick stepping stones.", "One careless move will echo across the pond." },
            new[] { "A moon bridge arches higher than it should.", "You climb into a harder pattern." },
            new[] { "A lantern-lit side trail burns brighter than the main way.", "The reward is closer, and so is the risk." },
            new[] { "A hidden stair rises beside the torii.", "The summit spirits do not mark easy routes." }
        };

        private static readonly string[][] ShopLines =
        {
            new[] { "A fox-mask keeper waits beside a tray of paper charms.", "\"Choose carefully. The garden remembers debt.\"" },
            new[] { "A lacquered stall rests by the koi pond.", "\"Tools for patient hands, prices for honest ones.\"" },
            new[] { "Under the bridge, a spirit merchant unfolds a silk cloth.", "\"Rare things drift here when no one is looking.\"" },
            new[] { "Lantern smoke curls around a quiet shrine shop.", "\"Spend what you carried. Keep what carries you.\"" },
            new[] { "At the final torii, the keeper opens one last box.", "\"No refunds beyond this gate.\"" }
        };

        private static readonly string[][] RestLines =
        {
            new[] { "A flat meditation stone sits warm in the courtyard.", "You rest until the bamboo stops swaying." },
            new[] { "Moss muffles every footstep.", "For a moment, even the pond holds still." },
            new[] { "You kneel beside the bridge and let the koi pass below.", "Their patience returns some of yours." },
            new[] { "A lantern niche offers shelter from the wind.", "You count your breath with the flame." },
            new[] { "The shrine bell rope hangs within reach.", "You do not ring it yet. You rest." }
        };

        private static readonly string[][] EventLines =
        {
            new[] { "A paper talisman lifts from the sand without wind.", "The garden offers a choice." },
            new[] { "A ripple crosses the pond against the current.", "Something below has noticed you." },
            new[] { "The bridge shadow lengthens into the shape of a gate.", "Not every crossing is visible." },
            new[] { "One lantern burns blue among the amber lights.", "Its flame bends toward your hand." },
            new[] { "At the summit, a sealed ema plaque turns over by itself.", "The last wish is not written in ink." }
        };

        private static readonly string[][] CursedLines =
        {
            new[] { "Black moss threads through the raked sand.", "The cursed route promises a sharper blessing if you do not look away." },
            new[] { "A koi with clouded scales circles the pond edge.", "The water offers strength with a shadow tied to it." },
            new[] { "The bridge boards creak in a rhythm that does not match your steps.", "Crossing here means carrying the echo forward." },
            new[] { "One lantern burns with smoke instead of flame.", "Its curse will follow until the pattern is satisfied." },
            new[] { "A cracked torii charm turns cold in your hand.", "The summit accepts risk only when it is named." }
        };

        private static readonly string[][] BossGateLines =
        {
            new[] { "A bamboo spirit rattles the grove ahead.", "The quiet route bends away; the direct path earns a sharper blessing." },
            new[] { "The koi pond darkens beneath a watching mask.", "You can circle the water, or cross where the ripples gather." },
            new[] { "A bridge guardian blocks the terrace.", "The safer stones are slower. The bright stones demand faith." },
            new[] { "Lantern flames lean toward the same unseen breath.", "Every path through the glow has a cost." },
            new[] { "The shrine gate seals behind you.", "This is the final threshold. No way around." }
        };

        private static readonly string[][] BossLines =
        {
            new[] { "Bamboo stalks snap in patterns no wind could make.", "You solve before the grove changes its mind." },
            new[] { "The pond spirit stirs under black water.", "Every wrong step sends rings across the grid." },
            new[] { "The bridge guardian lowers its paper fan.", "Only a complete pattern earns passage." },
            new[] { "Lanterns flare into a wall of gold and shadow.", "You solve through the heat of their gaze." },
            new[] { "The summit spirit waits beyond the torii.", "The final board is an offering, and it will not accept haste." }
        };

        private static readonly string[] RelicLines =
        {
            "An old charm glints between the bamboo roots.",
            "A lacquered token rests on the moss, dry despite the pond mist.",
            "Something bright turns beneath the bridge boards.",
            "A lantern niche hides a relic wrapped in faded cord.",
            "At the shrine threshold, a small offering waits for your hand."
        };

        private static readonly string[] RunEndVictory =
        {
            "The summit bell answers without being struck. Behind you, every garden path settles into stillness.",
            "You pass through the torii with the pattern complete."
        };

        private static readonly string[] RunEndDefeat =
        {
            "The lanterns dim one by one. The garden keeps the unfinished pattern.",
            "You will be allowed to enter again."
        };

        private static readonly ClassId[] NarrativeClassOrder =
        {
            ClassId.NumberFreak,
            ClassId.GardenMonk,
            ClassId.ShrineArchivist,
            ClassId.KoiGambler,
            ClassId.StoneGardener,
            ClassId.LanternSeer,
            ClassId.ReedDuelist,
            ClassId.QuietCartographer
        };

        private static readonly Dictionary<ClassId, string> ClassIntroLines = new Dictionary<ClassId, string>
        {
            { ClassId.NumberFreak, "You count the sand-ridges before the gate finishes opening. Patterns answer when you pressure them gently." },
            { ClassId.GardenMonk, "You set one palm on warm stone. The garden's breath steadies with yours." },
            { ClassId.ShrineArchivist, "Scroll cases whisper beneath your sleeve. Every candidate mark feels like a recovered inscription." },
            { ClassId.KoiGambler, "A gold koi breaks the surface as you pass. Risk follows you like a lucky ripple." },
            { ClassId.StoneGardener, "Your chisel rests heavy at your belt. Broken paths can still be shaped into order." },
            { ClassId.LanternSeer, "The lantern before you burns with a second shadow. Hidden rules show their edges early." },
            { ClassId.ReedDuelist, "You step lightly, reed blade low. A clean solve is a vow, not a flourish." },
            { ClassId.QuietCartographer, "Your map is blank where the garden is alive. You mark routes by listening before moving." }
        };

        private static readonly Dictionary<ClassId, string> ClassVictoryLines = new Dictionary<ClassId, string>
        {
            { ClassId.NumberFreak, "The final pattern leaves no remainder; even the bell seems to count you home." },
            { ClassId.GardenMonk, "The garden releases its breath with yours, and the last wound closes into quiet." },
            { ClassId.ShrineArchivist, "You bind the completed grid into memory; the archive has room for one more true route." },
            { ClassId.KoiGambler, "The koi turns gold beneath the torii. This time, the wager lands in your hand." },
            { ClassId.StoneGardener, "Stone, moss, and number settle into one shape. Nothing loose remains to mend." },
            { ClassId.LanternSeer, "Every hidden rule burns clear in the lantern glass, and the summit lets you pass." },
            { ClassId.ReedDuelist, "Your final stroke is exact. The reeds bow without touching the water." },
            { ClassId.QuietCartographer, "The living map completes itself behind you, one calm path at a time." }
        };

        private static readonly Dictionary<ClassId, string> ClassDefeatLines = new Dictionary<ClassId, string>
        {
            { ClassId.NumberFreak, "One count slipped from the pattern, but the unanswered digit stays in reach." },
            { ClassId.GardenMonk, "Your breath breaks before the gate, then returns; patience will carry you back." },
            { ClassId.ShrineArchivist, "The archive closes on an unfinished page. Its margin waits for your next hand." },
            { ClassId.KoiGambler, "The gold koi dives before you can call the wager. Another ripple is already forming." },
            { ClassId.StoneGardener, "The path fractures under your step. You remember where the repair must begin." },
            { ClassId.LanternSeer, "The lantern clouds over, but one ember keeps the hidden rule warm." },
            { ClassId.ReedDuelist, "The reed bends instead of breaking. The vow can be taken again." },
            { ClassId.QuietCartographer, "The map blurs at the edge of the garden. You still know where the first stone lies." }
        };

        private static string T(string key, string fallback) => LocalizationService.T(key, fallback);

        private static string LineKey(string group, int floor, int line) => $"Narrative.{group}.{floor}.{line}";

        private static string SingleKey(string group, int index) => $"Narrative.{group}.{index}";

        private static string ClassKey(string group, ClassId classId) => $"Narrative.{group}.{classId}";

        private static string JoinLocalizedLines(string group, int floor, string[] lines)
        {
            if (lines == null || lines.Length == 0)
                return string.Empty;

            if (lines.Length == 1)
                return T(LineKey(group, floor, 0), lines[0]);

            return $"{T(LineKey(group, floor, 0), lines[0])}\n{T(LineKey(group, floor, 1), lines[1])}";
        }

        public static IEnumerable<string> GetLocalizationKeys()
        {
            for (var i = 0; i < FloorEntryLines.Length; i++)
                yield return SingleKey("FloorEntry", i);

            foreach (var key in GetLineKeys("Node.Puzzle", PuzzleLines)) yield return key;
            foreach (var key in GetLineKeys("Node.ElitePuzzle", ElitePuzzleLines)) yield return key;
            foreach (var key in GetLineKeys("Node.Shop", ShopLines)) yield return key;
            foreach (var key in GetLineKeys("Node.Rest", RestLines)) yield return key;
            foreach (var key in GetLineKeys("Node.Event", EventLines)) yield return key;
            foreach (var key in GetLineKeys("Node.Cursed", CursedLines)) yield return key;
            foreach (var key in GetLineKeys("Node.PreBoss", BossGateLines)) yield return key;
            foreach (var key in GetLineKeys("Node.Boss", BossLines)) yield return key;

            for (var i = 0; i < RelicLines.Length; i++)
                yield return SingleKey("Node.Relic", i);

            yield return LineKey("RunEnd.Victory", 0, 0);
            yield return LineKey("RunEnd.Victory", 0, 1);
            yield return LineKey("RunEnd.Defeat", 0, 0);
            yield return LineKey("RunEnd.Defeat", 0, 1);

            foreach (var classId in NarrativeClassOrder)
            {
                yield return ClassKey("ClassIntro", classId);
                yield return ClassKey("ClassRunEnd.Victory", classId);
                yield return ClassKey("ClassRunEnd.Defeat", classId);
            }
        }

        private static IEnumerable<string> GetLineKeys(string group, string[][] lines)
        {
            for (var floor = 0; floor < lines.Length; floor++)
            {
                for (var line = 0; line < lines[floor].Length; line++)
                    yield return LineKey(group, floor, line);
            }
        }

        private static string GetClassLine(Dictionary<ClassId, string> lines, ClassId classId)
        {
            return lines.TryGetValue(classId, out var line)
                ? line
                : lines[ClassId.NumberFreak];
        }

        /// <summary>Returns a 1-2 line narrative string for the given node type and floor.</summary>
        public static string GetNodeFlavor(NodeType nodeType, int floorIndex)
        {
            var f = System.Math.Clamp(floorIndex, 0, 4);
            switch (nodeType)
            {
                case NodeType.Puzzle: return JoinLocalizedLines("Node.Puzzle", f, PuzzleLines[f]);
                case NodeType.ElitePuzzle: return JoinLocalizedLines("Node.ElitePuzzle", f, ElitePuzzleLines[f]);
                case NodeType.Shop: return JoinLocalizedLines("Node.Shop", f, ShopLines[f]);
                case NodeType.Rest: return JoinLocalizedLines("Node.Rest", f, RestLines[f]);
                case NodeType.Event: return JoinLocalizedLines("Node.Event", f, EventLines[f]);
                case NodeType.Cursed: return JoinLocalizedLines("Node.Cursed", f, CursedLines[f]);
                case NodeType.Boss: return JoinLocalizedLines("Node.Boss", f, BossLines[f]);
                case NodeType.PreBoss: return JoinLocalizedLines("Node.PreBoss", f, BossGateLines[f]);
                case NodeType.Relic: return T(SingleKey("Node.Relic", f), RelicLines[f]);
                default: return string.Empty;
            }
        }

        /// <summary>Returns the flavor text shown when the player enters a new floor.</summary>
        public static string GetFloorEntryFlavor(int floorIndex)
        {
            var f = System.Math.Clamp(floorIndex, 0, 4);
            return T(SingleKey("FloorEntry", f), FloorEntryLines[f]);
        }

        /// <summary>Returns the boss gate dialog lines for a given floor.</summary>
        public static string GetBossGateFlavor(int floorIndex)
        {
            var f = System.Math.Clamp(floorIndex, 0, 4);
            return JoinLocalizedLines("Node.PreBoss", f, BossGateLines[f]);
        }

        public static string GetRunEndFlavor(bool victory)
        {
            var lines = victory ? RunEndVictory : RunEndDefeat;
            var group = victory ? "RunEnd.Victory" : "RunEnd.Defeat";
            return $"{T(LineKey(group, 0, 0), lines[0])}\n\n{T(LineKey(group, 0, 1), lines[1])}";
        }

        public static string GetClassIntroFlavor(ClassId classId)
        {
            return T(ClassKey("ClassIntro", classId), GetClassLine(ClassIntroLines, classId));
        }

        public static string GetRunEndFlavor(bool victory, ClassId classId)
        {
            var group = victory ? "ClassRunEnd.Victory" : "ClassRunEnd.Defeat";
            var lines = victory ? ClassVictoryLines : ClassDefeatLines;
            return $"{GetRunEndFlavor(victory)}\n\n{T(ClassKey(group, classId), GetClassLine(lines, classId))}";
        }
    }
}
