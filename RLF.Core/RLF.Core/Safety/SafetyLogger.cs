using System;
using System.Collections.Generic;
using System.IO;

namespace RLF.Core.Safety
{
    /// <summary>
    /// Logger dedicado ao sistema de segurança.
    /// REFINADO: Tick throttled a cada 5 frames.
    /// </summary>
    public sealed class SafetyLogger
    {
        #region Singleton

        private static SafetyLogger _instance;
        private static readonly object _lock = new object();

        public static SafetyLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new SafetyLogger();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private StreamWriter _writer;
        private readonly List<string> _buffer = new List<string>(32);
        private int _framesSinceFlush;
        private int _tickFrameCounter;  // REFINADO: contador para throttle do Tick
        private bool _initialized;
        private bool _shutdown;  // REFINADO: flag para evitar shutdown duplo
        private string _logPath;

        private const int BUFFER_MAX = 20;
        private const int FLUSH_FRAMES = 90;
        private const int TICK_THROTTLE = 5;  // REFINADO: só processa a cada 5 frames

        #endregion

        #region Initialization

        public void Initialize(string logDirectory)
        {
            if (_initialized) return;  // Evita re-init

            try
            {
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);

                _logPath = Path.Combine(logDirectory, "Safety.log");

                // Rotação se muito grande
                if (File.Exists(_logPath))
                {
                    var fi = new FileInfo(_logPath);
                    if (fi.Length > 5 * 1024 * 1024) // 5MB
                    {
                        string backup = _logPath + ".old";
                        if (File.Exists(backup)) File.Delete(backup);
                        File.Move(_logPath, backup);
                    }
                }

                _writer = new StreamWriter(_logPath, true);
                _initialized = true;
                _shutdown = false;

                Log("=== Safety Logger Initialized ===");
            }
            catch
            {
                _initialized = false;
            }
        }

        #endregion

        #region Logging

        public void Log(string message)
        {
            if (!_initialized || _shutdown) return;

            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";

            lock (_lock)
            {
                _buffer.Add(line);

                // Flush imediato se buffer cheio
                if (_buffer.Count >= BUFFER_MAX)
                {
                    Flush();
                }
            }
        }

        public void LogError(string message)
        {
            Log($"[ERROR] {message}");
        }

        public void LogWarning(string message)
        {
            Log($"[WARN] {message}");
        }

        /// <summary>
        /// Chamado pelo SafeExecutionManager.
        /// REFINADO: Só processa flush a cada N frames para reduzir overhead.
        /// </summary>
        public void Tick()
        {
            if (!_initialized || _shutdown) return;

            _tickFrameCounter++;

            // REFINADO: só verifica flush a cada 5 frames
            if (_tickFrameCounter < TICK_THROTTLE)
                return;

            _tickFrameCounter = 0;
            _framesSinceFlush += TICK_THROTTLE;

            if (_framesSinceFlush >= FLUSH_FRAMES)
            {
                lock (_lock)
                {
                    Flush();
                }
            }
        }

        private void Flush()
        {
            if (!_initialized || _writer == null || _buffer.Count == 0 || _shutdown)
                return;

            try
            {
                foreach (var line in _buffer)
                    _writer.WriteLine(line);
                _writer.Flush();
                _buffer.Clear();
                _framesSinceFlush = 0;
            }
            catch { }
        }

        #endregion

        #region Shutdown

        public void Shutdown()
        {
            // REFINADO: evita shutdown duplo
            if (_shutdown) return;

            lock (_lock)
            {
                if (_shutdown) return;  // Double-check dentro do lock
                _shutdown = true;

                Flush();

                try
                {
                    _writer?.Close();
                    _writer?.Dispose();
                    _writer = null;
                }
                catch { }

                _initialized = false;
            }
        }

        /// <summary>
        /// REFINADO: Verifica se já foi desligado
        /// </summary>
        public bool IsShutdown => _shutdown;

        #endregion
    }
}