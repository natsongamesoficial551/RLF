using GTA;

namespace RLF.GTA.GTAOnly.Awareness.Systems
{
    public class MovementAwareness
    {
        public bool IsRunning { get; private set; }
        public bool IsWalking { get; private set; }
        public bool IsIdle { get; private set; }

        public float MovementIntensity { get; private set; }

        public void Update()
        {
            var ped = Game.Player.Character;

            if (!ped.Exists())
                return;

            IsRunning = ped.IsSprinting;
            IsWalking = ped.IsWalking;
            IsIdle = !IsRunning && !IsWalking;

            if (IsRunning)
                MovementIntensity = 1.0f;
            else if (IsWalking)
                MovementIntensity = 0.4f;
            else
                MovementIntensity = 0.1f;
        }
    }
}
