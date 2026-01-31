using System;
using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core.CharacterCreator.Data;

namespace RLF.GTA.CharacterCreator.Core
{
    public static class VehicleManager
    {
        public static CharacterVehicle CapturePlayerVehicle()
        {
            var vehicleData = new CharacterVehicle();

            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return vehicleData;

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("🚗 CAPTURANDO VEÍCULO DO PLAYER");

                if (player.IsInVehicle())
                {
                    var vehicle = player.CurrentVehicle;
                    if (vehicle != null && vehicle.Exists())
                    {
                        vehicleData.WasInVehicle = true;
                        vehicleData.HasVehicle = true;
                        vehicleData.Model = vehicle.Model.Hash.ToString();
                        vehicleData.PositionX = vehicle.Position.X;
                        vehicleData.PositionY = vehicle.Position.Y;
                        vehicleData.PositionZ = vehicle.Position.Z;
                        vehicleData.Heading = vehicle.Heading;

                        // Cores via OutputArgument
                        var primary = new OutputArgument();
                        var secondary = new OutputArgument();
                        Function.Call(Hash.GET_VEHICLE_COLOURS, vehicle.Handle, primary, secondary);
                        vehicleData.PrimaryColor = primary.GetResult<int>();
                        vehicleData.SecondaryColor = secondary.GetResult<int>();

                        var pearlescent = new OutputArgument();
                        var wheel = new OutputArgument();
                        Function.Call(Hash.GET_VEHICLE_EXTRA_COLOURS, vehicle.Handle, pearlescent, wheel);
                        vehicleData.PearlescentColor = pearlescent.GetResult<int>();
                        vehicleData.WheelColor = wheel.GetResult<int>();

                        // Placa
                        vehicleData.LicensePlate = Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, vehicle.Handle);
                        vehicleData.LicensePlateStyle = Function.Call<int>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT_INDEX, vehicle.Handle);

                        System.Diagnostics.Debug.WriteLine($"   ✓ Modelo: {vehicleData.Model}");
                        System.Diagnostics.Debug.WriteLine($"   ✓ Posição: ({vehicleData.PositionX:F2}, {vehicleData.PositionY:F2}, {vehicleData.PositionZ:F2})");
                        System.Diagnostics.Debug.WriteLine($"   ✓ Placa: {vehicleData.LicensePlate}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("   ℹ️ Player não está em veículo");
                }

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                return vehicleData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao capturar veículo: {ex.Message}");
                return vehicleData;
            }
        }

        public static bool RestorePlayerVehicle(CharacterVehicle vehicleData)
        {
            if (vehicleData == null || !vehicleData.HasVehicle || string.IsNullOrEmpty(vehicleData.Model))
            {
                System.Diagnostics.Debug.WriteLine("ℹ️ Nenhum veículo para restaurar");
                return false;
            }

            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("🚗 RESTAURANDO VEÍCULO");
                System.Diagnostics.Debug.WriteLine($"   Modelo: {vehicleData.Model}");

                int modelHash;
                if (!int.TryParse(vehicleData.Model, out modelHash))
                {
                    modelHash = Function.Call<int>(Hash.GET_HASH_KEY, vehicleData.Model);
                }

                Model model = new Model(modelHash);
                model.Request(5000);

                if (!model.IsLoaded)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Modelo não carregou");
                    return false;
                }

                Vector3 position = new Vector3(vehicleData.PositionX, vehicleData.PositionY, vehicleData.PositionZ);

                // Criar veículo usando World.CreateVehicle (corrigido)
                Vehicle vehicle = global::GTA.World.CreateVehicle(model, position, vehicleData.Heading);

                if (vehicle == null || !vehicle.Exists())
                {
                    System.Diagnostics.Debug.WriteLine("❌ Falha ao criar veículo");
                    model.MarkAsNoLongerNeeded();
                    return false;
                }

                // Aplicar cores
                Function.Call(Hash.SET_VEHICLE_COLOURS, vehicle.Handle,
                    vehicleData.PrimaryColor, vehicleData.SecondaryColor);

                Function.Call(Hash.SET_VEHICLE_EXTRA_COLOURS, vehicle.Handle,
                    vehicleData.PearlescentColor, vehicleData.WheelColor);

                // Aplicar placa
                if (!string.IsNullOrEmpty(vehicleData.LicensePlate))
                {
                    Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, vehicle.Handle, vehicleData.LicensePlate);
                    Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT_INDEX, vehicle.Handle, vehicleData.LicensePlateStyle);
                }

                // Colocar player dentro se estava antes
                if (vehicleData.WasInVehicle)
                {
                    Function.Call(Hash.SET_PED_INTO_VEHICLE, player.Handle, vehicle.Handle, -1);
                    System.Diagnostics.Debug.WriteLine("   ✓ Player dentro do veículo");
                }

                Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, vehicle.Handle, true, true);
                vehicle.IsPersistent = true;

                model.MarkAsNoLongerNeeded();

                System.Diagnostics.Debug.WriteLine("✅ VEÍCULO RESTAURADO COM SUCESSO");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao restaurar veículo: {ex.Message}");
                return false;
            }
        }

        public static void DeletePlayerVehicle()
        {
            try
            {
                var player = Game.Player.Character;
                if (player != null && player.Exists() && player.IsInVehicle())
                {
                    var vehicle = player.CurrentVehicle;
                    if (vehicle != null && vehicle.Exists())
                    {
                        vehicle.Delete();
                        System.Diagnostics.Debug.WriteLine("🗑️ Veículo deletado");
                    }
                }
            }
            catch { }
        }
    }
}