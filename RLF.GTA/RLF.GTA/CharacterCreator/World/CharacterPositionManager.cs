using System;
using GTA;
using GTA.Math;
using RLF.Core.CharacterCreator.Data;

namespace RLF.GTA.CharacterCreator.World
{
    /// <summary>
    /// Gerencia posições de spawn dos personagens
    /// </summary>
    public class CharacterPositionManager
    {
        // Posição padrão para novos personagens
        private static readonly Vector3 DefaultSpawn = new Vector3(-1037.0f, -2737.0f, 20.17f);
        private static readonly float DefaultHeading = 330f;

        /// <summary>
        /// Salva a posição atual do player no personagem
        /// </summary>
        public static void SaveCurrentPosition(CharacterData character)
        {
            if (character == null) return;

            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                character.UpdateLastPosition(
                    player.Position.X,
                    player.Position.Y,
                    player.Position.Z,
                    player.Heading
                );

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"💾 POSIÇÃO SALVA: {character.Name}");
                System.Diagnostics.Debug.WriteLine($"   X: {character.LastPositionX:F2}");
                System.Diagnostics.Debug.WriteLine($"   Y: {character.LastPositionY:F2}");
                System.Diagnostics.Debug.WriteLine($"   Z: {character.LastPositionZ:F2}");
                System.Diagnostics.Debug.WriteLine($"   Heading: {character.LastHeading:F2}");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao salvar posição: {ex.Message}");
            }
        }

        /// <summary>
        /// Teleporta o player para a última posição do personagem
        /// </summary>
        public static bool TeleportToLastPosition(CharacterData character)
        {
            if (character == null) return false;

            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return false;

                Vector3 position = GetLastPosition(character);
                float heading = character.LastHeading;

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"📍 TELEPORTANDO: {character.Name}");
                System.Diagnostics.Debug.WriteLine($"   X: {position.X:F2}");
                System.Diagnostics.Debug.WriteLine($"   Y: {position.Y:F2}");
                System.Diagnostics.Debug.WriteLine($"   Z: {position.Z:F2}");
                System.Diagnostics.Debug.WriteLine($"   Heading: {heading:F2}");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                player.Position = position;
                player.Heading = heading;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao teleportar: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retorna a última posição salva ou posição padrão
        /// </summary>
        public static Vector3 GetLastPosition(CharacterData character)
        {
            if (character == null)
                return DefaultSpawn;

            // Se nunca foi salvo (valores padrão 0), usa spawn padrão
            if (character.LastPositionX == 0f && character.LastPositionY == 0f && character.LastPositionZ == 0f)
                return DefaultSpawn;

            return new Vector3(
                character.LastPositionX,
                character.LastPositionY,
                character.LastPositionZ
            );
        }

        /// <summary>
        /// Verifica se a posição é válida (não é underwater, não é fora do mapa)
        /// </summary>
        public static bool IsValidPosition(Vector3 position)
        {
            // Verifica se está dentro dos limites do mapa GTA V
            if (position.X < -4000f || position.X > 4000f ||
                position.Y < -4000f || position.Y > 4000f ||
                position.Z < -500f || position.Z > 1500f)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Corrige posição inválida para spawn padrão
        /// </summary>
        public static Vector3 GetSafePosition(CharacterData character)
        {
            Vector3 position = GetLastPosition(character);

            if (!IsValidPosition(position))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Posição inválida detectada, usando spawn padrão");
                return DefaultSpawn;
            }

            return position;
        }

        /// <summary>
        /// Retorna o spawn padrão
        /// </summary>
        public static Vector3 GetDefaultSpawn()
        {
            return DefaultSpawn;
        }

        /// <summary>
        /// Retorna o heading padrão
        /// </summary>
        public static float GetDefaultHeading()
        {
            return DefaultHeading;
        }

        /// <summary>
        /// Reseta posição do personagem para padrão
        /// </summary>
        public static void ResetToDefaultPosition(CharacterData character)
        {
            if (character == null) return;

            character.UpdateLastPosition(
                DefaultSpawn.X,
                DefaultSpawn.Y,
                DefaultSpawn.Z,
                DefaultHeading
            );

            System.Diagnostics.Debug.WriteLine($"🔄 Posição resetada para padrão: {character.Name}");
        }
    }
}