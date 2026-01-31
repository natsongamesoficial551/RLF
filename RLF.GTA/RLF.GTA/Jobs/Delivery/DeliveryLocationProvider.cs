using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;

namespace RLF.GTA.Jobs.Delivery
{
    public sealed class DeliveryLocationProvider
    {
        private readonly Random _rng;
        private readonly List<Vector3> _pickupLocations;
        private readonly List<Vector3> _deliveryAddresses;

        public DeliveryLocationProvider()
        {
            _rng = new Random();
            _pickupLocations = new List<Vector3>(DeliveryConfig.PickupLocations);
            _deliveryAddresses = new List<Vector3>(DeliveryConfig.DeliveryAddresses);
        }

        // ✅ NOVO: Escolhe o pickup mais PRÓXIMO do jogador (dentro de um raio)
        public Vector3 GetRandomPickupLocation()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
            {
                // Fallback: retorna aleatório
                return _pickupLocations[_rng.Next(_pickupLocations.Count)];
            }

            Vector3 playerPos = player.Position;

            // Busca locais dentro de 1500m do jogador
            var nearbyLocations = _pickupLocations
                .Where(loc => World.GetDistance(playerPos, loc) <= 1500f)
                .ToList();

            // Se não encontrou nenhum próximo, busca os 3 mais próximos
            if (nearbyLocations.Count == 0)
            {
                nearbyLocations = _pickupLocations
                    .OrderBy(loc => World.GetDistance(playerPos, loc))
                    .Take(3)
                    .ToList();
            }

            // Escolhe um aleatório entre os próximos
            Vector3 selected = nearbyLocations[_rng.Next(nearbyLocations.Count)];

            return selected;
        }

        // ✅ MELHORADO: Escolhe endereço de entrega próximo ao pickup
        public Vector3 GetRandomDeliveryAddress()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
            {
                return _deliveryAddresses[_rng.Next(_deliveryAddresses.Count)];
            }

            Vector3 playerPos = player.Position;

            // Busca destinos entre 300m e 2000m do jogador
            var validDestinations = _deliveryAddresses
                .Where(dest =>
                {
                    float distance = World.GetDistance(playerPos, dest);
                    return distance >= 300f && distance <= 2000f;
                })
                .ToList();

            // Se não encontrou nenhum válido, busca os mais próximos
            if (validDestinations.Count == 0)
            {
                validDestinations = _deliveryAddresses
                    .OrderBy(dest => World.GetDistance(playerPos, dest))
                    .Skip(3)  // Pula os 3 mais próximos (muito perto)
                    .Take(10) // Pega os próximos 10
                    .ToList();
            }

            // Se ainda não tem nenhum, usa qualquer um
            if (validDestinations.Count == 0)
            {
                return _deliveryAddresses[_rng.Next(_deliveryAddresses.Count)];
            }

            return validDestinations[_rng.Next(validDestinations.Count)];
        }
    }
}