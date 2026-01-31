// ===============================
// RideRequest.cs
// ===============================
using System;
using GTA.Math;

namespace RLF.GTA.Jobs.Uber.Ride
{
    public sealed class RideRequest
    {
        public Guid Id { get; }
        public RideCategory Category { get; }
        public Vector3 PickupLocation { get; }
        public Vector3 DestinationLocation { get; }
        public DateTime CreatedAt { get; }
        public DateTime ExpiresAt { get; }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        public RideRequest(
            RideCategory category,
            Vector3 pickup,
            Vector3 destination,
            int timeoutSeconds)
        {
            Id = Guid.NewGuid();
            Category = category;
            PickupLocation = pickup;
            DestinationLocation = destination;
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = CreatedAt.AddSeconds(timeoutSeconds);
        }
    }
}