using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using RLF.Core.Logging;

namespace RLF.Core.Performance
{
    /// <summary>
    /// Profiler de tick com breakdown por sistema.
    /// GTA-SAFE: Mínimas alocações, reusa objetos.
    /// </summary>
    public sealed class TickProfiler
    {
        private readonly Dictionary<string, PerformanceMetrics> _systemMetrics;
        private readonly PerformanceMetrics _totalTickMetrics;
        private readonly Stopwatch _tickStopwatch;
        private readonly Stopwatch _systemStopwatch;
        private readonly StringBuilder _reportBuilder;

        private readonly Logger _logger;
        private readonly int _reportIntervalTicks;

        private int _tickCount;
        private bool _isEnabled;
        private string _currentSystem;

        // Configurações
        private readonly double _warningThresholdMs;
        private readonly double _criticalThresholdMs;

        public bool IsEnabled => _isEnabled;
        public int TickCount => _tickCount;
        public PerformanceMetrics TotalMetrics => _totalTickMetrics;

        /// <summary>
        /// Cria um profiler de tick.
        /// </summary>
        public TickProfiler(
            Logger logger,
            int reportIntervalTicks = 3600,  // ~1 minuto a 60fps
            double warningThresholdMs = 8.0,
            double criticalThresholdMs = 16.0,
            int sampleCapacity = 200)
        {
            _logger = logger;
            _reportIntervalTicks = Math.Max(60, reportIntervalTicks);
            _warningThresholdMs = warningThresholdMs;
            _criticalThresholdMs = criticalThresholdMs;

            _systemMetrics = new Dictionary<string, PerformanceMetrics>(StringComparer.Ordinal);
            _totalTickMetrics = new PerformanceMetrics("TotalTick", sampleCapacity);

            _tickStopwatch = new Stopwatch();
            _systemStopwatch = new Stopwatch();
            _reportBuilder = new StringBuilder(512);

            _isEnabled = true;
            _tickCount = 0;
        }

        /// <summary>
        /// Ativa/desativa o profiler.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;

            if (!enabled)
            {
                _tickStopwatch.Reset();
                _systemStopwatch.Reset();
            }
        }

        /// <summary>
        /// Inicia a medição de um tick completo.
        /// </summary>
        public void BeginTick()
        {
            if (!_isEnabled)
                return;

            _tickStopwatch.Restart();
            _tickCount++;
        }

        /// <summary>
        /// Finaliza a medição do tick e registra métricas.
        /// </summary>
        public void EndTick()
        {
            if (!_isEnabled)
                return;

            _tickStopwatch.Stop();
            double totalMs = _tickStopwatch.Elapsed.TotalMilliseconds;
            _totalTickMetrics.Record(totalMs);

            // Log de warning se tick muito longo
            if (totalMs >= _criticalThresholdMs)
            {
                _logger?.Warning($"[Profiler] Tick CRÍTICO: {totalMs:F2}ms (>{_criticalThresholdMs}ms)");
            }
            else if (totalMs >= _warningThresholdMs)
            {
                _logger?.Debug($"[Profiler] Tick lento: {totalMs:F2}ms (>{_warningThresholdMs}ms)");
            }

            // Relatório periódico
            if (_tickCount % _reportIntervalTicks == 0)
            {
                LogPeriodicReport();
            }
        }

        /// <summary>
        /// Inicia medição de um sistema específico.
        /// </summary>
        public void BeginSystem(string systemName)
        {
            if (!_isEnabled || string.IsNullOrEmpty(systemName))
                return;

            _currentSystem = systemName;
            _systemStopwatch.Restart();
        }

        /// <summary>
        /// Finaliza medição do sistema atual.
        /// </summary>
        public void EndSystem()
        {
            if (!_isEnabled || _currentSystem == null)
                return;

            _systemStopwatch.Stop();

            if (!_systemMetrics.TryGetValue(_currentSystem, out var metrics))
            {
                metrics = new PerformanceMetrics(_currentSystem);
                _systemMetrics[_currentSystem] = metrics;
            }

            metrics.Record(_systemStopwatch);
            _currentSystem = null;
        }

        /// <summary>
        /// Obtém métricas de um sistema específico.
        /// </summary>
        public PerformanceMetrics GetSystemMetrics(string systemName)
        {
            if (_systemMetrics.TryGetValue(systemName, out var metrics))
                return metrics;
            return null;
        }

        /// <summary>
        /// Retorna todos os nomes de sistemas rastreados.
        /// </summary>
        public IEnumerable<string> GetTrackedSystems()
        {
            return _systemMetrics.Keys;
        }

        /// <summary>
        /// Gera relatório completo de performance.
        /// </summary>
        public string GenerateReport()
        {
            _reportBuilder.Clear();
            _reportBuilder.AppendLine("=== RLF Performance Report ===");
            _reportBuilder.AppendLine($"Ticks analisados: {_tickCount}");
            _reportBuilder.AppendLine();

            _reportBuilder.AppendLine(">> Tick Total:");
            _reportBuilder.AppendLine($"   {_totalTickMetrics.GetSummary()}");
            _reportBuilder.AppendLine();

            if (_systemMetrics.Count > 0)
            {
                _reportBuilder.AppendLine(">> Por Sistema:");

                foreach (var kvp in _systemMetrics)
                {
                    _reportBuilder.AppendLine($"   {kvp.Value.GetSummary()}");
                }
            }

            _reportBuilder.AppendLine("==============================");
            return _reportBuilder.ToString();
        }

        /// <summary>
        /// Reseta todas as métricas.
        /// </summary>
        public void Reset()
        {
            _tickCount = 0;
            _totalTickMetrics.Reset();

            foreach (var metrics in _systemMetrics.Values)
            {
                metrics.Reset();
            }

            _logger?.Info("[Profiler] Métricas resetadas");
        }

        private void LogPeriodicReport()
        {
            if (_logger == null)
                return;

            _logger.Info($"[Profiler] Tick #{_tickCount} | " +
                        $"Avg={_totalTickMetrics.Average:F2}ms | " +
                        $"P95={_totalTickMetrics.P95:F2}ms | " +
                        $"P99={_totalTickMetrics.P99:F2}ms");
        }
    }
}