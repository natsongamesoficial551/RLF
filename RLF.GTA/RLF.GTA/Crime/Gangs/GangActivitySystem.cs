using GTA;
using GTA.Math;
using GTA.Native;
using RLF.Core.Crime;
using RLF.Core.Gangs;
using RLF.GTA.Jobs.Uber.Penalty;
using System;
using System.Collections.Generic;
using System.Linq;
using CoreCrimeType = RLF.Core.Crime.CrimeType;

namespace RLF.GTA.Gangs
{
    /// <summary>
    /// Gerencia atividades autônomas de gangues NPC pela cidade
    /// Gangues cometem crimes, patrulham territórios, e interagem entre si
    /// ESTE ARQUIVO VAI EM RLF.GTA (não em RLF.Core)
    /// </summary>
    public class GangActivitySystem
    {
        private readonly GangSystem _gangSystem;
        private readonly CrimeSystem _crimeSystem;

        private Dictionary<int, GangMemberNPC> _activeGangMembers;
        private Random _random;

        private const float UPDATE_INTERVAL = 5.0f;
        private float _updateTimer;

        private const float SPAWN_CHECK_INTERVAL = 10.0f;
        private float _spawnTimer;

        private const int MAX_GANG_NPCS_WORLD = 50;

        public bool IsEnabled { get; set; }

        public GangActivitySystem(GangSystem gangSystem, CrimeSystem crimeSystem)
        {
            _gangSystem = gangSystem ?? throw new ArgumentNullException(nameof(gangSystem));
            _crimeSystem = crimeSystem ?? throw new ArgumentNullException(nameof(crimeSystem));

            _activeGangMembers = new Dictionary<int, GangMemberNPC>();
            _random = new Random();
            _updateTimer = 0f;
            _spawnTimer = 0f;
            IsEnabled = true;
        }

        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;

            _updateTimer += deltaTime;
            _spawnTimer += deltaTime;

            if (_spawnTimer >= SPAWN_CHECK_INTERVAL)
            {
                SpawnGangMembersInTerritories();
                _spawnTimer = 0f;
            }

            if (_updateTimer >= UPDATE_INTERVAL)
            {
                UpdateGangActivities();
                CleanupInvalidNPCs();
                _updateTimer = 0f;
            }
        }

        /// <summary>
        /// Spawna membros de gangue em seus territórios
        /// </summary>
        private void SpawnGangMembersInTerritories()
        {
            if (_activeGangMembers.Count >= MAX_GANG_NPCS_WORLD) return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            var territories = TerritoryDatabase.GetAllTerritories();

            foreach (var territory in territories)
            {
                if (!territory.ControllingGang.HasValue) continue;

                // Spawna apenas em territórios próximos ao jogador
                Vector3 territoryCenter = new Vector3(territory.CenterX, territory.CenterY, territory.CenterZ);
                float distanceToPlayer = Vector3.Distance(territoryCenter, player.Position);
                if (distanceToPlayer > 500f) continue;
                if (distanceToPlayer < 50f) continue; // Muito perto do jogador

                // Verifica quantos membros já existem neste território
                int currentMembers = CountMembersInTerritory(territory.Id);
                if (currentMembers >= territory.MaxGangMembers) continue;

                // Chance de spawn baseada na força de controle e nível de atividade
                GangData gang = _gangSystem.GetGang(territory.ControllingGang.Value);
                if (gang == null) continue;

                float spawnChance = territory.ControlStrength * gang.ActivityLevel * 0.3f;
                if (_random.NextDouble() > spawnChance) continue;

                // Spawna membro
                SpawnGangMember(territory, gang);
            }
        }

        /// <summary>
        /// Spawna um membro de gangue específico
        /// </summary>
        private void SpawnGangMember(TerritoryData territory, GangData gang)
        {
            // Posição aleatória no território
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            float distance = (float)(_random.NextDouble() * territory.Radius * 0.7f);

            Vector3 spawnPos = new Vector3(
                territory.CenterX + (float)Math.Cos(angle) * distance,
                territory.CenterY + (float)Math.Sin(angle) * distance,
                territory.CenterZ
            );

            // Modelo do NPC baseado na gangue
            string pedModel = GetGangPedModel(gang.Type);
            Model model = new Model(pedModel);
            model.Request(1000);

            if (!model.IsLoaded) return;

            Ped ped = World.CreatePed(model, spawnPos);
            if (ped == null || !ped.Exists()) return;

            // Configura o NPC
            SetupGangMember(ped, gang);

            // Adiciona à lista
            var gangMember = new GangMemberNPC
            {
                Ped = ped,
                Gang = gang.Type,
                TerritoryId = territory.Id,
                SpawnedAt = DateTime.Now,
                CurrentActivity = GangActivity.Patrolling
            };

            _activeGangMembers[ped.Handle] = gangMember;

            model.MarkAsNoLongerNeeded();
        }

        /// <summary>
        /// Configura um NPC como membro de gangue
        /// </summary>
        private void SetupGangMember(Ped ped, GangData gang)
        {
            // Relationship group baseado na gangue
            string groupName = $"GANG_{gang.Type}";
            int groupHash = Function.Call<int>(Hash.GET_HASH_KEY, groupName);
            ped.RelationshipGroup = groupHash;

            // Arma aleatória disponível
            if (gang.AvailableWeapons.Count > 0)
            {
                string weapon = gang.AvailableWeapons[_random.Next(gang.AvailableWeapons.Count)];
                WeaponHash weaponHash = (WeaponHash)Function.Call<int>(Hash.GET_HASH_KEY, weapon);
                ped.Weapons.Give(weaponHash, 999, false, true);
            }

            // Comportamento
            ped.CanSwitchWeapons = true;
            ped.CanRagdoll = true;
            ped.BlockPermanentEvents = false;

            // Patrulha inicial
            ped.Task.WanderAround();
        }

        /// <summary>
        /// Atualiza atividades de gangues
        /// </summary>
        private void UpdateGangActivities()
        {
            foreach (var gangMember in _activeGangMembers.Values.ToList())
            {
                if (gangMember == null || !gangMember.IsValid()) continue;

                // Decide atividade baseado em probabilidade
                DecideActivity(gangMember);

                // Executa atividade atual
                ExecuteActivity(gangMember);

                // Detecta confrontos com gangues rivais
                CheckForGangConfrontations(gangMember);
            }
        }

        /// <summary>
        /// Decide próxima atividade do membro
        /// </summary>
        private void DecideActivity(GangMemberNPC member)
        {
            GangData gang = _gangSystem.GetGang(member.Gang);
            if (gang == null) return;

            // Se já está em atividade recente, mantém
            if ((DateTime.Now - member.ActivityStartedAt).TotalSeconds < 60)
                return;

            double roll = _random.NextDouble();

            if (gang.IsAggressive && roll < 0.15) // 15% chance de crime
            {
                member.CurrentActivity = GangActivity.CommittingCrime;
            }
            else if (roll < 0.30) // 15% chance de tráfico
            {
                member.CurrentActivity = GangActivity.DrugDealing;
            }
            else // 70% chance de patrulha
            {
                member.CurrentActivity = GangActivity.Patrolling;
            }

            member.ActivityStartedAt = DateTime.Now;
        }

        /// <summary>
        /// Executa atividade atual
        /// </summary>
        private void ExecuteActivity(GangMemberNPC member)
        {
            if (!member.IsValid()) return;

            switch (member.CurrentActivity)
            {
                case GangActivity.Patrolling:
                    ExecutePatrol(member);
                    break;

                case GangActivity.CommittingCrime:
                    ExecuteCrime(member);
                    break;

                case GangActivity.DrugDealing:
                    ExecuteDrugDeal(member);
                    break;
            }
        }

        /// <summary>
        /// Executa patrulha
        /// </summary>
        private void ExecutePatrol(GangMemberNPC member)
        {
            Ped ped = member.Ped;
            if (ped == null || !ped.Exists()) return;

            // Se não está fazendo nada, anda aleatoriamente
            if (!ped.IsInCombat && !ped.IsWalking && !ped.IsRunning)
            {
                ped.Task.WanderAround();
            }
        }

        /// <summary>
        /// Executa crime (assalto, roubo, etc)
        /// </summary>
        private void ExecuteCrime(GangMemberNPC member)
        {
            if (member.HasCommittedCrimeRecently()) return;

            Ped ped = member.Ped;
            if (ped == null || !ped.Exists()) return;

            // Procura vítima civil próxima
            Ped[] nearbyPeds = World.GetNearbyPeds(ped, 30f);
            if (nearbyPeds == null || nearbyPeds.Length == 0) return;

            foreach (Ped target in nearbyPeds)
            {
                if (target == null || !target.Exists()) continue;
                if (target.IsPlayer) continue;
                if (!target.IsAlive) continue;
                if (target.IsInVehicle()) continue;
                if (IsCop(target)) continue;
                if (IsGangMember(target)) continue;

                // Ameaça a vítima
                ped.Task.AimAt(target, 5000);

                // Registra crime
                Vector3 pos = ped.Position;
                var crime = _crimeSystem.RegisterCrime(
                    CoreCrimeType.PedestrianRobbery,  // <-- Use o alias já definido
                    pos.X, pos.Y, pos.Z,
                    "Street",
                    GetZoneName(pos)
                );

                if (crime != null)
                {
                    crime.AddFlag(CrimeFlags.WeaponUsed);
                    crime.AddFlag(CrimeFlags.Witnessed);
                }

                member.LastCrimeAt = DateTime.Now;

                GangData gang = _gangSystem.GetGang(member.Gang);
                gang?.RecordCrime();

                break;
            }
        }

        /// <summary>
        /// Executa tráfico de drogas
        /// </summary>
        private void ExecuteDrugDeal(GangMemberNPC member)
        {
            if (member.HasCommittedCrimeRecently()) return;

            Ped ped = member.Ped;
            if (ped == null || !ped.Exists()) return;

            // Procura "cliente" próximo
            Ped[] nearbyPeds = World.GetNearbyPeds(ped, 20f);
            if (nearbyPeds == null || nearbyPeds.Length == 0) return;

            foreach (Ped target in nearbyPeds)
            {
                if (target == null || !target.Exists()) continue;
                if (target.IsPlayer) continue;
                if (!target.IsAlive) continue;
                if (IsCop(target)) continue;

                // "Negocia"
                ped.Task.TurnTo(target);

                member.LastCrimeAt = DateTime.Now;
                break;
            }
        }

        /// <summary>
        /// Verifica confrontos com gangues rivais
        /// </summary>
        private void CheckForGangConfrontations(GangMemberNPC member)
        {
            Ped ped = member.Ped;
            if (ped == null || !ped.Exists()) return;
            if (ped.IsInCombat) return;

            GangData gang = _gangSystem.GetGang(member.Gang);
            if (gang == null) return;

            // Procura membros de gangues rivais próximos
            Ped[] nearbyPeds = World.GetNearbyPeds(ped, 40f);
            if (nearbyPeds == null) return;

            foreach (Ped target in nearbyPeds)
            {
                if (target == null || !target.Exists()) continue;
                if (target == ped) continue;
                if (!target.IsAlive) continue;

                // Verifica se é de gangue rival
                if (_activeGangMembers.ContainsKey(target.Handle))
                {
                    GangMemberNPC rivalMember = _activeGangMembers[target.Handle];

                    // Verifica inimizade
                    if (gang.IsEnemy(rivalMember.Gang))
                    {
                        // Inicia confronto
                        ped.Task.FightAgainst(target);
                        target.Task.FightAgainst(ped);

                        gang.RecordActivity();
                        gang.WorsenRelation(rivalMember.Gang, 1);
                        break;
                    }
                }
            }
        }

        private int CountMembersInTerritory(string territoryId)
        {
            return _activeGangMembers.Values.Count(m => m.TerritoryId == territoryId && m.IsValid());
        }

        private string GetGangPedModel(GangType gang)
        {
            // Retorna modelo de NPC apropriado para cada gangue
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
                case GangType.Rednecks: return "a_m_m_hillbilly_02";
                default: return "g_m_y_famca_01";
            }
        }

        private bool IsCop(Ped ped)
        {
            if (ped == null || !ped.Exists()) return false;
            return ped.RelationshipGroup == Function.Call<int>(Hash.GET_HASH_KEY, "COP");
        }

        private bool IsGangMember(Ped ped)
        {
            if (ped == null || !ped.Exists()) return false;
            return _activeGangMembers.ContainsKey(ped.Handle);
        }

        private string GetZoneName(Vector3 position)
        {
            return Function.Call<string>(Hash.GET_NAME_OF_ZONE, position.X, position.Y, position.Z);
        }

        private void CleanupInvalidNPCs()
        {
            var toRemove = new List<int>();

            foreach (var kvp in _activeGangMembers)
            {
                if (kvp.Value == null || !kvp.Value.IsValid())
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (int handle in toRemove)
            {
                _activeGangMembers.Remove(handle);
            }
        }

        public int GetActiveGangMemberCount()
        {
            return _activeGangMembers.Count(kvp => kvp.Value != null && kvp.Value.IsValid());
        }

        public void Shutdown()
        {
            // Remove todos NPCs
            foreach (var member in _activeGangMembers.Values)
            {
                if (member.IsValid())
                {
                    member.Ped.Delete();
                }
            }

            _activeGangMembers.Clear();
        }
    }

    /// <summary>
    /// Tipo de atividade de gangue
    /// </summary>
    public enum GangActivity
    {
        Patrolling,
        CommittingCrime,
        DrugDealing,
        Fighting,
        Fleeing
    }

    /// <summary>
    /// Representa um NPC membro de gangue
    /// </summary>
    public class GangMemberNPC
    {
        public Ped Ped { get; set; }
        public GangType Gang { get; set; }
        public string TerritoryId { get; set; }
        public DateTime SpawnedAt { get; set; }
        public GangActivity CurrentActivity { get; set; }
        public DateTime ActivityStartedAt { get; set; }
        public DateTime? LastCrimeAt { get; set; }

        public bool IsValid()
        {
            return Ped != null && Ped.Exists() && Ped.IsAlive;
        }

        public bool HasCommittedCrimeRecently()
        {
            if (!LastCrimeAt.HasValue) return false;
            return (DateTime.Now - LastCrimeAt.Value).TotalSeconds < 120; // 2 minutos
        }
    }
}
