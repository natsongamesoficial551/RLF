using GTA;
using GTA.Native;
using GTA.Math;
using GTA.UI;
using System;
using System.Drawing;

namespace RLF.GTA.AmmuNation
{
    /// <summary>
    /// Sistema principal de gerenciamento das Ammu-Nations
    /// </summary>
    public class AmmuNationSystem
    {
        // Configurações
        private const float INTERACTION_RADIUS = 3.5f;
        private const float EXIT_INTERACTION_RADIUS = 1.5f;
        private const int FADE_DURATION_MS = 500;

        // Posição fixa da saída dentro do interior
        private static readonly Vector3 INTERIOR_EXIT_POSITION = new Vector3(17.456f, -1114.646f, 29.809f);

        // Debug
        private const bool DEBUG_MODE = false;
        private int _lastDebugTime = 0;
        private const int DEBUG_INTERVAL = 2000;

        // Estado
        private bool _isPlayerInside = false;
        private Vector3 _returnPosition = Vector3.Zero;
        private bool _isFading = false;
        private AmmuNationLocation _nearestLocation = null;

        /// <summary>
        /// Atualização principal do sistema (chamado a cada frame)
        /// </summary>
        public void Tick()
        {
            try
            {
                // Não processar durante fade
                if (_isFading)
                    return;

                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                Vector3 playerPos = player.Position;

                // Verificar se está dentro do interior
                if (_isPlayerInside)
                {
                    HandleInteriorLogic(player, playerPos);
                }
                else
                {
                    HandleExteriorLogic(player, playerPos);
                }
            }
            catch (Exception ex)
            {
                // Log silencioso - não crashar o mod
                global::GTA.UI.Notification.Show($"~r~AmmuNation Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lógica quando o player está do lado de fora
        /// </summary>
        private void HandleExteriorLogic(Ped player, Vector3 playerPos)
        {
            _nearestLocation = null;
            float nearestDistance = float.MaxValue;

            // Verificar proximidade com qualquer entrada
            foreach (var location in AmmuNationLocations.ExternalLocations)
            {
                float distance = playerPos.DistanceTo(location.Position);

                // Debug visual - marker na porta
                if (DEBUG_MODE)
                {
                    World.DrawMarker(
                        MarkerType.VerticalCylinder,
                        location.Position,
                        Vector3.Zero,
                        Vector3.Zero,
                        new Vector3(0.4f, 0.4f, 0.8f),
                        Color.FromArgb(120, 0, 255, 0)
                    );
                }

                // Atualizar a mais próxima
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    _nearestLocation = location;
                }
            }

            // Verificar se está no raio da loja mais próxima
            if (_nearestLocation != null && nearestDistance <= INTERACTION_RADIUS)
            {
                // Mostrar prompt
                DisplayHelpText("Pressione ~INPUT_CONTEXT~ para entrar na Ammu-Nation");

                // Verificar input
                if (Game.IsControlJustPressed(Control.Context))
                {
                    EnterAmmuNation(player, _nearestLocation);
                    return;
                }
            }
        }

        /// <summary>
        /// Lógica quando o player está dentro do interior
        /// </summary>
        private void HandleInteriorLogic(Ped player, Vector3 playerPos)
        {
            // Verificar distância até o ponto de saída fixo
            float distanceToExit = playerPos.DistanceTo(INTERIOR_EXIT_POSITION);

            // Marker visual no ponto de saída (debug)
            if (DEBUG_MODE)
            {
                World.DrawMarker(
                    MarkerType.VerticalCylinder,
                    INTERIOR_EXIT_POSITION,
                    Vector3.Zero,
                    Vector3.Zero,
                    new Vector3(0.5f, 0.5f, 1.0f),
                    Color.FromArgb(120, 0, 255, 0)
                );
            }

            // Mostrar prompt de saída APENAS quando estiver no raio correto
            if (distanceToExit <= EXIT_INTERACTION_RADIUS)
            {
                DisplayHelpText("Pressione ~INPUT_CONTEXT~ para sair da Ammu-Nation");

                if (Game.IsControlJustPressed(Control.Context))
                {
                    ExitAmmuNation(player);
                }
            }
        }

        /// <summary>
        /// Teleporta o player para dentro da Ammu-Nation
        /// </summary>
        private void EnterAmmuNation(Ped player, AmmuNationLocation entrance)
        {
            // Proteção contra duplo input
            if (_isFading)
                return;

            try
            {
                _isFading = true;

                // Salvar posição de retorno
                _returnPosition = entrance.Position;

                // Fade out
                Screen.FadeOut(FADE_DURATION_MS);
                Script.Wait(FADE_DURATION_MS + 100);

                // Teleportar
                player.Position = AmmuNationLocations.InteriorPosition;
                player.Heading = AmmuNationLocations.InteriorHeading;

                // Garantir que está no chão
                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET,
                    player.Handle,
                    AmmuNationLocations.InteriorPosition.X,
                    AmmuNationLocations.InteriorPosition.Y,
                    AmmuNationLocations.InteriorPosition.Z,
                    false, false, false);

                // Estabilizar física (evita micro-drop)
                Function.Call(Hash.FREEZE_ENTITY_POSITION, player.Handle, true);
                Script.Wait(50);
                Function.Call(Hash.FREEZE_ENTITY_POSITION, player.Handle, false);

                // Marcar como dentro
                _isPlayerInside = true;

                Script.Wait(200);

                // Fade in
                Screen.FadeIn(FADE_DURATION_MS);
                Script.Wait(FADE_DURATION_MS);

                _isFading = false;

                global::GTA.UI.Notification.Show($"~g~Bem-vindo à Ammu-Nation");
            }
            catch (Exception ex)
            {
                _isFading = false;
                Screen.FadeIn(0); // Garantir que volta
                global::GTA.UI.Notification.Show($"~r~Erro ao entrar: {ex.Message}");
            }
        }

        /// <summary>
        /// Teleporta o player para fora da Ammu-Nation
        /// </summary>
        private void ExitAmmuNation(Ped player)
        {
            // Proteção contra duplo input
            if (_isFading)
                return;

            try
            {
                _isFading = true;

                // Fade out
                Screen.FadeOut(FADE_DURATION_MS);
                Script.Wait(FADE_DURATION_MS + 100);

                // Teleportar de volta
                player.Position = _returnPosition;

                // Garantir que está no chão
                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET,
                    player.Handle,
                    _returnPosition.X,
                    _returnPosition.Y,
                    _returnPosition.Z,
                    false, false, false);

                // Estabilizar física (evita micro-drop)
                Function.Call(Hash.FREEZE_ENTITY_POSITION, player.Handle, true);
                Script.Wait(50);
                Function.Call(Hash.FREEZE_ENTITY_POSITION, player.Handle, false);

                // Marcar como fora
                _isPlayerInside = false;

                Script.Wait(200);

                // Fade in
                Screen.FadeIn(FADE_DURATION_MS);
                Script.Wait(FADE_DURATION_MS);

                _isFading = false;

                global::GTA.UI.Notification.Show($"~y~Você saiu da Ammu-Nation");
            }
            catch (Exception ex)
            {
                _isFading = false;
                Screen.FadeIn(0); // Garantir que volta
                global::GTA.UI.Notification.Show($"~r~Erro ao sair: {ex.Message}");
            }
        }

        /// <summary>
        /// Exibe texto de ajuda na tela (chamado a cada frame quando necessário)
        /// </summary>
        private void DisplayHelpText(string message)
        {
            try
            {
                // Método correto - deve ser chamado TODO FRAME para não piscar
                global::GTA.UI.Screen.ShowHelpTextThisFrame(message);
            }
            catch
            {
                // Fallback silencioso
            }
        }

        /// <summary>
        /// Mostra informações de debug na tela
        /// </summary>
        private void ShowDebugInfo(Vector3 playerPos, float nearestDistance)
        {
            int now = Game.GameTime;
            if (now - _lastDebugTime < DEBUG_INTERVAL)
                return;

            _lastDebugTime = now;

            string debugMsg = $"~b~[AMMUNATION DEBUG]~w~\n";
            debugMsg += $"Player Pos: {playerPos.X:F1}, {playerPos.Y:F1}, {playerPos.Z:F1}\n";
            debugMsg += $"Total Lojas: {AmmuNationLocations.ExternalLocations.Count}\n";

            if (_nearestLocation != null)
            {
                debugMsg += $"Mais Próxima: ~y~{_nearestLocation.Name}~w~\n";
                debugMsg += $"Distância: ~o~{nearestDistance:F2}m~w~\n";
                debugMsg += $"Raio: {INTERACTION_RADIUS}m\n";

                if (nearestDistance <= INTERACTION_RADIUS)
                {
                    debugMsg += $"~g~DENTRO DO RAIO!~w~";
                }
                else
                {
                    debugMsg += $"~r~FORA DO RAIO~w~";
                }
            }

            global::GTA.UI.Notification.Show(debugMsg);
        }
    }
}