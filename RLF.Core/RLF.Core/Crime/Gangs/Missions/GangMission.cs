using System;
using System.Collections.Generic;

namespace RLF.Core.Gangs.Missions
{
    public enum GangMissionType
    {
        TerritoryTakeover,
        TerritoryDefense,
        StoreRobbery,
        BankHeist,
        DrugDelivery,
        DrugPickup,
        Kidnapping,
        Ambush,
        HitContract,
        WeaponsDeal,
        VehicleTheft,
        RecruitMembers,
        CollectProtectionMoney,
        Intimidation
    }

    public enum MissionState
    {
        NotStarted,
        Active,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    public class MissionObjective
    {
        public string Description { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsOptional { get; set; }
        public int CurrentProgress { get; set; }
        public int RequiredProgress { get; set; }

        public MissionObjective(string description, int required = 1, bool optional = false)
        {
            Description = description;
            RequiredProgress = required;
            IsOptional = optional;
            IsCompleted = false;
            CurrentProgress = 0;
        }

        public void UpdateProgress(int amount)
        {
            CurrentProgress += amount;
            if (CurrentProgress >= RequiredProgress) IsCompleted = true;
        }

        public float GetProgressPercentage()
        {
            return RequiredProgress == 0 ? 100f : (float)CurrentProgress / RequiredProgress * 100f;
        }
    }

    public class GangMission
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public GangMissionType Type { get; set; }
        public MissionState State { get; set; }

        // Localização
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float StartZ { get; set; }
        public float TargetX { get; set; }
        public float TargetY { get; set; }
        public float TargetZ { get; set; }
        public float TargetRadius { get; set; }

        // Gangue
        public GangType AssignedGang { get; set; }
        public bool RequiresGangMembers { get; set; }
        public int MinimumGangMembers { get; set; }
        public int MaximumGangMembers { get; set; }

        // Objetivos
        public List<MissionObjective> Objectives { get; set; }

        // Recompensas
        public decimal MoneyReward { get; set; }
        public int ReputationReward { get; set; }
        public int InfluenceReward { get; set; }

        // Tempo
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TimeLimitMinutes { get; set; }

        // Dificuldade
        public int DifficultyLevel { get; set; }
        public bool RequiresWeapons { get; set; }
        public bool RequiresVehicle { get; set; }

        // Alvos
        public string TargetPedModel { get; set; }
        public string TargetVehicleModel { get; set; }
        public GangType? TargetGang { get; set; }
        public string TargetTerritoryId { get; set; }

        public GangMission()
        {
            State = MissionState.NotStarted;
            Objectives = new List<MissionObjective>();
            TargetRadius = 50f;
            MinimumGangMembers = 0;
            MaximumGangMembers = 4;
            TimeLimitMinutes = 0;
            DifficultyLevel = 1;
        }

        public void Start()
        {
            State = MissionState.Active;
            StartedAt = DateTime.Now;
        }

        public void SetInProgress() { State = MissionState.InProgress; }
        public void Complete() { State = MissionState.Completed; CompletedAt = DateTime.Now; }
        public void Fail() { State = MissionState.Failed; CompletedAt = DateTime.Now; }
        public void Cancel() { State = MissionState.Cancelled; }

        public bool HasExpired()
        {
            if (TimeLimitMinutes == 0 || !StartedAt.HasValue) return false;
            return (DateTime.Now - StartedAt.Value).TotalMinutes >= TimeLimitMinutes;
        }

        public TimeSpan GetTimeRemaining()
        {
            if (TimeLimitMinutes == 0 || !StartedAt.HasValue) return TimeSpan.MaxValue;
            TimeSpan elapsed = DateTime.Now - StartedAt.Value;
            TimeSpan limit = TimeSpan.FromMinutes(TimeLimitMinutes);
            return elapsed >= limit ? TimeSpan.Zero : limit - elapsed;
        }

        public bool AreAllObjectivesComplete()
        {
            foreach (var obj in Objectives)
                if (!obj.IsOptional && !obj.IsCompleted) return false;
            return true;
        }

        public void AddObjective(string description, int required = 1, bool optional = false)
        {
            Objectives.Add(new MissionObjective(description, required, optional));
        }

        public void UpdateObjective(int index, int progress)
        {
            if (index >= 0 && index < Objectives.Count)
            {
                Objectives[index].UpdateProgress(progress);
                if (AreAllObjectivesComplete() && State == MissionState.InProgress) Complete();
            }
        }

        public float DistanceToTarget(float x, float y, float z)
        {
            float dx = TargetX - x;
            float dy = TargetY - y;
            float dz = TargetZ - z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public bool IsInTargetRadius(float x, float y, float z)
        {
            return DistanceToTarget(x, y, z) <= TargetRadius;
        }
    }
}
