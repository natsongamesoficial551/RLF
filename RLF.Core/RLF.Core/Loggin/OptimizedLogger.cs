using System;
using System.IO;
using System.Threading;
using RLF.Core.Pooling;
using LogLevel = RLF.Core.Logging.LogLevel;

namespace RLF.Core.Loggin
{
    /// <summary>
    /// Logger otimizado com buffer, rate limiting e flush assíncrono.
    /// </summary>
    public sealed class OptimizedLogger : IDisposable
    {
        private readonly LogBuffer _buffer;
        private readonly LogRateLimiter _rateLimiter;
        private readonly string _logFilePath;
        private readonly LogLevel _minLevel;

        private readonly object _fileLock;
        private readonly Timer _flushTimer;
        private readonly int _flushIntervalMs;
        private readonly int _flushThreshold;

        private StreamWriter _writer;
        private bool _disposed;
        private DateTime _lastFlush;

        public string LogFilePath => _logFilePath;
        public LogLevel MinLevel => _minLevel;
        public int BufferedCount => _buffer.Count;
        public bool IsDisposed => _disposed;

        public OptimizedLogger(
            string logDirectory,
            string logFileName,
            LogLevel minLevel = LogLevel.Info,
            int bufferCapacity = 500,
            int flushIntervalMs = 5000,
            int flushThreshold = 100,
            int rateLimitPerSecond = 20)
        {
            _minLevel = minLevel;
            _flushIntervalMs = Math.Max(1000, flushIntervalMs);
            _flushThreshold = Math.Max(10, flushThreshold);
            _fileLock = new object();

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string fileName = $"{logFileName}_{DateTime.Now:yyyy-MM-dd}.log";
            _logFilePath = Path.Combine(logDirectory, fileName);

            _buffer = new LogBuffer(bufferCapacity);
            _rateLimiter = new LogRateLimiter(rateLimitPerSecond, 1f);

            _flushTimer = new Timer(
                _ => FlushIfNeeded(),
                null,
                _flushIntervalMs,
                _flushIntervalMs
            );

            _lastFlush = DateTime.Now;

            Log(LogLevel.Info, "Logger", $"OptimizedLogger iniciado: {_logFilePath}");
        }

        #region Log Methods

        public void Debug(string message) => Log(LogLevel.Debug, null, message);
        public void Debug(string category, string message) => Log(LogLevel.Debug, category, message);

        public void Info(string message) => Log(LogLevel.Info, null, message);
        public void Info(string category, string message) => Log(LogLevel.Info, category, message);

        public void Warning(string message) => Log(LogLevel.Warning, null, message);
        public void Warning(string category, string message) => Log(LogLevel.Warning, category, message);

        public void Error(string message, Exception ex = null) => Log(LogLevel.Error, null, message, ex);
        public void Error(string category, string message, Exception ex = null) => Log(LogLevel.Error, category, message, ex);

        public void Critical(string message, Exception ex = null) => Log(LogLevel.Critical, null, message, ex);
        public void Critical(string category, string message, Exception ex = null) => Log(LogLevel.Critical, category, message, ex);

        #endregion

        public void Log(LogLevel level, string category, string message, Exception ex = null)
        {
            if (_disposed)
                return;

            if (level < _minLevel)
                return;

            if (level < LogLevel.Error)
            {
                string limitKey = category ?? "default";
                if (!_rateLimiter.ShouldAllow(limitKey))
                    return;
            }

            _buffer.Add(level, category, message, ex);

            if (level >= LogLevel.Critical || _buffer.Count >= _flushThreshold)
            {
                Flush();
            }
        }

        public void Flush()
        {
            if (_disposed)
                return;

            lock (_fileLock)
            {
                try
                {
                    EnsureWriter();

                    if (_writer != null)
                    {
                        int flushed = _buffer.FlushTo(_writer);
                        if (flushed > 0)
                        {
                            _writer.Flush();
                        }
                    }

                    _lastFlush = DateTime.Now;
                }
                catch
                {
                    // Falha silenciosa
                }
            }
        }

        private void FlushIfNeeded()
        {
            if (_disposed)
                return;

            if (_buffer.Count > 0)
            {
                Flush();
            }
        }

        private void EnsureWriter()
        {
            if (_writer != null)
                return;

            try
            {
                _writer = new StreamWriter(_logFilePath, append: true)
                {
                    AutoFlush = false
                };
            }
            catch
            {
                _writer = null;
            }
        }

        public string GetStats()
        {
            return StringBuilderPool.Build(sb =>
            {
                sb.AppendLine("=== OptimizedLogger Stats ===");
                sb.AppendLine(_buffer.GetStats());
                sb.AppendLine(_rateLimiter.GetStats());
                sb.AppendLine($"LastFlush: {_lastFlush:HH:mm:ss}");
                sb.AppendLine("=============================");
            });
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _flushTimer?.Dispose();

            try
            {
                Flush();
            }
            catch { }

            lock (_fileLock)
            {
                try
                {
                    _writer?.Dispose();
                    _writer = null;
                }
                catch { }
            }
        }
    }
}