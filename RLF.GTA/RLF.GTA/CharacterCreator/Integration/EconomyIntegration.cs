using System;
using RLF.Core.CharacterCreator.Enums;

namespace RLF.GTA.CharacterCreator.Integration
{
    /// <summary>
    /// Integração com sistema de economia (custos para mudanças)
    /// </summary>
    public static class EconomyIntegration
    {
        private static bool _isEnabled;
        private static bool _isInitialized;

        /// <summary>
        /// Custos para diferentes tipos de mudança
        /// </summary>
        public static class Costs
        {
            public static int HairChange = 50;
            public static int HairColorChange = 25;
            public static int FacialHairChange = 30;
            public static int MakeupChange = 40;
            public static int FaceChange = 500;
            public static int FullMakeover = 2500;
            public static int ClothingChange = 0;        // Grátis
            public static int AccessoryChange = 0;       // Grátis
            public static int GenderChange = 10000;      // Muito caro
        }

        /// <summary>
        /// Se a integração está habilitada
        /// </summary>
        public static bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; }
        }

        /// <summary>
        /// Inicializa a integração
        /// </summary>
        public static void Initialize(bool enabled = false)
        {
            if (_isInitialized) return;

            _isEnabled = enabled;
            _isInitialized = true;
        }

        /// <summary>
        /// Verifica se o jogador pode pagar por uma mudança
        /// </summary>
        public static bool CanAfford(int cost)
        {
            if (!_isEnabled) return true;

            // TODO: Integrar com seu sistema de economia
            // return EconomySystem.GetPlayerMoney() >= cost;

            return true; // Por enquanto, sempre pode pagar
        }

        /// <summary>
        /// Cobra o jogador por uma mudança
        /// </summary>
        public static bool Charge(int cost, string description = "")
        {
            if (!_isEnabled) return true;
            if (cost <= 0) return true;

            // TODO: Integrar com seu sistema de economia
            // if (!CanAfford(cost)) return false;
            // EconomySystem.RemoveMoney(cost, description);

            System.Diagnostics.Debug.WriteLine(string.Format(
                "[EconomyIntegration] Cobrando ${0} - {1}",
                cost,
                description
            ));

            return true;
        }

        /// <summary>
        /// Obtém o custo para um tipo de mudança
        /// </summary>
        public static int GetCost(ChangeType changeType)
        {
            switch (changeType)
            {
                case ChangeType.Hair:
                    return Costs.HairChange;
                case ChangeType.HairColor:
                    return Costs.HairColorChange;
                case ChangeType.FacialHair:
                    return Costs.FacialHairChange;
                case ChangeType.Makeup:
                    return Costs.MakeupChange;
                case ChangeType.Face:
                    return Costs.FaceChange;
                case ChangeType.FullMakeover:
                    return Costs.FullMakeover;
                case ChangeType.Clothing:
                    return Costs.ClothingChange;
                case ChangeType.Accessory:
                    return Costs.AccessoryChange;
                case ChangeType.Gender:
                    return Costs.GenderChange;
                default:
                    return 0;
            }
        }
    }

    /// <summary>
    /// Tipos de mudança para cobrança
    /// </summary>
    public enum ChangeType
    {
        Hair,
        HairColor,
        FacialHair,
        Makeup,
        Face,
        FullMakeover,
        Clothing,
        Accessory,
        Gender
    }
}