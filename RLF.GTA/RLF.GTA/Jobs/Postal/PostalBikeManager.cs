using System;
using GTA;
using GTA.Math;
using RLF.Core.Debug;

namespace RLF.GTA.Jobs.Postal
{
    public sealed class PostalBikeManager
    {
        private Vehicle _currentBike;
        private Blip _bikeBlip;

        public Vehicle CurrentBike => _currentBike;
        public bool HasBike => _currentBike != null && _currentBike.Exists();

        public bool SpawnBike(Vector3 position)
        {
            try
            {
                Cleanup();

                Vector3 spawnPos = World.GetNextPositionOnStreet(position);
                float heading = World.GetNextPositionOnStreet(position).GetHashCode() % 360;

                _currentBike = World.CreateVehicle(
                    new Model(PostalConfig.PostalBike),
                    spawnPos,
                    heading
                );

                if (_currentBike == null || !_currentBike.Exists())
                {
                    RLFDebug.Error(DebugChannel.System, "[Postal] Falha ao spawnar bicicleta");
                    return false;
                }

                _currentBike.IsPersistent = true;
                _currentBike.PlaceOnGround();

                CreateBikeBlip(spawnPos);

                RLFDebug.Info(DebugChannel.System, "[Postal] Bicicleta de correio spawnada");
                return true;
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[Postal] Erro ao spawnar bicicleta", ex);
                return false;
            }
        }

        private void CreateBikeBlip(Vector3 position)
        {
            try
            {
                _bikeBlip?.Delete();

                _bikeBlip = World.CreateBlip(position);
                _bikeBlip.Sprite = BlipSprite.PersonalVehicleBike;
                _bikeBlip.Color = BlipColor.Blue;
                _bikeBlip.Scale = 0.8f;
                _bikeBlip.Name = "Bicicleta dos Correios";
                _bikeBlip.IsShortRange = false;
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[Postal] Erro ao criar blip da bicicleta", ex);
            }
        }

        public void RemoveBikeBlip()
        {
            try
            {
                _bikeBlip?.Delete();
                _bikeBlip = null;
            }
            catch { }
        }

        public void Cleanup()
        {
            try
            {
                _bikeBlip?.Delete();
                _bikeBlip = null;

                if (_currentBike != null && _currentBike.Exists())
                {
                    _currentBike.IsPersistent = false;
                    _currentBike.Delete();
                }

                _currentBike = null;
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[Postal] Erro ao limpar bicicleta", ex);
            }
        }
    }
}
