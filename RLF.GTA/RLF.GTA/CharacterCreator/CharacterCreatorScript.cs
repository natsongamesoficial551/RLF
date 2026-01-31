using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using RLF.Core.CharacterCreator.Data;
using RLF.Core.CharacterCreator.Enums;
using RLF.Core.Events;
using RLF.GTA.CharacterCreator.Core;
using RLF.GTA.CharacterCreator.Integration;
using RLF.GTA.CharacterCreator.World;
using System;
using System.Windows.Forms;

namespace RLF.GTA.CharacterCreator
{
    public class CharacterCreatorScript : Script
    {
        private readonly MenuPool _menuPool;
        private UIMenu _mainMenu;
        private UIMenu _geneticsMenu;
        private UIMenu _faceMenu;
        private UIMenu _hairMenu;
        private UIMenu _facialHairMenu;
        private UIMenu _clothingMenu;
        private UIMenu _accessoriesMenu;
        private UIMenu _savedCharactersMenu;
        private UIMenu _loadCharacterMenu;

        private int _creatorStep = -1;
        private int _creatorTimer = 0;

        private CharacterCreatorSystem _system;
        private bool _isCreatorActive;
        private bool _isWaitingForName;
        private bool _hasAutoLoaded;

        private CharacterData _currentCharacter;
        private int _currentCharacterSlot = -1;

        private int _autoSavePositionTimer = 0;
        private const int AUTO_SAVE_POSITION_INTERVAL = 1800;

        private string _cachedPauseName = null;
        private bool _pauseNameApplied = false;
        private int _simulationEnforceTimer = 0;

        // ⭐ FIX: Sistema de aplicação atrasada
        private CharacterData _pendingLoadCharacter;
        private int _pendingLoadSlot = -1;
        private int _pendingLoadTimer = 0;
        private bool _pendingLoadActive = false;

        private bool _pendingTeleport = false;
        private int _pendingTeleportTimer = 0;

        private bool _pendingExit = false;
        private int _exitTimer = 0;

        private CharacterData _pendingEditCharacter;
        private int _pendingEditSlot = -1;
        private int _editTimer = 0;

        private bool _pendingGenderChangeActive = false;
        private CharacterGender _pendingGenderChange;
        private int _genderChangeTimer = 0;

        private Vector3 _originalPosition;
        private float _originalHeading;

        private readonly Vector3 _creatorPosition = new Vector3(402.87f, -996.87f, -100f);
        private readonly float _creatorHeading = 180f;

        private readonly Keys _goToCreatorKey = Keys.Z;
        private readonly Keys _editKey = Keys.E;
        private readonly Keys _loadMenuKey = Keys.L;

        private const int MAX_CHARACTERS = 25;

        public CharacterCreatorScript()
        {
            _menuPool = new MenuPool();
            Tick += OnTick;
            KeyDown += OnKeyDown;
            Aborted += OnAborted;
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                _system = CharacterCreatorSystem.Instance;
                _system.Initialize();

                IdentityIntegration.Initialize();
                EconomyIntegration.Initialize(false);

                BuildMenus();

                _isCreatorActive = false;
                _isWaitingForName = false;
                _hasAutoLoaded = false;

                global::GTA.UI.Notification.Show("~g~Character Creator~w~ carregado!\n~b~Z~w~ = Criar personagem\n~b~L~w~ = Carregar personagem\n~b~E~w~ = Menu");
            }
            catch (Exception ex)
            {
                global::GTA.UI.Notification.Show("~r~Erro: " + ex.Message);
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                _menuPool.ProcessMenus();

                _simulationEnforceTimer++;
                if (_simulationEnforceTimer >= 60)
                {
                    EnforceSimulationMode();
                    _simulationEnforceTimer = 0;
                }

                if (!_hasAutoLoaded && !_isCreatorActive)
                {
                    _hasAutoLoaded = true;
                    HandleAutoLoad();
                }

                if (_currentCharacter != null && _currentCharacterSlot >= 0 && !_isCreatorActive)
                {
                    _autoSavePositionTimer++;

                    if (_autoSavePositionTimer >= AUTO_SAVE_POSITION_INTERVAL)
                    {
                        AutoSavePosition();
                        _autoSavePositionTimer = 0;
                    }
                }

                // ⭐ FIX: Sistema de aplicação com delay
                if (_pendingLoadActive)
                {
                    _pendingLoadTimer++;

                    if (_pendingLoadTimer == 1)
                    {
                        global::GTA.UI.Screen.FadeOut(500);
                    }

                    if (_pendingLoadTimer == 30)
                    {
                        // Trocar modelo se necessário
                        PedHash requiredHash = _pendingLoadCharacter.Gender == CharacterGender.Male
                            ? PedHash.FreemodeMale01
                            : PedHash.FreemodeFemale01;

                        Ped player = Game.Player.Character;
                        if (player.Model.Hash != (int)requiredHash)
                        {
                            Model model = new Model(requiredHash);
                            model.Request(5000);
                            if (model.IsLoaded)
                            {
                                Function.Call(Hash.SET_PLAYER_MODEL, Game.Player.Handle, model.Hash);
                                model.MarkAsNoLongerNeeded();
                            }
                        }
                    }

                    if (_pendingLoadTimer == 60)
                    {
                        Ped player = Game.Player.Character;
                        if (player != null && player.Exists())
                        {
                            // Aplicar personagem
                            _system.Manager.Builder.SetPed(player);
                            _system.Manager.Builder.ApplyFullCharacter(_pendingLoadCharacter);

                            // Teleportar para posição
                            Vector3 spawnPos = CharacterPositionManager.GetSafePosition(_pendingLoadCharacter);
                            float spawnHeading = _pendingLoadCharacter.LastHeading;

                            player.Position = spawnPos;
                            player.Heading = spawnHeading;
                            player.IsPositionFrozen = false;
                            player.IsInvincible = false;

                            System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                            System.Diagnostics.Debug.WriteLine($"✅ PERSONAGEM APLICADO: {_pendingLoadCharacter.Name}");
                            System.Diagnostics.Debug.WriteLine($"   Spawn: ({spawnPos.X:F2}, {spawnPos.Y:F2}, {spawnPos.Z:F2})");
                            System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                            _currentCharacter = _pendingLoadCharacter;
                            _currentCharacterSlot = _pendingLoadSlot;

                            global::GTA.UI.Screen.FadeIn(500);
                            global::GTA.UI.Notification.Show($"~g~{_pendingLoadCharacter.Name}~w~ carregado!");

                            _pendingLoadActive = false;
                            _pendingLoadCharacter = null;
                            _pendingLoadSlot = -1;
                            _pendingLoadTimer = 0;
                        }
                    }
                }

                if (_creatorStep >= 0)
                {
                    _creatorTimer++;

                    switch (_creatorStep)
                    {
                        case 0:
                            global::GTA.UI.Screen.FadeOut(500);
                            _creatorTimer = 0;
                            _creatorStep = 1;
                            break;

                        case 1:
                            if (_creatorTimer < 30) break;
                            Function.Call(Hash.REQUEST_IPL, "v_carshowroom");
                            _creatorTimer = 0;
                            _creatorStep = 2;
                            break;

                        case 2:
                            if (_creatorTimer < 30) break;
                            Model model = new Model(PedHash.FreemodeMale01);
                            model.Request(5000);
                            if (model.IsLoaded)
                            {
                                Function.Call(Hash.SET_PLAYER_MODEL, Game.Player.Handle, model.Hash);
                                model.MarkAsNoLongerNeeded();
                                _creatorTimer = 0;
                                _creatorStep = 3;
                            }
                            break;

                        case 3:
                            if (_creatorTimer < 20) break;
                            Ped creatorPed = Game.Player.Character;
                            if (creatorPed != null && creatorPed.Exists())
                            {
                                creatorPed.Position = _creatorPosition;
                                creatorPed.Heading = _creatorHeading;
                                creatorPed.IsPositionFrozen = true;
                                creatorPed.IsInvincible = true;
                                _system.Manager.StartCreatorSession(creatorPed);
                            }
                            global::GTA.UI.Screen.FadeIn(500);
                            _creatorStep = -1;
                            break;
                    }
                }

                if (Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE))
                {
                    ApplyPauseMenuCharacterName();
                }
                else
                {
                    _pauseNameApplied = false;
                }

                if (_pendingExit)
                {
                    _exitTimer++;
                    if (_exitTimer > 20)
                    {
                        Ped player = Game.Player.Character;
                        if (player != null && player.Exists())
                        {
                            player.IsPositionFrozen = false;
                            player.IsInvincible = false;
                            player.Position = _originalPosition;
                            player.Heading = _originalHeading;
                        }
                        _isCreatorActive = false;
                        _mainMenu.Visible = false;
                        global::GTA.UI.Screen.FadeIn(500);
                        global::GTA.UI.Notification.Show("~y~Saiu sem salvar~w~");
                        _pendingExit = false;
                        _exitTimer = 0;
                    }
                }

                if (_pendingTeleport)
                {
                    _pendingTeleportTimer++;
                    if (_pendingTeleportTimer > 25)
                    {
                        Ped player = Game.Player.Character;
                        if (player != null && player.Exists())
                        {
                            player.IsPositionFrozen = false;
                            player.IsInvincible = false;

                            if (_currentCharacter != null)
                            {
                                Vector3 spawnPos = CharacterPositionManager.GetSafePosition(_currentCharacter);
                                float spawnHeading = _currentCharacter.LastHeading;

                                player.Position = spawnPos;
                                player.Heading = spawnHeading;
                            }
                        }

                        global::GTA.UI.Screen.FadeIn(500);
                        _pendingTeleport = false;
                        _pendingTeleportTimer = 0;
                        _isCreatorActive = false;
                    }
                }

                if (_pendingEditCharacter != null)
                {
                    _editTimer++;
                    if (_editTimer == 15)
                    {
                        Function.Call(Hash.REQUEST_IPL, "v_carshowroom");
                    }
                    if (_editTimer > 30)
                    {
                        bool success = _system.Manager.StartEditSession(_pendingEditCharacter, _pendingEditSlot, _creatorPosition, _creatorHeading);
                        global::GTA.UI.Screen.FadeIn(500);
                        if (success)
                        {
                            _isCreatorActive = true;
                            _mainMenu.Visible = true;
                            global::GTA.UI.Notification.Show("~y~Editando: " + _pendingEditCharacter.Name + "~w~\n~b~E~w~ = Menu | ~b~A/D~w~ = Girar");
                        }
                        else
                        {
                            global::GTA.UI.Notification.Show("~r~Erro ao iniciar edição!");
                        }
                        _pendingEditCharacter = null;
                        _pendingEditSlot = -1;
                        _editTimer = 0;
                    }
                }

                if (_pendingGenderChangeActive)
                {
                    _genderChangeTimer++;
                    if (_genderChangeTimer > 10)
                    {
                        PedHash hash = _pendingGenderChange == CharacterGender.Male ? PedHash.FreemodeMale01 : PedHash.FreemodeFemale01;
                        Model model = new Model(hash);
                        model.Request(5000);
                        if (model.IsLoaded)
                        {
                            Ped player = Game.Player.Character;
                            Vector3 pos = player.Position;
                            float heading = player.Heading;
                            Function.Call(Hash.SET_PLAYER_MODEL, Game.Player.Handle, model.Hash);
                            model.MarkAsNoLongerNeeded();
                            player = Game.Player.Character;
                            player.Position = pos;
                            player.Heading = heading;
                            player.IsPositionFrozen = true;
                            player.IsInvincible = true;
                            _system.Manager.CharacterData.Gender = _pendingGenderChange;
                            _system.Manager.Builder.SetPed(player);
                            _system.Manager.Builder.ApplyDefaultAppearance();
                            global::GTA.UI.Notification.Show(_pendingGenderChange == CharacterGender.Male ? "~b~Masculino" : "~p~Feminino");
                        }
                        _pendingGenderChangeActive = false;
                        _genderChangeTimer = 0;
                    }
                }

                if (_isWaitingForName)
                {
                    ProcessNameInput();
                    return;
                }

                if (_isCreatorActive)
                {
                    Ped player = Game.Player.Character;
                    DisableMovementControls();
                    if (player != null && player.Exists())
                    {
                        player.Position = _creatorPosition;
                        player.IsPositionFrozen = true;
                        player.IsInvincible = true;
                        if (Game.IsControlPressed(global::GTA.Control.MoveLeft)) player.Heading += 2f;
                        if (Game.IsControlPressed(global::GTA.Control.MoveRight)) player.Heading -= 2f;
                    }
                    if (!_mainMenu.Visible && !AnySubmenuVisible())
                    {
                        global::GTA.UI.Screen.ShowHelpTextThisFrame("Pressione ~b~E~w~ para abrir o menu | ~b~ESC~w~ para sair");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ OnTick erro: {ex.Message}");
            }
        }

        private void AutoSavePosition()
        {
            try
            {
                if (_currentCharacter == null || _currentCharacterSlot < 0) return;

                Ped player = Game.Player.Character;
                if (player == null || !player.Exists()) return;

                CharacterPositionManager.SaveCurrentPosition(_currentCharacter);
                _system.SlotManager.SaveSlot(_currentCharacterSlot, _currentCharacter);

                System.Diagnostics.Debug.WriteLine($"💾 Auto-save: {_currentCharacter.Name} @ ({_currentCharacter.LastPositionX:F2}, {_currentCharacter.LastPositionY:F2}, {_currentCharacter.LastPositionZ:F2})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro auto-save: {ex.Message}");
            }
        }

        private void HandleAutoLoad()
        {
            try
            {
                _system.SlotManager.LoadAllSlots();

                if (!_system.SlotManager.HasAnyCharacter())
                {
                    System.Diagnostics.Debug.WriteLine("⚡ Primeiro acesso - criar personagem");
                    global::GTA.UI.Notification.Show("~b~Bem-vindo!~w~\nCrie seu primeiro personagem");
                    Wait(2000);
                    GoToCreator();
                    return;
                }

                var (lastCharacter, lastSlot) = _system.SlotManager.GetMostRecentCharacterWithSlot();

                if (lastCharacter != null && lastSlot >= 0)
                {
                    System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                    System.Diagnostics.Debug.WriteLine($"⚡ AUTO-LOAD: {lastCharacter.Name} (Slot {lastSlot})");
                    System.Diagnostics.Debug.WriteLine($"   Posição: ({lastCharacter.LastPositionX:F2}, {lastCharacter.LastPositionY:F2}, {lastCharacter.LastPositionZ:F2})");
                    System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                    global::GTA.UI.Notification.Show($"~g~Carregando~w~ {lastCharacter.Name}...");

                    // ⭐ FIX: Usar sistema de aplicação atrasada
                    _pendingLoadCharacter = lastCharacter;
                    _pendingLoadSlot = lastSlot;
                    _pendingLoadTimer = 0;
                    _pendingLoadActive = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ HandleAutoLoad erro: {ex.Message}\n{ex.StackTrace}");
                global::GTA.UI.Notification.Show("~r~Erro no auto-load!");
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (_isWaitingForName) return;

                if (e.KeyCode == _goToCreatorKey && !_isCreatorActive)
                {
                    if (_currentCharacter != null && _currentCharacterSlot >= 0)
                    {
                        AutoSavePosition();
                    }
                    GoToCreator();
                }

                if (e.KeyCode == _loadMenuKey && !_isCreatorActive && !_loadCharacterMenu.Visible)
                {
                    if (_currentCharacter != null && _currentCharacterSlot >= 0)
                    {
                        AutoSavePosition();
                    }
                    RefreshLoadCharacterMenu();
                    _loadCharacterMenu.Visible = true;
                }

                if (e.KeyCode == _editKey && _isCreatorActive)
                {
                    _mainMenu.Visible = !_mainMenu.Visible;
                }

                if (e.KeyCode == Keys.Escape && _isCreatorActive && !_mainMenu.Visible && !AnySubmenuVisible())
                {
                    ExitWithoutSaving();
                }
            }
            catch { }
        }

        private void EnforceSimulationMode()
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists()) return;

                Function.Call(Hash.SET_MISSION_FLAG, false);
                Function.Call(Hash.CLEAR_PLAYER_WANTED_LEVEL, Game.Player.Handle);
                Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player.Handle, 0, false);
                Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player.Handle, false);
                Function.Call(Hash.SET_PLAYER_SWITCH_OUTRO, 0, 0);
                Function.Call(Hash.SET_GAME_PAUSED, false);

                player.IsPositionFrozen = false;
                player.IsInvincible = false;
                player.CanRagdoll = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro EnforceSimulationMode: {ex.Message}");
            }
        }

        private void GoToCreator()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            _originalPosition = player.Position;
            _originalHeading = player.Heading;
            _isCreatorActive = true;
            _creatorStep = 0;
        }

        private void ExitWithoutSaving()
        {
            try
            {
                global::GTA.UI.Screen.FadeOut(500);
                _pendingExit = true;
                _exitTimer = 0;
            }
            catch { }
        }

        private void StartSaveProcess()
        {
            _mainMenu.Visible = false;
            Function.Call(Hash.DISPLAY_ONSCREEN_KEYBOARD, true, "FMMC_KEY_TIP8", "", "", "", "", "", 30);
            _isWaitingForName = true;
            global::GTA.UI.Notification.Show("~b~Digite o nome do personagem~w~");
        }

        private void ProcessNameInput()
        {
            int status = Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD);

            if (status == 1)
            {
                string name = Function.Call<string>(Hash.GET_ONSCREEN_KEYBOARD_RESULT);
                FinishSaveWithName(string.IsNullOrEmpty(name) ? "Personagem" : name);
                _isWaitingForName = false;
            }
            else if (status == 2)
            {
                _isWaitingForName = false;
                _mainMenu.Visible = true;
                global::GTA.UI.Notification.Show("~y~Cancelado~w~");
            }
        }

        private void FinishSaveWithName(string name)
        {
            try
            {
                var character = _system.Manager.CharacterData;
                if (character != null)
                {
                    character.Name = name;
                    character.ModifiedAt = DateTime.Now;

                    if (character.LastPositionX == 0f && character.LastPositionY == 0f && character.LastPositionZ == 0f)
                    {
                        CharacterPositionManager.ResetToDefaultPosition(character);
                    }

                    int slot = _system.SlotManager.SaveToNextAvailableSlot(character);

                    if (slot >= 0)
                    {
                        _currentCharacter = character;
                        _currentCharacterSlot = slot;

                        System.Diagnostics.Debug.WriteLine($"💾 Personagem salvo: {name} no Slot {slot}");
                        global::GTA.UI.Notification.Show($"~g~Personagem '{name}' salvo!");

                        TeleportToLastPosition();
                    }
                    else
                    {
                        global::GTA.UI.Notification.Show("~r~Erro ao salvar!");
                    }
                }
            }
            catch (Exception ex)
            {
                global::GTA.UI.Notification.Show($"~r~Erro: {ex.Message}");
            }
        }

        private void TeleportToLastPosition()
        {
            try
            {
                global::GTA.UI.Screen.FadeOut(500);
                _pendingTeleport = true;
                _pendingTeleportTimer = 0;
            }
            catch { }
        }

        private void ApplyPauseMenuCharacterName()
        {
            try
            {
                var character = _currentCharacter ?? _system?.Manager?.CharacterData;
                if (character == null) return;

                string name = character.Name;
                if (string.IsNullOrWhiteSpace(name)) return;
                if (_pauseNameApplied && _cachedPauseName == name) return;

                Scaleform pauseHeader = new Scaleform("PAUSE_MENU_HEADER");
                pauseHeader.CallFunction("SET_HEADER_TITLE", name.ToUpper());

                _cachedPauseName = name;
                _pauseNameApplied = true;
            }
            catch { }
        }

        private void LoadCharacter(int slotIndex)
        {
            try
            {
                if (_currentCharacter != null && _currentCharacterSlot >= 0)
                {
                    CharacterPositionManager.SaveCurrentPosition(_currentCharacter);
                    _system.SlotManager.SaveSlot(_currentCharacterSlot, _currentCharacter);
                }

                var character = _system.SlotManager.GetSlot(slotIndex);
                if (character == null)
                {
                    global::GTA.UI.Notification.Show("~r~Slot vazio!");
                    return;
                }

                _loadCharacterMenu.Visible = false;

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"🔥 LOAD: {character.Name} (Slot {slotIndex})");
                System.Diagnostics.Debug.WriteLine($"   Pos: ({character.LastPositionX:F2}, {character.LastPositionY:F2}, {character.LastPositionZ:F2})");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                global::GTA.UI.Notification.Show($"~g~Carregando~w~ {character.Name}...");

                // ⭐ FIX: Usar sistema de aplicação atrasada
                _pendingLoadCharacter = character;
                _pendingLoadSlot = slotIndex;
                _pendingLoadTimer = 0;
                _pendingLoadActive = true;
            }
            catch (Exception ex)
            {
                global::GTA.UI.Notification.Show($"~r~Erro: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ LoadCharacter erro: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void EditCharacter(int slotIndex)
        {
            try
            {
                var character = _system.SlotManager.GetSlot(slotIndex);
                if (character == null)
                {
                    global::GTA.UI.Notification.Show("~r~Slot vazio!");
                    return;
                }

                _savedCharactersMenu.Visible = false;
                global::GTA.UI.Screen.FadeOut(500);

                _pendingEditCharacter = character;
                _pendingEditSlot = slotIndex;
                _editTimer = 0;
            }
            catch (Exception ex)
            {
                global::GTA.UI.Notification.Show($"~r~Erro: {ex.Message}");
            }
        }

        private void BuildMenus()
        {
            _mainMenu = new UIMenu("CRIAR PERSONAGEM", "~b~Personalize sua aparência");
            _menuPool.Add(_mainMenu);

            var genderList = new System.Collections.Generic.List<dynamic> { "Masculino", "Feminino" };
            var genderItem = new UIMenuListItem("Gênero", genderList, 0);
            genderItem.OnListChanged += (sender, index) =>
            {
                if (!_isCreatorActive) return;
                ChangeGender(index == 0 ? CharacterGender.Male : CharacterGender.Female);
            };
            _mainMenu.AddItem(genderItem);

            AddSeparator(_mainMenu);

            _geneticsMenu = new UIMenu("GENÉTICA", "~b~Herança dos pais");
            _menuPool.Add(_geneticsMenu);
            var geneticsItem = new UIMenuItem("Genética");
            _mainMenu.AddItem(geneticsItem);
            _mainMenu.BindMenuToItem(_geneticsMenu, geneticsItem);
            BuildGeneticsMenu();

            _faceMenu = new UIMenu("ROSTO", "~b~Formato facial");
            _menuPool.Add(_faceMenu);
            var faceItem = new UIMenuItem("Formato do Rosto");
            _mainMenu.AddItem(faceItem);
            _mainMenu.BindMenuToItem(_faceMenu, faceItem);
            BuildFaceMenu();

            _hairMenu = new UIMenu("CABELO", "~b~Estilo e cor");
            _menuPool.Add(_hairMenu);
            var hairItem = new UIMenuItem("Cabelo");
            _mainMenu.AddItem(hairItem);
            _mainMenu.BindMenuToItem(_hairMenu, hairItem);
            BuildHairMenu();

            _facialHairMenu = new UIMenu("BARBA", "~b~Pelos faciais");
            _menuPool.Add(_facialHairMenu);
            var facialHairItem = new UIMenuItem("Barba");
            _mainMenu.AddItem(facialHairItem);
            _mainMenu.BindMenuToItem(_facialHairMenu, facialHairItem);
            BuildFacialHairMenu();

            _clothingMenu = new UIMenu("ROUPAS", "~b~Vestuário");
            _menuPool.Add(_clothingMenu);
            var clothingItem = new UIMenuItem("Roupas");
            _mainMenu.AddItem(clothingItem);
            _mainMenu.BindMenuToItem(_clothingMenu, clothingItem);
            BuildClothingMenu();

            _accessoriesMenu = new UIMenu("ACESSÓRIOS", "~b~Props");
            _menuPool.Add(_accessoriesMenu);
            var accessoriesItem = new UIMenuItem("Acessórios");
            _mainMenu.AddItem(accessoriesItem);
            _mainMenu.BindMenuToItem(_accessoriesMenu, accessoriesItem);
            BuildAccessoriesMenu();

            AddSeparator(_mainMenu);

            _savedCharactersMenu = new UIMenu("PERSONAGENS SALVOS", "~b~Editar");
            _menuPool.Add(_savedCharactersMenu);
            var savedItem = new UIMenuItem("~y~Personagens Salvos");
            _mainMenu.AddItem(savedItem);
            _mainMenu.BindMenuToItem(_savedCharactersMenu, savedItem);
            BuildSavedCharactersMenu();

            AddSeparator(_mainMenu);

            var randomizeItem = new UIMenuItem("~b~Randomizar");
            _mainMenu.AddItem(randomizeItem);
            randomizeItem.Activated += (sender, item) =>
            {
                if (_isCreatorActive)
                {
                    _system.Manager.Randomize();
                    global::GTA.UI.Notification.Show("~b~Randomizado!");
                }
            };

            var resetItem = new UIMenuItem("~o~Resetar");
            _mainMenu.AddItem(resetItem);
            resetItem.Activated += (sender, item) =>
            {
                if (_isCreatorActive)
                {
                    _system.Manager.Builder.ApplyDefaultAppearance();
                    global::GTA.UI.Notification.Show("~o~Resetado!");
                }
            };

            AddSeparator(_mainMenu);

            var saveItem = new UIMenuItem("~g~Salvar Personagem");
            _mainMenu.AddItem(saveItem);
            saveItem.Activated += (sender, item) =>
            {
                if (_isCreatorActive) StartSaveProcess();
            };

            var exitItem = new UIMenuItem("~r~Sair sem Salvar");
            _mainMenu.AddItem(exitItem);
            exitItem.Activated += (sender, item) => ExitWithoutSaving();

            _mainMenu.RefreshIndex();

            _loadCharacterMenu = new UIMenu("CARREGAR PERSONAGEM", "~b~Escolha");
            _menuPool.Add(_loadCharacterMenu);
            BuildLoadCharacterMenu();
        }

        private void BuildLoadCharacterMenu()
        {
            _loadCharacterMenu.Clear();

            var slots = _system.SlotManager.GetAllSlots();
            int count = 0;

            for (int i = 0; i < MAX_CHARACTERS; i++)
            {
                int slotIndex = i;
                CharacterData character = (i < slots.Length) ? slots[i] : null;

                string label;
                string description;

                if (character != null)
                {
                    label = "~g~" + (i + 1) + ". " + character.Name;
                    string genderText = character.Gender == CharacterGender.Male ? "M" : "F";
                    Vector3 pos = CharacterPositionManager.GetLastPosition(character);
                    description = $"{genderText} | ({pos.X:F0}, {pos.Y:F0}, {pos.Z:F0})";
                    count++;
                }
                else
                {
                    label = "~c~" + (i + 1) + ". [Vazio]";
                    description = "Disponível";
                }

                var item = new UIMenuItem(label, description);

                if (character != null)
                {
                    item.Activated += (sender, selectedItem) => LoadCharacter(slotIndex);
                }
                else
                {
                    item.Enabled = false;
                }

                _loadCharacterMenu.AddItem(item);
            }

            AddSeparator(_loadCharacterMenu);

            var infoItem = new UIMenuItem("~b~" + count + "/" + MAX_CHARACTERS + " salvos");
            infoItem.Enabled = false;
            _loadCharacterMenu.AddItem(infoItem);

            var backItem = new UIMenuItem("~r~Voltar");
            _loadCharacterMenu.AddItem(backItem);
            backItem.Activated += (sender, item) => _loadCharacterMenu.Visible = false;

            _loadCharacterMenu.RefreshIndex();
        }

        private void RefreshLoadCharacterMenu()
        {
            _system.SlotManager.LoadAllSlots();
            BuildLoadCharacterMenu();
        }

        private void BuildSavedCharactersMenu()
        {
            _savedCharactersMenu.Clear();

            var slots = _system.SlotManager.GetAllSlots();
            int count = 0;

            for (int i = 0; i < MAX_CHARACTERS; i++)
            {
                int slotIndex = i;
                CharacterData character = (i < slots.Length) ? slots[i] : null;

                string label;
                string description;

                if (character != null)
                {
                    label = "~y~" + (i + 1) + ". " + character.Name;
                    string genderText = character.Gender == CharacterGender.Male ? "M" : "F";
                    description = genderText + " | " + character.CreatedAt.ToString("dd/MM/yyyy");
                    count++;
                }
                else
                {
                    label = "~c~" + (i + 1) + ". [Vazio]";
                    description = "Disponível";
                }

                var item = new UIMenuItem(label, description);

                if (character != null)
                {
                    item.Activated += (sender, selectedItem) => EditCharacter(slotIndex);
                }
                else
                {
                    item.Enabled = false;
                }

                _savedCharactersMenu.AddItem(item);
            }

            AddSeparator(_savedCharactersMenu);

            var infoItem = new UIMenuItem("~b~" + count + "/" + MAX_CHARACTERS + " usados");
            infoItem.Enabled = false;
            _savedCharactersMenu.AddItem(infoItem);

            var backItem = new UIMenuItem("~r~Voltar");
            _savedCharactersMenu.AddItem(backItem);
            backItem.Activated += (sender, item) =>
            {
                _savedCharactersMenu.Visible = false;
                if (_isCreatorActive) _mainMenu.Visible = true;
            };

            _savedCharactersMenu.RefreshIndex();
        }

        private void RefreshSavedCharactersMenu()
        {
            _system.SlotManager.LoadAllSlots();
            BuildSavedCharactersMenu();
        }

        private void AddSeparator(UIMenu menu)
        {
            var sep = new UIMenuItem("");
            sep.Enabled = false;
            menu.AddItem(sep);
        }

        private void ChangeGender(CharacterGender gender)
        {
            if (!_isCreatorActive) return;
            _pendingGenderChange = gender;
            _pendingGenderChangeActive = true;
            _genderChangeTimer = 0;
        }

        private void DisableMovementControls()
        {
            Game.DisableControlThisFrame(global::GTA.Control.MoveUp);
            Game.DisableControlThisFrame(global::GTA.Control.MoveDown);
            Game.DisableControlThisFrame(global::GTA.Control.Sprint);
            Game.DisableControlThisFrame(global::GTA.Control.Jump);
            Game.DisableControlThisFrame(global::GTA.Control.Enter);
            Game.DisableControlThisFrame(global::GTA.Control.Attack);
        }

        private bool AnySubmenuVisible()
        {
            return _geneticsMenu.Visible || _faceMenu.Visible || _hairMenu.Visible ||
                   _facialHairMenu.Visible || _clothingMenu.Visible || _accessoriesMenu.Visible ||
                   _savedCharactersMenu.Visible || _loadCharacterMenu.Visible;
        }

        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                if (_currentCharacter != null && _currentCharacterSlot >= 0)
                {
                    CharacterPositionManager.SaveCurrentPosition(_currentCharacter);
                    _system.SlotManager.SaveSlot(_currentCharacterSlot, _currentCharacter);
                }

                _mainMenu.Visible = false;
                Ped player = Game.Player.Character;
                if (player != null && player.Exists())
                {
                    player.IsPositionFrozen = false;
                    player.IsInvincible = false;
                }
                IdentityIntegration.Shutdown();
                CharacterCreatorEvents.ClearAllHandlers();
                _system.Shutdown();
            }
            catch { }
        }

        private void BuildGeneticsMenu()
        {
            var fatherList = new System.Collections.Generic.List<dynamic>();
            for (int i = 0; i < 46; i++) fatherList.Add("Pai " + (i + 1));
            var fatherItem = new UIMenuListItem("Pai", fatherList, 0);
            _geneticsMenu.AddItem(fatherItem);

            var motherList = new System.Collections.Generic.List<dynamic>();
            for (int i = 0; i < 46; i++) motherList.Add("Mãe " + (i + 1));
            var motherItem = new UIMenuListItem("Mãe", motherList, 0);
            _geneticsMenu.AddItem(motherItem);

            var resemblanceList = new System.Collections.Generic.List<dynamic>();
            for (int i = 0; i <= 10; i++) resemblanceList.Add((i * 10) + "%");
            var resemblanceItem = new UIMenuListItem("Semelhança", resemblanceList, 5);
            _geneticsMenu.AddItem(resemblanceItem);

            var skinList = new System.Collections.Generic.List<dynamic>();
            for (int i = 0; i <= 10; i++) skinList.Add((i * 10) + "%");
            var skinItem = new UIMenuListItem("Tom de Pele", skinList, 5);
            _geneticsMenu.AddItem(skinItem);

            _geneticsMenu.OnListChange += (menu, item, index) =>
            {
                if (!_isCreatorActive) return;

                var genetics = _system.Manager.CharacterData.Genetics;

                if (item.Text == "Pai")
                {
                    genetics.ShapeFirst = index;
                    genetics.SkinFirst = index;
                }
                else if (item.Text == "Mãe")
                {
                    genetics.ShapeSecond = index;
                    genetics.SkinSecond = index;
                }
                else if (item.Text == "Semelhança")
                {
                    genetics.ShapeMix = index / 10f;
                }
                else if (item.Text == "Tom de Pele")
                {
                    genetics.SkinMix = index / 10f;
                }

                _system.Manager.Builder.ApplyGenetics(genetics);
            };

            _geneticsMenu.RefreshIndex();
        }

        private void BuildFaceMenu()
        {
            string[] names = {
                "Largura Nariz", "Altura Nariz", "Comp. Nariz", "Ponte Nariz",
                "Ponta Nariz", "Desvio Nariz", "Alt. Sobrancelha", "Prof. Sobrancelha",
                "Alt. Maçãs", "Larg. Maçãs", "Larg. Olhos", "Aber. Olhos",
                "Esp. Lábios", "Larg. Mandíbula", "Alt. Mandíbula", "Larg. Queixo",
                "Alt. Queixo", "Comp. Queixo", "Forma Queixo", "Larg. Pescoço"
            };

            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                var list = new System.Collections.Generic.List<dynamic>();
                for (int v = -10; v <= 10; v++) list.Add(v.ToString());

                var item = new UIMenuListItem(names[i], list, 10);
                item.OnListChanged += (s, index) =>
                {
                    if (!_isCreatorActive) return;
                    float val = (index - 10) / 10f;
                    _system.Manager.CharacterData.FaceFeatures.SetFeature((FaceFeature)idx, val);
                    _system.Manager.Builder.ApplyFaceFeature((FaceFeature)idx, val);
                };
                _faceMenu.AddItem(item);
            }

            _faceMenu.RefreshIndex();
        }

        private void BuildHairMenu()
        {
            var styleList = new System.Collections.Generic.List<dynamic>();
            for (int i = 0; i < 80; i++) styleList.Add("Est. " + (i + 1));
            var styleItem = new UIMenuListItem("Estilo", styleList, 0);
            styleItem.OnListChanged += (s, i) =>
            {
                if (!_isCreatorActive) return;
                _system.Manager.CharacterData.Hair.Style = i;
                _system.Manager.Builder.ApplyHair(_system.Manager.CharacterData.Hair);
            };
            _hairMenu.AddItem(styleItem);

            var colorList = new System.Collections.Generic.List<dynamic>();
            for (int i = 0; i < 64; i++) colorList.Add("Cor " + (i + 1));
            var colorItem = new UIMenuListItem("Cor", colorList, 0);
            colorItem.OnListChanged += (s, i) =>
            {
                if (!_isCreatorActive) return;
                _system.Manager.CharacterData.Hair.PrimaryColor = i;
                _system.Manager.Builder.ApplyHair(_system.Manager.CharacterData.Hair);
            };
            _hairMenu.AddItem(colorItem);

            var highlightList = new System.Collections.Generic.List<dynamic>();
            for (int i = 0; i < 64; i++) highlightList.Add("Dest. " + (i + 1));
            var highlightItem = new UIMenuListItem("Destaque", highlightList, 0);
            highlightItem.OnListChanged += (s, i) =>
            {
                if (!_isCreatorActive) return;
                _system.Manager.CharacterData.Hair.HighlightColor = i;
                _system.Manager.Builder.ApplyHair(_system.Manager.CharacterData.Hair);
            };
            _hairMenu.AddItem(highlightItem);

            var eyeList = new System.Collections.Generic.List<dynamic>();
            for (int i = 0; i < 12; i++) eyeList.Add("Cor " + (i + 1));
            var eyeItem = new UIMenuListItem("Cor Olhos", eyeList, 0);
            eyeItem.OnListChanged += (s, i) =>
            {
                if (!_isCreatorActive) return;
                _system.Manager.CharacterData.EyeColor = i;
                _system.Manager.Builder.ApplyEyeColor(i);
            };
            _hairMenu.AddItem(eyeItem);

            _hairMenu.RefreshIndex();
        }

        private void BuildFacialHairMenu()
        {
            var styleList = new System.Collections.Generic.List<dynamic> { "Nenhum" };
            for (int i = 0; i < 29; i++) styleList.Add("Est. " + (i + 1));
            var styleItem = new UIMenuListItem("Estilo", styleList, 0);
            styleItem.OnListChanged += (s, i) =>
            {
                if (!_isCreatorActive) return;
                var ov = _system.Manager.CharacterData.Overlays.GetOverlay(OverlayType.FacialHair);
                _system.Manager.CharacterData.Overlays.SetOverlay(OverlayType.FacialHair, i - 1, ov.Opacity, ov.PrimaryColor);
                _system.Manager.Builder.ApplyOverlay(_system.Manager.CharacterData.Overlays.GetOverlay(OverlayType.FacialHair));
            };
            _facialHairMenu.AddItem(styleItem);

            var opacityList = new System.Collections.Generic.List<dynamic>();
            for (int i = 0; i <= 10; i++) opacityList.Add((i * 10) + "%");
            var opacityItem = new UIMenuListItem("Intensidade", opacityList, 10);
            opacityItem.OnListChanged += (s, i) =>
            {
                if (!_isCreatorActive) return;
                var ov = _system.Manager.CharacterData.Overlays.GetOverlay(OverlayType.FacialHair);
                _system.Manager.CharacterData.Overlays.SetOverlay(OverlayType.FacialHair, ov.Index, i / 10f, ov.PrimaryColor);
                _system.Manager.Builder.ApplyOverlay(_system.Manager.CharacterData.Overlays.GetOverlay(OverlayType.FacialHair));
            };
            _facialHairMenu.AddItem(opacityItem);

            var colorList = new System.Collections.Generic.List<dynamic>();
            for (int i = 0; i < 64; i++) colorList.Add("Cor " + (i + 1));
            var colorItem = new UIMenuListItem("Cor", colorList, 0);
            colorItem.OnListChanged += (s, i) =>
            {
                if (!_isCreatorActive) return;
                var ov = _system.Manager.CharacterData.Overlays.GetOverlay(OverlayType.FacialHair);
                _system.Manager.CharacterData.Overlays.SetOverlay(OverlayType.FacialHair, ov.Index, ov.Opacity, i);
                _system.Manager.Builder.ApplyOverlay(_system.Manager.CharacterData.Overlays.GetOverlay(OverlayType.FacialHair));
            };
            _facialHairMenu.AddItem(colorItem);

            _facialHairMenu.RefreshIndex();
        }

        private void BuildClothingMenu()
        {
            AddClothingItem("Camisa", ComponentType.Tops);
            AddClothingItem("Camiseta", ComponentType.Undershirt);
            AddClothingItem("Torso", ComponentType.Torso);
            AddClothingItem("Calças", ComponentType.Legs);
            AddClothingItem("Sapatos", ComponentType.Feet);
            AddClothingItem("Acessórios", ComponentType.Accessories);
            AddClothingItem("Máscara", ComponentType.Mask);
            AddClothingItem("Mochila", ComponentType.Bag);
            AddClothingItem("Colete", ComponentType.Armor);

            _clothingMenu.RefreshIndex();
        }

        private void AddClothingItem(string name, ComponentType comp)
        {
            var list = new System.Collections.Generic.List<dynamic>();
            for (int i = 0; i < 200; i++) list.Add("M" + i);

            var item = new UIMenuListItem(name, list, 0);
            item.OnListChanged += (s, i) =>
            {
                if (!_isCreatorActive) return;
                _system.Manager.CharacterData.Clothing.SetComponent(comp, i, 0);
                _system.Manager.Builder.ApplyComponent(comp, i, 0);
            };
            _clothingMenu.AddItem(item);
        }

        private void BuildAccessoriesMenu()
        {
            AddPropItem("Chapéu", PropType.Hat);
            AddPropItem("Óculos", PropType.Glasses);
            AddPropItem("Brinco", PropType.Ear);
            AddPropItem("Relógio", PropType.Watch);
            AddPropItem("Pulseira", PropType.Bracelet);

            AddSeparator(_accessoriesMenu);

            var clearItem = new UIMenuItem("~r~Remover Todos");
            _accessoriesMenu.AddItem(clearItem);
            clearItem.Activated += (s, i) =>
            {
                if (_isCreatorActive)
                {
                    _system.Manager.Builder.ClearAllProps();
                    global::GTA.UI.Notification.Show("~r~Removidos!");
                }
            };

            _accessoriesMenu.RefreshIndex();
        }

        private void AddPropItem(string name, PropType prop)
        {
            var list = new System.Collections.Generic.List<dynamic> { "Nenhum" };
            for (int i = 0; i < 150; i++) list.Add("M" + (i + 1));

            var item = new UIMenuListItem(name, list, 0);
            item.OnListChanged += (s, i) =>
            {
                if (!_isCreatorActive) return;
                _system.Manager.CharacterData.Props.SetProp(prop, i - 1, 0);
                _system.Manager.Builder.ApplyProp(prop, i - 1, 0);
            };
            _accessoriesMenu.AddItem(item);
        }
    }
}