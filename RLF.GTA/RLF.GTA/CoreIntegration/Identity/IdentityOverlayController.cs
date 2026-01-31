using System;
using GTA;
using RLF.Core.Debug;

namespace RLF.GTA.CoreIntegration.Identity
{
    /// <summary>
    /// Controla abertura/fechamento do IdentityOverlay via tecla J.
    /// </summary>
    public sealed class IdentityOverlayController : Script
    {
        private readonly IdentityOverlay _overlay;
        private bool _keyPressedLastTick;

        public IdentityOverlayController()
        {
            _overlay = new IdentityOverlay();
            _keyPressedLastTick = false;

            Tick += OnTick;

            RLFDebug.Info(
                DebugChannel.System,
                "IdentityOverlayController iniciado (tecla J)"
            );
        }

        private void OnTick(object sender, EventArgs e)
        {
            HandleInput();
            _overlay.Draw();
        }

        private void HandleInput()
        {
            bool keyDown = Game.IsKeyPressed(System.Windows.Forms.Keys.J);

            // ✅ Debounce (evita abrir/fechar várias vezes segurando a tecla)
            if (keyDown && !_keyPressedLastTick)
            {
                _overlay.Toggle();

                RLFDebug.Info(
                    DebugChannel.System,
                    $"IdentityOverlay {(_overlay.IsVisible ? "aberto" : "fechado")}"
                );
            }

            _keyPressedLastTick = keyDown;
        }
    }
}
