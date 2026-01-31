using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core;
using RLF.Core.Debug;
using RLF.Core.Identity.Enums;
using RLF.GTA.CoreIntegration.Identity.WeaponSchool;
using System;
using System.Collections.Generic;

namespace RLF.GTA.CoreIntegration.Identity.WeaponShop
{
    public sealed class WeaponShopAccessController : Script
    {
        // 📍 Coordenadas das Ammu-Nations
        private readonly List<Vector3> _weaponShops = new List<Vector3>
        {
            new Vector3(22.09f, -1107.28f, 29.80f),
            new Vector3(252.77f, -48.42f, 69.94f),
            new Vector3(-662.18f, -935.04f, 21.83f),
            new Vector3(810.25f, -2157.60f, 29.62f),
            new Vector3(1693.44f, 3759.50f, 34.71f)
        };

        private const float BLOCK_RADIUS = 2.5f;
        private bool _blockedThisFrame;

        public WeaponShopAccessController()
        {
            Tick += OnTick;

            try
            {
                RLFDebug.Info(DebugChannel.System, "WeaponShopAccessController iniciado");
            }
            catch { }
        }

        private void OnTick(object sender, EventArgs e)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
                return;

            // 🔓 Durante teste de porte, não bloqueia
            if (WeaponTestContext.IsActive)
                return;

            _blockedThisFrame = false;

            foreach (var shop in _weaponShops)
            {
                if (player.Position.DistanceTo(shop) <= BLOCK_RADIUS)
                {
                    HandleWeaponShopAccess(player);
                    break;
                }
            }
        }

        private void HandleWeaponShopAccess(Ped player)
        {
            var docSystem = RLFCore.Instance.Systems.Get("DocumentSystem")
                as RLF.Core.Identity.DocumentSystem;

            if (docSystem == null)
                return;

            bool hasPermit = docSystem.HasValidLicense(LicenseType.WeaponPermit);
            if (hasPermit)
                return;

            // 🚫 SEM PORTE — BLOQUEIA
            BlockInteraction(player);

            if (!_blockedThisFrame)
            {
                global::GTA.UI.Notification.Show(
                "❌ Porte de arma obrigatório\nCompra legal bloqueada"
            );
                _blockedThisFrame = true;
            }
        }

        private void BlockInteraction(Ped player)
        {
            // Impede abrir menus / interagir
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.Context, true);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.Attack, true);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.Aim, true);

            // Polícia nunca reage
            Game.Player.WantedLevel = 0;
            Function.Call(Hash.CLEAR_PLAYER_WANTED_LEVEL, Game.Player.Handle);
        }
    }
}
