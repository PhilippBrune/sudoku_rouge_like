using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// E2E scenario system. Build a sequence of named steps, then run them.
/// Each step is a coroutine with built-in timeout and logging.
///
/// Usage:
///   var scenario = new ScenarioRunner("Boss modifier choice");
///   scenario.AddStep("Open boss gate", () => OpenGate());
///   scenario.AddStep("Choose modifier", () => ChooseModifier());
///   scenario.AddStep("Verify constraint active", () => VerifyConstraint());
///   yield return scenario.Run();
/// </summary>
public class ScenarioRunner
{
    public string Name { get; }
    public float DefaultStepTimeout { get; set; } = 10f;

    private readonly List<ScenarioStep> _steps = new();

    public ScenarioRunner(string name)
    {
        Name = name;
    }

    /// <summary>Add a step that returns an IEnumerator (coroutine).</summary>
    public void AddStep(string name, Func<IEnumerator> action, float? timeout = null)
    {
        _steps.Add(new ScenarioStep
        {
            Name = name,
            Action = action,
            Timeout = timeout ?? DefaultStepTimeout,
        });
    }

    /// <summary>Add a synchronous step (runs in one frame).</summary>
    public void AddStep(string name, Action action, float? timeout = null)
    {
        _steps.Add(new ScenarioStep
        {
            Name = name,
            Action = () => Wrap(action),
            Timeout = timeout ?? DefaultStepTimeout,
        });
    }

    /// <summary>Execute all steps in order. Fails fast on any assertion.</summary>
    public IEnumerator Run()
    {
        Debug.Log($"[Scenario] ▶ {Name} — {_steps.Count} steps");

        for (int i = 0; i < _steps.Count; i++)
        {
            var step = _steps[i];
            Debug.Log($"[Scenario]   [{i + 1}/{_steps.Count}] {step.Name}");

            float elapsed = 0f;
            var enumerator = step.Action();
            bool done = false;

            while (!done)
            {
                try
                {
                    done = !enumerator.MoveNext();
                }
                catch (Exception ex)
                {
                    Assert.Fail($"[Scenario] Step '{step.Name}' threw: {ex.Message}\n{ex.StackTrace}");
                    yield break;
                }

                if (!done)
                {
                    elapsed += Time.unscaledDeltaTime;
                    if (elapsed > step.Timeout)
                    {
                        Assert.Fail($"[Scenario] Step '{step.Name}' timed out after {step.Timeout}s");
                        yield break;
                    }
                    yield return enumerator.Current;
                }
            }

            Debug.Log($"[Scenario]   ✓ {step.Name} ({elapsed:F1}s)");
        }

        Debug.Log($"[Scenario] ✓ {Name} completed");
    }

    private static IEnumerator Wrap(Action action)
    {
        action();
        yield break;
    }

    private struct ScenarioStep
    {
        public string Name;
        public Func<IEnumerator> Action;
        public float Timeout;
    }
}
