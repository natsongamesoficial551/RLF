// ===============================
// TrafficEvent.cs
// ===============================
using System;

namespace RLF.GTA.Jobs.Uber.Events
{
    public sealed class TrafficEvent
    {
        public DateTime OccurredAt { get; }
        public int DelaySeconds { get; }
        public string Description { get; }

        public TrafficEvent(int delaySeconds)
        {
            OccurredAt = DateTime.UtcNow;
            DelaySeconds = delaySeconds;
            Description = "Trânsito intenso detectado na rota";
        }
    }
}