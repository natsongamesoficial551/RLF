// ===============================
// RideState.cs
// ===============================
using System;
using GTA.Math;

namespace RLF.GTA.Jobs.Uber.Ride
{
    public sealed class RideState
    {
        public bool IsActive { get; internal set; }
        public RideCategory Category { get; internal set; }
        public Vector3 PickupLocation { get; internal set; }
        public Vector3 DestinationLocation { get; internal set; }
        public DateTime StartedAt { get; internal set; }
        public float DistanceTraveled { get; internal set; }
        public int TimeElapsedSeconds { get; internal set; }
        public bool PassengerOnBoard { get; internal set; }
        public int CrashCount { get; internal set; }
        public int DangerousDrivingCount { get; internal set; }

        public void Reset()
        {
            IsActive = false;
            Category = RideCategory.UberX;
            PickupLocation = Vector3.Zero;
            DestinationLocation = Vector3.Zero;
            StartedAt = DateTime.MinValue;
            DistanceTraveled = 0f;
            TimeElapsedSeconds = 0;
            PassengerOnBoard = false;
            CrashCount = 0;
            DangerousDrivingCount = 0;
        }
    }
}