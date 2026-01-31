using System;

namespace RLF.Core.Performance
{
    /// <summary>
    /// Snapshot imutável de dados do player.
    /// Capturado uma vez por frame para evitar múltiplas chamadas de natives.
    /// </summary>
    public sealed class PlayerSnapshot
    {
        // Posição
        public float PositionX { get; }
        public float PositionY { get; }
        public float PositionZ { get; }

        // Direção
        public float ForwardX { get; }
        public float ForwardY { get; }
        public float ForwardZ { get; }
        public float Heading { get; }

        // Estado
        public bool IsAlive { get; }
        public bool IsInVehicle { get; }
        public bool IsOnFoot { get; }
        public bool IsAiming { get; }
        public bool IsShooting { get; }
        public bool IsRunning { get; }
        public bool IsSprinting { get; }

        // Saúde
        public int Health { get; }
        public int MaxHealth { get; }
        public int Armor { get; }

        // Veículo (se estiver em um)
        public int CurrentVehicleHandle { get; }
        public bool IsDriver { get; }
        public float VehicleSpeed { get; }

        // Dinheiro
        public int Money { get; }

        // Metadata
        public int FrameCaptured { get; }
        public long TimestampMs { get; }

        /// <summary>
        /// Construtor interno - use PlayerSnapshotCapture para criar.
        /// </summary>
        public PlayerSnapshot(
            float posX, float posY, float posZ,
            float fwdX, float fwdY, float fwdZ, float heading,
            bool isAlive, bool inVehicle, bool onFoot,
            bool aiming, bool shooting, bool running, bool sprinting,
            int health, int maxHealth, int armor,
            int vehicleHandle, bool isDriver, float vehicleSpeed,
            int money, int frame)
        {
            PositionX = posX;
            PositionY = posY;
            PositionZ = posZ;
            ForwardX = fwdX;
            ForwardY = fwdY;
            ForwardZ = fwdZ;
            Heading = heading;

            IsAlive = isAlive;
            IsInVehicle = inVehicle;
            IsOnFoot = onFoot;
            IsAiming = aiming;
            IsShooting = shooting;
            IsRunning = running;
            IsSprinting = sprinting;

            Health = health;
            MaxHealth = maxHealth;
            Armor = armor;

            CurrentVehicleHandle = vehicleHandle;
            IsDriver = isDriver;
            VehicleSpeed = vehicleSpeed;

            Money = money;

            FrameCaptured = frame;
            TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Calcula distância até um ponto (sem alocar Vector3).
        /// </summary>
        public float DistanceTo(float x, float y, float z)
        {
            float dx = PositionX - x;
            float dy = PositionY - y;
            float dz = PositionZ - z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Calcula distância 2D (ignora Z).
        /// </summary>
        public float DistanceTo2D(float x, float y)
        {
            float dx = PositionX - x;
            float dy = PositionY - y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Verifica se está dentro de um raio.
        /// </summary>
        public bool IsWithinRange(float x, float y, float z, float range)
        {
            return DistanceTo(x, y, z) <= range;
        }
    }
}