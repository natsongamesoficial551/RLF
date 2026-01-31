using System;
using GTA;
using GTA.Math;
using GTA.Native;
using RLF.GTA.GTAOnly.LivingWorld.Core;

namespace RLF.GTA.GTAOnly.LivingWorld.Events
{
    public class VehicleBreakdownEvent : WorldEvent
    {
        public override string DisplayName => "Pane Mecânica";

        private Vehicle _vehicle;
        private Ped _driver;
        private Blip _blip;

        private DateTime _endTime;

        private bool _stopped;
        private bool _leftVehicle;
        private bool _repairing;

        protected override bool OnStart(WorldEventContext ctx)
        {
            Vector3 streetPos = global::GTA.World.GetNextPositionOnStreet(
                ctx.PlayerPosition + ctx.PlayerForward * LivingWorldConfig.MinSpawnDistance
            );

            if (streetPos == Vector3.Zero)
                return false;

            Vector3 curbPos = streetPos + ctx.PlayerRight * 2.8f;
            Position = curbPos;

            _vehicle = global::GTA.World.CreateVehicle(VehicleHash.Premier, curbPos, ctx.PlayerHeading);
            if (_vehicle == null || !_vehicle.Exists())
                return false;

            _vehicle.IsPersistent = true;

            _driver = global::GTA.World.CreatePed(PedHash.Business01AMM, curbPos);
            if (_driver == null || !_driver.Exists())
                return false;

            _driver.IsPersistent = true;
            _driver.SetIntoVehicle(_vehicle, VehicleSeat.Driver);

            _driver.Task.CruiseWithVehicle(_vehicle, 18f, VehicleDrivingFlags.None);

            if (LivingWorldConfig.CreateBlipForEvents)
            {
                _blip = global::GTA.World.CreateBlip(curbPos);
                if (_blip != null && _blip.Exists())
                {
                    _blip.Sprite = BlipSprite.PersonalVehicleCar;
                    _blip.IsShortRange = false;
                    _blip.Name = DisplayName;
                }
            }

            _endTime = DateTime.Now.AddSeconds(60);

            _stopped = false;
            _leftVehicle = false;
            _repairing = false;

            return true;
        }

        protected override void OnUpdate()
        {
            if (_vehicle == null || _driver == null)
                return;

            if (!_vehicle.Exists() || !_driver.Exists())
                return;

            var player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            float dist = player.Position.DistanceTo(_vehicle.Position);

            // 1️⃣ Para o carro
            if (!_stopped && dist < 35f)
            {
                _stopped = true;

                try
                {
                    Function.Call(Hash.SET_VEHICLE_ENGINE_ON, _vehicle, false, true, true);
                    Function.Call(Hash.SET_VEHICLE_HANDBRAKE, _vehicle, true);
                    Function.Call(Hash.BRING_VEHICLE_TO_HALT, _vehicle, 2.0f, 2, false);

                    // 🔧 ABRE O CAPÔ
                    Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, _vehicle, 4, false, false);
                }
                catch { }
            }

            // 2️⃣ Sai do carro
            if (_stopped && !_leftVehicle)
            {
                _leftVehicle = true;

                try
                {
                    _driver.Task.LeaveVehicle(_vehicle, LeaveVehicleFlags.None);
                }
                catch { }
            }

            // 3️⃣ Animação manual de mecânico (estável)
            if (_leftVehicle && !_repairing && !_driver.IsInVehicle())
            {
                _repairing = true;

                try
                {
                    Vector3 animPos = Function.Call<Vector3>(
                        Hash.GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS,
                        _vehicle,
                        1.2f, 1.2f, 0f
                    );

                    float groundZ;
                    if (global::GTA.World.GetGroundHeight(animPos, out groundZ))
                        animPos.Z = groundZ;

                    _driver.Position = animPos;
                    _driver.Heading = _vehicle.Heading + 90f;

                    Function.Call(Hash.REQUEST_ANIM_DICT, "mini@repair");
                    while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "mini@repair"))
                        Script.Wait(0);

                    _driver.Task.PlayAnimation(
                        "mini@repair",
                        "fixing_a_ped",
                        8.0f,
                        -8.0f,
                        -1,
                        AnimationFlags.Loop,
                        0f
                    );
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
                if (_vehicle != null && _vehicle.Exists())
                {
                    // fecha o capô ao terminar
                    Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, _vehicle, 4, false);
                }
            }
            catch { }

            try { _blip?.Delete(); } catch { }
        }
    }
}
