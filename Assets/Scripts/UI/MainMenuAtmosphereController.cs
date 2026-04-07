using UnityEngine;

namespace SudokuRoguelike.UI
{
    public sealed class MainMenuAtmosphereController : MonoBehaviour
    {
        private ParticleSystem _particles;

        public void Initialize(float intensity)
        {
            if (intensity <= 0) return;

            var go = new GameObject("MenuParticles");
            go.transform.SetParent(transform);
            _particles = go.AddComponent<ParticleSystem>();

            var main = _particles.main;
            main.maxParticles = (int)(50 * intensity);
            main.startLifetime = 4f;
            main.startSpeed = 0.2f;
            main.startSize = 0.05f;
            main.startColor = new Color(1f, 0.95f, 0.85f, 0.3f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }

        public void SetIntensity(float intensity)
        {
            if (_particles == null) return;
            var main = _particles.main;
            main.maxParticles = (int)(50 * intensity);
        }
    }
}
