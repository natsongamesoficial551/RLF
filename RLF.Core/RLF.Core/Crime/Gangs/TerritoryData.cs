using System;
using System.Collections.Generic;

namespace RLF.Core.Gangs
{
    public enum TerritoryControlState
    {
        Neutral,
        Controlled,
        Contested,
        UnderAttack
    }

    public class TerritoryData
    {
        public string Id { get; set; }
        public string Name { get; set; }

        // Coordenadas do centro
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float CenterZ { get; set; }
        public float Radius { get; set; }

        public GangType? ControllingGang { get; set; }
        public float ControlStrength { get; set; }
        public TerritoryControlState State { get; set; }
        public DateTime? LastCapturedAt { get; set; }

        public GangType? AttackingGang { get; set; }
        public float AttackProgress { get; set; }
        public DateTime? AttackStartedAt { get; set; }

        public decimal DailyIncome { get; set; }
        public int InfluencePoints { get; set; }

        public int MaxGangMembers { get; set; }
        public List<int> ActiveMemberHandles { get; set; }

        public TerritoryData()
        {
            State = TerritoryControlState.Neutral;
            ControlStrength = 0f;
            AttackProgress = 0f;
            ActiveMemberHandles = new List<int>();
            MaxGangMembers = 8;
            DailyIncome = 100m;
            InfluencePoints = 10;
        }

        public bool ContainsPosition(float x, float y, float z)
        {
            float dx = CenterX - x;
            float dy = CenterY - y;
            float dz = CenterZ - z;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            return distance <= Radius;
        }

        public void StartAttack(GangType attackingGang)
        {
            if (attackingGang == ControllingGang) return;
            State = TerritoryControlState.UnderAttack;
            AttackingGang = attackingGang;
            AttackProgress = 0f;
            AttackStartedAt = DateTime.Now;
        }

        public void UpdateAttack(float progressDelta)
        {
            if (State != TerritoryControlState.UnderAttack) return;
            AttackProgress += progressDelta;
            if (AttackProgress >= 1f) CompleteTakeover();
            else if (AttackProgress <= 0f) CancelAttack();
        }

        private void CompleteTakeover()
        {
            if (!AttackingGang.HasValue) return;
            ControllingGang = AttackingGang;
            ControlStrength = 0.5f;
            State = TerritoryControlState.Controlled;
            LastCapturedAt = DateTime.Now;
            AttackingGang = null;
            AttackProgress = 0f;
            AttackStartedAt = null;
        }

        public void CancelAttack()
        {
            AttackingGang = null;
            AttackProgress = 0f;
            AttackStartedAt = null;
            State = ControllingGang.HasValue ? TerritoryControlState.Controlled : TerritoryControlState.Neutral;
        }

        public void IncreaseControl(float amount)
        {
            ControlStrength = Math.Min(1f, ControlStrength + amount);
        }

        public void DecreaseControl(float amount)
        {
            ControlStrength = Math.Max(0f, ControlStrength - amount);
            if (ControlStrength <= 0f && ControllingGang.HasValue) LoseControl();
        }

        private void LoseControl()
        {
            ControllingGang = null;
            ControlStrength = 0f;
            State = TerritoryControlState.Neutral;
            LastCapturedAt = null;
        }

        public bool IsAvailableForCapture()
        {
            return State != TerritoryControlState.UnderAttack;
        }

        public TimeSpan TimeSinceCapture()
        {
            return LastCapturedAt.HasValue ? DateTime.Now - LastCapturedAt.Value : TimeSpan.MaxValue;
        }
    }
}
