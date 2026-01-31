using System;
using System.Collections.Generic;
using System.Linq;

namespace RLF.Core.Gangs
{
    /// <summary>
    /// Sistema central de gerenciamento de gangues
    /// </summary>
    public class GangSystem
    {
        private Dictionary<GangType, GangData> _gangs;
        private bool _isInitialized = false;

        public GangSystem()
        {
            _gangs = new Dictionary<GangType, GangData>();
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            InitializeGangs();
            SetupInitialRelations();
            AssignInitialTerritories();

            _isInitialized = true;
        }

        private void InitializeGangs()
        {
            // Families (Grove Street)
            _gangs[GangType.Families] = new GangData
            {
                Type = GangType.Families,
                Name = "The Families",
                Description = "Traditional street gang from Grove Street, Los Santos",
                TotalMembers = 50,
                ActiveMembers = 15,
                Treasury = 5000m,
                Reputation = 65,
                IsAggressive = true,
                ActivityLevel = 0.7f,
                CanRecruitPlayer = true
            };
            _gangs[GangType.Families].AddWeapon("WEAPON_PISTOL");
            _gangs[GangType.Families].AddWeapon("WEAPON_MICROSMG");
            _gangs[GangType.Families].AddVehicle("BISON");
            _gangs[GangType.Families].AddVehicle("GLENDALE");

            // Ballas
            _gangs[GangType.Ballas] = new GangData
            {
                Type = GangType.Ballas,
                Name = "Ballas",
                Description = "Rival street gang, known for purple colors",
                TotalMembers = 55,
                ActiveMembers = 18,
                Treasury = 5500m,
                Reputation = 70,
                IsAggressive = true,
                ActivityLevel = 0.8f,
                CanRecruitPlayer = true
            };
            _gangs[GangType.Ballas].AddWeapon("WEAPON_PISTOL");
            _gangs[GangType.Ballas].AddWeapon("WEAPON_SMG");
            _gangs[GangType.Ballas].AddVehicle("TORNADO");
            _gangs[GangType.Ballas].AddVehicle("PRIMO");

            // Vagos
            _gangs[GangType.Vagos] = new GangData
            {
                Type = GangType.Vagos,
                Name = "Los Santos Vagos",
                Description = "Mexican street gang operating in Los Santos",
                TotalMembers = 60,
                ActiveMembers = 20,
                Treasury = 6000m,
                Reputation = 68,
                IsAggressive = true,
                ActivityLevel = 0.75f,
                CanRecruitPlayer = true
            };
            _gangs[GangType.Vagos].AddWeapon("WEAPON_PISTOL");
            _gangs[GangType.Vagos].AddWeapon("WEAPON_MICROSMG");
            _gangs[GangType.Vagos].AddWeapon("WEAPON_ASSAULTRIFLE");
            _gangs[GangType.Vagos].AddVehicle("PEYOTE");
            _gangs[GangType.Vagos].AddVehicle("MANANA");

            // Marabunta Grande
            _gangs[GangType.Marabunta] = new GangData
            {
                Type = GangType.Marabunta,
                Name = "Marabunta Grande",
                Description = "Salvadoran gang known for brutality",
                TotalMembers = 45,
                ActiveMembers = 15,
                Treasury = 4500m,
                Reputation = 72,
                IsAggressive = true,
                ActivityLevel = 0.85f,
                CanRecruitPlayer = true
            };
            _gangs[GangType.Marabunta].AddWeapon("WEAPON_PISTOL");
            _gangs[GangType.Marabunta].AddWeapon("WEAPON_ASSAULTRIFLE");
            _gangs[GangType.Marabunta].AddVehicle("EMPEROR");
            _gangs[GangType.Marabunta].AddVehicle("BUCCANEER");

            // Armenian Mob
            _gangs[GangType.ArmenianMob] = new GangData
            {
                Type = GangType.ArmenianMob,
                Name = "Armenian Mob",
                Description = "Organized crime syndicate with business interests",
                TotalMembers = 35,
                ActiveMembers = 12,
                Treasury = 15000m,
                Reputation = 80,
                IsAggressive = false,
                ActivityLevel = 0.6f,
                CanRecruitPlayer = true
            };
            _gangs[GangType.ArmenianMob].AddWeapon("WEAPON_PISTOL");
            _gangs[GangType.ArmenianMob].AddWeapon("WEAPON_SMG");
            _gangs[GangType.ArmenianMob].AddWeapon("WEAPON_CARBINERIFLE");
            _gangs[GangType.ArmenianMob].AddVehicle("WASHINGTON");
            _gangs[GangType.ArmenianMob].AddVehicle("COGNOSCENTI");

            // Triad Tong
            _gangs[GangType.TriadTong] = new GangData
            {
                Type = GangType.TriadTong,
                Name = "Triad Tong",
                Description = "Chinese organized crime with international connections",
                TotalMembers = 40,
                ActiveMembers = 14,
                Treasury = 18000m,
                Reputation = 85,
                IsAggressive = false,
                ActivityLevel = 0.65f,
                CanRecruitPlayer = true
            };
            _gangs[GangType.TriadTong].AddWeapon("WEAPON_PISTOL");
            _gangs[GangType.TriadTong].AddWeapon("WEAPON_APPISTOL");
            _gangs[GangType.TriadTong].AddWeapon("WEAPON_CARBINERIFLE");
            _gangs[GangType.TriadTong].AddVehicle("SULTAN");
            _gangs[GangType.TriadTong].AddVehicle("SCHAFTER2");

            // Korean Mob
            _gangs[GangType.KoreanMob] = new GangData
            {
                Type = GangType.KoreanMob,
                Name = "Korean Mob",
                Description = "Korean organized crime operating in Little Seoul",
                TotalMembers = 30,
                ActiveMembers = 10,
                Treasury = 12000m,
                Reputation = 75,
                IsAggressive = false,
                ActivityLevel = 0.55f,
                CanRecruitPlayer = true
            };
            _gangs[GangType.KoreanMob].AddWeapon("WEAPON_PISTOL");
            _gangs[GangType.KoreanMob].AddWeapon("WEAPON_SMG");
            _gangs[GangType.KoreanMob].AddVehicle("FUTO");
            _gangs[GangType.KoreanMob].AddVehicle("KURUMA");

            // The Lost MC
            _gangs[GangType.LostMC] = new GangData
            {
                Type = GangType.LostMC,
                Name = "The Lost MC",
                Description = "Outlaw motorcycle club",
                TotalMembers = 40,
                ActiveMembers = 12,
                Treasury = 8000m,
                Reputation = 70,
                IsAggressive = true,
                ActivityLevel = 0.7f,
                CanRecruitPlayer = true
            };
            _gangs[GangType.LostMC].AddWeapon("WEAPON_PISTOL");
            _gangs[GangType.LostMC].AddWeapon("WEAPON_SAWNOFFSHOTGUN");
            _gangs[GangType.LostMC].AddWeapon("WEAPON_ASSAULTRIFLE");
            _gangs[GangType.LostMC].AddVehicle("DAEMON");
            _gangs[GangType.LostMC].AddVehicle("HEXER");
            _gangs[GangType.LostMC].AddVehicle("ZOMBIEA");

            // Rednecks
            _gangs[GangType.Rednecks] = new GangData
            {
                Type = GangType.Rednecks,
                Name = "Rednecks",
                Description = "Rural criminals from Blaine County",
                TotalMembers = 25,
                ActiveMembers = 8,
                Treasury = 3000m,
                Reputation = 50,
                IsAggressive = false,
                ActivityLevel = 0.4f,
                CanRecruitPlayer = false
            };
            _gangs[GangType.Rednecks].AddWeapon("WEAPON_PISTOL");
            _gangs[GangType.Rednecks].AddWeapon("WEAPON_PUMPSHOTGUN");
            _gangs[GangType.Rednecks].AddVehicle("REBEL");
            _gangs[GangType.Rednecks].AddVehicle("BODHI");
        }

        private void SetupInitialRelations()
        {
            // RIVALIDADES DE GANGUES DE RUA
            SetMutualRelation(GangType.Families, GangType.Ballas, -100);      // Arqui-inimigos
            SetMutualRelation(GangType.Families, GangType.Vagos, -70);
            SetMutualRelation(GangType.Ballas, GangType.Vagos, -60);
            SetMutualRelation(GangType.Ballas, GangType.Marabunta, -50);
            SetMutualRelation(GangType.Vagos, GangType.Marabunta, -40);

            // CRIME ORGANIZADO - Neutro/Competitivo entre si
            SetMutualRelation(GangType.ArmenianMob, GangType.TriadTong, -30);
            SetMutualRelation(GangType.ArmenianMob, GangType.KoreanMob, -20);
            SetMutualRelation(GangType.TriadTong, GangType.KoreanMob, -25);

            // CRIME ORGANIZADO vs GANGUES DE RUA - Desprezam gangues de rua
            SetRelationWithGroup(GangType.ArmenianMob, new[] { GangType.Families, GangType.Ballas, GangType.Vagos, GangType.Marabunta }, -40);
            SetRelationWithGroup(GangType.TriadTong, new[] { GangType.Families, GangType.Ballas, GangType.Vagos, GangType.Marabunta }, -35);
            SetRelationWithGroup(GangType.KoreanMob, new[] { GangType.Families, GangType.Ballas, GangType.Vagos, GangType.Marabunta }, -30);

            // LOST MC - Rivais com todos
            SetRelationWithGroup(GangType.LostMC,
                new[] { GangType.Families, GangType.Ballas, GangType.Vagos, GangType.Marabunta,
                        GangType.ArmenianMob, GangType.TriadTong, GangType.KoreanMob }, -50);
        }

        private void SetMutualRelation(GangType gang1, GangType gang2, int value)
        {
            _gangs[gang1].SetRelation(gang2, value);
            _gangs[gang2].SetRelation(gang1, value);
        }

        private void SetRelationWithGroup(GangType gang, GangType[] targets, int value)
        {
            foreach (var target in targets)
            {
                _gangs[gang].SetRelation(target, value);
                _gangs[target].SetRelation(gang, value);
            }
        }

        private void AssignInitialTerritories()
        {
            var territories = TerritoryDatabase.GetAllTerritories();

            foreach (var territory in territories)
            {
                if (territory.ControllingGang.HasValue)
                {
                    GangType gang = territory.ControllingGang.Value;
                    if (_gangs.ContainsKey(gang))
                    {
                        _gangs[gang].AddTerritory(territory.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Obtém dados de uma gangue
        /// </summary>
        public GangData GetGang(GangType type)
        {
            return _gangs.ContainsKey(type) ? _gangs[type] : null;
        }

        /// <summary>
        /// Obtém todas as gangues
        /// </summary>
        public List<GangData> GetAllGangs()
        {
            return _gangs.Values.ToList();
        }

        /// <summary>
        /// Processa renda diária de territórios
        /// </summary>
        public void ProcessDailyIncome()
        {
            foreach (var gang in _gangs.Values)
            {
                decimal income = gang.CalculateDailyIncome();
                gang.AddMoney(income);
                gang.RecordActivity();
            }
        }

        /// <summary>
        /// Transfere território de uma gangue para outra
        /// </summary>
        public void TransferTerritory(string territoryId, GangType fromGang, GangType toGang)
        {
            if (_gangs.ContainsKey(fromGang))
            {
                _gangs[fromGang].RemoveTerritory(territoryId);
            }

            if (_gangs.ContainsKey(toGang))
            {
                _gangs[toGang].AddTerritory(territoryId);
                _gangs[toGang].RecordTerritoryCapture();
            }

            // Piora relação entre gangues
            if (_gangs.ContainsKey(fromGang) && _gangs.ContainsKey(toGang))
            {
                _gangs[fromGang].WorsenRelation(toGang, 10);
                _gangs[toGang].WorsenRelation(fromGang, 10);
            }
        }

        /// <summary>
        /// Obtém gangue mais poderosa
        /// </summary>
        public GangType GetMostPowerfulGang()
        {
            var mostPowerful = _gangs.Values
                .OrderByDescending(g => g.PowerLevel)
                .FirstOrDefault();

            return mostPowerful?.Type ?? GangType.Independent;
        }

        /// <summary>
        /// Obtém gangues em guerra
        /// </summary>
        public List<Tuple<GangType, GangType>> GetActiveWars()
        {
            var wars = new List<Tuple<GangType, GangType>>();

            foreach (var gang in _gangs.Values)
            {
                foreach (var relation in gang.Relations)
                {
                    if (relation.Value <= -80) // Guerra ativa
                    {
                        // Evita duplicatas
                        var existing = wars.FirstOrDefault(w =>
                            (w.Item1 == gang.Type && w.Item2 == relation.Key) ||
                            (w.Item2 == gang.Type && w.Item1 == relation.Key));

                        if (existing == null)
                        {
                            wars.Add(new Tuple<GangType, GangType>(gang.Type, relation.Key));
                        }
                    }
                }
            }

            return wars;
        }

        public void Shutdown()
        {
            _gangs.Clear();
            _isInitialized = false;
        }
    }
}
