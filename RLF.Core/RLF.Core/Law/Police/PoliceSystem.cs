using RLF.Core.Events;
using RLF.Core.Logging;
using RLF.Core.Systems;
using System.Collections.Generic;
using System.Linq;

namespace RLF.Core.Law.Police
{
    /// <summary>
    /// Sistema central da Polícia (Core).
    /// Mantém estado lógico das unidades.
    /// NÃO usa GTA.
    /// </summary>
    public sealed class PoliceSystem : SystemBase
    {
        private readonly List<PoliceUnitModel> _units = new List<PoliceUnitModel>();

        public IReadOnlyList<PoliceUnitModel> Units => _units;

        public PoliceSystem(Logger logger, EventManager eventManager, int tickRate = 30)
            : base("PoliceSystem", logger, eventManager, tickRate)
        {
        }

        // =====================
        // Lifecycle
        // =====================

        protected override void OnStart()
        {
            Logger.Info("[PoliceSystem] Iniciado");
            CreateInitialUnits();
        }

        protected override void OnStop()
        {
            _units.Clear();
            Logger.Info("[PoliceSystem] Finalizado");
        }

        protected override void OnTick()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                _units[i].Tick();
            }
        }

        // =====================
        // Inicialização
        // =====================

        private void CreateInitialUnits()
        {
            _units.Clear();

            // 7 urbanas
            for (int i = 0; i < 7; i++)
            {
                _units.Add(new PoliceUnitModel(i + 1, true));
            }

            // 5 rurais
            for (int i = 0; i < 5; i++)
            {
                _units.Add(new PoliceUnitModel(i + 8, false));
            }

            Logger.Info("[PoliceSystem] 12 unidades criadas (7 urbanas / 5 rurais)");
        }

        // =====================
        // CONSULTAS
        // =====================

        public IReadOnlyList<PoliceUnitModel> GetAllUnits()
        {
            return _units;
        }

        public IReadOnlyList<PoliceUnitModel> GetUrbanUnits()
        {
            return _units.Where(u => u.IsUrban).ToList();
        }

        public IReadOnlyList<PoliceUnitModel> GetRuralUnits()
        {
            return _units.Where(u => !u.IsUrban).ToList();
        }

        public IReadOnlyList<PoliceUnitModel> GetPatrollingUnits()
        {
            return _units.Where(u => u.Status == PoliceUnitStatus.Patrolling).ToList();
        }

        // =====================
        // CONTROLE DE ESTADO
        // =====================

        public bool TryAcquireUnit(int unitId)
        {
            var unit = _units.FirstOrDefault(u => u.Id == unitId);
            if (unit == null)
                return false;

            if (unit.Status != PoliceUnitStatus.Patrolling)
                return false;

            unit.SetBusy();
            return true;
        }

        public void ReleaseUnit(int unitId)
        {
            var unit = _units.FirstOrDefault(u => u.Id == unitId);
            if (unit != null)
            {
                unit.SetPatrolling();
            }
        }

        public void DisableUnit(int unitId)
        {
            var unit = _units.FirstOrDefault(u => u.Id == unitId);
            if (unit != null)
            {
                unit.Disable();
            }
        }
    }
}
