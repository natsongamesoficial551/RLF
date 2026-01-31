using System;
using System.Collections.Generic;
using System.Linq;
using RLF.Core.Events;
using RLF.Core.Events.EventArgs;
using RLF.Core.Logging;
using RLF.Core.Performance;
using RLF.Core.Scheduling;
using RLF.Core.Systems;

namespace RLF.Core.Watchdog
{
    /// <summary>
    /// Monitor de saúde de sistemas.
    /// Detecta problemas de performance e aplica ações corretivas.
    /// </summary>
    public sealed class Watchdog : ISchedulable
    {
        #region Fields

        private readonly Dictionary<string, SystemHealthStatus> _systemHealth;
        private readonly List<string> _disabledSystems;
        private readonly List<string> _throttledSystems;
        private readonly object _lock;

        private readonly Logger _logger;
        private readonly EventManager _events;
        private readonly WatchdogConfig _config;
        private readonly TickProfiler _profiler;
        private readonly TaskScheduler _scheduler;
        private readonly SystemRegistry _systemRegistry;

        // 🆕 AJUSTE CRÍTICO: Sistemas que não devem ser monitorados
        private readonly HashSet<string> _exemptSystems;

        private DateTime _lastAnalysis;
        private int _totalWarnings;
        private int _totalThrottles;
        private int _totalDisables;
        private int _totalRecoveries;

        #endregion

        #region Properties

        public bool IsEnabled { get; private set; }
        public int DisabledSystemCount => _disabledSystems.Count;
        public int ThrottledSystemCount => _throttledSystems.Count;

        #endregion

        #region ISchedulable

        public string ScheduleName => "Watchdog";
        public TaskPriority Priority => TaskPriority.High;
        public int TickInterval => 30; // Analisa a cada 30 ticks (~0.5s a 60fps)
        public bool IsActive => IsEnabled;

        public void ExecuteScheduled()
        {
            AnalyzeSystems();
        }

        #endregion

        #region Constructor

        public Watchdog(
            Logger logger,
            EventManager events,
            WatchdogConfig config,
            TickProfiler profiler,
            TaskScheduler scheduler,
            SystemRegistry systemRegistry)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _events = events;
            _config = config ?? new WatchdogConfig();
            _profiler = profiler;
            _scheduler = scheduler;
            _systemRegistry = systemRegistry;

            _systemHealth = new Dictionary<string, SystemHealthStatus>(StringComparer.Ordinal);
            _disabledSystems = new List<string>();
            _throttledSystems = new List<string>();
            _lock = new object();

            // 🆕 AJUSTE CRÍTICO: Inicializa whitelist de sistemas críticos
            _exemptSystems = new HashSet<string>(
                _config.ExemptSystems ?? new string[0],
                StringComparer.OrdinalIgnoreCase
            );

            _lastAnalysis = DateTime.Now;
            IsEnabled = _config.Enabled;

            _logger.Info($"[Watchdog] Inicializado (Enabled={IsEnabled}, Exempt={_exemptSystems.Count})");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Ativa/desativa o watchdog.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            _logger.Info($"[Watchdog] {(enabled ? "Ativado" : "Desativado")}");
        }

        /// <summary>
        /// Registra um sistema para monitoramento.
        /// </summary>
        public void Monitor(string systemName, int tickRate)
        {
            if (string.IsNullOrEmpty(systemName))
                return;

            // 🆕 AJUSTE CRÍTICO: Verifica se sistema está na whitelist
            if (_exemptSystems.Contains(systemName))
            {
                _logger.Warning($"[Watchdog] Sistema '{systemName}' está na whitelist de isenção - NÃO será monitorado");
                return;
            }

            lock (_lock)
            {
                if (!_systemHealth.ContainsKey(systemName))
                {
                    _systemHealth[systemName] = new SystemHealthStatus(systemName, tickRate);
                    _logger.Debug($"[Watchdog] Monitorando: {systemName}");
                }
            }
        }

        /// <summary>
        /// Para de monitorar um sistema.
        /// </summary>
        public void Unmonitor(string systemName)
        {
            lock (_lock)
            {
                _systemHealth.Remove(systemName);
                _disabledSystems.Remove(systemName);
                _throttledSystems.Remove(systemName);
            }
        }

        /// <summary>
        /// Reporta execução de um sistema.
        /// Chamado pelo TaskScheduler após cada execução.
        /// </summary>
        public void ReportExecution(string systemName, double executionMs)
        {
            if (!IsEnabled || string.IsNullOrEmpty(systemName))
                return;

            // 🆕 AJUSTE CRÍTICO: Ignora sistemas isentos
            if (_exemptSystems.Contains(systemName))
                return;

            lock (_lock)
            {
                if (!_systemHealth.TryGetValue(systemName, out var status))
                {
                    status = new SystemHealthStatus(systemName, 1);
                    _systemHealth[systemName] = status;
                }

                status.RecordExecution(executionMs);

                // Verifica violações
                if (executionMs >= _config.DisableThresholdMs)
                {
                    status.RecordViolation(true);
                    _totalWarnings++;
                    _logger.Warning($"[Watchdog] CRÍTICO: {systemName} levou {executionMs:F2}ms (>{_config.DisableThresholdMs}ms)");
                }
                else if (executionMs >= _config.CriticalThresholdMs)
                {
                    status.RecordViolation(true);
                    _totalWarnings++;
                    _logger.Warning($"[Watchdog] Lento: {systemName} levou {executionMs:F2}ms (>{_config.CriticalThresholdMs}ms)");
                }
                else if (executionMs >= _config.WarningThresholdMs)
                {
                    status.RecordViolation(false);
                    _logger.Debug($"[Watchdog] Warning: {systemName} levou {executionMs:F2}ms (>{_config.WarningThresholdMs}ms)");
                }
            }
        }

        /// <summary>
        /// Obtém status de saúde de um sistema.
        /// </summary>
        public SystemHealthStatus GetStatus(string systemName)
        {
            lock (_lock)
            {
                _systemHealth.TryGetValue(systemName, out var status);
                return status;
            }
        }

        /// <summary>
        /// Força recuperação de um sistema desativado.
        /// </summary>
        public bool ForceRecovery(string systemName)
        {
            lock (_lock)
            {
                if (!_systemHealth.TryGetValue(systemName, out var status))
                    return false;

                if (status.State != HealthState.Disabled)
                    return false;

                return TryRecoverSystem(systemName, status);
            }
        }

        /// <summary>
        /// 🆕 NOVO: Adiciona sistema à whitelist de isenção
        /// </summary>
        public void AddExemption(string systemName)
        {
            if (string.IsNullOrEmpty(systemName))
                return;

            _exemptSystems.Add(systemName);
            _logger.Info($"[Watchdog] Sistema '{systemName}' adicionado à whitelist de isenção");

            // Remove do monitoramento se estava sendo monitorado
            Unmonitor(systemName);
        }

        /// <summary>
        /// 🆕 NOVO: Remove sistema da whitelist de isenção
        /// </summary>
        public void RemoveExemption(string systemName)
        {
            if (string.IsNullOrEmpty(systemName))
                return;

            _exemptSystems.Remove(systemName);
            _logger.Info($"[Watchdog] Sistema '{systemName}' removido da whitelist de isenção");
        }

        /// <summary>
        /// 🆕 NOVO: Verifica se sistema está isento
        /// </summary>
        public bool IsExempt(string systemName)
        {
            return _exemptSystems.Contains(systemName);
        }

        #endregion

        #region Analysis

        /// <summary>
        /// Analisa todos os sistemas e aplica ações corretivas.
        /// </summary>
        private void AnalyzeSystems()
        {
            if (!IsEnabled)
                return;

            DateTime now = DateTime.Now;

            lock (_lock)
            {
                foreach (var kvp in _systemHealth)
                {
                    var systemName = kvp.Key;
                    var status = kvp.Value;

                    // Reseta violações se passou tempo suficiente
                    if ((now - status.LastViolationTime).TotalSeconds >= _config.ViolationResetSeconds)
                    {
                        if (status.ViolationCount > 0)
                        {
                            status.ResetViolations();

                            // Se estava throttled e não tem mais violações, restaura
                            if (status.State == HealthState.Throttled)
                            {
                                RestoreSystem(systemName, status);
                            }
                        }
                    }

                    // Analisa baseado no estado atual
                    switch (status.State)
                    {
                        case HealthState.Healthy:
                            AnalyzeHealthySystem(systemName, status);
                            break;

                        case HealthState.Throttled:
                            AnalyzeThrottledSystem(systemName, status);
                            break;

                        case HealthState.Disabled:
                            AnalyzeDisabledSystem(systemName, status, now);
                            break;

                        case HealthState.Recovering:
                            AnalyzeRecoveringSystem(systemName, status);
                            break;
                    }
                }
            }

            _lastAnalysis = now;
        }

        private void AnalyzeHealthySystem(string name, SystemHealthStatus status)
        {
            // Verifica se precisa throttle
            if (status.ViolationCount >= _config.ViolationsBeforeThrottle)
            {
                ApplyThrottle(name, status);
            }
        }

        private void AnalyzeThrottledSystem(string name, SystemHealthStatus status)
        {
            // Verifica se precisa desativar
            if (status.CriticalViolationCount >= _config.ViolationsBeforeDisable)
            {
                DisableSystem(name, status);
            }
        }

        private void AnalyzeDisabledSystem(string name, SystemHealthStatus status, DateTime now)
        {
            // Verifica se pode tentar recuperar
            if (_config.AutoRecovery && status.DisabledTime.HasValue)
            {
                var disabledFor = (now - status.DisabledTime.Value).TotalSeconds;
                if (disabledFor >= _config.RecoveryDelaySeconds)
                {
                    TryRecoverSystem(name, status);
                }
            }
        }

        private void AnalyzeRecoveringSystem(string name, SystemHealthStatus status)
        {
            // Se continua tendo violações durante recuperação, desativa novamente
            if (status.CriticalViolationCount >= 2)
            {
                DisableSystem(name, status);
                _logger.Warning($"[Watchdog] Recuperação falhou: {name}");
            }
            // Se passou tempo sem violações, restaura saudável
            else if (status.ViolationCount == 0)
            {
                status.RestoreHealthy();
                _logger.Info($"[Watchdog] Recuperado com sucesso: {name}");
                _totalRecoveries++;

                RaiseEvent("watchdog:system_recovered", name);
            }
        }

        #endregion

        #region Actions

        private void ApplyThrottle(string name, SystemHealthStatus status)
        {
            status.ApplyThrottle(_config.ThrottleFactor);

            if (!_throttledSystems.Contains(name))
                _throttledSystems.Add(name);

            _totalThrottles++;

            // Atualiza TickRate no scheduler
            UpdateSystemTickRate(name, status.CurrentTickRate);

            _logger.Warning($"[Watchdog] Throttle aplicado: {name} (TickRate: {status.OriginalTickRate} -> {status.CurrentTickRate})");

            RaiseEvent("watchdog:system_throttled", name);
        }

        private void DisableSystem(string name, SystemHealthStatus status)
        {
            status.Disable();

            if (!_disabledSystems.Contains(name))
                _disabledSystems.Add(name);

            _throttledSystems.Remove(name);
            _totalDisables++;

            // Desativa no scheduler
            _scheduler?.SetTaskEnabled(name, false);

            // Pausa o sistema
            var system = _systemRegistry?.Get(name);
            system?.Pause();

            _logger.Error($"[Watchdog] Sistema DESATIVADO: {name}");

            RaiseEvent("watchdog:system_disabled", name);
        }

        private bool TryRecoverSystem(string name, SystemHealthStatus status)
        {
            status.StartRecovery();

            _disabledSystems.Remove(name);

            // Reativa no scheduler
            _scheduler?.SetTaskEnabled(name, true);

            // Resume o sistema
            var system = _systemRegistry?.Get(name);
            system?.Resume();

            _logger.Info($"[Watchdog] Tentando recuperar: {name}");

            RaiseEvent("watchdog:system_recovering", name);
            return true;
        }

        private void RestoreSystem(string name, SystemHealthStatus status)
        {
            status.RestoreHealthy();

            _throttledSystems.Remove(name);

            // Restaura TickRate original
            UpdateSystemTickRate(name, status.OriginalTickRate);

            _logger.Info($"[Watchdog] Sistema restaurado: {name}");

            RaiseEvent("watchdog:system_restored", name);
        }

        private void UpdateSystemTickRate(string name, int newTickRate)
        {
            // Não há como mudar TickRate de uma tarefa existente no scheduler atual
            // Isso seria uma melhoria futura
            // Por agora, o throttle funciona através do pause/resume
        }

        private void RaiseEvent(string eventName, string systemName)
        {
            _events?.Raise(eventName, new RLFEventArgs<string>(systemName));
        }

        #endregion

        #region Stats

        /// <summary>
        /// Retorna estatísticas do watchdog.
        /// </summary>
        public string GetStats()
        {
            lock (_lock)
            {
                return $"[Watchdog] Systems={_systemHealth.Count} | " +
                       $"Exempt={_exemptSystems.Count} | " +
                       $"Throttled={_throttledSystems.Count} | " +
                       $"Disabled={_disabledSystems.Count} | " +
                       $"Warnings={_totalWarnings} | " +
                       $"Throttles={_totalThrottles} | " +
                       $"Disables={_totalDisables} | " +
                       $"Recoveries={_totalRecoveries}";
            }
        }

        /// <summary>
        /// Retorna relatório detalhado de todos os sistemas.
        /// </summary>
        public string GetDetailedReport()
        {
            var lines = new List<string>
            {
                "=== Watchdog Report ===",
                GetStats(),
                ""
            };

            lock (_lock)
            {
                if (_exemptSystems.Count > 0)
                {
                    lines.Add(">> Sistemas Isentos:");
                    foreach (var system in _exemptSystems)
                    {
                        lines.Add($"   - {system}");
                    }
                    lines.Add("");
                }

                foreach (var kvp in _systemHealth)
                {
                    lines.Add(kvp.Value.ToString());
                }
            }

            lines.Add("=======================");
            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Lista sistemas desativados.
        /// </summary>
        public IReadOnlyList<string> GetDisabledSystems()
        {
            lock (_lock)
            {
                return _disabledSystems.ToArray();
            }
        }

        /// <summary>
        /// Lista sistemas throttled.
        /// </summary>
        public IReadOnlyList<string> GetThrottledSystems()
        {
            lock (_lock)
            {
                return _throttledSystems.ToArray();
            }
        }

        /// <summary>
        /// 🆕 NOVO: Lista sistemas isentos
        /// </summary>
        public IReadOnlyList<string> GetExemptSystems()
        {
            return _exemptSystems.ToArray();
        }

        #endregion
    }
}