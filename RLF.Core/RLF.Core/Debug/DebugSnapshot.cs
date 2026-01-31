using System;
using System.Collections.Generic;
using System.Text;

namespace RLF.Core.Debug
{
    public class DebugSnapshot
    {
        public DateTime Timestamp { get; } = DateTime.Now;
        public string Source { get; }
        public Dictionary<string, object> Data { get; }

        public DebugSnapshot(string source)
        {
            Source = source;
            Data = new Dictionary<string, object>();
        }

        public void Add(string key, object value)
        {
            Data[key] = value;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"--- SNAPSHOT [{Source}] {Timestamp:HH:mm:ss.fff} ---");

            foreach (var kv in Data)
                sb.AppendLine($"{kv.Key}: {kv.Value}");

            return sb.ToString();
        }
    }
}
