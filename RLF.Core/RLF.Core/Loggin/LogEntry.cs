using System;
using RLF.Core.Pooling;
using LogLevel = RLF.Core.Logging.LogLevel;

namespace RLF.Core.Loggin
{
    /// <summary>
    /// Entrada de log estruturada e poolável.
    /// </summary>
    public sealed class LogEntry : IPoolable
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public string ExceptionInfo { get; set; }
        public int ThreadId { get; set; }

        public LogEntry()
        {
            Reset();
        }

        public void Reset()
        {
            Timestamp = default;
            Level = LogLevel.Info;
            Category = null;
            Message = null;
            ExceptionInfo = null;
            ThreadId = 0;
        }

        public void Set(LogLevel level, string category, string message, Exception ex = null)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Category = category;
            Message = message;
            ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            if (ex != null)
            {
                ExceptionInfo = $"{ex.GetType().Name}: {ex.Message}";
                if (ex.StackTrace != null)
                {
                    ExceptionInfo += Environment.NewLine + ex.StackTrace;
                }
            }
        }

        public string Format()
        {
            return StringBuilderPool.Build(sb =>
            {
                sb.Append('[');
                sb.Append(Timestamp.ToString("HH:mm:ss.fff"));
                sb.Append("] [");
                sb.Append(Level.ToString().ToUpper());
                sb.Append(']');

                if (!string.IsNullOrEmpty(Category))
                {
                    sb.Append(" [");
                    sb.Append(Category);
                    sb.Append(']');
                }

                sb.Append(' ');
                sb.Append(Message);

                if (!string.IsNullOrEmpty(ExceptionInfo))
                {
                    sb.AppendLine();
                    sb.Append("  Exception: ");
                    sb.Append(ExceptionInfo);
                }
            });
        }
    }
}