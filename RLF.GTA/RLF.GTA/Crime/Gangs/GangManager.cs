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

namespace RLF.GTA.Gangs
{
    /// <summary>
    /// Gang Manager com LemonUI - VERSÃO CORRIGIDA
    /// NÃO herda de Script - é gerenciado pelo CrimeManager
    /// </summary>
    public class GangManager
    {
        private GangSystem _gangSystem;
        private PlayerGangMembership _playerMembership;
        private GangActivitySystem _activitySystem;
        private CrimeSystem _crimeSystem;
        private EconomySystem _economySystem;

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
        private const float INTERACTION_COOLDOWN = 0.5f; // 500ms entre aberturas de menu

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

                // LemonUI Pool
                _menuPool = new global::LemonUI.ObjectPool();
                Notification.Show("~g~LemonUI Pool OK");

                _gangSystem = new GangSystem();
                _gangSystem.Initialize();
                Notification.Show("~g~GangSystem OK");

                _playerMembership = new PlayerGangMembership();
                Notification.Show("~g~PlayerMembership OK");

                _activitySystem = new GangActivitySystem(_gangSystem, _crimeSystem);
                Notification.Show("~g~ActivitySystem OK");

                SetupRecruitmentLocations();
                Notification.Show($"~g~{_recruitmentLocations.Count} recruitment locations");

                CreateTerritoryBlips();
                Notification.Show($"~g~{_territoryBlips.Count} territory blips");

                CreateRecruitmentBlips();
                Notification.Show($"~g~{_recruitmentBlips.Count} recruitment blips");

                _isInitialized = true;
                _lastDebugTime = DateTime.Now;

                Notification.Show("~g~[Gang System] READY!");
                Notification.Show("~y~Press E near gang blips | F7 = Debug");
            }
            catch (Exception ex)
            {
                Notification.Show($"~r~[Gang System] ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Chamado pelo CrimeManager a cada frame
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isInitialized) return;

            try
            {
                // Atualiza cooldown
                if (_interactionCooldown > 0f)
                {
                    _interactionCooldown -= deltaTime;
                }

                // Processa menus LemonUI
                _menuPool.Process();

                // Atualiza sistemas
                _activitySystem.Update(deltaTime);

                // Verifica interação com locais de recrutamento
                CheckRecruitmentInteractions();

                // Debug
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

        /// <summary>
        /// Chamado pelo CrimeManager quando uma tecla é pressionada
        /// </summary>
        public void OnKeyDown(System.Windows.Forms.Keys key)
        {
            try
            {
                if (key == System.Windows.Forms.Keys.F7)
                {
                    _debugEnabled = !_debugEnabled;
                    Notification.Show(_debugEnabled ? "~g~Gang Debug ON" : "~r~Gang Debug OFF");
                }
            }
            catch (Exception ex)
            {
                Notification.Show($"~r~[Gang System] Key Error: {ex.Message}");
            }
        }

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

                    // Mostra help text
                    string helpText = $"Pressione ~INPUT_CONTEXT~ para interagir com {location.Gang.GetDisplayName()}";
                    global::GTA.UI.Screen.ShowHelpTextThisFrame(helpText);

                    // Verifica se E foi pressionado
                    if (Game.IsControlJustPressed(Control.Context))
                    {
                        // Verifica cooldown
                        if (_interactionCooldown <= 0f)
                        {
                            OpenRecruitmentMenu(location.Gang);
                            _interactionCooldown = INTERACTION_COOLDOWN;
                        }
                    }

                    break; // Só processa o mais próximo
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
                if (gang == null)
                {
                    Notification.Show($"~r~ERROR: Gang {gangType} not found!");
                    return;
                }

                // Fecha menu anterior se existir
                if (_currentMenu != null && _currentMenu.Visible)
                {
                    _currentMenu.Visible = false;
                }

                // Cria novo menu
                _currentMenu = new global::LemonUI.Menus.NativeMenu("Gang Recruitment", $"~b~{gang.Name}");
                _menuPool.Add(_currentMenu);

                // ===== INFO =====
                global::LemonUI.Menus.NativeItem infoItem = new global::LemonUI.Menus.NativeItem("Gang Info", $"Members: {gang.TotalMembers} | Power: {gang.PowerLevel}");
                infoItem.Enabled = false;
                _currentMenu.Add(infoItem);

                global::LemonUI.Menus.NativeItem territoriesItem = new global::LemonUI.Menus.NativeItem("Territories", $"{gang.TerritoryCount} controlled");
                territoriesItem.Enabled = false;
                _currentMenu.Add(territoriesItem);

                global::LemonUI.Menus.NativeItem reputationItem = new global::LemonUI.Menus.NativeItem("Reputation", $"{gang.Reputation}/100");
                reputationItem.Enabled = false;
                _currentMenu.Add(reputationItem);

                _currentMenu.Add(new global::LemonUI.Menus.NativeItem("─────────────────"));

                // ===== JÁ É MEMBRO =====
                if (_playerMembership.IsInGang())
                {
                    if (_playerMembership.IsInGang(gangType))
                    {
                        // É membro desta gangue
                        global::LemonUI.Menus.NativeItem currentRankItem = new global::LemonUI.Menus.NativeItem("Your Rank", $"~y~{_playerMembership.GetRankName()}");
                        currentRankItem.Enabled = false;
                        _currentMenu.Add(currentRankItem);

                        global::LemonUI.Menus.NativeItem respectItem = new global::LemonUI.Menus.NativeItem("Your Respect", $"~b~{_playerMembership.Respect}/100");
                        respectItem.Enabled = false;
                        _currentMenu.Add(respectItem);

                        _currentMenu.Add(new global::LemonUI.Menus.NativeItem("─────────────────"));

                        global::LemonUI.Menus.NativeItem leaveBtn = new global::LemonUI.Menus.NativeItem("~r~Leave Gang", "Leave this gang permanently");
                        leaveBtn.Activated += (sender, e) =>
                        {
                            _playerMembership.LeaveGang();
                            Notification.Show($"~r~You left {gang.Type.GetColor()}{gang.Name}");
                            _currentMenu.Visible = false;
                        };
                        _currentMenu.Add(leaveBtn);
                    }
                    else
                    {
                        // Já é de outra gangue
                        global::LemonUI.Menus.NativeItem alreadyInGangItem = new global::LemonUI.Menus.NativeItem("~r~Already in another gang", "Leave your current gang first");
                        alreadyInGangItem.Enabled = false;
                        _currentMenu.Add(alreadyInGangItem);
                    }
                }
                else
                {
                    // ===== NÃO É MEMBRO - MOSTRAR REQUISITOS =====
                    int minCrimes = gang.Type.IsStreetGang() ? 5 :
                                   gang.Type.IsOrganizedCrime() ? 15 :
                                   gang.Type == GangType.LostMC ? 10 : 5;

                    int minRep = gang.Type.IsStreetGang() ? 10 :
                                gang.Type.IsOrganizedCrime() ? 40 :
                                gang.Type == GangType.LostMC ? 25 : 10;

                    global::LemonUI.Menus.NativeItem reqItem = new global::LemonUI.Menus.NativeItem("Requirements", $"{minCrimes} crimes | {minRep} reputation");
                    reqItem.Enabled = false;
                    _currentMenu.Add(reqItem);

                    global::LemonUI.Menus.NativeItem yourCrimesItem = new global::LemonUI.Menus.NativeItem("Your Crimes", $"{_playerMembership.CrimesCommitted} committed");
                    yourCrimesItem.Enabled = false;
                    _currentMenu.Add(yourCrimesItem);

                    global::LemonUI.Menus.NativeItem yourRepItem = new global::LemonUI.Menus.NativeItem("Your Reputation", $"{_playerMembership.GetReputationWithGang(gangType)}");
                    yourRepItem.Enabled = false;
                    _currentMenu.Add(yourRepItem);

                    _currentMenu.Add(new global::LemonUI.Menus.NativeItem("─────────────────"));

                    bool canJoin = _playerMembership.CanJoinGang(gangType, gang);

                    global::LemonUI.Menus.NativeItem joinBtn = new global::LemonUI.Menus.NativeItem(
                        canJoin ? "~g~Join Gang" : "~r~Cannot Join Yet",
                        canJoin ? "Become a member" : "Requirements not met"
                    );

                    joinBtn.Enabled = canJoin;

                    if (canJoin)
                    {
                        joinBtn.Activated += (sender, e) =>
                        {
                            if (_playerMembership.JoinGang(gangType))
                            {
                                Notification.Show($"~g~Welcome to {gang.Type.GetColor()}{gang.Name}!");
                                Notification.Show($"~y~Rank: ~w~{_playerMembership.GetRankName()}");
                                _currentMenu.Visible = false;
                            }
                        };
                    }

                    _currentMenu.Add(joinBtn);
                }

                // ===== CLOSE =====
                global::LemonUI.Menus.NativeItem closeBtn = new global::LemonUI.Menus.NativeItem("Close");
                closeBtn.Activated += (sender, e) => { _currentMenu.Visible = false; };
                _currentMenu.Add(closeBtn);

                // Mostra o menu
                _currentMenu.Visible = true;

                Notification.Show($"~g~Opening menu for {gang.Name}");
            }
            catch (Exception ex)
            {
                Notification.Show($"~r~OpenRecruitmentMenu ERROR: {ex.Message}");
            }
        }

        private void ShowDebugInfo()
        {
            if (!_debugEnabled) return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            Vector3 playerPos = player.Position;

            float nearestDist = float.MaxValue;
            string nearestGang = "None";

            foreach (var location in _recruitmentLocations.Values)
            {
                float dist = Vector3.Distance(playerPos, location.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestGang = location.Gang.GetDisplayName();
                }
            }

            string gangInfo = _playerMembership.IsInGang()
                ? $"{_playerMembership.CurrentGang.Value.GetColor()}{_playerMembership.CurrentGang.Value.GetDisplayName()}"
                : "~w~None";

            string rankInfo = _playerMembership.IsInGang()
                ? $"~y~{_playerMembership.GetRankName()} ~w~({_playerMembership.Respect})"
                : "~w~N/A";

            var currentTerritory = TerritoryDatabase.GetTerritoryAtPosition(
                player.Position.X, player.Position.Y, player.Position.Z);

            string territoryInfo = currentTerritory != null
                ? $"~b~{currentTerritory.Name}"
                : "~w~None";

            int activeNPCs = _activitySystem.GetActiveGangMemberCount();
            int crimes = _playerMembership.CrimesCommitted;

            string debugText =
                $"~y~=== GANG DEBUG ===~n~" +
                $"~w~Your Gang: {gangInfo}~n~" +
                $"~w~Rank: {rankInfo}~n~" +
                $"~w~Crimes: ~r~{crimes}~n~" +
                $"~w~Territory: {territoryInfo}~n~" +
                $"~w~Nearest Gang: ~p~{nearestGang} ~w~({nearestDist:F1}m)~n~" +
                $"~w~Active Gang NPCs: ~g~{activeNPCs}";

            global::GTA.UI.Screen.ShowSubtitle(debugText, 1050);
        }

        public void Shutdown()
        {
            try
            {
                Notification.Show("~y~[Gang System] Shutting down...");

                // Remove blips
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

                // Fecha menus
                if (_currentMenu != null)
                {
                    _currentMenu.Visible = false;
                }

                _activitySystem?.Shutdown();
                _gangSystem?.Shutdown();

                Notification.Show("~g~[Gang System] Shutdown complete");
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

        public void OnPlayerEarnedMoney(decimal amount)
        {
            if (amount <= 0) return;
            _playerMembership.RecordMoneyEarned(amount);
        }

        public bool CaptureTerritory(string territoryId)
        {
            if (!_playerMembership.IsInGang()) return false;

            var territory = TerritoryDatabase.GetTerritoryById(territoryId);
            if (territory == null) return false;

            var playerGang = _playerMembership.CurrentGang.Value;
            var previousGang = territory.ControllingGang;

            if (previousGang.HasValue)
            {
                _gangSystem.TransferTerritory(territoryId, previousGang.Value, playerGang);
            }
            else
            {
                var gang = _gangSystem.GetGang(playerGang);
                gang?.AddTerritory(territoryId);
            }

            territory.StartAttack(playerGang);
            territory.UpdateAttack(1.0f);

            if (_territoryBlips.ContainsKey(territoryId))
            {
                _territoryBlips[territoryId].Color = GetBlipColorForGang(playerGang);
                _territoryBlips[territoryId].Name = $"{territory.Name} ({playerGang.GetDisplayName()})";
            }

            _playerMembership.RecordTerritoryCapture();

            Notification.Show($"~g~Territory Captured!~n~" +
                $"~w~{territory.Name} is now controlled by {playerGang.GetColor()}{playerGang.GetDisplayName()}");

            return true;
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
