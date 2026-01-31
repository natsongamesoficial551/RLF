using GTA;
using System;

namespace RLF.GTA.Law.Police
{
    public sealed class AirPolicePatrolManager : Script
    {
        private readonly AirPoliceDetectionService _detector = new AirPoliceDetectionService();
        private AirPoliceApproachController _activeApproach;

        private int _nextDetectionAt;
        private const int DETECTION_INTERVAL = 5000; // Verifica a cada 5 segundos

        public AirPolicePatrolManager()
        {
            Tick += OnTick;
            _nextDetectionAt = 0;
        }

        private void OnTick(object sender, EventArgs e)
        {
            // Se já está em uma abordagem ativa
            if (_activeApproach != null)
            {
                _activeApproach.Tick();

                if (_activeApproach.IsFinished)
                {
                    _activeApproach = null;
                    _nextDetectionAt = Game.GameTime + 30000; // Cooldown de 30s
                }

                return;
            }

            // Verifica se pode detectar
            if (Game.GameTime < _nextDetectionAt)
                return;

            // Não detecta durante testes
            if (IsTestActive())
                return;

            // Tenta detectar voo ilegal
            AirPoliceTarget target;
            if (_detector.TryDetectIllegalFlight(out target))
            {
                _activeApproach = new AirPoliceApproachController(target);
                _nextDetectionAt = Game.GameTime + DETECTION_INTERVAL;
            }
            else
            {
                _nextDetectionAt = Game.GameTime + DETECTION_INTERVAL;
            }
        }

        private bool IsTestActive()
        {
            try
            {
                // Verifica teste de voo
                var flightTestType = System.Type.GetType("RLF.GTA.Identity.FlightSchool.FlightTestContext");
                if (flightTestType != null)
                {
                    var isActiveProp = flightTestType.GetProperty("IsActive",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (isActiveProp != null)
                    {
                        bool flightTestActive = (bool)isActiveProp.GetValue(null);
                        if (flightTestActive)
                            return true;
                    }
                }

                // Verifica teste de direção
                var drivingTestType = System.Type.GetType("RLF.GTA.Identity.DrivingSchool.DrivingTestContext");
                if (drivingTestType != null)
                {
                    var isActiveProp = drivingTestType.GetProperty("IsActive",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (isActiveProp != null)
                    {
                        bool drivingTestActive = (bool)isActiveProp.GetValue(null);
                        if (drivingTestActive)
                            return true;
                    }
                }

                // Verifica teste de armas
                var weaponTestType = System.Type.GetType("RLF.GTA.CoreIntegration.Identity.WeaponSchool.WeaponTestContext");
                if (weaponTestType != null)
                {
                    var isActiveProp = weaponTestType.GetProperty("IsActive",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (isActiveProp != null)
                    {
                        bool weaponTestActive = (bool)isActiveProp.GetValue(null);
                        if (weaponTestActive)
                            return true;
                    }
                }
            }
            catch { }

            return false;
        }
    }
}