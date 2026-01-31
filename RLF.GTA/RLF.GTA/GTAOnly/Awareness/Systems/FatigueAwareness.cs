using GTA;
using System;

namespace RLF.GTA.GTAOnly.Awareness.Systems
{
    public class FatigueAwareness
    {
        public float FatigueLevel { get; private set; }

        private DateTime _lastUpdate;

        public FatigueAwareness()
        {
            _lastUpdate = DateTime.Now;
        }

        public void Update()
        {
            var ped = Game.Player.Character;
            if (!ped.Exists())
                return;

            var now = DateTime.Now;
            var deltaSeconds = (now - _lastUpdate).TotalSeconds;
            _lastUpdate = now;

            if (ped.IsSprinting)
            {
                FatigueLevel += (float)(deltaSeconds * 0.05);
            }
            else
            {
                FatigueLevel -= (float)(deltaSeconds * 0.03);
            }

            FatigueLevel = Math.Max(0f, Math.Min(1f, FatigueLevel));
        }
    }
}
