using GTA;
using GTA.Native;
using GTA.UI;
using System;
using System.Reflection;
using Vector3 = global::GTA.Math.Vector3;

namespace RLF.GTA.Law.Police
{
    public sealed class PoliceApproachController
    {
        private static readonly Vector3 ImpoundPos =
            new Vector3(441.482f, -999.975f, 30.723f);

        private readonly PoliceUnit _unit;
        private readonly PoliceTarget _target;

        private PoliceApproachState _state;
        private int _stateUntil;

        private int _stopToleranceUntil;
        private bool _wantedApplied;

        private bool _inspectionDone;
        private bool _needsImpound;

        private bool _p1ExitRequested;
        private int _p1ExitRequestedAt;

        private bool _inspectionLocked;

        private bool _p1ReturnRequested;
        private bool _p2ExitRequested;
        private bool _p2InPlayerVehicle;
        private bool _p2DrivingToImpound;
        private bool _p2RespawnRequested;

        private int _approachAttempts;

        private int _globalTimeout;
        private const int MAX_SCENARIO_TIME = 600000;

        private int _lastForceWalkAt;
        private bool _vehiclePositioned;

        private Vector3 _playerStoppedPosition;

        public bool IsFinished => _state == PoliceApproachState.Finished;

        public PoliceApproachController(PoliceUnit unit, PoliceTarget target)
        {
            _unit = unit;
            _target = target;

            _state = PoliceApproachState.Following;
            _stateUntil = Game.GameTime + 8000;
            _stopToleranceUntil = Game.GameTime + 15000;

            _globalTimeout = Game.GameTime + MAX_SCENARIO_TIME;
            _lastForceWalkAt = 0;
            _vehiclePositioned = false;
            _playerStoppedPosition = Vector3.Zero;
        }

        public void Tick()
        {
            if (Game.GameTime > _globalTimeout)
            {
                Finish();
                return;
            }

            if (_unit == null || !_unit.EntitiesExist() || _target == null || !_target.IsValid())
            {
                Finish();
                return;
            }

            if (_state != PoliceApproachState.VehicleTaken &&
                _state != PoliceApproachState.Cleanup &&
                _state != PoliceApproachState.Finished)
            {
                EnforceP2InVehicle();
            }

            switch (_state)
            {
                case PoliceApproachState.Following: UpdateFollowing(); break;
                case PoliceApproachState.SignalingStop: UpdateSignaling(); break;
                case PoliceApproachState.WaitingStop: UpdateWaitingStop(); break;

                case PoliceApproachState.OfficerExit: UpdateOfficerExit(); break;
                case PoliceApproachState.OfficerApproach: UpdateOfficerApproach(); break;
                case PoliceApproachState.OfficerInspection: UpdateInspection(); break;

                case PoliceApproachState.VehicleTaken: UpdateVehicleTaken(); break;
                case PoliceApproachState.Cleanup: Cleanup(); break;
            }
        }

        private void EnforceP2InVehicle()
        {
            try
            {
                var p2 = _unit.OfficerP2;
                var v = _unit.Vehicle;

                if (p2 == null || !p2.Exists() || v == null || !v.Exists())
                    return;

                if (!p2.IsInVehicle(v))
                {
                    p2.Task.ClearAllImmediately();
                    p2.SetIntoVehicle(v, VehicleSeat.Passenger);
                }
            }
            catch { }
        }

        private void UpdateFollowing()
        {
            var p1 = _unit.OfficerP1;
            var v = _unit.Vehicle;
            var playerVehicle = _target.Vehicle;

            EnableLightSiren(true);

            float playerSpeed = playerVehicle.Speed * 3.6f;

            if (playerSpeed < 2f)
            {
                _playerStoppedPosition = playerVehicle.Position;
                _state = PoliceApproachState.SignalingStop;
                _stateUntil = Game.GameTime + 2000;
                return;
            }

            Vector3 targetPos = playerVehicle.Position;

            Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                p1.Handle,
                v.Handle,
                targetPos.X,
                targetPos.Y,
                targetPos.Z,
                25f,
                2883621,
                3f
            );
        }

        private void UpdateSignaling()
        {
            var playerVehicle = _target.Vehicle;
            float playerSpeed = playerVehicle.Speed * 3.6f;

            if (playerSpeed < 0.5f)
            {
                _playerStoppedPosition = playerVehicle.Position;
                _state = PoliceApproachState.WaitingStop;
                _stateUntil = Game.GameTime + 2000;
                return;
            }

            if (playerSpeed > 10f)
            {
                _state = PoliceApproachState.Following;
                return;
            }

            if (Game.GameTime > _stopToleranceUntil && IsFleeingReal())
            {
                StartPursuit();
            }
        }

        private void UpdateWaitingStop()
        {
            if (Game.GameTime < _stateUntil)
                return;

            _p1ExitRequested = false;
            _inspectionLocked = false;
            _p1ReturnRequested = false;
            _p2ExitRequested = false;
            _p2InPlayerVehicle = false;
            _p2DrivingToImpound = false;
            _p2RespawnRequested = false;
            _approachAttempts = 0;
            _lastForceWalkAt = 0;
            _vehiclePositioned = false;

            _state = PoliceApproachState.OfficerExit;
            _stateUntil = Game.GameTime + 30000;
        }

        private void UpdateOfficerExit()
        {
            var p1 = _unit.OfficerP1;
            var v = _unit.Vehicle;
            var playerVehicle = _target.Vehicle;

            if (Game.GameTime > _stateUntil)
            {
                _state = PoliceApproachState.OfficerApproach;
                _stateUntil = Game.GameTime + 60000;
                return;
            }

            Vector3 targetPosition = playerVehicle.Position;

            if (!_vehiclePositioned)
            {
                float distToPlayer = v.Position.DistanceTo(targetPosition);
                float policeSpeed = v.Speed * 3.6f;

                if (distToPlayer > 6f)
                {
                    Vector3 currentPlayerPos = playerVehicle.Position;

                    Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                        p1.Handle,
                        v.Handle,
                        currentPlayerPos.X,
                        currentPlayerPos.Y,
                        currentPlayerPos.Z,
                        20f,
                        2883621,
                        2f
                    );
                    return;
                }

                Vector3 playerForward = playerVehicle.ForwardVector;
                Vector3 behindPlayer = targetPosition - (playerForward * 6f);

                Vector3 toPolice = v.Position - targetPosition;
                float dotProduct = Vector3.Dot(toPolice, playerForward);
                bool isActuallyBehind = dotProduct < 0f;

                if (!isActuallyBehind)
                {
                    Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                        p1.Handle,
                        v.Handle,
                        behindPlayer.X,
                        behindPlayer.Y,
                        behindPlayer.Z,
                        8f,
                        2883621,
                        3f
                    );
                    return;
                }

                if (policeSpeed > 0.5f)
                {
                    p1.Task.ClearAllImmediately();
                    Function.Call(Hash.SET_VEHICLE_HANDBRAKE, v.Handle, true);
                    return;
                }

                p1.Task.ClearAllImmediately();
                Function.Call(Hash.SET_VEHICLE_HANDBRAKE, v.Handle, true);

                _vehiclePositioned = true;
                _p1ExitRequestedAt = Game.GameTime;

                _stateUntil = Game.GameTime + 800;
                return;
            }

            if (Game.GameTime < _stateUntil)
                return;

            if (!_p1ExitRequested)
            {
                p1.Task.ClearAllImmediately();
                Function.Call(Hash.TASK_LEAVE_VEHICLE,
                    p1.Handle,
                    v.Handle,
                    0
                );

                _p1ExitRequested = true;
                _p1ExitRequestedAt = Game.GameTime;
                return;
            }

            if (p1.IsInVehicle(v))
            {
                if (Game.GameTime - _p1ExitRequestedAt > 10000)
                {
                    p1.Task.ClearAllImmediately();
                    Function.Call(Hash.TASK_LEAVE_VEHICLE, p1.Handle, v.Handle, 0);
                    _p1ExitRequestedAt = Game.GameTime;
                }
                return;
            }

            if (p1.IsGettingIntoVehicle || p1.IsRagdoll || p1.IsJumping || p1.IsFalling)
                return;

            if (Game.GameTime - _p1ExitRequestedAt < 1500)
                return;

            if (!p1.IsOnFoot)
                return;

            _state = PoliceApproachState.OfficerApproach;
            _stateUntil = Game.GameTime + 60000;
            _approachAttempts = 0;
        }

        private void UpdateOfficerApproach()
        {
            var p1 = _unit.OfficerP1;
            var player = _target.Ped;
            var playerVehicle = _target.Vehicle;

            if (Game.GameTime > _stateUntil)
            {
                Finish();
                return;
            }

            bool isMotorcycle = playerVehicle.Model.IsBike || playerVehicle.Model.IsBicycle;

            Vector3 targetPos;
            if (isMotorcycle)
            {
                Vector3 leftVector = -playerVehicle.RightVector;
                targetPos = playerVehicle.Position + (leftVector * 1.5f);
            }
            else
            {
                targetPos = Function.Call<Vector3>(
                    Hash.GET_WORLD_POSITION_OF_ENTITY_BONE,
                    playerVehicle.Handle,
                    Function.Call<int>(
                        Hash.GET_ENTITY_BONE_INDEX_BY_NAME,
                        playerVehicle.Handle,
                        "door_dside_f"
                    )
                );
            }

            float distToTarget = p1.Position.DistanceTo(targetPos);

            if (distToTarget <= 2.5f)
            {
                _state = PoliceApproachState.OfficerInspection;
                _stateUntil = Game.GameTime + 6000;
                _approachAttempts = 0;
                return;
            }

            if (Game.GameTime - _lastForceWalkAt > 2000)
            {
                p1.Task.ClearAll();

                Function.Call(
                    Hash.TASK_GO_TO_COORD_ANY_MEANS,
                    p1.Handle,
                    targetPos.X, targetPos.Y, targetPos.Z,
                    1.0f,
                    0,
                    false,
                    786603,
                    0xbf800000
                );

                _lastForceWalkAt = Game.GameTime;
            }

            if (distToTarget < 8f)
            {
                p1.Task.LookAt(player, 1000);
            }

            _approachAttempts++;
            if (_approachAttempts > 1500)
            {
                if (distToTarget > 100f)
                {
                    Finish();
                    return;
                }

                p1.Position = targetPos + (playerVehicle.RightVector * 1.5f);
                _approachAttempts = 0;
                _lastForceWalkAt = Game.GameTime;
            }
        }

        private void UpdateInspection()
        {
            var p1 = _unit.OfficerP1;
            var player = _target.Ped;

            float dist = p1.Position.DistanceTo(player.Position);
            if (dist > 6.0f)
            {
                _state = PoliceApproachState.OfficerApproach;
                _stateUntil = Game.GameTime + 20000;
                _approachAttempts = 0;
                _lastForceWalkAt = 0;
                return;
            }

            if (!_inspectionLocked)
            {
                p1.Task.ClearAllImmediately();
                p1.Task.StandStill(6000);
                p1.Task.TurnTo(player);

                Function.Call(Hash.REQUEST_ANIM_DICT, "amb@world_human_clipboard@male@base");
                int timeout = Game.GameTime + 3000;
                while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "amb@world_human_clipboard@male@base"))
                {
                    Script.Yield();
                    if (Game.GameTime > timeout) break;
                }

                Function.Call(Hash.TASK_PLAY_ANIM,
                    p1.Handle,
                    "amb@world_human_clipboard@male@base",
                    "base",
                    8.0f,
                    -8.0f,
                    6000,
                    1,
                    0.0f,
                    false, false, false
                );

                _inspectionLocked = true;
                _stateUntil = Game.GameTime + 6000;

                Notification.Show("~y~Policial verificando documentos...");
                return;
            }

            if (Game.GameTime < _stateUntil)
                return;

            if (!_inspectionDone)
            {
                p1.Task.ClearAllImmediately();

                _needsImpound = DetermineImpoundNeeded();
                ApplyPenalties();

                _inspectionDone = true;

                if (_needsImpound)
                {
                    Notification.Show("~r~Seu veículo será apreendido!");
                    _state = PoliceApproachState.VehicleTaken;
                    _stateUntil = Game.GameTime + 2000;
                }
                else
                {
                    Notification.Show("~g~Documentação OK. Pode seguir.");
                    _state = PoliceApproachState.Cleanup;
                    _stateUntil = Game.GameTime + 10000;
                }
            }
        }

        private void ApplyPenalties()
        {
            try
            {
                PolicePenaltyService.ApplyAllPenalties(_target);
            }
            catch { }
        }

        private void UpdateVehicleTaken()
        {
            var p1 = _unit.OfficerP1;
            var p2 = _unit.OfficerP2;
            var playerVehicle = _target.Vehicle;
            var policeVehicle = _unit.Vehicle;

            if (IsAircraft(playerVehicle))
            {
                Notification.Show("~o~Aeronave não pode ser apreendida.");
                _state = PoliceApproachState.Cleanup;
                _stateUntil = Game.GameTime + 8000;
                return;
            }

            if (Game.GameTime < _stateUntil)
                return;

            if (_target.Ped.IsInVehicle(playerVehicle))
            {
                _target.Ped.Task.LeaveVehicle(playerVehicle, true);
                _stateUntil = Game.GameTime + 3000;
                return;
            }

            if (!p1.IsInVehicle(policeVehicle))
            {
                if (!_p1ReturnRequested)
                {
                    p1.Task.ClearAll();
                    Function.Call(Hash.TASK_ENTER_VEHICLE,
                        p1.Handle,
                        policeVehicle.Handle,
                        8000,
                        (int)VehicleSeat.Driver,
                        2.0f,
                        1,
                        0
                    );
                    _p1ReturnRequested = true;
                    _stateUntil = Game.GameTime + 8000;
                }
                return;
            }

            if (p2.IsInVehicle(policeVehicle) && !_p2ExitRequested)
            {
                p2.Task.ClearAll();
                p2.Task.LeaveVehicle(policeVehicle, false);
                _p2ExitRequested = true;
                _stateUntil = Game.GameTime + 4000;
                return;
            }

            if (!_p2InPlayerVehicle)
            {
                if (p2.IsInVehicle(playerVehicle))
                {
                    _p2InPlayerVehicle = true;
                    _stateUntil = Game.GameTime + 2000;
                    return;
                }

                if (Game.GameTime > _stateUntil)
                {
                    Function.Call(Hash.TASK_ENTER_VEHICLE,
                        p2.Handle,
                        playerVehicle.Handle,
                        10000,
                        (int)VehicleSeat.Driver,
                        2.0f,
                        1,
                        0
                    );
                    _stateUntil = Game.GameTime + 10000;
                }
                return;
            }

            if (!_p2DrivingToImpound)
            {
                Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                    p2.Handle,
                    playerVehicle.Handle,
                    ImpoundPos.X, ImpoundPos.Y, ImpoundPos.Z,
                    20f,
                    2883621,
                    10f
                );
                _p2DrivingToImpound = true;
            }

            float distToImpound = playerVehicle.Position.DistanceTo(ImpoundPos);
            float distP2ToPolice = p2.Position.DistanceTo(policeVehicle.Position);

            if (distP2ToPolice > 100f && !_p2RespawnRequested)
            {
                p2.Position = policeVehicle.GetOffsetPosition(new Vector3(1.2f, 0f, 0f));
                p2.SetIntoVehicle(policeVehicle, VehicleSeat.Passenger);
                _p2RespawnRequested = true;
            }

            if (distToImpound < 35f)
            {
                PolicePenaltyService.MarkVehicleImpoundedKeepWorld(playerVehicle);
                PolicePenaltyService.FinalizeWorldVehicleDelete(playerVehicle);

                if (!p2.IsInVehicle(policeVehicle))
                {
                    p2.SetIntoVehicle(policeVehicle, VehicleSeat.Passenger);
                }

                Notification.Show("~r~Veículo apreendido e levado ao pátio.");
                Finish();
            }
        }

        private void Cleanup()
        {
            var p1 = _unit.OfficerP1;
            var p2 = _unit.OfficerP2;
            var v = _unit.Vehicle;

            if (p2 != null && p2.Exists() && v != null && v.Exists())
            {
                if (!p2.IsInVehicle(v))
                {
                    p2.SetIntoVehicle(v, VehicleSeat.Passenger);
                }
            }

            if (p1.IsInVehicle(v))
            {
                p1.Task.ClearAllImmediately();
                Function.Call(Hash.SET_VEHICLE_HANDBRAKE, v.Handle, false);
                DisableSiren();
                Script.Wait(100);
                _unit.SetPatrolling();
                Finish();
                return;
            }

            if (Game.GameTime > _stateUntil)
            {
                p1.Task.ClearAllImmediately();
                p1.SetIntoVehicle(v, VehicleSeat.Driver);
                Script.Wait(100);
                Function.Call(Hash.SET_VEHICLE_HANDBRAKE, v.Handle, false);
                DisableSiren();
                _unit.SetPatrolling();
                Finish();
                return;
            }

            if (!_p1ReturnRequested)
            {
                p1.Task.ClearAll();
                Function.Call(Hash.TASK_ENTER_VEHICLE,
                    p1.Handle,
                    v.Handle,
                    8000,
                    (int)VehicleSeat.Driver,
                    2.0f,
                    1,
                    0
                );
                _p1ReturnRequested = true;
            }
        }

        private void Finish()
        {
            DisableSiren();
            _state = PoliceApproachState.Finished;
        }

        private bool IsAircraft(Vehicle vehicle)
        {
            try
            {
                var model = vehicle.Model;
                return model.IsHelicopter || model.IsPlane;
            }
            catch
            {
                return false;
            }
        }

        private bool DetermineImpoundNeeded()
        {
            try
            {
                MethodInfo m = typeof(PolicePenaltyService).GetMethod(
                    "ShouldImpound",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                if (m != null)
                {
                    object r = m.Invoke(null, new object[] { _target });
                    if (r is bool) return (bool)r;
                }
            }
            catch { }

            return false;
        }

        private bool IsPlayerStopped()
        {
            return _target.Vehicle != null &&
                   _target.Vehicle.Exists() &&
                   _target.Vehicle.Speed < 0.5f;
        }

        private bool IsFleeingReal()
        {
            float kmh = _target.Vehicle.Speed * 3.6f;
            float dist = _unit.Vehicle.Position.DistanceTo(_target.Vehicle.Position);
            return kmh > 60f && dist > 80f;
        }

        private void StartPursuit()
        {
            if (!_wantedApplied)
            {
                Game.Player.WantedLevel = 1;
                _wantedApplied = true;
            }

            Notification.Show("~r~Você fugiu da abordagem policial!");
            Finish();
        }

        private void EnableLightSiren(bool lightOnly)
        {
            Function.Call(Hash.SET_VEHICLE_SIREN, _unit.Vehicle.Handle, true);
            if (lightOnly)
                Function.Call(Hash.SET_VEHICLE_HAS_MUTED_SIRENS, _unit.Vehicle.Handle, true);
        }

        private void DisableSiren()
        {
            if (_unit.Vehicle != null && _unit.Vehicle.Exists())
                Function.Call(Hash.SET_VEHICLE_SIREN, _unit.Vehicle.Handle, false);
        }
    }
}