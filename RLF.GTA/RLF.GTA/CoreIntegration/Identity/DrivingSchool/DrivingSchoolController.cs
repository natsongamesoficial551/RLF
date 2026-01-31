using GTA;
using GTA.Math;
using LemonUI;
using LemonUI.Menus;
using RLF.Core.Debug;
using RLF.Core.Identity.Enums;
using RLF.GTA.CoreIntegration.Identity;
using System;
using System.Windows.Forms;

namespace RLF.GTA.Identity.DrivingSchool
{
    public sealed class DrivingSchoolController : Script
    {
        private readonly Vector3 _schoolPos =
            new Vector3(-1588.747f, -833.688f, 9.881f);

        private Blip _schoolBlip;

        private LemonUI.ObjectPool _uiPool;
        private NativeMenu _menu;

        private DrivingTestSession _currentSession;

        public DrivingSchoolController()
        {
            Tick += OnTick;
            Aborted += OnAborted;

            CreateBlip();
            CreateMenu();

            try
            {
                RLFDebug.Info(
                    DebugChannel.System,
                    "DrivingSchoolController iniciado (CNH)"
                );
            }
            catch { }
        }

        // ===============================
        // 🗺️ BLIP
        // ===============================
        private void CreateBlip()
        {
            // ❌ Se já tem CNH válida, não cria blip
            if (!LicenseTestAvailabilityService.ShouldShowTest(LicenseType.DriverCar))
                return;

            _schoolBlip = global::GTA.World.CreateBlip(_schoolPos);
            _schoolBlip.Sprite = BlipSprite.PersonalVehicleCar;
            _schoolBlip.Color = BlipColor.Blue;
            _schoolBlip.Name = "Autoescola";
            _schoolBlip.IsShortRange = true;
        }

        // ===============================
        // 📋 MENU
        // ===============================
        private void CreateMenu()
        {
            _uiPool = new LemonUI.ObjectPool();

            _menu = new NativeMenu(
                "AUTOESCOLA",
                "Teste prático"
            );

            _uiPool.Add(_menu);

            // ✅ Só adiciona o teste se NÃO tiver CNH
            if (LicenseTestAvailabilityService.ShouldShowTest(LicenseType.DriverCar))
            {
                var carTest = new NativeItem("Iniciar teste de CNH (Carro)");
                _menu.Add(carTest);

                carTest.Activated += (s, e) => StartCarTest();
            }
        }

        // ===============================
        // ▶ START TEST
        // ===============================
        private void StartCarTest()
        {
            if (_currentSession != null)
                return;

            _menu.Visible = false;
            _currentSession = new DrivingTestSession();
        }

        // ===============================
        // 🔁 TICK
        // ===============================
        private void OnTick(object sender, EventArgs e)
        {
            _uiPool.Process();

            if (_currentSession != null)
            {
                _currentSession.Tick();

                if (_currentSession.IsFinished)
                    _currentSession = null;

                return;
            }

            HandleInteraction();
        }

        // ===============================
        // 🧭 INTERAÇÃO
        // ===============================
        private void HandleInteraction()
        {
            // ❌ Se já tem CNH válida, não interage
            if (!LicenseTestAvailabilityService.ShouldShowTest(LicenseType.DriverCar))
            {
                _menu.Visible = false;
                return;
            }

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            float dist = player.Position.DistanceTo(_schoolPos);

            if (dist < 3f)
            {
                global::GTA.UI.Screen.ShowHelpTextThisFrame(
                    "Pressione ~INPUT_VEH_HEADLIGHT~ (H) para iniciar o teste de CNH"
                );

                if (Game.IsKeyPressed(Keys.H))
                {
                    _menu.Visible = !_menu.Visible;
                }
            }
            else
            {
                _menu.Visible = false;
            }
        }

        // ===============================
        // 🧹 CLEANUP
        // ===============================
        private void OnAborted(object sender, EventArgs e)
        {
            try { _schoolBlip?.Delete(); } catch { }
        }
    }
}
