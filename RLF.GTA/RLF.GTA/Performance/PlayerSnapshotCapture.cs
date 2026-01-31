using GTA;
using GTA.Math;
using RLF.Core.Performance;

namespace RLF.GTA.Performance
{
    /// <summary>
    /// Captura dados do player uma vez por frame.
    /// </summary>
    public sealed class PlayerSnapshotCapture
    {
        private PlayerSnapshot _current;
        private int _lastCaptureFrame;

        private static PlayerSnapshotCapture _instance;
        public static PlayerSnapshotCapture Instance => _instance ?? (_instance = new PlayerSnapshotCapture());

        public PlayerSnapshot Current => _current;
        public bool IsCurrentFrame => _lastCaptureFrame == Game.FrameCount;

        private PlayerSnapshotCapture() { }

        public PlayerSnapshot Capture()
        {
            int currentFrame = Game.FrameCount;

            if (_current != null && _lastCaptureFrame == currentFrame)
                return _current;

            _lastCaptureFrame = currentFrame;

            Ped player = Game.Player.Character;

            if (player == null || !player.Exists())
            {
                _current = null;
                return null;
            }

            Vector3 pos = player.Position;
            Vector3 fwd = player.ForwardVector;

            bool inVehicle = player.IsInVehicle();
            Vehicle vehicle = inVehicle ? player.CurrentVehicle : null;

            int vehicleHandle = 0;
            bool isDriver = false;
            float vehicleSpeed = 0f;

            if (vehicle != null && vehicle.Exists())
            {
                vehicleHandle = vehicle.Handle;
                isDriver = vehicle.Driver == player;
                vehicleSpeed = vehicle.Speed;
            }

            _current = new PlayerSnapshot(
                pos.X, pos.Y, pos.Z,
                fwd.X, fwd.Y, fwd.Z,
                player.Heading,
                player.IsAlive,
                inVehicle,
                !inVehicle,
                player.IsAiming,
                player.IsShooting,
                player.IsRunning,
                player.IsSprinting,
                player.Health,
                player.MaxHealth,
                player.Armor,
                vehicleHandle,
                isDriver,
                vehicleSpeed,
                Game.Player.Money,
                currentFrame
            );

            return _current;
        }

        public PlayerSnapshot Get()
        {
            if (!IsCurrentFrame)
                return Capture();

            return _current;
        }

        public void Invalidate()
        {
            _lastCaptureFrame = -1;
        }
    }
}