using System;
using System.Collections.Generic;
using System.Linq;

namespace RLF.Core.Gangs
{
    /// <summary>
    /// Representa uma gangue com seus dados e status
    /// </summary>
    public class GangData
    {
        public GangType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        // Estatísticas
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public decimal Treasury { get; set; }               // Cofre da gangue
        public int Reputation { get; set; }                 // Reputação (0-100)
        public int PowerLevel { get; set; }                 // Nível de poder (baseado em territórios)
        
        // Territórios
        public List<string> ControlledTerritories { get; set; }
        public int TerritoryCount => ControlledTerritories.Count;
        
        // Relações com outras gangues (-100 a 100)
        public Dictionary<GangType, int> Relations { get; set; }
        
        // Equipamento
        public List<string> AvailableWeapons { get; set; }
        public List<string> AvailableVehicles { get; set; }
        
        // Atividades
        public DateTime LastActivity { get; set; }
        public int CrimesCommitted { get; set; }
        public int TerritoriesCaptured { get; set; }
        public int WarsFought { get; set; }
        
        // Config
        public bool IsAggressive { get; set; }              // Se ataca outros territórios
        public float ActivityLevel { get; set; }            // 0.0 a 1.0 - quão ativa é
        public bool CanRecruitPlayer { get; set; }          // Se pode recrutar jogador

        public GangData()
        {
            ControlledTerritories = new List<string>();
            Relations = new Dictionary<GangType, int>();
            AvailableWeapons = new List<string>();
            AvailableVehicles = new List<string>();
            LastActivity = DateTime.Now;
            ActivityLevel = 0.5f;
            CanRecruitPlayer = true;
        }

        /// <summary>
        /// Adiciona dinheiro ao cofre
        /// </summary>
        public void AddMoney(decimal amount)
        {
            Treasury += amount;
        }

        /// <summary>
        /// Remove dinheiro do cofre
        /// </summary>
        public bool SpendMoney(decimal amount)
        {
            if (Treasury < amount) return false;
            
            Treasury -= amount;
            return true;
        }

        /// <summary>
        /// Adiciona território controlado
        /// </summary>
        public void AddTerritory(string territoryId)
        {
            if (!ControlledTerritories.Contains(territoryId))
            {
                ControlledTerritories.Add(territoryId);
                UpdatePowerLevel();
            }
        }

        /// <summary>
        /// Remove território controlado
        /// </summary>
        public void RemoveTerritory(string territoryId)
        {
            if (ControlledTerritories.Contains(territoryId))
            {
                ControlledTerritories.Remove(territoryId);
                UpdatePowerLevel();
            }
        }

        /// <summary>
        /// Atualiza nível de poder baseado em territórios
        /// </summary>
        private void UpdatePowerLevel()
        {
            PowerLevel = TerritoryCount * 10;
        }

        /// <summary>
        /// Aumenta reputação
        /// </summary>
        public void IncreaseReputation(int amount)
        {
            Reputation = Math.Min(100, Reputation + amount);
        }

        /// <summary>
        /// Diminui reputação
        /// </summary>
        public void DecreaseReputation(int amount)
        {
            Reputation = Math.Max(0, Reputation - amount);
        }

        /// <summary>
        /// Define relação com outra gangue
        /// </summary>
        public void SetRelation(GangType otherGang, int value)
        {
            value = Math.Max(-100, Math.Min(100, value));
            Relations[otherGang] = value;
        }

        /// <summary>
        /// Obtém relação com outra gangue
        /// </summary>
        public int GetRelation(GangType otherGang)
        {
            if (Relations.ContainsKey(otherGang))
                return Relations[otherGang];
            
            return 0; // Neutro por padrão
        }

        /// <summary>
        /// Melhora relação com outra gangue
        /// </summary>
        public void ImproveRelation(GangType otherGang, int amount)
        {
            int current = GetRelation(otherGang);
            SetRelation(otherGang, current + amount);
        }

        /// <summary>
        /// Piora relação com outra gangue
        /// </summary>
        public void WorsenRelation(GangType otherGang, int amount)
        {
            int current = GetRelation(otherGang);
            SetRelation(otherGang, current - amount);
        }

        /// <summary>
        /// Verifica se é inimiga de outra gangue
        /// </summary>
        public bool IsEnemy(GangType otherGang)
        {
            return GetRelation(otherGang) <= -50;
        }

        /// <summary>
        /// Verifica se é aliada de outra gangue
        /// </summary>
        public bool IsAlly(GangType otherGang)
        {
            return GetRelation(otherGang) >= 50;
        }

        /// <summary>
        /// Verifica se é neutra com outra gangue
        /// </summary>
        public bool IsNeutral(GangType otherGang)
        {
            int relation = GetRelation(otherGang);
            return relation > -50 && relation < 50;
        }

        /// <summary>
        /// Registra atividade
        /// </summary>
        public void RecordActivity()
        {
            LastActivity = DateTime.Now;
        }

        /// <summary>
        /// Registra crime cometido
        /// </summary>
        public void RecordCrime()
        {
            CrimesCommitted++;
            RecordActivity();
        }

        /// <summary>
        /// Registra território capturado
        /// </summary>
        public void RecordTerritoryCapture()
        {
            TerritoriesCaptured++;
            RecordActivity();
        }

        /// <summary>
        /// Registra guerra
        /// </summary>
        public void RecordWar()
        {
            WarsFought++;
            RecordActivity();
        }

        /// <summary>
        /// Calcula renda diária dos territórios
        /// </summary>
        public decimal CalculateDailyIncome()
        {
            decimal totalIncome = 0m;
            
            foreach (string territoryId in ControlledTerritories)
            {
                var territory = TerritoryDatabase.GetTerritoryById(territoryId);
                if (territory != null)
                {
                    // Renda baseada na força de controle
                    totalIncome += territory.DailyIncome * (decimal)territory.ControlStrength;
                }
            }
            
            return totalIncome;
        }

        /// <summary>
        /// Adiciona arma disponível
        /// </summary>
        public void AddWeapon(string weaponHash)
        {
            if (!AvailableWeapons.Contains(weaponHash))
            {
                AvailableWeapons.Add(weaponHash);
            }
        }

        /// <summary>
        /// Adiciona veículo disponível
        /// </summary>
        public void AddVehicle(string vehicleModel)
        {
            if (!AvailableVehicles.Contains(vehicleModel))
            {
                AvailableVehicles.Add(vehicleModel);
            }
        }

        /// <summary>
        /// Verifica se jogador pode ser recrutado baseado em requisitos
        /// </summary>
        public bool CanRecruitPlayerWithRequirements(int playerCrimes, int playerReputation)
        {
            if (!CanRecruitPlayer) return false;
            
            // Requisitos mínimos
            int minCrimes = GetMinimumCrimesRequired();
            int minReputation = GetMinimumReputationRequired();
            
            return playerCrimes >= minCrimes && playerReputation >= minReputation;
        }

        private int GetMinimumCrimesRequired()
        {
            // Gangues de rua: requisitos baixos
            if (Type.IsStreetGang()) return 5;
            
            // Crime organizado: requisitos médios
            if (Type.IsOrganizedCrime()) return 15;
            
            // Lost MC: requisitos médios
            if (Type == GangType.LostMC) return 10;
            
            return 5;
        }

        private int GetMinimumReputationRequired()
        {
            // Gangues de rua: reputação baixa
            if (Type.IsStreetGang()) return 10;
            
            // Crime organizado: reputação alta
            if (Type.IsOrganizedCrime()) return 40;
            
            // Lost MC: reputação média
            if (Type == GangType.LostMC) return 25;
            
            return 10;
        }
    }
}
