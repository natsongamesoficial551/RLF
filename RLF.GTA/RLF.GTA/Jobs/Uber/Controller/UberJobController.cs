using System;
using GTA;
using GTA.Math;
using RLF.Core;
using RLF.Core.Debug;
using RLF.Core.Economy;
using RLF.GTA.CoreIntegration;
using RLF.GTA.Jobs.Uber.Core;
using RLF.GTA.Jobs.Uber.Passenger;
using RLF.GTA.Jobs.Uber.Ride;
using RLF.GTA.Jobs.Uber.UI;
using RLF.GTA.Jobs.Uber.Vehicle;

namespace RLF.GTA.Jobs.Uber.Controller
{
    public sealed class UberJobController : Script
    {
        private static UberJobController _instance;
        public static UberJobController Instance => _instance;

        private UberJob _job;
        private UberVehicleValidator _vehicleValidator;
        private UberHUD _hud;
        private PassengerNPC _passenger;
        private UberCategorySelector _categorySelector;

        private global::GTA.Vehicle _currentVehicle;
        private Blip _pickupBlip;
        private Blip _destinationBlip;

        private RideRequest _currentRequest;
        private DateTime _requestReceivedAt;
        private RideCategory _selectedCategory;

        private bool _initialized;
        private bool _selectingCategory;
        private bool _waitingForAccept;
        private bool _drivingToPickup;
        private bool _passengerOnBoard;
        private bool _drivingToDestination;

        private float _lastVehicleHealth;
        private Vector3 _lastPosition;
        private DateTime _rideStartTime;

        public UberJobController()
        {
            if (_instance != null)
            {
                RLFDebug.Warning(DebugChannel.System, "[UberController] Instância duplicada - abortando");
                Abort();
                return;
            }

            _instance = this;
            _initialized = false;
            _selectingCategory = false;

            Tick += OnTick;
            Aborted += OnAborted;

            RLFDebug.Info(DebugChannel.System, "[UberController] Script carregado");
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

            if (_selectingCategory)
            {
                _categorySelector.Process();
                return;
            }

            _hud?.Draw(_job.Account, _job.RideManager.CurrentRide);

            if (_waitingForAccept)
            {
                HandleRequestAcceptance();
            }
            else if (_drivingToPickup)
            {
                HandleDrivingToPickup();
            }
            else if (_passengerOnBoard && _drivingToDestination)
            {
                HandleDrivingToDestination();
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

                _job = new UberJob(core.Logger, core.EventManager, economy);
                _vehicleValidator = new UberVehicleValidator(core.Logger);
                _hud = new UberHUD();
                _passenger = new PassengerNPC(core.Logger);
                _categorySelector = new UberCategorySelector();

                _initialized = true;

                RLFDebug.Info(DebugChannel.System, "[UberController] Inicializado com sucesso");
                global::GTA.UI.Notification.Show("~g~Sistema Uber carregado");
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[UberController] Falha crítica na inicialização", ex);
            }
        }

        public bool TryActivateApp(global::GTA.Vehicle vehicle)
        {
            if (_job == null)
            {
                global::GTA.UI.Notification.Show("~r~❌ Sistema Uber não inicializado");
                RLFDebug.Error(DebugChannel.System, "[UberController] Tentativa de ativação com job null");
                return false;
            }

            if (_job.Account.IsBanned)
            {
                string banMsg = _job.PenaltySystem.GetBanMessage(_job.Account);
                global::GTA.UI.Notification.Show($"~r~🚫 {banMsg}");
                RLFDebug.Warning(DebugChannel.System, "[UberController] Conta banida");
                return false;
            }

            if (!_vehicleValidator.IsValidUberVehicle(vehicle, out string reason))
            {
                global::GTA.UI.Notification.Show($"~r~❌ {reason}");
                RLFDebug.Warning(DebugChannel.System, $"[UberController] Veículo inválido: {reason}");
                return false;
            }

            _currentVehicle = vehicle;
            _lastVehicleHealth = vehicle.HealthFloat;
            _lastPosition = Game.Player.Character.Position;

            _selectingCategory = true;
            _categorySelector.Show(
                vehicle,
                onSelected: (category) => OnCategorySelected(category),
                onCancelled: () => OnCategorySelectionCancelled()
            );

            RLFDebug.Info(DebugChannel.System, "[UberController] Seletor de categoria exibido");
            return true;
        }

        private void OnCategorySelected(RideCategory category)
        {
            _selectingCategory = false;
            _selectedCategory = category;
            _job.AppActive = true;
            _hud.IsVisible = true;

            GenerateNewRequest();

            global::GTA.UI.Notification.Show($"~g~✅ App Uber Ativado\n~w~Serviço: {GetCategoryDisplayName(category)}");
            RLFDebug.Info(DebugChannel.System, $"[UberController] Categoria selecionada: {category}");
        }

        private void OnCategorySelectionCancelled()
        {
            _selectingCategory = false;
            _currentVehicle = null;

            global::GTA.UI.Notification.Show("~y~Ativação do Uber cancelada");
            RLFDebug.Info(DebugChannel.System, "[UberController] Seleção de categoria cancelada");
        }

        private void GenerateNewRequest()
        {
            if (!_job.AppActive)
                return;

            _currentRequest = _job.RideManager.GenerateNewRequest(
                _selectedCategory,
                _job.Settings.RideRequestTimeoutSeconds
            );

            _requestReceivedAt = DateTime.UtcNow;
            _waitingForAccept = true;

            UberNotificationSystem.ShowRideRequest(_selectedCategory, _job.Settings.RideRequestTimeoutSeconds);
            RLFDebug.Info(DebugChannel.System, $"[UberController] Solicitação gerada: {_selectedCategory}");
        }

        private string GetCategoryDisplayName(RideCategory category)
        {
            switch (category)
            {
                case RideCategory.UberBlack:
                    return "Uber Black";
                case RideCategory.UberPool:
                    return "Uber Pool";
                case RideCategory.UberX:
                    return "Uber X";
                default:
                    return "Uber";
            }
        }

        private void HandleRequestAcceptance()
        {
            if (_currentRequest.IsExpired)
            {
                _waitingForAccept = false;
                _currentRequest = null;

                global::GTA.UI.Notification.Show("~y~⏱️ Solicitação expirou");
                RLFDebug.Info(DebugChannel.System, "[UberController] Solicitação expirada");

                Wait(_job.Settings.NewRideDelaySeconds * 1000);
                GenerateNewRequest();
                return;
            }

            if (Game.IsControlJustPressed(Control.FrontendAccept))
            {
                AcceptRequest();
            }
            else if (Game.IsControlJustPressed(Control.FrontendCancel))
            {
                DeclineRequest();
            }
        }

        private void AcceptRequest()
        {
            _waitingForAccept = false;
            _job.RideManager.AcceptRequest();

            CreatePickupBlip();
            UberNotificationSystem.ShowRideAccepted();

            _drivingToPickup = true;
            _rideStartTime = DateTime.UtcNow;

            RLFDebug.Info(DebugChannel.System, "[UberController] Corrida aceita");
        }

        private void DeclineRequest()
        {
            _waitingForAccept = false;
            _job.RideManager.CancelRequest();

            var economy = EconomyBridge.Current;
            if (economy != null)
            {
                _job.PenaltySystem.ApplyCancellationPenalty(_job.Account, economy);
            }

            UberNotificationSystem.ShowPenalty(_job.Settings.CancellationPenalty);

            _job.Save();

            RLFDebug.Warning(DebugChannel.System, "[UberController] Corrida recusada - penalidade aplicada");

            Wait(_job.Settings.NewRideDelaySeconds * 1000);
            GenerateNewRequest();
        }

        private void CreatePickupBlip()
        {
            _pickupBlip?.Delete();

            _pickupBlip = World.CreateBlip(_job.RideManager.CurrentRide.PickupLocation);
            _pickupBlip.Sprite = BlipSprite.Standard;
            _pickupBlip.Color = BlipColor.Blue;
            _pickupBlip.Name = "Passageiro";
            _pickupBlip.IsShortRange = false;

            World.WaypointPosition = _job.RideManager.CurrentRide.PickupLocation;

            RLFDebug.Info(DebugChannel.System, "[UberController] Blip de coleta criado");
        }

        private void HandleDrivingToPickup()
        {
            Ped player = Game.Player.Character;

            if (!player.IsInVehicle(_currentVehicle))
            {
                global::GTA.UI.Notification.Show("~r~❌ Você saiu do veículo - corrida cancelada");
                CancelRide();
                return;
            }

            Vector3 pickup = _job.RideManager.CurrentRide.PickupLocation;
            float distance = player.Position.DistanceTo(pickup);

            if (distance < 15f)
            {
                PickupPassenger();
            }
        }

        private void PickupPassenger()
        {
            _pickupBlip?.Delete();
            _drivingToPickup = false;

            if (_passenger.Spawn(_job.RideManager.CurrentRide.PickupLocation, _job.Account.AverageRating))
            {
                _passenger.EnterVehicle(_currentVehicle);
                Wait(3000);

                _job.RideManager.PassengerPickedUp();
                _passengerOnBoard = true;
                _drivingToDestination = true;

                CreateDestinationBlip();

                global::GTA.UI.Notification.Show("~g~✅ Passageiro embarcado\n~w~Siga para o destino");
                RLFDebug.Info(DebugChannel.System, "[UberController] Passageiro embarcado");
            }
            else
            {
                global::GTA.UI.Notification.Show("~r~❌ Falha ao spawnar passageiro");
                RLFDebug.Error(DebugChannel.System, "[UberController] Falha ao spawnar passageiro");
                CancelRide();
            }
        }

        private void CreateDestinationBlip()
        {
            _destinationBlip?.Delete();

            _destinationBlip = World.CreateBlip(_job.RideManager.CurrentRide.DestinationLocation);
            _destinationBlip.Sprite = BlipSprite.Standard;
            _destinationBlip.Color = BlipColor.Yellow;
            _destinationBlip.Name = "Destino";
            _destinationBlip.IsShortRange = false;

            World.WaypointPosition = _job.RideManager.CurrentRide.DestinationLocation;

            RLFDebug.Info(DebugChannel.System, "[UberController] Blip de destino criado");
        }

        private void HandleDrivingToDestination()
        {
            Ped player = Game.Player.Character;

            if (!player.IsInVehicle(_currentVehicle))
            {
                global::GTA.UI.Notification.Show("~r~❌ Você saiu do veículo - corrida cancelada");
                CancelRide();
                return;
            }

            if (_currentVehicle.HealthFloat < _lastVehicleHealth - 50f)
            {
                _job.RideManager.RecordCrash();
                _lastVehicleHealth = _currentVehicle.HealthFloat;

                RLFDebug.Warning(DebugChannel.System, $"[UberController] Batida registrada - total: {_job.RideManager.CurrentRide.CrashCount}");
            }

            float traveled = player.Position.DistanceTo(_lastPosition);
            _lastPosition = player.Position;

            _job.RideManager.UpdateRideMetrics(
                _job.RideManager.CurrentRide.DistanceTraveled + traveled,
                (int)(DateTime.UtcNow - _rideStartTime).TotalSeconds
            );

            float distance = player.Position.DistanceTo(_job.RideManager.CurrentRide.DestinationLocation);

            if (distance < 15f)
            {
                CompleteRide();
            }
        }

        private void CompleteRide()
        {
            _destinationBlip?.Delete();

            float timeElapsed = (float)(DateTime.UtcNow - _rideStartTime).TotalSeconds;

            _passenger.ExitVehicle();
            Wait(2000);
            _passenger.Cleanup();

            _job.CompleteRide(timeElapsed);

            var ride = _job.RideManager.CurrentRide;
            decimal payment = RidePaymentCalculator.CalculatePayment(ride, _job.Settings);
            float rating = _job.RatingSystem.CalculateRideRating(ride, (int)timeElapsed);

            UberNotificationSystem.ShowRideCompleted(payment, rating);

            RLFDebug.Info(DebugChannel.System, $"[UberController] Corrida concluída - ${payment:F2} | {rating:F1}★");

            ResetRideState();

            Wait(_job.Settings.NewRideDelaySeconds * 1000);
            GenerateNewRequest();
        }

        private void CancelRide()
        {
            _pickupBlip?.Delete();
            _destinationBlip?.Delete();
            _passenger?.Cleanup();

            ResetRideState();

            _job.Save();

            RLFDebug.Warning(DebugChannel.System, "[UberController] Corrida cancelada");

            Wait(_job.Settings.NewRideDelaySeconds * 1000);
            GenerateNewRequest();
        }

        private void ResetRideState()
        {
            _waitingForAccept = false;
            _drivingToPickup = false;
            _passengerOnBoard = false;
            _drivingToDestination = false;
            _currentRequest = null;

            _job.RideManager.CompleteRide();
        }

        public bool RequestTermination()
        {
            if (_job == null || !_job.AppActive)
                return false;

            try
            {
                _pickupBlip?.Delete();
                _destinationBlip?.Delete();
                _passenger?.Cleanup();
                _categorySelector?.Hide();

                _job.AppActive = false;
                _hud.IsVisible = false;
                _selectingCategory = false;

                ResetRideState();

                _job.Save();

                RLFDebug.Info(DebugChannel.System, "[UberController] Serviço encerrado manualmente");
                return true;
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[UberController] Erro ao encerrar serviço", ex);
                return false;
            }
        }

        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                _pickupBlip?.Delete();
                _destinationBlip?.Delete();
                _passenger?.Cleanup();
                _job?.Save();
                _instance = null;

                RLFDebug.Info(DebugChannel.System, "[UberController] Script encerrado");
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[UberController] Erro durante cleanup", ex);
            }
        }
    }
}