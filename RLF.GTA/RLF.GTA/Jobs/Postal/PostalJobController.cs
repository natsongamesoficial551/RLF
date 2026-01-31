using System;
using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core;
using RLF.Core.Debug;
using RLF.Core.Jobs.Core;
using RLF.Core.Jobs.Enums;
using RLF.GTA.CoreIntegration;

namespace RLF.GTA.Jobs.Postal
{
    public sealed class PostalJobController : Script
    {
        private PostalJob _job;
        private PostalTaskManager _taskManager;
        private PostalLocationProvider _locationProvider;

        private enum PostalState
        {
            Idle,
            WaitingForBike,
            OnDelivery
        }

        private PostalState _currentState;
        private Vehicle _currentBike;
        private Blip _bikeBlip;

        private bool _initialized;
        private bool _lastShiftWasActive;

        public PostalJobController()
        {
            Tick += OnTick;
            Aborted += OnAborted;

            _currentState = PostalState.Idle;
            _initialized = false;
            _lastShiftWasActive = false;

            RLFDebug.Info(DebugChannel.System, "[PostalController] Carregado");
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_initialized)
            {
                TryInitialize();
                return;
            }

            if (_job == null)
                return;

            CheckForActiveShift();

            switch (_currentState)
            {
                case PostalState.Idle:
                    break;

                case PostalState.WaitingForBike:
                    HandleWaitingForBike();
                    break;

                case PostalState.OnDelivery:
                    HandleOnDelivery();
                    break;
            }
        }

        private void TryInitialize()
        {
            try
            {
                var core = RLFCore.Instance;
                if (core == null || core.State != CoreState.Running)
                    return;

                var economy = EconomyBridge.Current;
                if (economy == null)
                    return;

                var jobSystem = core.Systems.Get("JobSystem") as JobSystem;
                if (jobSystem == null)
                {
                    RLFDebug.Error(DebugChannel.System, "[PostalController] JobSystem não encontrado");
                    return;
                }

                _job = jobSystem.Registry.Get(JobType.Delivery) as PostalJob;

                if (_job == null)
                {
                    _job = new PostalJob(core.Logger, core.EventManager, economy);
                    jobSystem.Registry.Register(_job);
                    RLFDebug.Info(DebugChannel.System, "[PostalController] PostalJob criado e registrado");
                }

                core.EventManager.Subscribe("job:shift_started",
                    new System.EventHandler<RLF.Core.Events.EventArgs.RLFEventArgs>(OnShiftStarted));

                _taskManager = new PostalTaskManager();
                _locationProvider = new PostalLocationProvider();

                _initialized = true;

                RLFDebug.Info(DebugChannel.System, "[PostalController] Inicializado com sucesso");
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PostalController] Erro na inicialização", ex);
            }
        }

        private void CheckForActiveShift()
        {
            bool shiftIsActive = _job.Status == JobStatus.OnShift &&
                                _job.CurrentShift != null &&
                                _job.CurrentShift.IsActive;

            if (shiftIsActive && !_lastShiftWasActive && _currentState == PostalState.Idle)
            {
                RLFDebug.Info(DebugChannel.System, "[PostalController] Turno ativo detectado");
                StartWaitingForBike();
            }

            _lastShiftWasActive = shiftIsActive;
        }

        private void OnShiftStarted(object sender, RLF.Core.Events.EventArgs.RLFEventArgs e)
        {
            try
            {
                var shiftEvent = e as RLF.Core.Jobs.Events.ShiftStartedEvent;
                if (shiftEvent == null || shiftEvent.JobType != JobType.Delivery)
                    return;

                RLFDebug.Info(DebugChannel.System, "[PostalController] Evento shift_started recebido");

                if (_currentState == PostalState.Idle)
                {
                    StartWaitingForBike();
                }
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PostalController] Erro no evento", ex);
            }
        }

        private void StartWaitingForBike()
        {
            _currentState = PostalState.WaitingForBike;
            RLFDebug.Info(DebugChannel.System, "[PostalController] Aguardando jogador pegar bicicleta");
        }

        private void HandleWaitingForBike()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            // Verificar se o jogador entrou em uma bicicleta
            if (player.IsInVehicle())
            {
                Vehicle vehicle = player.CurrentVehicle;

                // Verificar se é uma bicicleta
                if (vehicle != null && vehicle.Exists() && vehicle.Model.IsBicycle)
                {
                    _currentBike = vehicle;

                    // Remover blip da bicicleta se existir
                    if (_bikeBlip != null && _bikeBlip.Exists())
                    {
                        _bikeBlip.Delete();
                        _bikeBlip = null;
                    }

                    // Iniciar primeira entrega
                    Vector3 deliveryAddress = _locationProvider.GetRandomDeliveryAddress();
                    _taskManager.StartTask(deliveryAddress);

                    _currentState = PostalState.OnDelivery;

                    global::GTA.UI.Notification.Show(
                        $"~b~Primeira Entrega~w~\n" +
                        $"Leve a correspondência até o destino marcado\n" +
                        $"~y~{_job.CurrentShift.TasksRemaining}~w~ entregas restantes"
                    );

                    RLFDebug.Info(DebugChannel.System, "[PostalController] Primeira entrega iniciada");
                }
            }
        }

        private void HandleOnDelivery()
        {
            Ped player = Game.Player.Character;

            if (_currentBike == null || !_currentBike.Exists())
            {
                global::GTA.UI.Notification.Show("~r~Bicicleta dos correios perdida\n~w~Turno cancelado");
                EndShift();
                return;
            }

            float distance = player.Position.DistanceTo(_taskManager.CurrentDestination);

            if (distance < 10f)
            {
                global::GTA.UI.Screen.ShowHelpTextThisFrame(
                    $"Pressione ~INPUT_CONTEXT~ para entregar a correspondência~n~Distância: {distance:F1}m"
                );

                if (Game.IsKeyPressed(System.Windows.Forms.Keys.E))
                {
                    CompleteDelivery();
                }
            }
        }

        private void CompleteDelivery()
        {
            _taskManager.CompleteTask();
            _job.CompleteTask();

            // ✅ O pagamento já foi processado automaticamente pelo JobBase.CompleteTask()
            // O HUD de economia mostrará o popup verde automaticamente

            global::GTA.UI.Notification.Show("~g~Correspondência Entregue!");

            RLFDebug.Info(DebugChannel.System, $"[PostalController] Entrega completa - Restantes: {_job.CurrentShift.TasksRemaining}");

            if (_job.CurrentShift.IsCompleted)
            {
                EndShift();
            }
            else
            {
                Wait(2000);
                Vector3 nextDelivery = _locationProvider.GetRandomDeliveryAddress();
                _taskManager.StartTask(nextDelivery);

                global::GTA.UI.Notification.Show(
                    $"~b~Próxima Entrega~w~\n" +
                    $"~y~{_job.CurrentShift.TasksRemaining}~w~ entregas restantes"
                );
            }
        }

        private void EndShift()
        {
            _taskManager.Cleanup();

            // Remover blip da bicicleta se existir
            if (_bikeBlip != null && _bikeBlip.Exists())
            {
                _bikeBlip.Delete();
                _bikeBlip = null;
            }

            // Deletar bicicleta
            if (_currentBike != null && _currentBike.Exists())
            {
                _currentBike.IsPersistent = false;
                _currentBike.Delete();
                _currentBike = null;
            }

            if (_job.CurrentShift.IsCompleted)
            {
                decimal totalEarned = _job.CurrentShift.TasksCompleted * _job.PaymentSettings.BasePayPerTask +
                                      _job.PaymentSettings.ShiftCompletionBonus;

                global::GTA.UI.Notification.Show(
                    $"~g~Turno de Carteiro Concluído!~w~\n" +
                    $"Entregas realizadas: ~y~{_job.CurrentShift.TasksCompleted}~w~\n" +
                    $"Ganhos totais: ~g~${totalEarned:F2}"
                );

                // ✅ O pagamento já foi processado automaticamente pelo JobBase.CompleteShift()
                // O HUD de economia já está mostrando o valor correto
            }
            else
            {
                global::GTA.UI.Notification.Show("~y~Turno encerrado");
            }

            _currentState = PostalState.Idle;
            _lastShiftWasActive = false;

            RLFDebug.Info(DebugChannel.System, "[PostalController] Turno encerrado");
        }

        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                if (_bikeBlip != null && _bikeBlip.Exists())
                    _bikeBlip.Delete();

                _taskManager?.Cleanup();

                if (_currentBike != null && _currentBike.Exists())
                {
                    _currentBike.IsPersistent = false;
                    _currentBike.Delete();
                }
            }
            catch { }
        }
    }
}