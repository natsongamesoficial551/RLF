using GTA;
using GTA.Math;
using GTA.Native;

namespace RLF.GTA.Law.Police
{
    public sealed class PoliceSpawner
    {
        public bool Spawn(PoliceUnit unit)
        {
            if (unit == null)
                return false;

            Vehicle vehicle = null;
            Ped p1 = null;
            Ped p2 = null;

            try
            {
                if (HasNearbyPoliceVehicle(unit.SpawnPosition, 15f))
                    return false;

                Vector3 streetPos = World.GetNextPositionOnStreet(unit.SpawnPosition);

                if (HasNearbyPoliceVehicle(streetPos, 15f))
                    return false;

                Vector3 ahead =
                    World.GetNextPositionOnStreet(streetPos + new Vector3(10f, 10f, 0f));

                Vector3 dir = ahead - streetPos;
                float heading =
                    dir.Length() > 0.01f
                        ? (float)(System.Math.Atan2(dir.Y, dir.X) * 57.29578)
                        : 0f;

                vehicle = World.CreateVehicle(VehicleHash.Police3, streetPos, heading);
                if (vehicle == null || !vehicle.Exists())
                    return false;

                Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, vehicle.Handle, true, true);
                Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, vehicle.Handle);

                vehicle.Rotation = new Vector3(0f, 0f, heading);
                vehicle.Velocity = Vector3.Zero;
                vehicle.IsPersistent = true;

                Function.Call(Hash.SET_ENTITY_COLLISION, vehicle.Handle, false, false);

                p1 = World.CreatePed(PedHash.Cop01SMY, streetPos);
                p2 = World.CreatePed(PedHash.Cop01SMY, streetPos);

                if (!p1.Exists() || !p2.Exists())
                    throw new System.Exception();

                p1.IsPersistent = true;
                p2.IsPersistent = true;

                p1.SetIntoVehicle(vehicle, VehicleSeat.Driver);
                p2.SetIntoVehicle(vehicle, VehicleSeat.Passenger);

                unit.Bind(vehicle, p1, p2);
                unit.SetPatrolling();

                Script.Wait(2000);
                if (vehicle != null && vehicle.Exists())
                {
                    Function.Call(Hash.SET_ENTITY_COLLISION, vehicle.Handle, true, true);
                }

                return true;
            }
            catch
            {
                try { vehicle?.Delete(); } catch { }
                try { p1?.Delete(); } catch { }
                try { p2?.Delete(); } catch { }
                return false;
            }
        }

        private bool HasNearbyPoliceVehicle(Vector3 position, float radius)
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                Vehicle[] nearbyVehicles = World.GetNearbyVehicles(player, radius);

                foreach (var veh in nearbyVehicles)
                {
                    if (veh == null || !veh.Exists())
                        continue;

                    float dist = veh.Position.DistanceTo(position);
                    if (dist > radius)
                        continue;

                    if (veh.Model == VehicleHash.Police ||
                        veh.Model == VehicleHash.Police2 ||
                        veh.Model == VehicleHash.Police3 ||
                        veh.Model == VehicleHash.Police4)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}