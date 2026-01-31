using GTA;
using GTA.Math;
using GTA.Native;

namespace RLF.GTA.Law.Police
{
    public sealed class PoliceDetectionService
    {
        private const float DETECT_RADIUS = 50f;

        public bool TryDetect(PoliceUnit unit, out PoliceTarget target, out string reason)
        {
            target = null;
            reason = null;

            if (unit == null || unit.Vehicle == null || !unit.Vehicle.Exists())
                return false;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return false;

            Vehicle pv = player.CurrentVehicle;
            if (pv == null || !pv.Exists())
                return false;

            float dist = unit.Vehicle.Position.DistanceTo(pv.Position);
            if (dist > DETECT_RADIUS)
                return false;

            if (IsWrongWayAccurate(pv))
            {
                reason = "Contramão";
                target = new PoliceTarget(player, pv);
                return true;
            }

            if (IsSpeedingByRoadLimit(pv, out float kmh, out float roadLimit))
            {
                if (kmh > (roadLimit + 50f))
                {
                    if (ShouldEnforce(0.60))
                    {
                        reason = $"Excesso severo ({(int)kmh} km/h em via de {(int)roadLimit} km/h)";
                        target = new PoliceTarget(player, pv);
                        return true;
                    }
                }
                else if (kmh > (roadLimit + 35f))
                {
                    if (ShouldEnforce(0.30))
                    {
                        reason = $"Excesso de velocidade ({(int)kmh} km/h em via de {(int)roadLimit} km/h)";
                        target = new PoliceTarget(player, pv);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsSpeedingByRoadLimit(Vehicle v, out float kmh, out float roadLimit)
        {
            kmh = 0f;
            roadLimit = 0f;

            try
            {
                kmh = v.Speed * 3.6f;
                Vector3 pos = v.Position;

                Vehicle[] nearbyVehicles = World.GetNearbyVehicles(pos, 50f);
                int trafficCount = nearbyVehicles.Length;

                if (trafficCount >= 8)
                {
                    roadLimit = 60f;
                }
                else if (trafficCount >= 3)
                {
                    roadLimit = 80f;
                }
                else
                {
                    roadLimit = 120f;
                }

                return kmh > (roadLimit + 35f);
            }
            catch
            {
                return false;
            }
        }

        private bool IsWrongWayAccurate(Vehicle v)
        {
            try
            {
                Vector3 pos = v.Position;
                float kmh = v.Speed * 3.6f;

                if (kmh < 20f)
                    return false;

                Vector3 roadA = World.GetNextPositionOnStreet(pos);
                Vector3 ahead = pos + (v.ForwardVector * 25f);
                Vector3 roadB = World.GetNextPositionOnStreet(ahead);
                Vector3 roadDir = (roadB - roadA);

                if (roadDir.Length() < 0.5f)
                    return false;

                roadDir.Normalize();
                float dot = Vector3.Dot(v.ForwardVector, roadDir);

                return dot < -0.5f;
            }
            catch
            {
                return false;
            }
        }

        private bool ShouldEnforce(double chance)
        {
            int seed = Game.GameTime ^ System.Environment.TickCount;
            var rng = new System.Random(seed);
            return rng.NextDouble() <= chance;
        }
    }
}