using System;
using System.Collections.Generic;
using RLF.Core.Jobs.Enums;
using RLF.Core.Logging;

namespace RLF.Core.Jobs.Core
{
    public sealed class JobRegistry
    {
        private readonly Logger _logger;
        private readonly Dictionary<JobType, JobBase> _jobs;

        public JobRegistry(Logger logger)
        {
            _logger = logger;
            _jobs = new Dictionary<JobType, JobBase>();
        }

        public void Register(JobBase job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            if (_jobs.ContainsKey(job.Type))
            {
                _logger.Warning($"Trabalho {job.Type} já registrado - sobrescrevendo");
                _jobs[job.Type] = job;
                return;
            }

            _jobs.Add(job.Type, job);
            _logger.Info($"Trabalho registrado: {job.Type}");
        }

        public JobBase Get(JobType type)
        {
            return _jobs.TryGetValue(type, out var job) ? job : null;
        }

        public bool Exists(JobType type)
        {
            return _jobs.ContainsKey(type);
        }

        public IReadOnlyCollection<JobBase> GetAll()
        {
            return _jobs.Values;
        }
    }
}