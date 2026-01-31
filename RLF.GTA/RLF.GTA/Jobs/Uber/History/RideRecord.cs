// ===============================
// RideRecord.cs
// ===============================
using System;
using RLF.GTA.Jobs.Uber.Ride;

namespace RLF.GTA.Jobs.Uber.History
{
    public sealed class RideRecord
    {
        public DateTime Date { get; set; }
        public RideCategory Category { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public float Distance { get; set; }
        public decimal Payment { get; set; }
        public decimal Tip { get; set; }
        public float Rating { get; set; }
        public string Events { get; set; }

        public RideRecord()
        {
            Date = DateTime.UtcNow;
            Origin = "Desconhecido";
            Destination = "Desconhecido";
            Events = string.Empty;
        }
    }
}