using GTA;
using RLF.Core;
using RLF.Core.Debug;
using RLF.Core.Identity;
using RLF.Core.Identity.Enums;
using RLF.Core.Jobs.Core;
using RLF.Core.Jobs.Enums;
using RLF.GTA.Jobs.Delivery;
using System;

namespace RLF.GTA.Jobs.Delivery
{
    public sealed class DeliveryPhoneHandler
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

                // ✅ VALIDAÇÃO DE CNH (igual ao Uber)
                var docSystem = core.Systems.Get("DocumentSystem") as DocumentSystem;
                if (docSystem == null || !docSystem.HasValidLicense(LicenseType.DriverCar))
                {
                    global::GTA.UI.Notification.Show(
                        "~r~❌ CNH válida necessária para trabalhar como entregador de moto"
                    );
                    RLFDebug.Warning(DebugChannel.System, "[DeliveryPhone] CNH inválida ou ausente");
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
                    RLFDebug.Error(DebugChannel.System, "[DeliveryPhone] JobSystem não encontrado");
                    return;
                }

                var deliveryJob = jobSystem.Registry.Get(JobType.Delivery) as DeliveryJob;
                if (deliveryJob == null)
                {
                    deliveryJob = new DeliveryJob(core.Logger, core.EventManager, economy);
                    jobSystem.Registry.Register(deliveryJob);
                }

                bool started = jobSystem.TryStartShift(JobType.Delivery);

                if (started)
                {
                    global::GTA.UI.Notification.Show(
                        "~g~Turno Iniciado~w~\n" +
                        "Dirija-se ao ponto de retirada da moto"
                    );
                    RLFDebug.Info(DebugChannel.System, "[DeliveryPhone] Turno iniciado com sucesso");
                }
                else
                {
                    string message = deliveryJob.GetStatusMessage();
                    global::GTA.UI.Notification.Show($"~y~{message}");
                    RLFDebug.Info(DebugChannel.System, $"[DeliveryPhone] Turno não disponível: {message}");
                }
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[DeliveryPhone] Erro ao processar ligação", ex);
                global::GTA.UI.Notification.Show("~r~Erro ao processar solicitação");
            }
        }
    }
}