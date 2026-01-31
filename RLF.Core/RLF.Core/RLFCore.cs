using RLF.Core.Configuration;
using RLF.Core.Debug;
using RLF.Core.Economy;
using RLF.Core.Economy.Debt;
using RLF.Core.Economy.Expenses;
using RLF.Core.Economy.Wallet;
using RLF.Core.Entities;
using RLF.Core.Events;
using RLF.Core.Events.EventArgs;
using RLF.Core.Identity;
using RLF.Core.Identity.Debug;
using RLF.Core.IO;
using RLF.Core.Law;
using RLF.Core.Law.Police;
using RLF.Core.Logging;        // Logger original
using RLF.Core.Loggin;         // OptimizedLogger, LogBuffer, etc.
using RLF.Core.Performance;
using RLF.Core.Pooling;
using RLF.Core.Scheduling;
using RLF.Core.Systems;
using RLF.Core.Utilities;
using RLF.Core.Vehicles;
using RLF.Core.CharacterCreator.Storage;
using RLF.Core.Watchdog;
using System;
using System.IO;

namespace RLF.Core
{
    public enum CoreState
    {
        Uninitialized,
        Initializing,
        Running,
        Paused,
        ShuttingDown,
        Stopped
    }

    /// <summary>
    /// Núcleo do Real Life Framework.
    /// Inicialização segura, eventos desacoplados, scheduler otimizado e shutdown limpo.
    /// </summary>
    public sealed class RLFCore
    {
        #region Singleton

        private static RLFCore _instance;
        private static readonly object _instanceLock = new object();

        public static RLFCore Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                            _instance = new RLFCore();
                    }
                }
                return _instance;
            }
        }

        public VehicleOwnershipSystem VehicleOwnership { get; private set; }

        #endregion

        #region Fields

        private Logger _logger;
        private EventManager _eventManager;
        private IniReader _config;
        private SystemRegistry _systemRegistry;

        // 🚀 Performance & Scheduling
        private PerformanceConfig _performanceConfig;
        private TickProfiler _tickProfiler;
        private TaskScheduler _taskScheduler;

        // 🛡️ Watchdog & Entities
        private EntityRegistry _entityRegistry;
        private Watchdog.Watchdog _watchdog;
        private WatchdogConfig _watchdogConfig;

        // 📝 Logging Otimizado
        private OptimizedLogger _optimizedLogger;

        // 📁 I/O Assíncrono
        private AsyncFileQueue _fileQueue;

        // 🔎 Identity Debug
        private IdentityDebugListener _identityDebugListener;

        // 💰 Economy
        private EconomySystem _economySystem;

        // 🧑 Character Creator
        private CharacterStore _characterStore;

        private CoreState _state;
        private readonly object _stateLock = new object();

        private bool _debugMode;
        private string _configPath;
        private string _logDirectory;
        private string _dataPath;

        private readonly Version _version;
        private DateTime _initializationTime;

        #endregion

        #region Properties

        public Logger Logger => _logger;
        public EventManager EventManager => _eventManager;
        public IniReader Config => _config;
        public SystemRegistry Systems => _systemRegistry;

        // 🚀 Performance
        public PerformanceConfig PerformanceConfig => _performanceConfig;
        public TickProfiler Profiler => _tickProfiler;
        public TaskScheduler Scheduler => _taskScheduler;

        // 🛡️ Entities & Watchdog
        public EntityRegistry Entities => _entityRegistry;
        public Watchdog.Watchdog Watchdog => _watchdog;

        // 📝 Logging Otimizado
        public OptimizedLogger OptimizedLogger => _optimizedLogger;

        // 📁 I/O Assíncrono
        public AsyncFileQueue FileQueue => _fileQueue;

        // 💰 Economia
        public EconomySystem Economy => _economySystem;

        // 🧑 Character Store
        public CharacterStore CharacterStore => _characterStore;

        // 📁 Caminho de dados
        public string DataPath => _dataPath;

        public CoreState State
        {
            get
            {
                lock (_stateLock)
                    return _state;
            }
        }

        public bool DebugMode => _debugMode;
        public Version Version => _version;

        public TimeSpan Uptime =>
            _state == CoreState.Uninitialized
                ? TimeSpan.Zero
                : DateTime.Now - _initializationTime;

        #endregion

        #region Constructor

        private RLFCore()
        {
            _state = CoreState.Uninitialized;
            _version = new Version(1, 1, 0);

            _debugMode = false;
            _configPath = "RLF.ini";
            _logDirectory = "Logs";
            _dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Rockstar Games",
                "GTA V",
                "RLF"
            );
        }

        #endregion

        #region Initialization

        public bool Initialize()
        {
            return Initialize(_configPath, _logDirectory, false);
        }

        public bool Initialize(string configPath, string logDirectory, bool debugMode)
        {
            VehicleOwnership = new VehicleOwnershipSystem();

            lock (_stateLock)
            {
                if (_state != CoreState.Uninitialized)
                    return _state == CoreState.Running;

                _state = CoreState.Initializing;
            }

            try
            {
                _configPath = string.IsNullOrWhiteSpace(configPath) ? "RLF.ini" : configPath;
                _logDirectory = string.IsNullOrWhiteSpace(logDirectory) ? "Logs" : logDirectory;
                _debugMode = debugMode;

                // 1️⃣ Diretórios
                EnsureDataDirectory();

                // 2️⃣ Logger básico (não depende de config)
                if (!InitializeLogger())
                    return FailInit("Logger");

                // 3️⃣ Configuração
                if (!InitializeConfiguration())
                    return FailInit("Configuration");

                ApplyConfiguration();
                InitializeDebug();

                // 4️⃣ EventManager
                if (!InitializeEventManager())
                    return FailInit("EventManager");

                // 5️⃣ Performance (Scheduler + Profiler)
                if (!InitializePerformance())
                    return FailInit("Performance");

                SafeExecutor.Logger = _logger;

                // 6️⃣ EntityRegistry (DEPOIS do scheduler)
                if (!InitializeEntityRegistry())
                    return FailInit("EntityRegistry");

                // 7️⃣ Sistemas
                if (!InitializeSystems())
                    return FailInit("Systems");

                // 8️⃣ Watchdog (DEPOIS dos sistemas)
                if (!InitializeWatchdog())
                    return FailInit("Watchdog");

                // 9️⃣ Logging otimizado (DEPOIS do config)
                InitializeOptimizedLogger();

                // 🔟 I/O assíncrono (DEPOIS do config)
                InitializeFileQueue();

                // 1️⃣1️⃣ Character Creator
                if (!InitializeCharacterCreator())
                    return FailInit("CharacterCreator");

                _initializationTime = DateTime.Now;

                SetState(CoreState.Running);
                RaiseEvent("core:initialized", new RLFEventArgs());

                _logger.Info($"RLF Core v{_version} inicializado com sucesso");
                _logger.Info($"[Performance] Scheduler={_performanceConfig.SchedulerEnabled}, Budget={_performanceConfig.TickBudgetMs}ms");
                _logger.Info($"[Entities] Registry ativo, Cleanup={_config.GetFloat("Entities", "CleanupIntervalSeconds", 10f)}s");

                RLFDebug.Info(DebugChannel.Core, $"RLF Core v{_version} inicializado (State=Running)");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Critical("Falha crítica na inicialização do Core", ex);
                try { RLFDebug.Critical(DebugChannel.Crash, "Falha crítica na inicialização do Core", ex); } catch { }
                SetState(CoreState.Stopped);
                return false;
            }
        }

        private void EnsureDataDirectory()
        {
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }
        }

        private bool FailInit(string step)
        {
            _logger?.Critical($"Falha ao inicializar: {step}");
            try { RLFDebug.Critical(DebugChannel.Crash, $"Falha ao inicializar: {step}"); } catch { }
            SetState(CoreState.Stopped);
            return false;
        }

        #endregion

        #region Logger

        private bool InitializeLogger()
        {
            try
            {
                _logger = new Logger(
                    logDirectory: _logDirectory,
                    logFileName: "RLF_Core",
                    minLogLevel: _debugMode ? LogLevel.Debug : LogLevel.Info,
                    debugMode: _debugMode
                );

                return _logger.Initialize();
            }
            catch
            {
                return false;
            }
        }

        private bool InitializeOptimizedLogger()
        {
            try
            {
                bool useOptimized = _config.GetBool("Logging", "UseOptimizedLogger", true);

                if (!useOptimized)
                    return true;

                int bufferCapacity = _config.GetInt("Logging", "BufferCapacity", 500);
                int flushIntervalMs = _config.GetInt("Logging", "FlushIntervalMs", 5000);
                int flushThreshold = _config.GetInt("Logging", "FlushThreshold", 100);
                int rateLimitPerSecond = _config.GetInt("Logging", "RateLimitPerSecond", 20);

                _optimizedLogger = new OptimizedLogger(
                    _logDirectory,
                    "RLF_Optimized",
                    _debugMode ? LogLevel.Debug : LogLevel.Info,
                    bufferCapacity,
                    flushIntervalMs,
                    flushThreshold,
                    rateLimitPerSecond
                );

                _logger?.Info("[OptimizedLogger] Inicializado");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error("Falha ao inicializar OptimizedLogger", ex);
                return true; // Não crítico
            }
        }

        #endregion

        #region Configuration

        private bool InitializeConfiguration()
        {
            try
            {
                _config = new IniReader(_configPath, _logger);

                if (!File.Exists(_configPath))
                {
                    CreateDefaultConfiguration();
                    _config.Save();
                }

                return _config.Load();
            }
            catch (Exception ex)
            {
                _logger?.Error("Falha ao inicializar configuração", ex);
                return false;
            }
        }

        private void CreateDefaultConfiguration()
        {
            // Core
            _config.SetBool("Core", "DebugMode", false);
            _config.SetInt("Core", "LogRetentionDays", 7);

            // Events
            _config.SetInt("Events", "MaxHandlersPerEvent", 100);
            _config.SetFloat("TimeSystem", "MaxDeltaSeconds", 0.25f);

            // Debug
            _config.SetBool("Debug", "Enabled", true);
            _config.SetString("Debug", "MinLevel", "Info");

            // Logging Otimizado
            _config.SetBool("Logging", "UseOptimizedLogger", true);
            _config.SetInt("Logging", "BufferCapacity", 500);
            _config.SetInt("Logging", "FlushIntervalMs", 5000);
            _config.SetInt("Logging", "FlushThreshold", 100);
            _config.SetInt("Logging", "RateLimitPerSecond", 20);

            // I/O
            _config.SetInt("IO", "MaxQueueSize", 100);

            // Character Creator
            _config.SetInt("CharacterCreator", "MaxSlots", 5);
            _config.SetBool("CharacterCreator", "AutoSave", true);
            _config.SetBool("CharacterCreator", "AllowMultipleCharacters", true);

            // Entities
            _config.SetFloat("Entities", "CleanupIntervalSeconds", 10f);
            _config.SetInt("Entities", "TickInterval", 60);

            // 🚀 Performance
            _config.SetBool("Performance", "ProfilerEnabled", true);
            _config.SetInt("Performance", "ProfilerReportIntervalTicks", 3600);
            _config.SetFloat("Performance", "TickWarningThresholdMs", 8.0f);
            _config.SetFloat("Performance", "TickCriticalThresholdMs", 16.0f);
            _config.SetBool("Performance", "SchedulerEnabled", true);
            _config.SetFloat("Performance", "TickBudgetMs", 12.0f);
            _config.SetInt("Performance", "DefaultTaskInterval", 1);
            _config.SetBool("Performance", "CacheEnabled", true);
            _config.SetInt("Performance", "PlayerSnapshotTTLFrames", 1);
            _config.SetInt("Performance", "WorldCacheDefaultTTLMs", 500);

            // Watchdog
            _config.SetBool("Watchdog", "Enabled", true);
            _config.SetFloat("Watchdog", "CheckIntervalSeconds", 5f);
            _config.SetFloat("Watchdog", "WarningThresholdMs", 8f);
            _config.SetFloat("Watchdog", "CriticalThresholdMs", 16f);
            _config.SetFloat("Watchdog", "ThrottleMultiplier", 2f);
            _config.SetInt("Watchdog", "MaxThrottleLevel", 4);
            _config.SetFloat("Watchdog", "RecoveryThresholdMs", 4f);
            _config.SetInt("Watchdog", "TickInterval", 30);
        }

        private void ApplyConfiguration()
        {
            try
            {
                _debugMode = _config.GetBool("Core", "DebugMode", _debugMode);
            }
            catch { }
        }

        #endregion

        #region Debug

        private void InitializeDebug()
        {
            try
            {
                RLFDebug.Initialize(new DebugConfig
                {
                    Enabled = true,
                    MinLevel = DebugLevel.Info
                });

                RLFDebug.Info(DebugChannel.Core, "Debug inicializado");
            }
            catch { }
        }

        #endregion

        #region Performance

        private bool InitializePerformance()
        {
            try
            {
                _performanceConfig = PerformanceConfig.LoadFromIni(_config);

                // Garante que as configs estão salvas
                _performanceConfig.SaveDefaults(_config);
                _config.Save();

                // Cria Profiler
                _tickProfiler = new TickProfiler(
                    _logger,
                    _performanceConfig.ProfilerReportIntervalTicks,
                    _performanceConfig.TickWarningThresholdMs,
                    _performanceConfig.TickCriticalThresholdMs
                );
                _tickProfiler.SetEnabled(_performanceConfig.ProfilerEnabled);

                // Cria Scheduler
                _taskScheduler = new TaskScheduler(_logger, _performanceConfig.TickBudgetMs);
                _taskScheduler.SetEnabled(_performanceConfig.SchedulerEnabled);

                _logger?.Info($"[Performance] Profiler={_performanceConfig.ProfilerEnabled}, Scheduler={_performanceConfig.SchedulerEnabled}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error("Falha ao inicializar Performance", ex);
                return false;
            }
        }

        private bool InitializeEntityRegistry()
        {
            try
            {
                float cleanupInterval = _config.GetFloat("Entities", "CleanupIntervalSeconds", 10f);
                int tickInterval = _config.GetInt("Entities", "TickInterval", 60);

                _entityRegistry = new EntityRegistry(_logger, cleanupInterval, tickInterval);

                // Registra no scheduler (que agora já existe)
                if (_taskScheduler != null)
                {
                    _taskScheduler.Register(_entityRegistry);
                }

                _logger?.Info("[EntityRegistry] Inicializado");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error("Falha ao inicializar EntityRegistry", ex);
                return false;
            }
        }

        private bool InitializeWatchdog()
        {
            try
            {
                _watchdogConfig = WatchdogConfig.LoadFromIni(_config);
                _watchdogConfig.SaveDefaults(_config);

                _watchdog = new Watchdog.Watchdog(
                    _logger,
                    _eventManager,
                    _watchdogConfig,
                    _tickProfiler,
                    _taskScheduler,
                    _systemRegistry
                );

                // Registra no scheduler
                if (_taskScheduler != null)
                {
                    _taskScheduler.Register(_watchdog);
                }

                _logger?.Info($"[Watchdog] Inicializado (Enabled={_watchdogConfig.Enabled})");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error("Falha ao inicializar Watchdog", ex);
                return false;
            }
        }

        private bool InitializeFileQueue()
        {
            try
            {
                int maxQueueSize = _config.GetInt("IO", "MaxQueueSize", 100);
                _fileQueue = new AsyncFileQueue(maxQueueSize);

                _logger?.Info("[AsyncFileQueue] Inicializado");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error("Falha ao inicializar AsyncFileQueue", ex);
                return true; // Não crítico
            }
        }

        #endregion

        #region EventManager

        private bool InitializeEventManager()
        {
            try
            {
                int maxHandlers = _config.GetInt("Events", "MaxHandlersPerEvent", 100);
                _eventManager = new EventManager();
                return _eventManager.Initialize(_logger, maxHandlers);
            }
            catch (Exception ex)
            {
                _logger?.Error("Falha ao inicializar EventManager", ex);
                return false;
            }
        }

        #endregion

        #region Systems

        private bool InitializeSystems()
        {
            try
            {
                _systemRegistry = new SystemRegistry(_logger);

                // 🚀 Conecta o scheduler ao registry
                if (_taskScheduler != null && _performanceConfig.SchedulerEnabled)
                {
                    _systemRegistry.SetScheduler(_taskScheduler);
                }

                _systemRegistry.Register(new CoreLifecycleSystem(_logger, _eventManager));
                _systemRegistry.Register(new TimeSystem(_logger, _eventManager));

                _systemRegistry.Register(
                    new RLF.Core.Needs.NeedsSystem(
                        _logger,
                        _eventManager,
                        RLF.Core.Needs.NeedsSettingsLoader.Load()
                    )
                );

                _systemRegistry.Register(
                    new RealTimeWeatherSystem(_logger, _eventManager)
                );

                _systemRegistry.Register(
                    new DocumentSystem(_logger, _eventManager)
                );

                _systemRegistry.Register(
                    new LawSystem(_logger, _eventManager)
                );

                _systemRegistry.Register(
                    new PoliceSystem(_logger, _eventManager)
                );

                // Inicializar Economy
                _economySystem = new EconomySystem(
                    initialBalance: 0m,
                    walletSettings: new WalletSettings(),
                    expenseSettings: new ExpenseSettings(),
                    debtSettings: new DebtSettings()
                );

                // JobSystem
                _systemRegistry.Register(
                    new RLF.Core.Jobs.Core.JobSystem(
                        _logger,
                        _eventManager,
                        _economySystem
                    )
                );

                // Identity Debug Listener
                _identityDebugListener = new IdentityDebugListener(_eventManager);
                _identityDebugListener.StartListening();

                _systemRegistry.StartAll();
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error("Falha ao inicializar sistemas", ex);
                return false;
            }
        }

        #endregion

        #region Character Creator

        private bool InitializeCharacterCreator()
        {
            try
            {
                int maxSlots = _config.GetInt("CharacterCreator", "MaxSlots", 5);
                string charactersPath = Path.Combine(_dataPath, "Characters");

                _characterStore = new CharacterStore(charactersPath, maxSlots);

                _logger?.Info($"CharacterCreator inicializado (MaxSlots={maxSlots}, Path={charactersPath})");
                RLFDebug.Info(DebugChannel.Core, "CharacterCreator inicializado");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error("Falha ao inicializar CharacterCreator", ex);
                return false;
            }
        }

        public bool NeedsCharacterCreation()
        {
            return _characterStore == null || !_characterStore.HasAnyCharacter();
        }

        public CharacterCreator.Data.CharacterData GetLastCharacter()
        {
            return _characterStore?.GetMostRecentCharacter();
        }

        public CharacterCreator.Data.CharacterData LoadCharacter(int slot)
        {
            return _characterStore?.LoadCharacter(slot);
        }

        public bool SaveCharacter(CharacterCreator.Data.CharacterData character, int slot)
        {
            return _characterStore?.SaveCharacter(character, slot) ?? false;
        }

        #endregion

        #region Tick Bridge

        /// <summary>
        /// Chamado pelo RLF.GTA a cada frame.
        /// Usa Scheduler quando habilitado, senão fallback para TickAll.
        /// </summary>
        public void Tick()
        {
            if (_state != CoreState.Running)
                return;

            // 📊 Inicia medição
            _tickProfiler?.BeginTick();

            // 🚀 Usa scheduler se habilitado
            if (_taskScheduler != null && _taskScheduler.IsEnabled)
            {
                _taskScheduler.Tick(_tickProfiler);
            }

            // Fallback: sistemas não gerenciados pelo scheduler
            _systemRegistry?.TickAll();

            // 📊 Finaliza medição
            _tickProfiler?.EndTick();
        }

        #endregion

        #region Events API

        public bool RaiseEvent(string name, RLFEventArgs args)
            => _eventManager?.Raise(name, args) ?? false;

        public bool Subscribe(string name, EventHandler<RLFEventArgs> handler)
            => _eventManager?.Subscribe(name, handler) ?? false;

        public bool Unsubscribe(string name, EventHandler<RLFEventArgs> handler)
            => _eventManager?.Unsubscribe(name, handler) ?? false;

        #endregion

        #region Scheduler API

        /// <summary>
        /// Registra uma tarefa customizada no scheduler.
        /// </summary>
        public bool RegisterTask(string name, Action action, TaskPriority priority = TaskPriority.Normal, int interval = 1)
        {
            return _taskScheduler?.Register(name, action, priority, interval) ?? false;
        }

        /// <summary>
        /// Remove uma tarefa do scheduler.
        /// </summary>
        public bool UnregisterTask(string name)
        {
            return _taskScheduler?.Unregister(name) ?? false;
        }

        /// <summary>
        /// Obtém relatório de performance.
        /// </summary>
        public string GetPerformanceReport()
        {
            return _tickProfiler?.GenerateReport() ?? "Profiler não disponível";
        }

        #endregion

        #region Pooling Stats

        /// <summary>
        /// Obtém estatísticas dos pools de memória.
        /// </summary>
        public string GetPoolingStats()
        {
            return StringBuilderPool.Build(sb =>
            {
                sb.AppendLine("=== Pooling Stats ===");
                sb.AppendLine(StringBuilderPool.GetStats());
                sb.AppendLine(ListPool<int>.GetStats());
                sb.AppendLine("====================");
            });
        }

        #endregion

        #region State

        private void SetState(CoreState newState)
        {
            lock (_stateLock)
            {
                _state = newState;
            }
        }

        #endregion

        #region Shutdown

        public bool Shutdown()
        {
            lock (_stateLock)
            {
                if (_state == CoreState.ShuttingDown || _state == CoreState.Stopped)
                    return false;

                _state = CoreState.ShuttingDown;
            }

            _logger?.Info("Iniciando shutdown do Core...");

            // 📊 Log performance final
            if (_tickProfiler != null)
            {
                _logger?.Info(_tickProfiler.GenerateReport());
            }

            // 🔎 Para listeners
            _identityDebugListener?.StopListening();

            // 🚀 Para scheduler e limpa tarefas
            _taskScheduler?.Clear();

            // 🛡️ Para sistemas
            _systemRegistry?.StopAll();
            _systemRegistry?.Clear();

            // 📣 Limpa eventos
            _eventManager?.ClearAll();

            // 📁 Aguarda I/O pendente e fecha
            _fileQueue?.WaitForCompletion(2000);
            _fileQueue?.Dispose();

            // 📝 Flush e fecha logs
            _optimizedLogger?.Dispose();

            // 🧑 Limpa character store
            _characterStore = null;

            SetState(CoreState.Stopped);
            _logger?.Info("Core desligado com sucesso");
            return true;
        }

        #endregion
    }
}