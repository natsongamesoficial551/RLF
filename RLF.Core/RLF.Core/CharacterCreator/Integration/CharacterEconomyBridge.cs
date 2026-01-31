using RLF.Core.Economy;
using RLF.Core.Economy.Bank;
using RLF.Core.Economy.Debt;
using RLF.Core.Economy.Expenses;
using RLF.Core.Economy.Wallet;
using System;
using System.Collections.Generic;

namespace RLF.GTA.CharacterCreator.Integration
{
    /// <summary>
    /// Integra sistema de economia com Character Creator
    /// Cada personagem tem sua própria economia independente
    /// </summary>
    public class CharacterEconomyBridge
    {
        private Dictionary<string, CharacterEconomyData> _characterEconomies;
        private BankSystem _bankSystem;
        private string _activeCharacterId;

        public EconomySystem ActiveEconomy { get; private set; }
        public BankSystem BankSystem => _bankSystem;

        public CharacterEconomyBridge()
        {
            _characterEconomies = new Dictionary<string, CharacterEconomyData>();
            _bankSystem = new BankSystem();
        }

        /// <summary>
        /// Inicializa economia para um personagem
        /// </summary>
        public void InitializeCharacterEconomy(string characterId, decimal initialPocketMoney = 500m)
        {
            if (string.IsNullOrEmpty(characterId))
                return;

            if (_characterEconomies.ContainsKey(characterId))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Economia já existe para {characterId}");
                return;
            }

            // Configurações padrão
            var walletSettings = new WalletSettings
            {
                AllowNegativeBalance = true,
                MinBalanceLimit = -10000m,
                MaxBalanceLimit = 999999999m
            };

            var expenseSettings = new ExpenseSettings
            {
                DailyLivingCost = 15m,
                DailyTransportCost = 5m,
                WeeklyBasicTax = 50m,
                HasHouse = false,
                PoliceEnabled = false
            };

            var debtSettings = new DebtSettings
            {
                Enabled = true,
                DailyInterestRate = 0.02m,
                DebtThreshold = -1m,
                AutoApplyInterest = true
            };

            // Cria sistema de economia
            var economy = new EconomySystem(
                initialPocketMoney,
                walletSettings,
                expenseSettings,
                debtSettings
            );

            // Armazena dados
            var data = new CharacterEconomyData
            {
                CharacterId = characterId,
                Economy = economy,
                CreatedAt = DateTime.UtcNow,
                LastActive = DateTime.UtcNow
            };

            _characterEconomies[characterId] = data;

            // Configura banco
            _bankSystem.SetActiveCharacter(characterId);

            System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"💰 ECONOMIA CRIADA: {characterId}");
            System.Diagnostics.Debug.WriteLine($"   Bolso inicial: ${initialPocketMoney:N2}");
            System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
        }

        /// <summary>
        /// Ativa economia de um personagem
        /// </summary>
        public bool ActivateCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return false;

            // Cria se não existir
            if (!_characterEconomies.ContainsKey(characterId))
            {
                InitializeCharacterEconomy(characterId);
            }

            // Salva personagem anterior
            if (!string.IsNullOrEmpty(_activeCharacterId) && _characterEconomies.ContainsKey(_activeCharacterId))
            {
                _characterEconomies[_activeCharacterId].LastActive = DateTime.UtcNow;
            }

            // Ativa novo
            _activeCharacterId = characterId;
            ActiveEconomy = _characterEconomies[characterId].Economy;
            _characterEconomies[characterId].LastActive = DateTime.UtcNow;

            // Ativa conta bancária
            _bankSystem.SetActiveCharacter(characterId);

            System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"✅ PERSONAGEM ATIVO: {characterId}");
            System.Diagnostics.Debug.WriteLine($"   Bolso: ${ActiveEconomy.Wallet.Balance:N2}");
            System.Diagnostics.Debug.WriteLine($"   Banco: ${_bankSystem.CurrentAccount?.Balance ?? 0:N2}");
            System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

            return true;
        }

        /// <summary>
        /// Remove economia de um personagem (quando deletado)
        /// </summary>
        public void RemoveCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return;

            if (_characterEconomies.ContainsKey(characterId))
            {
                _characterEconomies.Remove(characterId);
                System.Diagnostics.Debug.WriteLine($"🗑️ Economia removida: {characterId}");
            }

            // Se era o ativo, limpa
            if (_activeCharacterId == characterId)
            {
                _activeCharacterId = null;
                ActiveEconomy = null;
            }
        }

        /// <summary>
        /// Obtém economia de um personagem
        /// </summary>
        public EconomySystem GetEconomy(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return null;

            if (!_characterEconomies.ContainsKey(characterId))
                return null;

            return _characterEconomies[characterId].Economy;
        }

        /// <summary>
        /// Salva dados de economia (para serialização)
        /// </summary>
        public Dictionary<string, CharacterEconomySaveData> SaveAll()
        {
            var saveData = new Dictionary<string, CharacterEconomySaveData>();

            foreach (var entry in _characterEconomies)
            {
                var data = new CharacterEconomySaveData
                {
                    CharacterId = entry.Key,
                    PocketMoney = entry.Value.Economy.Wallet.Balance,
                    BankMoney = _bankSystem.GetAccount(entry.Key)?.Balance ?? 0,
                    CreatedAt = entry.Value.CreatedAt,
                    LastActive = entry.Value.LastActive
                };

                saveData[entry.Key] = data;
            }

            System.Diagnostics.Debug.WriteLine($"💾 Salvando economia de {saveData.Count} personagens");
            return saveData;
        }

        /// <summary>
        /// Carrega dados salvos
        /// </summary>
        public void LoadAll(Dictionary<string, CharacterEconomySaveData> saveData)
        {
            if (saveData == null)
                return;

            var bankBalances = new Dictionary<string, decimal>();

            foreach (var entry in saveData.Values)
            {
                // Inicializa com dinheiro salvo
                InitializeCharacterEconomy(entry.CharacterId, entry.PocketMoney);

                // Guarda saldo do banco
                if (entry.BankMoney > 0)
                    bankBalances[entry.CharacterId] = entry.BankMoney;

                // Atualiza timestamps
                if (_characterEconomies.ContainsKey(entry.CharacterId))
                {
                    _characterEconomies[entry.CharacterId].CreatedAt = entry.CreatedAt;
                    _characterEconomies[entry.CharacterId].LastActive = entry.LastActive;
                }
            }

            // Carrega saldos bancários
            _bankSystem.LoadBalances(bankBalances);

            System.Diagnostics.Debug.WriteLine($"📂 Carregadas economias de {saveData.Count} personagens");
        }

        /// <summary>
        /// Reseta economia de um personagem
        /// </summary>
        public void ResetCharacter(string characterId, decimal initialMoney = 500m)
        {
            if (string.IsNullOrEmpty(characterId))
                return;

            // Remove existente
            RemoveCharacter(characterId);

            // Cria nova
            InitializeCharacterEconomy(characterId, initialMoney);

            System.Diagnostics.Debug.WriteLine($"🔄 Economia resetada: {characterId}");
        }

        /// <summary>
        /// Obtém total de dinheiro (bolso + banco)
        /// </summary>
        public decimal GetTotalMoney(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return 0;

            decimal pocket = GetEconomy(characterId)?.Wallet.Balance ?? 0;
            decimal bank = _bankSystem.GetAccount(characterId)?.Balance ?? 0;

            return pocket + bank;
        }
    }

    /// <summary>
    /// Dados de economia de um personagem em memória
    /// </summary>
    internal class CharacterEconomyData
    {
        public string CharacterId { get; set; }
        public EconomySystem Economy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActive { get; set; }
    }

    /// <summary>
    /// Dados para salvar/carregar
    /// </summary>
    [Serializable]
    public class CharacterEconomySaveData
    {
        public string CharacterId { get; set; }
        public decimal PocketMoney { get; set; }
        public decimal BankMoney { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActive { get; set; }
    }
}
