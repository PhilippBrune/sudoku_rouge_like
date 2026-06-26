using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SudokuRoguelike.Tests
{
    [TestFixture]
    public class GeneratedUiLocalizationCoverageTests
    {
        private const string MainMenuBlueprintBuilderPath = "Assets/Scripts/UI/MainMenuBlueprintBuilder.cs";
        private const string MainMenuChallengePanelsPath = "Assets/Scripts/UI/MainMenuBlueprintBuilder.ChallengePanels.cs";
        private const string InRunUiBlueprintBuilderPath = "Assets/Scripts/UI/InRunUiBlueprintBuilder.cs";
        private const string MainMenuControllerPath = "Assets/Scripts/UI/MainMenuController.cs";
        private const string InRunControllerPath = "Assets/Scripts/UI/InRunController.cs";
        private const string BossGateViewControllerPath = "Assets/Scripts/UI/BossGateViewController.cs";
        private const string RewardViewControllerPath = "Assets/Scripts/UI/RewardViewController.cs";
        private const string HudViewControllerPath = "Assets/Scripts/UI/HudViewController.cs";
        private const string ShopViewControllerPath = "Assets/Scripts/UI/ShopViewController.cs";
        private const string CursePanelControllerPath = "Assets/Scripts/UI/CursePanelController.cs";
        private const string EndScreenViewControllerPath = "Assets/Scripts/UI/EndScreenViewController.cs";
        private const string ItemsMenuControllerPath = "Assets/Scripts/UI/ItemsMenuController.cs";
        private const string MetaProgressionPanelControllerPath = "Assets/Scripts/UI/MetaProgressionPanelController.cs";
        private const string EndScreenPresenterPath = "Assets/Scripts/UI/EndScreenPresenter.cs";

        private static readonly HashSet<string> KnownGeneratedUiStringDebt = new HashSet<string>();

        private static readonly HashSet<string> ApprovedLiteralUiText = new HashSet<string>
        {
            Debt(MainMenuBlueprintBuilderPath, "BuildText", "\"Run of the Nine\"")
        };

        [Test]
        public void GeneratedUiVisibleText_DoesNotAddUntrackedHardcodedStrings()
        {
            var rawTextCalls = FindRawGeneratedUiTextCalls(
                    MainMenuBlueprintBuilderPath,
                    MainMenuChallengePanelsPath,
                    InRunUiBlueprintBuilderPath,
                    MainMenuControllerPath,
                    InRunControllerPath,
                    BossGateViewControllerPath,
                    RewardViewControllerPath,
                    HudViewControllerPath,
                    ShopViewControllerPath,
                    CursePanelControllerPath,
                    EndScreenViewControllerPath,
                    ItemsMenuControllerPath,
                    MetaProgressionPanelControllerPath)
                .Concat(FindRawDirectPresenterStrings(
                    MetaProgressionPanelControllerPath,
                    EndScreenPresenterPath))
                .Where(call => !IsNonLocalizedChrome(call.Expression))
                .ToList();

            var rawDebtIds = rawTextCalls
                .Select(call => call.Id)
                .Where(id => !ApprovedLiteralUiText.Contains(id))
                .ToArray();

            var unexpected = rawTextCalls
                .Where(call => !KnownGeneratedUiStringDebt.Contains(call.Id)
                    && !ApprovedLiteralUiText.Contains(call.Id))
                .Select(call => $"{call.Path}:{call.Line} {call.Api} {call.Expression}")
                .ToArray();

            var staleAllowlist = KnownGeneratedUiStringDebt
                .Where(id => !rawDebtIds.Contains(id))
                .OrderBy(id => id)
                .ToArray();

            Assert.IsEmpty(
                unexpected,
                "New raw generated UI strings must be localized with T(...)/LocalizationService.Format(...) or added to the known-debt allowlist with a follow-up plan.");

            Assert.IsEmpty(
                staleAllowlist,
                "Known generated UI string debt no longer matches source. Remove stale entries after localization work lands.");
        }

        private static IEnumerable<UiTextCall> FindRawGeneratedUiTextCalls(params string[] paths)
        {
            foreach (var path in paths)
            {
                var text = File.ReadAllText(path);
                foreach (var call in FindCalls(path, text))
                {
                    if (!IsRawStringExpression(call.Expression))
                        continue;

                    yield return call;
                }
            }
        }

        private static IEnumerable<UiTextCall> FindCalls(string path, string text)
        {
            foreach (var api in new[] { "BuildText", "BuildButton", "BuildToggle", "CreateText", "CreatePanelButton", "CreateActionButton", "CreateOverlayPanel", "SetStatus", "AddHeader", "AddIconEntry" })
            {
                var searchIndex = 0;
                while (searchIndex < text.Length)
                {
                    var apiIndex = text.IndexOf(api, searchIndex, StringComparison.Ordinal);
                    if (apiIndex < 0)
                        break;

                    searchIndex = apiIndex + api.Length;

                    if (!IsIdentifierBoundary(text, apiIndex - 1)
                        || !IsIdentifierBoundary(text, apiIndex + api.Length))
                    {
                        continue;
                    }

                    var openParen = SkipWhitespace(text, apiIndex + api.Length);
                    if (openParen >= text.Length || text[openParen] != '(')
                        continue;

                    var closeParen = FindMatchingParen(text, openParen);
                    if (closeParen < 0)
                        continue;

                    var args = SplitTopLevelArguments(text.Substring(openParen + 1, closeParen - openParen - 1));
                    foreach (var visibleArgIndex in GetVisibleArgumentIndices(api))
                    {
                        if (args.Count <= visibleArgIndex)
                            continue;

                        var expression = NormalizeExpression(args[visibleArgIndex]);
                        yield return new UiTextCall(path, api, expression, GetLineNumber(text, apiIndex));
                    }
                    searchIndex = closeParen + 1;
                }
            }
        }

        private static IEnumerable<UiTextCall> FindRawDirectPresenterStrings(params string[] paths)
        {
            foreach (var path in paths)
            {
                var lines = File.ReadAllLines(path);
                for (var i = 0; i < lines.Length; i++)
                {
                    var trimmed = lines[i].Trim();
                    var expression = FindDirectVisibleStringExpression(trimmed);
                    if (expression == null || !IsRawStringExpression(expression))
                        continue;

                    yield return new UiTextCall(path, "DirectVisibleText", expression, i + 1);
                }
            }
        }

        private static string FindDirectVisibleStringExpression(string trimmedLine)
        {
            if (trimmedLine.StartsWith("return \"", StringComparison.Ordinal)
                || trimmedLine.StartsWith("return $\"", StringComparison.Ordinal)
                || trimmedLine.StartsWith("return @\"", StringComparison.Ordinal)
                || trimmedLine.StartsWith("return $@\"", StringComparison.Ordinal)
                || trimmedLine.StartsWith("return @$\"", StringComparison.Ordinal))
            {
                return NormalizeExpression(trimmedLine.Substring("return ".Length).TrimEnd(';'));
            }

            foreach (var token in new[] { ".text = ", "Append(", "AppendLine(" })
            {
                var index = trimmedLine.IndexOf(token, StringComparison.Ordinal);
                if (index < 0)
                    continue;

                var expression = trimmedLine.Substring(index + token.Length).Trim();
                if (expression.EndsWith(");", StringComparison.Ordinal))
                    expression = expression.Substring(0, expression.Length - 2);
                else if (expression.EndsWith(";", StringComparison.Ordinal))
                    expression = expression.Substring(0, expression.Length - 1);

                return NormalizeExpression(expression);
            }

            return null;
        }

        private static bool IsRawStringExpression(string expression)
        {
            return expression.StartsWith("\"", StringComparison.Ordinal)
                || expression.StartsWith("$\"", StringComparison.Ordinal)
                || expression.StartsWith("@\"", StringComparison.Ordinal)
                || expression.StartsWith("$@\"", StringComparison.Ordinal)
                || expression.StartsWith("@$\"", StringComparison.Ordinal);
        }

        private static bool IsNonLocalizedChrome(string expression)
        {
            return expression == "\"\""
                || expression == "\"<\""
                || expression == "\">\""
                || expression == "\"0\""
                || expression == "\"(Q)\""
                || expression == "\"(O)\""
                || expression == "\"(?)\""
                || expression == "\"?\""
                || expression == "\"???\""
                || expression == "\"\\u2713\""
                || expression == "\"\u2713\"";
        }

        private static IReadOnlyList<int> GetVisibleArgumentIndices(string api)
        {
            switch (api)
            {
                case "SetStatus":
                case "AddHeader":
                    return new[] { 0 };
                case "AddIconEntry":
                    return new[] { 1, 2 };
                case "CreatePanelButton":
                case "CreateActionButton":
                    return new[] { 4 };
                case "CreateOverlayPanel":
                    return new[] { 2 };
                default:
                    return new[] { 2 };
            }
        }

        private static bool IsIdentifierBoundary(string text, int index)
        {
            if (index < 0 || index >= text.Length)
                return true;

            return !char.IsLetterOrDigit(text[index]) && text[index] != '_';
        }

        private static int SkipWhitespace(string text, int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;

            return index;
        }

        private static int FindMatchingParen(string text, int openParenIndex)
        {
            var depth = 0;
            var inString = false;
            var verbatimString = false;

            for (var i = openParenIndex; i < text.Length; i++)
            {
                var current = text[i];

                if (inString)
                {
                    if (current == '"' && verbatimString && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }

                    if (current == '"' && (verbatimString || !IsEscaped(text, i)))
                    {
                        inString = false;
                        verbatimString = false;
                    }

                    continue;
                }

                if (current == '"' || (current == '@' && i + 1 < text.Length && text[i + 1] == '"'))
                {
                    verbatimString = current == '@';
                    inString = true;
                    if (current == '@')
                        i++;
                    continue;
                }

                if (current == '(')
                {
                    depth++;
                    continue;
                }

                if (current == ')')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }

            return -1;
        }

        private static List<string> SplitTopLevelArguments(string argumentText)
        {
            var args = new List<string>();
            var start = 0;
            var parenDepth = 0;
            var braceDepth = 0;
            var bracketDepth = 0;
            var inString = false;
            var verbatimString = false;

            for (var i = 0; i < argumentText.Length; i++)
            {
                var current = argumentText[i];

                if (inString)
                {
                    if (current == '"' && verbatimString && i + 1 < argumentText.Length && argumentText[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }

                    if (current == '"' && (verbatimString || !IsEscaped(argumentText, i)))
                    {
                        inString = false;
                        verbatimString = false;
                    }

                    continue;
                }

                if (current == '"' || (current == '@' && i + 1 < argumentText.Length && argumentText[i + 1] == '"'))
                {
                    verbatimString = current == '@';
                    inString = true;
                    if (current == '@')
                        i++;
                    continue;
                }

                switch (current)
                {
                    case '(':
                        parenDepth++;
                        break;
                    case ')':
                        parenDepth--;
                        break;
                    case '{':
                        braceDepth++;
                        break;
                    case '}':
                        braceDepth--;
                        break;
                    case '[':
                        bracketDepth++;
                        break;
                    case ']':
                        bracketDepth--;
                        break;
                    case ',' when parenDepth == 0 && braceDepth == 0 && bracketDepth == 0:
                        args.Add(argumentText.Substring(start, i - start));
                        start = i + 1;
                        break;
                }
            }

            args.Add(argumentText.Substring(start));
            return args;
        }

        private static bool IsEscaped(string text, int quoteIndex)
        {
            var slashCount = 0;
            for (var i = quoteIndex - 1; i >= 0 && text[i] == '\\'; i--)
                slashCount++;

            return slashCount % 2 == 1;
        }

        private static string NormalizeExpression(string expression)
        {
            var builder = new StringBuilder();
            var previousWasWhitespace = false;

            foreach (var current in expression.Trim())
            {
                if (char.IsWhiteSpace(current))
                {
                    if (!previousWasWhitespace)
                    {
                        builder.Append(' ');
                        previousWasWhitespace = true;
                    }

                    continue;
                }

                builder.Append(current);
                previousWasWhitespace = false;
            }

            return builder.ToString();
        }

        private static int GetLineNumber(string text, int index)
        {
            var line = 1;
            for (var i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n')
                    line++;
            }

            return line;
        }

        private static string Debt(string path, string api, string expression)
        {
            return $"{path}|{api}|{expression}";
        }

        private readonly struct UiTextCall
        {
            public UiTextCall(string path, string api, string expression, int line)
            {
                Path = path;
                Api = api;
                Expression = expression;
                Line = line;
                Id = Debt(path, api, expression);
            }

            public string Path { get; }
            public string Api { get; }
            public string Expression { get; }
            public int Line { get; }
            public string Id { get; }
        }
    }
}
