using GTA;
using GTA.Math;
using LemonUI;
using LemonUI.Menus;
using RLF.Core.Debug;
using RLF.Core.Identity.Enums;
using RLF.GTA.CoreIntegration.Identity;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RLF.GTA.CoreIntegration.Identity.WeaponSchool
{
    public sealed class WeaponSchoolController : Script
    {
        // 📍 Ammu-Nations onde existe o teste
        private readonly List<Vector3> _weaponSchoolLocations = new List<Vector3>
        {
            new Vector3(22.09f, -1107.28f, 29.80f),   // Downtown
            new Vector3(252.77f, -48.42f, 69.94f),   // Hawick
            new Vector3(-662.18f, -935.04f, 21.83f), // Little Seoul
            new Vector3(810.25f, -2157.60f, 29.62f), // Cypress Flats
            new Vector3(1693.44f, 3759.50f, 34.71f)  // Sandy Shores
        };

        private readonly List<Blip> _blips = new List<Blip>();

        private ObjectPool _uiPool;
        private NativeMenu _menu;

        private WeaponCombatTestSession _currentSession;

        public WeaponSchoolController()
        {
            Tick += OnTick;
            Aborted += OnAborted;

            CreateBlips();
            CreateMenu();

            try
            {
                RLFDebug.Info(
                    DebugChannel.System,
                    "WeaponSchoolController iniciado"
                );
            }
            catch { }
        }

        // ===============================
        // 🗺️ BLIPS
        // ===============================
        private void CreateBlips()
        {
            // Só cria blip se o jogador NÃO tiver porte válido
            if (!LicenseTestAvailabilityService.ShouldShowTest(LicenseType.WeaponPermit))
                return;

            foreach (var pos in _weaponSchoolLocations)
            {
                Blip blip = World.CreateBlip(pos);
                blip.Sprite = BlipSprite.Shotgun; // você já confirmou que existe no seu SHVDN
                blip.Color = BlipColor.Red;
                blip.Name = "Teste de Porte de Arma";
                blip.IsShortRange = true;

                _blips.Add(blip);
            }
        }

        // ===============================
        // 📋 MENU
        // ===============================
        private void CreateMenu()
        {
            _uiPool = new ObjectPool();

            _menu = new NativeMenu(
                "AMMU-NATION",
                "Teste de porte de arma"
            );

            _uiPool.Add(_menu);

            // ✅ Só adiciona a opção SE NÃO tiver porte válido
            if (LicenseTestAvailabilityService.ShouldShowTest(LicenseType.WeaponPermit))
            {
                var startTestItem = new NativeItem("Iniciar teste de porte de arma");
                _menu.Add(startTestItem);

                startTestItem.Activated += (s, e) => StartWeaponTest();
            }
        }

        // ===============================
        // ▶ START TEST
        // ===============================
        private void StartWeaponTest()
        {
            if (_currentSession != null)
                return;

            _menu.Visible = false;
            _currentSession = new WeaponCombatTestSession();
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
            // Se já tem porte válido, não interage com teste
            if (!LicenseTestAvailabilityService.ShouldShowTest(LicenseType.WeaponPermit))
            {
                _menu.Visible = false;
                return;
            }

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            foreach (var pos in _weaponSchoolLocations)
            {
                float dist = player.Position.DistanceTo(pos);

                if (dist < 3.0f)
                {
                    global::GTA.UI.Screen.ShowHelpTextThisFrame(
                        "Pressione ~INPUT_VEH_HEADLIGHT~ (H) para iniciar o teste de porte de arma"
                    );

                    if (Game.IsKeyPressed(Keys.H))
                    {
                        _menu.Visible = !_menu.Visible;
                    }

                    return;
                }
            }

            _menu.Visible = false;
        }

        // ===============================
        // 🧹 CLEANUP
        // ===============================
        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                foreach (var blip in _blips)
                    blip.Delete();
            }
            catch { }
        }
    }
}
