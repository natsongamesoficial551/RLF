// ===============================
// VehicleConditionChecker.cs
// ===============================
using GTA;
using RLF.Core.Logging;

namespace RLF.GTA.Jobs.Uber.Vehicle
{
    public sealed class VehicleConditionChecker
    {
        private readonly Logger _logger;

        public VehicleConditionChecker(Logger logger)
        {
            _logger = logger;
        }

        public VehicleCondition CheckCondition(global::GTA.Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists())
                return VehicleCondition.Critical;

            float healthPercent = vehicle.HealthFloat / 1000f;

            VehicleCondition condition;

            if (healthPercent >= 0.9f)
                condition = VehicleCondition.Excellent;
            else if (healthPercent >= 0.7f)
                condition = VehicleCondition.Good;
            else if (healthPercent >= 0.5f)
                condition = VehicleCondition.Fair;
            else if (healthPercent >= 0.3f)
                condition = VehicleCondition.Poor;
            else
                condition = VehicleCondition.Critical;

            _logger.Info($"[Uber] Condição do veículo: {condition} ({healthPercent * 100:F0}%)");
            return condition;
        }

        public bool IsAcceptableCondition(VehicleCondition condition)
        {
            return condition >= VehicleCondition.Fair;
        }

        public float GetRatingMultiplier(VehicleCondition condition)
        {
            switch (condition)
            {
                case VehicleCondition.Excellent:
                    return 1.1f; // +10% rating
                case VehicleCondition.Good:
                    return 1.0f; // Normal
                case VehicleCondition.Fair:
                    return 0.95f; // -5%
                case VehicleCondition.Poor:
                    return 0.85f; // -15%
                case VehicleCondition.Critical:
                    return 0.7f; // -30%
                default:
                    return 1.0f;
            }
        }
    }
}