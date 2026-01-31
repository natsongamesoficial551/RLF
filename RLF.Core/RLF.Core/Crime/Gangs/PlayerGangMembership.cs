using System;
using System.Collections.Generic;

namespace RLF.Core.Gangs
{
    /// <summary>
    /// Rank do jogador na gangue
    /// </summary>
    public enum GangRank
    {
        Prospect,           // Aspirante
        Member,             // Membro
        Veteran,            // Veterano
        Lieutenant,         // Tenente
        Underboss,          // Sub-chefe
        Boss                // Chefe (apenas em gangues próprias futuras)
    }

    /// <summary>
    /// Gerencia a participação do jogador em gangues
    /// </summary>
    public class PlayerGangMembership
    {
        public GangType? CurrentGang { get; private set; }
        public GangRank Rank { get; private set; }
        public int Respect { get; private set; }                   // Respeito na gangue (0-100)
        public DateTime? JoinedAt { get; private set; }
        public int MissionsCompleted { get; private set; }
        public int TerritoriesCaptured { get; private set; }
        public int CrimesCommitted { get; private set; }
        public decimal MoneyEarned { get; private set; }
        
        // Benefícios
        public bool CanStartMissions { get; private set; }
        public bool CanRecruitNPCs { get; private set; }
        public bool CanAccessWeapons { get; private set; }
        public bool CanAccessVehicles { get; private set; }
        public int MaxNPCFollowers { get; private set; }
        
        // Histórico
        private List<GangType> _previousGangs;
        private Dictionary<GangType, int> _reputationWithGangs;

        public PlayerGangMembership()
        {
            CurrentGang = null;
            Rank = GangRank.Prospect;
            Respect = 0;
            MissionsCompleted = 0;
            TerritoriesCaptured = 0;
            CrimesCommitted = 0;
            MoneyEarned = 0m;
            
            _previousGangs = new List<GangType>();
            _reputationWithGangs = new Dictionary<GangType, int>();
            
            UpdateBenefits();
        }

        /// <summary>
        /// Entra em uma gangue
        /// </summary>
        public bool JoinGang(GangType gang)
        {
            if (CurrentGang.HasValue) return false;
            
            CurrentGang = gang;
            Rank = GangRank.Prospect;
            Respect = 10; // Começa com respeito baixo
            JoinedAt = DateTime.Now;
            
            // Reseta estatísticas
            MissionsCompleted = 0;
            TerritoriesCaptured = 0;
            
            UpdateBenefits();
            return true;
        }

        /// <summary>
        /// Sai da gangue atual
        /// </summary>
        public void LeaveGang()
        {
            if (!CurrentGang.HasValue) return;
            
            // Adiciona ao histórico
            _previousGangs.Add(CurrentGang.Value);
            
            // Reseta
            CurrentGang = null;
            Rank = GangRank.Prospect;
            Respect = 0;
            JoinedAt = null;
            
            UpdateBenefits();
        }

        /// <summary>
        /// Aumenta respeito
        /// </summary>
        public void IncreaseRespect(int amount)
        {
            Respect = Math.Min(100, Respect + amount);
            CheckForPromotion();
        }

        /// <summary>
        /// Diminui respeito
        /// </summary>
        public void DecreaseRespect(int amount)
        {
            Respect = Math.Max(0, Respect - amount);
            
            // Se respeito chegar a 0, é expulso
            if (Respect <= 0 && CurrentGang.HasValue)
            {
                LeaveGang();
            }
            else
            {
                CheckForDemotion();
            }
        }

        /// <summary>
        /// Verifica se deve ser promovido
        /// </summary>
        private void CheckForPromotion()
        {
            GangRank newRank = CalculateRankFromRespect();
            
            if (newRank > Rank)
            {
                Rank = newRank;
                UpdateBenefits();
            }
        }

        /// <summary>
        /// Verifica se deve ser rebaixado
        /// </summary>
        private void CheckForDemotion()
        {
            GangRank newRank = CalculateRankFromRespect();
            
            if (newRank < Rank)
            {
                Rank = newRank;
                UpdateBenefits();
            }
        }

        /// <summary>
        /// Calcula rank baseado no respeito
        /// </summary>
        private GangRank CalculateRankFromRespect()
        {
            if (Respect >= 90) return GangRank.Underboss;
            if (Respect >= 70) return GangRank.Lieutenant;
            if (Respect >= 50) return GangRank.Veteran;
            if (Respect >= 25) return GangRank.Member;
            return GangRank.Prospect;
        }

        /// <summary>
        /// Atualiza benefícios baseado no rank
        /// </summary>
        private void UpdateBenefits()
        {
            switch (Rank)
            {
                case GangRank.Prospect:
                    CanStartMissions = false;
                    CanRecruitNPCs = false;
                    CanAccessWeapons = false;
                    CanAccessVehicles = false;
                    MaxNPCFollowers = 0;
                    break;

                case GangRank.Member:
                    CanStartMissions = true;
                    CanRecruitNPCs = false;
                    CanAccessWeapons = true;
                    CanAccessVehicles = false;
                    MaxNPCFollowers = 1;
                    break;

                case GangRank.Veteran:
                    CanStartMissions = true;
                    CanRecruitNPCs = true;
                    CanAccessWeapons = true;
                    CanAccessVehicles = true;
                    MaxNPCFollowers = 2;
                    break;

                case GangRank.Lieutenant:
                    CanStartMissions = true;
                    CanRecruitNPCs = true;
                    CanAccessWeapons = true;
                    CanAccessVehicles = true;
                    MaxNPCFollowers = 3;
                    break;

                case GangRank.Underboss:
                    CanStartMissions = true;
                    CanRecruitNPCs = true;
                    CanAccessWeapons = true;
                    CanAccessVehicles = true;
                    MaxNPCFollowers = 4;
                    break;

                case GangRank.Boss:
                    CanStartMissions = true;
                    CanRecruitNPCs = true;
                    CanAccessWeapons = true;
                    CanAccessVehicles = true;
                    MaxNPCFollowers = 6;
                    break;
            }
        }

        /// <summary>
        /// Registra missão completada
        /// </summary>
        public void RecordMissionCompleted()
        {
            MissionsCompleted++;
            IncreaseRespect(5);
        }

        /// <summary>
        /// Registra território capturado
        /// </summary>
        public void RecordTerritoryCapture()
        {
            TerritoriesCaptured++;
            IncreaseRespect(10);
        }

        /// <summary>
        /// Registra crime cometido
        /// </summary>
        public void RecordCrime()
        {
            CrimesCommitted++;
            IncreaseRespect(1);
        }

        /// <summary>
        /// Registra dinheiro ganho
        /// </summary>
        public void RecordMoneyEarned(decimal amount)
        {
            MoneyEarned += amount;
            
            // Respeito extra por cada $1000 ganhos
            int respectBonus = (int)(amount / 1000m);
            if (respectBonus > 0)
            {
                IncreaseRespect(respectBonus);
            }
        }

        /// <summary>
        /// Verifica se jogador pode entrar em uma gangue específica
        /// </summary>
        public bool CanJoinGang(GangType gang, GangData gangData)
        {
            if (CurrentGang.HasValue) return false;
            if (!gangData.CanRecruitPlayer) return false;
            
            // Verifica se já foi membro e traiu
            if (_previousGangs.Contains(gang))
            {
                // Traidores não podem voltar
                return false;
            }
            
            // Verifica requisitos de crimes e reputação
            return gangData.CanRecruitPlayerWithRequirements(CrimesCommitted, GetReputationWithGang(gang));
        }

        /// <summary>
        /// Define reputação com uma gangue
        /// </summary>
        public void SetReputationWithGang(GangType gang, int reputation)
        {
            reputation = Math.Max(-100, Math.Min(100, reputation));
            _reputationWithGangs[gang] = reputation;
        }

        /// <summary>
        /// Obtém reputação com uma gangue
        /// </summary>
        public int GetReputationWithGang(GangType gang)
        {
            if (_reputationWithGangs.ContainsKey(gang))
                return _reputationWithGangs[gang];
            
            return 0; // Neutro por padrão
        }

        /// <summary>
        /// Melhora reputação com gangue
        /// </summary>
        public void ImproveReputationWithGang(GangType gang, int amount)
        {
            int current = GetReputationWithGang(gang);
            SetReputationWithGang(gang, current + amount);
        }

        /// <summary>
        /// Piora reputação com gangue
        /// </summary>
        public void WorsenReputationWithGang(GangType gang, int amount)
        {
            int current = GetReputationWithGang(gang);
            SetReputationWithGang(gang, current - amount);
        }

        /// <summary>
        /// Verifica se é membro de alguma gangue
        /// </summary>
        public bool IsInGang()
        {
            return CurrentGang.HasValue;
        }

        /// <summary>
        /// Verifica se é membro de gangue específica
        /// </summary>
        public bool IsInGang(GangType gang)
        {
            return CurrentGang.HasValue && CurrentGang.Value == gang;
        }

        /// <summary>
        /// Tempo desde que entrou na gangue
        /// </summary>
        public TimeSpan TimeSinceJoined()
        {
            if (!JoinedAt.HasValue)
                return TimeSpan.Zero;
            
            return DateTime.Now - JoinedAt.Value;
        }

        /// <summary>
        /// Obtém nome do rank
        /// </summary>
        public string GetRankName()
        {
            switch (Rank)
            {
                case GangRank.Prospect: return "Prospect";
                case GangRank.Member: return "Member";
                case GangRank.Veteran: return "Veteran";
                case GangRank.Lieutenant: return "Lieutenant";
                case GangRank.Underboss: return "Underboss";
                case GangRank.Boss: return "Boss";
                default: return "Unknown";
            }
        }

        /// <summary>
        /// Obtém descrição do rank
        /// </summary>
        public string GetRankDescription()
        {
            switch (Rank)
            {
                case GangRank.Prospect:
                    return "New recruit proving their worth";
                case GangRank.Member:
                    return "Full member of the gang";
                case GangRank.Veteran:
                    return "Experienced and trusted member";
                case GangRank.Lieutenant:
                    return "Leader of operations";
                case GangRank.Underboss:
                    return "Second-in-command";
                case GangRank.Boss:
                    return "Leader of the gang";
                default:
                    return "";
            }
        }
    }
}
