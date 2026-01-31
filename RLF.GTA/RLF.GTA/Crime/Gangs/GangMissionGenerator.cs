using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core.Gangs;
using RLF.Core.Gangs.Missions;

namespace RLF.GTA.Gangs
{
    /// <summary>
    /// Gera missões dinâmicas para gangues
    /// SISTEMA REALISTA: Missões aparecem naturalmente, não são spam
    /// </summary>
    public class GangMissionGenerator
    {
        private readonly GangSystem _gangSystem;
        private readonly PlayerGangMembership _playerMembership;
        private readonly Random _random;

        private List<GangMission> _availableMissions;
        private DateTime _lastMissionGeneration;
        
        private const float MISSION_GENERATION_INTERVAL_HOURS = 2.0f; // Nova missão a cada 2h de jogo
        private const int MAX_AVAILABLE_MISSIONS = 3; // Máximo 3 missões disponíveis por vez

        // DEBUG
        private bool _debugEnabled = true;

        public GangMissionGenerator(GangSystem gangSystem, PlayerGangMembership playerMembership)
        {
            _gangSystem = gangSystem ?? throw new ArgumentNullException(nameof(gangSystem));
            _playerMembership = playerMembership ?? throw new ArgumentNullException(nameof(playerMembership));
            
            _random = new Random();
            _availableMissions = new List<GangMission>();
            _lastMissionGeneration = DateTime.Now.AddHours(-MISSION_GENERATION_INTERVAL_HOURS); // Permite gerar logo de início
        }

        public void Update(float deltaTime)
        {
            // Só gera missões se player estiver em gangue
            if (!_playerMembership.IsInGang()) return;

            // Verifica se é hora de gerar nova missão
            TimeSpan timeSinceLastGen = DateTime.Now - _lastMissionGeneration;
            if (timeSinceLastGen.TotalHours >= MISSION_GENERATION_INTERVAL_HOURS)
            {
                if (_availableMissions.Count < MAX_AVAILABLE_MISSIONS)
                {
                    GenerateNewMission();
                    _lastMissionGeneration = DateTime.Now;
                }
            }

            // Remove missões expiradas (24h sem aceitar)
            CleanupExpiredMissions();
        }

        private void GenerateNewMission()
        {
            if (!_playerMembership.IsInGang()) return;

            GangType playerGang = _playerMembership.CurrentGang.Value;
            GangRank playerRank = _playerMembership.Rank;

            // Missões disponíveis baseadas no rank
            List<GangMissionType> availableTypes = GetAvailableMissionTypes(playerRank);
            if (availableTypes.Count == 0) return;

            // Escolhe tipo aleatório
            GangMissionType type = availableTypes[_random.Next(availableTypes.Count)];

            // Cria missão
            GangMission mission = CreateMissionByType(type, playerGang);
            if (mission != null)
            {
                _availableMissions.Add(mission);

                if (_debugEnabled)
                {
                    global::GTA.UI.Notification.Show($"~g~[DEBUG] Nova missão gerada: {mission.Name}");
                }
            }
        }

        private List<GangMissionType> GetAvailableMissionTypes(GangRank rank)
        {
            List<GangMissionType> types = new List<GangMissionType>();

            // Todas ranks podem fazer roubos básicos
            types.Add(GangMissionType.StoreRobbery);

            if (rank >= GangRank.Member)
            {
                types.Add(GangMissionType.DrugDelivery);
                types.Add(GangMissionType.CollectProtectionMoney);
            }

            if (rank >= GangRank.Veteran)
            {
                types.Add(GangMissionType.Kidnapping);
                types.Add(GangMissionType.VehicleTheft);
                types.Add(GangMissionType.Intimidation);
            }

            if (rank >= GangRank.Lieutenant)
            {
                types.Add(GangMissionType.TerritoryTakeover);
                types.Add(GangMissionType.Ambush);
                types.Add(GangMissionType.HitContract);
            }

            return types;
        }

        private GangMission CreateMissionByType(GangMissionType type, GangType gang)
        {
            switch (type)
            {
                case GangMissionType.StoreRobbery:
                    return CreateStoreRobberyMission(gang);

                case GangMissionType.Kidnapping:
                    return CreateKidnappingMission(gang);

                case GangMissionType.CollectProtectionMoney:
                    return CreateProtectionMoneyMission(gang);

                case GangMissionType.VehicleTheft:
                    return CreateVehicleTheftMission(gang);

                case GangMissionType.TerritoryTakeover:
                    return CreateTerritoryTakeoverMission(gang);

                case GangMissionType.DrugDelivery:
                    return CreateDrugDeliveryMission(gang);

                case GangMissionType.Intimidation:
                    return CreateIntimidationMission(gang);

                case GangMissionType.Ambush:
                    return CreateAmbushMission(gang);

                case GangMissionType.HitContract:
                    return CreateHitContractMission(gang);

                default:
                    return null;
            }
        }

        // ===== CRIAÇÃO DE MISSÕES ESPECÍFICAS =====

        private GangMission CreateStoreRobberyMission(GangType gang)
        {
            // Escolhe loja 24/7 aleatória
            Vector3[] stores = new Vector3[]
            {
                new Vector3(24.47f, -1346.19f, 29.5f),        // Innocence Blvd
                new Vector3(-3039.54f, 584.38f, 7.91f),       // Clinton Ave
                new Vector3(1728.78f, 6415.76f, 35.04f),      // Barbareno Rd
                new Vector3(1697.87f, 4923.42f, 42.06f),      // Route 68
                new Vector3(372.72f, 326.89f, 103.57f)        // Mirror Park
            };

            Vector3 targetStore = stores[_random.Next(stores.Length)];

            GangMission mission = new GangMission
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Roubo de Loja 24/7",
                Description = "Roube uma loja 24/7 com sua gangue. Intimide o atendente e pegue o dinheiro.",
                Type = GangMissionType.StoreRobbery,
                AssignedGang = gang,
                TargetX = targetStore.X,
                TargetY = targetStore.Y,
                TargetZ = targetStore.Z,
                TargetRadius = 50f,
                RequiresGangMembers = true,
                MinimumGangMembers = 1,
                MaximumGangMembers = 3,
                MoneyReward = _random.Next(500, 1500),
                ReputationReward = 5,
                TimeLimitMinutes = 15,
                DifficultyLevel = 1,
                RequiresWeapons = true
            };

            mission.AddObjective("Vá até a loja 24/7", 1);
            mission.AddObjective("Intimide o atendente", 1);
            mission.AddObjective("Pegue o dinheiro", 1);
            mission.AddObjective("Escape da área", 1);

            return mission;
        }

        private GangMission CreateKidnappingMission(GangType gang)
        {
            // Locais onde ricos aparecem
            Vector3[] richAreas = new Vector3[]
            {
                new Vector3(-800f, -220f, 37f),    // Rockford Hills
                new Vector3(100f, 550f, 175f),     // Vinewood Hills
                new Vector3(-1500f, -400f, 40f),   // Del Perro
                new Vector3(1400f, -1500f, 60f)    // El Burro Heights (businessmen)
            };

            Vector3 targetArea = richAreas[_random.Next(richAreas.Length)];

            GangMission mission = new GangMission
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Sequestro de Pessoa Rica",
                Description = "Sequestre uma pessoa rica e leve-a até o ponto de resgate. Não a mate!",
                Type = GangMissionType.Kidnapping,
                AssignedGang = gang,
                TargetX = targetArea.X,
                TargetY = targetArea.Y,
                TargetZ = targetArea.Z,
                TargetRadius = 100f,
                RequiresGangMembers = true,
                MinimumGangMembers = 2,
                MaximumGangMembers = 4,
                MoneyReward = _random.Next(3000, 8000),
                ReputationReward = 15,
                TimeLimitMinutes = 25,
                DifficultyLevel = 3,
                RequiresWeapons = true,
                RequiresVehicle = true,
                TargetPedModel = "a_m_m_business_01" // Businessman rico
            };

            mission.AddObjective("Encontre uma pessoa rica", 1);
            mission.AddObjective("Capture-a (não mate!)", 1);
            mission.AddObjective("Leve ao ponto de resgate", 1);

            return mission;
        }

        private GangMission CreateProtectionMoneyMission(GangType gang)
        {
            // Lojas pequenas que pagam proteção
            Vector3[] businesses = new Vector3[]
            {
                new Vector3(-1486.29f, -377.68f, 40.16f),  // Rob's Liquor
                new Vector3(1134.15f, -982.38f, 46.42f),   // El Rancho Liquor
                new Vector3(-1221.42f, -908.54f, 12.33f)   // San Andreas Liquor
            };

            Vector3 target = businesses[_random.Next(businesses.Length)];

            GangMission mission = new GangMission
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Coletar Dinheiro de Proteção",
                Description = "Colete o pagamento de proteção de um estabelecimento local.",
                Type = GangMissionType.CollectProtectionMoney,
                AssignedGang = gang,
                TargetX = target.X,
                TargetY = target.Y,
                TargetZ = target.Z,
                TargetRadius = 30f,
                RequiresGangMembers = false,
                MoneyReward = _random.Next(300, 800),
                ReputationReward = 3,
                TimeLimitMinutes = 10,
                DifficultyLevel = 1,
                RequiresWeapons = false
            };

            mission.AddObjective("Vá até o estabelecimento", 1);
            mission.AddObjective("Colete o pagamento", 1);

            return mission;
        }

        private GangMission CreateVehicleTheftMission(GangType gang)
        {
            // Veículos de luxo para roubar
            string[] luxuryCars = new string[]
            {
                "SCHAFTER2",
                "COGNOSCENTI",
                "DUBSTA2",
                "BALLER",
                "XLS"
            };

            string targetVehicle = luxuryCars[_random.Next(luxuryCars.Length)];

            // Área rica onde veículo aparece
            Vector3 searchArea = new Vector3(-800f, -200f, 37f); // Rockford Hills

            GangMission mission = new GangMission
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Roubo de Veículo de Luxo",
                Description = $"Roube um {targetVehicle} e leve ao esconderijo.",
                Type = GangMissionType.VehicleTheft,
                AssignedGang = gang,
                TargetX = searchArea.X,
                TargetY = searchArea.Y,
                TargetZ = searchArea.Z,
                TargetRadius = 200f,
                MoneyReward = _random.Next(2000, 5000),
                ReputationReward = 10,
                TimeLimitMinutes = 20,
                DifficultyLevel = 2,
                TargetVehicleModel = targetVehicle
            };

            mission.AddObjective($"Encontre um {targetVehicle}", 1);
            mission.AddObjective("Roube o veículo", 1);
            mission.AddObjective("Leve ao esconderijo", 1);

            return mission;
        }

        private GangMission CreateTerritoryTakeoverMission(GangType gang)
        {
            // Territórios neutros ou inimigos
            var allTerritories = TerritoryDatabase.GetAllTerritories();
            var availableTerritories = allTerritories
                .Where(t => !t.ControllingGang.HasValue || 
                           (t.ControllingGang.HasValue && _gangSystem.GetGang(gang).IsEnemy(t.ControllingGang.Value)))
                .ToList();

            if (availableTerritories.Count == 0) return null;

            var targetTerritory = availableTerritories[_random.Next(availableTerritories.Count)];

            GangMission mission = new GangMission
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Invasão: {targetTerritory.Name}",
                Description = $"Capture o território {targetTerritory.Name} para sua gangue.",
                Type = GangMissionType.TerritoryTakeover,
                AssignedGang = gang,
                TargetX = targetTerritory.CenterX,
                TargetY = targetTerritory.CenterY,
                TargetZ = targetTerritory.CenterZ,
                TargetRadius = targetTerritory.Radius,
                TargetTerritoryId = targetTerritory.Id,
                TargetGang = targetTerritory.ControllingGang,
                RequiresGangMembers = true,
                MinimumGangMembers = 3,
                MaximumGangMembers = 6,
                MoneyReward = 5000,
                ReputationReward = 25,
                InfluenceReward = 50,
                TimeLimitMinutes = 30,
                DifficultyLevel = 4,
                RequiresWeapons = true
            };

            mission.AddObjective("Entre no território inimigo", 1);
            mission.AddObjective("Elimine membros inimigos", 5);
            mission.AddObjective("Capture o território", 1);

            return mission;
        }

        private GangMission CreateDrugDeliveryMission(GangType gang)
        {
            Vector3 pickup = new Vector3(1200f, 3600f, 38f); // Sandy Shores industrial
            Vector3 delivery = new Vector3(-1100f, -1550f, 4f); // La Puerta

            GangMission mission = new GangMission
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Entrega de Mercadoria",
                Description = "Pegue a mercadoria e entregue no ponto marcado. Evite a polícia.",
                Type = GangMissionType.DrugDelivery,
                AssignedGang = gang,
                StartX = pickup.X,
                StartY = pickup.Y,
                StartZ = pickup.Z,
                TargetX = delivery.X,
                TargetY = delivery.Y,
                TargetZ = delivery.Z,
                TargetRadius = 50f,
                MoneyReward = _random.Next(1500, 3000),
                ReputationReward = 8,
                TimeLimitMinutes = 15,
                DifficultyLevel = 2,
                RequiresVehicle = true
            };

            mission.AddObjective("Pegue a mercadoria", 1);
            mission.AddObjective("Entregue no destino", 1);

            return mission;
        }

        private GangMission CreateIntimidationMission(GangType gang)
        {
            Vector3 target = new Vector3(-50f, -1100f, 26f); // Downtown business

            GangMission mission = new GangMission
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Intimidação",
                Description = "Intimide um comerciante que está resistindo pagar proteção.",
                Type = GangMissionType.Intimidation,
                AssignedGang = gang,
                TargetX = target.X,
                TargetY = target.Y,
                TargetZ = target.Z,
                TargetRadius = 30f,
                RequiresGangMembers = true,
                MinimumGangMembers = 1,
                MaximumGangMembers = 2,
                MoneyReward = _random.Next(400, 1000),
                ReputationReward = 5,
                TimeLimitMinutes = 10,
                DifficultyLevel = 1,
                RequiresWeapons = true
            };

            mission.AddObjective("Encontre o comerciante", 1);
            mission.AddObjective("Intimide-o (atire perto, não mate)", 1);

            return mission;
        }

        private GangMission CreateAmbushMission(GangType gang)
        {
            // Emboscada contra gangue rival
            var rivalGangs = _gangSystem.GetAllGangs()
                .Where(g => g.Type != gang && _gangSystem.GetGang(gang).IsEnemy(g.Type))
                .ToList();

            if (rivalGangs.Count == 0) return null;

            GangType rival = rivalGangs[_random.Next(rivalGangs.Count)].Type;
            var rivalTerritories = TerritoryDatabase.GetTerritoriesByGang(rival);
            
            if (rivalTerritories.Count == 0) return null;

            var targetTerritory = rivalTerritories[_random.Next(rivalTerritories.Count)];

            GangMission mission = new GangMission
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Emboscada: {rival.GetDisplayName()}",
                Description = $"Prepare uma emboscada para membros de {rival.GetDisplayName()}.",
                Type = GangMissionType.Ambush,
                AssignedGang = gang,
                TargetGang = rival,
                TargetX = targetTerritory.CenterX,
                TargetY = targetTerritory.CenterY,
                TargetZ = targetTerritory.CenterZ,
                TargetRadius = 100f,
                RequiresGangMembers = true,
                MinimumGangMembers = 2,
                MaximumGangMembers = 4,
                MoneyReward = _random.Next(2000, 4000),
                ReputationReward = 20,
                TimeLimitMinutes = 20,
                DifficultyLevel = 3,
                RequiresWeapons = true
            };

            mission.AddObjective("Vá até o ponto de emboscada", 1);
            mission.AddObjective("Elimine membros rivais", 3);

            return mission;
        }

        private GangMission CreateHitContractMission(GangType gang)
        {
            Vector3 targetLocation = new Vector3(200f, -900f, 30f); // Downtown

            GangMission mission = new GangMission
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Contrato de Eliminação",
                Description = "Elimine um alvo específico. Seja discreto.",
                Type = GangMissionType.HitContract,
                AssignedGang = gang,
                TargetX = targetLocation.X,
                TargetY = targetLocation.Y,
                TargetZ = targetLocation.Z,
                TargetRadius = 150f,
                MoneyReward = _random.Next(5000, 10000),
                ReputationReward = 30,
                TimeLimitMinutes = 30,
                DifficultyLevel = 5,
                RequiresWeapons = true,
                TargetPedModel = "a_m_m_business_01"
            };

            mission.AddObjective("Localize o alvo", 1);
            mission.AddObjective("Elimine o alvo", 1);
            mission.AddObjective("Escape da área", 1);

            return mission;
        }

        // ===== GERENCIAMENTO DE MISSÕES =====

        public List<GangMission> GetAvailableMissions()
        {
            return new List<GangMission>(_availableMissions.Where(m => m.State == MissionState.NotStarted));
        }

        public bool AcceptMission(string missionId)
        {
            var mission = _availableMissions.FirstOrDefault(m => m.Id == missionId);
            if (mission == null) return false;

            mission.Start();
            
            if (_debugEnabled)
            {
                global::GTA.UI.Notification.Show($"~g~[DEBUG] Missão aceita: {mission.Name}");
            }

            return true;
        }

        public void CancelMission(string missionId)
        {
            var mission = _availableMissions.FirstOrDefault(m => m.Id == missionId);
            if (mission != null)
            {
                mission.Cancel();
                _availableMissions.Remove(mission);
            }
        }

        public GangMission GetActiveMission()
        {
            return _availableMissions.FirstOrDefault(m => m.State == MissionState.Active || m.State == MissionState.InProgress);
        }

        private void CleanupExpiredMissions()
        {
            DateTime cutoff = DateTime.Now.AddHours(-24);
            
            _availableMissions.RemoveAll(m => 
                m.State == MissionState.NotStarted && 
                m.StartedAt.HasValue && 
                m.StartedAt.Value < cutoff);
        }

        public void SetDebugMode(bool enabled)
        {
            _debugEnabled = enabled;
        }
    }
}
