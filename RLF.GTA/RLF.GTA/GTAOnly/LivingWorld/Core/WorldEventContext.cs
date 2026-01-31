using GTA;
using GTA.Math;

namespace RLF.GTA.GTAOnly.LivingWorld.Core
{
    /// <summary>
    /// Contexto compartilhado para eventos do mundo
    /// </summary>
    public sealed class WorldEventContext
    {
        public Ped PlayerPed { get; }
        public Vector3 PlayerPosition { get; }
        public Vector3 PlayerForward { get; }
        public Vector3 PlayerRight { get; }
        public float PlayerHeading { get; }

        public WorldEventContext()
        {
            PlayerPed = Game.Player.Character;

            if (PlayerPed != null && PlayerPed.Exists())
            {
                PlayerPosition = PlayerPed.Position;
                PlayerForward = PlayerPed.ForwardVector;
                PlayerRight = PlayerPed.RightVector;
                PlayerHeading = PlayerPed.Heading;
            }
            else
            {
                PlayerPosition = Vector3.Zero;
                PlayerForward = Vector3.WorldNorth;
                PlayerRight = Vector3.WorldEast;
                PlayerHeading = 0f;
            }
        }
    }
}
