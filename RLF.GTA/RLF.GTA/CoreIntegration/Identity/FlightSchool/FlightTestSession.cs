using GTA;
using GTA.Math;
using RLF.Core;
using RLF.Core.Identity.Enums;
using System;
using System.Collections.Generic;

namespace RLF.GTA.Identity.FlightSchool
{
    public sealed class FlightTestSession
    {
        public bool IsFinished { get; private set; }

        private readonly LicenseType _licenseType = LicenseType.PilotPlane;
        private readonly VehicleHash _vehicleModel = VehicleHash.Velum;
        private readonly List<Vector3> _checkpoints;

        private int _currentIndex;
        private Vehicle _aircraft;
        private Blip _checkpointBlip;

        private DateTime _startTime;
        private const int TIME_LIMIT_SECONDS = 420; // 7 minutos - tempo realista mas apertado
        private const float CHECKPOINT_RADIUS = 50f;

        public FlightTestSession()
        {
            _checkpoints = FlightTestRoutes.PlaneRoute;
            Start();
        }

        // ===============================
        // ▶ START
        // ===============================
        private void Start()
        {
            FlightTestContext.Enter();

            Ped player = Game.Player.Character;

            Vector3 spawnPos = _checkpoints[0];

            _aircraft = global::GTA.World.CreateVehicle(
                new Model(_vehicleModel),
                spawnPos
            );

            if (_aircraft == null || !_aircraft.Exists())
            {
                Fail("Falha ao iniciar a aeronave do teste");
                return;
            }

            _aircraft.Heading = 59f;

            player.SetIntoVehicle(_aircraft, VehicleSeat.Driver);

            _startTime = DateTime.Now;

            CreateCheckpoint();

            global::GTA.UI.Notification.Show(
            $"✈️ Teste de voo iniciado\n📍 Destino: Sandy Shores Airfield\n⏱️ Tempo limite: {TIME_LIMIT_SECONDS / 60} minutos"
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

            // ❌ AERONAVE INVÁLIDA
            if (_aircraft == null || !_aircraft.Exists())
            {
                Fail("Aeronave do teste foi perdida");
                return;
            }

            // ❌ SAIU DA AERONAVE
            if (!player.IsInVehicle(_aircraft))
            {
                Fail("Você saiu da aeronave");
                return;
            }

            // ❌ TEMPO ESGOTADO
            TimeSpan elapsed = DateTime.Now - _startTime;
            int remainingSeconds = TIME_LIMIT_SECONDS - (int)elapsed.TotalSeconds;

            if (remainingSeconds <= 0)
            {
                Fail("Tempo esgotado");
                return;
            }

            // 📊 MOSTRAR TEMPO RESTANTE NA TELA
            string timeColor = remainingSeconds > 120 ? "~w~" : remainingSeconds > 60 ? "~o~" : "~r~";
            int minutes = remainingSeconds / 60;
            int seconds = remainingSeconds % 60;

            global::GTA.UI.Screen.ShowSubtitle(
                $"{timeColor}Tempo restante: {minutes}:{seconds:D2}",
                1
            );

            // ✅ Verifica se chegou no checkpoint
            Vector3 target = _checkpoints[_currentIndex];
            float distToTarget = player.Position.DistanceTo(target);

            if (distToTarget < CHECKPOINT_RADIUS)
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
            Vector3 pos = _checkpoints[_currentIndex];

            _checkpointBlip = global::GTA.World.CreateBlip(pos);
            _checkpointBlip.Color = BlipColor.Yellow;
            _checkpointBlip.Scale = 1.5f;
            _checkpointBlip.ShowRoute = true;
        }

        // ===============================
        // ✅ PASSOU
        // ===============================
        private void Pass()
        {
            FlightTestContext.Exit();

            GiveLicense();
            Cleanup();

            global::GTA.UI.Notification.Show(
            "✅ Teste de voo concluído!\n🎖️ CHT (Piloto de Avião) emitida."
        );

            IsFinished = true;
        }

        // ===============================
        // ❌ FALHOU
        // ===============================
        private void Fail(string reason)
        {
            FlightTestContext.Exit();

            Cleanup();

            global::GTA.UI.Notification.Show(
            $"❌ Teste reprovado\nMotivo: {reason}"
        );

            IsFinished = true;
        }

        // ===============================
        // 📄 CHT
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
                reason: "Aprovado no teste de voo cross-country"
            );
        }

        // ===============================
        // 🧹 CLEANUP
        // ===============================
        private void Cleanup()
        {
            try { _checkpointBlip?.Delete(); } catch { }
            try { _aircraft?.Delete(); } catch { }
        }
    }
}