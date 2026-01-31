using System;
using GTA;
using GTA.Native;

namespace RLF.GTA.Crime
{
    /// <summary>
    /// Carteira individual de cada NPC.
    /// Gera dinheiro aleatório baseado em aparência e localização.
    /// </summary>
    public class NPCWallet
    {
        public Ped Owner { get; private set; }
        public decimal Money { get; private set; }
        public bool WasRobbed { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public NPCWallet(Ped ped)
        {
            Owner = ped;
            Money = GenerateRandomMoney(ped);
            WasRobbed = false;
            CreatedAt = DateTime.Now;
        }

        /// <summary>
        /// Rouba todo o dinheiro do NPC.
        /// </summary>
        public decimal Rob()
        {
            if (WasRobbed || Money <= 0)
                return 0m;

            decimal stolenAmount = Money;
            Money = 0m;
            WasRobbed = true;

            return stolenAmount;
        }

        /// <summary>
        /// Verifica se o NPC ainda tem dinheiro para roubar.
        /// </summary>
        public bool HasMoney()
        {
            return Money > 0m && !WasRobbed;
        }

        /// <summary>
        /// Gera quantidade aleatória de dinheiro baseado no NPC.
        /// </summary>
        private decimal GenerateRandomMoney(Ped ped)
        {
            if (ped == null || !ped.Exists())
                return 0m;

            Random rng = new Random(ped.Handle + DateTime.Now.Millisecond);

            // Detecta tipo de NPC por aparência/modelo
            PedType pedType = DetectPedType(ped);

            decimal minMoney = 0m;
            decimal maxMoney = 0m;

            switch (pedType)
            {
                case PedType.Poor:
                    minMoney = 10m;
                    maxMoney = 80m;
                    break;

                case PedType.Average:
                    minMoney = 50m;
                    maxMoney = 200m;
                    break;

                case PedType.Rich:
                    minMoney = 150m;
                    maxMoney = 600m;
                    break;

                case PedType.Business:
                    minMoney = 200m;
                    maxMoney = 800m;
                    break;

                default:
                    minMoney = 20m;
                    maxMoney = 150m;
                    break;
            }

            // Gera valor aleatório no range
            decimal range = maxMoney - minMoney;
            decimal randomAmount = minMoney + (range * (decimal)rng.NextDouble());

            // Arredonda para múltiplos de 5
            return Math.Round(randomAmount / 5m) * 5m;
        }

        /// <summary>
        /// Detecta o "tipo" do NPC por roupas e modelo.
        /// </summary>
        private PedType DetectPedType(Ped ped)
        {
            if (ped == null || !ped.Exists())
                return PedType.Average;

            // Detecta businessmen (terno)
            if (IsBusinessPed(ped))
                return PedType.Business;

            // Detecta ricos (por área ou modelo)
            if (IsRichPed(ped))
                return PedType.Rich;

            // Detecta pobres (homeless, bêbados)
            if (IsPoorPed(ped))
                return PedType.Poor;

            // Default: classe média
            return PedType.Average;
        }

        private bool IsBusinessPed(Ped ped)
        {
            // Lista de modelos de businessman
            PedHash[] businessModels = new PedHash[]
            {
                PedHash.Business01AFY,
                PedHash.Business01AMM,
                PedHash.Business01AMY,
                PedHash.Business02AFM,
                PedHash.Business02AFY,
                PedHash.Business03AFY,
                PedHash.Business03AMY,
                PedHash.Business04AFY,
                PedHash.Business02AFY
            };

            PedHash pedHash = (PedHash)ped.Model.Hash;
            foreach (var model in businessModels)
            {
                if (pedHash == model)
                    return true;
            }

            return false;
        }

        private bool IsRichPed(Ped ped)
        {
            // Verifica se está em área rica
            string zone = Function.Call<string>(Hash.GET_NAME_OF_ZONE, 
                ped.Position.X, ped.Position.Y, ped.Position.Z);

            string[] richZones = new string[]
            {
                "RICHM",  // Richman
                "PBOX",   // Pillbox Hill
                "WVINE",  // West Vinewood
                "VINE",   // Vinewood
                "BEACH"   // Vespucci Beach (turistas)
            };

            foreach (var richZone in richZones)
            {
                if (zone.Contains(richZone))
                    return true;
            }

            return false;
        }

        private bool IsPoorPed(Ped ped)
        {
            // Modelos de homeless e pobres
            PedHash[] poorModels = new PedHash[]
            {
                PedHash.Tramp01,
                PedHash.Tramp01AFM,
                PedHash.TrampBeac01AMM
            };

            PedHash pedHash = (PedHash)ped.Model.Hash;
            foreach (var model in poorModels)
            {
                if (pedHash == model)
                    return true;
            }

            return false;
        }

        public bool IsValid()
        {
            return Owner != null && Owner.Exists() && Owner.IsAlive;
        }
    }

    /// <summary>
    /// Classificação econômica do NPC.
    /// </summary>
    public enum PedType
    {
        Poor,       // $10-$80
        Average,    // $50-$200
        Rich,       // $150-$600
        Business    // $200-$800
    }
}
