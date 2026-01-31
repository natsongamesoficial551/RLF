using System;
using GTA;
using GTA.Math;
using RLF.Core.Debug;

namespace RLF.GTA.Jobs.Delivery
{
    public sealed class DeliveryVehicleManager
    {
        private Vehicle _currentVehicle;
        private Blip _vehicleBlip;

        public Vehicle CurrentVehicle => _currentVehicle;
        public bool HasVehicle => _currentVehicle != null && _currentVehicle.Exists();

        public bool SpawnVehicle(Vector3 position)
        {
            try
            {
                Cleanup();

                Vector3 spawnPos = World.GetNextPositionOnStreet(position);
                float heading = World.GetNextPositionOnStreet(position).GetHashCode() % 360;

                _currentVehicle = World.CreateVehicle(
                    new Model(DeliveryConfig.DeliveryVehicle),
                    spawnPos,
                    heading
                );

                if (_currentVehicle == null || !_currentVehicle.Exists())
                {
                    RLFDebug.Error(DebugChannel.System, "[Delivery] Falha ao spawnar veículo");
                    return false;
                }

                _currentVehicle.IsPersistent = true;
                _currentVehicle.PlaceOnGround();

                CreateVehicleBlip(spawnPos);

                RLFDebug.Info(DebugChannel.System, "[Delivery] Veículo de entrega spawnado");
                return true;
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[Delivery] Erro ao spawnar veículo", ex);
                return false;
            }
        }

        private void CreateVehicleBlip(Vector3 position)
        {
            try
            {
                _vehicleBlip?.Delete();

                _vehicleBlip = World.CreateBlip(position);
                _vehicleBlip.Sprite = BlipSprite.PersonalVehicleCar; // ✅ CORRIGIDO
                _vehicleBlip.Color = BlipColor.Blue;
                _vehicleBlip.Scale = 0.8f;
                _vehicleBlip.Name = "Moto de Entrega";
                _vehicleBlip.IsShortRange = false;
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[Delivery] Erro ao criar blip do veículo", ex);
            }
        }

        public void RemoveVehicleBlip()
        {
            try
            {
                _vehicleBlip?.Delete();
                _vehicleBlip = null;
            }
            catch { }
        }

        public void Cleanup()
        {
            try
            {
                _vehicleBlip?.Delete();
                _vehicleBlip = null;

                if (_currentVehicle != null && _currentVehicle.Exists())
                {
                    _currentVehicle.IsPersistent = false;
                    _currentVehicle.Delete();
                }

                _currentVehicle = null;
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[Delivery] Erro ao limpar veículo", ex);
            }
        }
    }
}