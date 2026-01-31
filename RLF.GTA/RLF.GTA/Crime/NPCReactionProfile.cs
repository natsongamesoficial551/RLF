using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace RLF.GTA.Crime
{
    /// <summary>
    /// Perfil comportamental de um NPC para reações a crimes.
    /// Define características de personalidade que influenciam como o NPC reage a situações criminais.
    /// </summary>
    public class NPCReactionProfile
    {
        public Ped Ped { get; private set; }
        public float Fear { get; private set; }
        public float Courage { get; private set; }
        public float Aggression { get; private set; }
        public float Intelligence { get; private set; }
        public float LoyaltyToPlayer { get; private set; }
        public bool IsArmed { get; private set; }
        public bool IsInGang { get; private set; }
        public bool IsCop { get; private set; }
        public float DistanceFromCrime { get; set; }
        public DateTime ProfileCreatedAt { get; private set; }

        public NPCReactionProfile(Ped ped)
        {
            if (ped == null || !ped.Exists())
            {
                Ped = null;
                return;
            }

            Ped = ped;
            ProfileCreatedAt = DateTime.Now;

            GeneratePersonalityTraits();
            DetermineArmedStatus();
            DetermineAffiliation();
        }

        private void GeneratePersonalityTraits()
        {
            Random rng = new Random(Ped.Handle);

            Fear = 0.3f + (float)(rng.NextDouble() * 0.7);
            Courage = 1f - Fear + (float)((rng.NextDouble() - 0.5) * 0.3);
            Aggression = (float)(rng.NextDouble());
            Intelligence = (float)(rng.NextDouble());
            LoyaltyToPlayer = 0f;

            Fear = Math.Max(0f, Math.Min(Fear, 1f));
            Courage = Math.Max(0f, Math.Min(Courage, 1f));
            Aggression = Math.Max(0f, Math.Min(Aggression, 1f));
            Intelligence = Math.Max(0f, Math.Min(Intelligence, 1f));
        }

        private void DetermineArmedStatus()
        {
            if (Ped == null || !Ped.Exists()) return;

            IsArmed = Ped.Weapons.Current != null && 
                      Ped.Weapons.Current.Hash != WeaponHash.Unarmed;
        }

        private void DetermineAffiliation()
        {
            if (Ped == null || !Ped.Exists()) return;

            IsCop = Ped.RelationshipGroup == Function.Call<int>(Hash.GET_HASH_KEY, "COP");
            IsInGang = Ped.RelationshipGroup == Function.Call<int>(Hash.GET_HASH_KEY, "GANG_1") ||
                       Ped.RelationshipGroup == Function.Call<int>(Hash.GET_HASH_KEY, "GANG_2") ||
                       Ped.RelationshipGroup == Function.Call<int>(Hash.GET_HASH_KEY, "GANG_9") ||
                       Ped.RelationshipGroup == Function.Call<int>(Hash.GET_HASH_KEY, "GANG_10");
        }

        public bool IsValid()
        {
            return Ped != null && Ped.Exists() && Ped.IsAlive;
        }

        public float GetReportProbability()
        {
            if (!IsValid()) return 0f;
            if (IsCop) return 1f;
            if (IsInGang) return 0.1f;

            float baseProbability = 0.6f;
            baseProbability -= Fear * 0.3f;
            baseProbability += Courage * 0.2f;
            baseProbability += Intelligence * 0.1f;

            if (DistanceFromCrime > 50f) baseProbability *= 0.5f;
            if (DistanceFromCrime > 100f) baseProbability *= 0.3f;

            return Math.Max(0f, Math.Min(baseProbability, 1f));
        }

        public float GetFleeChance()
        {
            if (!IsValid()) return 1f;
            if (IsCop) return 0f;

            float fleeChance = Fear * 0.7f;
            fleeChance += (1f - Courage) * 0.3f;

            if (IsArmed) fleeChance *= 0.5f;
            if (IsInGang) fleeChance *= 0.3f;

            return Math.Max(0f, Math.Min(fleeChance, 1f));
        }

        public float GetFightBackChance()
        {
            if (!IsValid()) return 0f;
            if (!IsArmed) return 0f;
            if (IsCop) return 0.9f;

            float fightChance = Aggression * 0.5f;
            fightChance += Courage * 0.3f;
            fightChance -= Fear * 0.2f;

            if (IsInGang) fightChance *= 1.5f;

            return Math.Max(0f, Math.Min(fightChance, 1f));
        }

        public float GetComplianceChance()
        {
            if (!IsValid()) return 0f;

            float complianceChance = Fear * 0.5f;
            complianceChance += Intelligence * 0.3f;
            complianceChance -= Courage * 0.2f;

            if (IsArmed) complianceChance *= 0.7f;
            if (IsInGang) complianceChance *= 0.5f;

            return Math.Max(0f, Math.Min(complianceChance, 1f));
        }

        public TimeSpan TimeSinceCreation()
        {
            return DateTime.Now - ProfileCreatedAt;
        }
    }
}
