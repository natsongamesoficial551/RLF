using GTA;
using GTA.Math;
using RLF.Core;
using RLF.Core.Identity.Enums;
using System;
using System.Collections.Generic;

namespace RLF.GTA.Identity.DrivingSchool
{
    public sealed class DrivingTestSession
    {
        public bool IsFinished { get; private set; }

        private readonly LicenseType _licenseType = LicenseType.DriverCar;
        private readonly VehicleHash _vehicleModel = VehicleHash.Adder;
        private readonly List<Vector3> _checkpoints;

        private int _currentIndex;
        private Vehicle _vehicle;
        private Blip _checkpointBlip;

        private float _lastHealth;
        private Vector3 _startPos;

        private const float MAX_DISTANCE_FROM_ROUTE = 150f;

        public DrivingTestSession()
        {
            _checkpoints = DrivingTestRoutes.CarRoute;
            Start();
        }

        // ===============================
        // ▶ START
        // ===============================
        private void Start()
        {
            // 🚫 BLOQUEIA SISTEMA DE LEI (teste de CNH ativo)
            DrivingTestContext.Enter();

            Ped player = Game.Player.Character;

            Vector3 spawnPos = global::GTA.World.GetNextPositionOnStreet(_checkpoints[0]);

            _vehicle = global::GTA.World.CreateVehicle(
                new Model(_vehicleModel),
                spawnPos
            );

            if (_vehicle == null || !_vehicle.Exists())
            {
                Fail("Falha ao iniciar o veículo do teste");
                return;
            }

            player.SetIntoVehicle(_vehicle, VehicleSeat.Driver);

            _lastHealth = _vehicle.Health;
            _startPos = spawnPos;

            CreateCheckpoint();

            global::GTA.UI.Notification.Show(
                "🚦 Teste prático iniciado\n❗ Qualquer batida reprova automaticamente"
            );
        }

        // ===============================
        // 🔁 TICK
        // ===============================
        public void Tick()
        {
            if (IsFinished)
                return;

            Ped player = Game.Player.Character;

            // ❌ MORTE
            if (player.IsDead)
            {
                Fail("Você morreu durante o teste");
                return;
            }

            // ❌ VEÍCULO INVÁLIDO
            if (_vehicle == null || !_vehicle.Exists())
            {
                Fail("Veículo do teste foi perdido");
                return;
            }

            // ❌ SAIU DO VEÍCULO
            if (!player.IsInVehicle(_vehicle))
            {
                Fail("Você saiu do veículo");
                return;
            }

            // ❌ BATEU (QUALQUER DANO)
            if (_vehicle.Health < _lastHealth)
            {
                Fail("Você bateu durante o teste");
                return;
            }

            _lastHealth = _vehicle.Health;

            // ❌ LONGE DEMAIS (fuga / bug / queda)
            float distFromStart = player.Position.DistanceTo(_startPos);
            if (distFromStart > MAX_DISTANCE_FROM_ROUTE)
            {
                Fail("Você se afastou demais do percurso");
                return;
            }

            Vector3 target = global::GTA.World.GetNextPositionOnStreet(
                _checkpoints[_currentIndex]
            );

            float dist = player.Position.DistanceTo(target);

            if (dist < 6f)
            {
                AdvanceCheckpoint();
            }
        }

        // ===============================
        // ▶ CHECKPOINTS
        // ===============================
        private void AdvanceCheckpoint()
        {
            _checkpointBlip?.Delete();
            _currentIndex++;

            if (_currentIndex >= _checkpoints.Count)
            {
                Pass();
                return;
            }

            CreateCheckpoint();
        }

        private void CreateCheckpoint()
        {
            Vector3 streetPos = global::GTA.World.GetNextPositionOnStreet(
                _checkpoints[_currentIndex]
            );

            _checkpointBlip = global::GTA.World.CreateBlip(streetPos);
            _checkpointBlip.Color = BlipColor.Yellow;
            _checkpointBlip.Scale = 0.85f;
        }

        // ===============================
        // ✅ PASSOU
        // ===============================
        private void Pass()
        {
            // 🔓 LIBERA SISTEMA DE LEI
            DrivingTestContext.Exit();

            GiveLicense();
            Cleanup();

            global::GTA.UI.Notification.Show(
                "✅ Teste concluído com sucesso!\nCNH emitida."
            );

            IsFinished = true;
        }

        // ===============================
        // ❌ FALHOU
        // ===============================
        private void Fail(string reason)
        {
            // 🔓 LIBERA SISTEMA DE LEI
            DrivingTestContext.Exit();

            Cleanup();

            global::GTA.UI.Notification.Show(
                $"❌ Teste reprovado\nMotivo: {reason}"
            );

            IsFinished = true;
        }

        // ===============================
        // 📄 CNH
        // ===============================
        private void GiveLicense()
        {
            var docSystem = RLFCore.Instance.Systems.Get("DocumentSystem")
                as RLF.Core.Identity.DocumentSystem;

            if (docSystem == null)
                return;

            docSystem.GrantLicense(
                type: _licenseType,
                validityDays: 365,
                reason: "Aprovado no teste prático"
            );
        }

        // ===============================
        // 🧹 CLEANUP
        // ===============================
        private void Cleanup()
        {
            try { _checkpointBlip?.Delete(); } catch { }
            try { _vehicle?.Delete(); } catch { }
        }
    }
}
