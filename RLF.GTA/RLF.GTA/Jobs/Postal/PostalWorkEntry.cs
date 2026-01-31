using GTA;
using System;
using RLF.Core.Debug;

namespace RLF.GTA.Jobs.Postal
{
    /// <summary>
    /// Ponto de entrada do sistema de trabalho de Carteiro
    /// </summary>
    public class PostalWorkEntry : Script
    {
        private PostalWorkSystem _workSystem;
        private PostalJobController _jobController;

        /// <summary>
        /// Construtor - inicializa o sistema
        /// </summary>
        public PostalWorkEntry()
        {
            try
            {
                // Inicializar sistema de pontos de trabalho
                _workSystem = new PostalWorkSystem();

                // Inicializar controlador de job
                _jobController = new PostalJobController();

                // Registrar evento de tick
                Tick += OnTick;
                Aborted += OnAborted;

                RLFDebug.Info(DebugChannel.System, "[PostalWorkEntry] Sistema de Carteiro iniciado");
                global::GTA.UI.Notification.Show("~g~Sistema de Carteiro iniciado!");
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PostalWorkEntry] Erro ao iniciar", ex);
                global::GTA.UI.Notification.Show($"~r~Erro ao iniciar Carteiro: {ex.Message}");
            }
        }

        /// <summary>
        /// Evento de tick (chamado a cada frame)
        /// </summary>
        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                // Atualizar sistema de pontos de trabalho
                _workSystem?.Tick();

                // O jobController tem seu próprio Tick via herança de Script
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PostalWorkEntry] Erro no Tick", ex);
            }
        }

        /// <summary>
        /// Limpeza ao abortar o script
        /// </summary>
        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                _workSystem?.Cleanup();
                RLFDebug.Info(DebugChannel.System, "[PostalWorkEntry] Sistema encerrado");
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PostalWorkEntry] Erro no Aborted", ex);
            }
        }
    }
}
