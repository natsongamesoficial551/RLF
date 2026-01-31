using System;

namespace RLF.Core.Entities
{
    /// <summary>
    /// Representa uma entidade rastreada pelo EntityRegistry.
    /// </summary>
    public sealed class TrackedEntity
    {
        public int Handle { get; }
        public RLFEntityType Type { get; }  // ALTERADO
        public string Tag { get; }
        public string Owner { get; }
        public DateTime CreatedAt { get; }
        public float MaxLifetimeSeconds { get; }
        public float MaxDistanceFromPlayer { get; }
        public bool IsPersistent { get; }
        public object UserData { get; }
        public float SpawnX { get; }
        public float SpawnY { get; }
        public float SpawnZ { get; }

        public TimeSpan Age => DateTime.Now - CreatedAt;
        public double AgeSeconds => Age.TotalSeconds;

        public TrackedEntity(
            int handle,
            RLFEntityType type,  // ALTERADO
            string tag = null,
            string owner = null,
            float maxLifetimeSeconds = 0f,
            float maxDistanceFromPlayer = 0f,
            bool isPersistent = false,
            object userData = null,
            float spawnX = 0f,
            float spawnY = 0f,
            float spawnZ = 0f)
        {
            Handle = handle;
            Type = type;
            Tag = tag ?? string.Empty;
            Owner = owner ?? "Unknown";
            CreatedAt = DateTime.Now;
            MaxLifetimeSeconds = maxLifetimeSeconds;
            MaxDistanceFromPlayer = maxDistanceFromPlayer;
            IsPersistent = isPersistent;
            UserData = userData;
            SpawnX = spawnX;
            SpawnY = spawnY;
            SpawnZ = spawnZ;
        }

        public bool IsExpiredByTime()
        {
            if (IsPersistent || MaxLifetimeSeconds <= 0)
                return false;
            return AgeSeconds >= MaxLifetimeSeconds;
        }

        public bool IsExpiredByDistance(float playerX, float playerY, float playerZ)
        {
            if (IsPersistent || MaxDistanceFromPlayer <= 0)
                return false;

            float dx = SpawnX - playerX;
            float dy = SpawnY - playerY;
            float dz = SpawnZ - playerZ;
            float distSq = dx * dx + dy * dy + dz * dz;
            float maxDistSq = MaxDistanceFromPlayer * MaxDistanceFromPlayer;

            return distSq > maxDistSq;
        }

        public override string ToString()
        {
            return $"[{Type}:{Handle}] Tag={Tag}, Owner={Owner}, Age={AgeSeconds:F1}s";
        }
    }
}