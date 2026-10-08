using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SudokuRoguelike.Tests
{
    /// <summary>
    /// Source-contract tests check a class, not a file. Large controllers are split into partial files
    /// (X.cs plus X.Topic.cs, see docs/controller-extraction-plan.md), so reading only X.cs would make a
    /// contract fail, or silently stop checking, whenever code moves into a focused partial file.
    /// </summary>
    internal static class SourceText
    {
        /// <summary>Text of <paramref name="path"/> followed by its partial siblings (X.*.cs).</summary>
        public static string ReadClass(string path)
        {
            var builder = new StringBuilder();
            foreach (var file in WithPartials(path))
                builder.AppendLine(File.ReadAllText(file));
            return builder.ToString();
        }

        /// <summary><paramref name="path"/> and every X.*.cs file next to it, main file first.</summary>
        public static IEnumerable<string> WithPartials(string path)
        {
            yield return path;
            var directory = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(directory) || !path.EndsWith(".cs") || !Directory.Exists(directory))
                yield break;

            foreach (var partial in Directory.GetFiles(directory, name + ".*.cs").OrderBy(p => p))
                yield return partial.Replace('\\', '/');
        }
    }
}
