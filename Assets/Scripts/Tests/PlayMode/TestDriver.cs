using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Base class for all PlayMode tests. Provides shared setup/teardown,
/// helper assertions, and access to game services via Bootstrap.
/// </summary>
public abstract class TestDriver
{
    /// <summary>Maximum seconds a single test step may run before timeout.</summary>
    protected const float DefaultTimeout = 10f;

    [UnitySetUp]
    public IEnumerator BaseSetUp()
    {
        // Wait one frame so Unity finishes any pending scene loads
        yield return null;

        // Subclasses override OnSetUp for custom init
        yield return OnSetUp();
    }

    [UnityTearDown]
    public IEnumerator BaseTearDown()
    {
        yield return OnTearDown();
        yield return null;
    }

    /// <summary>Override to add per-test setup logic.</summary>
    protected virtual IEnumerator OnSetUp() { yield break; }

    /// <summary>Override to add per-test teardown logic.</summary>
    protected virtual IEnumerator OnTearDown() { yield break; }

    // ── Helper assertions ──────────────────────────────────────────────────

    /// <summary>Assert a service/component is available via FindAnyObjectByType.</summary>
    protected T AssertServiceExists<T>() where T : Object
    {
        var svc = Object.FindAnyObjectByType<T>();
        Assert.IsNotNull(svc, $"Expected service {typeof(T).Name} to exist in scene");
        return svc;
    }

    /// <summary>Wait up to <paramref name="seconds"/> for a condition to become true.</summary>
    protected IEnumerator WaitUntil(System.Func<bool> condition, float seconds = DefaultTimeout, string message = null)
    {
        float elapsed = 0f;
        while (!condition() && elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        Assert.IsTrue(condition(), message ?? $"Condition not met within {seconds}s");
    }

    /// <summary>Wait a fixed number of frames.</summary>
    protected IEnumerator WaitFrames(int count)
    {
        for (int i = 0; i < count; i++)
            yield return null;
    }
}
