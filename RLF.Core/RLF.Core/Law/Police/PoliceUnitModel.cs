using System;

namespace RLF.Core.Law.Police
{
    /// <summary>
    /// Modelo lógico de uma viatura/unidade policial (Core).
    /// Não usa GTA types.
    /// </summary>
    public sealed class PoliceUnitModel
    {
        public int Id { get; private set; }
        public bool IsUrban { get; private set; }

        // Setter propositalmente protegido (Core controla mudança via métodos)
        public PoliceUnitStatus Status { get; private set; }

        // Controle interno simples (se você quiser usar depois)
        private int _busyUntilMs;

        public PoliceUnitModel(int id, bool isUrban)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));

            Id = id;
            IsUrban = isUrban;
            Status = PoliceUnitStatus.Patrolling;
            _busyUntilMs = 0;
        }

        public void Tick()
        {
            // Se você quiser no futuro usar cooldown por tempo,
            // dá pra ligar aqui com TimeSystem.
            // Por enquanto mantemos simples e estável.
        }

        public void SetPatrolling()
        {
            Status = PoliceUnitStatus.Patrolling;
            _busyUntilMs = 0;
        }

        public void SetBusy()
        {
            Status = PoliceUnitStatus.Busy;
            _busyUntilMs = 0;
        }

        // ✅ ADIÇÃO NECESSÁRIA (resolve seu erro)
        public void Disable()
        {
            Status = PoliceUnitStatus.Disabled;
            _busyUntilMs = 0;
        }
    }
}
