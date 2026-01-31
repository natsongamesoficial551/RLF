using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core;
using RLF.Core.Debug;
using RLF.Core.Identity.Enums;
using System;
using System.Collections.Generic;

namespace RLF.GTA.CoreIntegration.Identity.WeaponSchool
{
    public sealed class WeaponCombatTestSession
    {
        public bool IsFinished { get; private set; }

        // ===============================
        // COORDENADAS FIXAS (MISSÃO)
        // ===============================
        private static readonly Vector3 PlayerSpawn =
            new Vector3(1303.344f, 1104.304f, 105.635f);

        private static readonly Vector3[] EnemySpawns =
        {
            new Vector3(1349.553f, 1149.181f, 113.759f),
            new Vector3(1380.083f, 1146.210f, 114.334f),
            new Vector3(1380.895f, 1149.094f, 114.334f)
        };

        private const float CleanRadius = 60f;
        private const WeaponHash TestWeapon = WeaponHash.Pistol;

        // ===============================
        // STATE
        // ===============================
        private readonly List<Ped> _enemies = new List<Ped>();
        private readonly List<Blip> _enemyBlips = new List<Blip>();

        private int _initialHealth;
        private int _initialArmor;

        // ===============================
        // START
        // ===============================
        public WeaponCombatTestSession()
        {
            Start();
        }

        private void Start()
        {
            WeaponTestContext.Enter();

            Ped player = Game.Player.Character;

            // 🔒 Polícia SEMPRE desligada
            ForceNoPolice();

            // Teleporta jogador
            player.Position = PlayerSpawn;

            // Setup de segurança
            player.Health = player.MaxHealth;
            player.Armor = 100;
            player.CanRagdoll = false;

            _initialHealth = player.Health;
            _initialArmor = player.Armor;

            // Armas
            player.Weapons.RemoveAll();
            player.Weapons.Give(TestWeapon, 120, true, true);

            // Limpa área ANTES
            ClearArea(PlayerSpawn, CleanRadius);

            // Spawna inimigos
            SpawnEnemies();

            global::GTA.UI.Notification.Show(
            "🔫 Teste de porte iniciado\n❗ Elimine os 3 inimigos sem tomar nenhum tiro"
        );

            try { RLFDebug.Info(DebugChannel.System, "[WeaponCombatTest] Iniciado"); } catch { }
        }

        // ===============================
        // 🔁 TICK
        // ===============================
        public void Tick()
        {
            if (IsFinished)
                return;

            Ped player = Game.Player.Character;

            // 🔒 Polícia sempre 0 (durante)
            ForceNoPolice();

            // Entrou em veículo
            if (player.IsInVehicle())
            {
                Fail("Entrou em veículo");
                return;
            }

            // Troca de arma
            if (player.Weapons.Current.Hash != TestWeapon)
            {
                Fail("Troca de arma não permitida");
                return;
            }

            // Tomou QUALQUER dano
            if (player.Health < _initialHealth ||
                player.Armor < _initialArmor)
            {
                Fail("Você foi atingido");
                return;
            }

            // Morreu (failsafe)
            if (player.IsDead)
            {
                Fail("Jogador morreu");
                return;
            }

            // Limpa NPCs externos continuamente
            ClearArea(player.Position, CleanRadius);

            // Verifica inimigos
            bool allDead = true;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var enemy = _enemies[i];

                if (enemy == null || !enemy.Exists())
                    continue;

                // 🔴 Se morreu, remove o blip imediatamente
                if (enemy.IsDead)
                {
                    if (i < _enemyBlips.Count)
                    {
                        var blip = _enemyBlips[i];
                        if (blip != null && blip.Exists())
                            blip.Delete();
                    }
                    continue;
                }

                // Ainda existe inimigo vivo
                allDead = false;
            }


            if (allDead)
                Pass();
        }

        private static Vector3 GetGroundSafePosition(Vector3 raw)
        {
            try
            {
                var outZ = new OutputArgument();
                bool found = Function.Call<bool>(
                    Hash.GET_GROUND_Z_FOR_3D_COORD,
                    raw.X, raw.Y, raw.Z + 200f,
                    outZ,
                    false
                );

                if (found)
                {
                    float z = outZ.GetResult<float>();
                    return new Vector3(raw.X, raw.Y, z + 1.0f);
                }
            }
            catch { }

            // fallback: retorna original
            return raw;
        }


        // ===============================
        // 👥 NPCS + BLIPS
        // ===============================
        private void SpawnEnemies()
        {
            Ped player = Game.Player.Character;

            foreach (var pos in EnemySpawns)
            {
                Vector3 safePos = GetGroundSafePosition(pos);

                Ped enemy = World.CreatePed(
                    PedHash.Cop01SMY,
                    safePos
                );

                if (enemy == null || !enemy.Exists())
                    continue;

                enemy.Weapons.RemoveAll();
                enemy.Weapons.Give(TestWeapon, 120, true, true);

                enemy.BlockPermanentEvents = true;
                enemy.CanRagdoll = true;

                Function.Call(Hash.SET_PED_ACCURACY, enemy.Handle, 12);
                Function.Call(Hash.SET_PED_SHOOT_RATE, enemy.Handle, 350);

                Function.Call(
                    Hash.TASK_COMBAT_PED,
                    enemy.Handle,
                    player.Handle,
                    0,
                    16
                );

                _enemies.Add(enemy);

                // 🔴 BLIP VERMELHO (CÍRCULO)
                Blip blip = enemy.AddBlip();
                blip.Color = BlipColor.Red;
                blip.Scale = 0.8f;
                blip.Name = "Inimigo (Teste de Porte)";
                _enemyBlips.Add(blip);
            }
        }

        // ===============================
        // 🧹 LIMPEZA DE ÁREA
        // ===============================
        private void ClearArea(Vector3 center, float radius)
        {
            foreach (Ped ped in World.GetNearbyPeds(center, radius))
            {
                if (ped == null || !ped.Exists())
                    continue;

                if (ped == Game.Player.Character)
                    continue;

                if (_enemies.Contains(ped))
                    continue;

                try { ped.Delete(); } catch { }
            }
        }

        // ===============================
        // 🚓 POLÍCIA OFF
        // ===============================
        private void ForceNoPolice()
        {
            try
            {
                Game.Player.WantedLevel = 0;

                // Opcional: reforça limpando perseguição / crimes
                Function.Call(Hash.CLEAR_PLAYER_WANTED_LEVEL, Game.Player.Handle);
                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player.Handle, true);
                Function.Call(Hash.SET_EVERYONE_IGNORE_PLAYER, Game.Player.Handle, true);
            }
            catch { }
        }

        // ===============================
        // ✅ PASS
        // ===============================
        private void Pass()
        {
            ForceNoPolice();

            WeaponTestContext.Exit();
            Cleanup();
            GivePermit();

            global::GTA.UI.Notification.Show(
            "✅ Teste aprovado\nPorte de arma concedido"
        );

            try { RLFDebug.Info(DebugChannel.System, "[WeaponCombatTest] Aprovado"); } catch { }

            IsFinished = true;
        }

        // ===============================
        // ❌ FAIL
        // ===============================
        private void Fail(string reason)
        {
            ForceNoPolice();

            WeaponTestContext.Exit();
            Cleanup();

            global::GTA.UI.Notification.Show(
            $"❌ Teste reprovado\n{reason}"
        );

            try { RLFDebug.Warning(DebugChannel.System, $"[WeaponCombatTest] Reprovado - {reason}"); } catch { }

            IsFinished = true;
        }

        // ===============================
        // 📄 LICENSE
        // ===============================
        private void GivePermit()
        {
            var docSystem =
                RLFCore.Instance.Systems.Get("DocumentSystem")
                as RLF.Core.Identity.DocumentSystem;

            if (docSystem == null)
                return;

            docSystem.GrantLicense(
                LicenseType.WeaponPermit,
                validityDays: 365,
                reason: "Aprovado no teste de combate para porte de arma"
            );
        }

        // ===============================
        // 🧹 CLEANUP
        // ===============================
        private void Cleanup()
        {
            try
            {
                foreach (var blip in _enemyBlips)
                {
                    if (blip != null && blip.Exists())
                        blip.Delete();
                }
            }
            catch { }
            _enemyBlips.Clear();

            try
            {
                foreach (var enemy in _enemies)
                {
                    if (enemy != null && enemy.Exists())
                        enemy.Delete();
                }
            }
            catch { }
            _enemies.Clear();

            try
            {
                Ped player = Game.Player.Character;
                player.CanRagdoll = true;
            }
            catch { }
        }
    }
}
