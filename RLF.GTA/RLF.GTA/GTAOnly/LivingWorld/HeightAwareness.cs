using GTA;
using GTA.Math;
using System;

namespace RLF.GTA.GTAOnly.Awareness.Systems
{
    public class HeightAwareness
    {
        public float HeightFactor { get; private set; }
        public bool IsAtRisk { get; private set; }

        public void Update()
        {
            var ped = Game.Player.Character;

            if (!ped.Exists())
                return;

            float heightAboveGround = ped.HeightAboveGround;

            HeightFactor = Math.Min(1f, heightAboveGround / 10f);
            IsAtRisk = heightAboveGround > 3.0f;
        }
    }
}
