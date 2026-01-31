using System;
using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core.CharacterCreator.Data;

namespace RLF.GTA.CharacterCreator.Core
{
    public class CharacterRespawnManager
    {
        private bool _isRespawning = false;
        private int _respawnTimer = 0;
        private Vector3 _deathPosition;
        private CharacterData _currentCharacter;

        public void Update()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                // Detectar morte
                if (player.IsDead && !_isRespawning)
                {
                    _isRespawning = true;
                    _respawnTimer = 0;
                    _deathPosition = player.Position;

                    System.Diagnostics.Debug.WriteLine("💀 PLAYER MORREU - Iniciando respawn customizado");
                }

                // Processar respawn
                if (_isRespawning)
                {
                    ProcessRespawn();
                }
            }
            catch { }
        }

        private void ProcessRespawn()
        {
            _respawnTimer++;

            try
            {
                var player = Game.Player.Character;

                // Frame 1-30: Aguardar tela de morte
                if (_respawnTimer < 30)
                {
                    // Desabilitar respawn automático do GTA
                    Function.Call(Hash.IGNORE_NEXT_RESTART, true);
                    return;
                }

                // Frame 30-60: Fade out
                if (_respawnTimer == 30)
                {
                    global::GTA.UI.Screen.FadeOut(1000);
                    return;
                }

                if (_respawnTimer < 60)
                {
                    Function.Call(Hash.IGNORE_NEXT_RESTART, true);
                    return;
                }

                // Frame 60: Reviver e reposicionar
                if (_respawnTimer == 60)
                {
                    // Reviver player
                    if (player.IsDead)
                    {
                        Function.Call(Hash.RESURRECT_PED, player.Handle);
                        player.Health = player.MaxHealth;
                    }

                    // Teleportar para hospital mais próximo
                    Vector3 hospitalPos = GetNearestHospital(_deathPosition);
                    player.Position = hospitalPos;
                    player.Heading = 0f;

                    // Limpar wanted level
                    Function.Call(Hash.CLEAR_PLAYER_WANTED_LEVEL, Game.Player.Handle);

                    // Reaplicar aparência (caso tenha bugado)
                    if (_currentCharacter != null)
                    {
                        var builder = CharacterCreatorSystem.Instance.Manager.Builder;
                        builder.SetPed(player);
                        builder.ApplyFullCharacter(_currentCharacter);
                    }

                    System.Diagnostics.Debug.WriteLine($"✅ RESPAWN: Hospital em ({hospitalPos.X:F2}, {hospitalPos.Y:F2}, {hospitalPos.Z:F2})");
                }

                // Frame 80: Fade in
                if (_respawnTimer == 80)
                {
                    global::GTA.UI.Screen.FadeIn(1000);
                }

                // Frame 100: Finalizar
                if (_respawnTimer >= 100)
                {
                    _isRespawning = false;
                    _respawnTimer = 0;
                    System.Diagnostics.Debug.WriteLine("✅ Respawn concluído");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro no respawn: {ex.Message}");
                _isRespawning = false;
            }
        }

        public void SetCurrentCharacter(CharacterData character)
        {
            _currentCharacter = character;
        }

        private Vector3 GetNearestHospital(Vector3 position)
        {
            // Hospitais principais do GTA V
            Vector3[] hospitals = new Vector3[]
            {
                new Vector3(1839.6f, 3672.93f, 34.28f),      // Sandy Shores
                new Vector3(-247.76f, 6331.23f, 32.43f),     // Paleto Bay
                new Vector3(297.52f, -584.5f, 43.26f),       // Pillbox Hill
                new Vector3(-449.67f, -340.83f, 34.5f),      // Mount Zonah
                new Vector3(1151.21f, -1529.62f, 35.37f),    // St. Fiacre
                new Vector3(-874.64f, -307.71f, 39.58f)      // Portola Drive
            };

            Vector3 nearest = hospitals[0];
            float minDist = position.DistanceTo(nearest);

            foreach (var hospital in hospitals)
            {
                float dist = position.DistanceTo(hospital);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = hospital;
                }
            }

            return nearest;
        }
    }
}