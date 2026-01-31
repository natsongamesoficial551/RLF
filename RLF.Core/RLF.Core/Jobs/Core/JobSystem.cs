using System;
using RLF.Core.Economy;
using RLF.Core.Events;
using RLF.Core.Jobs.Enums;
using RLF.Core.Logging;
using RLF.Core.Systems;

namespace RLF.Core.Jobs.Core
{
    public sealed class JobSystem : SystemBase
    {
        private readonly EconomySystem _economy;
        private readonly JobRegistry _registry;

        public JobRegistry Registry => _registry;

        public JobSystem(
            Logger logger,
            EventManager eventManager,
            EconomySystem economy)
            : base("JobSystem", logger, eventManager)
        {
            _economy = economy;
            _registry = new JobRegistry(logger);
        }

        protected override void OnStart()
        {
            Logger.Info($"{Name}: Sistema de empregos iniciado");
        }

        protected override void OnStop()
        {
            Logger.Info($"{Name}: Sistema de empregos encerrado");
        }

        protected override void OnTick()
        {
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
    }
}