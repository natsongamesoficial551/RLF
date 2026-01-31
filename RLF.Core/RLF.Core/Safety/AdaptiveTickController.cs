using System;
using System.Collections.Generic;

namespace RLF.Core.Safety
{
    /// <summary>
    /// Controlador central de ticks adaptativos.
    /// REFINADO: Usa SystemCategory diretamente no GetFrequencyMultiplier.
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

        #region Fields

        private readonly Dictionary<string, SystemTickConfig> _systems = new Dictionary<string, SystemTickConfig>(32);
        private readonly List<string> _systemIds = new List<string>(32);
        private readonly List<string> _pendingRemovals = new List<string>(8);
        private readonly List<string> _systemsToRun = new List<string>(16);

        private TickStatistics _stats = new TickStatistics();
        private GameplayContextAnalyzer _contextAnalyzer;

        private int _frameCounter;
        private int _lastStatsFrame;

        private bool _globalPause;
        private bool _globalFreeze;
        private int _globalFreezeUntilFrame;

        private const int MAX_SYSTEMS_PER_TICK = 30;
        private const int STATS_INTERVAL_FRAMES = 30;
        private const float MS_TO_FRAMES = 0.03f;

        #endregion

        #region Constructor

        private AdaptiveTickController()
        {
            _contextAnalyzer = GameplayContextAnalyzer.Instance;
        }

        #endregion

        #region Properties

        public TickStatistics Statistics => _stats;
        public bool IsGloballyPaused => _globalPause;
        public bool IsGloballyFrozen => _globalFreeze && _frameCounter < _globalFreezeUntilFrame;
        public int RegisteredSystemCount => _systems.Count;

        #endregion

        #region Registration

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

                int normalFrames = (int)(normalTickRateMs * MS_TO_FRAMES);
                int reducedFrames = (int)(reducedTickRateMs * MS_TO_FRAMES);
                int minimalFrames = (int)(minimalTickRateMs * MS_TO_FRAMES);

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

                SafetyLogger.Instance?.Log($"[AdaptiveTickController] Registered: {displayName} ({category}/{priority})");
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

        #region Control

        public void PauseSystem(string systemId, string reason = null)
        {
            if (_systems.TryGetValue(systemId, out var config))
            {
                config.IsPaused = true;
                SafetyLogger.Instance?.Log($"[AdaptiveTickController] Paused: {systemId}" +
                    (reason != null ? $" ({reason})" : ""));
            }
        }

        public void ResumeSystem(string systemId)
        {
            if (_systems.TryGetValue(systemId, out var config))
            {
                config.IsPaused = false;
                SafetyLogger.Instance?.Log($"[AdaptiveTickController] Resumed: {systemId}");
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
            SafetyLogger.Instance?.Log($"[AdaptiveTickController] Global freeze: {durationFrames} frames");
        }

        #endregion

        #region Main Tick

        public void ProcessTick()
        {
            _frameCounter++;

            if (_globalFreeze && _frameCounter < _globalFreezeUntilFrame)
                return;
            _globalFreeze = false;

            // Pending removals
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

            // Update tick rates
            bool isSurvival = _contextAnalyzer.IsInSurvivalMode;
            if (!isSurvival || _frameCounter % 30 == 0)
            {
                UpdateTickRates(isSurvival);
            }

            // Determine which systems run
            _systemsToRun.Clear();
            DetermineSystemsToRun();

            // Execute
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
                catch (Exception ex)
                {
                    SafetyLogger.Instance?.LogError($"[AdaptiveTickController] Error in {systemId}: {ex.Message}");
                }

                config.LastTickFrame = _frameCounter;
            }

            // Stats
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

                if (survival)
                {
                    config.CurrentTickFrames = config.MinimalTickFrames * 2;
                    continue;
                }

                // REFINADO: Usa SystemCategory diretamente
                float mult = _contextAnalyzer.GetFrequencyMultiplier(config.Category);

                switch (config.Priority)
                {
                    case TickPriority.High: mult = mult < 0.5f ? 0.5f : mult; break;
                    case TickPriority.Low: mult *= 0.7f; break;
                    case TickPriority.Background: mult *= 0.5f; break;
                    case TickPriority.Batch: mult *= 0.3f; break;
                }

                int target;
                if (mult <= 0.2f)
                    target = config.MinimalTickFrames;
                else if (mult <= 0.6f)
                    target = config.ReducedTickFrames;
                else
                    target = config.NormalTickFrames;

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

                    if (config.IsFrozen && _frameCounter < config.FreezeUntilFrame)
                        continue;
                    config.IsFrozen = false;

                    if (_globalPause && config.Priority != TickPriority.Critical)
                        continue;

                    if (config.CanRunCallback != null && !config.CanRunCallback())
                    {
                        config.SkippedTicks++;
                        continue;
                    }

                    if (!_contextAnalyzer.ShouldSystemRun(config.SystemId))
                    {
                        config.SkippedTicks++;
                        continue;
                    }

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

        #region Query

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