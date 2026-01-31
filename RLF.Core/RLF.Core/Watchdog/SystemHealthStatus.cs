using System;

namespace RLF.Core.Watchdog
{
    /// <summary>
    /// Estado de saúde de um sistema.
    /// </summary>
    public enum HealthState
    {
        /// <summary>
        /// Sistema funcionando normalmente.
        /// </summary>
        Healthy,

        /// <summary>
        /// Sistema com performance degradada (throttled).
        /// </summary>
        Throttled,

        /// <summary>
        /// Sistema desativado por problemas críticos.
        /// </summary>
        Disabled,

        /// <summary>
        /// Sistema em processo de recuperação.
        /// </summary>
        Recovering
    }

    /// <summary>
    /// Status detalhado de saúde de um sistema monitorado.
    /// </summary>
    public sealed class SystemHealthStatus
    {
        /// <summary>
        /// Nome do sistema.
        /// </summary>
        public string SystemName { get; }

        /// <summary>
        /// Estado atual de saúde.
        /// </summary>
        public HealthState State { get; private set; }

        /// <summary>
        /// Contagem de violações de tempo.
        /// </summary>
        public int ViolationCount { get; private set; }

        /// <summary>
        /// Contagem de violações críticas.
        /// </summary>
        public int CriticalViolationCount { get; private set; }

        /// <summary>
        /// TickRate original do sistema.
        /// </summary>
        public int OriginalTickRate { get; }

        /// <summary>
        /// TickRate atual (pode estar throttled).
        /// </summary>
        public int CurrentTickRate { get; private set; }

        /// <summary>
        /// Última vez que houve violação.
        /// </summary>
        public DateTime LastViolationTime { get; private set; }

        /// <summary>
        /// Última vez que foi desativado.
        /// </summary>
        public DateTime? DisabledTime { get; private set; }

        /// <summary>
        /// Tempo médio de execução (ms).
        /// </summary>
        public double AverageExecutionMs { get; private set; }

        /// <summary>
        /// Tempo máximo de execução registrado (ms).
        /// </summary>
        public double MaxExecutionMs { get; private set; }

        /// <summary>
        /// Total de execuções monitoradas.
        /// </summary>
        public long TotalExecutions { get; private set; }

        // Para cálculo de média
        private double _executionSum;

        // 🆕 NOVO: Spike tolerance - permite alguns picos isolados sem penalizar
        private int _consecutiveViolations;
        private const int MAX_TOLERATED_SPIKES = 2; // Permite 2 spikes isolados

        public SystemHealthStatus(string systemName, int originalTickRate)
        {
            SystemName = systemName;
            OriginalTickRate = Math.Max(1, originalTickRate);
            CurrentTickRate = OriginalTickRate;
            State = HealthState.Healthy;
            ViolationCount = 0;
            CriticalViolationCount = 0;
            LastViolationTime = DateTime.MinValue;
            DisabledTime = null;
            AverageExecutionMs = 0;
            MaxExecutionMs = 0;
            TotalExecutions = 0;
            _executionSum = 0;
            _consecutiveViolations = 0;
        }

        /// <summary>
        /// Registra uma execução do sistema.
        /// </summary>
        public void RecordExecution(double executionMs)
        {
            TotalExecutions++;
            _executionSum += executionMs;
            AverageExecutionMs = _executionSum / TotalExecutions;

            if (executionMs > MaxExecutionMs)
                MaxExecutionMs = executionMs;

            // 🆕 NOVO: Reset de violações consecutivas se execução normal
            // Threshold flexível: considera "normal" se <= 50% acima da média
            double normalThreshold = AverageExecutionMs * 1.5;

            if (executionMs <= normalThreshold)
            {
                _consecutiveViolations = 0;
            }
        }

        /// <summary>
        /// Registra uma violação de tempo.
        /// 🆕 MELHORADO: Agora com spike tolerance
        /// </summary>
        public void RecordViolation(bool isCritical)
        {
            ViolationCount++;
            LastViolationTime = DateTime.Now;

            if (isCritical)
            {
                _consecutiveViolations++;

                // 🆕 SPIKE TOLERANCE: Só conta como violação crítica real
                // se for consecutiva ou frequente
                if (_consecutiveViolations > MAX_TOLERATED_SPIKES)
                {
                    CriticalViolationCount++;
                }
                // Spikes isolados são logados mas não penalizam tanto
            }
            else
            {
                // Violações não-críticas resetam contador de consecutivas
                // (permite que sistema se recupere naturalmente)
                _consecutiveViolations = Math.Max(0, _consecutiveViolations - 1);
            }
        }

        /// <summary>
        /// Reseta contadores de violação.
        /// </summary>
        public void ResetViolations()
        {
            ViolationCount = 0;
            CriticalViolationCount = 0;
            _consecutiveViolations = 0;
        }

        /// <summary>
        /// Aplica throttling ao sistema.
        /// </summary>
        public void ApplyThrottle(int factor)
        {
            CurrentTickRate = OriginalTickRate * Math.Max(1, factor);
            State = HealthState.Throttled;
        }

        /// <summary>
        /// Desativa o sistema.
        /// </summary>
        public void Disable()
        {
            State = HealthState.Disabled;
            DisabledTime = DateTime.Now;
        }

        /// <summary>
        /// Inicia processo de recuperação.
        /// </summary>
        public void StartRecovery()
        {
            State = HealthState.Recovering;
            CurrentTickRate = OriginalTickRate;
            ResetViolations();
        }

        /// <summary>
        /// Restaura estado saudável.
        /// </summary>
        public void RestoreHealthy()
        {
            State = HealthState.Healthy;
            CurrentTickRate = OriginalTickRate;
            DisabledTime = null;
        }

        /// <summary>
        /// 🆕 NOVO: Calcula taxa de spikes (violações / total execuções)
        /// Útil para identificar sistemas com problemas pontuais vs crônicos
        /// </summary>
        public double GetSpikeRate()
        {
            if (TotalExecutions == 0)
                return 0;

            return (double)CriticalViolationCount / TotalExecutions;
        }

        /// <summary>
        /// 🆕 NOVO: Verifica se sistema tem spikes ocasionais (< 2%) ou crônicos
        /// </summary>
        public bool HasOccasionalSpikesOnly()
        {
            return GetSpikeRate() < 0.02; // < 2% = spikes ocasionais aceitáveis
        }

        public override string ToString()
        {
            string health = HasOccasionalSpikesOnly() ? "OK (spikes ocasionais)" : "PROBLEM";

            return $"[{SystemName}] State={State}, TickRate={CurrentTickRate}, " +
                   $"Violations={ViolationCount}, Critical={CriticalViolationCount}, " +
                   $"Consecutive={_consecutiveViolations}, SpikeRate={GetSpikeRate():P2}, " +
                   $"Avg={AverageExecutionMs:F2}ms, Max={MaxExecutionMs:F2}ms, Health={health}";
        }
    }
}