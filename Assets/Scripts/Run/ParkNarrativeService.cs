using SudokuRoguelike.Core;

namespace SudokuRoguelike.Run
{
    /// <summary>
    /// Provides floor-themed flavor text for each node type.
    /// All text is keyed by (NodeType, floorIndex 0-4).
    /// Display: 2-3 lines at the top of the node screen, fading after ~4 seconds.
    /// </summary>
    public static class ParkNarrativeService
    {
        // ── Floor entry lines (shown when a new floor begins) ──────────────────

        private static readonly string[] FloorEntryLines =
        {
            "The iron gate creaks. The park opens up ahead — paths branching, benches waiting, the sound of leaves.",
            "The path dips toward the pond. Ducks argue softly somewhere past the reeds.",
            "The canopy thickens. Old roots have lifted the stones into ridges underfoot.",
            "The fountain plaza opens out — pigeons, noise, people. You breathe through it.",
            "Quieter now. The far bench is visible through the last line of trees."
        };

        // ── Node flavor text (keyed by floor 0-4) ──────────────────────────────

        private static readonly string[][] PuzzleLines =
        {
            new[] { "You keep moving. The path ahead is clear.", "A straight stretch of gravel. You focus." },
            new[] { "The pond reflects the sky. You work as you walk.", "The bridge ahead — you'll cross it." },
            new[] { "Roots underfoot. You find your footing and push on.", "The grove is quiet. You think." },
            new[] { "The crowd noise fades behind you. One step at a time.", "The fountain's white noise helps." },
            new[] { "Almost there. The path narrows toward the gate.", "Final stretch. You settle into it." }
        };

        private static readonly string[][] ElitePuzzleLines =
        {
            new[] { "A steep shortcut through the overgrowth. Worth a try.", "The path is rougher here — and faster." },
            new[] { "The bank drops away sharply. The scenic route is longer.", "You take the harder way around the pond." },
            new[] { "An overgrown trail forks off the main path. You follow it.", "Dense shade and silence." },
            new[] { "A side alley cuts through the plaza — busier, noisier, shorter.", "You push through." },
            new[] { "A narrow gap in the hedge. You can just fit.", "Almost — push through." }
        };

        private static readonly string[][] ShopLines =
        {
            new[] { "A cheerful vendor with a striped awning. The coffee smells like the right thing.", "\"What can I get you?\"" },
            new[] { "The cart is parked by the pond. The vendor waves you over.", "\"Special today: whatever helps most.\"" },
            new[] { "Tucked between two oaks — the familiar cart.", "\"Busy day? I've seen a lot of people come through.\"" },
            new[] { "The plaza vendor again, set up near the fountain.", "\"You look like you need this.\"" },
            new[] { "One last stop. The cart is almost out of stock.", "\"Last one. Make it count.\"" }
        };

        private static readonly string[][] RestLines =
        {
            new[] { "The morning air is cool. A pigeon watches you from a fence post.", "You sit for a moment." },
            new[] { "The pond catches the light. Somewhere, a duck argues about nothing.", "You rest your feet." },
            new[] { "Old roots have lifted the path into small ridges. You rest your feet.", "The birdsong is constant here." },
            new[] { "The fountain masks the noise from the plaza. You almost relax.", "You breathe." },
            new[] { "Almost there. The bench is worn smooth from years of use.", "You sit. Just for a moment." }
        };

        private static readonly string[][] EventLines =
        {
            new[] { "Someone crosses your path.", "The park holds its usual surprises." },
            new[] { "By the water's edge, something catches your eye.", "The pond gives nothing away." },
            new[] { "Under the old trees — something unexpected.", "The grove has its own rules." },
            new[] { "In the thick of the plaza, someone stops you.", "The noise doesn't help." },
            new[] { "Near the end, a brief detour.", "The park makes one last offer." }
        };

        private static readonly string[][] BossGateLines =
        {
            new[] { "You hear it before you see it — The Playground.", "You could loop around the quieter side..." },
            new[] { "Music and confetti — The Wedding Party.", "A gap in the crowd, or straight through?" },
            new[] { "The market spills across the whole path — The Street Market.", "Busy. Loud. Worth it?" },
            new[] { "Chanting from the plaza — The Protest March.", "Every way through has a cost." },
            new[] { "The band is playing at full volume — The Festival Stage.", "This is the last gate. No way around." }
        };

        private static readonly string[][] BossLines =
        {
            new[] { "Children scatter across the path, joyful and chaotic.", "You find your way through the noise." },
            new[] { "Laughter, music, confetti. The group doesn't move.", "You settle in and wait for a gap." },
            new[] { "Vendors call from every side. Carts block the pavement.", "You navigate carefully." },
            new[] { "The chant fills the plaza. No way around.", "You move with the current." },
            new[] { "The music is loud. It fills every space.", "You solve through it. That's all there is." }
        };

        private static readonly string[] RelicLines =
        {
            "Something glints in the grass — someone dropped it.",
            "A small thing, half-buried by the path. Someone's treasure.",
            "Among the roots, something catches the light.",
            "Left on a bench — intentional, or forgotten?",
            "At the far edge of the path, near the gate: something small and useful."
        };

        private static readonly string[] RunEndVictory =
        {
            "The far gate is ahead. The park stretches behind you — every path you took, every pause, every noise you pushed through.",
            "You made it through."
        };

        private static readonly string[] RunEndDefeat =
        {
            "You sit down. Not on a bench — just on a low wall by the path. The park continues without you.",
            "Next time, maybe."
        };

        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>Returns a 1-2 line narrative string for the given node type and floor.</summary>
        public static string GetNodeFlavor(NodeType nodeType, int floorIndex)
        {
            var f = System.Math.Clamp(floorIndex, 0, 4);
            string[] lines;

            switch (nodeType)
            {
                case NodeType.Puzzle:       lines = PuzzleLines[f]; break;
                case NodeType.ElitePuzzle:  lines = ElitePuzzleLines[f]; break;
                case NodeType.Shop:         lines = ShopLines[f]; break;
                case NodeType.Rest:         lines = RestLines[f]; break;
                case NodeType.Event:        lines = EventLines[f]; break;
                case NodeType.Boss:         lines = BossLines[f]; break;
                case NodeType.PreBoss:      lines = BossGateLines[f]; break;
                case NodeType.Relic:        return RelicLines[f];
                default:                    return string.Empty;
            }

            return lines != null && lines.Length >= 2
                ? $"{lines[0]}\n{lines[1]}"
                : lines != null && lines.Length == 1 ? lines[0] : string.Empty;
        }

        /// <summary>Returns the flavor text shown when the player enters a new floor.</summary>
        public static string GetFloorEntryFlavor(int floorIndex)
        {
            var f = System.Math.Clamp(floorIndex, 0, 4);
            return FloorEntryLines[f];
        }

        /// <summary>Returns the boss gate dialog lines for a given floor (for the BossGate choice panel header).</summary>
        public static string GetBossGateFlavor(int floorIndex)
        {
            var f = System.Math.Clamp(floorIndex, 0, 4);
            var lines = BossGateLines[f];
            return lines != null && lines.Length >= 2 ? $"{lines[0]}\n{lines[1]}" : string.Empty;
        }

        public static string GetRunEndFlavor(bool victory)
        {
            var lines = victory ? RunEndVictory : RunEndDefeat;
            return $"{lines[0]}\n\n{lines[1]}";
        }
    }
}
