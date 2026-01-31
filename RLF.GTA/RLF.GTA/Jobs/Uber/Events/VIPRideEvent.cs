// ===============================
// VIPRideEvent.cs
// ===============================
using System;

namespace RLF.GTA.Jobs.Uber.Events
{
    public sealed class VIPRideEvent
    {
        public DateTime OccurredAt { get; }
        public decimal BonusMultiplier { get; }
        public string PassengerName { get; }
        public string Description { get; }

        public VIPRideEvent(string passengerName, decimal bonusMultiplier)
        {
            OccurredAt = DateTime.UtcNow;
            PassengerName = passengerName;
            BonusMultiplier = bonusMultiplier;
            Description = $"Passageiro VIP: {passengerName}";
        }
    }
}