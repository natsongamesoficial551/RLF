using System;

namespace RLF.Core.Safety
{
    /// <summary>
    /// Analisa contexto do gameplay usando dados fornecidos pelo Provider.
    /// REFINADO: GetFrequencyMultiplier aceita SystemCategory diretamente.
    /// </summary>
    public sealed class GameplayContextAnalyzer
    {
        #region Singleton

        private static GameplayContextAnalyzer _instance;
        private static readonly object _lock = new object();

        public static GameplayContextAnalyzer Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new GameplayContextAnalyzer();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private FullContext _currentContext = new FullContext();
        private ISafetyDataProvider _dataProvider;

        // FPS buffer
        private readonly float[] _fpsBuffer = new float[60];
        private int _fpsBufferIndex;
        private int _fpsBufferCount;

        // Cache
        private float _lastPosX, _lastPosY, _lastPosZ;

        // Frame counters
        private int _frameCounter;
        private int _lastFullAnalysisFrame;
        private int _lastWorldScanFrame;
        private int _lastGCCheckFrame;

        // Cached world data
        private int _cachedNearbyPedCount;
        private int _cachedRelevantNPCCount;

        // GC tracking
        private int _lastGCCount;

        // Thresholds (frames @ ~30fps)
        private const int IDLE_THRESHOLD_FRAMES = 900;
        private const int AFK_THRESHOLD_FRAMES = 3600;
        private const int COMBAT_COOLDOWN_FRAMES = 300;
        private const int FULL_ANALYSIS_INTERVAL = 15;
        private const int WORLD_SCAN_INTERVAL = 60;
        private const int GC_CHECK_INTERVAL = 90;
        private const int SURVIVAL_MODE_FRAMES = 54000;
        private const float FAST_DRIVING_SPEED = 25f;

        #endregion

        #region Constructor

        private GameplayContextAnalyzer()
        {
            _lastGCCount = GC.CollectionCount(0);
        }

        #endregion

        #region Properties

        public FullContext CurrentContext => _currentContext;
        public PlayerActivityState PlayerState => _currentContext.Player.ActivityState;
        public WorldState CurrentWorldState => _currentContext.World.CurrentState;
        public PerformanceLevel CurrentPerformanceLevel => _currentContext.Performance.Level;
        public bool IsPlayerIdle => _currentContext.Player.IdleFrames >= IDLE_THRESHOLD_FRAMES;
        public bool IsPlayerAFK => _currentContext.Player.AFKFrames >= AFK_THRESHOLD_FRAMES;
        public bool IsInProtectionMode => _currentContext.Performance.Level >= PerformanceLevel.LightProtection;
        public bool IsInSurvivalMode => _currentContext.Session.SurvivalModeActive;

        public bool ShouldReduceActivity =>
            IsPlayerIdle || IsPlayerAFK || IsInProtectionMode ||
            _currentContext.World.IsDeepNight || IsInSurvivalMode;

        #endregion

        #region Initialization

        public void SetDataProvider(ISafetyDataProvider provider)
        {
            _dataProvider = provider;
        }

        #endregion

        #region Main Analysis

        public FullContext Analyze()
        {
            if (_dataProvider == null)
            {
                _currentContext.IsValid = false;
                return _currentContext;
            }

            _frameCounter++;
            _currentContext.Session.TotalFrames = _frameCounter;

            AnalyzeQuick();

            if (_frameCounter - _lastFullAnalysisFrame >= FULL_ANALYSIS_INTERVAL)
            {
                _lastFullAnalysisFrame = _frameCounter;
                AnalyzeMedium();
            }

            if (_frameCounter - _lastWorldScanFrame >= WORLD_SCAN_INTERVAL)
            {
                _lastWorldScanFrame = _frameCounter;
                AnalyzeWorld();
            }

            CheckSessionTime();

            _currentContext.IsValid = true;
            return _currentContext;
        }

        #endregion

        #region Quick Analysis (every frame)

        private void AnalyzeQuick()
        {
            float frameTime = _dataProvider.GetLastFrameTime();
            if (frameTime > 0.001f)
            {
                float fps = 1f / frameTime;
                fps = fps > 200f ? 200f : fps;

                _fpsBuffer[_fpsBufferIndex] = fps;
                _fpsBufferIndex = (_fpsBufferIndex + 1) % 60;
                if (_fpsBufferCount < 60) _fpsBufferCount++;

                _currentContext.Performance.CurrentFPS = fps;
            }

            float posX = _dataProvider.GetPlayerPositionX();
            float posY = _dataProvider.GetPlayerPositionY();
            float posZ = _dataProvider.GetPlayerPositionZ();
            float speed = _dataProvider.GetPlayerSpeed();

            _currentContext.Player.PositionX = posX;
            _currentContext.Player.PositionY = posY;
            _currentContext.Player.PositionZ = posZ;
            _currentContext.Player.Speed = speed;
            _currentContext.Player.IsInVehicle = _dataProvider.IsPlayerInVehicle();

            float dx = _lastPosX - posX;
            float dy = _lastPosY - posY;
            float dz = _lastPosZ - posZ;
            float moved = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

            if (moved < 0.3f && speed < 0.1f)
            {
                _currentContext.Player.IdleFrames++;
            }
            else
            {
                _currentContext.Player.IdleFrames = 0;
            }
            _lastPosX = posX;
            _lastPosY = posY;
            _lastPosZ = posZ;

            bool hasInput = _dataProvider.IsAnyMovementInputPressed() ||
                           _dataProvider.IsAttackInputPressed() ||
                           _dataProvider.IsAimInputPressed();

            if (hasInput)
            {
                _currentContext.Player.AFKFrames = 0;
            }
            else
            {
                _currentContext.Player.AFKFrames++;
            }

            if (_currentContext.Player.FramesSinceCombat < COMBAT_COOLDOWN_FRAMES)
            {
                _currentContext.Player.FramesSinceCombat++;
            }

            float hp = _dataProvider.GetPlayerHealth();
            if (hp < _currentContext.Player.LastHealth)
            {
                _currentContext.Player.TookDamageRecently = true;
                _currentContext.Player.FramesSinceCombat = 0;
            }
            else if (_currentContext.Player.FramesSinceCombat > 90)
            {
                _currentContext.Player.TookDamageRecently = false;
            }
            _currentContext.Player.LastHealth = hp;
            _currentContext.Player.Health = hp;
        }

        #endregion

        #region Medium Analysis (every 15 frames)

        private void AnalyzeMedium()
        {
            _currentContext.Player.WantedLevel = _dataProvider.GetPlayerWantedLevel();
            _currentContext.Player.IsWanted = _currentContext.Player.WantedLevel > 0;
            _currentContext.Player.IsInCover = _dataProvider.IsPlayerInCover();
            _currentContext.Player.IsInInterior = _dataProvider.IsInteriorScene();

            bool inMenu = _dataProvider.IsGamePaused();
            bool inCutscene = _dataProvider.IsCutsceneActive();

            _currentContext.World.Hour = _dataProvider.GetGameHour();
            _currentContext.World.IsDeepNight = _currentContext.World.Hour >= 0 &&
                                                _currentContext.World.Hour < 5;

            int weather = _dataProvider.GetCurrentWeather();
            _currentContext.World.IsHeavyWeather = (weather == 7 || weather == 8 || weather == 11);

            if (_fpsBufferCount > 0)
            {
                float sum = 0;
                for (int i = 0; i < _fpsBufferCount; i++)
                    sum += _fpsBuffer[i];
                _currentContext.Performance.AverageFPS = sum / _fpsBufferCount;
            }

            if (_frameCounter - _lastGCCheckFrame >= GC_CHECK_INTERVAL)
            {
                _lastGCCheckFrame = _frameCounter;
                int gcCount = GC.CollectionCount(0);
                _currentContext.Performance.GCSpikeDetected = (gcCount > _lastGCCount);
                _lastGCCount = gcCount;
            }
            else
            {
                _currentContext.Performance.GCSpikeDetected = false;
            }

            DeterminePlayerState(inMenu, inCutscene);
            DeterminePerformanceLevel();
        }

        #endregion

        #region World Analysis (every 60 frames)

        private void AnalyzeWorld()
        {
            if (_currentContext.Session.SurvivalModeActive)
            {
                _currentContext.World.NearbyPedCount = 0;
                _currentContext.World.RelevantNPCCount = 0;
                _currentContext.World.CurrentState = WorldState.Quiet;
                return;
            }

            float scanRadius = 50f;
            if (_currentContext.Performance.Level >= PerformanceLevel.HeavyProtection)
            {
                scanRadius = 25f;
            }

            int pedCount = _dataProvider.GetNearbyPedCount(scanRadius);

            _cachedNearbyPedCount = pedCount;
            _cachedRelevantNPCCount = pedCount > 5 ? pedCount / 2 : pedCount;

            _currentContext.World.NearbyPedCount = pedCount;
            _currentContext.World.RelevantNPCCount = _cachedRelevantNPCCount;

            DetermineWorldState();
        }

        #endregion

        #region Session Time

        private void CheckSessionTime()
        {
            int minutes = _frameCounter / 1800;
            _currentContext.Session.MinutesSinceStart = minutes;

            if (_frameCounter >= SURVIVAL_MODE_FRAMES && !_currentContext.Session.SurvivalModeActive)
            {
                _currentContext.Session.SurvivalModeActive = true;
                _currentContext.Performance.Level = PerformanceLevel.Survival;
                SafetyLogger.Instance?.Log("[GameplayContextAnalyzer] === SURVIVAL MODE ACTIVATED === (30+ min session)");
            }
        }

        #endregion

        #region State Determination

        private void DeterminePlayerState(bool inMenu, bool inCutscene)
        {
            var p = _currentContext.Player;

            if (inCutscene)
            {
                p.ActivityState = PlayerActivityState.InCutscene;
                return;
            }

            if (inMenu)
            {
                p.ActivityState = PlayerActivityState.InMenu;
                return;
            }

            if (p.AFKFrames >= AFK_THRESHOLD_FRAMES)
            {
                p.ActivityState = PlayerActivityState.AFK;
                return;
            }

            if (p.FramesSinceCombat < COMBAT_COOLDOWN_FRAMES || p.IsWanted)
            {
                p.ActivityState = PlayerActivityState.InCombat;
                return;
            }

            if (p.IsInInterior)
            {
                p.ActivityState = PlayerActivityState.InInterior;
                return;
            }

            if (p.IsInVehicle)
            {
                p.ActivityState = p.Speed >= FAST_DRIVING_SPEED ?
                    PlayerActivityState.DrivingFast : PlayerActivityState.DrivingSlow;
                return;
            }

            if (p.IdleFrames >= IDLE_THRESHOLD_FRAMES)
            {
                p.ActivityState = PlayerActivityState.Idle;
                return;
            }

            p.ActivityState = PlayerActivityState.Walking;
        }

        private void DetermineWorldState()
        {
            var w = _currentContext.World;

            if (!w.HasActiveEvents && _cachedRelevantNPCCount == 0)
            {
                w.CurrentState = WorldState.Quiet;
            }
            else if (_cachedNearbyPedCount < 3)
            {
                w.CurrentState = WorldState.NoNPCsNearby;
            }
            else if (w.IsDeepNight)
            {
                w.CurrentState = WorldState.DeepNight;
            }
            else if (w.IsHeavyWeather)
            {
                w.CurrentState = WorldState.HeavyWeather;
            }
            else
            {
                w.CurrentState = WorldState.Normal;
            }
        }

        private void DeterminePerformanceLevel()
        {
            var perf = _currentContext.Performance;

            if (_currentContext.Session.SurvivalModeActive)
            {
                perf.Level = PerformanceLevel.Survival;
                return;
            }

            if (perf.GCSpikeDetected)
            {
                perf.Level = PerformanceLevel.Critical;
                return;
            }

            float fps = perf.AverageFPS;

            if (fps < 25f)
            {
                perf.Level = PerformanceLevel.Critical;
                perf.ConsecutiveLowFPSFrames++;
                perf.ConsecutiveHighFPSFrames = 0;
            }
            else if (fps < 35f)
            {
                perf.ConsecutiveLowFPSFrames++;
                perf.ConsecutiveHighFPSFrames = 0;
                perf.Level = perf.ConsecutiveLowFPSFrames > 60 ?
                    PerformanceLevel.HeavyProtection : PerformanceLevel.LightProtection;
            }
            else if (fps < 45f)
            {
                perf.Level = PerformanceLevel.LightProtection;
                if (perf.ConsecutiveLowFPSFrames > 0) perf.ConsecutiveLowFPSFrames--;
            }
            else if (fps > 55f)
            {
                perf.ConsecutiveHighFPSFrames++;
                perf.ConsecutiveLowFPSFrames = 0;
                perf.Level = perf.ConsecutiveHighFPSFrames > 120 ?
                    PerformanceLevel.Optimal : PerformanceLevel.Normal;
            }
            else
            {
                perf.Level = PerformanceLevel.Normal;
            }
        }

        #endregion

        #region Public Query Methods

        public bool ShouldSystemRun(string systemId)
        {
            if (_currentContext.Session.SurvivalModeActive)
            {
                string lower = systemId.ToLowerInvariant();
                return lower == "core" || lower == "player" || lower.Contains("core");
            }

            var player = _currentContext.Player;
            string type = systemId.ToLowerInvariant();

            switch (type)
            {
                case "crimescanner":
                    return _cachedRelevantNPCCount > 0 &&
                           player.ActivityState != PlayerActivityState.InMenu &&
                           player.ActivityState != PlayerActivityState.AFK;

                case "npcreaction":
                    return player.ActivityState == PlayerActivityState.InCombat;

                case "traffic":
                    return player.ActivityState == PlayerActivityState.DrivingFast ||
                           player.ActivityState == PlayerActivityState.DrivingSlow;

                case "livingworld":
                    return player.ActivityState != PlayerActivityState.InMenu &&
                           player.ActivityState != PlayerActivityState.AFK &&
                           !player.IsInInterior &&
                           _currentContext.Performance.Level < PerformanceLevel.HeavyProtection;

                case "debug":
                    return false;

                default:
                    return player.ActivityState != PlayerActivityState.InMenu &&
                           player.ActivityState != PlayerActivityState.InCutscene;
            }
        }

        /// <summary>
        /// REFINADO: Overload que aceita SystemCategory diretamente (preferível)
        /// </summary>
        public float GetFrequencyMultiplier(SystemCategory category)
        {
            if (_currentContext.Session.SurvivalModeActive)
                return 0.05f;

            float mult = 1f;

            // Por performance
            switch (_currentContext.Performance.Level)
            {
                case PerformanceLevel.Critical: mult = 0.1f; break;
                case PerformanceLevel.HeavyProtection: mult = 0.25f; break;
                case PerformanceLevel.LightProtection: mult = 0.5f; break;
                case PerformanceLevel.Normal: mult = 0.8f; break;
            }

            // Por estado do player
            switch (_currentContext.Player.ActivityState)
            {
                case PlayerActivityState.AFK: mult *= 0.1f; break;
                case PlayerActivityState.Idle: mult *= 0.3f; break;
                case PlayerActivityState.InMenu:
                case PlayerActivityState.InCutscene: return 0f;
                case PlayerActivityState.InCombat: mult = mult < 0.8f ? 0.8f : mult; break;
            }

            // Por mundo
            if (_currentContext.World.IsDeepNight) mult *= 0.7f;

            // Ajustes específicos por categoria
            switch (category)
            {
                case SystemCategory.Traffic:
                    if (_currentContext.Player.ActivityState == PlayerActivityState.DrivingFast)
                        mult = mult < 1f ? 1f : mult; // Priorizar trânsito
                    break;

                case SystemCategory.AI:
                    if (_currentContext.Player.ActivityState == PlayerActivityState.DrivingFast)
                        mult *= 0.5f; // Reduzir AI quando dirigindo rápido
                    break;

                case SystemCategory.LivingWorld:
                    if (_currentContext.Player.ActivityState == PlayerActivityState.DrivingFast)
                        mult *= 0.3f; // Pausar eventos secundários
                    break;

                case SystemCategory.Debug:
                    return 0f;
            }

            return mult < 0.05f ? 0.05f : (mult > 1f ? 1f : mult);
        }

        /// <summary>
        /// Overload mantido para compatibilidade (usa string)
        /// </summary>
        public float GetFrequencyMultiplier(string systemType)
        {
            // Tenta converter para categoria
            if (Enum.TryParse<SystemCategory>(systemType, true, out var category))
            {
                return GetFrequencyMultiplier(category);
            }

            // Fallback: mapeamento manual
            string lower = systemType.ToLowerInvariant();
            switch (lower)
            {
                case "core": return GetFrequencyMultiplier(SystemCategory.Core);
                case "combat": return GetFrequencyMultiplier(SystemCategory.Combat);
                case "ai": return GetFrequencyMultiplier(SystemCategory.AI);
                case "traffic": return GetFrequencyMultiplier(SystemCategory.Traffic);
                case "crime":
                case "crimescanner": return GetFrequencyMultiplier(SystemCategory.Crime);
                case "economy": return GetFrequencyMultiplier(SystemCategory.Economy);
                case "jobs": return GetFrequencyMultiplier(SystemCategory.Jobs);
                case "ui": return GetFrequencyMultiplier(SystemCategory.UI);
                case "weather": return GetFrequencyMultiplier(SystemCategory.Weather);
                case "livingworld": return GetFrequencyMultiplier(SystemCategory.LivingWorld);
                case "debug": return GetFrequencyMultiplier(SystemCategory.Debug);
                default: return GetFrequencyMultiplier(SystemCategory.Custom);
            }
        }

        public void RegisterActiveEvent()
        {
            _currentContext.World.HasActiveEvents = true;
            _currentContext.World.ActiveEventCount++;
        }

        public void UnregisterActiveEvent()
        {
            _currentContext.World.ActiveEventCount--;
            if (_currentContext.World.ActiveEventCount <= 0)
            {
                _currentContext.World.ActiveEventCount = 0;
                _currentContext.World.HasActiveEvents = false;
            }
        }

        public void ResetSession()
        {
            _frameCounter = 0;
            _currentContext.Session.TotalFrames = 0;
            _currentContext.Session.MinutesSinceStart = 0;
            _currentContext.Session.SurvivalModeActive = false;
            SafetyLogger.Instance?.Log("[GameplayContextAnalyzer] Session reset");
        }

        #endregion
    }
}