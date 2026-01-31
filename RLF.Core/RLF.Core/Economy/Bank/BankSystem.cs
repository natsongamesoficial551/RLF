using System;
using System.Collections.Generic;
using RLF.Core.Economy.Transactions;

namespace RLF.Core.Economy.Bank
{
    /// <summary>
    /// Sistema bancário com contas separadas por personagem
    /// </summary>
    public class BankSystem
    {
        private Dictionary<string, BankAccount> _accounts;
        private string _currentCharacterId;
        
        public BankAccount CurrentAccount
        {
            get
            {
                if (string.IsNullOrEmpty(_currentCharacterId))
                    return null;
                    
                if (!_accounts.ContainsKey(_currentCharacterId))
                    _accounts[_currentCharacterId] = new BankAccount(_currentCharacterId);
                    
                return _accounts[_currentCharacterId];
            }
        }

        public event Action<BankTransactionEvent> OnBankTransaction;
        public event Action<string, decimal, decimal> OnBalanceChanged;

        public BankSystem()
        {
            _accounts = new Dictionary<string, BankAccount>();
        }

        /// <summary>
        /// Define o personagem ativo (deve ser chamado ao trocar de personagem)
        /// </summary>
        public void SetActiveCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return;

            _currentCharacterId = characterId;

            if (!_accounts.ContainsKey(characterId))
            {
                _accounts[characterId] = new BankAccount(characterId);
                System.Diagnostics.Debug.WriteLine($"💳 Nova conta bancária criada: {characterId}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"💳 Conta bancária carregada: {characterId}");
                System.Diagnostics.Debug.WriteLine($"   Saldo: ${CurrentAccount.Balance:N2}");
            }
        }

        /// <summary>
        /// Deposita dinheiro do bolso para o banco
        /// </summary>
        public bool Deposit(decimal amount)
        {
            if (CurrentAccount == null || amount <= 0)
                return false;

            decimal oldBalance = CurrentAccount.Balance;
            CurrentAccount.Balance += amount;

            OnBankTransaction?.Invoke(new BankTransactionEvent
            {
                Type = BankTransactionType.Deposit,
                Amount = amount,
                NewBalance = CurrentAccount.Balance,
                Timestamp = DateTime.UtcNow
            });

            OnBalanceChanged?.Invoke(_currentCharacterId, oldBalance, CurrentAccount.Balance);

            System.Diagnostics.Debug.WriteLine($"💰 DEPÓSITO: ${amount:N2} | Novo saldo: ${CurrentAccount.Balance:N2}");
            return true;
        }

        /// <summary>
        /// Saca dinheiro do banco para o bolso
        /// </summary>
        public bool Withdraw(decimal amount)
        {
            if (CurrentAccount == null || amount <= 0)
                return false;

            if (CurrentAccount.Balance < amount)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Saldo insuficiente! Tentou sacar ${amount:N2}, tem ${CurrentAccount.Balance:N2}");
                return false;
            }

            decimal oldBalance = CurrentAccount.Balance;
            CurrentAccount.Balance -= amount;

            OnBankTransaction?.Invoke(new BankTransactionEvent
            {
                Type = BankTransactionType.Withdrawal,
                Amount = amount,
                NewBalance = CurrentAccount.Balance,
                Timestamp = DateTime.UtcNow
            });

            OnBalanceChanged?.Invoke(_currentCharacterId, oldBalance, CurrentAccount.Balance);

            System.Diagnostics.Debug.WriteLine($"💸 SAQUE: ${amount:N2} | Novo saldo: ${CurrentAccount.Balance:N2}");
            return true;
        }

        /// <summary>
        /// Transferência entre contas (futuro)
        /// </summary>
        public bool Transfer(string targetCharacterId, decimal amount)
        {
            if (CurrentAccount == null || amount <= 0)
                return false;

            if (CurrentAccount.Balance < amount)
                return false;

            if (!_accounts.ContainsKey(targetCharacterId))
                _accounts[targetCharacterId] = new BankAccount(targetCharacterId);

            decimal senderOldBalance = CurrentAccount.Balance;
            decimal receiverOldBalance = _accounts[targetCharacterId].Balance;

            CurrentAccount.Balance -= amount;
            _accounts[targetCharacterId].Balance += amount;

            OnBankTransaction?.Invoke(new BankTransactionEvent
            {
                Type = BankTransactionType.Transfer,
                Amount = amount,
                NewBalance = CurrentAccount.Balance,
                TargetCharacterId = targetCharacterId,
                Timestamp = DateTime.UtcNow
            });

            OnBalanceChanged?.Invoke(_currentCharacterId, senderOldBalance, CurrentAccount.Balance);
            OnBalanceChanged?.Invoke(targetCharacterId, receiverOldBalance, _accounts[targetCharacterId].Balance);

            System.Diagnostics.Debug.WriteLine($"💱 TRANSFERÊNCIA: ${amount:N2} para {targetCharacterId}");
            return true;
        }

        /// <summary>
        /// Obtém conta de um personagem específico
        /// </summary>
        public BankAccount GetAccount(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return null;

            if (!_accounts.ContainsKey(characterId))
                _accounts[characterId] = new BankAccount(characterId);

            return _accounts[characterId];
        }

        /// <summary>
        /// Salva todas as contas (para serialização)
        /// </summary>
        public Dictionary<string, decimal> GetAllBalances()
        {
            var balances = new Dictionary<string, decimal>();
            foreach (var account in _accounts)
            {
                balances[account.Key] = account.Value.Balance;
            }
            return balances;
        }

        /// <summary>
        /// Carrega saldos salvos
        /// </summary>
        public void LoadBalances(Dictionary<string, decimal> balances)
        {
            if (balances == null)
                return;

            foreach (var entry in balances)
            {
                if (!_accounts.ContainsKey(entry.Key))
                    _accounts[entry.Key] = new BankAccount(entry.Key);

                _accounts[entry.Key].Balance = entry.Value;
            }

            System.Diagnostics.Debug.WriteLine($"💾 Carregadas {balances.Count} contas bancárias");
        }

        /// <summary>
        /// Reseta conta do personagem atual
        /// </summary>
        public void ResetCurrentAccount()
        {
            if (CurrentAccount != null)
            {
                decimal oldBalance = CurrentAccount.Balance;
                CurrentAccount.Balance = 0;
                OnBalanceChanged?.Invoke(_currentCharacterId, oldBalance, 0);
                System.Diagnostics.Debug.WriteLine($"🔄 Conta resetada: {_currentCharacterId}");
            }
        }
    }

    /// <summary>
    /// Conta bancária individual
    /// </summary>
    public class BankAccount
    {
        public string CharacterId { get; private set; }
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime LastTransaction { get; set; }

        public BankAccount(string characterId)
        {
            CharacterId = characterId;
            Balance = 0;
            CreatedAt = DateTime.UtcNow;
            LastTransaction = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Evento de transação bancária
    /// </summary>
    public class BankTransactionEvent
    {
        public BankTransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public decimal NewBalance { get; set; }
        public string TargetCharacterId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Tipos de transação bancária
    /// </summary>
    public enum BankTransactionType
    {
        Deposit,      // Depósito
        Withdrawal,   // Saque
        Transfer,     // Transferência
        Interest      // Juros (futuro)
    }
}
