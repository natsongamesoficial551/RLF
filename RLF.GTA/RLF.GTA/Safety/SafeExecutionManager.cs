using System;
using System.Collections.Generic;
using System.IO;
using GTA;

namespace RLF.GTA.Safety
{
    /// <summary>
    /// Gerenciador principal de execução segura do RLF.
    /// OTIMIZADO: Menos overhead por tick, health checks espaçados.
    /// </summary>
    public sealed class SafeExecutionManager : Script
    {
        #region Singleton
        private static SafeExecutionManager _instance;
        public static SafeExecutionManager Instance => _instance;
        #endregion

        #region Components
        private GameplayContextAnalyzer _contextAnalyzer;
        private AdaptiveTickController _tickController;
        private ScriptActivityMonitor _activityMonitor;
        #endregion

        #region Private Fields
        private bool _initialized;
        private int _frameCounter;
        private int _lastHealthCheckFrame;
        private int _crashesPrevented;

        private const int HEALTH_CHECK_INTERVAL = 150;  // ~5 segundos
        private const string LOG_DIR = "scripts/RLF/Logs";
        #endregion

        #region Constructor
        public SafeExecutionManager()
        {
            _instance = this;
            Tick += OnTick;
            Aborted += OnAborted;
            Initialize();
        }
        #endregion

        #region Initialization
        private void Initialize()
        {
            try
            {
                EnsureLogDirectory();
                SafetyLogger.Instance.Initialize();
                SafetyLogger.Instance.Log("=== RLF Safety System v2.0 (Optimized) ===");

                _contextAnalyzer = GameplayContextAnalyzer.Instance;
                _tickController = AdaptiveTickController.Instance;
                _activityMonitor = ScriptActivityMonitor.Instance;

                _activityMonitor.RegisterScript("RLF.Safety.Core", "Safety Core");

                _initialized = true;
                SafetyLogger.Instance.Log("[SafeExecutionManager] Inicializado com sucesso");
            }
            catch (Exception ex)
            {
                SafetyLogger.Instance?.LogError($"[SafeExecutionManager] Erro init: {ex.Message}");
            }
        }

        private void EnsureLogDirectory()
        {
            try
            {
                if (!Directory.Exists(LOG_DIR))
                    Directory.CreateDirectory(LOG_DIR);
            }
            catch { }
        }
        #endregion

        #region Main Loop - OTIMIZADO
        private void OnTick(object sender, EventArgs e)
        {
            if (!_initialized) return;

            _frameCounter++;

            var execCtx = _activityMonitor.BeginExecution("RLF.Safety.Core", "MainTick");
            if (execCtx == null) return;

            try
            {
                // 1. Análise de contexto (já otimizada internamente)
                _contextAnalyzer.Analyze();

                // 2. Processar ticks dos sistemas
                _tickController.ProcessTick();

                // 3. Monitor (já com throttle interno)
                _activityMonitor.MonitorTick();

                // 4. Health check espaçado
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

        private void OnAborted(object sender, EventArgs e)
        {
            var ctx = _contextAnalyzer.CurrentContext;
            SafetyLogger.Instance?.Log("=== RLF Safety System Finalizando ===");
            SafetyLogger.Instance?.Log($"Sessão: {ctx.Session.MinutesSinceStart} minutos");
            SafetyLogger.Instance?.Log($"Frames: {_frameCounter}");
            SafetyLogger.Instance?.Log($"Crashes prevenidos: {_crashesPrevented}");
            SafetyLogger.Instance?.Log($"Survival mode ativo: {ctx.Session.SurvivalModeActive}");
            SafetyLogger.Instance?.Shutdown();
        }
        #endregion

        #region Public API
        public void RegisterSystem(
            string systemId,
            string displayName,
            AdaptiveTickController.SystemCategory category,
            AdaptiveTickController.TickPriority priority,
            Action tickCallback,
            int normalTickRateMs = 0,
            int reducedTickRateMs = 100,
            int minimalTickRateMs = 500,
            Func<bool> canRunCallback = null)
        {
            if (!_initialized) return;

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
            AdaptiveTickController.SystemCategory category,
            Action tickCallback,
            int batchIntervalMs = 1000)
        {
            RegisterSystem(
                systemId, displayName, category,
                AdaptiveTickController.TickPriority.Batch,
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
            if (action == null) return false;

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
            if (func == null) return defaultValue;

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

        public void PauseCategory(AdaptiveTickController.SystemCategory category, string reason = null)
        {
            _tickController.PauseCategory(category, reason);
        }

        public void ResumeCategory(AdaptiveTickController.SystemCategory category)
        {
            _tickController.ResumeCategory(category);
        }

        public GameplayContextAnalyzer.FullContext GetCurrentContext()
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

        public GameplayContextAnalyzer.PerformanceLevel GetPerformanceLevel()
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

        /// <summary>
        /// Reset de sessão - chamar quando jogador carrega save
        /// </summary>
        public void ResetSession()
        {
            _contextAnalyzer.ResetSession();
            SafetyLogger.Instance?.Log("[SafeExecutionManager] Sessão resetada pelo usuário");
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
            var ctx = _contextAnalyzer.CurrentContext;
            var stats = _tickController.Statistics;

            // Log periódico
            SafetyLogger.Instance?.Log(
                $"[Health] Min:{ctx.Session.MinutesSinceStart} " +
                $"FPS:{ctx.Performance.AverageFPS:F0} " +
                $"Mode:{ctx.Performance.Level} " +
                $"Systems:{stats.ActiveSystems}/{stats.TotalSystems} " +
                $"Survival:{ctx.Session.SurvivalModeActive}"
            );

            // Ajustes automáticos
            PerformAutomaticAdjustments(ctx);
        }

        private void PerformAutomaticAdjustments(GameplayContextAnalyzer.FullContext ctx)
        {
            // Menu -> pausar categorias pesadas
            if (ctx.Player.ActivityState == GameplayContextAnalyzer.PlayerActivityState.InMenu)
            {
                _tickController.PauseCategory(AdaptiveTickController.SystemCategory.AI, "Menu");
                _tickController.PauseCategory(AdaptiveTickController.SystemCategory.Crime, "Menu");
                _tickController.PauseCategory(AdaptiveTickController.SystemCategory.LivingWorld, "Menu");
            }

            // Cutscene -> freeze global
            if (ctx.Player.ActivityState == GameplayContextAnalyzer.PlayerActivityState.InCutscene)
            {
                _tickController.GlobalPause(true, "Cutscene");
            }
            else if (_tickController.IsGloballyPaused)
            {
                _tickController.GlobalPause(false);
                _tickController.ResumeCategory(AdaptiveTickController.SystemCategory.AI);
                _tickController.ResumeCategory(AdaptiveTickController.SystemCategory.Crime);
                _tickController.ResumeCategory(AdaptiveTickController.SystemCategory.LivingWorld);
            }

            // Performance crítica -> freeze
            if (ctx.Performance.Level == GameplayContextAnalyzer.PerformanceLevel.Critical)
            {
                _tickController.GlobalFreeze(3, "Critical performance");
            }

            // Survival mode -> pausar tudo não essencial
            if (ctx.Session.SurvivalModeActive)
            {
                _tickController.PauseCategory(AdaptiveTickController.SystemCategory.LivingWorld, "Survival");
                _tickController.PauseCategory(AdaptiveTickController.SystemCategory.Debug, "Survival");
            }
        }
        #endregion
    }

    #region Safety Logger - OTIMIZADO
    public sealed class SafetyLogger
    {
        private static SafetyLogger _instance;
        private static readonly object _lock = new object();

        public static SafetyLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new SafetyLogger();
                    }
                }
                return _instance;
            }
        }

        private StreamWriter _writer;
        private readonly List<string> _buffer = new List<string>(32);
        private int _framesSinceFlush;
        private bool _initialized;

        private const string LOG_PATH = "scripts/RLF/Logs/Safety.log";
        private const int BUFFER_MAX = 20;
        private const int FLUSH_FRAMES = 90;  // ~3 segundos

        public void Initialize()
        {
            try
            {
                string dir = Path.GetDirectoryName(LOG_PATH);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Rotação se muito grande
                if (File.Exists(LOG_PATH))
                {
                    var fi = new FileInfo(LOG_PATH);
                    if (fi.Length > 5 * 1024 * 1024) // 5MB
                    {
                        string backup = LOG_PATH + ".old";
                        if (File.Exists(backup)) File.Delete(backup);
                        File.Move(LOG_PATH, backup);
                    }
                }

                _writer = new StreamWriter(LOG_PATH, true);
                _initialized = true;
            }
            catch
            {
                _initialized = false;
            }
        }

        public void Log(string message)
        {
            if (!_initialized) return;

            // Formato simples, sem DateTime.Now.ToString elaborado
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";

            lock (_lock)
            {
                _buffer.Add(line);
                _framesSinceFlush++;

                if (_buffer.Count >= BUFFER_MAX || _framesSinceFlush >= FLUSH_FRAMES)
                {
                    Flush();
                }
            }
        }

        public void LogError(string message)
        {
            Log($"[ERROR] {message}");
        }

        public void LogWarning(string message)
        {
            Log($"[WARN] {message}");
        }

        private void Flush()
        {
            if (!_initialized || _writer == null || _buffer.Count == 0)
                return;

            try
            {
                foreach (var line in _buffer)
                    _writer.WriteLine(line);
                _writer.Flush();
                _buffer.Clear();
                _framesSinceFlush = 0;
            }
            catch { }
        }

        public void Shutdown()
        {
            lock (_lock)
            {
                Flush();
                try
                {
                    _writer?.Close();
                    _writer?.Dispose();
                }
                catch { }
                _initialized = false;
            }
        }
    }
    #endregion
}