using GTA;
using System;

namespace RLF.GTA.GTAOnly.Awareness.Systems
{
    public class StressAwareness
    {
        private DateTime _lastCombatTime;

        public bool IsStressed { get; private set; }
        public float StressLevel { get; private set; }

        public void Update()
        {
            var ped = Game.Player.Character;

            if (!ped.Exists())
                return;

            if (ped.IsInCombat)
            {
                _lastCombatTime = DateTime.Now;
                StressLevel = 1.0f;
            }
            else
            {
                var secondsSinceCombat = (DateTime.Now - _lastCombatTime).TotalSeconds;

                StressLevel = (float)Math.Max(0, 1.0 - (secondsSinceCombat / 10.0));
            }

            IsStressed = StressLevel > 0.2f;
        }
    }
}
