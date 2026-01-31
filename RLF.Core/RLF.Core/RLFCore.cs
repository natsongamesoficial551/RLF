using RLF.Core.CharacterCreator.Storage;
using RLF.Core.Configuration;
using RLF.Core.Debug;
using RLF.Core.Economy;
using RLF.Core.Economy.Debt;
using RLF.Core.Economy.Expenses;
using RLF.Core.Economy.Wallet;
using RLF.Core.Events;
using RLF.Core.Events.EventArgs;
using RLF.Core.Identity;
using RLF.Core.Identity.Debug;
using RLF.Core.Law;
using RLF.Core.Law.Police;
using RLF.Core.Logging;
using RLF.Core.Safety;  // 🛡️ NOVO: Safety System
using RLF.Core.Systems;
using RLF.Core.Utilities;
using RLF.Core.Vehicles;
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
    /// Inicialização segura, eventos desacoplados e shutdown limpo.
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

        // 🔎 Identity Debug
        private IdentityDebugListener _identityDebugListener;

        // 💰 Economy
        private EconomySystem _economySystem;

        // 🧑 Character Creator
        private CharacterStore _characterStore;

        // 🛡️ Safety System
        private bool _safetySystemAvailable;

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

        // 💰 Economia
        public EconomySystem Economy => _economySystem;

        // 🧑 Character Store
        public CharacterStore CharacterStore => _characterStore;

        // 📁 Caminho de dados
        public string DataPath => _dataPath;

        // 🛡️ Safety Manager (inicialização real é feita pelo RLF.GTA)
        public SafeExecutionManager SafetyManager => SafeExecutionManager.Instance;

        // 🛡️ Indica se o Safety System está disponível
        public bool IsSafetySystemAvailable => _safetySystemAvailable;

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

                SafeExecutor.Logger = _logger;

                // 5️⃣ Sistemas
                if (!InitializeSystems())
                    return FailInit("Systems");

                // 6️⃣ Character Creator
                if (!InitializeCharacterCreator())
                    return FailInit("CharacterCreator");

                // 7️⃣ Safety System (marca como disponível - inicialização real é no RLF.GTA)
                InitializeSafetySystem();

                _initializationTime = DateTime.Now;

                SetState(CoreState.Running);
                RaiseEvent("core:initialized", new RLFEventArgs());

                _logger.Info($"RLF Core v{_version} inicializado com sucesso");
                _logger.Info($"[Safety] Sistema de segurança disponível: {_safetySystemAvailable}");

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

            // Character Creator
            _config.SetInt("CharacterCreator", "MaxSlots", 5);
            _config.SetBool("CharacterCreator", "AutoSave", true);
            _config.SetBool("CharacterCreator", "AllowMultipleCharacters", true);

            // 🛡️ Safety System
            _config.SetBool("Safety", "Enabled", true);
            _config.SetInt("Safety", "SurvivalModeMinutes", 30);
            _config.SetInt("Safety", "HealthCheckIntervalFrames", 150);
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

        #region Safety System

        /// <summary>
        /// Marca o Safety System como disponível.
        /// A inicialização REAL é feita pelo RLF.GTA (SafetyBridgeScript).
        /// O Core apenas expõe disponibilidade e acesso ao manager.
        /// </summary>
        private void InitializeSafetySystem()
        {
            try
            {
                bool enabled = _config.GetBool("Safety", "Enabled", true);

                if (!enabled)
                {
                    _safetySystemAvailable = false;
                    _logger?.Info("[Safety] Sistema de segurança desabilitado via config");
                    return;
                }

                // Apenas marca como disponível
                // A inicialização real acontece no RLF.GTA via SafetyBridgeScript
                _safetySystemAvailable = true;
                _logger?.Info("[Safety] Sistema de segurança disponível para inicialização via RLF.GTA");
                RLFDebug.Info(DebugChannel.Core, "[Safety] Disponível para inicialização");
            }
            catch (Exception ex)
            {
                _logger?.Error("[Safety] Erro ao preparar sistema de segurança", ex);
                _safetySystemAvailable = false;
            }
        }

        /// <summary>
        /// Verifica se o Safety System está em modo de proteção.
        /// Útil para outros sistemas ajustarem comportamento.
        /// </summary>
        public bool IsSafetyProtectionActive()
        {
            if (!_safetySystemAvailable)
                return false;

            try
            {
                return SafetyManager?.IsInProtectionMode() ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica se o Safety System está em modo survival.
        /// Útil para outros sistemas reduzirem carga drasticamente.
        /// </summary>
        public bool IsSafetySurvivalActive()
        {
            if (!_safetySystemAvailable)
                return false;

            try
            {
                return SafetyManager?.IsInSurvivalMode() ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtém o nível de performance atual do Safety System.
        /// </summary>
        public PerformanceLevel GetSafetyPerformanceLevel()
        {
            if (!_safetySystemAvailable)
                return PerformanceLevel.Normal;

            try
            {
                return SafetyManager?.GetPerformanceLevel() ?? PerformanceLevel.Normal;
            }
            catch
            {
                return PerformanceLevel.Normal;
            }
        }

        /// <summary>
        /// Registra um sistema no Safety Manager para controle de tick adaptativo.
        /// Wrapper conveniente para não precisar acessar SafetyManager diretamente.
        /// </summary>
        public void RegisterSystemForSafety(
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
            if (!_safetySystemAvailable)
            {
                _logger?.Warning($"[Safety] Tentativa de registrar sistema '{systemId}' mas Safety não está disponível");
                return;
            }

            try
            {
                SafetyManager?.RegisterSystem(
                    systemId,
                    displayName,
                    category,
                    priority,
                    tickCallback,
                    normalTickRateMs,
                    reducedTickRateMs,
                    minimalTickRateMs,
                    canRunCallback
                );
            }
            catch (Exception ex)
            {
                _logger?.Error($"[Safety] Erro ao registrar sistema '{systemId}'", ex);
            }
        }

        /// <summary>
        /// Registra um sistema batch no Safety Manager.
        /// </summary>
        public void RegisterBatchSystemForSafety(
            string systemId,
            string displayName,
            SystemCategory category,
            Action tickCallback,
            int batchIntervalMs = 1000)
        {
            if (!_safetySystemAvailable)
                return;

            try
            {
                SafetyManager?.RegisterBatchSystem(
                    systemId,
                    displayName,
                    category,
                    tickCallback,
                    batchIntervalMs
                );
            }
            catch (Exception ex)
            {
                _logger?.Error($"[Safety] Erro ao registrar batch system '{systemId}'", ex);
            }
        }

        #endregion

        #region Tick Bridge

        /// <summary>
        /// Chamado pelo RLF.GTA a cada frame.
        /// </summary>
        public void Tick()
        {
            if (_state != CoreState.Running)
                return;

            _systemRegistry?.TickAll();
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

            // 🔎 Para listeners
            _identityDebugListener?.StopListening();

            // 🛡️ Para sistemas
            _systemRegistry?.StopAll();
            _systemRegistry?.Clear();

            // 📣 Limpa eventos
            _eventManager?.ClearAll();

            // 🧑 Limpa character store
            _characterStore = null;

            // 🛡️ Safety System: NÃO fazemos shutdown aqui!
            // O shutdown do Safety é responsabilidade do RLF.GTA (SafetyBridgeScript)
            // Isso evita shutdown duplo e garante ordem correta de cleanup
            _safetySystemAvailable = false;

            SetState(CoreState.Stopped);
            _logger?.Info("Core desligado com sucesso");
            return true;
        }

        #endregion
    }
}