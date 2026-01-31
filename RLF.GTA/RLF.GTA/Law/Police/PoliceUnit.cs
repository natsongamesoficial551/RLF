using GTA;
using GTA.Math;

namespace RLF.GTA.Law.Police
{
    public sealed class PoliceUnit
    {
        public int UnitId { get; }
        public PoliceUnitType Type { get; }
        public Vector3 SpawnPosition { get; set; }

        public Vehicle Vehicle { get; private set; }
        public Ped OfficerP1 { get; private set; }
        public Ped OfficerP2 { get; private set; }

        public Blip DebugBlip { get; private set; }

        public PoliceUnitState State { get; private set; }
        private int _busyUntil;

        public bool IsBusy => Game.GameTime < _busyUntil;

        public PoliceUnit(int id, PoliceUnitType type, Vector3 spawn)
        {
            UnitId = id;
            Type = type;
            SpawnPosition = spawn;
        }

        public void Bind(Vehicle v, Ped p1, Ped p2)
        {
            Vehicle = v;
            OfficerP1 = p1;
            OfficerP2 = p2;
        }

        public bool EntitiesExist()
        {
            return Vehicle?.Exists() == true &&
                   OfficerP1?.Exists() == true &&
                   OfficerP2?.Exists() == true;
        }

        public void MarkBusy(int ms)
        {
            _busyUntil = Game.GameTime + ms;
            State = PoliceUnitState.Busy;
        }

        public void SetPatrolling()
        {
            State = PoliceUnitState.Patrolling;
            _busyUntil = 0;
        }

        public void SetApproaching()
        {
            State = PoliceUnitState.Approaching;
        }

        public void EnsureDebugBlip()
        {
            // Blips removidos - apenas para debug
        }

        public void SoftCleanup()
        {
            try { DebugBlip?.Delete(); } catch { }

            try
            {
                Vehicle?.MarkAsNoLongerNeeded();
                OfficerP1?.MarkAsNoLongerNeeded();
                OfficerP2?.MarkAsNoLongerNeeded();
            }
            catch { }

            Vehicle = null;
            OfficerP1 = null;
            OfficerP2 = null;
            DebugBlip = null;
        }
    }
}