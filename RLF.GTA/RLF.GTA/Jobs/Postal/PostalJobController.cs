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
        private PostalBikeManager _bikeManager;
        private PostalTaskManager _taskManager;
        private PostalLocationProvider _locationProvider;

        private enum PostalState
        {
            Idle,
            NeedBike,
            DrivingToPickup,
            OnDelivery
        }

        private PostalState _currentState;
        private Vector3 _pickupLocation;
        private Blip _pickupBlip;

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

                case PostalState.NeedBike:
                    HandleNeedBike();
                    break;

                case PostalState.DrivingToPickup:
                    HandleDrivingToPickup();
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

                _bikeManager = new PostalBikeManager();
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
                StartPickupPhase();
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
                    StartPickupPhase();
                }
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PostalController] Erro no evento", ex);
            }
        }

        private void StartPickupPhase()
        {
            try
            {
                _pickupLocation = _locationProvider.GetRandomPickupLocation();
                _currentState = PostalState.NeedBike;

                float distance = Game.Player.Character.Position.DistanceTo(_pickupLocation);

                RLFDebug.Info(DebugChannel.System, $"[PostalController] Pickup: {_pickupLocation} (Distância: {distance:F0}m)");

                if (_pickupBlip != null && _pickupBlip.Exists())
                {
                    _pickupBlip.Delete();
                    _pickupBlip = null;
                }

                _pickupBlip = World.CreateBlip(_pickupLocation);

                if (_pickupBlip == null || !_pickupBlip.Exists())
                {
                    int blipHandle = Function.Call<int>(Hash.ADD_BLIP_FOR_COORD,
                        _pickupLocation.X,
                        _pickupLocation.Y,
                        _pickupLocation.Z);

                    if (blipHandle != 0)
                    {
                        _pickupBlip = new Blip(blipHandle);
                    }
                }

                if (_pickupBlip == null || !_pickupBlip.Exists())
                {
                    RLFDebug.Error(DebugChannel.System, "[PostalController] Falha ao criar blip");
                    global::GTA.UI.Notification.Show("~r~Erro ao marcar local de retirada");
                    return;
                }

                Function.Call(Hash.SET_BLIP_SPRITE, _pickupBlip.Handle, (int)BlipSprite.Standard);
                Function.Call(Hash.SET_BLIP_COLOUR, _pickupBlip.Handle, (int)BlipColor.Blue);
                Function.Call(Hash.SET_BLIP_SCALE, _pickupBlip.Handle, 1.2f);
                Function.Call(Hash.SET_BLIP_AS_SHORT_RANGE, _pickupBlip.Handle, false);
                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "Retirada de Bicicleta");
                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, _pickupBlip.Handle);

                World.WaypointPosition = _pickupLocation;

                global::GTA.UI.Notification.Show(
                    $"~b~Ponto de Retirada Marcado~w~\n" +
                    $"Dirija-se ao local para pegar sua bicicleta\n" +
                    $"~y~{_job.CurrentShift.TasksTotal}~w~ entregas disponíveis\n" +
                    $"~g~Não é necessário CNH!"
                );

                RLFDebug.Info(DebugChannel.System, $"[PostalController] Blip criado com sucesso");
            }
            catch (Exception ex)
            {
                global::GTA.UI.Notification.Show("~r~Erro ao iniciar turno");
                RLFDebug.Error(DebugChannel.System, "[PostalController] Erro ao iniciar pickup", ex);
            }
        }

        private void HandleNeedBike()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            float distance = player.Position.DistanceTo(_pickupLocation);

            if (distance < 5f)
            {
                global::GTA.UI.Screen.ShowHelpTextThisFrame(
                    "Pressione ~INPUT_CONTEXT~ para pegar a bicicleta dos correios"
                );

                if (Game.IsKeyPressed(System.Windows.Forms.Keys.E))
                {
                    if (_bikeManager.SpawnBike(_pickupLocation))
                    {
                        if (_pickupBlip != null && _pickupBlip.Exists())
                        {
                            _pickupBlip.Delete();
                            _pickupBlip = null;
                        }

                        player.Task.EnterVehicle(_bikeManager.CurrentBike, VehicleSeat.Driver);
                        _currentState = PostalState.DrivingToPickup;

                        global::GTA.UI.Notification.Show(
                            "~g~Bicicleta dos Correios Disponível~w~\n" +
                            "Monte na bicicleta para começar as entregas"
                        );

                        RLFDebug.Info(DebugChannel.System, "[PostalController] Bicicleta spawnada");
                    }
                }
            }
        }

        private void HandleDrivingToPickup()
        {
            Ped player = Game.Player.Character;

            if (!_bikeManager.HasBike)
            {
                global::GTA.UI.Notification.Show("~r~Bicicleta dos correios perdida\n~w~Turno cancelado");
                EndShift();
                return;
            }

            if (!player.IsInVehicle(_bikeManager.CurrentBike))
                return;

            _bikeManager.RemoveBikeBlip();

            Vector3 deliveryAddress = _locationProvider.GetRandomDeliveryAddress();
            _taskManager.StartTask(deliveryAddress);

            _currentState = PostalState.OnDelivery;

            global::GTA.UI.Notification.Show(
                $"~b~Nova Entrega de Correspondência~w~\n" +
                $"Leve a correspondência até o destino marcado\n" +
                $"~y~{_job.CurrentShift.TasksRemaining}~w~ entregas restantes"
            );

            RLFDebug.Info(DebugChannel.System, "[PostalController] Primeira entrega iniciada");
        }

        private void HandleOnDelivery()
        {
            Ped player = Game.Player.Character;

            if (!_bikeManager.HasBike)
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
            if (_pickupBlip != null && _pickupBlip.Exists())
            {
                _pickupBlip.Delete();
                _pickupBlip = null;
            }

            _taskManager.Cleanup();
            _bikeManager.Cleanup();

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
                if (_pickupBlip != null && _pickupBlip.Exists())
                    _pickupBlip.Delete();

                _taskManager?.Cleanup();
                _bikeManager?.Cleanup();
            }
            catch { }
        }
    }
}