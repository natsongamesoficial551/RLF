using RLF.Core.Configuration;
using System.Linq;

namespace RLF.Core.Watchdog
{
    /// <summary>
    /// Configurações do sistema Watchdog.
    /// </summary>
    public sealed class WatchdogConfig
    {
        /// <summary>
        /// Se o watchdog está ativo.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Tempo máximo de tick antes de warning (ms).
        /// </summary>
        public double WarningThresholdMs { get; set; } = 8.0;

        /// <summary>
        /// Tempo máximo de tick antes de ação corretiva (ms).
        /// </summary>
        public double CriticalThresholdMs { get; set; } = 16.0;

        /// <summary>
        /// Tempo máximo de tick antes de desativar sistema (ms).
        /// </summary>
        public double DisableThresholdMs { get; set; } = 50.0;

        /// <summary>
        /// Quantidade de violações antes de aplicar throttling.
        /// </summary>
        public int ViolationsBeforeThrottle { get; set; } = 3;

        /// <summary>
        /// Quantidade de violações críticas antes de desativar.
        /// </summary>
        public int ViolationsBeforeDisable { get; set; } = 10;

        /// <summary>
        /// Fator de throttle aplicado (multiplica o TickRate).
        /// </summary>
        public int ThrottleFactor { get; set; } = 2;

        /// <summary>
        /// Tempo em segundos para resetar contador de violações.
        /// </summary>
        public float ViolationResetSeconds { get; set; } = 60f;

        /// <summary>
        /// Se deve tentar recuperar sistemas desativados automaticamente.
        /// </summary>
        public bool AutoRecovery { get; set; } = true;

        /// <summary>
        /// Tempo em segundos para tentar recuperar um sistema.
        /// </summary>
        public float RecoveryDelaySeconds { get; set; } = 30f;

        /// <summary>
        /// 🆕 NOVO: Sistemas que NUNCA devem ser monitorados pelo Watchdog.
        /// Sistemas críticos como Input, UI, Logger não devem ser desabilitados.
        /// </summary>
        public string[] ExemptSystems { get; set; } = new[]
        {
            "InputSystem",
            "UIRenderer",
            "Logger",
            "OptimizedLogger",
            "CrashHandler",
            "SafetySystem",
            "CoreLifecycle"
        };

        /// <summary>
        /// Carrega configurações do INI.
        /// </summary>
        public static WatchdogConfig LoadFromIni(IniReader ini)
        {
            var config = new WatchdogConfig();

            if (ini == null)
                return config;

            config.Enabled = ini.GetBool("Watchdog", "Enabled", true);
            config.WarningThresholdMs = ini.GetFloat("Watchdog", "WarningThresholdMs", 8.0f);
            config.CriticalThresholdMs = ini.GetFloat("Watchdog", "CriticalThresholdMs", 16.0f);
            config.DisableThresholdMs = ini.GetFloat("Watchdog", "DisableThresholdMs", 50.0f);
            config.ViolationsBeforeThrottle = ini.GetInt("Watchdog", "ViolationsBeforeThrottle", 3);
            config.ViolationsBeforeDisable = ini.GetInt("Watchdog", "ViolationsBeforeDisable", 10);
            config.ThrottleFactor = ini.GetInt("Watchdog", "ThrottleFactor", 2);
            config.ViolationResetSeconds = ini.GetFloat("Watchdog", "ViolationResetSeconds", 60f);
            config.AutoRecovery = ini.GetBool("Watchdog", "AutoRecovery", true);
            config.RecoveryDelaySeconds = ini.GetFloat("Watchdog", "RecoveryDelaySeconds", 30f);

            // 🆕 Carrega lista de sistemas isentos (separados por vírgula)
            string exemptList = ini.GetString("Watchdog", "ExemptSystems",
                "InputSystem,UIRenderer,Logger,OptimizedLogger,CrashHandler,SafetySystem,CoreLifecycle");

            if (!string.IsNullOrWhiteSpace(exemptList))
            {
                config.ExemptSystems = exemptList.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToArray();
            }

            return config;
        }

        /// <summary>
        /// Salva configurações padrão no INI.
        /// </summary>
        public void SaveDefaults(IniReader ini)
        {
            if (ini == null)
                return;

            ini.SetBool("Watchdog", "Enabled", Enabled);
            ini.SetFloat("Watchdog", "WarningThresholdMs", (float)WarningThresholdMs);
            ini.SetFloat("Watchdog", "CriticalThresholdMs", (float)CriticalThresholdMs);
            ini.SetFloat("Watchdog", "DisableThresholdMs", (float)DisableThresholdMs);
            ini.SetInt("Watchdog", "ViolationsBeforeThrottle", ViolationsBeforeThrottle);
            ini.SetInt("Watchdog", "ViolationsBeforeDisable", ViolationsBeforeDisable);
            ini.SetInt("Watchdog", "ThrottleFactor", ThrottleFactor);
            ini.SetFloat("Watchdog", "ViolationResetSeconds", ViolationResetSeconds);
            ini.SetBool("Watchdog", "AutoRecovery", AutoRecovery);
            ini.SetFloat("Watchdog", "RecoveryDelaySeconds", RecoveryDelaySeconds);

            // 🆕 Salva lista de sistemas isentos
            if (ExemptSystems != null && ExemptSystems.Length > 0)
            {
                ini.SetString("Watchdog", "ExemptSystems", string.Join(",", ExemptSystems));
            }
        }
    }
}