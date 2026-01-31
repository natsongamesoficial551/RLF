using System;
using GTA;
using GTA.Native;
using RLF.Core.CharacterCreator.Enums;

namespace RLF.GTA.CharacterCreator.Core
{
    /// <summary>
    /// Hashes dos modelos freemode do GTA V
    /// </summary>
    public static class FreemodePeds
    {
        public const string MALE = "mp_m_freemode_01";
        public const string FEMALE = "mp_f_freemode_01";

        public static PedHash GetHash(CharacterGender gender)
        {
            return gender == CharacterGender.Male 
                ? PedHash.FreemodeMale01 
                : PedHash.FreemodeFemale01;
        }

        public static string GetModelName(CharacterGender gender)
        {
            return gender == CharacterGender.Male ? MALE : FEMALE;
        }

        public static bool IsFreemodeModel(Ped ped)
        {
            if (ped == null) return false;
            var model = ped.Model;
            return model == PedHash.FreemodeMale01 || model == PedHash.FreemodeFemale01;
        }

        /// <summary>
        /// Carrega o modelo freemode na memória
        /// </summary>
        public static bool LoadModel(CharacterGender gender)
        {
            var model = new Model(GetHash(gender));
            
            if (!model.IsValid) return false;
            
            model.Request();
            
            int timeout = 1000;
            while (!model.IsLoaded && timeout > 0)
            {
                Script.Yield();
                timeout -= 10;
            }
            
            return model.IsLoaded;
        }

        /// <summary>
        /// Libera o modelo da memória
        /// </summary>
        public static void UnloadModel(CharacterGender gender)
        {
            var model = new Model(GetHash(gender));
            model.MarkAsNoLongerNeeded();
        }
    }
}