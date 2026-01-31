using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using RLF.Core.Logging;

namespace RLF.GTA.Jobs.Uber.Ride
{
    public sealed class RideManager
    {
        private readonly Logger _logger;
        private RideState _currentRide;
        private readonly Random _rng;

        public RideState CurrentRide => _currentRide;

        private static readonly List<Vector3> UrbanPickupPoints = new List<Vector3>
        {
            // Downtown Los Santos
            new Vector3(-73.35f, -818.57f, 326.17f),
            new Vector3(129.58f, -1045.82f, 29.19f),
            new Vector3(-258.32f, -979.45f, 31.22f),
            new Vector3(-350.83f, -789.60f, 33.97f),
            new Vector3(-564.01f, -931.39f, 23.87f),
            
            // Vinewood Hills (áreas residenciais)
            new Vector3(318.70f, -229.39f, 54.22f),
            new Vector3(-1498.88f, -378.98f, 40.16f),
            new Vector3(-1821.37f, 794.97f, 138.09f),
            
            // Vespucci / Del Perro (áreas residenciais costeiras)
            new Vector3(-1155.12f, -1425.68f, 4.95f),
            new Vector3(-1108.95f, -1639.01f, 4.40f),
            new Vector3(-2072.59f, -317.05f, 13.31f),
            new Vector3(-3087.42f, 221.09f, 14.07f),
            
            // Sandy Shores (área urbana)
            new Vector3(1961.93f, 3748.96f, 32.34f),
            new Vector3(1698.37f, 3753.45f, 34.71f),
            new Vector3(1392.13f, 3608.43f, 34.98f),
            
            // Paleto Bay (área urbana)
            new Vector3(-258.32f, 6205.40f, 31.49f),
            new Vector3(1729.21f, 6415.05f, 35.04f),
            
            // Áreas comerciais e restaurantes
            new Vector3(249.35f, -1730.79f, 29.67f),
            new Vector3(934.95f, -2352.26f, 30.55f),
            new Vector3(2558.13f, 382.93f, 108.62f),
            new Vector3(2677.12f, 3286.57f, 55.24f)
        };

        private static readonly List<Vector3> UrbanDestinationPoints = new List<Vector3>
        {
            // Downtown / Pilbox Hill
            new Vector3(-73.35f, -818.57f, 326.17f),
            new Vector3(129.58f, -1045.82f, 29.19f),
            new Vector3(-258.32f, -979.45f, 31.22f),
            
            // Vinewood
            new Vector3(318.70f, -229.39f, 54.22f),
            new Vector3(-1498.88f, -378.98f, 40.16f),
            
            // Áreas residenciais
            new Vector3(-1821.37f, 794.97f, 138.09f),
            new Vector3(-350.83f, -789.60f, 33.97f),
            new Vector3(-564.01f, -931.39f, 23.87f),
            
            // Praias e costa
            new Vector3(-1155.12f, -1425.68f, 4.95f),
            new Vector3(-1108.95f, -1639.01f, 4.40f),
            new Vector3(-2072.59f, -317.05f, 13.31f),
            
            // Sandy Shores
            new Vector3(1961.93f, 3748.96f, 32.34f),
            new Vector3(1698.37f, 3753.45f, 34.71f),
            
            // Paleto Bay
            new Vector3(-258.32f, 6205.40f, 31.49f),
            new Vector3(1729.21f, 6415.05f, 35.04f),
            
            // Aeroporto
            new Vector3(-1037.5f, -2738.4f, 13.8f),
            
            // Áreas comerciais
            new Vector3(249.35f, -1730.79f, 29.67f),
            new Vector3(934.95f, -2352.26f, 30.55f)
        };

        public RideManager(Logger logger)
        {
            _logger = logger;
            _currentRide = new RideState();
            _rng = new Random();
        }

        public RideRequest GenerateNewRequest(RideCategory category, int timeoutSeconds)
        {
            Ped player = Game.Player.Character;
            Vector3 playerPos = player.Position;

            Vector3 pickup = GetValidUrbanPickup(playerPos);
            Vector3 destination = GetValidUrbanDestination(pickup);

            var request = new RideRequest(category, pickup, destination, timeoutSeconds);

            _currentRide.Category = category;
            _currentRide.PickupLocation = pickup;
            _currentRide.DestinationLocation = destination;

            float distance = World.GetDistance(pickup, destination);
            _logger.Info($"[RideManager] Solicitação gerada: {category} | Distância: {distance:F0}m | Pickup válido: {IsValidUrbanArea(pickup)}");

            return request;
        }

        private Vector3 GetValidUrbanPickup(Vector3 playerPosition)
        {
            var validPickups = UrbanPickupPoints
                .Where(p => IsValidUrbanArea(p))
                .Where(p => World.GetDistance(p, playerPosition) >= 200f)
                .Where(p => World.GetDistance(p, playerPosition) <= 2500f)
                .ToList();

            if (validPickups.Count == 0)
            {
                _logger.Warning("[RideManager] Nenhum pickup urbano válido encontrado - usando fallback");
                validPickups = UrbanPickupPoints.Take(5).ToList();
            }

            Vector3 selected = validPickups[_rng.Next(validPickups.Count)];

            Vector3 groundPos = World.GetNextPositionOnStreet(selected);

            if (groundPos.Z < 0f || groundPos.Z > 500f)
            {
                groundPos = new Vector3(selected.X, selected.Y, World.GetGroundHeight(new Vector2(selected.X, selected.Y)));
            }

            _logger.Info($"[RideManager] Pickup selecionado: {groundPos} (Z: {groundPos.Z:F1}m)");
            return groundPos;
        }

        private Vector3 GetValidUrbanDestination(Vector3 pickupPos)
        {
            var validDestinations = UrbanDestinationPoints
                .Where(d => IsValidUrbanArea(d))
                .Where(d => World.GetDistance(d, pickupPos) >= 500f)
                .Where(d => World.GetDistance(d, pickupPos) <= 4000f)
                .ToList();

            if (validDestinations.Count == 0)
            {
                _logger.Warning("[RideManager] Nenhum destino urbano válido - usando fallback");
                validDestinations = UrbanDestinationPoints.Take(5).ToList();
            }

            Vector3 selected = validDestinations[_rng.Next(validDestinations.Count)];

            Vector3 groundPos = World.GetNextPositionOnStreet(selected);

            if (groundPos.Z < 0f || groundPos.Z > 500f)
            {
                groundPos = new Vector3(selected.X, selected.Y, World.GetGroundHeight(new Vector2(selected.X, selected.Y)));
            }

            _logger.Info($"[RideManager] Destino selecionado: {groundPos} (Z: {groundPos.Z:F1}m)");
            return groundPos;
        }

        private bool IsValidUrbanArea(Vector3 position)
        {
            float z = position.Z;

            if (z < 0f || z > 300f)
            {
                _logger.Warning($"[RideManager] Posição rejeitada - altitude inválida: {z:F1}m");
                return false;
            }

            return true;
        }

        public void AcceptRequest()
        {
            _currentRide.IsActive = true;
            _currentRide.StartedAt = DateTime.UtcNow;
            _logger.Info("[RideManager] Solicitação aceita");
        }

        public void CancelRequest()
        {
            _currentRide.Reset();
            _logger.Info("[RideManager] Solicitação cancelada");
        }

        public void PassengerPickedUp()
        {
            _currentRide.PassengerOnBoard = true;
            _logger.Info("[RideManager] Passageiro embarcado");
        }

        public void UpdateRideMetrics(float distance, int timeSeconds)
        {
            _currentRide.DistanceTraveled = distance;
            _currentRide.TimeElapsedSeconds = timeSeconds;
        }

        public void RecordCrash()
        {
            _currentRide.CrashCount++;
            _logger.Warning($"[RideManager] Batida registrada - Total: {_currentRide.CrashCount}");
        }

        public void CompleteRide()
        {
            _logger.Info($"[RideManager] Corrida concluída - Distância: {_currentRide.DistanceTraveled:F0}m | Tempo: {_currentRide.TimeElapsedSeconds}s");
            _currentRide.Reset();
        }
    }
}