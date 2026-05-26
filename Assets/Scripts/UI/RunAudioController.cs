using System.Collections;
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

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioSource _uiSource;
        private AudioSource _bossSource;

        private AudioClip _puzzleLoop;
        private AudioClip _shopLoop;
        private AudioClip _restLoop;

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
        private AudioClip _runVictoryFanfareSfx;

        private AudioClip _buttonHoverSfx;
        private AudioClip _buttonClickSfx;
        private AudioClip _menuOpenSfx;
        private AudioClip _menuCloseSfx;
        private AudioClip _itemRewardSfx;
        private AudioClip _relicRevealSfx;

        private Context _context = Context.Path;
        private bool _startCompleted;
        private bool _contextRequested;

        private void Awake()
        {
            // Singleton guard: destroy duplicate if one already exists across scene loads.
            // Keep Awake minimal — only DontDestroyOnLoad and save-service wiring here.
            // AudioSource setup and clip generation moved to Start() so the audio driver's
            // realtime thread has finished initialising before we touch it (avoids ADTM timeout).
            if (FindFirstObjectByType<RunAudioController>() != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);

        }

        private void Start()
        {
            // Configure the AudioSource that was required by [RequireComponent]
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

            if (FindFirstObjectByType<AudioListener>() == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                    mainCam.gameObject.AddComponent<AudioListener>();
            }

            _puzzleLoop = AudioAssetService.GetClip(
                AudioClipId.MusicPuzzleLoop,
                ProceduralSfxLibrary.BuildPuzzleLoop);
            _shopLoop = AudioAssetService.GetClip(
                AudioClipId.MusicShopLoop,
                ProceduralSfxLibrary.BuildShopLoop);
            _restLoop = AudioAssetService.GetClip(
                AudioClipId.MusicRestLoop,
                ProceduralSfxLibrary.BuildRestLoop);

            for (var f = 0; f < 5; f++)
            {
                var floorIndex = f;
                _floorLoops[f] = AudioAssetService.GetFloorLoop(
                    floorIndex,
                    () => FloorMusicGenerator.BuildFloorLoop(floorIndex));
                _bossLayers[f] = AudioAssetService.GetBossLayer(
                    floorIndex,
                    () => FloorMusicGenerator.BuildBossLayer(floorIndex));
            }

            _wrongSfx = AudioAssetService.GetClip(AudioClipId.SfxWrongPlacement, ProceduralSfxLibrary.BuildWrongPlaceSfx);
            _solvedSfx = AudioAssetService.GetClip(AudioClipId.SfxPuzzleSolved, ProceduralSfxLibrary.BuildLevelCompleteSfx);
            _shopPurchaseSfx = AudioAssetService.GetClip(AudioClipId.SfxShopPurchase, ProceduralSfxLibrary.BuildShopPurchaseSfx);
            _shopRerollSfx = AudioAssetService.GetClip(AudioClipId.SfxShopReroll, ProceduralSfxLibrary.BuildShopRerollSfx);
            _itemUseSfx = AudioAssetService.GetClip(AudioClipId.SfxItemUse, ProceduralSfxLibrary.BuildItemUseSfx);
            _rewardClaimSfx = AudioAssetService.GetClip(AudioClipId.SfxRewardClaim, ProceduralSfxLibrary.BuildRewardClaimSfx);
            _pathAdvanceSfx = AudioAssetService.GetClip(AudioClipId.SfxPathAdvance, ProceduralSfxLibrary.BuildPathAdvanceSfx);
            _correctPlaceSfx = AudioAssetService.GetClip(AudioClipId.SfxCorrectPlacement, ProceduralSfxLibrary.BuildCorrectPlaceSfx);
            _cellSelectSfx = AudioAssetService.GetClip(AudioClipId.SfxCellSelect, ProceduralSfxLibrary.BuildCellSelectSfx);
            _pencilToggleSfx = AudioAssetService.GetClip(AudioClipId.SfxPencilToggle, ProceduralSfxLibrary.BuildPencilToggleSfx);
            _restHealSfx = AudioAssetService.GetClip(AudioClipId.SfxRestHeal, ProceduralSfxLibrary.BuildRestHealSfx);
            _gameOverSfx = AudioAssetService.GetClip(AudioClipId.SfxGameOver, ProceduralSfxLibrary.BuildGameOverStingerSfx);
            _relicPickupSfx = AudioAssetService.GetClip(AudioClipId.SfxRelicPickup, ProceduralSfxLibrary.BuildRelicPickupSfx);

            _combo5Sfx = AudioAssetService.GetClip(AudioClipId.SfxCombo5, ProceduralSfxLibrary.BuildCombo5Sfx);
            _combo10Sfx = AudioAssetService.GetClip(AudioClipId.SfxCombo10, ProceduralSfxLibrary.BuildCombo10Sfx);
            _comboBreakSfx = AudioAssetService.GetClip(AudioClipId.SfxComboBreak, ProceduralSfxLibrary.BuildComboBreakSfx);
            _hpLossSfx = AudioAssetService.GetClip(AudioClipId.SfxHpLoss, ProceduralSfxLibrary.BuildHpLossSfx);
            _hpCriticalSfx = AudioAssetService.GetClip(AudioClipId.SfxHpCritical, ProceduralSfxLibrary.BuildHpCriticalSfx);
            _pencilDepletedSfx = AudioAssetService.GetClip(AudioClipId.SfxPencilDepleted, ProceduralSfxLibrary.BuildPencilDepletedSfx);
            _goldGainSfx = AudioAssetService.GetClip(AudioClipId.SfxGoldGain, ProceduralSfxLibrary.BuildGoldGainSfx);
            _fogRevealSfx = AudioAssetService.GetClip(AudioClipId.SfxFogReveal, ProceduralSfxLibrary.BuildFogRevealSfx);
            _modifierActivateSfx = AudioAssetService.GetClip(AudioClipId.SfxModifierActivate, ProceduralSfxLibrary.BuildModifierActivateSfx);
            _bossGateOpenSfx = AudioAssetService.GetClip(AudioClipId.SfxBossGateOpen, ProceduralSfxLibrary.BuildBossGateOpenSfx);
            _floorTransitionSfx = AudioAssetService.GetClip(AudioClipId.SfxFloorTransition, ProceduralSfxLibrary.BuildFloorTransitionSfx);
            _levelUpSfx = AudioAssetService.GetClip(AudioClipId.SfxLevelUp, ProceduralSfxLibrary.BuildLevelUpSfx);
            _achievementSfx = AudioAssetService.GetClip(AudioClipId.SfxAchievement, ProceduralSfxLibrary.BuildAchievementSfx);
            _xpBarFillSfx = AudioAssetService.GetClip(AudioClipId.SfxXpBarFill, ProceduralSfxLibrary.BuildXpBarFillSfx);
            _victorySfx = AudioAssetService.GetClip(AudioClipId.SfxVictory, ProceduralSfxLibrary.BuildVictoryStingerSfx);
            _runVictoryFanfareSfx = AudioAssetService.GetClip(AudioClipId.SfxRunVictoryFanfare, ProceduralSfxLibrary.BuildRunVictoryFanfareSfx);

            _buttonHoverSfx = AudioAssetService.GetClip(AudioClipId.UiButtonHover, ProceduralSfxLibrary.BuildButtonHoverSfx);
            _buttonClickSfx = AudioAssetService.GetClip(AudioClipId.UiButtonClick, ProceduralSfxLibrary.BuildButtonClickSfx);
            _menuOpenSfx = AudioAssetService.GetClip(AudioClipId.UiMenuOpen, ProceduralSfxLibrary.BuildMenuOpenSfx);
            _menuCloseSfx = AudioAssetService.GetClip(AudioClipId.UiMenuClose, ProceduralSfxLibrary.BuildMenuCloseSfx);
            _itemRewardSfx = AudioAssetService.GetClip(AudioClipId.UiItemReward, ProceduralSfxLibrary.BuildItemRewardSfx);
            _relicRevealSfx = AudioAssetService.GetClip(AudioClipId.UiRelicReveal, ProceduralSfxLibrary.BuildRelicRevealSfx);

            _startCompleted = true;
            if (_contextRequested)
                SetContext(_context);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _musicSource?.Pause();
                _bossSource?.Pause();
            }
            else
            {
                _musicSource?.UnPause();
                if (_bossLayerActive) _bossSource?.UnPause();
            }
        }

        private void Update()
        {
            // Read volumes from OptionsController.LiveAudio (updated immediately on slider drag)
            // instead of disk-polling, so Music/SFX/UI sliders give real-time audio feedback.
            var audio = OptionsController.LiveAudio;
            var muted = audio.MuteAll;
            var baseVolume = Mathf.Clamp01(audio.MasterVolume);
            var musicVol = baseVolume * Mathf.Clamp01(audio.MusicVolume) * 0.75f;
            var duckMul = _isDucking ? 0.8f : 1f;
            var pathMul = _context == Context.Path ? 0.6f : 1f;
            _musicSource.mute = muted;
            _sfxSource.mute = muted;
            _uiSource.mute = muted;
            _bossSource.mute = muted;
            _musicSource.volume = musicVol * duckMul * pathMul;
            _sfxSource.volume = baseVolume * Mathf.Clamp01(audio.SfxVolume) * 0.55f;
            _uiSource.volume = baseVolume * Mathf.Clamp01(audio.UiVolume) * 0.65f;
            _bossSource.volume = _bossLayerActive ? musicVol * 0.6f * duckMul : 0f;

            if ((_context == Context.Puzzle || _context == Context.Path) && !_musicSource.mute)
            {
                if (_musicSource.clip != null && !_musicSource.isPlaying)
                    _musicSource.Play();
            }
        }

        public void SetContext(Context context)
        {
            // Called before Start() has initialised AudioSources (e.g. on the same frame
            // the controller is instantiated). Cache the context; Start() will apply it.
            _contextRequested = true;
            if (!_startCompleted) { _context = context; return; }

            if (_context == context && _musicSource.isPlaying) return;
            _context = context;

            // Cancel any crossfade in progress so it doesn't fight the new clip assignment.
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = null;
            }

            if (context != Context.Puzzle && _bossLayerActive)
                StopBossLayer();

            _musicSource.clip = context switch
            {
                Context.Puzzle => _puzzleLoop,
                Context.Path   => _puzzleLoop,   // floor loop at 60 % volume (see Update)
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
        /// <summary>4-second ascending cadence fanfare — played only on final-boss run victory.</summary>
        public void PlayRunVictoryFanfare() => PlaySfx(_runVictoryFanfareSfx, 1f);

        public void PlayButtonHover() => PlayUi(_buttonHoverSfx, 0.4f);
        public void PlayButtonClick() => PlayUi(_buttonClickSfx, 0.6f);
        public void PlayMenuOpen() => PlayUi(_menuOpenSfx, 0.5f);
        public void PlayMenuClose() => PlayUi(_menuCloseSfx, 0.5f);
        public void PlayItemReward() => PlayUi(_itemRewardSfx, 0.7f);
        public void PlayRelicReveal() => PlayUi(_relicRevealSfx, 0.8f);

        public void SetFloorIndex(int floorIndex)
        {
            floorIndex = Mathf.Clamp(floorIndex, 0, 4);
            if (floorIndex == _currentFloorIndex) return;
            _currentFloorIndex = floorIndex;

            _puzzleLoop = _floorLoops[floorIndex];

            if (_context == Context.Puzzle)
            {
                if (_crossfadeCoroutine != null) StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = StartCoroutine(CrossfadeToClip(_floorLoops[floorIndex]));
            }
        }

        public void StartBossLayer()
        {
            if (_bossLayerActive) return;
            _bossLayerActive = true;
            var idx = Mathf.Clamp(_currentFloorIndex, 0, 4);
            _bossSource.clip = _bossLayers[idx];
            _bossSource.Play();
        }

        public void StopBossLayer()
        {
            _bossLayerActive = false;
            _bossSource.Stop();
        }

        /// <summary>
        /// Immediately stops all looping music (puzzle loop + boss layer) and any
        /// in-progress crossfade. Call this before returning to the main menu so
        /// run music does not overlap with the menu music track.
        /// </summary>
        public void StopAll()
        {
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = null;
            }
            // Reset context to Path (clip = null) BEFORE stopping, so the Update()
            // auto-restart guard cannot replay music on the next frame.
            _context = Context.Path;
            _contextRequested = false;
            if (_musicSource != null) { _musicSource.clip = null; _musicSource.Stop(); }
            if (_bossSource  != null) { _bossLayerActive = false; _bossSource.Stop(); }
        }

        public void SetMusicDucking(bool ducking)
        {
            _isDucking = ducking;
        }

        private IEnumerator CrossfadeToClip(AudioClip newClip)
        {
            var startVol = _musicSource.volume;
            var elapsed = 0f;
            while (elapsed < 0.8f)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / 0.8f);
                yield return null;
            }
            _musicSource.Stop();

            yield return new WaitForSecondsRealtime(0.2f);

            _musicSource.clip = newClip;
            if (newClip != null)
            {
                _musicSource.Play();
                elapsed = 0f;
                while (elapsed < 0.8f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            _crossfadeCoroutine = null;
        }

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
