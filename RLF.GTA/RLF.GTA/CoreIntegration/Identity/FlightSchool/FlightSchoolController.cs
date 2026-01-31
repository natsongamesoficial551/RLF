using GTA;
using GTA.Math;
using LemonUI;
using LemonUI.Menus;
using RLF.Core.Debug;
using RLF.Core.Identity.Enums;
using RLF.GTA.CoreIntegration.Identity;
using System;
using System.Windows.Forms;

namespace RLF.GTA.Identity.FlightSchool
{
    public sealed class FlightSchoolController : Script
    {
        // 📍 MENU fica no galpão (posição original)
        private readonly Vector3 _schoolPos =
            new Vector3(-1652.5f, -3142.3f, 13.9f);

        // 📍 TESTE inicia aqui (pista LSIA)
        private readonly Vector3 _testStartPos =
            new Vector3(-1580.776f, -3002.077f, 13.429f);

        private Blip _schoolBlip;

        private LemonUI.ObjectPool _uiPool;
        private NativeMenu _menu;

        private FlightTestSession _currentSession;

        public FlightSchoolController()
        {
            Tick += OnTick;
            Aborted += OnAborted;

            CreateBlip();
            CreateMenu();

            try
            {
                RLFDebug.Info(
                    DebugChannel.System,
                    "FlightSchoolController iniciado - Voo LSIA → Sandy Shores"
                );
            }
            catch { }
        }

        // ===============================
        // 🗺️ BLIP (no galpão)
        // ===============================
        private void CreateBlip()
        {
            if (!LicenseTestAvailabilityService.ShouldShowTest(LicenseType.PilotPlane))
                return;

            _schoolBlip = global::GTA.World.CreateBlip(_schoolPos);
            _schoolBlip.Sprite = BlipSprite.Plane;
            _schoolBlip.Color = BlipColor.Blue;
            _schoolBlip.Name = "Escola de Aviação";
            _schoolBlip.IsShortRange = true;
        }

        // ===============================
        // 📋 MENU
        // ===============================
        private void CreateMenu()
        {
            _uiPool = new LemonUI.ObjectPool();

            _menu = new NativeMenu(
                "ESCOLA DE AVIAÇÃO",
                "Teste prático - Voo Cross-Country"
            );

            _uiPool.Add(_menu);

            if (LicenseTestAvailabilityService.ShouldShowTest(LicenseType.PilotPlane))
            {
                var planeTest = new NativeItem("Iniciar teste de CHT (LSIA → Sandy Shores)",
                    "Voe do aeroporto principal até Sandy Shores Airfield");
                _menu.Add(planeTest);

                planeTest.Activated += (s, e) => StartPlaneTest();
            }
        }

        // ===============================
        // ▶ START TEST
        // ===============================
        private void StartPlaneTest()
        {
            if (_currentSession != null)
                return;

            _menu.Visible = false;
            _currentSession = new FlightTestSession();
        }

        // ===============================
        // 🔁 TICK
        // ===============================
        private void OnTick(object sender, EventArgs e)
        {
            _uiPool.Process();

            // 🔍 MODO DEBUG - Remove depois de testar
            if (Game.IsKeyPressed(Keys.F9))
            {
                Ped player = Game.Player.Character;
                global::GTA.UI.Notification.Show(
                $"Posição: {player.Position.X:F1}, {player.Position.Y:F1}, {player.Position.Z:F1}\n" +
                $"Heading: {player.Heading:F1}°"
            );
            }

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
        // 🧭 INTERAÇÃO (no galpão)
        // ===============================
        private void HandleInteraction()
        {
            if (!LicenseTestAvailabilityService.ShouldShowTest(LicenseType.PilotPlane))
            {
                _menu.Visible = false;
                return;
            }

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            float dist = player.Position.DistanceTo(_schoolPos);

            if (dist < 5f)
            {
                global::GTA.UI.Screen.ShowHelpTextThisFrame(
                    "Pressione ~INPUT_VEH_HEADLIGHT~ (H) para iniciar o teste de voo cross-country"
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