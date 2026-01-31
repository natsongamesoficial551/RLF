using GTA;
using System;

namespace RLF.GTA.Law.Police
{
    public static class PoliceDecisionService
    {
        private const double APPROACH_CHANCE = 0.65;

        public static bool ShouldApproach()
        {
            try
            {
                int seed = Game.GameTime ^ Environment.TickCount;
                var rng = new Random(seed);
                return rng.NextDouble() <= APPROACH_CHANCE;
            }
            catch
            {
                return true;
            }
        }
    }
}