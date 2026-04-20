using System.Collections.Generic;

namespace SudokuRoguelike.Items
{
    /// <summary>
    /// Describes the outcome of ApplyItemEffect so the UI knows what to render.
    /// </summary>
    public sealed class ItemEffectResult
    {
        public enum ResultKind { None, Message, BoardChanged, CellsFilled, Highlights, Hints, Reroll }

        public ResultKind Kind;
        public string Text;
        public List<(int row, int col)> Cells = new List<(int, int)>();

        public static readonly ItemEffectResult None = new ItemEffectResult { Kind = ResultKind.None };

        public static ItemEffectResult Message(string msg) =>
            new ItemEffectResult { Kind = ResultKind.Message, Text = msg };

        public static ItemEffectResult BoardChanged(string msg) =>
            new ItemEffectResult { Kind = ResultKind.BoardChanged, Text = msg };

        public static ItemEffectResult CellsFilled(List<(int, int)> cells, string msg) =>
            new ItemEffectResult { Kind = ResultKind.CellsFilled, Cells = cells, Text = msg };

        public static ItemEffectResult Highlights(List<(int, int)> cells, string msg) =>
            new ItemEffectResult { Kind = ResultKind.Highlights, Cells = cells, Text = msg };

        public static ItemEffectResult Hints(List<(int, int)> cells, string msg) =>
            new ItemEffectResult { Kind = ResultKind.Hints, Cells = cells, Text = msg };

        public static ItemEffectResult Reroll() =>
            new ItemEffectResult { Kind = ResultKind.Reroll, Text = "Item slots rerolled." };
    }
}
