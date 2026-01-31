// ===============================
// RandomEventSystem.cs
// ===============================
using GTA;
using RLF.Core.Logging;
using System;

namespace RLF.GTA.Jobs.Uber.Events
{
    public sealed class RandomEventSystem
    {
        private readonly Logger _logger;
        private readonly Random _rng;

        // Chances base de eventos (0.0 a 1.0)
        private const float TrafficJamChance = 0.15f;
        private const float AccidentChance = 0.08f;
        private const float VIPChance = 0.05f;
        private const float BadWeatherChance = 0.12f;

        public RandomEventSystem(Logger logger)
        {
            _logger = logger;
            _rng = new Random();
        }

        public EventType RollEvent(bool isNightTime)
        {
            float roll = (float)_rng.NextDouble();

            // Eventos noturnos têm chance aumentada
            if (isNightTime && roll < 0.20f)
            {
                _logger.Info("[Uber] Evento: Corrida noturna perigosa");
                return EventType.NightRide;
            }

            // Eventos diurnos
            if (roll < TrafficJamChance)
            {
                _logger.Info("[Uber] Evento: Trânsito intenso");
                return EventType.TrafficJam;
            }
            else if (roll < TrafficJamChance + AccidentChance)
            {
                _logger.Info("[Uber] Evento: Acidente no trajeto");
                return EventType.Accident;
            }
            else if (roll < TrafficJamChance + AccidentChance + VIPChance)
            {
                _logger.Info("[Uber] Evento: Passageiro VIP");
                return EventType.VIPPassenger;
            }
            else if (roll < TrafficJamChance + AccidentChance + VIPChance + BadWeatherChance)
            {
                _logger.Info("[Uber] Evento: Clima ruim");
                return EventType.BadWeather;
            }

            return EventType.None;
        }

        public decimal GetEventPaymentMultiplier(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.VIPPassenger:
                    return 1.5m; // +50%
                case EventType.UrgentRide:
                    return 1.3m; // +30%
                case EventType.NightRide:
                    return 1.25m; // +25%
                case EventType.TrafficJam:
                    return 1.1m; // +10% (compensação)
                case EventType.BadWeather:
                    return 1.15m; // +15%
                default:
                    return 1.0m;
            }
        }

        public float GetEventRatingImpact(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.TrafficJam:
                    return -0.1f; // Pequena penalidade (não é culpa do motorista)
                case EventType.Accident:
                    return -0.2f; // Atraso significativo
                case EventType.VIPPassenger:
                    return 0.3f; // Bônus se bem executado
                default:
                    return 0f;
            }
        }
    }
}