// ===============================
// PassengerBehavior.cs
// ===============================
using System;
using RLF.Core.Logging;

namespace RLF.GTA.Jobs.Uber.Passenger
{
    public sealed class PassengerBehavior
    {
        private readonly Logger _logger;
        private readonly Random _rng;

        public PassengerEventType CurrentBehavior { get; private set; }

        public PassengerBehavior(Logger logger)
        {
            _logger = logger;
            _rng = new Random();
            CurrentBehavior = PassengerEventType.None;
        }

        public PassengerEventType RollBehavior(float driverRating)
        {
            float roll = (float)_rng.NextDouble();

            // Motoristas bem avaliados têm menos passageiros problemáticos
            float problemChance = 0.30f - (driverRating - 3.0f) * 0.05f;
            problemChance = Math.Max(0.10f, Math.Min(0.40f, problemChance));

            if (roll < problemChance)
            {
                // Passageiro problemático
                int problem = _rng.Next(0, 5);
                switch (problem)
                {
                    case 0:
                        CurrentBehavior = PassengerEventType.Complaining;
                        _logger.Info("[Uber] Passageiro: Reclamando");
                        break;
                    case 1:
                        CurrentBehavior = PassengerEventType.Drunk;
                        _logger.Info("[Uber] Passageiro: Bêbado");
                        break;
                    case 2:
                        CurrentBehavior = PassengerEventType.RequestSpeedUp;
                        _logger.Info("[Uber] Passageiro: Pedindo para correr");
                        break;
                    case 3:
                        CurrentBehavior = PassengerEventType.Impatient;
                        _logger.Info("[Uber] Passageiro: Impaciente");
                        break;
                    case 4:
                        CurrentBehavior = PassengerEventType.TryExitEarly;
                        _logger.Info("[Uber] Passageiro: Tentando sair antes do destino");
                        break;
                }
            }
            else if (roll > 0.85f)
            {
                // Passageiro amigável
                CurrentBehavior = PassengerEventType.Friendly;
                _logger.Info("[Uber] Passageiro: Amigável");
            }
            else
            {
                // Passageiro normal
                CurrentBehavior = PassengerEventType.OnPhone;
            }

            return CurrentBehavior;
        }

        public float GetRatingImpact()
        {
            switch (CurrentBehavior)
            {
                case PassengerEventType.Complaining:
                    return -0.3f;
                case PassengerEventType.Drunk:
                    return -0.2f;
                case PassengerEventType.Impatient:
                    return -0.1f;
                case PassengerEventType.Friendly:
                    return 0.2f;
                default:
                    return 0f;
            }
        }

        public float GetTipMultiplier()
        {
            switch (CurrentBehavior)
            {
                case PassengerEventType.Friendly:
                    return 1.5f; // +50% gorjeta
                case PassengerEventType.Complaining:
                case PassengerEventType.Drunk:
                    return 0.5f; // -50% gorjeta
                case PassengerEventType.Impatient:
                    return 0.7f; // -30% gorjeta
                default:
                    return 1.0f;
            }
        }
    }
}