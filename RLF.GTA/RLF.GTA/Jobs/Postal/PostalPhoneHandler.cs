using GTA;
using RLF.Core;
using RLF.Core.Debug;
using RLF.Core.Jobs.Core;
using RLF.Core.Jobs.Enums;
using RLF.GTA.Jobs.Postal;
using System;

namespace RLF.GTA.Jobs.Postal
{
    public sealed class PostalPhoneHandler
    {
        public void OnContactCalled()
        {
            try
            {
                var core = RLFCore.Instance;
                if (core == null || core.State != CoreState.Running)
                {
                    global::GTA.UI.Notification.Show("~r~Sistema indisponível\n~w~Tente novamente em alguns segundos");
                    return;
                }

                var economy = GTA.CoreIntegration.EconomyBridge.Current;
                if (economy == null)
                {
                    global::GTA.UI.Notification.Show("~r~Sistema indisponível");
                    return;
                }

                var jobSystem = core.Systems.Get("JobSystem") as JobSystem;
                if (jobSystem == null)
                {
                    global::GTA.UI.Notification.Show("~r~Sistema de empregos indisponível");
                    RLFDebug.Error(DebugChannel.System, "[PostalPhone] JobSystem não encontrado");
                    return;
                }

                var postalJob = jobSystem.Registry.Get(JobType.Delivery) as PostalJob;
                if (postalJob == null)
                {
                    postalJob = new PostalJob(core.Logger, core.EventManager, economy);
                    jobSystem.Registry.Register(postalJob);
                }

                bool started = jobSystem.TryStartShift(JobType.Delivery);

                if (started)
                {
                    global::GTA.UI.Notification.Show(
                        "~g~Turno de Carteiro Iniciado~w~\n" +
                        "Dirija-se ao ponto de retirada da bicicleta\n" +
                        "~y~Não é necessário CNH para este trabalho"
                    );
                    RLFDebug.Info(DebugChannel.System, "[PostalPhone] Turno iniciado com sucesso");
                }
                else
                {
                    string message = postalJob.GetStatusMessage();
                    global::GTA.UI.Notification.Show($"~y~{message}");
                    RLFDebug.Info(DebugChannel.System, $"[PostalPhone] Turno não disponível: {message}");
                }
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PostalPhone] Erro ao processar ligação", ex);
                global::GTA.UI.Notification.Show("~r~Erro ao processar solicitação");
            }
        }
    }
}
