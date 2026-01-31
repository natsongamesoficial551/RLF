using GTA;
using GTA.Math;
using RLF.Core.Logging;
using System;

namespace RLF.GTA.Jobs.Uber.Passenger
{
    public sealed class PassengerNPC
    {
        private readonly Logger _logger;
        private Ped _npc;
        private PassengerBehavior _behavior;

        public Ped NPC => _npc;
        public PassengerBehavior Behavior => _behavior;
        public bool Exists => _npc != null && _npc.Exists();

        public PassengerNPC(Logger logger)
        {
            _logger = logger;
            _behavior = new PassengerBehavior(logger);
        }

        public bool Spawn(Vector3 position, float driverRating)
        {
            try
            {
                PedHash[] models = new PedHash[]
                {
                    PedHash.Business01AMY,
                    PedHash.Business01AFY,
                    PedHash.Business02AMY,
                    PedHash.Business03AMY,
                    PedHash.Business04AFY,
                    PedHash.FreemodeFemale01,
                    PedHash.FreemodeMale01
                };

                Random rng = new Random();
                PedHash selectedModel = models[rng.Next(models.Length)];

                _npc = World.CreatePed(selectedModel, position);

                if (_npc == null || !_npc.Exists())
                {
                    _logger.Error("[Uber] Falha ao spawnar passageiro");
                    return false;
                }

                _npc.IsPersistent = true;
                _npc.BlockPermanentEvents = true;

                _behavior.RollBehavior(driverRating);

                _logger.Info($"[Uber] Passageiro spawnado: {_behavior.CurrentBehavior}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("[Uber] Erro ao spawnar passageiro", ex);
                return false;
            }
        }

        // ✅ CORRIGIDO: Usa global::GTA.Vehicle
        public void EnterVehicle(global::GTA.Vehicle vehicle)
        {
            if (_npc != null && _npc.Exists() && vehicle != null && vehicle.Exists())
            {
                _npc.Task.EnterVehicle(vehicle, VehicleSeat.RightRear);
            }
        }

        public void ExitVehicle()
        {
            if (_npc != null && _npc.Exists())
            {
                _npc.Task.LeaveVehicle();
            }
        }

        public void Cleanup()
        {
            if (_npc != null && _npc.Exists())
            {
                _npc.MarkAsNoLongerNeeded();
                _npc = null;
            }
        }
    }
}