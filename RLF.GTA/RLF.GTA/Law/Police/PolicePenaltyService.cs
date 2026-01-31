using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using RLF.Core;
using RLF.Core.Economy;
using RLF.Core.Economy.Transactions;
using RLF.Core.Identity;
using RLF.Core.Identity.Enums;
using RLF.Core.Vehicles;
using RLF.GTA.Vehicles;
using System;
using System.Linq;

namespace RLF.GTA.Law.Police
{
    public static class PolicePenaltyService
    {
        private const int FINE_WEAPON = 6000;
        private const int FINE_NO_LICENSE = 3000;
        private const int FINE_EXPIRED_LICENSE = 2000;
        private const int FINE_SUSPENDED_LICENSE = 5000;

        public static bool ShouldImpound(PoliceTarget target)
        {
            if (!Validate(target))
                return false;

            var docSystem = GetDocumentSystem();
            if (docSystem == null)
                return false;

            var player = target.Ped;
            bool shouldImpound = false;

            bool hasCNH = docSystem.HasValidLicense(LicenseType.DriverCar);

            if (!hasCNH)
            {
                shouldImpound = true;

                docSystem.DetectViolation(
                    ViolationType.DrivingWithoutLicense,
                    ViolationSeverity.Critical,
                    "Condução sem CNH válida detectada pela polícia"
                );
            }

            bool hasWeapon = player.Weapons.Current != null &&
                             player.Weapons.Current.Hash != WeaponHash.Unarmed;

            if (hasWeapon)
            {
                bool hasPermit = docSystem.HasValidLicense(LicenseType.WeaponPermit);

                if (!hasPermit)
                {
                    shouldImpound = true;

                    docSystem.DetectViolation(
                        ViolationType.WeaponWithoutPermit,
                        ViolationSeverity.Critical,
                        "Porte de arma ilegal detectado pela polícia"
                    );
                }
            }

            return shouldImpound;
        }

        public static bool MarkVehicleImpoundedKeepWorld(Vehicle vehicle)
        {
            var ownership = VehicleOwnershipBridge.Current;
            if (ownership == null || vehicle == null || !vehicle.Exists())
                return false;

            string plate = GetPlateTextSafe(vehicle);
            string normalized = NormalizePlate(plate);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            VehicleData data = ownership.Vehicles
                .FirstOrDefault(v =>
                    v != null &&
                    v.State == VehicleState.World &&
                    NormalizePlate(v.Plate) == normalized
                );

            if (data == null)
                return false;

            if (data.Id != System.Guid.Empty &&
                VehicleGarageManualStore.StoringVehicleIds.Contains(data.Id))
                return false;

            data.State = VehicleState.Impound;
            ownership.Save();

            Notification.Show($"~r~Veículo {plate} apreendido!");
            return true;
        }

        public static void ApplyAllPenalties(PoliceTarget target)
        {
            if (!Validate(target))
                return;

            var docSystem = GetDocumentSystem();
            if (docSystem == null)
                return;

            var player = target.Ped;

            if (!docSystem.HasValidLicense(LicenseType.DriverCar))
            {
                ApplyFine(FINE_NO_LICENSE, "Condução sem CNH válida");
                Notification.Show("~r~Multa: Condução sem CNH válida");
            }

            if (player.Weapons.Current != null &&
                player.Weapons.Current.Hash != WeaponHash.Unarmed)
            {
                if (!docSystem.HasValidLicense(LicenseType.WeaponPermit))
                {
                    ApplyFine(FINE_WEAPON, "Porte de arma sem autorização");
                    Notification.Show("~r~Multa: Porte ilegal de arma");

                    try
                    {
                        player.Weapons.RemoveAll();
                        Notification.Show("~o~Armas confiscadas");
                    }
                    catch { }
                }
            }
        }

        public static void ApplyWeaponPenaltyOnly(PoliceTarget target)
        {
            if (!Validate(target))
                return;

            var docSystem = GetDocumentSystem();
            if (docSystem == null)
                return;

            var player = target.Ped;

            if (player.Weapons.Current != null &&
                player.Weapons.Current.Hash != WeaponHash.Unarmed)
            {
                if (!docSystem.HasValidLicense(LicenseType.WeaponPermit))
                {
                    ApplyFine(FINE_WEAPON, "Porte de arma sem autorização");

                    try
                    {
                        player.Weapons.RemoveAll();
                        Notification.Show("~o~Armas confiscadas");
                    }
                    catch { }
                }
            }
        }

        public static void ApplyNoLicensePenaltyOnly(PoliceTarget target)
        {
            if (!Validate(target))
                return;

            var docSystem = GetDocumentSystem();
            if (docSystem == null)
                return;

            if (!docSystem.HasValidLicense(LicenseType.DriverCar))
            {
                ApplyFine(FINE_NO_LICENSE, "Condução sem CNH válida");
            }
        }

        public static void FinalizeWorldVehicleDelete(Vehicle vehicle)
        {
            try
            {
                if (vehicle == null || !vehicle.Exists())
                    return;

                vehicle.IsPersistent = false;
                vehicle.MarkAsNoLongerNeeded();

                vehicle.Position = new Vector3(10000f, 10000f, -200f);
                vehicle.Velocity = Vector3.Zero;

                Script.Yield();

                vehicle.Delete();
            }
            catch { }
        }

        private static bool Validate(PoliceTarget target)
        {
            return target != null &&
                   target.Ped != null && target.Ped.Exists() &&
                   target.Vehicle != null && target.Vehicle.Exists();
        }

        private static DocumentSystem GetDocumentSystem()
        {
            try
            {
                var core = RLFCore.Instance;
                if (core == null)
                    return null;

                var docSystem = core.Systems?.Get("DocumentSystem") as DocumentSystem;
                return docSystem;
            }
            catch
            {
                return null;
            }
        }

        private static void ApplyFine(int amount, string description)
        {
            EconomySystem economy = RLFCore.Instance?.Economy;
            if (economy == null || economy.Wallet == null)
                return;

            economy.Wallet.ApplyTransaction(
                new EconomyTransaction(
                    amount: -amount,
                    type: TransactionType.Fine,
                    legality: TransactionLegality.Legal,
                    origin: TransactionOrigin.Fine,
                    description: description
                )
            );
        }

        private static string GetPlateTextSafe(Vehicle vehicle)
        {
            try
            {
                return Function.Call<string>(
                    Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,
                    vehicle.Handle
                );
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizePlate(string plate)
        {
            return (plate ?? string.Empty).Trim().Replace(" ", "");
        }
    }
}