using RLF.Core.CharacterCreator.Data;
using System;

namespace RLF.Core.CharacterCreator.Storage
{
    /// <summary>
    /// Migração de dados de personagem entre versões
    /// </summary>
    public static class CharacterMigration
    {
        public const int CurrentVersion = 1;

        /// <summary>
        /// Migra dados de personagem para a versão atual
        /// </summary>
        public static CharacterData Migrate(CharacterData data)
        {
            if (data == null)
                return null;

            // Garantir que todos os objetos existam
            EnsureDataIntegrity(data);

            // Atualizar timestamp
            data.ModifiedAt = DateTime.Now;

            return data;
        }

        /// <summary>
        /// Garante que todos os sub-objetos existam
        /// </summary>
        private static void EnsureDataIntegrity(CharacterData data)
        {
            if (data.Genetics == null)
                data.Genetics = new CharacterGenetics();

            if (data.FaceFeatures == null)
                data.FaceFeatures = new CharacterFaceFeatures();

            if (data.Overlays == null)
                data.Overlays = new CharacterOverlays();

            if (data.Hair == null)
                data.Hair = new CharacterHair();

            if (data.Clothing == null)
                data.Clothing = new CharacterClothing();

            if (data.Props == null)
                data.Props = new CharacterProps();

            if (string.IsNullOrEmpty(data.Id))
                data.Id = Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(data.Name))
                data.Name = "Personagem";

            if (data.CreatedAt == default(DateTime))
                data.CreatedAt = DateTime.Now;

            if (data.ModifiedAt == default(DateTime))
                data.ModifiedAt = DateTime.Now;
        }

        /// <summary>
        /// Valida se os dados do personagem estão íntegros
        /// </summary>
        public static bool Validate(CharacterData data)
        {
            if (data == null)
                return false;

            if (string.IsNullOrEmpty(data.Id))
                return false;

            if (data.Genetics == null)
                return false;

            if (data.FaceFeatures == null)
                return false;

            if (data.Overlays == null)
                return false;

            if (data.Hair == null)
                return false;

            if (data.Clothing == null)
                return false;

            if (data.Props == null)
                return false;

            return true;
        }

        /// <summary>
        /// Repara dados corrompidos ou incompletos
        /// </summary>
        public static CharacterData Repair(CharacterData data)
        {
            if (data == null)
                data = new CharacterData();

            EnsureDataIntegrity(data);

            // Validar ranges
            if (data.EyeColor < 0 || data.EyeColor > 31)
                data.EyeColor = 0;

            if (data.Genetics.ShapeMix < 0f || data.Genetics.ShapeMix > 1f)
                data.Genetics.ShapeMix = 0.5f;

            if (data.Genetics.SkinMix < 0f || data.Genetics.SkinMix > 1f)
                data.Genetics.SkinMix = 0.5f;

            if (data.Genetics.ShapeFirst < 0 || data.Genetics.ShapeFirst > 45)
                data.Genetics.ShapeFirst = 0;

            if (data.Genetics.ShapeSecond < 0 || data.Genetics.ShapeSecond > 45)
                data.Genetics.ShapeSecond = 0;

            if (data.Hair.Style < 0)
                data.Hair.Style = 0;

            if (data.Hair.PrimaryColor < 0 || data.Hair.PrimaryColor > 63)
                data.Hair.PrimaryColor = 0;

            if (data.Hair.HighlightColor < 0 || data.Hair.HighlightColor > 63)
                data.Hair.HighlightColor = 0;

            data.ModifiedAt = DateTime.Now;

            return data;
        }
    }
}