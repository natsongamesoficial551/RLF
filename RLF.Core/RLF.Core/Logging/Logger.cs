using System;
using System.IO;
using System.Text;

namespace RLF.Core.Logging
{
    /// <summary>
    /// Níveis de severidade dos logs.
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,    // Informações detalhadas para debug
        Info = 1,     // Informações gerais
        Warning = 2,  // Avisos que não impedem execução
        Error = 3,    // Erros que podem afetar funcionalidade
        Critical = 4  // Erros críticos que podem parar o sistema
    }

    /// <summary>
    /// Sistema de logging thread-safe do RLF.
    /// Grava logs em arquivo com rotação automática e suporte a níveis de severidade.
    /// </summary>
    public sealed class Logger
    {
        // Configurações
        private readonly string _logDirectory;
        private readonly string _logFileName;
        private readonly LogLevel _minLogLevel;
        private readonly bool _debugMode;
        private readonly long _maxFileSizeBytes;

        // Estado interno
        private readonly object _lock = new object();
        private bool _isInitialized;
        private string _currentLogPath;

        /// <summary>
        /// Indica se o logger está inicializado.
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                lock (_lock)
                {
                    return _isInitialized;
                }
            }
        }

        /// <summary>
        /// Modo debug ativo.
        /// </summary>
        public bool DebugMode
        {
            get { return _debugMode; }
        }

        /// <summary>
        /// Caminho completo do arquivo de log atual.
        /// </summary>
        public string CurrentLogPath
        {
            get
            {
                lock (_lock)
                {
                    return _currentLogPath;
                }
            }
        }

        /// <summary>
        /// Construtor. Não realiza operações pesadas.
        /// </summary>
        /// <param name="logDirectory">Diretório onde os logs serão salvos</param>
        /// <param name="logFileName">Nome base do arquivo de log (sem extensão)</param>
        /// <param name="minLogLevel">Nível mínimo de log a ser gravado</param>
        /// <param name="debugMode">Ativa logs de debug</param>
        /// <param name="maxFileSizeMB">Tamanho máximo do arquivo em MB (padrão: 10MB)</param>
        public Logger(
            string logDirectory = "Logs",
            string logFileName = "RLF",
            LogLevel minLogLevel = LogLevel.Info,
            bool debugMode = false,
            int maxFileSizeMB = 10)
        {
            _logDirectory = logDirectory ?? "Logs";
            _logFileName = logFileName ?? "RLF";
            _minLogLevel = debugMode ? LogLevel.Debug : minLogLevel;
            _debugMode = debugMode;
            _maxFileSizeBytes = maxFileSizeMB * 1024L * 1024L; // Converte MB para bytes
            _isInitialized = false;
            _currentLogPath = string.Empty;
        }

        /// <summary>
        /// Inicializa o logger criando diretórios e arquivo inicial.
        /// </summary>
        /// <returns>True se inicializado com sucesso</returns>
        public bool Initialize()
        {
            lock (_lock)
            {
                if (_isInitialized)
                    return true;

                try
                {
                    // Cria o diretório de logs se não existir
                    if (!Directory.Exists(_logDirectory))
                    {
                        Directory.CreateDirectory(_logDirectory);
                    }

                    // Define o caminho do arquivo de log
                    _currentLogPath = GenerateLogFilePath();

                    // Cria o arquivo inicial com header
                    WriteLogHeader();

                    _isInitialized = true;

                    // Log de inicialização
                    Info("Logger initialized successfully");
                    Info($"Log file: {_currentLogPath}");
                    Info($"Debug mode: {_debugMode}");

                    return true;
                }
                catch (Exception ex)
                {
                    // Falha crítica, tenta gravar no console
                    System.Diagnostics.Debug.WriteLine($"[Logger] CRITICAL: Failed to initialize: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Gera o caminho completo do arquivo de log com timestamp.
        /// </summary>
        /// <returns>Caminho completo do arquivo</returns>
        private string GenerateLogFilePath()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"{_logFileName}_{timestamp}.log";
            return Path.Combine(_logDirectory, fileName);
        }

        /// <summary>
        /// Escreve o header inicial do arquivo de log.
        /// </summary>
        private void WriteLogHeader()
        {
            try
            {
                StringBuilder header = new StringBuilder();
                header.AppendLine("================================================================================");
                header.AppendLine($"  Real Life Framework - Log File");
                header.AppendLine($"  Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                header.AppendLine($"  Debug Mode: {_debugMode}");
                header.AppendLine($"  Min Log Level: {_minLogLevel}");
                header.AppendLine("================================================================================");
                header.AppendLine();

                File.WriteAllText(_currentLogPath, header.ToString(), Encoding.UTF8);
            }
            catch
            {
                // Silenciosamente falha - será tentado novamente no primeiro log
            }
        }

        /// <summary>
        /// Escreve uma entrada de log no arquivo.
        /// Thread-safe e com proteção contra falhas.
        /// </summary>
        /// <param name="level">Nível de severidade</param>
        /// <param name="message">Mensagem a ser logada</param>
        /// <param name="exception">Exceção opcional</param>
        private void WriteLog(LogLevel level, string message, Exception exception = null)
        {
            // Verifica se deve logar baseado no nível mínimo
            if (level < _minLogLevel)
                return;

            if (string.IsNullOrWhiteSpace(message))
                return;

            lock (_lock)
            {
                if (!_isInitialized)
                    return;

                try
                {
                    // Verifica se precisa rotacionar o arquivo
                    CheckAndRotateLog();

                    // Formata a linha de log
                    string logLine = FormatLogLine(level, message, exception);

                    // Grava no arquivo
                    File.AppendAllText(_currentLogPath, logLine + Environment.NewLine, Encoding.UTF8);

                    // Se debug mode, também grava no console
                    if (_debugMode)
                    {
                        System.Diagnostics.Debug.WriteLine(logLine);
                    }
                }
                catch (Exception ex)
                {
                    // Falha ao gravar log, tenta console como fallback
                    System.Diagnostics.Debug.WriteLine($"[Logger] Failed to write log: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[Logger] Original message: {message}");
                }
            }
        }

        /// <summary>
        /// Formata uma linha de log com timestamp e informações.
        /// </summary>
        /// <param name="level">Nível do log</param>
        /// <param name="message">Mensagem</param>
        /// <param name="exception">Exceção opcional</param>
        /// <returns>Linha formatada</returns>
        private string FormatLogLine(LogLevel level, string message, Exception exception)
        {
            StringBuilder sb = new StringBuilder();

            // Timestamp
            sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]");

            // Nível com padding
            sb.Append($" [{level.ToString().ToUpper().PadRight(8)}]");

            // Mensagem
            sb.Append($" {message}");

            // Exception se existir
            if (exception != null)
            {
                sb.AppendLine();
                sb.Append($"    Exception: {exception.GetType().Name}: {exception.Message}");

                if (!string.IsNullOrWhiteSpace(exception.StackTrace))
                {
                    sb.AppendLine();
                    sb.Append($"    StackTrace: {exception.StackTrace}");
                }

                // Inner exception
                if (exception.InnerException != null)
                {
                    sb.AppendLine();
                    sb.Append($"    InnerException: {exception.InnerException.Message}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Verifica o tamanho do arquivo e rotaciona se necessário.
        /// </summary>
        private void CheckAndRotateLog()
        {
            try
            {
                if (!File.Exists(_currentLogPath))
                    return;

                FileInfo fileInfo = new FileInfo(_currentLogPath);

                if (fileInfo.Length >= _maxFileSizeBytes)
                {
                    // Cria novo arquivo
                    string oldPath = _currentLogPath;
                    _currentLogPath = GenerateLogFilePath();

                    // Escreve header no novo arquivo
                    WriteLogHeader();

                    // Log da rotação
                    Info($"Log file rotated. Old file: {Path.GetFileName(oldPath)}");
                }
            }
            catch
            {
                // Falha silenciosa - continua usando o arquivo atual
            }
        }

        /// <summary>
        /// Log de nível Debug.
        /// </summary>
        public void Debug(string message)
        {
            WriteLog(LogLevel.Debug, message);
        }

        /// <summary>
        /// Log de nível Info.
        /// </summary>
        public void Info(string message)
        {
            WriteLog(LogLevel.Info, message);
        }

        /// <summary>
        /// Log de nível Warning.
        /// </summary>
        public void Warning(string message)
        {
            WriteLog(LogLevel.Warning, message);
        }

        /// <summary>
        /// Log de nível Warning com exceção.
        /// CORREÇÃO: Método adicionado para aceitar Exception como segundo parâmetro.
        /// </summary>
        public void Warning(string message, Exception exception)
        {
            WriteLog(LogLevel.Warning, message, exception);
        }

        /// <summary>
        /// Log de nível Error.
        /// </summary>
        public void Error(string message, Exception exception = null)
        {
            WriteLog(LogLevel.Error, message, exception);
        }

        /// <summary>
        /// Log de nível Critical.
        /// </summary>
        public void Critical(string message, Exception exception = null)
        {
            WriteLog(LogLevel.Critical, message, exception);
        }

        /// <summary>
        /// Limpa logs antigos do diretório.
        /// </summary>
        /// <param name="daysToKeep">Quantidade de dias de logs a manter</param>
        /// <returns>Quantidade de arquivos removidos</returns>
        public int CleanOldLogs(int daysToKeep = 7)
        {
            if (daysToKeep < 1)
                return 0;

            lock (_lock)
            {
                try
                {
                    if (!Directory.Exists(_logDirectory))
                        return 0;

                    DateTime cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                    string[] logFiles = Directory.GetFiles(_logDirectory, "*.log");
                    int removedCount = 0;

                    foreach (string file in logFiles)
                    {
                        // Não remove o arquivo atual
                        if (file.Equals(_currentLogPath, StringComparison.OrdinalIgnoreCase))
                            continue;

                        try
                        {
                            FileInfo fileInfo = new FileInfo(file);

                            if (fileInfo.CreationTime < cutoffDate)
                            {
                                File.Delete(file);
                                removedCount++;
                            }
                        }
                        catch
                        {
                            // Falha ao remover arquivo individual, continua
                        }
                    }

                    if (removedCount > 0)
                    {
                        Info($"Cleaned {removedCount} old log file(s)");
                    }

                    return removedCount;
                }
                catch (Exception ex)
                {
                    Error("Failed to clean old logs", ex);
                    return 0;
                }
            }
        }

        /// <summary>
        /// Força gravação de todos os buffers no disco.
        /// Útil antes de shutdown.
        /// </summary>
        public void Flush()
        {
            lock (_lock)
            {
                if (!_isInitialized)
                    return;

                try
                {
                    // Não há buffer real, mas serve como ponto de sincronização
                    Info("Logger flushed");
                }
                catch
                {
                    // Silencioso
                }
            }
        }
    }
}