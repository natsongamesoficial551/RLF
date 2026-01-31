using GTA;
using GTA.Native;
using GTA.UI;
using System;
using System.Collections.Generic;
using Vector3 = global::GTA.Math.Vector3;

namespace RLF.GTA.Law.Police
{
    public sealed class PolicePatrolManager : Script
    {
        private const float MIN_SPAWN_DISTANCE = 120f;

        private readonly List<PoliceUnit> _units = new List<PoliceUnit>();
        private readonly PoliceSpawner _spawner = new PoliceSpawner();
        private readonly PoliceDetectionService _detector = new PoliceDetectionService();

        private PoliceApproachController _activeApproach;

        private int _spawnAllowedAt;
        private bool _initialized;

        private int _nextUnitToSpawn;
        private int _spawnCooldownUntil;

        private readonly Dictionary<int, Vector3> _patrolTargets = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, int> _nextPatrolTaskAt = new Dictionary<int, int>();

        private int _nextUprightCheckAt;
        private int _detectionCooldownUntil;

        private static readonly Random _rng = new Random();

        public PolicePatrolManager()
        {
            Tick += OnTick;
            _spawnAllowedAt = Game.GameTime + 5000;
            _nextUnitToSpawn = 0;
            _spawnCooldownUntil = 0;
            _nextUprightCheckAt = 0;
            _detectionCooldownUntil = 0;
        }

        private void Initialize()
        {
            if (_initialized)
                return;

            CreateUnits();
            _initialized = true;
        }

        private void CreateUnits()
        {
            _units.Clear();
            _patrolTargets.Clear();
            _nextPatrolTaskAt.Clear();

            for (int i = 1; i <= PoliceSpawnConfig.UrbanUnits; i++)
                _units.Add(new PoliceUnit(i, PoliceUnitType.Urban,
                    PoliceSpawnConfig.GetSpawnPoint(PoliceUnitType.Urban, i)));

            int baseId = PoliceSpawnConfig.UrbanUnits;
            for (int i = 1; i <= PoliceSpawnConfig.RuralUnits; i++)
            {
                int id = baseId + i;
                _units.Add(new PoliceUnit(id, PoliceUnitType.Rural,
                    PoliceSpawnConfig.GetSpawnPoint(PoliceUnitType.Rural, id)));
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (Game.GameTime < _spawnAllowedAt)
                return;

            Initialize();

            if (Game.GameTime >= _nextUprightCheckAt)
            {
                CheckAndFixUpsideDownVehicles();
                _nextUprightCheckAt = Game.GameTime + 3000;
            }

            SpawnNextUnitIfNeeded();

            foreach (PoliceUnit u in _units)
            {
                if (!u.EntitiesExist())
                {
                    u.SoftCleanup();
                    continue;
                }

                if (u.State == PoliceUnitState.Patrolling)
                    EnforceInVehicle(u);

                if (u.State == PoliceUnitState.Approaching && _activeApproach == null)
                {
                    u.SetPatrolling();
                    AssignNewPatrolTarget(u);
                }

                if (u.State == PoliceUnitState.Patrolling)
                    UpdatePatrol(u);
            }

            bool isDrivingTestActive = false;
            bool isWeaponTestActive = false;

            try
            {
                var drivingTestType = System.Type.GetType("RLF.GTA.Identity.DrivingSchool.DrivingTestContext");
                if (drivingTestType != null)
                {
                    var isActiveProp = drivingTestType.GetProperty("IsActive",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (isActiveProp != null)
                        isDrivingTestActive = (bool)isActiveProp.GetValue(null);
                }

                var weaponTestType = System.Type.GetType("RLF.GTA.CoreIntegration.Identity.WeaponSchool.WeaponTestContext");
                if (weaponTestType != null)
                {
                    var isActiveProp = weaponTestType.GetProperty("IsActive",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (isActiveProp != null)
                        isWeaponTestActive = (bool)isActiveProp.GetValue(null);
                }
            }
            catch { }

            if (isDrivingTestActive || isWeaponTestActive)
            {
                if (_activeApproach != null)
                    _activeApproach = null;
                return;
            }

            if (_activeApproach != null)
            {
                _activeApproach.Tick();

                if (_activeApproach.IsFinished)
                {
                    foreach (PoliceUnit u in _units)
                    {
                        if (u.State == PoliceUnitState.Approaching || u.State == PoliceUnitState.Busy)
                        {
                            if (u.EntitiesExist())
                            {
                                u.OfficerP1.Task.ClearAllImmediately();
                                u.OfficerP2.Task.ClearAllImmediately();

                                if (!u.OfficerP1.IsInVehicle(u.Vehicle))
                                    u.OfficerP1.SetIntoVehicle(u.Vehicle, VehicleSeat.Driver);

                                if (!u.OfficerP2.IsInVehicle(u.Vehicle))
                                    u.OfficerP2.SetIntoVehicle(u.Vehicle, VehicleSeat.Passenger);

                                Script.Wait(50);

                                Function.Call(Hash.SET_VEHICLE_HANDBRAKE, u.Vehicle.Handle, false);
                            }

                            u.SetPatrolling();
                            AssignNewPatrolTarget(u);
                        }
                    }

                    _activeApproach = null;
                    _detectionCooldownUntil = Game.GameTime + 5000;
                }
                return;
            }

            if (Game.GameTime < _detectionCooldownUntil)
                return;

            foreach (PoliceUnit u in _units)
            {
                if (u.State != PoliceUnitState.Patrolling || u.IsBusy)
                    continue;

                PoliceTarget target;
                string reason;

                if (_detector.TryDetect(u, out target, out reason))
                {
                    if (!PoliceDecisionService.ShouldApproach())
                        continue;

                    u.SetApproaching();
                    u.MarkBusy(30000);
                    _activeApproach = new PoliceApproachController(u, target);
                    break;
                }
            }
        }

        private void CheckAndFixUpsideDownVehicles()
        {
            foreach (PoliceUnit u in _units)
            {
                if (!u.EntitiesExist())
                    continue;

                Vehicle v = u.Vehicle;
                if (v == null || !v.Exists())
                    continue;

                Vector3 rotation = v.Rotation;
                float roll = rotation.X;
                float pitch = rotation.Y;

                bool isUpsideDown = System.Math.Abs(roll) > 90f || System.Math.Abs(pitch) > 90f;

                if (isUpsideDown)
                {
                    Vector3 spawnPos = u.SpawnPosition;
                    Vector3 streetPos = World.GetNextPositionOnStreet(spawnPos);

                    Vector3 ahead = World.GetNextPositionOnStreet(streetPos + new Vector3(10f, 10f, 0f));
                    Vector3 dir = ahead - streetPos;
                    float heading = dir.Length() > 0.01f
                        ? (float)(System.Math.Atan2(dir.Y, dir.X) * 57.29578)
                        : 0f;

                    v.Position = streetPos;
                    v.Rotation = new Vector3(0f, 0f, heading);
                    v.Velocity = Vector3.Zero;

                    Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, v.Handle);
                    Script.Yield();
                    Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, v.Handle);

                    if (!u.OfficerP1.IsInVehicle(v))
                        u.OfficerP1.SetIntoVehicle(v, VehicleSeat.Driver);

                    if (!u.OfficerP2.IsInVehicle(v))
                        u.OfficerP2.SetIntoVehicle(v, VehicleSeat.Passenger);

                    u.OfficerP1.Task.ClearAllImmediately();
                    u.OfficerP2.Task.ClearAllImmediately();

                    u.SetPatrolling();
                    AssignNewPatrolTarget(u);
                }
            }
        }

        private void EnforceInVehicle(PoliceUnit u)
        {
            try
            {
                if (!u.OfficerP1.IsInVehicle(u.Vehicle))
                    u.OfficerP1.SetIntoVehicle(u.Vehicle, VehicleSeat.Driver);

                if (!u.OfficerP2.IsInVehicle(u.Vehicle))
                    u.OfficerP2.SetIntoVehicle(u.Vehicle, VehicleSeat.Passenger);
            }
            catch { }
        }

        private void SpawnNextUnitIfNeeded()
        {
            if (Game.GameTime < _spawnCooldownUntil)
                return;

            if (_nextUnitToSpawn >= _units.Count)
            {
                for (int i = 0; i < _units.Count; i++)
                {
                    var u = _units[i];
                    if (!u.EntitiesExist())
                    {
                        if (_spawner.Spawn(u))
                        {
                            u.EnsureDebugBlip();
                            AssignNewPatrolTarget(u);
                            _spawnCooldownUntil = Game.GameTime + 3000;
                        }
                        else
                        {
                            _spawnCooldownUntil = Game.GameTime + 5000;
                        }
                        return;
                    }
                }
                return;
            }

            var unit = _units[_nextUnitToSpawn];

            if (_spawner.Spawn(unit))
            {
                unit.EnsureDebugBlip();
                AssignNewPatrolTarget(unit);
                _nextUnitToSpawn++;
                _spawnCooldownUntil = Game.GameTime + 3000;
            }
            else
            {
                _spawnCooldownUntil = Game.GameTime + 5000;
            }
        }

        private void AssignNewPatrolTarget(PoliceUnit u)
        {
            Vector3 basePos = u.Vehicle.Position;

            int ox = _rng.Next(-500, 501);
            int oy = _rng.Next(-500, 501);

            Vector3 target =
                World.GetNextPositionOnStreet(basePos + new Vector3(ox, oy, 0f));

            _patrolTargets[u.UnitId] = target;
            _nextPatrolTaskAt[u.UnitId] = 0;
        }

        private void UpdatePatrol(PoliceUnit u)
        {
            int now = Game.GameTime;

            int next;
            if (_nextPatrolTaskAt.TryGetValue(u.UnitId, out next) && now < next)
                return;

            Vector3 target;
            if (!_patrolTargets.TryGetValue(u.UnitId, out target))
            {
                AssignNewPatrolTarget(u);
                return;
            }

            if (u.Vehicle.Position.DistanceTo(target) < 25f)
            {
                AssignNewPatrolTarget(u);
                return;
            }

            float speed = u.Type == PoliceUnitType.Urban ? 12f : 18f;

            Function.Call(
                Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                u.OfficerP1.Handle,
                u.Vehicle.Handle,
                target.X, target.Y, target.Z,
                speed,
                786603,
                10f
            );

            _nextPatrolTaskAt[u.UnitId] = now + 4000;
        }
    }
}