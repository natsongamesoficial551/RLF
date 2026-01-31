using GTA;
using RLF.Core.Entities;

namespace RLF.GTA.Entities
{
    /// <summary>
    /// Ponte entre EntityRegistry e entidades do GTA.
    /// </summary>
    public static class GTAEntityBridge
    {
        private static EntityRegistry _registry;
        private static bool _initialized;

        public static void Initialize(EntityRegistry registry)
        {
            if (_initialized || registry == null)
                return;

            _registry = registry;

            _registry.SetDeleteHandler(new EntityDeleteHandler(DeleteEntity));
            _registry.SetExistsHandler(new EntityExistsHandler(EntityExists));

            _initialized = true;
        }

        public static void UpdatePlayerPosition()
        {
            if (_registry == null)
                return;

            var player = Game.Player.Character;
            if (player != null && player.Exists())
            {
                var pos = player.Position;
                _registry.UpdatePlayerPosition(pos.X, pos.Y, pos.Z);
            }
        }

        private static bool EntityExists(int handle, RLFEntityType type)
        {
            try
            {
                switch (type)
                {
                    case RLFEntityType.Vehicle:
                        var vehicle = (Vehicle)Entity.FromHandle(handle);
                        return vehicle != null && vehicle.Exists();

                    case RLFEntityType.Ped:
                        var ped = (Ped)Entity.FromHandle(handle);
                        return ped != null && ped.Exists();

                    case RLFEntityType.Object:
                        var prop = (Prop)Entity.FromHandle(handle);
                        return prop != null && prop.Exists();

                    case RLFEntityType.Blip:
                        return global::GTA.Native.Function.Call<bool>(
                            global::GTA.Native.Hash.DOES_BLIP_EXIST,
                            handle
                        );

                    default:
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool DeleteEntity(int handle, RLFEntityType type)
        {
            try
            {
                switch (type)
                {
                    case RLFEntityType.Vehicle:
                        var vehicle = (Vehicle)Entity.FromHandle(handle);
                        if (vehicle != null && vehicle.Exists())
                        {
                            vehicle.Delete();
                            return true;
                        }
                        break;

                    case RLFEntityType.Ped:
                        var ped = (Ped)Entity.FromHandle(handle);
                        if (ped != null && ped.Exists())
                        {
                            ped.Delete();
                            return true;
                        }
                        break;

                    case RLFEntityType.Object:
                        var prop = (Prop)Entity.FromHandle(handle);
                        if (prop != null && prop.Exists())
                        {
                            prop.Delete();
                            return true;
                        }
                        break;

                    case RLFEntityType.Blip:
                        if (global::GTA.Native.Function.Call<bool>(global::GTA.Native.Hash.DOES_BLIP_EXIST, handle))
                        {
                            global::GTA.Native.Function.Call(global::GTA.Native.Hash.REMOVE_BLIP, handle);
                            return true;
                        }
                        break;
                }
            }
            catch
            {
                // Falha silenciosa
            }

            return false;
        }

        public static bool RegisterEntity(
            Entity entity,
            RLFEntityType type,
            string tag = null,
            string owner = null,
            float maxLifetime = 0f,
            float maxDistance = 0f,
            bool persistent = false)
        {
            if (_registry == null || entity == null || !entity.Exists())
                return false;

            var pos = entity.Position;

            return _registry.Register(
                entity.Handle,
                type,
                tag,
                owner,
                maxLifetime,
                maxDistance,
                persistent,
                pos.X,
                pos.Y,
                pos.Z
            );
        }

        public static bool RegisterVehicle(
            Vehicle vehicle,
            string tag = null,
            string owner = null,
            float maxLifetime = 300f,
            float maxDistance = 200f,
            bool persistent = false)
        {
            return RegisterEntity(vehicle, RLFEntityType.Vehicle, tag, owner, maxLifetime, maxDistance, persistent);
        }

        public static bool RegisterPed(
            Ped ped,
            string tag = null,
            string owner = null,
            float maxLifetime = 180f,
            float maxDistance = 150f,
            bool persistent = false)
        {
            return RegisterEntity(ped, RLFEntityType.Ped, tag, owner, maxLifetime, maxDistance, persistent);
        }

        public static bool RegisterBlip(
            Blip blip,
            string tag = null,
            string owner = null,
            bool persistent = true)
        {
            if (_registry == null || blip == null || !blip.Exists())
                return false;

            var pos = blip.Position;

            return _registry.Register(
                blip.Handle,
                RLFEntityType.Blip,
                tag,
                owner,
                0f,
                0f,
                persistent,
                pos.X,
                pos.Y,
                pos.Z
            );
        }
    }
}