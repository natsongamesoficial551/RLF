using System;
using System.Collections.Generic;
using GTA;

namespace RLF.GTA.Safety
{
    /// <summary>
    /// Controlador central de ticks adaptativos.
    /// OTIMIZADO: Menos alocações, contadores de frame, sem DateTime excessivo.
    /// </summary>
    public sealed class AdaptiveTickController
    {
        #region Singleton
        private static AdaptiveTickController _instance;
        private static readonly object _lock = new object();

        public static AdaptiveTickController Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new AdaptiveTickController();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Enums
        public enum TickPriority
        {
            Critical = 0,
            High = 1,
            Normal = 2,
            Low = 3,
            Background = 4,
            Batch = 5
        }

        public enum SystemCategory
        {
            Core,
            Combat,
            AI,
            Traffic,
            Crime,
            Economy,
            Jobs,
            UI,
            Weather,
            LivingWorld,
            Debug,
            Custom
        }
        #endregion

        #region Data Classes - OTIMIZADO: struct-like
        public class SystemTickConfig
        {
            public string SystemId;
            public string DisplayName;
            public SystemCategory Category;
            public TickPriority Priority;

            // Tick rates em FRAMES (não ms)
            public int NormalTickFrames;
            public int ReducedTickFrames;
            public int MinimalTickFrames;

            public int CurrentTickFrames;
            public int LastTickFrame;
            public int TickCount;
            public int SkippedTicks;

            public bool IsEnabled;
            public bool IsPaused;
            public bool IsFrozen;
            public int FreezeUntilFrame;

            public Action TickCallback;
            public Func<bool> CanRunCallback;
        }

        public class TickStatistics
        {
            public int TotalSystems;
            public int ActiveSystems;
            public int PausedSystems;
            public int SystemsRanThisTick;
        }
        #endregion

        #region Private Fields
        private readonly Dictionary<string, SystemTickConfig> _systems = new Dictionary<string, SystemTickConfig>(32);
        private readonly List<string> _systemIds = new List<string>(32);  // Para iteração rápida
        private readonly List<string> _pendingRemovals = new List<string>(8);
        private readonly List<string> _systemsToRun = new List<string>(16);  // Reutilizado

        private TickStatistics _stats = new TickStatistics();
        private GameplayContextAnalyzer _contextAnalyzer;

        private int _frameCounter;
        private int _lastStatsFrame;

        private bool _globalPause;
        private bool _globalFreeze;
        private int _globalFreezeUntilFrame;

        private const int MAX_SYSTEMS_PER_TICK = 30;
        private const int STATS_INTERVAL_FRAMES = 30;

        // Conversão MS para frames (assumindo ~30fps)
        private const float MS_TO_FRAMES = 0.03f;
        #endregion

        #region Constructor
        private AdaptiveTickController()
        {
            _contextAnalyzer = GameplayContextAnalyzer.Instance;
        }
        #endregion

        #region Public Properties
        public TickStatistics Statistics => _stats;
        public bool IsGloballyPaused => _globalPause;
        public bool IsGloballyFrozen => _globalFreeze && _frameCounter < _globalFreezeUntilFrame;
        public int RegisteredSystemCount => _systems.Count;
        #endregion

        #region System Registration
        public void RegisterSystem(
            string systemId,
            string displayName,
            SystemCategory category,
            TickPriority priority,
            Action tickCallback,
            int normalTickRateMs = 0,
            int reducedTickRateMs = 100,
            int minimalTickRateMs = 500,
            Func<bool> canRunCallback = null)
        {
            if (string.IsNullOrEmpty(systemId) || tickCallback == null)
                return;

            lock (_lock)
            {
                if (_systems.ContainsKey(systemId))
                {
                    _systems[systemId].TickCallback = tickCallback;
                    _systems[systemId].CanRunCallback = canRunCallback;
                    return;
                }

                // Converter MS para frames
                int normalFrames = (int)(normalTickRateMs * MS_TO_FRAMES);
                int reducedFrames = (int)(reducedTickRateMs * MS_TO_FRAMES);
                int minimalFrames = (int)(minimalTickRateMs * MS_TO_FRAMES);

                // Mínimos
                if (reducedFrames < 3) reducedFrames = 3;
                if (minimalFrames < 15) minimalFrames = 15;

                var config = new SystemTickConfig
                {
                    SystemId = systemId,
                    DisplayName = displayName,
                    Category = category,
                    Priority = priority,
                    NormalTickFrames = normalFrames,
                    ReducedTickFrames = reducedFrames,
                    MinimalTickFrames = minimalFrames,
                    CurrentTickFrames = normalFrames,
                    LastTickFrame = 0,
                    IsEnabled = true,
                    TickCallback = tickCallback,
                    CanRunCallback = canRunCallback
                };

                if (priority == TickPriority.Batch)
                {
                    config.CurrentTickFrames = minimalFrames > 30 ? minimalFrames : 30;
                }

                _systems[systemId] = config;
                _systemIds.Add(systemId);

                SafetyLogger.Instance?.Log($"[AdaptiveTickController] Registrado: {displayName} ({category}/{priority})");
            }
        }

        public void UnregisterSystem(string systemId)
        {
            lock (_lock)
            {
                if (_systems.ContainsKey(systemId))
                {
                    _pendingRemovals.Add(systemId);
                }
            }
        }

        public bool IsSystemRegistered(string systemId)
        {
            return _systems.ContainsKey(systemId);
        }
        #endregion

        #region System Control
        public void PauseSystem(string systemId, string reason = null)
        {
            if (_systems.TryGetValue(systemId, out var config))
            {
                config.IsPaused = true;
                SafetyLogger.Instance?.Log($"[AdaptiveTickController] Pausado: {systemId}" +
                    (reason != null ? $" ({reason})" : ""));
            }
        }

        public void ResumeSystem(string systemId)
        {
            if (_systems.TryGetValue(systemId, out var config))
            {
                config.IsPaused = false;
                SafetyLogger.Instance?.Log($"[AdaptiveTickController] Retomado: {systemId}");
            }
        }

        public void FreezeSystem(string systemId, int durationFrames, string reason = null)
        {
            if (_systems.TryGetValue(systemId, out var config))
            {
                config.IsFrozen = true;
                config.FreezeUntilFrame = _frameCounter + durationFrames;
            }
        }

        public void PauseCategory(SystemCategory category, string reason = null)
        {
            foreach (var config in _systems.Values)
            {
                if (config.Category == category)
                    config.IsPaused = true;
            }
        }

        public void ResumeCategory(SystemCategory category)
        {
            foreach (var config in _systems.Values)
            {
                if (config.Category == category)
                    config.IsPaused = false;
            }
        }

        public void GlobalPause(bool pause, string reason = null)
        {
            _globalPause = pause;
        }

        public void GlobalFreeze(int durationFrames, string reason = null)
        {
            _globalFreeze = true;
            _globalFreezeUntilFrame = _frameCounter + durationFrames;
            SafetyLogger.Instance?.Log($"[AdaptiveTickController] Freeze global: {durationFrames} frames");
        }
        #endregion

        #region Main Tick Processing - OTIMIZADO
        public void ProcessTick()
        {
            _frameCounter++;

            // Freeze global
            if (_globalFreeze && _frameCounter < _globalFreezeUntilFrame)
                return;
            _globalFreeze = false;

            // Remoções pendentes
            if (_pendingRemovals.Count > 0)
            {
                lock (_lock)
                {
                    foreach (var id in _pendingRemovals)
                    {
                        _systems.Remove(id);
                        _systemIds.Remove(id);
                    }
                    _pendingRemovals.Clear();
                }
            }

            // Atualizar tick rates (não todo frame em survival)
            bool isSurvival = _contextAnalyzer.IsInSurvivalMode;
            if (!isSurvival || _frameCounter % 30 == 0)
            {
                UpdateTickRates(isSurvival);
            }

            // Determinar quais rodam
            _systemsToRun.Clear();
            DetermineSystemsToRun();

            // Executar
            int ran = 0;
            foreach (var systemId in _systemsToRun)
            {
                if (!_systems.TryGetValue(systemId, out var config))
                    continue;

                try
                {
                    config.TickCallback?.Invoke();
                    config.TickCount++;
                    ran++;
                }
                catch
                {
                    // Erro tratado pelo SafeExecutionManager
                }

                config.LastTickFrame = _frameCounter;
            }

            // Stats espaçadas
            if (_frameCounter - _lastStatsFrame >= STATS_INTERVAL_FRAMES)
            {
                _lastStatsFrame = _frameCounter;
                UpdateStats(ran);
            }
        }

        private void UpdateTickRates(bool survival)
        {
            foreach (var config in _systems.Values)
            {
                if (config.Priority == TickPriority.Critical)
                {
                    config.CurrentTickFrames = config.NormalTickFrames;
                    continue;
                }

                // Survival = mínimo para todos
                if (survival)
                {
                    config.CurrentTickFrames = config.MinimalTickFrames * 2;
                    continue;
                }

                float mult = _contextAnalyzer.GetFrequencyMultiplier(config.Category.ToString());

                // Ajuste por prioridade
                switch (config.Priority)
                {
                    case TickPriority.High: mult = mult < 0.5f ? 0.5f : mult; break;
                    case TickPriority.Low: mult *= 0.7f; break;
                    case TickPriority.Background: mult *= 0.5f; break;
                    case TickPriority.Batch: mult *= 0.3f; break;
                }

                // Calcular frame target
                int target;
                if (mult <= 0.2f)
                    target = config.MinimalTickFrames;
                else if (mult <= 0.6f)
                    target = config.ReducedTickFrames;
                else
                    target = config.NormalTickFrames;

                // Suavizar transição
                int diff = target - config.CurrentTickFrames;
                if (diff > 2 || diff < -2)
                    config.CurrentTickFrames += diff / 3;
                else
                    config.CurrentTickFrames = target;
            }
        }

        private void DetermineSystemsToRun()
        {
            int count = 0;

            // Iterar por prioridade (Critical primeiro)
            for (int priority = 0; priority <= 5 && count < MAX_SYSTEMS_PER_TICK; priority++)
            {
                foreach (var systemId in _systemIds)
                {
                    if (count >= MAX_SYSTEMS_PER_TICK)
                        break;

                    if (!_systems.TryGetValue(systemId, out var config))
                        continue;

                    if ((int)config.Priority != priority)
                        continue;

                    if (!config.IsEnabled || config.IsPaused)
                        continue;

                    // Freeze check
                    if (config.IsFrozen && _frameCounter < config.FreezeUntilFrame)
                        continue;
                    config.IsFrozen = false;

                    // Pausa global (exceto critical)
                    if (_globalPause && config.Priority != TickPriority.Critical)
                        continue;

                    // CanRun callback
                    if (config.CanRunCallback != null && !config.CanRunCallback())
                    {
                        config.SkippedTicks++;
                        continue;
                    }

                    // Context check
                    if (!_contextAnalyzer.ShouldSystemRun(config.SystemId))
                    {
                        config.SkippedTicks++;
                        continue;
                    }

                    // Frame rate check
                    int framesSince = _frameCounter - config.LastTickFrame;
                    if (framesSince < config.CurrentTickFrames)
                        continue;

                    _systemsToRun.Add(systemId);
                    count++;
                }
            }
        }

        private void UpdateStats(int ran)
        {
            _stats.TotalSystems = _systems.Count;
            _stats.SystemsRanThisTick = ran;

            int active = 0, paused = 0;
            foreach (var c in _systems.Values)
            {
                if (c.IsPaused) paused++;
                else if (c.IsEnabled) active++;
            }
            _stats.ActiveSystems = active;
            _stats.PausedSystems = paused;
        }
        #endregion

        #region Query Methods
        public SystemTickConfig GetSystemConfig(string systemId)
        {
            _systems.TryGetValue(systemId, out var config);
            return config;
        }

        public IEnumerable<SystemTickConfig> GetSystemsByCategory(SystemCategory category)
        {
            foreach (var config in _systems.Values)
            {
                if (config.Category == category)
                    yield return config;
            }
        }

        public int GetCurrentTickRate(string systemId)
        {
            if (_systems.TryGetValue(systemId, out var config))
                return config.CurrentTickFrames;
            return -1;
        }

        public bool IsSystemActive(string systemId)
        {
            if (_systems.TryGetValue(systemId, out var config))
                return config.IsEnabled && !config.IsPaused && !config.IsFrozen;
            return false;
        }

        public Dictionary<string, object> GetStatusReport()
        {
            return new Dictionary<string, object>
            {
                ["TotalSystems"] = _stats.TotalSystems,
                ["ActiveSystems"] = _stats.ActiveSystems,
                ["PausedSystems"] = _stats.PausedSystems,
                ["GlobalPaused"] = _globalPause,
                ["GlobalFrozen"] = _globalFreeze,
                ["Frame"] = _frameCounter
            };
        }
        #endregion

        #region Batch Helper
        public void RegisterBatchSystem(
            string systemId,
            string displayName,
            SystemCategory category,
            Action tickCallback,
            int batchIntervalMs = 1000)
        {
            RegisterSystem(
                systemId, displayName, category,
                TickPriority.Batch, tickCallback,
                batchIntervalMs, batchIntervalMs * 2, batchIntervalMs * 4
            );
        }
        #endregion
    }
}