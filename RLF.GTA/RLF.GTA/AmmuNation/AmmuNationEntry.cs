using GTA;
using System;

namespace RLF.GTA.AmmuNation
{
    /// <summary>
    /// Ponto de entrada do mod Ammu-Nation
    /// </summary>
    public class AmmuNationEntry : Script
    {
        private AmmuNationSystem _ammuNationSystem;

        /// <summary>
        /// Construtor - inicializa o sistema
        /// </summary>
        public AmmuNationEntry()
        {
            try
            {
                // Inicializar sistema
                _ammuNationSystem = new AmmuNationSystem();

                // Registrar evento de tick
                Tick += OnTick;

                // Notificar inicialização
                global::GTA.UI.Notification.Show("~g~Ammu-Nation System iniciado com sucesso!");
            }
            catch (Exception ex)
            {
                global::GTA.UI.Notification.Show($"~r~Erro ao iniciar Ammu-Nation: {ex.Message}");
            }
        }

        /// <summary>
        /// Evento de tick (chamado a cada frame)
        /// </summary>
        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                _ammuNationSystem?.Tick();
            }
            catch (Exception ex)
            {
                global::GTA.UI.Notification.Show($"~r~AmmuNation Tick Error: {ex.Message}");
            }
        }
    }
}
