namespace RLF.Core.Performance
{
    /// <summary>
    /// Configurações de performance carregadas do INI.
    /// </summary>
    public sealed class PerformanceConfig
    {
        // Profiler
        public bool ProfilerEnabled { get; set; } = true;
        public int ProfilerReportIntervalTicks { get; set; } = 3600;
        public double TickWarningThresholdMs { get; set; } = 8.0;
        public double TickCriticalThresholdMs { get; set; } = 16.0;

        // Scheduler
        public bool SchedulerEnabled { get; set; } = true;
        public double TickBudgetMs { get; set; } = 12.0;
        public int DefaultTaskInterval { get; set; } = 1;

        // Cache (Fase 2)
        public bool CacheEnabled { get; set; } = true;
        public int PlayerSnapshotTTLFrames { get; set; } = 1;
        public int WorldCacheDefaultTTLMs { get; set; } = 500;

        /// <summary>
        /// Carrega configurações de um IniReader.
        /// </summary>
        public static PerformanceConfig LoadFromIni(Configuration.IniReader ini)
        {
            var config = new PerformanceConfig();

            if (ini == null)
                return config;

            // [Performance]
            config.ProfilerEnabled = ini.GetBool("Performance", "ProfilerEnabled", true);
            config.ProfilerReportIntervalTicks = ini.GetInt("Performance", "ProfilerReportIntervalTicks", 3600);
            config.TickWarningThresholdMs = ini.GetFloat("Performance", "TickWarningThresholdMs", 8.0f);
            config.TickCriticalThresholdMs = ini.GetFloat("Performance", "TickCriticalThresholdMs", 16.0f);

            config.SchedulerEnabled = ini.GetBool("Performance", "SchedulerEnabled", true);
            config.TickBudgetMs = ini.GetFloat("Performance", "TickBudgetMs", 12.0f);
            config.DefaultTaskInterval = ini.GetInt("Performance", "DefaultTaskInterval", 1);

            config.CacheEnabled = ini.GetBool("Performance", "CacheEnabled", true);
            config.PlayerSnapshotTTLFrames = ini.GetInt("Performance", "PlayerSnapshotTTLFrames", 1);
            config.WorldCacheDefaultTTLMs = ini.GetInt("Performance", "WorldCacheDefaultTTLMs", 500);

            return config;
        }

        /// <summary>
        /// Salva configurações padrão no INI.
        /// </summary>
        public void SaveDefaults(Configuration.IniReader ini)
        {
            if (ini == null)
                return;

            ini.SetBool("Performance", "ProfilerEnabled", ProfilerEnabled);
            ini.SetInt("Performance", "ProfilerReportIntervalTicks", ProfilerReportIntervalTicks);
            ini.SetFloat("Performance", "TickWarningThresholdMs", (float)TickWarningThresholdMs);
            ini.SetFloat("Performance", "TickCriticalThresholdMs", (float)TickCriticalThresholdMs);

            ini.SetBool("Performance", "SchedulerEnabled", SchedulerEnabled);
            ini.SetFloat("Performance", "TickBudgetMs", (float)TickBudgetMs);
            ini.SetInt("Performance", "DefaultTaskInterval", DefaultTaskInterval);

            ini.SetBool("Performance", "CacheEnabled", CacheEnabled);
            ini.SetInt("Performance", "PlayerSnapshotTTLFrames", PlayerSnapshotTTLFrames);
            ini.SetInt("Performance", "WorldCacheDefaultTTLMs", WorldCacheDefaultTTLMs);
        }
    }
}