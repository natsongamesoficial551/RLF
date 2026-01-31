using System;
using System.Collections.Generic;
using System.IO;

namespace RLF.Core.Safety
{
    /// <summary>
    /// Gerenciador principal de execução segura.
    /// REFINADO: Proteção contra shutdown duplo.
    /// </summary>
    public sealed class SafeExecutionManager
    {
        #region Singleton

        private static SafeExecutionManager _instance;
        private static readonly object _lock = new object();

        public static SafeExecutionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new SafeExecutionManager();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Components

        private GameplayContextAnalyzer _contextAnalyzer;
        private AdaptiveTickController _tickController;
        private ScriptActivityMonitor _activityMonitor;

        #endregion

        #region Fields

        private bool _initialized;
        private bool _shutdown;  // REFINADO: flag para evitar shutdown duplo
        private int _frameCounter;
        private int _lastHealthCheckFrame;
        private int _crashesPrevented;
        private string _logDirectory;

        private const int HEALTH_CHECK_INTERVAL = 150;

        #endregion

        #region Constructor

        private SafeExecutionManager() { }

        #endregion

        #region Properties

        public bool IsInitialized => _initialized;
        public bool IsShutdown => _shutdown;  // REFINADO: expõe estado
        public int CrashesPrevented => _crashesPrevented;

        #endregion

        #region Initialization

        public void Initialize(string logDirectory, ISafetyDataProvider dataProvider)
        {
            // REFINADO: evita re-init e init após shutdown
            if (_initialized || _shutdown)
                return;

            try
            {
                _logDirectory = logDirectory;

                if (!Directory.Exists(_logDirectory))
                    Directory.CreateDirectory(_logDirectory);

                SafetyLogger.Instance.Initialize(_logDirectory);
                SafetyLogger.Instance.Log("=== RLF Safety System v2.0 (Core) ===");

                _contextAnalyzer = GameplayContextAnalyzer.Instance;
                _tickController = AdaptiveTickController.Instance;
                _activityMonitor = ScriptActivityMonitor.Instance;

                _contextAnalyzer.SetDataProvider(dataProvider);
                _activityMonitor.RegisterScript("RLF.Safety.Core", "Safety Core");

                _initialized = true;
                SafetyLogger.Instance.Log("[SafeExecutionManager] Initialized successfully");
            }
            catch (Exception ex)
            {
                SafetyLogger.Instance?.LogError($"[SafeExecutionManager] Init error: {ex.Message}");
            }
        }

        #endregion

        #region Main Tick

        public void Tick()
        {
            if (!_initialized || _shutdown) return;

            _frameCounter++;

            var execCtx = _activityMonitor.BeginExecution("RLF.Safety.Core", "MainTick");
            if (execCtx == null) return;

            try
            {
                _contextAnalyzer.Analyze();
                _tickController.ProcessTick();
                _activityMonitor.MonitorTick();
                SafetyLogger.Instance.Tick();

                if (_frameCounter - _lastHealthCheckFrame >= HEALTH_CHECK_INTERVAL)
                {
                    _lastHealthCheckFrame = _frameCounter;
                    PerformHealthCheck();
                }

                _activityMonitor.EndExecution(execCtx);
            }
            catch (Exception ex)
            {
                _activityMonitor.RecordException("RLF.Safety.Core", ex, "MainTick");
                _crashesPrevented++;
            }
        }

        #endregion

        #region Public API

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
            if (!_initialized || _shutdown) return;

            _activityMonitor.RegisterScript(systemId, displayName);

            Action safeCallback = () => ExecuteSystemSafely(systemId, tickCallback);

            _tickController.RegisterSystem(
                systemId, displayName, category, priority,
                safeCallback, normalTickRateMs, reducedTickRateMs, minimalTickRateMs,
                canRunCallback
            );
        }

        public void RegisterBatchSystem(
            string systemId,
            string displayName,
            SystemCategory category,
            Action tickCallback,
            int batchIntervalMs = 1000)
        {
            RegisterSystem(
                systemId, displayName, category,
                TickPriority.Batch,
                tickCallback, batchIntervalMs, batchIntervalMs * 2, batchIntervalMs * 4
            );
        }

        public void UnregisterSystem(string systemId)
        {
            _tickController.UnregisterSystem(systemId);
            _activityMonitor.UnregisterScript(systemId);
        }

        public bool ExecuteSafely(string contextName, Action action)
        {
            if (action == null || _shutdown) return false;

            var ctx = _activityMonitor.BeginExecution(contextName, "Safe");
            if (ctx == null) return false;

            try
            {
                action();
                _activityMonitor.EndExecution(ctx);
                return true;
            }
            catch (Exception ex)
            {
                _activityMonitor.RecordException(contextName, ex);
                _crashesPrevented++;
                return false;
            }
        }

        public T ExecuteSafely<T>(string contextName, Func<T> func, T defaultValue = default)
        {
            if (func == null || _shutdown) return defaultValue;

            var ctx = _activityMonitor.BeginExecution(contextName, "Safe<T>");
            if (ctx == null) return defaultValue;

            try
            {
                var result = func();
                _activityMonitor.EndExecution(ctx);
                return result;
            }
            catch (Exception ex)
            {
                _activityMonitor.RecordException(contextName, ex);
                _crashesPrevented++;
                return defaultValue;
            }
        }

        public void PauseSystem(string systemId, string reason = null)
        {
            _tickController.PauseSystem(systemId, reason);
        }

        public void ResumeSystem(string systemId)
        {
            _tickController.ResumeSystem(systemId);
        }

        public void PauseCategory(SystemCategory category, string reason = null)
        {
            _tickController.PauseCategory(category, reason);
        }

        public void ResumeCategory(SystemCategory category)
        {
            _tickController.ResumeCategory(category);
        }

        public FullContext GetCurrentContext()
        {
            return _contextAnalyzer.CurrentContext;
        }

        public bool IsInProtectionMode()
        {
            return _contextAnalyzer.IsInProtectionMode;
        }

        public bool IsInSurvivalMode()
        {
            return _contextAnalyzer.IsInSurvivalMode;
        }

        public PerformanceLevel GetPerformanceLevel()
        {
            return _contextAnalyzer.CurrentPerformanceLevel;
        }

        public void RegisterActiveEvent()
        {
            _contextAnalyzer.RegisterActiveEvent();
        }

        public void UnregisterActiveEvent()
        {
            _contextAnalyzer.UnregisterActiveEvent();
        }

        public bool ForceSystemRecovery(string systemId)
        {
            bool ok = _activityMonitor.ForceRecovery(systemId);
            if (ok) _tickController.ResumeSystem(systemId);
            return ok;
        }

        public void ResetSession()
        {
            _contextAnalyzer.ResetSession();
            SafetyLogger.Instance?.Log("[SafeExecutionManager] Session reset by user");
        }

        public Dictionary<string, object> GetFullStatusReport()
        {
            var ctx = _contextAnalyzer.CurrentContext;
            return new Dictionary<string, object>
            {
                ["SessionMinutes"] = ctx.Session.MinutesSinceStart,
                ["SurvivalMode"] = ctx.Session.SurvivalModeActive,
                ["TotalFrames"] = _frameCounter,
                ["CrashesPrevented"] = _crashesPrevented,
                ["PlayerState"] = ctx.Player.ActivityState.ToString(),
                ["WorldState"] = ctx.World.CurrentState.ToString(),
                ["PerformanceLevel"] = ctx.Performance.Level.ToString(),
                ["AvgFPS"] = ctx.Performance.AverageFPS,
                ["TickController"] = _tickController.GetStatusReport(),
                ["Monitor"] = _activityMonitor.GetHealthReport()
            };
        }

        #endregion

        #region Private Methods

        private void ExecuteSystemSafely(string systemId, Action callback)
        {
            if (_shutdown) return;

            if (!_activityMonitor.CanScriptExecute(systemId))
                return;

            var ctx = _activityMonitor.BeginExecution(systemId, "Tick");
            if (ctx == null) return;

            try
            {
                callback();
                _activityMonitor.EndExecution(ctx);
            }
            catch (Exception ex)
            {
                _activityMonitor.RecordException(systemId, ex, "Tick");
                _crashesPrevented++;
            }
        }

        private void PerformHealthCheck()
        {
            if (_shutdown) return;

            var ctx = _contextAnalyzer.CurrentContext;
            var stats = _tickController.Statistics;

            SafetyLogger.Instance?.Log(
                $"[Health] Min:{ctx.Session.MinutesSinceStart} " +
                $"FPS:{ctx.Performance.AverageFPS:F0} " +
                $"Mode:{ctx.Performance.Level} " +
                $"Systems:{stats.ActiveSystems}/{stats.TotalSystems} " +
                $"Survival:{ctx.Session.SurvivalModeActive}"
            );

            PerformAutomaticAdjustments(ctx);
        }

        private void PerformAutomaticAdjustments(FullContext ctx)
        {
            if (ctx.Player.ActivityState == PlayerActivityState.InMenu)
            {
                _tickController.PauseCategory(SystemCategory.AI, "Menu");
                _tickController.PauseCategory(SystemCategory.Crime, "Menu");
                _tickController.PauseCategory(SystemCategory.LivingWorld, "Menu");
            }

            if (ctx.Player.ActivityState == PlayerActivityState.InCutscene)
            {
                _tickController.GlobalPause(true, "Cutscene");
            }
            else if (_tickController.IsGloballyPaused)
            {
                _tickController.GlobalPause(false);
                _tickController.ResumeCategory(SystemCategory.AI);
                _tickController.ResumeCategory(SystemCategory.Crime);
                _tickController.ResumeCategory(SystemCategory.LivingWorld);
            }

            if (ctx.Performance.Level == PerformanceLevel.Critical)
            {
                _tickController.GlobalFreeze(3, "Critical performance");
            }

            if (ctx.Session.SurvivalModeActive)
            {
                _tickController.PauseCategory(SystemCategory.LivingWorld, "Survival");
                _tickController.PauseCategory(SystemCategory.Debug, "Survival");
            }
        }

        #endregion

        #region Shutdown

        public void Shutdown()
        {
            // REFINADO: proteção contra shutdown duplo
            if (_shutdown) return;

            lock (_lock)
            {
                if (_shutdown) return;  // Double-check
                _shutdown = true;

                if (!_initialized) return;

                var ctx = _contextAnalyzer?.CurrentContext;
                SafetyLogger.Instance?.Log("=== RLF Safety System Shutting Down ===");

                if (ctx != null)
                {
                    SafetyLogger.Instance?.Log($"Session: {ctx.Session.MinutesSinceStart} minutes");
                }

                SafetyLogger.Instance?.Log($"Frames: {_frameCounter}");
                SafetyLogger.Instance?.Log($"Crashes prevented: {_crashesPrevented}");
                SafetyLogger.Instance?.Shutdown();

                _initialized = false;
            }
        }

        #endregion
    }
}