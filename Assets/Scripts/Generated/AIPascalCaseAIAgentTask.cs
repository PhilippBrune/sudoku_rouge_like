using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class AITask : MonoBehaviour
{
    private const string reviewFilePattern = "*_review.md";
    private const string specOutputPath = "Assets/tickets/ai-spec/";
    private const string testOutputPath = "Assets/testcases/";
    private static int reqId = 1;

    void Start()
    {
        ProcessReviews();
    }

    void ProcessReviews()
    {
        string[] reviewFiles = Directory.GetFiles(Application.dataPath, reviewFilePattern);

        foreach (string file in reviewFiles)
        {
            ProcessReviewFile(file);
            reqId++;
        }
    }

    void ProcessReviewFile(string file)
    {
        string fileContent = File.ReadAllText(file);

        string specContent = GenerateSpec(fileContent, file);
        File.WriteAllText(Path.Combine(specOutputPath, Path.GetFileName(file) + "_spec.md"), specContent);

        string testContent = ExtractTestScenarios(fileContent);
        File.AppendAllText(Path.Combine(testOutputPath, "all_test_scenarios.md"), testContent);
    }

    string GenerateSpec(string fileContent, string file)
    {
        string spec = "# Requirement Specification\n\n";
        spec += $"Source Review File: {Path.GetFileName(file)}\n\n";

        string[] requirements = Regex.Split(fileContent, "## Requirements");

        foreach (string req in requirements)
        {
            string[] reqParts = req.Split(new[] { "\n", "---\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (reqParts.Length < 3)
                continue;

            spec += "## Requirements\n\n";
            spec += $"### REQ-{reqId.ToString("D4")}\n\n";
            spec += $"Title: {reqParts[0].Trim()}\n";
            spec += $"Description: {reqParts[1].Trim()}\n";
            spec += $"Related Systems: {reqParts[2].Trim()}\n";
            spec += $"Implementation Notes: {(reqParts.Length > 3 ? reqParts[3].Trim() : "N/A")}";
        }

        return spec;
    }

    string ExtractTestScenarios(string fileContent)
    {
        string testScenarios = "# Consolidated Test Scenarios\n\n";

        string[] scenarioParts = Regex.Split(fileContent, "## Test Scenarios");

        foreach (string scenario in scenarioParts)
        {
            string[] parts = scenario.Split(new[] { "\n", "---\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
                continue;

            testScenarios += "## Test Scenario\n\n";
            testScenarios += $"Source: {Path.GetFileName(file)}\n\n";
            testScenarios += $"Description: {parts[0].Trim()}\n";
            testScenarios += $"Steps: {parts[1].Trim()}\n";
            testScenarios += $"Expected Result: {(parts.Length > 2 ? parts[2].Trim() : "N/A")}\n";
        }

        return testScenarios;
    }
}