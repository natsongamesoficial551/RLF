using System;
using System.IO;
using GTA;
using GTA.UI;
using RLF.GTA.CharacterCreator;
using RLF.GTA.CharacterCreator.Integration;
using RLF.GTA.CharacterCreator.Storage;
using RLF.GTA.CoreIntegration.UI;
using RLF.GTA.Phone;
using RLF.Core.CharacterCreator.Data;

namespace RLF.GTA.Integration
{
    /// <summary>
    /// Sistema principal que integra Character Creator com Sistema de Economia
    /// Gerencia 25 slots de personagens, cada um com economia independente
    /// </summary>
    public class IntegratedCharacterEconomySystem : Script
    {
        // Sistemas principais
        private CharacterCreatorSystem _characterSystem;
        private CharacterEconomyBridge _economyBridge;
        private CharacterEconomyStorage _economyStorage;

        // UI
        private MultiCharacterEconomyHUD _economyHUD;
        private PhoneMenuSimulator _phoneMenu;

        // Estado
        private string _currentCharacterId;
        private int _currentSlotIndex;
        private bool _isInitialized;

        // Configuração
        private string _basePath;
        private const int MAX_SLOTS = 25;

        public IntegratedCharacterEconomySystem()
        {
            Tick += OnTick;
            Aborted += OnAborted;

            Initialize();
        }

        private void Initialize()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("🎮 INTEGRATED CHARACTER-ECONOMY SYSTEM");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                // Caminhos
                _basePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "scripts", "RLF", "CharacterData"
                );

                // Inicializa Character Creator
                CharacterCreatorSystem.Instance.Initialize();
                _characterSystem = CharacterCreatorSystem.Instance;

                // Inicializa ponte de economia
                _economyBridge = new CharacterEconomyBridge();

                // Inicializa storage de economia
                _economyStorage = new CharacterEconomyStorage(_basePath);

                // Carrega economias salvas
                LoadAllCharacterEconomies();

                // Carrega personagem mais recente
                var (character, slotIndex) = _characterSystem.SlotManager.GetMostRecentCharacterWithSlot();

                if (character != null)
                {
                    LoadCharacterWithEconomy(character, slotIndex);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ℹ️ Nenhum personagem encontrado");
                    Notification.Show("~y~[Sistema]~w~ Nenhum personagem. Crie um novo!");
                }

                _isInitialized = true;

                System.Diagnostics.Debug.WriteLine("✅ Sistema integrado inicializado");
                Notification.Show("~g~[Sistema]~w~ Inicializado!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERRO na inicialização: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                Notification.Show("~r~[Sistema]~w~ ERRO ao inicializar!");
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_isInitialized)
                return;

            try
            {
                // Atualiza HUD
                if (_economyHUD != null)
                {
                    _economyHUD.Draw();
                }

                // Bloqueia dinheiro nativo constantemente
                BlockNativeMoney();

                // Comandos de debug (F8 = mostrar saldos)
                if (Game.IsKeyPressed(System.Windows.Forms.Keys.F8))
                {
                    _economyHUD?.ShowFullBalance();
                    Wait(200);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro no Tick: {ex.Message}");
            }
        }

        /// <summary>
        /// Carrega personagem com sua economia
        /// </summary>
        public void LoadCharacterWithEconomy(CharacterData character, int slotIndex)
        {
            if (character == null)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"👤 CARREGANDO: {character.Name}");
                System.Diagnostics.Debug.WriteLine($"   Slot: {slotIndex}");
                System.Diagnostics.Debug.WriteLine($"   ID: {character.Id}");

                // Salva personagem anterior se existir
                SaveCurrentCharacterEconomy();

                // Carrega economia do personagem
                var economyData = _economyStorage.LoadCharacterEconomy(character.Id);

                // Ativa economia
                _economyBridge.ActivateCharacter(character.Id);

                // Define saldo inicial no wallet usando transação de ajuste
                if (economyData != null && _economyBridge.ActiveEconomy != null)
                {
                    // Calcula diferença para ajustar
                    decimal currentBalance = _economyBridge.ActiveEconomy.Wallet.Balance;
                    decimal targetBalance = economyData.PocketMoney;
                    decimal adjustment = targetBalance - currentBalance;

                    if (adjustment != 0)
                    {
                        var adjustTx = new RLF.Core.Economy.Transactions.EconomyTransaction(
                            amount: adjustment,
                            type: RLF.Core.Economy.Transactions.TransactionType.Adjustment,
                            legality: RLF.Core.Economy.Transactions.TransactionLegality.Legal,
                            origin: RLF.Core.Economy.Transactions.TransactionOrigin.Unknown,
                            description: "Carregamento de personagem"
                        );
                        _economyBridge.ActiveEconomy.ApplyTransaction(adjustTx);
                    }
                }

                // Atualiza estado
                _currentCharacterId = character.Id;
                _currentSlotIndex = slotIndex;

                // Inicializa/atualiza HUD
                if (_economyHUD == null)
                {
                    _economyHUD = new MultiCharacterEconomyHUD(
                        _economyBridge.ActiveEconomy,
                        _economyBridge.BankSystem
                    );
                }

                _economyHUD.SetActiveCharacter(character.Id, _economyBridge.ActiveEconomy.Wallet.Balance);

                // Inicializa/atualiza celular
                if (_phoneMenu == null)
                {
                    _phoneMenu = new PhoneMenuSimulator(
                        _economyBridge.ActiveEconomy,
                        _economyBridge.BankSystem
                    );
                }

                // Aplica personagem ao player
                _characterSystem.Manager.LoadCharacterToPlayer(character);

                // Bloqueia dinheiro nativo
                BlockNativeMoney();

                System.Diagnostics.Debug.WriteLine("✅ Personagem carregado com economia!");
                System.Diagnostics.Debug.WriteLine($"   Bolso: ${_economyBridge.ActiveEconomy.Wallet.Balance:N2}");
                System.Diagnostics.Debug.WriteLine($"   Banco: ${_economyBridge.BankSystem.CurrentAccount?.Balance ?? 0:N2}");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                Notification.Show($"~g~[OK]~w~ {character.Name} carregado!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao carregar personagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                Notification.Show("~r~[ERRO]~w~ Falha ao carregar personagem!");
            }
        }

        /// <summary>
        /// Cria novo personagem com economia inicial
        /// </summary>
        public void CreateNewCharacterWithEconomy(CharacterData character, int slotIndex)
        {
            if (character == null)
                return;

            try
            {
                // Salva personagem atual
                SaveCurrentCharacterEconomy();

                // Cria economia inicial
                _economyBridge.InitializeCharacterEconomy(character.Id, 500m);

                // Salva economia
                var saveData = new CharacterEconomySaveData
                {
                    CharacterId = character.Id,
                    PocketMoney = 500m,
                    BankMoney = 0m,
                    CreatedAt = DateTime.UtcNow,
                    LastActive = DateTime.UtcNow
                };

                _economyStorage.SaveCharacterEconomy(character.Id, saveData);

                // Carrega o personagem
                LoadCharacterWithEconomy(character, slotIndex);

                Notification.Show("~g~[Novo Personagem]~w~ Economia criada!");
                Notification.Show($"💰 Você começa com ~g~$500");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao criar personagem: {ex.Message}");
                Notification.Show("~r~[ERRO]~w~ Falha ao criar personagem!");
            }
        }

        /// <summary>
        /// Deleta personagem e sua economia
        /// </summary>
        public void DeleteCharacterWithEconomy(string characterId, int slotIndex)
        {
            if (string.IsNullOrEmpty(characterId))
                return;

            try
            {
                // Deleta economia
                _economyStorage.DeleteCharacterEconomy(characterId);
                _economyBridge.RemoveCharacter(characterId);

                // Deleta personagem
                _characterSystem.SlotManager.DeleteSlot(slotIndex);

                System.Diagnostics.Debug.WriteLine($"🗑️ Personagem e economia deletados: {characterId}");
                Notification.Show("~r~[Deletado]~w~ Personagem e dados removidos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao deletar: {ex.Message}");
                Notification.Show("~r~[ERRO]~w~ Falha ao deletar!");
            }
        }

        /// <summary>
        /// Salva economia do personagem atual
        /// </summary>
        private void SaveCurrentCharacterEconomy()
        {
            if (string.IsNullOrEmpty(_currentCharacterId))
                return;

            try
            {
                var economy = _economyBridge.GetEconomy(_currentCharacterId);
                if (economy == null)
                    return;

                var saveData = new CharacterEconomySaveData
                {
                    CharacterId = _currentCharacterId,
                    PocketMoney = economy.Wallet.Balance,
                    BankMoney = _economyBridge.BankSystem.GetAccount(_currentCharacterId)?.Balance ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    LastActive = DateTime.UtcNow
                };

                _economyStorage.SaveCharacterEconomy(_currentCharacterId, saveData);

                System.Diagnostics.Debug.WriteLine($"💾 Economia salva: {_currentCharacterId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao salvar economia: {ex.Message}");
            }
        }

        /// <summary>
        /// Carrega economias de todos os personagens salvos
        /// </summary>
        private void LoadAllCharacterEconomies()
        {
            try
            {
                for (int i = 0; i < MAX_SLOTS; i++)
                {
                    var character = _characterSystem.SlotManager.GetSlot(i);
                    if (character != null)
                    {
                        var economyData = _economyStorage.LoadCharacterEconomy(character.Id);

                        if (economyData != null)
                        {
                            _economyBridge.InitializeCharacterEconomy(character.Id, economyData.PocketMoney);

                            // Carrega saldo do banco
                            if (economyData.BankMoney > 0)
                            {
                                var account = _economyBridge.BankSystem.GetAccount(character.Id);
                                if (account != null)
                                    account.Balance = economyData.BankMoney;
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("📂 Economias de todos personagens carregadas");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao carregar economias: {ex.Message}");
            }
        }

        /// <summary>
        /// Bloqueia dinheiro nativo do GTA
        /// </summary>
        private void BlockNativeMoney()
        {
            try
            {
                Game.Player.Money = 0;
            }
            catch { }
        }

        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Salvando antes de fechar...");

                // Salva personagem atual
                SaveCurrentCharacterEconomy();

                // Salva personagem no slot
                if (!string.IsNullOrEmpty(_currentCharacterId) && _currentSlotIndex >= 0)
                {
                    var character = _characterSystem.SlotManager.GetSlot(_currentSlotIndex);
                    if (character != null)
                    {
                        _characterSystem.SlotManager.SaveSlot(_currentSlotIndex, character);
                    }
                }

                System.Diagnostics.Debug.WriteLine("✅ Dados salvos!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao salvar: {ex.Message}");
            }
        }

        /// <summary>
        /// API pública para troca de personagem
        /// </summary>
        public void SwitchCharacter(int slotIndex)
        {
            var character = _characterSystem.SlotManager.GetSlot(slotIndex);
            if (character != null)
            {
                LoadCharacterWithEconomy(character, slotIndex);
            }
            else
            {
                Notification.Show($"~r~[Erro]~w~ Slot {slotIndex} vazio!");
            }
        }
    }
}