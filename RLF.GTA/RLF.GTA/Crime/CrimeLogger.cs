using System;
using System.IO;

namespace RLF.GTA.Crime
{
    /// <summary>
    /// Sistema de log para debug do Crime System.
    /// Escreve logs em arquivo para diagnóstico.
    /// </summary>
    public static class CrimeLogger
    {
        private static string _logPath;
        private static bool _isInitialized = false;
        private static object _lockObject = new object();

        public static void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                string scriptsFolder = Path.Combine(Environment.CurrentDirectory, "scripts");
                _logPath = Path.Combine(scriptsFolder, "CrimeSystem.log");

                // Cria arquivo novo ao inicializar
                lock (_lockObject)
                {
                    File.WriteAllText(_logPath, 
                        $"=== CRIME SYSTEM LOG ===\n" +
                        $"Started: {DateTime.Now}\n" +
                        $"========================\n\n");
                }

                _isInitialized = true;
                Log("Logger initialized successfully");
            }
            catch (Exception ex)
            {
                // Se falhar, desabilita log
                _isInitialized = false;
            }
        }

        public static void Log(string message)
        {
            if (!_isInitialized) return;

            try
            {
                lock (_lockObject)
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    string logLine = $"[{timestamp}] {message}\n";
                    File.AppendAllText(_logPath, logLine);
                }
            }
            catch
            {
                // Ignora erros de escrita
            }
        }

        public static void LogError(string message, Exception ex = null)
        {
            string errorMsg = $"ERROR: {message}";
            if (ex != null)
            {
                errorMsg += $"\nException: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }
            Log(errorMsg);
        }

        public static void LogCrime(RLF.Core.Crime.CrimeRecord crime)
        {
            if (crime == null) return;

            Log($"CRIME COMMITTED: {crime.Type} | " +
                $"Severity: {crime.Severity} | " +
                $"Location: {crime.LocationName} | " +
                $"Heat: +{(crime.GetHeatContribution() * 100f):F1}% | " +
                $"Witnessed: {crime.WasWitnessed()}");
        }

        public static void LogHeatChange(float newHeat, RLF.Core.Crime.HeatState newState)
        {
            Log($"HEAT CHANGED: {newState} ({(newHeat * 100f):F1}%)");
        }
    }
}
