using RLF.Core.Economy;
using RLF.Core.Events;
using RLF.Core.Jobs.Enums;
using RLF.Core.Logging;
using RLF.Core.Safety;
using RLF.Core.Systems;
using System;

namespace RLF.Core.Jobs.Core
{
    public sealed class JobSystem : SystemBase
    {
        #region Constants

        private const string SAFETY_SYSTEM_ID = "RLF.Core.JobSystem";
        private const string SAFETY_DISPLAY_NAME = "Job System";
        private const int NORMAL_TICK_MS = 500;      // 2x por segundo normal
        private const int REDUCED_TICK_MS = 1500;    // 1x a cada 1.5s reduzido
        private const int MINIMAL_TICK_MS = 5000;    // 1x a cada 5s mínimo

        #endregion

        #region Fields

        private readonly EconomySystem _economy;
        private readonly JobRegistry _registry;
        private bool _registeredInSafety;

        #endregion

        #region Properties

        public JobRegistry Registry => _registry;

        #endregion

        #region Constructor

        public JobSystem(
            Logger logger,
            EventManager eventManager,
            EconomySystem economy)
            : base("JobSystem", logger, eventManager)
        {
            _economy = economy;
            _registry = new JobRegistry(logger);
        }

        #endregion

        #region Lifecycle

        protected override void OnStart()
        {
            Logger.Info($"{Name}: Sistema de empregos iniciado");

            // 🛡️ Registra no Safety System para tick adaptativo
            RegisterInSafetySystem();
        }

        protected override void OnStop()
        {
            Logger.Info($"{Name}: Sistema de empregos encerrado");

            // 🛡️ Remove do Safety System
            UnregisterFromSafetySystem();
        }

        protected override void OnTick()
        {
            // 🛡️ Se registrado no Safety, o tick é controlado por lá
            // Este método pode ficar vazio ou ter lógica que DEVE rodar todo frame

            // Se NÃO conseguiu registrar no Safety, faz fallback manual
            if (!_registeredInSafety)
            {
                OnSafetyTick();
            }
        }

        #endregion

        #region Safety System Integration

        /// <summary>
        /// Registra o sistema no Safety System para tick adaptativo.
        /// </summary>
        private void RegisterInSafetySystem()
        {
            try
            {
                // Verifica se o Safety está disponível
                if (!RLFCore.Instance.IsSafetySystemAvailable)
                {
                    Logger.Warning($"{Name}: Safety System não disponível, usando tick normal");
                    _registeredInSafety = false;
                    return;
                }

                // Registra com prioridade Normal e categoria Jobs
                RLFCore.Instance.RegisterSystemForSafety(
                    systemId: SAFETY_SYSTEM_ID,
                    displayName: SAFETY_DISPLAY_NAME,
                    category: SystemCategory.Jobs,
                    priority: TickPriority.Normal,
                    tickCallback: OnSafetyTick,
                    normalTickRateMs: NORMAL_TICK_MS,
                    reducedTickRateMs: REDUCED_TICK_MS,
                    minimalTickRateMs: MINIMAL_TICK_MS,
                    canRunCallback: CanJobSystemRun
                );

                _registeredInSafety = true;
                Logger.Info($"{Name}: Registrado no Safety System (Normal: {NORMAL_TICK_MS}ms, Reduced: {REDUCED_TICK_MS}ms, Minimal: {MINIMAL_TICK_MS}ms)");
            }
            catch (Exception ex)
            {
                Logger.Error($"{Name}: Erro ao registrar no Safety System", ex);
                _registeredInSafety = false;
            }
        }

        /// <summary>
        /// Remove o registro do Safety System.
        /// </summary>
        private void UnregisterFromSafetySystem()
        {
            if (!_registeredInSafety)
                return;

            try
            {
                RLFCore.Instance.SafetyManager?.UnregisterSystem(SAFETY_SYSTEM_ID);
                _registeredInSafety = false;
                Logger.Info($"{Name}: Removido do Safety System");
            }
            catch (Exception ex)
            {
                Logger.Error($"{Name}: Erro ao remover do Safety System", ex);
            }
        }

        /// <summary>
        /// Callback que determina se o JobSystem pode rodar.
        /// Retorna false para pular o tick em certas condições.
        /// </summary>
        private bool CanJobSystemRun()
        {
            // Pode adicionar lógica customizada aqui
            // Por exemplo: não rodar se não tem jobs ativos
            // Por enquanto, sempre pode rodar
            return true;
        }

        /// <summary>
        /// Tick controlado pelo Safety System.
        /// Chamado com frequência adaptativa baseada no contexto do jogo.
        /// </summary>
        private void OnSafetyTick()
        {
            // Lógica principal do JobSystem que pode rodar com frequência reduzida
            UpdateActiveJobs();
        }

        #endregion

        #region Job Logic

        /// <summary>
        /// Atualiza todos os jobs ativos.
        /// </summary>
        private void UpdateActiveJobs()
        {
            // Aqui vai a lógica de update dos jobs
            // Por exemplo: verificar tempos, atualizar estados, etc.

            // TODO: Implementar lógica de update se necessário
            // _registry.UpdateAll();
        }

        public bool TryStartShift(JobType type)
        {
            var job = _registry.Get(type);
            if (job == null)
            {
                Logger.Warning($"Trabalho {type} não encontrado");
                return false;
            }

            return job.TryStartShift(DateTime.Now);
        }

        public void CompleteTask(JobType type)
        {
            var job = _registry.Get(type);
            if (job == null)
                return;

            job.CompleteTask();
        }

        public string GetJobStatus(JobType type)
        {
            var job = _registry.Get(type);
            return job?.GetStatusMessage() ?? "Trabalho não encontrado";
        }

        #endregion
    }
}