using System;
using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core;
using RLF.Core.Debug;
using RLF.Core.Jobs.Core;
using RLF.Core.Jobs.Enums;
using RLF.GTA.CoreIntegration;

namespace RLF.GTA.Jobs.Delivery
{
    public sealed class DeliveryJobController : Script
    {
        private DeliveryJob _job;
        private DeliveryVehicleManager _vehicleManager;
        private DeliveryTaskManager _taskManager;
        private DeliveryLocationProvider _locationProvider;

        private enum DeliveryState
        {
            Idle,
            NeedVehicle,
            DrivingToPickup,
            OnDelivery
        }

        private DeliveryState _currentState;
        private Vector3 _pickupLocation;
        private Blip _pickupBlip;

        private bool _initialized;
        private bool _lastShiftWasActive;

        public DeliveryJobController()
        {
            Tick += OnTick;
            Aborted += OnAborted;

            _currentState = DeliveryState.Idle;
            _initialized = false;
            _lastShiftWasActive = false;

            RLFDebug.Info(DebugChannel.System, "[DeliveryController] Carregado");
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
                case DeliveryState.Idle:
                    break;

                case DeliveryState.NeedVehicle:
                    HandleNeedVehicle();
                    break;

                case DeliveryState.DrivingToPickup:
                    HandleDrivingToPickup();
                    break;

                case DeliveryState.OnDelivery:
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
                    RLFDebug.Error(DebugChannel.System, "[DeliveryController] JobSystem não encontrado");
                    return;
                }

                _job = jobSystem.Registry.Get(JobType.Delivery) as DeliveryJob;

                if (_job == null)
                {
                    _job = new DeliveryJob(core.Logger, core.EventManager, economy);
                    jobSystem.Registry.Register(_job);
                    RLFDebug.Info(DebugChannel.System, "[DeliveryController] DeliveryJob criado e registrado");
                }

                core.EventManager.Subscribe("job:shift_started",
                    new System.EventHandler<RLF.Core.Events.EventArgs.RLFEventArgs>(OnShiftStarted));

                _vehicleManager = new DeliveryVehicleManager();
                _taskManager = new DeliveryTaskManager();
                _locationProvider = new DeliveryLocationProvider();

                _initialized = true;

                RLFDebug.Info(DebugChannel.System, "[DeliveryController] Inicializado com sucesso");
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[DeliveryController] Erro na inicialização", ex);
            }
        }

        private void CheckForActiveShift()
        {
            bool shiftIsActive = _job.Status == JobStatus.OnShift &&
                                _job.CurrentShift != null &&
                                _job.CurrentShift.IsActive;

            if (shiftIsActive && !_lastShiftWasActive && _currentState == DeliveryState.Idle)
            {
                RLFDebug.Info(DebugChannel.System, "[DeliveryController] Turno ativo detectado");
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

                RLFDebug.Info(DebugChannel.System, "[DeliveryController] Evento shift_started recebido");

                if (_currentState == DeliveryState.Idle)
                {
                    StartPickupPhase();
                }
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[DeliveryController] Erro no evento", ex);
            }
        }

        private void StartPickupPhase()
        {
            try
            {
                _pickupLocation = _locationProvider.GetRandomPickupLocation();
                _currentState = DeliveryState.NeedVehicle;

                float distance = Game.Player.Character.Position.DistanceTo(_pickupLocation);

                RLFDebug.Info(DebugChannel.System, $"[DeliveryController] Pickup: {_pickupLocation} (Distância: {distance:F0}m)");

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
                    RLFDebug.Error(DebugChannel.System, "[DeliveryController] Falha ao criar blip");
                    global::GTA.UI.Notification.Show("~r~Erro ao marcar local de retirada");
                    return;
                }

                Function.Call(Hash.SET_BLIP_SPRITE, _pickupBlip.Handle, (int)BlipSprite.Standard);
                Function.Call(Hash.SET_BLIP_COLOUR, _pickupBlip.Handle, (int)BlipColor.Blue);
                Function.Call(Hash.SET_BLIP_SCALE, _pickupBlip.Handle, 1.2f);
                Function.Call(Hash.SET_BLIP_AS_SHORT_RANGE, _pickupBlip.Handle, false);
                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "Ponto de Retirada");
                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, _pickupBlip.Handle);

                World.WaypointPosition = _pickupLocation;

                global::GTA.UI.Notification.Show(
                    $"~b~Ponto de Retirada Marcado~w~\n" +
                    $"Dirija-se ao local para pegar sua moto\n" +
                    $"~y~{_job.CurrentShift.TasksTotal}~w~ entregas disponíveis"
                );

                RLFDebug.Info(DebugChannel.System, $"[DeliveryController] Blip criado com sucesso");
            }
            catch (Exception ex)
            {
                global::GTA.UI.Notification.Show("~r~Erro ao iniciar turno");
                RLFDebug.Error(DebugChannel.System, "[DeliveryController] Erro ao iniciar pickup", ex);
            }
        }

        private void HandleNeedVehicle()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            float distance = player.Position.DistanceTo(_pickupLocation);

            if (distance < 5f)
            {
                global::GTA.UI.Screen.ShowHelpTextThisFrame(
                    "Pressione ~INPUT_CONTEXT~ para pegar a moto de entrega"
                );

                if (Game.IsKeyPressed(System.Windows.Forms.Keys.E))
                {
                    if (_vehicleManager.SpawnVehicle(_pickupLocation))
                    {
                        if (_pickupBlip != null && _pickupBlip.Exists())
                        {
                            _pickupBlip.Delete();
                            _pickupBlip = null;
                        }

                        player.Task.EnterVehicle(_vehicleManager.CurrentVehicle, VehicleSeat.Driver);
                        _currentState = DeliveryState.DrivingToPickup;

                        global::GTA.UI.Notification.Show(
                            "~g~Moto de Entrega Disponível~w~\n" +
                            "Entre na moto para começar as entregas"
                        );

                        RLFDebug.Info(DebugChannel.System, "[DeliveryController] Moto spawnada");
                    }
                }
            }
        }

        private void HandleDrivingToPickup()
        {
            Ped player = Game.Player.Character;

            if (!_vehicleManager.HasVehicle)
            {
                global::GTA.UI.Notification.Show("~r~Veículo de entrega perdido\n~w~Turno cancelado");
                EndShift();
                return;
            }

            if (!player.IsInVehicle(_vehicleManager.CurrentVehicle))
                return;

            _vehicleManager.RemoveVehicleBlip();

            Vector3 deliveryAddress = _locationProvider.GetRandomDeliveryAddress();
            _taskManager.StartTask(deliveryAddress);

            _currentState = DeliveryState.OnDelivery;

            global::GTA.UI.Notification.Show(
                $"~b~Nova Entrega~w~\n" +
                $"Leve o pedido até o destino marcado\n" +
                $"~y~{_job.CurrentShift.TasksRemaining}~w~ entregas restantes"
            );

            RLFDebug.Info(DebugChannel.System, "[DeliveryController] Primeira entrega iniciada");
        }

        private void HandleOnDelivery()
        {
            Ped player = Game.Player.Character;

            if (!_vehicleManager.HasVehicle)
            {
                global::GTA.UI.Notification.Show("~r~Veículo de entrega perdido\n~w~Turno cancelado");
                EndShift();
                return;
            }

            float distance = player.Position.DistanceTo(_taskManager.CurrentDestination);

            if (distance < 10f)
            {
                global::GTA.UI.Screen.ShowHelpTextThisFrame(
                    $"Pressione ~INPUT_CONTEXT~ para entregar o pedido~n~Distância: {distance:F1}m"
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

            global::GTA.UI.Notification.Show("~g~Entrega Concluída!");

            RLFDebug.Info(DebugChannel.System, $"[DeliveryController] Entrega completa - Restantes: {_job.CurrentShift.TasksRemaining}");

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
            _vehicleManager.Cleanup();

            if (_job.CurrentShift.IsCompleted)
            {
                decimal totalEarned = _job.CurrentShift.TasksCompleted * _job.PaymentSettings.BasePayPerTask +
                                      _job.PaymentSettings.ShiftCompletionBonus;

                global::GTA.UI.Notification.Show(
                    $"~g~Turno Concluído!~w~\n" +
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

            _currentState = DeliveryState.Idle;
            _lastShiftWasActive = false;

            RLFDebug.Info(DebugChannel.System, "[DeliveryController] Turno encerrado");
        }

        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                if (_pickupBlip != null && _pickupBlip.Exists())
                    _pickupBlip.Delete();

                _taskManager?.Cleanup();
                _vehicleManager?.Cleanup();
            }
            catch { }
        }
    }
}