using System;
using GTA;
using GTA.Math;
using GTA.Native;
using RLF.GTA.GTAOnly.LivingWorld.Core;

namespace RLF.GTA.GTAOnly.LivingWorld.Events
{
    public class TrafficAccidentEvent : WorldEvent
    {
        public override string DisplayName => "Acidente de Trânsito";

        private Vehicle _vehicle;
        private Ped _driver;
        private Blip _blip;

        private DateTime _endTime;
        private bool _triggered;

        protected override bool OnStart(WorldEventContext ctx)
        {
            // 🔹 Spawn MAIS PRÓXIMO (menos falha)
            Vector3 spawnPos = global::GTA.World.GetNextPositionOnStreet(
                ctx.PlayerPosition + ctx.PlayerForward * 55f
            );

            if (spawnPos == Vector3.Zero)
                return false;

            Position = spawnPos;

            _vehicle = global::GTA.World.CreateVehicle(VehicleHash.Sultan, spawnPos, ctx.PlayerHeading);
            if (_vehicle == null || !_vehicle.Exists())
                return false;

            _vehicle.IsPersistent = true;
            _vehicle.EngineHealth = 900f;

            _driver = global::GTA.World.CreatePed(PedHash.Business01AMM, spawnPos);
            if (_driver == null || !_driver.Exists())
                return false;

            _driver.IsPersistent = true;
            _driver.SetIntoVehicle(_vehicle, VehicleSeat.Driver);

            _driver.Task.CruiseWithVehicle(_vehicle, 18f, VehicleDrivingFlags.None);

            try
            {
                Function.Call(Hash.SET_PED_CONFIG_FLAG, _driver, 32, true);
                Function.Call(Hash.SET_PED_CONFIG_FLAG, _driver, 281, true);
            }
            catch { }

            if (LivingWorldConfig.CreateBlipForEvents)
            {
                _blip = global::GTA.World.CreateBlip(spawnPos);
                if (_blip != null && _blip.Exists())
                {
                    _blip.Sprite = BlipSprite.Deathmatch;
                    _blip.IsShortRange = false;
                    _blip.Name = DisplayName;
                }
            }

            _endTime = DateTime.Now.AddSeconds(50);
            _triggered = false;

            return true;
        }

        protected override void OnUpdate()
        {
            if (_vehicle == null || !_vehicle.Exists())
            {
                Stop();
                return;
            }

            var player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            float dist = player.Position.DistanceTo(_vehicle.Position);

            if (!_triggered && dist < 40f)
            {
                _triggered = true;

                try
                {
                    Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, _vehicle, 38f);
                    Function.Call(Hash.SET_VEHICLE_REDUCE_GRIP, _vehicle, true);
                }
                catch { }
            }

            if (DateTime.Now > _endTime)
                Stop();
        }

        protected override void OnStop()
        {
            try
            {
                _blip?.Delete();

                // 🧹 LIMPEZA SEGURA (ANTI-CRASH)
                if (_driver != null && _driver.Exists())
                    _driver.MarkAsNoLongerNeeded();

                if (_vehicle != null && _vehicle.Exists())
                    _vehicle.MarkAsNoLongerNeeded();
            }
            catch { }
        }
    }
}
