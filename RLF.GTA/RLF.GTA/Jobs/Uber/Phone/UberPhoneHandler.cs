using System;
using GTA;
using RLF.Core;
using RLF.Core.Debug;
using RLF.Core.Identity;
using RLF.Core.Identity.Enums;
using RLF.GTA.Jobs.Uber.Controller;

namespace RLF.GTA.Jobs.Uber.Phone
{
    public sealed class UberPhoneHandler
    {
        public void OnContactCalled()
        {
            try
            {
                var core = RLFCore.Instance;
                if (core == null || core.State != CoreState.Running)
                {
                    global::GTA.UI.Notification.Show("~r~❌ Sistema indisponível");
                    RLFDebug.Error(DebugChannel.System, "[UberPhone] RLFCore não inicializado");
                    return;
                }

                var docSystem = core.Systems.Get("DocumentSystem") as DocumentSystem;
                if (docSystem == null || !docSystem.HasValidLicense(LicenseType.DriverCar))
                {
                    global::GTA.UI.Notification.Show(
                        "~r~❌ CNH válida necessária para trabalhar como motorista Uber"
                    );
                    RLFDebug.Warning(DebugChannel.System, "[UberPhone] CNH inválida ou ausente");
                    return;
                }

                Ped player = Game.Player.Character;
                if (!player.IsInVehicle())
                {
                    global::GTA.UI.Notification.Show(
                        "~r~❌ Entre em um veículo antes de ativar o Uber"
                    );
                    RLFDebug.Warning(DebugChannel.System, "[UberPhone] Jogador não está em veículo");
                    return;
                }

                global::GTA.Vehicle vehicle = player.CurrentVehicle;

                if (player.SeatIndex != VehicleSeat.Driver)
                {
                    global::GTA.UI.Notification.Show(
                        "~r~❌ Você precisa estar dirigindo o veículo"
                    );
                    RLFDebug.Warning(DebugChannel.System, "[UberPhone] Jogador não está no assento do motorista");
                    return;
                }

                var controller = UberJobController.Instance;

                if (controller == null)
                {
                    global::GTA.UI.Notification.Show(
                        "~r~❌ Sistema Uber não inicializado\n~w~Aguarde alguns segundos e tente novamente"
                    );
                    RLFDebug.Error(DebugChannel.System, "[UberPhone] UberJobController instance não encontrada");
                    return;
                }

                bool activated = controller.TryActivateApp(vehicle);

                if (!activated)
                {
                    RLFDebug.Warning(DebugChannel.System, "[UberPhone] Falha na ativação - verifique logs do controller");
                }
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[UberPhone] Exceção fatal durante ativação", ex);
                global::GTA.UI.Notification.Show("~r~❌ Erro crítico ao ativar Uber");
            }
        }
    }
}