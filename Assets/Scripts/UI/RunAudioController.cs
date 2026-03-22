using System.Collections;
using SudokuRoguelike.Save;
using UnityEngine;

namespace SudokuRoguelike.UI
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class RunAudioController : MonoBehaviour
    {
        public enum Context
        {
            Path,
            Puzzle,
            Shop,
            Rest
        }

        private readonly SaveFileService _save = new();
        private readonly ProfileService _profile = new();

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioSource _uiSource;
        private AudioSource _bossSource;

        private AudioClip _puzzleLoop;
        private AudioClip _shopLoop;
        private AudioClip _restLoop;

        // Floor-based music
        private readonly AudioClip[] _floorLoops = new AudioClip[5];
        private readonly AudioClip[] _bossLayers = new AudioClip[5];
        private int _currentFloorIndex = -1;
        private bool _bossLayerActive;
        private bool _isDucking;
        private Coroutine _crossfadeCoroutine;
        private AudioClip _wrongSfx;
        private AudioClip _solvedSfx;
        private AudioClip _shopPurchaseSfx;
        private AudioClip _shopRerollSfx;
        private AudioClip _itemUseSfx;
        private AudioClip _rewardClaimSfx;
        private AudioClip _pathAdvanceSfx;
        private AudioClip _correctPlaceSfx;
        private AudioClip _cellSelectSfx;
        private AudioClip _pencilToggleSfx;
        private AudioClip _restHealSfx;
        private AudioClip _gameOverSfx;
        private AudioClip _relicPickupSfx;

        // New SFX from AudioVisualDirection spec
        private AudioClip _combo5Sfx;
        private AudioClip _combo10Sfx;
        private AudioClip _comboBreakSfx;
        private AudioClip _hpLossSfx;
        private AudioClip _hpCriticalSfx;
        private AudioClip _pencilDepletedSfx;
        private AudioClip _goldGainSfx;
        private AudioClip _fogRevealSfx;
        private AudioClip _modifierActivateSfx;
        private AudioClip _bossGateOpenSfx;
        private AudioClip _floorTransitionSfx;
        private AudioClip _levelUpSfx;
        private AudioClip _achievementSfx;
        private AudioClip _xpBarFillSfx;
        private AudioClip _victorySfx;

        // UI SFX
        private AudioClip _buttonHoverSfx;
        private AudioClip _buttonClickSfx;
        private AudioClip _menuOpenSfx;
        private AudioClip _menuCloseSfx;
        private AudioClip _itemRewardSfx;
        private AudioClip _relicRevealSfx;

        private Context _context = Context.Path;

        private void Awake()
        {
            _musicSource = GetComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;

            var sfxGo = new GameObject("RunSfxSource", typeof(AudioSource));
            sfxGo.transform.SetParent(transform, false);
            _sfxSource = sfxGo.GetComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            var uiGo = new GameObject("RunUiSource", typeof(AudioSource));
            uiGo.transform.SetParent(transform, false);
            _uiSource = uiGo.GetComponent<AudioSource>();
            _uiSource.loop = false;
            _uiSource.playOnAwake = false;
            _uiSource.spatialBlend = 0f;

            var bossGo = new GameObject("RunBossSource", typeof(AudioSource));
            bossGo.transform.SetParent(transform, false);
            _bossSource = bossGo.GetComponent<AudioSource>();
            _bossSource.loop = true;
            _bossSource.playOnAwake = false;
            _bossSource.spatialBlend = 0f;

            // Ensure a listener exists so runtime-generated clips are audible in minimal prototype scenes.
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    mainCam.gameObject.AddComponent<AudioListener>();
                }
            }

            if (_save.TryLoadProfile(out var envelope))
            {
                _profile.ApplyEnvelope(envelope);
            }

            // Context loops — delegate to ProceduralSfxLibrary
            _puzzleLoop = ProceduralSfxLibrary.BuildPuzzleLoop();
            _shopLoop = ProceduralSfxLibrary.BuildShopLoop();
            _restLoop = ProceduralSfxLibrary.BuildRestLoop();

            // Floor-based music loops + boss layers
            for (var f = 0; f < 5; f++)
            {
                _floorLoops[f] = FloorMusicGenerator.BuildFloorLoop(f);
                _bossLayers[f] = FloorMusicGenerator.BuildBossLayer(f);
            }

            // Existing gameplay SFX
            _wrongSfx = ProceduralSfxLibrary.BuildWrongPlaceSfx();
            _solvedSfx = ProceduralSfxLibrary.BuildLevelCompleteSfx();
            _shopPurchaseSfx = ProceduralSfxLibrary.BuildShopPurchaseSfx();
            _shopRerollSfx = ProceduralSfxLibrary.BuildShopRerollSfx();
            _itemUseSfx = ProceduralSfxLibrary.BuildItemUseSfx();
            _rewardClaimSfx = ProceduralSfxLibrary.BuildRewardClaimSfx();
            _pathAdvanceSfx = ProceduralSfxLibrary.BuildPathAdvanceSfx();
            _correctPlaceSfx = ProceduralSfxLibrary.BuildCorrectPlaceSfx();
            _cellSelectSfx = ProceduralSfxLibrary.BuildCellSelectSfx();
            _pencilToggleSfx = ProceduralSfxLibrary.BuildPencilToggleSfx();
            _restHealSfx = ProceduralSfxLibrary.BuildRestHealSfx();
            _gameOverSfx = ProceduralSfxLibrary.BuildGameOverStingerSfx();
            _relicPickupSfx = ProceduralSfxLibrary.BuildRelicPickupSfx();

            // New gameplay SFX
            _combo5Sfx = ProceduralSfxLibrary.BuildCombo5Sfx();
            _combo10Sfx = ProceduralSfxLibrary.BuildCombo10Sfx();
            _comboBreakSfx = ProceduralSfxLibrary.BuildComboBreakSfx();
            _hpLossSfx = ProceduralSfxLibrary.BuildHpLossSfx();
            _hpCriticalSfx = ProceduralSfxLibrary.BuildHpCriticalSfx();
            _pencilDepletedSfx = ProceduralSfxLibrary.BuildPencilDepletedSfx();
            _goldGainSfx = ProceduralSfxLibrary.BuildGoldGainSfx();
            _fogRevealSfx = ProceduralSfxLibrary.BuildFogRevealSfx();
            _modifierActivateSfx = ProceduralSfxLibrary.BuildModifierActivateSfx();
            _bossGateOpenSfx = ProceduralSfxLibrary.BuildBossGateOpenSfx();
            _floorTransitionSfx = ProceduralSfxLibrary.BuildFloorTransitionSfx();
            _levelUpSfx = ProceduralSfxLibrary.BuildLevelUpSfx();
            _achievementSfx = ProceduralSfxLibrary.BuildAchievementSfx();
            _xpBarFillSfx = ProceduralSfxLibrary.BuildXpBarFillSfx();
            _victorySfx = ProceduralSfxLibrary.BuildVictoryStingerSfx();

            // UI SFX
            _buttonHoverSfx = ProceduralSfxLibrary.BuildButtonHoverSfx();
            _buttonClickSfx = ProceduralSfxLibrary.BuildButtonClickSfx();
            _menuOpenSfx = ProceduralSfxLibrary.BuildMenuOpenSfx();
            _menuCloseSfx = ProceduralSfxLibrary.BuildMenuCloseSfx();
            _itemRewardSfx = ProceduralSfxLibrary.BuildItemRewardSfx();
            _relicRevealSfx = ProceduralSfxLibrary.BuildRelicRevealSfx();
        }

        private void Start()
        {
            SetContext(Context.Path);
        }

        private float _nextProfileRefresh;

        private void Update()
        {
            if (Time.unscaledTime >= _nextProfileRefresh)
            {
                _nextProfileRefresh = Time.unscaledTime + 1f;
                if (_save.TryLoadProfile(out var envelope))
                {
                    _profile.ApplyEnvelope(envelope);
                }
            }

            var muted = _profile.Options.Audio.MuteAll;
            var baseVolume = Mathf.Clamp01(_profile.Options.Audio.MasterVolume);
            var musicVol = baseVolume * Mathf.Clamp01(_profile.Options.Audio.MusicVolume) * 0.75f;
            var duckMul = _isDucking ? 0.8f : 1f;
            _musicSource.mute = muted;
            _sfxSource.mute = muted;
            _uiSource.mute = muted;
            _bossSource.mute = muted;
            _musicSource.volume = musicVol * duckMul;
            _sfxSource.volume = baseVolume * Mathf.Clamp01(_profile.Options.Audio.SfxVolume) * 0.55f;
            _uiSource.volume = baseVolume * Mathf.Clamp01(_profile.Options.Audio.UiVolume) * 0.65f;
            _bossSource.volume = _bossLayerActive ? musicVol * 0.6f * duckMul : 0f;

            if (_context == Context.Puzzle && !_musicSource.mute)
            {
                if (_musicSource.clip != null && !_musicSource.isPlaying)
                {
                    _musicSource.Play();
                }
            }
        }

        public void SetContext(Context context)
        {
            if (_context == context && _musicSource.isPlaying)
            {
                return;
            }

            _context = context;

            // Stop boss layer when leaving puzzle context
            if (context != Context.Puzzle && _bossLayerActive)
            {
                StopBossLayer();
            }

            _musicSource.clip = context switch
            {
                Context.Puzzle => _puzzleLoop,
                Context.Shop => _shopLoop,
                Context.Rest => _restLoop,
                _ => null
            };

            if (_musicSource.clip == null)
            {
                _musicSource.Stop();
                return;
            }

            _musicSource.Play();
        }

        // ── Existing gameplay SFX playback ───────────────────────────────

        public void PlayWrongPlacement() => PlaySfx(_wrongSfx, 1f);
        public void PlayPuzzleSolved() => PlaySfx(_solvedSfx, 1f);
        public void PlayShopPurchase() => PlaySfx(_shopPurchaseSfx, 1f);
        public void PlayShopReroll() => PlaySfx(_shopRerollSfx, 1f);
        public void PlayItemUse() => PlaySfx(_itemUseSfx, 1f);
        public void PlayRewardClaim() => PlaySfx(_rewardClaimSfx, 1f);
        public void PlayPathAdvance() => PlaySfx(_pathAdvanceSfx, 1f);
        public void PlayCorrectPlacement() => PlaySfx(_correctPlaceSfx, 0.7f);
        public void PlayCellSelect() => PlaySfx(_cellSelectSfx, 0.5f);
        public void PlayPencilToggle() => PlaySfx(_pencilToggleSfx, 0.6f);
        public void PlayRestHeal() => PlaySfx(_restHealSfx, 1f);
        public void PlayGameOver() => PlaySfx(_gameOverSfx, 1f);
        public void PlayRelicPickup() => PlaySfx(_relicPickupSfx, 1f);

        // ── New gameplay SFX ─────────────────────────────────────────────

        public void PlayComboMilestone(int combo)
        {
            if (combo >= 10) PlaySfx(_combo10Sfx, 0.8f);
            else if (combo >= 5) PlaySfx(_combo5Sfx, 0.8f);
        }

        public void PlayComboBreak() => PlaySfx(_comboBreakSfx, 0.6f);
        public void PlayHpLoss() => PlaySfx(_hpLossSfx, 1f);
        public void PlayHpCritical() => PlaySfx(_hpCriticalSfx, 0.8f);
        public void PlayPencilDepleted() => PlaySfx(_pencilDepletedSfx, 0.5f);
        public void PlayGoldGain() => PlaySfx(_goldGainSfx, 0.6f);
        public void PlayFogReveal() => PlaySfx(_fogRevealSfx, 0.7f);
        public void PlayModifierActivate() => PlaySfx(_modifierActivateSfx, 0.8f);
        public void PlayBossGateOpen() => PlaySfx(_bossGateOpenSfx, 1f);
        public void PlayFloorTransition() => PlaySfx(_floorTransitionSfx, 1f);
        public void PlayLevelUp() => PlaySfx(_levelUpSfx, 1f);
        public void PlayAchievement() => PlaySfx(_achievementSfx, 0.9f);
        public void PlayXpBarFill() => PlaySfx(_xpBarFillSfx, 0.7f);
        public void PlayVictory() => PlaySfx(_victorySfx, 1f);

        // ── UI SFX (routed through UiVolume channel) ─────────────────────

        public void PlayButtonHover() => PlayUi(_buttonHoverSfx, 0.4f);
        public void PlayButtonClick() => PlayUi(_buttonClickSfx, 0.6f);
        public void PlayMenuOpen() => PlayUi(_menuOpenSfx, 0.5f);
        public void PlayMenuClose() => PlayUi(_menuCloseSfx, 0.5f);
        public void PlayItemReward() => PlayUi(_itemRewardSfx, 0.7f);
        public void PlayRelicReveal() => PlayUi(_relicRevealSfx, 0.8f);

        // ── Floor Music ─────────────────────────────────────────────────

        /// <summary>
        /// Switch to floor-specific music with crossfade (800ms out, 200ms silence, 800ms in).
        /// </summary>
        public void SetFloorIndex(int floorIndex)
        {
            floorIndex = Mathf.Clamp(floorIndex, 0, 4);
            if (floorIndex == _currentFloorIndex) return;
            _currentFloorIndex = floorIndex;

            // Use the floor loop as the puzzle loop for this floor
            _puzzleLoop = _floorLoops[floorIndex];

            if (_context == Context.Puzzle)
            {
                if (_crossfadeCoroutine != null) StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = StartCoroutine(CrossfadeToClip(_floorLoops[floorIndex]));
            }
        }

        /// <summary>
        /// Start additive boss tension layer on top of current floor music.
        /// </summary>
        public void StartBossLayer()
        {
            if (_bossLayerActive) return;
            _bossLayerActive = true;
            var idx = Mathf.Clamp(_currentFloorIndex, 0, 4);
            _bossSource.clip = _bossLayers[idx];
            _bossSource.Play();
        }

        /// <summary>
        /// Stop the boss tension layer.
        /// </summary>
        public void StopBossLayer()
        {
            _bossLayerActive = false;
            _bossSource.Stop();
        }

        /// <summary>
        /// Duck music volume 20% during XP bar animation.
        /// </summary>
        public void SetMusicDucking(bool ducking)
        {
            _isDucking = ducking;
        }

        private IEnumerator CrossfadeToClip(AudioClip newClip)
        {
            // Fade out (800ms)
            var startVol = _musicSource.volume;
            var elapsed = 0f;
            while (elapsed < 0.8f)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / 0.8f);
                yield return null;
            }
            _musicSource.Stop();

            // Silence (200ms)
            yield return new WaitForSecondsRealtime(0.2f);

            // Fade in (800ms)
            _musicSource.clip = newClip;
            if (newClip != null)
            {
                _musicSource.Play();
                elapsed = 0f;
                while (elapsed < 0.8f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    // Volume will be reset by Update, but we nudge it for smoothness
                    yield return null;
                }
            }
            _crossfadeCoroutine = null;
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private void PlaySfx(AudioClip clip, float volume)
        {
            if (clip != null) _sfxSource.PlayOneShot(clip, volume);
        }

        private void PlayUi(AudioClip clip, float volume)
        {
            if (clip != null) _uiSource.PlayOneShot(clip, volume);
        }
    }
}
