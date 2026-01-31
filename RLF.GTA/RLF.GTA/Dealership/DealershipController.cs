using GTA;
using GTA.Math;
using RLF.Core;
using RLF.Core.Economy.Transactions;
using RLF.Core.Identity;
using RLF.Core.Identity.Enums;
using RLF.Core.Vehicles;
using RLF.GTA.CoreIntegration;
using RLF.GTA.CoreIntegration;
using RLF.GTA.Vehicles;
using System;
using System.Windows.Forms;


namespace RLF.GTA.Dealership
{
    public sealed class DealershipController : Script
    {
        // 📍 Porta externa da Premium Deluxe Motorsport
        private readonly Vector3 _dealershipDoor =
            new Vector3(-61.445f, -1093.042f, 26.502f);

        private Blip _dealershipBlip;

        private DealershipMenu _menu;
        private bool _menuActive;

        public DealershipController()
        {
            Tick += OnTick;
            Aborted += OnAborted;

            _menu = new DealershipMenu();
            CreateBlip();
        }

        // ===============================
        // 🗺️ BLIP DA CONCESSIONÁRIA
        // ===============================
        private void CreateBlip()
        {
            _dealershipBlip = World.CreateBlip(_dealershipDoor);
            _dealershipBlip.Sprite = BlipSprite.PersonalVehicleCar;
            _dealershipBlip.Color = BlipColor.Red;
            _dealershipBlip.Scale = 0.9f;
            _dealershipBlip.Name = "Concessionária";
            _dealershipBlip.IsShortRange = true;
        }

        // ===============================
        // 🔁 TICK
        // ===============================
        private void OnTick(object sender, EventArgs e)
        {
            Ped player = Game.Player.Character;

            // Menu ativo
            if (_menuActive)
            {
                try
                {
                    var selected = _menu.Tick();

                    if (selected != null)
                    {
                        BuyVehicle(selected);
                        _menuActive = false;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Segurança: fecha menu se perder estado
                    _menuActive = false;
                    _menu.Reset();
                }

                return;
            }


            float dist = player.Position.DistanceTo(_dealershipDoor);

            if (dist < 2.0f)
            {
                global::GTA.UI.Screen.ShowHelpTextThisFrame(
                    "Pressione ~INPUT_CONTEXT~ (E) para acessar a concessionária"
                );

                if (Game.IsKeyPressed(Keys.E))
                {
                    TryOpenDealership();
                }
            }
        }

        // ===============================
        // 🚗 ABRIR MENU
        // ===============================
        private void TryOpenDealership()
        {
            var docSystem = RLFCore.Instance.Systems.Get("DocumentSystem")
                as DocumentSystem;

            if (docSystem == null ||
                !docSystem.HasValidLicense(LicenseType.DriverCar))
            {
                global::GTA.UI.Notification.Show(
                    "❌ Você precisa de uma CNH válida para comprar veículos"
                );
                return;
            }

            if (!EconomyBridge.IsReady)
            {
                global::GTA.UI.Notification.Show(
                    "❌ Sistema econômico ainda não foi inicializado"
                );
                return;
            }

            _menu.Reset();
            _menuActive = true;
        }

        private string GetPlateText(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists())
                return string.Empty;

            // Native: pega a placa do jeito compatível com versões antigas
            return global::GTA.Native.Function.Call<string>(
                global::GTA.Native.Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,
                vehicle.Handle
            );
        }


        // ===============================
        // 💰 COMPRA DE VEÍCULO (CORE REAL)
        // ===============================
        private void BuyVehicle(DealershipCatalog.VehicleEntry entry)
        {
            var economy = EconomyBridge.Current;
            if (economy == null)
            {
                global::GTA.UI.Notification.Show("❌ Sistema econômico indisponível");
                return;
            }

            if (economy.Wallet.Balance < entry.Price)
            {
                global::GTA.UI.Notification.Show("❌ Dinheiro insuficiente");
                return;
            }

            bool applied = economy.Wallet.ApplyTransaction(
                new EconomyTransaction(
                    -entry.Price,
                    TransactionType.Expense,
                    TransactionLegality.Legal,
                    TransactionOrigin.LivingCost,
                    $"Compra de veículo: {entry.Name}"
                )
            );

            if (!applied)
            {
                global::GTA.UI.Notification.Show("❌ Falha ao processar pagamento");
                return;
            }

            Ped player = Game.Player.Character;
            Vector3 spawn = player.Position + player.ForwardVector * 5f;

            Vehicle vehicle = World.CreateVehicle(entry.Model, spawn);
            if (vehicle == null || !vehicle.Exists())
            {
                global::GTA.UI.Notification.Show("❌ Falha ao criar o veículo");
                return;
            }

            player.SetIntoVehicle(vehicle, VehicleSeat.Driver);

            vehicle.IsPersistent = true;
            vehicle.PreviouslyOwnedByPlayer = true;

            var ownership = VehicleOwnershipBridge.Current;
            if (ownership != null)
            {
                var data = new VehicleData
                {
                    Id = Guid.NewGuid(), // 🔥 FIX CRÍTICO
                    Model = (int)entry.Model,
                    Plate = GetPlateText(vehicle),
                    PrimaryColor = (int)vehicle.Mods.PrimaryColor,
                    SecondaryColor = (int)vehicle.Mods.SecondaryColor,
                    Heading = vehicle.Heading,
                    State = VehicleState.World
                };

                ownership.RegisterVehicle(data);
                ownership.Save(); // 🔥 GARANTE PERSISTÊNCIA
            }

            global::GTA.UI.Notification.Show(
                $"🚗 {entry.Name} comprado com sucesso!\n-${entry.Price:N0}"
            );
        }


        private void OnAborted(object sender, EventArgs e)
        {
            try { _dealershipBlip?.Delete(); } catch { }
            _menuActive = false;
        }
    }
}
