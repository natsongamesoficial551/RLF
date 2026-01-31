using System;
using System.IO;
using System.Text;

namespace RLF.Core.Debug
{
    public static class RLFDebug
    {
        private static readonly object _lock = new object();
        private static DebugConfig _config = new DebugConfig();
        private static string _logPath;

        public static void Initialize(DebugConfig config = null)
        {
            _config = config ?? new DebugConfig();

            Directory.CreateDirectory("scripts/RLF/Debug");
            _logPath = $"scripts/RLF/Debug/RLF_Debug_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";

            WriteRaw("===============================================");
            WriteRaw(" RLF DEBUG SYSTEM INITIALIZED");
            WriteRaw($" Started: {DateTime.Now}");
            WriteRaw("===============================================");
        }

        // =========================
        // PUBLIC API
        // =========================

        public static void Trace(DebugChannel ch, string msg, object ctx = null)
            => Write(DebugLevel.Trace, ch, msg, ctx);

        public static void Info(DebugChannel ch, string msg, object ctx = null)
            => Write(DebugLevel.Info, ch, msg, ctx);

        public static void Warning(DebugChannel ch, string msg, object ctx = null)
            => Write(DebugLevel.Warning, ch, msg, ctx);

        public static void Error(DebugChannel ch, string msg, Exception ex = null, object ctx = null)
            => Write(DebugLevel.Error, ch, msg, ctx, ex);

        public static void Critical(DebugChannel ch, string msg, Exception ex = null, object ctx = null)
            => Write(DebugLevel.Critical, ch, msg, ctx, ex);

        // =========================
        // CORE WRITE
        // =========================

        private static void Write(
            DebugLevel level,
            DebugChannel channel,
            string message,
            object context = null,
            Exception exception = null)
        {
            if (!_config.Enabled)
                return;

            if (level < _config.MinLevel)
                return;

            if (!_config.IsChannelEnabled(channel))
                return;

            var sb = new StringBuilder();

            sb.Append($"[{DateTime.Now:HH:mm:ss.fff}]");
            sb.Append($"[{level.ToString().ToUpper()}]");
            sb.Append($"[{channel}] ");
            sb.Append(message);

            if (context != null)
            {
                sb.AppendLine();
                sb.Append(" Context: ");
                sb.Append(context);
            }

            if (exception != null)
            {
                sb.AppendLine();
                sb.Append(" Exception: ");
                sb.Append(exception.GetType().Name);
                sb.Append(" - ");
                sb.Append(exception.Message);
            }

            WriteRaw(sb.ToString());
        }

        private static void WriteRaw(string text)
        {
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logPath, text + Environment.NewLine);
                }
                catch
                {
                    // nunca quebrar o jogo por causa de debug
                }
            }
        }
    }
}
