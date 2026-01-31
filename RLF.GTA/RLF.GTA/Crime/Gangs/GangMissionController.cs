using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using RLF.Core.Crime;
using RLF.Core.Economy;
using RLF.Core.Economy.Transactions;
using RLF.Core.Gangs;
using RLF.Core.Gangs.Missions;
using CoreCrimeType = RLF.Core.Crime.CrimeType;

namespace RLF.GTA.Gangs
{
    /// <summary>
    /// Controla a execução de missões de gangue
    /// Gerencia objetivos, spawns, NPCs aliados e recompensas
    /// </summary>
    public class GangMissionController
    {
        private readonly GangSystem _gangSystem;
        private readonly PlayerGangMembership _playerMembership;
        private readonly CrimeSystem _crimeSystem;
        private readonly EconomySystem _economySystem;
        private readonly GangMissionGenerator _missionGenerator;

        private GangMission _activeMission;
        private List<Ped> _allyNPCs;
        private List<Ped> _enemyNPCs;
        private List<Vehicle> _missionVehicles;
        private Ped _targetPed;
        private Vehicle _targetVehicle;
        private Blip _missionBlip;
        private Blip _objectiveBlip;

        private DateTime _lastUpdate;
        private bool _debugEnabled = true;

        private const float UPDATE_INTERVAL = 0.5f;
        private float _updateTimer;

        public bool HasActiveMission => _activeMission != null && _activeMission.State == MissionState.Active;

        public GangMissionController(
            GangSystem gangSystem,
            PlayerGangMembership playerMembership,
            CrimeSystem crimeSystem,
            EconomySystem economySystem,
            GangMissionGenerator missionGenerator)
        {
            _gangSystem = gangSystem ?? throw new ArgumentNullException(nameof(gangSystem));
            _playerMembership = playerMembership ?? throw new ArgumentNullException(nameof(playerMembership));
            _crimeSystem = crimeSystem ?? throw new ArgumentNullException(nameof(crimeSystem));
            _economySystem = economySystem ?? throw new ArgumentNullException(nameof(economySystem));
            _missionGenerator = missionGenerator ?? throw new ArgumentNullException(nameof(missionGenerator));

            _allyNPCs = new List<Ped>();
            _enemyNPCs = new List<Ped>();
            _missionVehicles = new List<Vehicle>();
            _lastUpdate = DateTime.Now;
            _updateTimer = 0f;
        }

        public void Update(float deltaTime)
        {
            if (!HasActiveMission) return;

            _updateTimer += deltaTime;
            if (_updateTimer < UPDATE_INTERVAL) return;
            _updateTimer = 0f;

            try
            {
                // Verifica timeout
                if (_activeMission.HasExpired())
                {
                    FailMission("Tempo esgotado!");
                    return;
                }

                // Atualiza baseado no tipo de missão
                switch (_activeMission.Type)
                {
                    case GangMissionType.StoreRobbery:
                        UpdateStoreRobbery();
                        break;

                    case GangMissionType.Kidnapping:
                        UpdateKidnapping();
                        break;

                    case GangMissionType.CollectProtectionMoney:
                        UpdateProtectionMoney();
                        break;

                    case GangMissionType.VehicleTheft:
                        UpdateVehicleTheft();
                        break;

                    case GangMissionType.TerritoryTakeover:
                        UpdateTerritoryTakeover();
                        break;

                    case GangMissionType.DrugDelivery:
                        UpdateDrugDelivery();
                        break;

                    case GangMissionType.Intimidation:
                        UpdateIntimidation();
                        break;

                    case GangMissionType.Ambush:
                        UpdateAmbush();
                        break;

                    case GangMissionType.HitContract:
                        UpdateHitContract();
                        break;
                }

                // Verifica se completou
                if (_activeMission.AreAllObjectivesComplete())
                {
                    CompleteMission();
                }
            }
            catch (Exception ex)
            {
                if (_debugEnabled)
                {
                    Notification.Show($"~r~[DEBUG] Mission Update Error: {ex.Message}");
                }
            }
        }

        public bool StartMission(string missionId)
        {
            if (HasActiveMission) return false;

            _activeMission = _missionGenerator.GetAvailableMissions().FirstOrDefault(m => m.Id == missionId);
            if (_activeMission == null) return false;

            _missionGenerator.AcceptMission(missionId);
            _activeMission.SetInProgress();

            // Spawn NPCs aliados se necessário
            if (_activeMission.RequiresGangMembers && _activeMission.MinimumGangMembers > 0)
            {
                SpawnAllyNPCs(_activeMission.MinimumGangMembers);
            }

            // Cria blip da missão
            CreateMissionBlip();

            // Setup específico por tipo
            SetupMissionSpecifics();

            Notification.Show($"~b~MISSÃO INICIADA~n~~w~{_activeMission.Name}");
            Notification.Show($"~y~{_activeMission.Description}");

            if (_debugEnabled)
            {
                Notification.Show($"~g~[DEBUG] Mission started: {_activeMission.Name}");
                Notification.Show($"~g~[DEBUG] Objectives: {_activeMission.Objectives.Count}");
            }

            return true;
        }

        private void SetupMissionSpecifics()
        {
            switch (_activeMission.Type)
            {
                case GangMissionType.Kidnapping:
                    SpawnTargetPed();
                    break;

                case GangMissionType.VehicleTheft:
                    SpawnTargetVehicle();
                    break;

                case GangMissionType.TerritoryTakeover:
                case GangMissionType.Ambush:
                    SpawnEnemyNPCs(5);
                    break;

                case GangMissionType.HitContract:
                    SpawnTargetPed();
                    break;
            }
        }

        // ===== UPDATES POR TIPO DE MISSÃO =====

        private void UpdateStoreRobbery()
        {
            Ped player = Game.Player.Character;
            Vector3 targetPos = new Vector3(_activeMission.TargetX, _activeMission.TargetY, _activeMission.TargetZ);
            float distance = player.Position.DistanceTo(targetPos);

            // Objetivo 0: Vá até a loja
            if (!_activeMission.Objectives[0].IsCompleted && distance <= _activeMission.TargetRadius)
            {
                _activeMission.UpdateObjective(0, 1);
                Notification.Show("~g~Objetivo completo! ~w~Entre na loja e roube");
                
                if (_debugEnabled)
                    Notification.Show($"~g~[DEBUG] Reached store area");
            }

            // Objetivo 1 e 2: Intimidar e roubar (detectado por EstablishmentRobbery)
            // Objetivo 3: Escapar
            if (_activeMission.Objectives[2].IsCompleted && distance > 100f)
            {
                _activeMission.UpdateObjective(3, 1);
            }
        }

        private void UpdateKidnapping()
        {
            Ped player = Game.Player.Character;
            
            // Objetivo 0: Encontrar rico
            if (!_activeMission.Objectives[0].IsCompleted)
            {
                if (_targetPed == null || !_targetPed.Exists())
                {
                    // Procura rico próximo
                    Ped[] nearbyPeds = World.GetNearbyPeds(player, 100f);
                    foreach (Ped ped in nearbyPeds)
                    {
                        if (ped == null || !ped.Exists() || ped.IsPlayer) continue;
                        
                        // Verifica se é businessman
                        Model model = ped.Model;
                        if (model.Hash == Function.Call<int>(Hash.GET_HASH_KEY, "a_m_m_business_01") ||
                            model.Hash == Function.Call<int>(Hash.GET_HASH_KEY, "a_f_m_business_02"))
                        {
                            _targetPed = ped;
                            CreateObjectiveBlip(_targetPed.Position);
                            _activeMission.UpdateObjective(0, 1);
                            Notification.Show("~g~Alvo identificado! ~w~Capture-o sem matar");
                            break;
                        }
                    }
                }
            }

            // Objetivo 1: Capturar (ped com mãos levantadas perto do player)
            if (_activeMission.Objectives[0].IsCompleted && !_activeMission.Objectives[1].IsCompleted)
            {
                if (_targetPed != null && _targetPed.Exists() && !_targetPed.IsDead)
                {
                    float dist = player.Position.DistanceTo(_targetPed.Position);
                    
                    // Se player apontou arma e target está perto
                    if (player.IsAiming && dist < 5f)
                    {
                        _targetPed.Task.HandsUp(10000);
                        _activeMission.UpdateObjective(1, 1);
                        
                        // Define ponto de resgate
                        Vector3 rescuePoint = new Vector3(-1100f, -1600f, 4f); // La Puerta warehouse
                        CreateObjectiveBlip(rescuePoint);
                        
                        Notification.Show("~g~Alvo capturado! ~w~Leve-o ao ponto de resgate");
                        
                        if (_debugEnabled)
                            Notification.Show($"~g~[DEBUG] Target captured, go to rescue point");
                    }
                }
                else if (_targetPed != null && _targetPed.IsDead)
                {
                    FailMission("Você matou o alvo!");
                }
            }

            // Objetivo 2: Levar ao resgate
            if (_activeMission.Objectives[1].IsCompleted)
            {
                Vector3 rescuePoint = new Vector3(-1100f, -1600f, 4f);
                float dist = player.Position.DistanceTo(rescuePoint);
                
                if (dist < 10f && _targetPed != null && _targetPed.Exists())
                {
                    _activeMission.UpdateObjective(2, 1);
                }
            }
        }

        private void UpdateProtectionMoney()
        {
            Ped player = Game.Player.Character;
            Vector3 targetPos = new Vector3(_activeMission.TargetX, _activeMission.TargetY, _activeMission.TargetZ);
            float distance = player.Position.DistanceTo(targetPos);

            // Objetivo 0: Ir ao local
            if (!_activeMission.Objectives[0].IsCompleted && distance <= _activeMission.TargetRadius)
            {
                _activeMission.UpdateObjective(0, 1);
                Notification.Show("~g~Local alcançado! ~w~Pressione E para coletar");
            }

            // Objetivo 1: Coletar (detectado por E pressionado)
            if (_activeMission.Objectives[0].IsCompleted && !_activeMission.Objectives[1].IsCompleted)
            {
                Screen.ShowHelpTextThisFrame("Pressione ~INPUT_CONTEXT~ para coletar o dinheiro");
                
                if (Game.IsControlJustPressed(Control.Context) && distance < 5f)
                {
                    _activeMission.UpdateObjective(1, 1);
                }
            }
        }

        private void UpdateVehicleTheft()
        {
            Ped player = Game.Player.Character;

            // Objetivo 0: Encontrar veículo
            if (!_activeMission.Objectives[0].IsCompleted)
            {
                if (_targetVehicle == null || !_targetVehicle.Exists())
                {
                    // Procura veículo próximo do modelo correto
                    Vehicle[] nearbyVehicles = World.GetNearbyVehicles(player, 150f);
                    foreach (Vehicle veh in nearbyVehicles)
                    {
                        if (veh == null || !veh.Exists()) continue;
                        
                        string modelName = Function.Call<string>(Hash.GET_DISPLAY_NAME_FROM_VEHICLE_MODEL, veh.Model.Hash);
                        
                        if (modelName == _activeMission.TargetVehicleModel)
                        {
                            _targetVehicle = veh;
                            CreateObjectiveBlip(_targetVehicle.Position);
                            _activeMission.UpdateObjective(0, 1);
                            Notification.Show($"~g~{_activeMission.TargetVehicleModel} encontrado! ~w~Roube-o");
                            break;
                        }
                    }
                }
            }

            // Objetivo 1: Roubar (estar dentro do veículo)
            if (_activeMission.Objectives[0].IsCompleted && !_activeMission.Objectives[1].IsCompleted)
            {
                if (player.IsInVehicle() && player.CurrentVehicle == _targetVehicle)
                {
                    _activeMission.UpdateObjective(1, 1);
                    
                    Vector3 hideout = new Vector3(1200f, 3600f, 38f); // Sandy Shores
                    CreateObjectiveBlip(hideout);
                    Notification.Show("~g~Veículo roubado! ~w~Leve ao esconderijo");
                }
            }

            // Objetivo 2: Levar ao esconderijo
            if (_activeMission.Objectives[1].IsCompleted)
            {
                Vector3 hideout = new Vector3(1200f, 3600f, 38f);
                float dist = player.Position.DistanceTo(hideout);
                
                if (dist < 20f && player.IsInVehicle() && player.CurrentVehicle == _targetVehicle)
                {
                    _activeMission.UpdateObjective(2, 1);
                }
            }
        }

        private void UpdateTerritoryTakeover()
        {
            Ped player = Game.Player.Character;
            Vector3 territoryCenter = new Vector3(_activeMission.TargetX, _activeMission.TargetY, _activeMission.TargetZ);
            float distance = player.Position.DistanceTo(territoryCenter);

            // Objetivo 0: Entrar no território
            if (!_activeMission.Objectives[0].IsCompleted && distance <= _activeMission.TargetRadius)
            {
                _activeMission.UpdateObjective(0, 1);
                Notification.Show("~r~INVASÃO INICIADA! ~w~Elimine os inimigos");
            }

            // Objetivo 1: Eliminar inimigos (conta mortos)
            if (_activeMission.Objectives[0].IsCompleted && !_activeMission.Objectives[1].IsCompleted)
            {
                int deadEnemies = _enemyNPCs.Count(e => e == null || !e.Exists() || e.IsDead);
                _activeMission.Objectives[1].CurrentProgress = deadEnemies;
                
                if (deadEnemies >= _activeMission.Objectives[1].RequiredProgress)
                {
                    _activeMission.UpdateObjective(1, 0); // Já tem progress
                    Notification.Show("~g~Inimigos eliminados! ~w~Território capturado");
                }
            }

            // Objetivo 2: Capturar (automático após matar todos)
            if (_activeMission.Objectives[1].IsCompleted)
            {
                _activeMission.UpdateObjective(2, 1);
            }
        }

        private void UpdateDrugDelivery()
        {
            Ped player = Game.Player.Character;
            Vector3 pickupPos = new Vector3(_activeMission.StartX, _activeMission.StartY, _activeMission.StartZ);
            Vector3 deliveryPos = new Vector3(_activeMission.TargetX, _activeMission.TargetY, _activeMission.TargetZ);

            // Objetivo 0: Pegar mercadoria
            if (!_activeMission.Objectives[0].IsCompleted)
            {
                float dist = player.Position.DistanceTo(pickupPos);
                
                if (dist < 10f)
                {
                    _activeMission.UpdateObjective(0, 1);
                    CreateObjectiveBlip(deliveryPos);
                    Notification.Show("~g~Mercadoria coletada! ~w~Entregue no destino");
                }
            }

            // Objetivo 1: Entregar
            if (_activeMission.Objectives[0].IsCompleted)
            {
                float dist = player.Position.DistanceTo(deliveryPos);
                
                if (dist < _activeMission.TargetRadius)
                {
                    _activeMission.UpdateObjective(1, 1);
                }
            }
        }

        private void UpdateIntimidation()
        {
            Ped player = Game.Player.Character;

            // Objetivo 0: Encontrar comerciante
            if (!_activeMission.Objectives[0].IsCompleted)
            {
                if (_targetPed == null || !_targetPed.Exists())
                {
                    Vector3 targetPos = new Vector3(_activeMission.TargetX, _activeMission.TargetY, _activeMission.TargetZ);
                    float dist = player.Position.DistanceTo(targetPos);
                    
                    if (dist < _activeMission.TargetRadius)
                    {
                        // Spawn comerciante
                        SpawnTargetPed();
                        _activeMission.UpdateObjective(0, 1);
                        Notification.Show("~y~Comerciante encontrado! ~w~Intimide-o (atire perto)");
                    }
                }
            }

            // Objetivo 1: Intimidar (atirar perto sem matar)
            if (_activeMission.Objectives[0].IsCompleted && !_activeMission.Objectives[1].IsCompleted)
            {
                if (_targetPed != null && _targetPed.Exists())
                {
                    // Verifica se player atirou perto
                    if (player.IsShooting)
                    {
                        float dist = player.Position.DistanceTo(_targetPed.Position);
                        if (dist < 15f && !_targetPed.IsDead)
                        {
                            _targetPed.Task.HandsUp(10000);
                            _activeMission.UpdateObjective(1, 1);
                            Notification.Show("~g~Comerciante intimidado!");
                        }
                    }
                    
                    if (_targetPed.IsDead)
                    {
                        FailMission("Você matou o comerciante!");
                    }
                }
            }
        }

        private void UpdateAmbush()
        {
            Ped player = Game.Player.Character;
            Vector3 ambushPos = new Vector3(_activeMission.TargetX, _activeMission.TargetY, _activeMission.TargetZ);
            float distance = player.Position.DistanceTo(ambushPos);

            // Objetivo 0: Ir ao ponto de emboscada
            if (!_activeMission.Objectives[0].IsCompleted && distance <= _activeMission.TargetRadius)
            {
                _activeMission.UpdateObjective(0, 1);
                Notification.Show("~r~EMBOSCADA! ~w~Elimine os rivais");
            }

            // Objetivo 1: Eliminar rivais
            if (_activeMission.Objectives[0].IsCompleted)
            {
                int deadEnemies = _enemyNPCs.Count(e => e == null || !e.Exists() || e.IsDead);
                _activeMission.Objectives[1].CurrentProgress = deadEnemies;
                
                if (deadEnemies >= _activeMission.Objectives[1].RequiredProgress)
                {
                    _activeMission.UpdateObjective(1, 0);
                }
            }
        }

        private void UpdateHitContract()
        {
            Ped player = Game.Player.Character;

            // Objetivo 0: Localizar alvo
            if (!_activeMission.Objectives[0].IsCompleted)
            {
                if (_targetPed == null || !_targetPed.Exists())
                {
                    Vector3 targetArea = new Vector3(_activeMission.TargetX, _activeMission.TargetY, _activeMission.TargetZ);
                    float dist = player.Position.DistanceTo(targetArea);
                    
                    if (dist < _activeMission.TargetRadius)
                    {
                        SpawnTargetPed();
                        _activeMission.UpdateObjective(0, 1);
                        Notification.Show("~r~Alvo localizado! ~w~Elimine-o discretamente");
                    }
                }
            }

            // Objetivo 1: Eliminar
            if (_activeMission.Objectives[0].IsCompleted && !_activeMission.Objectives[1].IsCompleted)
            {
                if (_targetPed != null && _targetPed.IsDead)
                {
                    _activeMission.UpdateObjective(1, 1);
                    Notification.Show("~g~Alvo eliminado! ~w~Escape da área");
                }
            }

            // Objetivo 2: Escapar
            if (_activeMission.Objectives[1].IsCompleted)
            {
                Vector3 targetArea = new Vector3(_activeMission.TargetX, _activeMission.TargetY, _activeMission.TargetZ);
                float dist = player.Position.DistanceTo(targetArea);
                
                if (dist > 200f)
                {
                    _activeMission.UpdateObjective(2, 1);
                }
            }
        }

        // ===== SPAWNING =====

        private void SpawnAllyNPCs(int count)
        {
            Ped player = Game.Player.Character;
            GangData gang = _gangSystem.GetGang(_playerMembership.CurrentGang.Value);
            if (gang == null) return;

            string pedModel = GetGangPedModel(_playerMembership.CurrentGang.Value);
            Model model = new Model(pedModel);
            model.Request(1000);

            if (!model.IsLoaded) return;

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = player.Position.Around(5f);
                Ped ally = World.CreatePed(model, spawnPos);

                if (ally != null && ally.Exists())
                {
                    ally.RelationshipGroup = player.RelationshipGroup;
                    
                    // Arma
                    if (gang.AvailableWeapons.Count > 0)
                    {
                        string weapon = gang.AvailableWeapons[0];
                        WeaponHash weaponHash = (WeaponHash)Function.Call<int>(Hash.GET_HASH_KEY, weapon);
                        ally.Weapons.Give(weaponHash, 999, true, true);
                    }

                    ally.Task.FollowToOffsetFromEntity(player, new Vector3(2f * i, 2f * i, 0f), 5f, -1, 10f, true);
                    _allyNPCs.Add(ally);
                }
            }

            model.MarkAsNoLongerNeeded();

            if (_debugEnabled)
                Notification.Show($"~g~[DEBUG] Spawned {_allyNPCs.Count} ally NPCs");
        }

        private void SpawnEnemyNPCs(int count)
        {
            if (!_activeMission.TargetGang.HasValue) return;

            string pedModel = GetGangPedModel(_activeMission.TargetGang.Value);
            Model model = new Model(pedModel);
            model.Request(1000);

            if (!model.IsLoaded) return;

            Vector3 spawnCenter = new Vector3(_activeMission.TargetX, _activeMission.TargetY, _activeMission.TargetZ);

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = spawnCenter.Around(20f);
                Ped enemy = World.CreatePed(model, spawnPos);

                if (enemy != null && enemy.Exists())
                {
                    int gangHash = Function.Call<int>(Hash.GET_HASH_KEY, $"GANG_{_activeMission.TargetGang.Value}");
                    enemy.RelationshipGroup = gangHash;

                    enemy.Weapons.Give(WeaponHash.Pistol, 999, true, true);
                    enemy.Task.FightAgainst(Game.Player.Character);
                    
                    _enemyNPCs.Add(enemy);
                }
            }

            model.MarkAsNoLongerNeeded();

            if (_debugEnabled)
                Notification.Show($"~g~[DEBUG] Spawned {_enemyNPCs.Count} enemy NPCs");
        }

        private void SpawnTargetPed()
        {
            string modelName = _activeMission.TargetPedModel ?? "a_m_m_business_01";
            Model model = new Model(modelName);
            model.Request(1000);

            if (!model.IsLoaded) return;

            Vector3 spawnPos = new Vector3(_activeMission.TargetX, _activeMission.TargetY, _activeMission.TargetZ);
            _targetPed = World.CreatePed(model, spawnPos);

            if (_targetPed != null && _targetPed.Exists())
            {
                _targetPed.Task.WanderAround();
                CreateObjectiveBlip(_targetPed.Position);
            }

            model.MarkAsNoLongerNeeded();
        }

        private void SpawnTargetVehicle()
        {
            string modelName = _activeMission.TargetVehicleModel ?? "SCHAFTER2";
            Model model = new Model(modelName);
            model.Request(1000);

            if (!model.IsLoaded) return;

            Vector3 spawnPos = new Vector3(_activeMission.TargetX, _activeMission.TargetY, _activeMission.TargetZ);
            _targetVehicle = World.CreateVehicle(model, spawnPos);

            if (_targetVehicle != null && _targetVehicle.Exists())
            {
                CreateObjectiveBlip(_targetVehicle.Position);
            }

            model.MarkAsNoLongerNeeded();
        }

        // ===== BLIPS =====

        private void CreateMissionBlip()
        {
            if (_missionBlip != null && _missionBlip.Exists())
                _missionBlip.Delete();

            Vector3 pos = new Vector3(_activeMission.TargetX, _activeMission.TargetY, _activeMission.TargetZ);
            _missionBlip = World.CreateBlip(pos);
            _missionBlip.Sprite = BlipSprite.GTAOMission;
            _missionBlip.Color = BlipColor.Yellow;
            _missionBlip.Name = _activeMission.Name;
        }

        private void CreateObjectiveBlip(Vector3 position)
        {
            if (_objectiveBlip != null && _objectiveBlip.Exists())
                _objectiveBlip.Delete();

            _objectiveBlip = World.CreateBlip(position);
            _objectiveBlip.Sprite = BlipSprite.Waypoint;
            _objectiveBlip.Color = BlipColor.Green;
            _objectiveBlip.Name = "Objetivo";
        }

        // ===== FINALIZAÇÃO =====

        private void CompleteMission()
        {
            if (_activeMission == null) return;

            _activeMission.Complete();

            // Recompensas
            Game.Player.Money += (int)_activeMission.MoneyReward;
            
            var transaction = new EconomyTransaction(
                amount: _activeMission.MoneyReward,
                type: TransactionType.Income,
                legality: TransactionLegality.Illegal,
                origin: TransactionOrigin.GangMission,
                description: $"Missão: {_activeMission.Name}"
            );
            _economySystem.Wallet.ApplyTransaction(transaction);

            _playerMembership.IncreaseRespect(_activeMission.ReputationReward);

            Notification.Show($"~g~MISSÃO COMPLETA!~n~" +
                $"~w~+${_activeMission.MoneyReward}~n~" +
                $"~y~+{_activeMission.ReputationReward} Respeito");

            // Territory capture
            if (_activeMission.Type == GangMissionType.TerritoryTakeover && !string.IsNullOrEmpty(_activeMission.TargetTerritoryId))
            {
                var territory = TerritoryDatabase.GetTerritoryById(_activeMission.TargetTerritoryId);
                if (territory != null)
                {
                    var previousGang = territory.ControllingGang;
                    var playerGang = _playerMembership.CurrentGang.Value;

                    if (previousGang.HasValue)
                    {
                        _gangSystem.TransferTerritory(_activeMission.TargetTerritoryId, previousGang.Value, playerGang);
                    }

                    territory.StartAttack(playerGang);
                    territory.UpdateAttack(1.0f);

                    Notification.Show($"~g~TERRITÓRIO CAPTURADO!~n~~w~{territory.Name}");
                }
            }

            Cleanup();
        }

        private void FailMission(string reason)
        {
            if (_activeMission == null) return;

            _activeMission.Fail();

            Notification.Show($"~r~MISSÃO FALHOU!~n~~w~{reason}");

            if (_debugEnabled)
                Notification.Show($"~r~[DEBUG] Mission failed: {reason}");

            Cleanup();
        }

        private void Cleanup()
        {
            // Remove NPCs
            foreach (Ped ally in _allyNPCs)
            {
                if (ally != null && ally.Exists())
                    ally.Delete();
            }
            _allyNPCs.Clear();

            foreach (Ped enemy in _enemyNPCs)
            {
                if (enemy != null && enemy.Exists())
                    enemy.Delete();
            }
            _enemyNPCs.Clear();

            // Remove blips
            if (_missionBlip != null && _missionBlip.Exists())
                _missionBlip.Delete();
            
            if (_objectiveBlip != null && _objectiveBlip.Exists())
                _objectiveBlip.Delete();

            // Limpa referências
            _targetPed = null;
            _targetVehicle = null;
            _activeMission = null;
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

        public void SetDebugMode(bool enabled)
        {
            _debugEnabled = enabled;
        }

        public void Shutdown()
        {
            Cleanup();
        }
    }
}
