using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using RLF.Core;
using RLF.Core.Logging;
using RLF.Core.Events;
using RLF.Core.Events.EventArgs;

namespace RLF.GTA
{
    /// <summary>
    /// Gerenciador de shutdown do framework.
    /// Garante que todos os recursos sejam liberados corretamente.
    /// </summary>
    public class ShutdownHandler
    {
        #region Campos Privados

        private readonly RLFCore _core;
        private readonly Logger _logger;
        private readonly EventManager _eventManager;

        // Callbacks de shutdown
        private readonly List<Action> _shutdownCallbacks;

        // Controle de estado
        private bool _isShuttingDown;
        private bool _shutdownCompleted;
        private DateTime _shutdownStartTime;

        #endregion

        #region Construtor

        /// <summary>
        /// Inicializa o ShutdownHandler.
        /// </summary>
        public ShutdownHandler(RLFCore core, Logger logger)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _eventManager = _core.EventManager;
            if (_eventManager == null)
            {
                throw new InvalidOperationException("EventManager não disponível no Core");
            }

            _shutdownCallbacks = new List<Action>();

            _logger.Info("ShutdownHandler: Inicializado");
            RegisterShutdownListeners();
        }

        #endregion

        #region Registro de Callbacks

        private void RegisterShutdownListeners()
        {
            try
            {
                _eventManager.Subscribe("shutdown:register_callback", OnRegisterCallback);
                _logger.Debug("ShutdownHandler: Listeners registrados");
            }
            catch (Exception ex)
            {
                _logger.Error($"ShutdownHandler: Erro ao registrar listeners: {ex.Message}");
            }
        }

        private void OnRegisterCallback(object sender, RLFEventArgs e)
        {
            try
            {
                _logger.Debug("ShutdownHandler: Evento de registro recebido");
            }
            catch (Exception ex)
            {
                _logger.Error($"ShutdownHandler: Erro ao registrar callback: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra um callback para ser executado durante o shutdown.
        /// </summary>
        public void RegisterCallback(Action callback)
        {
            if (callback == null)
            {
                _logger.Warning("ShutdownHandler: Tentativa de registrar callback null");
                return;
            }

            if (_isShuttingDown)
            {
                _logger.Warning("ShutdownHandler: Tentativa de registrar callback durante shutdown");
                return;
            }

            _shutdownCallbacks.Add(callback);
            _logger.Debug($"ShutdownHandler: Callback registrado (Total: {_shutdownCallbacks.Count})");
        }

        #endregion

        #region Execução de Shutdown

        /// <summary>
        /// Executa o shutdown completo do framework.
        /// </summary>
        public void ExecuteShutdown()
        {
            if (_isShuttingDown)
            {
                _logger.Warning("ShutdownHandler: Shutdown já em andamento");
                return;
            }

            if (_shutdownCompleted)
            {
                _logger.Warning("ShutdownHandler: Shutdown já foi completado");
                return;
            }

            try
            {
                _isShuttingDown = true;
                _shutdownStartTime = DateTime.Now;

                _logger.Info("=== ShutdownHandler: Iniciando processo de shutdown ===");

                NotifyShutdownStart();
                ExecuteCallbacks();
                CleanupGTAComponents();
                ShutdownCore();
                FinalizeShutdown();

                _shutdownCompleted = true;

                var shutdownDuration = (DateTime.Now - _shutdownStartTime).TotalMilliseconds;
                _logger.Info($"=== ShutdownHandler: Shutdown completo em {shutdownDuration:F2}ms ===");
            }
            catch (Exception ex)
            {
                _logger.Error($"ShutdownHandler: Erro crítico durante shutdown: {ex.Message}");
                _logger.Error($"Stack Trace: {ex.StackTrace}");
                _shutdownCompleted = true;
            }
        }

        /// <summary>
        /// Fase 1: Notifica o início do shutdown via eventos.
        /// ✅ CORRIGIDO: Usar new RLFEventArgs() ao invés de null
        /// </summary>
        private void NotifyShutdownStart()
        {
            try
            {
                _logger.Info("ShutdownHandler: Fase 1 - Notificando início do shutdown");
                _eventManager.Raise("shutdown:starting", new RLFEventArgs());
            }
            catch (Exception ex)
            {
                _logger.Error($"ShutdownHandler: Erro ao notificar início: {ex.Message}");
            }
        }

        /// <summary>
        /// Fase 2: Executa todos os callbacks registrados.
        /// </summary>
        private void ExecuteCallbacks()
        {
            try
            {
                _logger.Info($"ShutdownHandler: Fase 2 - Executando {_shutdownCallbacks.Count} callbacks");

                int successCount = 0;
                int errorCount = 0;

                foreach (var callback in _shutdownCallbacks)
                {
                    try
                    {
                        callback?.Invoke();
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.Error($"ShutdownHandler: Erro ao executar callback: {ex.Message}");
                    }
                }

                _logger.Info($"ShutdownHandler: Callbacks executados - Sucesso: {successCount}, Erros: {errorCount}");
            }
            catch (Exception ex)
            {
                _logger.Error($"ShutdownHandler: Erro ao executar callbacks: {ex.Message}");
            }
        }

        /// <summary>
        /// Fase 3: Limpa componentes específicos do GTA.
        /// ✅ CORRIGIDO: Usar new RLFEventArgs() ao invés de null
        /// </summary>
        private void CleanupGTAComponents()
        {
            try
            {
                _logger.Info("ShutdownHandler: Fase 3 - Limpando componentes GTA");
                _eventManager.Raise("shutdown:gta_cleanup", new RLFEventArgs());
            }
            catch (Exception ex)
            {
                _logger.Error($"ShutdownHandler: Erro ao limpar componentes GTA: {ex.Message}");
            }
        }

        /// <summary>
        /// Fase 4: Executa o shutdown do Core.
        /// </summary>
        private void ShutdownCore()
        {
            try
            {
                _logger.Info("ShutdownHandler: Fase 4 - Shutdown do Core");
                _core?.Shutdown();
                _logger.Info("ShutdownHandler: Core shutdown concluído");
            }
            catch (Exception ex)
            {
                _logger.Error($"ShutdownHandler: Erro ao fazer shutdown do Core: {ex.Message}");
            }
        }

        /// <summary>
        /// Fase 5: Finalização do shutdown.
        /// ✅ CORRIGIDO: Usar new RLFEventArgs() ao invés de null
        /// </summary>
        private void FinalizeShutdown()
        {
            try
            {
                _logger.Info("ShutdownHandler: Fase 5 - Finalizando");

                _shutdownCallbacks.Clear();
                _eventManager.Raise("shutdown:completed", new RLFEventArgs());
                _eventManager.Unsubscribe("shutdown:register_callback", OnRegisterCallback);

                _logger.Info("ShutdownHandler: Finalização concluída");
            }
            catch (Exception ex)
            {
                _logger.Error($"ShutdownHandler: Erro ao finalizar: {ex.Message}");
            }
        }

        #endregion

        #region Utilitários

        /// <summary>
        /// Verifica se o shutdown está em andamento.
        /// </summary>
        public bool IsShuttingDown => _isShuttingDown;

        /// <summary>
        /// Verifica se o shutdown foi completado.
        /// </summary>
        public bool IsShutdownCompleted => _shutdownCompleted;

        #endregion
    }
}