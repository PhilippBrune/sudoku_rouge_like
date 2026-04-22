using UnityEngine;
using UnityEngine.SceneManagement;

namespace SudokuRoguelike.Bootstrap
{
    /// <summary>
    /// Bootstrap for a dedicated "Splash" scene (build index 0).
    /// Place this on any GameObject in the Splash scene. After the
    /// logo animation completes, it loads the main game scene by name.
    ///
    /// Unity editor setup required:
    ///   1. Create a new scene named "Splash" (File > New Scene).
    ///   2. Add an empty GameObject and attach this component.
    ///   3. Set <see cref="mainSceneName"/> in the Inspector (default: "Prototype").
    ///   4. Open Build Settings, drag Splash to index 0 and the main scene to index 1.
    ///   5. In GameBootstrap, the inline splash is automatically skipped when
    ///      <see cref="HasShownSplash"/> is true.
    /// </summary>
    public sealed class SplashSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private string mainSceneName = "Prototype";

        /// <summary>
        /// Set to true by this bootstrap so GameBootstrap knows the splash
        /// already ran in a prior scene and should not show it inline.
        /// </summary>
        public static bool HasShownSplash { get; private set; }

        private void Start()
        {
            HasShownSplash = false; // reset so it's only true after Show() completes
            var splashGo = new GameObject("SplashScreenController");
            var splash = splashGo.AddComponent<SplashScreenController>();
            splash.Initialize(transform);
            splash.Show(() =>
            {
                HasShownSplash = true;
                SceneManager.LoadScene(mainSceneName);
            });
        }
    }
}
