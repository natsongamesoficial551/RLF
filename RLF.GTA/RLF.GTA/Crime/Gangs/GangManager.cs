using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;
using RLF.Core.Crime;
using RLF.Core.Economy;
using RLF.Core.Gangs;
using RLF.Core.Gangs.Missions;

namespace RLF.GTA.Gangs
{
    /// <summary>
    /// Gang Manager COMPLETO - Recrutamento + Missões + Emboscadas
    /// NÃO herda de Script - é gerenciado pelo CrimeManager
    /// </summary>
    public class GangManager
    {
        private GangSystem _gangSystem;
        private PlayerGangMembership _playerMembership;
        private GangActivitySystem _activitySystem;
        private CrimeSystem _crimeSystem;
        private EconomySystem _economySystem;

        // ✅ SISTEMAS DE MISSÃO E EMBOSCADA
        private GangMissionGenerator _missionGenerator;
        private GangMissionController _missionController;
        private GangAmbushSystem _ambushSystem;

        // LemonUI
        private global::LemonUI.ObjectPool _menuPool;
        private global::LemonUI.Menus.NativeMenu _currentMenu;

        private Dictionary<GangType, RecruitmentLocation> _recruitmentLocations;
        private Dictionary<string, Blip> _territoryBlips;
        private Dictionary<GangType, Blip> _recruitmentBlips;

        private bool _isInitialized = false;
        private bool _debugEnabled = true;
        private DateTime _lastDebugTime;

        // Controle de interação
        private bool _wasNearRecruitmentLastFrame = false;
        private GangType? _currentNearbyGang = null;
        private float _interactionCooldown = 0f;

        private const float INTERACTION_DISTANCE = 10.0f;
        private const float INTERACTION_COOLDOWN = 0.5f;

        public GangManager(CrimeSystem crimeSystem, EconomySystem economySystem)
        {
            _crimeSystem = crimeSystem ?? throw new ArgumentNullException(nameof(crimeSystem));
            _economySystem = economySystem ?? throw new ArgumentNullException(nameof(economySystem));

            _recruitmentLocations = new Dictionary<GangType, RecruitmentLocation>();
            _territoryBlips = new Dictionary<string, Blip>();
            _recruitmentBlips = new Dictionary<GangType, Blip>();

            Initialize();
        }

        private void Initialize()
        {
            try
            {
                Notification.Show("~b~[Gang System] ~w~Loading...");

                _menuPool = new global::LemonUI.ObjectPool();

                _gangSystem = new GangSystem();
                _gangSystem.Initialize();

                _playerMembership = new PlayerGangMembership();

                _activitySystem = new GangActivitySystem(_gangSystem, _crimeSystem);

                // ✅ INICIALIZA SISTEMAS DE MISSÃO
                _missionGenerator = new GangMissionGenerator(_gangSystem, _playerMembership);
                _missionController = new GangMissionController(_gangSystem, _playerMembership, _crimeSystem, _economySystem, _missionGenerator);

                // ✅ INICIALIZA SISTEMA DE EMBOSCADAS
                _ambushSystem = new GangAmbushSystem(_gangSystem, _playerMembership);

                SetupRecruitmentLocations();
                CreateTerritoryBlips();
                CreateRecruitmentBlips();

                _isInitialized = true;
                _lastDebugTime = DateTime.Now;

                Notification.Show("~g~[Gang System] READY!");
                Notification.Show("~y~E = Interact | F7 = Gang Debug | F8 = Force Mission | F6 = Force Ambush");
            }
            catch (Exception ex)
            {
                Notification.Show($"~r~[Gang System] ERROR: {ex.Message}");
            }
        }

        public void Update(float deltaTime)
        {
            if (!_isInitialized) return;

            try
            {
                if (_interactionCooldown > 0f)
                    _interactionCooldown -= deltaTime;

                _menuPool.Process();

                // Crime systems
                _activitySystem.Update(deltaTime);

                // ✅ ATUALIZA SISTEMAS DE MISSÃO E EMBOSCADA
                _missionGenerator.Update(deltaTime);
                _missionController.Update(deltaTime);
                _ambushSystem.Update(deltaTime);

                CheckRecruitmentInteractions();

                if (_debugEnabled && (DateTime.Now - _lastDebugTime).TotalSeconds >= 1.0)
                {
                    ShowDebugInfo();
                    _lastDebugTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Notification.Show($"~r~[Gang System] Update Error: {ex.Message}");
            }
        }

        public void OnKeyDown(System.Windows.Forms.Keys key)
        {
            try
            {
                // F7 = Gang Debug Toggle
                if (key == System.Windows.Forms.Keys.F7)
                {
                    _debugEnabled = !_debugEnabled;
                    _missionGenerator.SetDebugMode(_debugEnabled);
                    _missionController.SetDebugMode(_debugEnabled);
                    _ambushSystem.SetDebugMode(_debugEnabled);
                    Notification.Show(_debugEnabled ? "~g~Gang Debug ON" : "~r~Gang Debug OFF");
                }

                // F8 = Forçar geração de missão (debug)
                if (key == System.Windows.Forms.Keys.F8 && _debugEnabled)
                {
                    if (_playerMembership.IsInGang())
                    {
                        OpenMissionsMenu();
                    }
                    else
                    {
                        Notification.Show("~r~Você precisa estar em uma gangue!");
                    }
                }

                // F6 = Forçar emboscada (debug)
                if (key == System.Windows.Forms.Keys.F6 && _debugEnabled)
                {
                    _ambushSystem.ForceAmbush();
                }
            }
            catch (Exception ex)
            {
                Notification.Show($"~r~[Gang System] Key Error: {ex.Message}");
            }
        }

        // [RestanteDo código anterior permanece igual até SetupRecruitmentLocations...]

        private void SetupRecruitmentLocations()
        {
            _recruitmentLocations[GangType.Families] = new RecruitmentLocation
            {
                Position = new Vector3(127f, -1930f, 21.38f),
                Name = "The Families - Grove Street",
                Gang = GangType.Families
            };

            _recruitmentLocations[GangType.Ballas] = new RecruitmentLocation
            {
                Position = new Vector3(500f, -1750f, 29.28f),
                Name = "Ballas - Rancho",
                Gang = GangType.Ballas
            };

            _recruitmentLocations[GangType.Vagos] = new RecruitmentLocation
            {
                Position = new Vector3(900f, -2100f, 30.26f),
                Name = "Los Santos Vagos - Cypress Flats",
                Gang = GangType.Vagos
            };

            _recruitmentLocations[GangType.Marabunta] = new RecruitmentLocation
            {
                Position = new Vector3(-1050f, -1550f, 4.62f),
                Name = "Marabunta Grande - La Puerta",
                Gang = GangType.Marabunta
            };

            _recruitmentLocations[GangType.ArmenianMob] = new RecruitmentLocation
            {
                Position = new Vector3(-650f, -1220f, 11.18f),
                Name = "Armenian Mob - Little Seoul",
                Gang = GangType.ArmenianMob
            };

            _recruitmentLocations[GangType.TriadTong] = new RecruitmentLocation
            {
                Position = new Vector3(500f, -650f, 24.59f),
                Name = "Triad Tong - Textile City",
                Gang = GangType.TriadTong
            };

            _recruitmentLocations[GangType.KoreanMob] = new RecruitmentLocation
            {
                Position = new Vector3(-750f, -1050f, 12.87f),
                Name = "Korean Mob - Little Seoul",
                Gang = GangType.KoreanMob
            };

            _recruitmentLocations[GangType.LostMC] = new RecruitmentLocation
            {
                Position = new Vector3(980f, -120f, 74.35f),
                Name = "The Lost MC - East Vinewood",
                Gang = GangType.LostMC
            };
        }

        private void CreateTerritoryBlips()
        {
            var territories = TerritoryDatabase.GetAllTerritories();

            foreach (var territory in territories)
            {
                if (territory.ControllingGang.HasValue)
                {
                    Blip blip = World.CreateBlip(new Vector3(territory.CenterX, territory.CenterY, territory.CenterZ), territory.Radius);
                    blip.Sprite = BlipSprite.BigCircle;
                    blip.Color = GetBlipColorForGang(territory.ControllingGang.Value);
                    blip.Alpha = 100;
                    blip.Name = $"{territory.Name} ({territory.ControllingGang.Value.GetDisplayName()})";

                    _territoryBlips[territory.Id] = blip;
                }
            }
        }

        private void CreateRecruitmentBlips()
        {
            foreach (var location in _recruitmentLocations.Values)
            {
                Blip blip = World.CreateBlip(location.Position);
                blip.Sprite = BlipSprite.GangLeader;
                blip.Color = GetBlipColorForGang(location.Gang);
                blip.Scale = 1.2f;
                blip.Name = location.Name;
                blip.IsShortRange = true;

                _recruitmentBlips[location.Gang] = blip;
            }
        }

        private BlipColor GetBlipColorForGang(GangType gang)
        {
            switch (gang)
            {
                case GangType.Families: return BlipColor.Green;
                case GangType.Ballas: return BlipColor.Purple;
                case GangType.Vagos: return BlipColor.Yellow;
                case GangType.Marabunta: return BlipColor.Blue;
                case GangType.ArmenianMob: return BlipColor.White;
                case GangType.TriadTong: return BlipColor.Red;
                case GangType.KoreanMob: return BlipColor.RedDark2;
                case GangType.LostMC: return BlipColor.Orange;
                default: return BlipColor.White;
            }
        }

        private void CheckRecruitmentInteractions()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            Vector3 playerPos = player.Position;
            bool isNearAnyRecruitment = false;
            GangType? nearbyGang = null;

            foreach (var location in _recruitmentLocations.Values)
            {
                float distance = Vector3.Distance(playerPos, location.Position);

                if (distance <= INTERACTION_DISTANCE)
                {
                    isNearAnyRecruitment = true;
                    nearbyGang = location.Gang;

                    string helpText = $"Pressione ~INPUT_CONTEXT~ para interagir com {location.Gang.GetDisplayName()}";
                    global::GTA.UI.Screen.ShowHelpTextThisFrame(helpText);

                    if (Game.IsControlJustPressed(Control.Context))
                    {
                        if (_interactionCooldown <= 0f)
                        {
                            OpenRecruitmentMenu(location.Gang);
                            _interactionCooldown = INTERACTION_COOLDOWN;
                        }
                    }

                    break;
                }
            }

            _wasNearRecruitmentLastFrame = isNearAnyRecruitment;
            _currentNearbyGang = nearbyGang;
        }

        private void OpenRecruitmentMenu(GangType gangType)
        {
            try
            {
                GangData gang = _gangSystem.GetGang(gangType);
                if (gang == null) return;

                if (_currentMenu != null && _currentMenu.Visible)
                    _currentMenu.Visible = false;

                _currentMenu = new global::LemonUI.Menus.NativeMenu("Gang Menu", $"~b~{gang.Name}");
                _menuPool.Add(_currentMenu);

                // Gang Info
                global::LemonUI.Menus.NativeItem infoItem = new global::LemonUI.Menus.NativeItem("Gang Info", $"Members: {gang.TotalMembers} | Power: {gang.PowerLevel}");
                infoItem.Enabled = false;
                _currentMenu.Add(infoItem);

                global::LemonUI.Menus.NativeItem territoriesItem = new global::LemonUI.Menus.NativeItem("Territories", $"{gang.TerritoryCount} controlled");
                territoriesItem.Enabled = false;
                _currentMenu.Add(territoriesItem);

                _currentMenu.Add(new global::LemonUI.Menus.NativeItem("─────────────────"));

                // Se é membro desta gangue
                if (_playerMembership.IsInGang(gangType))
                {
                    global::LemonUI.Menus.NativeItem currentRankItem = new global::LemonUI.Menus.NativeItem("Your Rank", $"~y~{_playerMembership.GetRankName()}");
                    currentRankItem.Enabled = false;
                    _currentMenu.Add(currentRankItem);

                    global::LemonUI.Menus.NativeItem respectItem = new global::LemonUI.Menus.NativeItem("Your Respect", $"~b~{_playerMembership.Respect}/100");
                    respectItem.Enabled = false;
                    _currentMenu.Add(respectItem);

                    _currentMenu.Add(new global::LemonUI.Menus.NativeItem("─────────────────"));

                    // ✅ BOTÃO DE MISSÕES
                    global::LemonUI.Menus.NativeItem missionsBtn = new global::LemonUI.Menus.NativeItem("~g~Gang Missions", "View and accept gang missions");
                    missionsBtn.Activated += (sender, e) =>
                    {
                        OpenMissionsMenu();
                    };
                    _currentMenu.Add(missionsBtn);

                    global::LemonUI.Menus.NativeItem leaveBtn = new global::LemonUI.Menus.NativeItem("~r~Leave Gang", "Leave this gang permanently");
                    leaveBtn.Activated += (sender, e) =>
                    {
                        _playerMembership.LeaveGang();
                        Notification.Show($"~r~You left {gang.Type.GetColor()}{gang.Name}");
                        _currentMenu.Visible = false;
                    };
                    _currentMenu.Add(leaveBtn);
                }
                else if (_playerMembership.IsInGang())
                {
                    global::LemonUI.Menus.NativeItem alreadyInGangItem = new global::LemonUI.Menus.NativeItem("~r~Already in another gang", "Leave your current gang first");
                    alreadyInGangItem.Enabled = false;
                    _currentMenu.Add(alreadyInGangItem);
                }
                else
                {
                    // Join gang logic (anterior)
                    int minCrimes = gang.Type.IsStreetGang() ? 5 : gang.Type.IsOrganizedCrime() ? 15 : 10;
                    int minRep = gang.Type.IsStreetGang() ? 10 : gang.Type.IsOrganizedCrime() ? 40 : 25;

                    global::LemonUI.Menus.NativeItem reqItem = new global::LemonUI.Menus.NativeItem("Requirements", $"{minCrimes} crimes | {minRep} reputation");
                    reqItem.Enabled = false;
                    _currentMenu.Add(reqItem);

                    bool canJoin = _playerMembership.CanJoinGang(gangType, gang);

                    global::LemonUI.Menus.NativeItem joinBtn = new global::LemonUI.Menus.NativeItem(
                        canJoin ? "~g~Join Gang" : "~r~Cannot Join Yet",
                        canJoin ? "Become a member" : "Requirements not met");

                    joinBtn.Enabled = canJoin;

                    if (canJoin)
                    {
                        joinBtn.Activated += (sender, e) =>
                        {
                            if (_playerMembership.JoinGang(gangType))
                            {
                                Notification.Show($"~g~Welcome to {gang.Type.GetColor()}{gang.Name}!");
                                _currentMenu.Visible = false;
                            }
                        };
                    }

                    _currentMenu.Add(joinBtn);
                }

                global::LemonUI.Menus.NativeItem closeBtn = new global::LemonUI.Menus.NativeItem("Close");
                closeBtn.Activated += (sender, e) => { _currentMenu.Visible = false; };
                _currentMenu.Add(closeBtn);

                _currentMenu.Visible = true;
            }
            catch (Exception ex)
            {
                Notification.Show($"~r~Menu Error: {ex.Message}");
            }
        }

        // ✅ NOVO: Menu de Missões
        private void OpenMissionsMenu()
        {
            try
            {
                if (!_playerMembership.IsInGang())
                {
                    Notification.Show("~r~You need to be in a gang!");
                    return;
                }

                if (_currentMenu != null && _currentMenu.Visible)
                    _currentMenu.Visible = false;

                _currentMenu = new global::LemonUI.Menus.NativeMenu("Gang Missions", $"~b~Available Missions");
                _menuPool.Add(_currentMenu);

                var missions = _missionGenerator.GetAvailableMissions();

                if (missions.Count == 0)
                {
                    global::LemonUI.Menus.NativeItem noMissionsItem = new global::LemonUI.Menus.NativeItem("~r~No missions available", "Check back later");
                    noMissionsItem.Enabled = false;
                    _currentMenu.Add(noMissionsItem);
                }
                else
                {
                    foreach (var mission in missions)
                    {
                        string colorCode = mission.DifficultyLevel <= 2 ? "~g~" : mission.DifficultyLevel <= 3 ? "~y~" : "~r~";

                        global::LemonUI.Menus.NativeItem missionItem = new global::LemonUI.Menus.NativeItem(
                            $"{colorCode}{mission.Name}",
                            $"{mission.Description}~n~" +
                            $"~w~Reward: ~g~${mission.MoneyReward}~w~ | Respect: ~y~+{mission.ReputationReward}~n~" +
                            $"~w~Difficulty: {colorCode}{"★".Repeat(mission.DifficultyLevel)}"
                        );

                        missionItem.Activated += (sender, e) =>
                        {
                            if (_missionController.StartMission(mission.Id))
                            {
                                _currentMenu.Visible = false;
                            }
                        };

                        _currentMenu.Add(missionItem);
                    }
                }

                global::LemonUI.Menus.NativeItem closeBtn = new global::LemonUI.Menus.NativeItem("Close");
                closeBtn.Activated += (sender, e) => { _currentMenu.Visible = false; };
                _currentMenu.Add(closeBtn);

                _currentMenu.Visible = true;
            }
            catch (Exception ex)
            {
                Notification.Show($"~r~Missions Menu Error: {ex.Message}");
            }
        }

        private void ShowDebugInfo()
        {
            if (!_debugEnabled) return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            string gangInfo = _playerMembership.IsInGang()
                ? $"{_playerMembership.CurrentGang.Value.GetColor()}{_playerMembership.CurrentGang.Value.GetDisplayName()}"
                : "~w~None";

            string rankInfo = _playerMembership.IsInGang()
                ? $"~y~{_playerMembership.GetRankName()} ~w~({_playerMembership.Respect})"
                : "~w~N/A";

            int activeNPCs = _activitySystem.GetActiveGangMemberCount();
            int availableMissions = _missionGenerator.GetAvailableMissions().Count;
            bool hasActiveMission = _missionController.HasActiveMission;
            bool ambushActive = _ambushSystem.IsAmbushActive;

            string debugText =
                $"~y~=== GANG DEBUG ===~n~" +
                $"~w~Gang: {gangInfo}~n~" +
                $"~w~Rank: {rankInfo}~n~" +
                $"~w~Active NPCs: ~g~{activeNPCs}~n~" +
                $"~w~Missions: ~b~{availableMissions} available~n~" +
                $"~w~Active Mission: {(hasActiveMission ? "~g~YES" : "~r~NO")}~n~" +
                $"~w~Ambush: {(ambushActive ? "~r~ACTIVE!" : "~g~None")}";

            global::GTA.UI.Screen.ShowSubtitle(debugText, 1050);
        }

        public void Shutdown()
        {
            try
            {
                foreach (var blip in _territoryBlips.Values)
                {
                    if (blip != null && blip.Exists())
                        blip.Delete();
                }
                _territoryBlips.Clear();

                foreach (var blip in _recruitmentBlips.Values)
                {
                    if (blip != null && blip.Exists())
                        blip.Delete();
                }
                _recruitmentBlips.Clear();

                if (_currentMenu != null)
                    _currentMenu.Visible = false;

                _activitySystem?.Shutdown();
                _missionController?.Shutdown();
                _ambushSystem?.Shutdown();
                _gangSystem?.Shutdown();
            }
            catch (Exception ex)
            {
                Notification.Show($"~r~[Gang System] Shutdown Error: {ex.Message}");
            }
        }

        public void OnPlayerCommittedCrime(CrimeRecord crime)
        {
            if (crime == null) return;
            _playerMembership.RecordCrime();

            if (_playerMembership.IsInGang())
            {
                var gang = _gangSystem.GetGang(_playerMembership.CurrentGang.Value);
                gang?.RecordCrime();
            }
        }

        public GangType? GetPlayerGang() => _playerMembership.CurrentGang;
        public bool IsPlayerInGang(GangType gang) => _playerMembership.IsInGang(gang);
        public GangRank? GetPlayerRank() => _playerMembership.IsInGang() ? _playerMembership.Rank : (GangRank?)null;
    }

    public class RecruitmentLocation
    {
        public Vector3 Position { get; set; }
        public string Name { get; set; }
        public GangType Gang { get; set; }
    }
}

// Helper extension
public static class StringExtensions
{
    public static string Repeat(this string s, int count)
    {
        return new string(s.ToCharArray()[0], count);
    }
}