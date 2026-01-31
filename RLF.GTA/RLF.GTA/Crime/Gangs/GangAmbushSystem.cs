using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using RLF.Core.Gangs;

namespace RLF.GTA.Gangs
{
    /// <summary>
    /// Sistema de emboscadas de gangues rivais contra o player
    /// REALISTA: Muito raro, só acontece em dias diferentes, 4 veículos perseguindo
    /// </summary>
    public class GangAmbushSystem
    {
        private readonly GangSystem _gangSystem;
        private readonly PlayerGangMembership _playerMembership;
        private readonly Random _random;

        private DateTime _lastAmbushDate;
        private bool _hasAmbushedToday;
        private ActiveAmbush _currentAmbush;

        // DEBUG
        private bool _debugEnabled = true;

        // CONFIGURAÇÕES REALISTAS
        private const float AMBUSH_CHANCE_PER_DAY = 0.15f; // 15% de chance por dia de jogo
        private const int MIN_DAYS_BETWEEN_AMBUSHES = 3; // Mínimo 3 dias entre emboscadas
        private const int AMBUSH_VEHICLES = 4; // Sempre 4 veículos
        private const int ENEMIES_PER_VEHICLE = 2; // 2 inimigos por veículo = 8 total
        private const float SPAWN_DISTANCE = 80f; // Spawnam a 80m do player

        public bool IsAmbushActive => _currentAmbush != null && _currentAmbush.IsActive;

        private class ActiveAmbush
        {
            public bool IsActive { get; set; }
            public List<Vehicle> Vehicles { get; set; }
            public List<Ped> Enemies { get; set; }
            public GangType AttackingGang { get; set; }
            public DateTime StartedAt { get; set; }
            public int EnemiesKilled { get; set; }

            public ActiveAmbush()
            {
                Vehicles = new List<Vehicle>();
                Enemies = new List<Ped>();
                IsActive = true;
                StartedAt = DateTime.Now;
                EnemiesKilled = 0;
            }
        }

        public GangAmbushSystem(GangSystem gangSystem, PlayerGangMembership playerMembership)
        {
            _gangSystem = gangSystem ?? throw new ArgumentNullException(nameof(gangSystem));
            _playerMembership = playerMembership ?? throw new ArgumentNullException(nameof(playerMembership));
            
            _random = new Random();
            _lastAmbushDate = DateTime.MinValue;
            _hasAmbushedToday = false;
        }

        public void Update(float deltaTime)
        {
            // Só funciona se player estiver em gangue
            if (!_playerMembership.IsInGang()) return;

            // Atualiza emboscada ativa
            if (IsAmbushActive)
            {
                UpdateActiveAmbush();
            }
            else
            {
                // Verifica se deve tentar emboscada
                CheckForAmbushTrigger();
            }
        }

        private void CheckForAmbushTrigger()
        {
            // Pega dia atual do jogo
            int currentDay = Function.Call<int>(Hash.GET_CLOCK_DAY_OF_MONTH);
            int currentMonth = Function.Call<int>(Hash.GET_CLOCK_MONTH);
            int currentYear = Function.Call<int>(Hash.GET_CLOCK_YEAR);

            DateTime gameDate = new DateTime(currentYear, currentMonth, currentDay);

            // Verifica se é um novo dia
            if (gameDate.Date != _lastAmbushDate.Date)
            {
                _hasAmbushedToday = false;
                _lastAmbushDate = gameDate;

                if (_debugEnabled)
                {
                    Notification.Show($"~y~[DEBUG] New game day: {gameDate:yyyy-MM-dd}");
                }
            }

            // Se já teve emboscada hoje, não tenta de novo
            if (_hasAmbushedToday) return;

            // Verifica intervalo mínimo entre emboscadas
            TimeSpan timeSinceLastAmbush = gameDate - _lastAmbushDate;
            if (timeSinceLastAmbush.Days < MIN_DAYS_BETWEEN_AMBUSHES)
            {
                if (_debugEnabled && _random.NextDouble() < 0.01f) // Debug ocasional
                {
                    Notification.Show($"~y~[DEBUG] Too soon for ambush ({timeSinceLastAmbush.Days}/{MIN_DAYS_BETWEEN_AMBUSHES} days)");
                }
                return;
            }

            // Player precisa estar dirigindo em área urbana
            Ped player = Game.Player.Character;
            if (!player.IsInVehicle() || player.CurrentVehicle.Speed < 5f)
                return;

            // Verifica se está em área urbana (não em montanha/deserto)
            string zone = Function.Call<string>(Hash.GET_NAME_OF_ZONE, player.Position.X, player.Position.Y, player.Position.Z);
            string[] urbanZones = { "DOWNT", "LACT", "STAD", "ELYSIAN", "GOLF", "VESP", "DELSOL", "LMESA", "CYPRE" };
            
            bool isUrban = false;
            foreach (string urbanZone in urbanZones)
            {
                if (zone.Contains(urbanZone))
                {
                    isUrban = true;
                    break;
                }
            }

            if (!isUrban) return;

            // Rola chance de emboscada (só 1x por dia)
            float roll = (float)_random.NextDouble();
            if (roll < AMBUSH_CHANCE_PER_DAY)
            {
                TriggerAmbush();
                _hasAmbushedToday = true;
            }
            else if (_debugEnabled && roll < AMBUSH_CHANCE_PER_DAY + 0.05f)
            {
                Notification.Show($"~y~[DEBUG] Ambush roll failed ({roll:F2} >= {AMBUSH_CHANCE_PER_DAY})");
            }
        }

        private void TriggerAmbush()
        {
            Ped player = Game.Player.Character;
            if (!player.IsInVehicle()) return;

            // Escolhe gangue inimiga
            GangType playerGang = _playerMembership.CurrentGang.Value;
            var allGangs = _gangSystem.GetAllGangs();
            var enemyGangs = allGangs.FindAll(g => 
                g.Type != playerGang && 
                _gangSystem.GetGang(playerGang).IsEnemy(g.Type));

            if (enemyGangs.Count == 0)
            {
                if (_debugEnabled)
                    Notification.Show($"~r~[DEBUG] No enemy gangs found for ambush");
                return;
            }

            GangType attackingGang = enemyGangs[_random.Next(enemyGangs.Count)].Type;

            _currentAmbush = new ActiveAmbush
            {
                AttackingGang = attackingGang
            };

            // Spawn 4 veículos em posições diferentes ao redor do player
            Vector3 playerPos = player.Position;
            Vector3 playerVelocity = player.CurrentVehicle.Velocity;
            Vector3 behind = playerPos - playerVelocity.Normalized * SPAWN_DISTANCE;

            // Posições: atrás esquerda, atrás direita, esquerda, direita
            Vector3[] spawnOffsets = new Vector3[]
            {
                new Vector3(-20f, -SPAWN_DISTANCE, 0f),  // Atrás esquerda
                new Vector3(20f, -SPAWN_DISTANCE, 0f),   // Atrás direita
                new Vector3(-30f, -40f, 0f),             // Esquerda
                new Vector3(30f, -40f, 0f)               // Direita
            };

            string vehicleModel = GetGangVehicleModel(attackingGang);
            string pedModel = GetGangPedModel(attackingGang);

            Model vehModel = new Model(vehicleModel);
            Model npcModel = new Model(pedModel);
            
            vehModel.Request(1000);
            npcModel.Request(1000);

            if (!vehModel.IsLoaded || !npcModel.IsLoaded)
            {
                if (_debugEnabled)
                    Notification.Show($"~r~[DEBUG] Failed to load models for ambush");
                return;
            }

            for (int i = 0; i < AMBUSH_VEHICLES; i++)
            {
                Vector3 spawnPos = behind + spawnOffsets[i];
                spawnPos.Z = World.GetGroundHeight(spawnPos);

                Vehicle ambushVehicle = World.CreateVehicle(vehModel, spawnPos);
                if (ambushVehicle == null || !ambushVehicle.Exists())
                    continue;

                _currentAmbush.Vehicles.Add(ambushVehicle);

                // Spawn 2 NPCs por veículo
                for (int j = 0; j < ENEMIES_PER_VEHICLE; j++)
                {
                    Ped enemy = World.CreatePed(npcModel, spawnPos);
                    if (enemy == null || !enemy.Exists())
                        continue;

                    // Configuração do inimigo
                    int gangHash = Function.Call<int>(Hash.GET_HASH_KEY, $"GANG_{attackingGang}");
                    enemy.RelationshipGroup = gangHash;

                    // Arma
                    WeaponHash[] weapons = { WeaponHash.Pistol, WeaponHash.MicroSMG, WeaponHash.SMG };
                    enemy.Weapons.Give(weapons[_random.Next(weapons.Length)], 999, true, true);

                    // Coloca no veículo
                    VehicleSeat seat = (j == 0) ? VehicleSeat.Driver : VehicleSeat.RightFront;
                    enemy.SetIntoVehicle(ambushVehicle, seat);

                    // Comportamento: perseguir e atirar no player
                    if (j == 0) // Driver
                    {
                        enemy.Task.VehicleChase(player);
                    }
                    else // Passageiro
                    {
                        Function.Call(Hash.TASK_COMBAT_PED, enemy.Handle, player.Handle, 0, 16);
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, enemy.Handle, 5, true); // Can use vehicles
                    }

                    Function.Call(Hash.SET_PED_COMBAT_ABILITY, enemy.Handle, 2); // Professional
                    Function.Call(Hash.SET_PED_ACCURACY, enemy.Handle, 40); // 40% accuracy
                    Function.Call(Hash.SET_PED_FIRING_PATTERN, enemy.Handle, 0xC6EE6B4C); // Burst fire

                    _currentAmbush.Enemies.Add(enemy);
                }

                // Veículo não pode ser destruído facilmente
                ambushVehicle.EngineHealth = 1000f;
                ambushVehicle.BodyHealth = 1000f;
            }

            vehModel.MarkAsNoLongerNeeded();
            npcModel.MarkAsNoLongerNeeded();

            // Notificações
            Notification.Show($"~r~EMBOSCADA!~n~~w~{attackingGang.GetDisplayName()} está te caçando!", true);
            Notification.Show($"~y~{AMBUSH_VEHICLES} veículos | {AMBUSH_VEHICLES * ENEMIES_PER_VEHICLE} inimigos~n~~w~Sobreviva!");

            if (_debugEnabled)
            {
                Notification.Show($"~g~[DEBUG] Ambush triggered!");
                Notification.Show($"~g~[DEBUG] Gang: {attackingGang}");
                Notification.Show($"~g~[DEBUG] Vehicles: {_currentAmbush.Vehicles.Count}");
                Notification.Show($"~g~[DEBUG] Enemies: {_currentAmbush.Enemies.Count}");
            }

            // Play som de alerta
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "CHECKPOINT_UNDER_THE_BRIDGE", "HUD_MINI_GAME_SOUNDSET");
        }

        private void UpdateActiveAmbush()
        {
            if (_currentAmbush == null || !_currentAmbush.IsActive)
                return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || !player.IsAlive)
            {
                EndAmbush(false);
                return;
            }

            // Conta inimigos vivos
            int aliveEnemies = 0;
            foreach (Ped enemy in _currentAmbush.Enemies)
            {
                if (enemy != null && enemy.Exists() && !enemy.IsDead)
                {
                    aliveEnemies++;
                }
            }

            _currentAmbush.EnemiesKilled = _currentAmbush.Enemies.Count - aliveEnemies;

            // Verifica se player matou todos
            if (aliveEnemies == 0)
            {
                EndAmbush(true);
                return;
            }

            // Verifica se player fugiu (muito longe de todos os veículos por 30s)
            bool isFarFromAll = true;
            foreach (Vehicle veh in _currentAmbush.Vehicles)
            {
                if (veh == null || !veh.Exists())
                    continue;

                float dist = player.Position.DistanceTo(veh.Position);
                if (dist < 150f)
                {
                    isFarFromAll = false;
                    break;
                }
            }

            if (isFarFromAll && (DateTime.Now - _currentAmbush.StartedAt).TotalSeconds > 30)
            {
                EndAmbush(true);
                return;
            }

            // Debug info periódico
            if (_debugEnabled && (DateTime.Now - _currentAmbush.StartedAt).TotalSeconds % 5 < 0.5f)
            {
                Screen.ShowSubtitle($"~y~[EMBOSCADA]~n~" +
                    $"~r~Inimigos: {aliveEnemies}/{_currentAmbush.Enemies.Count}~n~" +
                    $"~w~Mortos: {_currentAmbush.EnemiesKilled}~n~" +
                    $"~w~Tempo: {(DateTime.Now - _currentAmbush.StartedAt).TotalSeconds:F0}s", 500);
            }
        }

        private void EndAmbush(bool survived)
        {
            if (_currentAmbush == null)
                return;

            if (survived)
            {
                int reward = _currentAmbush.EnemiesKilled * 100; // $100 por inimigo morto
                int respectGain = _currentAmbush.EnemiesKilled * 2;

                Game.Player.Money += reward;
                _playerMembership.IncreaseRespect(respectGain);

                Notification.Show($"~g~EMBOSCADA SOBREVIVIDA!~n~" +
                    $"~w~Inimigos eliminados: {_currentAmbush.EnemiesKilled}~n~" +
                    $"~g~+${reward}~n~" +
                    $"~y~+{respectGain} Respeito");

                if (_debugEnabled)
                {
                    Notification.Show($"~g~[DEBUG] Ambush survived - {_currentAmbush.EnemiesKilled} kills");
                }
            }
            else
            {
                Notification.Show($"~r~Você morreu na emboscada");
            }

            // Cleanup
            foreach (Ped enemy in _currentAmbush.Enemies)
            {
                if (enemy != null && enemy.Exists())
                {
                    enemy.MarkAsNoLongerNeeded();
                }
            }

            foreach (Vehicle veh in _currentAmbush.Vehicles)
            {
                if (veh != null && veh.Exists())
                {
                    veh.MarkAsNoLongerNeeded();
                }
            }

            _currentAmbush.IsActive = false;
            _currentAmbush = null;
        }

        private string GetGangVehicleModel(GangType gang)
        {
            switch (gang)
            {
                case GangType.Families: return "BISON";
                case GangType.Ballas: return "TORNADO";
                case GangType.Vagos: return "PEYOTE";
                case GangType.Marabunta: return "EMPEROR";
                case GangType.ArmenianMob: return "WASHINGTON";
                case GangType.TriadTong: return "SULTAN";
                case GangType.KoreanMob: return "FUTO";
                case GangType.LostMC: return "DAEMON"; // Motos! Pode spawnar grupo de motoqueiros
                default: return "BISON";
            }
        }

        private string GetGangPedModel(GangType gang)
        {
            switch (gang)
            {
                case GangType.Families: return "g_m_y_famca_01";
                case GangType.Ballas: return "g_m_y_ballaeast_01";
                case GangType.Vagos: return "g_m_y_salvagoon_03";
                case GangType.Marabunta: return "g_m_y_salvaboss_01";
                case GangType.ArmenianMob: return "g_m_m_armboss_01";
                case GangType.TriadTong: return "g_m_m_chigoon_02";
                case GangType.KoreanMob: return "g_m_m_korboss_01";
                case GangType.LostMC: return "g_m_y_lost_01";
                default: return "g_m_y_famca_01";
            }
        }

        public void ForceAmbush()
        {
            if (IsAmbushActive)
            {
                Notification.Show($"~r~Já há uma emboscada ativa!");
                return;
            }

            if (!_playerMembership.IsInGang())
            {
                Notification.Show($"~r~Você precisa estar em uma gangue!");
                return;
            }

            TriggerAmbush();
        }

        public void SetDebugMode(bool enabled)
        {
            _debugEnabled = enabled;
        }

        public void Shutdown()
        {
            if (_currentAmbush != null)
            {
                EndAmbush(false);
            }
        }
    }
}
