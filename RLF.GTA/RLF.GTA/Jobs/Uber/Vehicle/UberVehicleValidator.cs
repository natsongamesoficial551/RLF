// ===============================
// UberVehicleValidator.cs
// ===============================
using GTA;
using RLF.Core.Logging;

namespace RLF.GTA.Jobs.Uber.Vehicle
{
    public sealed class UberVehicleValidator
    {
        private readonly Logger _logger;

        public UberVehicleValidator(Logger logger)
        {
            _logger = logger;
        }

        public bool IsValidUberVehicle(global::GTA.Vehicle vehicle, out string reason)
        {
            reason = string.Empty;

            if (vehicle == null || !vehicle.Exists())
            {
                reason = "Veículo inválido";
                return false;
            }

            // Apenas carros
            if (!vehicle.Model.IsCar)
            {
                reason = "Uber funciona apenas com carros";
                return false;
            }

            // Não pode ser roubado
            if (vehicle.IsStolen)
            {
                reason = "Veículo roubado não permitido";
                return false;
            }

            // Deve estar em boas condições (>50% saúde)
            if (vehicle.HealthFloat < 500f)
            {
                reason = "Veículo em más condições";
                return false;
            }

            _logger.Info($"[Uber] Veículo validado: {vehicle.Model.Hash}");
            return true;
        }

        public VehicleCondition CheckCondition(global::GTA.Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists())
                return VehicleCondition.Critical;

            float healthPercent = vehicle.HealthFloat / 1000f;

            if (healthPercent >= 0.8f)
                return VehicleCondition.Excellent;
            else if (healthPercent >= 0.6f)
                return VehicleCondition.Good;
            else if (healthPercent >= 0.4f)
                return VehicleCondition.Fair;
            else if (healthPercent >= 0.2f)
                return VehicleCondition.Poor;
            else
                return VehicleCondition.Critical;
        }
    }

    public enum VehicleCondition
    {
        Excellent,
        Good,
        Fair,
        Poor,
        Critical
    }
}