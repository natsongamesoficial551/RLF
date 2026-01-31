using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace RLF.GTA.Safety
{
    /// <summary>
    /// Analisa o contexto atual do gameplay para determinar estados do jogador e mundo.
    /// OTIMIZADO: Cache agressivo, menos DateTime.Now, throttling em calls pesados.
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

        #region Enums
        public enum PlayerActivityState
        {
            Unknown,
            Idle,
            InMenu,
            InInterior,
            DrivingFast,
            InCombat,
            AFK,
            InCutscene,
            Walking,
            DrivingSlow,
            Swimming,
            Flying,
            Falling
        }

        public enum WorldState
        {
            Normal,
            Quiet,
            NoNPCsNearby,
            DeepNight,
            HeavyWeather,
            HighActivity
        }

        public enum PerformanceLevel
        {
            Optimal,
            Normal,
            LightProtection,
            HeavyProtection,
            Critical,
            Survival  // NOVO: Modo sobrevivência após longo tempo
        }

        [Flags]
        public enum InteriorType
        {
            None = 0,
            Small = 1,
            Medium = 2,
            Large = 4,
            Safehouse = 8,
            Mission = 16
        }
        #endregion

        #region Context Data Classes
        public class PlayerContext
        {
            public PlayerActivityState ActivityState;
            public Vector3 Position;
            public float Speed;
            public int IdleFrames;          // OTIMIZADO: frames ao invés de float time
            public int AFKFrames;           // OTIMIZADO: frames ao invés de DateTime
            public bool IsInVehicle;
            public bool IsArmed;
            public bool IsShooting;
            public bool IsInCover;
            public bool IsWanted;
            public int WantedLevel;
            public bool IsInInterior;
            public InteriorType CurrentInteriorType;
            public int FramesSinceCombat;   // OTIMIZADO: frames ao invés de DateTime
            public float Health;
            public float LastHealth;
            public bool TookDamageRecently;
        }

        public class WorldContext
        {
            public WorldState CurrentState;
            public int Hour;
            public bool IsDeepNight;
            public bool IsHeavyWeather;
            public int NearbyPedCount;       // Cache - atualiza a cada N frames
            public int NearbyVehicleCount;   // Cache
            public int RelevantNPCCount;     // Cache
            public bool HasActiveEvents;
            public int ActiveEventCount;
        }

        public class PerformanceContext
        {
            public PerformanceLevel Level;
            public float CurrentFPS;
            public float AverageFPS;
            public int ConsecutiveLowFPSFrames;
            public int ConsecutiveHighFPSFrames;
            public bool GCSpikeDetected;
            public int FramesSinceGCCheck;   // OTIMIZADO: throttle GC checks
        }

        public class SessionContext
        {
            public int TotalFrames;          // NOVO: tracking de sessão
            public int MinutesSinceStart;    // NOVO: para modo survival
            public bool SurvivalModeActive;  // NOVO
        }

        public class FullContext
        {
            public PlayerContext Player = new PlayerContext();
            public WorldContext World = new WorldContext();
            public PerformanceContext Performance = new PerformanceContext();
            public SessionContext Session = new SessionContext();
            public bool IsValid;
        }
        #endregion

        #region Private Fields - OTIMIZADO: menos objetos, mais primitivos
        private FullContext _currentContext = new FullContext();

        // FPS tracking simplificado - array fixo ao invés de Queue
        private readonly float[] _fpsBuffer = new float[60];  // ~2 segundos
        private int _fpsBufferIndex;
        private int _fpsBufferCount;

        // Cache de posição
        private Vector3 _lastPosition;
        private Vector3 _lastCameraDirection;

        // Contadores de frame (substitui DateTime)
        private int _frameCounter;
        private int _lastFullAnalysisFrame;
        private int _lastWorldScanFrame;
        private int _lastGCCheckFrame;
        private int _sessionStartFrame;

        // Cache de mundo (atualiza raramente)
        private int _cachedNearbyPedCount;
        private int _cachedRelevantNPCCount;
        private float _cachedNearestPedDist = 999f;

        // GC tracking otimizado
        private int _lastGCCount;

        // Thresholds em frames (assumindo ~30fps base)
        private const int IDLE_THRESHOLD_FRAMES = 900;        // ~30 segundos
        private const int AFK_THRESHOLD_FRAMES = 3600;        // ~2 minutos
        private const int COMBAT_COOLDOWN_FRAMES = 300;       // ~10 segundos
        private const int FULL_ANALYSIS_INTERVAL = 15;        // A cada 15 frames (~500ms)
        private const int WORLD_SCAN_INTERVAL = 60;           // A cada 60 frames (~2s)
        private const int GC_CHECK_INTERVAL = 90;             // A cada 90 frames (~3s)
        private const int SURVIVAL_MODE_FRAMES = 54000;       // ~30 minutos
        private const float FAST_DRIVING_SPEED = 25f;
        #endregion

        #region Constructor
        private GameplayContextAnalyzer()
        {
            _lastGCCount = GC.CollectionCount(0);
            _sessionStartFrame = 0;
        }
        #endregion

        #region Public Properties
        public FullContext CurrentContext => _currentContext;
        public PlayerActivityState PlayerState => _currentContext.Player.ActivityState;
        public WorldState CurrentWorldState => _currentContext.World.CurrentState;
        public PerformanceLevel CurrentPerformanceLevel => _currentContext.Performance.Level;
        public bool IsPlayerIdle => _currentContext.Player.IdleFrames >= IDLE_THRESHOLD_FRAMES;
        public bool IsPlayerAFK => _currentContext.Player.AFKFrames >= AFK_THRESHOLD_FRAMES;
        public bool IsInProtectionMode => _currentContext.Performance.Level >= PerformanceLevel.LightProtection;
        public bool IsInSurvivalMode => _currentContext.Session.SurvivalModeActive;
        public bool ShouldReduceActivity => IsPlayerIdle || IsPlayerAFK || IsInProtectionMode ||
                                            _currentContext.World.IsDeepNight || IsInSurvivalMode;
        #endregion

        #region Main Analysis - OTIMIZADO
        /// <summary>
        /// Análise principal - LEVE por frame, PESADA espaçada
        /// </summary>
        public FullContext Analyze()
        {
            _frameCounter++;
            _currentContext.Session.TotalFrames = _frameCounter;

            // Análise ULTRA-LEVE todo frame (apenas essenciais)
            AnalyzeQuick();

            // Análise MÉDIA a cada N frames
            if (_frameCounter - _lastFullAnalysisFrame >= FULL_ANALYSIS_INTERVAL)
            {
                _lastFullAnalysisFrame = _frameCounter;
                AnalyzeMedium();
            }

            // Análise PESADA (world scan) muito espaçada
            if (_frameCounter - _lastWorldScanFrame >= WORLD_SCAN_INTERVAL)
            {
                _lastWorldScanFrame = _frameCounter;
                AnalyzeWorld();
            }

            // Check de sessão para survival mode
            CheckSessionTime();

            _currentContext.IsValid = true;
            return _currentContext;
        }
        #endregion

        #region Quick Analysis - TODO FRAME (ultra-leve)
        private void AnalyzeQuick()
        {
            // FPS tracking simples
            float frameTime = Game.LastFrameTime;
            if (frameTime > 0.001f)
            {
                float fps = 1f / frameTime;
                fps = fps > 200f ? 200f : fps;  // Cap sem Math.Min

                _fpsBuffer[_fpsBufferIndex] = fps;
                _fpsBufferIndex = (_fpsBufferIndex + 1) % 60;
                if (_fpsBufferCount < 60) _fpsBufferCount++;

                _currentContext.Performance.CurrentFPS = fps;
            }

            // Player básico (sem natives pesados)
            Ped player = Game.Player?.Character;
            if (player == null || !player.Exists())
                return;

            Vector3 pos = player.Position;
            float speed = player.Velocity.Length();

            _currentContext.Player.Position = pos;
            _currentContext.Player.Speed = speed;
            _currentContext.Player.IsInVehicle = player.IsInVehicle();

            // Detecção de movimento (sem camera direction - pesado)
            float moved = (_lastPosition - pos).Length();
            if (moved < 0.3f && speed < 0.1f)
            {
                _currentContext.Player.IdleFrames++;
            }
            else
            {
                _currentContext.Player.IdleFrames = 0;
            }
            _lastPosition = pos;

            // Input detection simplificado (sem GameplayCamera)
            bool hasInput = Game.IsControlPressed(Control.MoveUpOnly) ||
                           Game.IsControlPressed(Control.MoveDownOnly) ||
                           Game.IsControlPressed(Control.Attack) ||
                           Game.IsControlPressed(Control.Aim);

            if (hasInput)
            {
                _currentContext.Player.AFKFrames = 0;
            }
            else
            {
                _currentContext.Player.AFKFrames++;
            }

            // Combat cooldown counter
            if (_currentContext.Player.FramesSinceCombat < COMBAT_COOLDOWN_FRAMES)
            {
                _currentContext.Player.FramesSinceCombat++;
            }

            // Dano recebido
            float hp = player.Health;
            if (hp < _currentContext.Player.LastHealth)
            {
                _currentContext.Player.TookDamageRecently = true;
                _currentContext.Player.FramesSinceCombat = 0;
            }
            else if (_currentContext.Player.FramesSinceCombat > 90) // ~3s
            {
                _currentContext.Player.TookDamageRecently = false;
            }
            _currentContext.Player.LastHealth = hp;
            _currentContext.Player.Health = hp;
        }
        #endregion

        #region Medium Analysis - A CADA 15 FRAMES
        private void AnalyzeMedium()
        {
            Ped player = Game.Player?.Character;
            if (player == null) return;

            // Wanted level
            _currentContext.Player.WantedLevel = Game.Player.WantedLevel;
            _currentContext.Player.IsWanted = _currentContext.Player.WantedLevel > 0;

            // Combate
            _currentContext.Player.IsShooting = Game.IsControlPressed(Control.Attack);
            _currentContext.Player.IsInCover = player.IsInCover;

            // Interior (native leve)
            _currentContext.Player.IsInInterior = Function.Call<bool>(Hash.IS_INTERIOR_SCENE);
            if (_currentContext.Player.IsInInterior)
            {
                _currentContext.Player.CurrentInteriorType = InteriorType.Small; // Assume small
            }

            // Menu/Cutscene
            bool inMenu = Game.IsPaused;
            bool inCutscene = Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);

            // Hora do jogo
            _currentContext.World.Hour = Function.Call<int>(Hash.GET_CLOCK_HOURS);
            _currentContext.World.IsDeepNight = _currentContext.World.Hour >= 0 &&
                                                _currentContext.World.Hour < 5;

            // Clima (só enum, sem processamento)
            Weather w = World.Weather;
            _currentContext.World.IsHeavyWeather = (w == Weather.Raining ||
                                                    w == Weather.ThunderStorm ||
                                                    w == Weather.Blizzard);

            // Calcular FPS médio
            if (_fpsBufferCount > 0)
            {
                float sum = 0;
                for (int i = 0; i < _fpsBufferCount; i++)
                    sum += _fpsBuffer[i];
                _currentContext.Performance.AverageFPS = sum / _fpsBufferCount;
            }

            // GC check throttled
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

            // Determinar estados
            DeterminePlayerState(inMenu, inCutscene);
            DeterminePerformanceLevel();
        }
        #endregion

        #region World Analysis - A CADA 60 FRAMES (pesado, espaçado)
        private void AnalyzeWorld()
        {
            Ped player = Game.Player?.Character;
            if (player == null) return;

            // Em survival mode, não escanear mundo
            if (_currentContext.Session.SurvivalModeActive)
            {
                _currentContext.World.NearbyPedCount = 0;
                _currentContext.World.RelevantNPCCount = 0;
                _currentContext.World.CurrentState = WorldState.Quiet;
                return;
            }

            // Em proteção pesada, escanear com raio menor
            float scanRadius = 50f;
            if (_currentContext.Performance.Level >= PerformanceLevel.HeavyProtection)
            {
                scanRadius = 25f;
            }

            Vector3 pos = player.Position;

            // World.GetNearbyPeds - ÚNICO CALL PESADO, bem espaçado
            Ped[] peds = World.GetNearbyPeds(pos, scanRadius);

            int pedCount = 0;
            int relevantCount = 0;
            float nearestDist = 999f;

            if (peds != null)
            {
                int maxCheck = peds.Length > 20 ? 20 : peds.Length; // Limitar iteração

                for (int i = 0; i < maxCheck; i++)
                {
                    Ped p = peds[i];
                    if (p == null || p == player) continue;

                    pedCount++;
                    float dist = (p.Position - pos).Length();

                    if (dist < nearestDist)
                        nearestDist = dist;

                    if (dist < 30f)
                        relevantCount++;
                }
            }

            _cachedNearbyPedCount = pedCount;
            _cachedRelevantNPCCount = relevantCount;
            _cachedNearestPedDist = nearestDist;

            _currentContext.World.NearbyPedCount = pedCount;
            _currentContext.World.RelevantNPCCount = relevantCount;

            // Determinar estado do mundo
            DetermineWorldState();
        }
        #endregion

        #region Session Time Check - SURVIVAL MODE
        private void CheckSessionTime()
        {
            // Calcular minutos aproximados (30fps base)
            int minutes = _frameCounter / 1800;
            _currentContext.Session.MinutesSinceStart = minutes;

            // Ativar survival mode após 30 minutos
            if (_frameCounter >= SURVIVAL_MODE_FRAMES && !_currentContext.Session.SurvivalModeActive)
            {
                _currentContext.Session.SurvivalModeActive = true;
                _currentContext.Performance.Level = PerformanceLevel.Survival;
                SafetyLogger.Instance?.Log("[GameplayContextAnalyzer] === SURVIVAL MODE ATIVADO === (30+ min de sessão)");
            }

            // Em survival, forçar nível mais baixo periodicamente
            if (_currentContext.Session.SurvivalModeActive)
            {
                // A cada 5 minutos extras, logar
                if (minutes > 30 && minutes % 5 == 0 && _frameCounter % 9000 < 30)
                {
                    SafetyLogger.Instance?.Log($"[GameplayContextAnalyzer] Survival mode: {minutes} minutos de sessão");
                }
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

            // Combate
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
            else if (_cachedNearestPedDist > 80f)
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

            // Survival mode override
            if (_currentContext.Session.SurvivalModeActive)
            {
                perf.Level = PerformanceLevel.Survival;
                return;
            }

            // GC spike = critical temporário
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
        public bool ShouldSystemRun(string systemType)
        {
            // Em survival mode, quase nada roda
            if (_currentContext.Session.SurvivalModeActive)
            {
                switch (systemType.ToLower())
                {
                    case "core":
                    case "player":
                        return true;
                    default:
                        return false;
                }
            }

            var player = _currentContext.Player;
            var world = _currentContext.World;

            switch (systemType.ToLower())
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

        public float GetFrequencyMultiplier(string systemType)
        {
            // Survival mode = quase zero
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

            return mult < 0.05f ? 0.05f : (mult > 1f ? 1f : mult);
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

        /// <summary>
        /// Reset de sessão (para quando jogador carrega save, etc)
        /// </summary>
        public void ResetSession()
        {
            _frameCounter = 0;
            _currentContext.Session.TotalFrames = 0;
            _currentContext.Session.MinutesSinceStart = 0;
            _currentContext.Session.SurvivalModeActive = false;
            SafetyLogger.Instance?.Log("[GameplayContextAnalyzer] Sessão resetada");
        }
        #endregion
    }
}