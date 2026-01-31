using System;
using GTA.Math;

namespace RLF.Core.Crime.Establishments
{
    /// <summary>
    /// Tipo de estabelecimento que pode ser roubado.
    /// </summary>
    public enum EstablishmentType
    {
        Store24_7,          // Lojas de conveniência
        AmmuNation,         // Lojas de armas
        FleecaBank,         // Bancos pequenos
        PacificBank,        // Banco grande (Pacific Standard)
        GasStation,         // Postos de gasolina
        LiquorStore,        // Lojas de bebidas
        Pharmacy            // Farmácias
    }

    /// <summary>
    /// Estado atual de um estabelecimento.
    /// </summary>
    public enum EstablishmentState
    {
        Available,          // Disponível para roubo
        BeingRobbed,        // Sendo roubado agora
        Alarmed,            // Alarme disparado
        PoliceNotified,     // Polícia foi chamada
        Cooldown            // Em cooldown após roubo
    }

    /// <summary>
    /// Dados de um estabelecimento roubável.
    /// </summary>
    public class EstablishmentData
    {
        public string Id { get; set; }
        public EstablishmentType Type { get; set; }
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 CounterPosition { get; set; }      // Posição do balcão/caixa
        public Vector3 SafePosition { get; set; }         // Posição do cofre (se houver)
        public float InteractionRadius { get; set; }
        
        // Propriedades de segurança
        public bool HasAlarm { get; set; }
        public bool HasSafe { get; set; }
        public bool HasCameras { get; set; }
        public float AlarmTriggerChance { get; set; }     // Chance de disparar alarme
        
        // Recompensas
        public decimal MinCashRegister { get; set; }      // Dinheiro mínimo no caixa
        public decimal MaxCashRegister { get; set; }      // Dinheiro máximo no caixa
        public decimal MinSafeMoney { get; set; }         // Dinheiro mínimo no cofre
        public decimal MaxSafeMoney { get; set; }         // Dinheiro máximo no cofre
        public float SafeOpenTime { get; set; }           // Tempo para abrir cofre (segundos)
        
        // Estado
        public EstablishmentState State { get; set; }
        public DateTime? LastRobbedAt { get; set; }
        public float CooldownHours { get; set; }          // Horas de cooldown após roubo
        
        // NPCs
        public string ClerkPedModel { get; set; }         // Modelo do atendente

        public EstablishmentData()
        {
            State = EstablishmentState.Available;
            InteractionRadius = 2.0f;
            CooldownHours = 1.0f;
            AlarmTriggerChance = 0.3f;
        }

        public bool IsAvailable()
        {
            if (State != EstablishmentState.Available && State != EstablishmentState.Cooldown)
                return false;

            if (LastRobbedAt.HasValue)
            {
                TimeSpan elapsed = DateTime.Now - LastRobbedAt.Value;
                if (elapsed.TotalHours < CooldownHours)
                    return false;
            }

            return true;
        }

        public TimeSpan GetRemainingCooldown()
        {
            if (!LastRobbedAt.HasValue)
                return TimeSpan.Zero;

            TimeSpan elapsed = DateTime.Now - LastRobbedAt.Value;
            TimeSpan cooldown = TimeSpan.FromHours(CooldownHours);

            if (elapsed >= cooldown)
                return TimeSpan.Zero;

            return cooldown - elapsed;
        }
    }
}
