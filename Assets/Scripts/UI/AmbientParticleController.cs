using SudokuRoguelike.Core;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    /// <summary>
    /// Per-floor ambient particle effects using Unity's ParticleSystem.
    /// Respects ParticleIntensity setting (scales count + opacity) and ReduceMotion (disables entirely).
    /// Max 2 visual layers active simultaneously.
    /// </summary>
    public sealed class AmbientParticleController : MonoBehaviour
    {
        private readonly AccessibilityService _accessibility = new();
        private ParticleSystem _primarySystem;
        private ParticleSystem _secondarySystem;
        private int _currentFloor = -1;

        private void Awake()
        {
            _primarySystem = CreateParticleSystem("AmbientPrimary");
            _secondarySystem = CreateParticleSystem("AmbientSecondary");
        }

        /// <summary>
        /// Switch to the ambient particles for the given floor.
        /// </summary>
        public void SetFloor(int floorIndex)
        {
            floorIndex = Mathf.Clamp(floorIndex, 0, 4);
            if (floorIndex == _currentFloor) return;
            _currentFloor = floorIndex;

            _accessibility.Refresh();

            if (_accessibility.IsReduceMotion || _accessibility.ParticleIntensity <= 0f)
            {
                _primarySystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _secondarySystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }

            var intensity = _accessibility.ParticleIntensity;

            switch (floorIndex)
            {
                case 0: // Bamboo Courtyard — petals
                    ConfigureSystem(_primarySystem, FloorParticlePreset.Petals, intensity);
                    _secondarySystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    break;
                case 1: // Moss Garden — dust motes + rain dots
                    ConfigureSystem(_primarySystem, FloorParticlePreset.DustMotes, intensity);
                    ConfigureSystem(_secondarySystem, FloorParticlePreset.RainDots, intensity * 0.5f);
                    break;
                case 2: // Koi Terrace — water ripples
                    ConfigureSystem(_primarySystem, FloorParticlePreset.WaterRipples, intensity);
                    _secondarySystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    break;
                case 3: // Stone Lantern Walk — dust motes + lantern glow
                    ConfigureSystem(_primarySystem, FloorParticlePreset.DustMotes, intensity);
                    ConfigureSystem(_secondarySystem, FloorParticlePreset.LanternGlow, intensity * 0.7f);
                    break;
                case 4: // Shrine Summit — petals + light rays
                    ConfigureSystem(_primarySystem, FloorParticlePreset.Petals, intensity);
                    ConfigureSystem(_secondarySystem, FloorParticlePreset.LightRays, intensity * 0.6f);
                    break;
            }
        }

        private enum FloorParticlePreset
        {
            Petals,
            DustMotes,
            RainDots,
            WaterRipples,
            LanternGlow,
            LightRays
        }

        private static void ConfigureSystem(ParticleSystem ps, FloorParticlePreset preset, float intensity)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.loop = true;

            var emission = ps.emission;
            emission.enabled = true;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            shape.scale = new Vector3(12f, 8f, 1f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = GetDefaultParticleMaterial();

            switch (preset)
            {
                case FloorParticlePreset.Petals:
                    main.startLifetime = 6f;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
                    main.startColor = new Color(0.95f, 0.75f, 0.80f, 0.6f * intensity);
                    main.maxParticles = Mathf.RoundToInt(8 * intensity);
                    main.gravityModifier = 0.05f;
                    emission.rateOverTime = 1.5f * intensity;
                    break;

                case FloorParticlePreset.DustMotes:
                    main.startLifetime = 8f;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
                    main.startColor = new Color(1f, 1f, 0.9f, 0.35f * intensity);
                    main.maxParticles = Mathf.RoundToInt(6 * intensity);
                    main.gravityModifier = -0.01f;
                    emission.rateOverTime = 0.8f * intensity;
                    break;

                case FloorParticlePreset.RainDots:
                    main.startLifetime = 1.5f;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.03f);
                    main.startColor = new Color(0.7f, 0.8f, 0.9f, 0.25f * intensity);
                    main.maxParticles = Mathf.RoundToInt(20 * intensity);
                    main.gravityModifier = 0.3f;
                    emission.rateOverTime = 5f * intensity;
                    var rainVel = ps.velocityOverLifetime;
                    rainVel.enabled = true;
                    rainVel.y = new ParticleSystem.MinMaxCurve(-4f, -2f);
                    break;

                case FloorParticlePreset.WaterRipples:
                    main.startLifetime = 3f;
                    main.startSpeed = 0f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
                    main.startColor = new Color(0.6f, 0.8f, 1f, 0.3f * intensity);
                    main.maxParticles = Mathf.RoundToInt(4 * intensity);
                    main.gravityModifier = 0f;
                    emission.rateOverTime = 1f * intensity;
                    var sizeOL = ps.sizeOverLifetime;
                    sizeOL.enabled = true;
                    sizeOL.size = new ParticleSystem.MinMaxCurve(0.5f, AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1f));
                    break;

                case FloorParticlePreset.LanternGlow:
                    main.startLifetime = 4f;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.30f);
                    main.startColor = new Color(1f, 0.85f, 0.4f, 0.25f * intensity);
                    main.maxParticles = Mathf.RoundToInt(2 * intensity);
                    main.gravityModifier = -0.02f;
                    emission.rateOverTime = 0.5f * intensity;
                    break;

                case FloorParticlePreset.LightRays:
                    main.startLifetime = 5f;
                    main.startSpeed = 0f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
                    main.startColor = new Color(1f, 1f, 0.9f, 0.12f * intensity);
                    main.maxParticles = Mathf.RoundToInt(3 * intensity);
                    main.gravityModifier = 0f;
                    emission.rateOverTime = 0.6f * intensity;
                    var rotOL = ps.rotationOverLifetime;
                    rotOL.enabled = true;
                    rotOL.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
                    break;
            }

            if (!ps.isPlaying) ps.Play();
        }

        private ParticleSystem CreateParticleSystem(string name)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(transform, false);
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Disable default emission until configured
            var emission = ps.emission;
            emission.enabled = false;

            return ps;
        }

        private static Material GetDefaultParticleMaterial()
        {
            var shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("UI/Default");
            return new Material(shader);
        }
    }
}
