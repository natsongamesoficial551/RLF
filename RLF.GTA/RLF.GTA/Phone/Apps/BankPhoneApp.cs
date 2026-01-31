using System;
using GTA;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;
using RLF.Core.Economy;
using RLF.Core.Economy.Bank;
using RLF.Core.Economy.Transactions;

namespace RLF.GTA.Phone.Apps
{
    /// <summary>
    /// Aplicativo de banco no celular
    /// VERSÃO DEFINITIVA - Usa apenas ApplyTransaction (método oficial)
    /// </summary>
    public class BankPhoneApp
    {
        private ObjectPool _menuPool;
        private NativeMenu _mainMenu;
        private NativeMenu _depositMenu;
        private NativeMenu _withdrawMenu;

        private EconomySystem _economy;
        private BankSystem _bankSystem;

        // Valores rápidos para depósito/saque
        private readonly int[] _quickAmounts = { 50, 100, 250, 500, 1000, 2500, 5000, 10000 };

        public BankPhoneApp(ObjectPool menuPool, EconomySystem economy, BankSystem bankSystem)
        {
            _menuPool = menuPool;
            _economy = economy;
            _bankSystem = bankSystem;

            CreateMenus();
        }

        private void CreateMenus()
        {
            // Menu principal do banco
            _mainMenu = new NativeMenu("🏦 BANCO", "Serviços Bancários")
            {
                Alignment = global::GTA.UI.Alignment.Right
            };

            // Info de saldos
            var infoItem = new NativeItem("💰 Saldos", GetBalanceInfo());
            infoItem.Enabled = false;
            _mainMenu.Add(infoItem);

            _mainMenu.Add(new NativeSeparatorItem());

            // Depositar
            var depositItem = new NativeItem("📥 Depositar", "Transferir dinheiro para o banco");
            depositItem.Activated += OnDepositSelected;
            _mainMenu.Add(depositItem);

            // Sacar
            var withdrawItem = new NativeItem("📤 Sacar", "Retirar dinheiro do banco");
            withdrawItem.Activated += OnWithdrawSelected;
            _mainMenu.Add(withdrawItem);

            _mainMenu.Add(new NativeSeparatorItem());

            // Extrato (futuro)
            var statementItem = new NativeItem("📋 Extrato", "Ver histórico de transações");
            statementItem.Activated += (s, e) => Notification.Show("~y~[Banco]~w~ Em breve!");
            _mainMenu.Add(statementItem);

            // Atualizar info ao abrir menu
            _mainMenu.Opening += (s, e) => UpdateBalanceInfo(infoItem);

            _menuPool.Add(_mainMenu);

            // Menu de depósito
            CreateDepositMenu();

            // Menu de saque
            CreateWithdrawMenu();
        }

        private void CreateDepositMenu()
        {
            _depositMenu = new NativeMenu("📥 DEPOSITAR", "Quanto deseja depositar?")
            {
                Alignment = global::GTA.UI.Alignment.Right
            };

            // Valores rápidos
            foreach (int amount in _quickAmounts)
            {
                var item = new NativeItem($"${amount:N0}", $"Depositar ${amount:N0}");
                item.Activated += (s, e) => ProcessDeposit(amount);
                _depositMenu.Add(item);
            }

            _depositMenu.Add(new NativeSeparatorItem());

            // Depositar tudo
            var allItem = new NativeItem("💵 Depositar Tudo", "Depositar todo dinheiro em mãos");
            allItem.Activated += (s, e) => ProcessDeposit(_economy.Wallet.Balance);
            _depositMenu.Add(allItem);

            // Voltar
            var backItem = new NativeItem("← Voltar", "Voltar ao menu principal");
            backItem.Activated += (s, e) =>
            {
                _depositMenu.Visible = false;
                _mainMenu.Visible = true;
            };
            _depositMenu.Add(backItem);

            _menuPool.Add(_depositMenu);
        }

        private void CreateWithdrawMenu()
        {
            _withdrawMenu = new NativeMenu("📤 SACAR", "Quanto deseja sacar?")
            {
                Alignment = global::GTA.UI.Alignment.Right
            };

            // Valores rápidos
            foreach (int amount in _quickAmounts)
            {
                var item = new NativeItem($"${amount:N0}", $"Sacar ${amount:N0}");
                item.Activated += (s, e) => ProcessWithdraw(amount);
                _withdrawMenu.Add(item);
            }

            _withdrawMenu.Add(new NativeSeparatorItem());

            // Sacar tudo
            var allItem = new NativeItem("💸 Sacar Tudo", "Sacar todo saldo do banco");
            allItem.Activated += (s, e) =>
            {
                if (_bankSystem.CurrentAccount != null)
                    ProcessWithdraw(_bankSystem.CurrentAccount.Balance);
            };
            _withdrawMenu.Add(allItem);

            // Voltar
            var backItem = new NativeItem("← Voltar", "Voltar ao menu principal");
            backItem.Activated += (s, e) =>
            {
                _withdrawMenu.Visible = false;
                _mainMenu.Visible = true;
            };
            _withdrawMenu.Add(backItem);

            _menuPool.Add(_withdrawMenu);
        }

        private void OnDepositSelected(object sender, EventArgs e)
        {
            if (_economy.Wallet.Balance <= 0)
            {
                Notification.Show("~r~[Banco]~w~ Você não tem dinheiro em mãos!");
                return;
            }

            _mainMenu.Visible = false;
            _depositMenu.Visible = true;
        }

        private void OnWithdrawSelected(object sender, EventArgs e)
        {
            if (_bankSystem.CurrentAccount == null || _bankSystem.CurrentAccount.Balance <= 0)
            {
                Notification.Show("~r~[Banco]~w~ Você não tem saldo no banco!");
                return;
            }

            _mainMenu.Visible = false;
            _withdrawMenu.Visible = true;
        }

        private void ProcessDeposit(decimal amount)
        {
            if (amount <= 0)
            {
                Notification.Show("~r~[Banco]~w~ Valor inválido!");
                return;
            }

            if (_economy.Wallet.Balance < amount)
            {
                Notification.Show($"~r~[Banco]~w~ Você só tem ~g~${_economy.Wallet.Balance:N0}~w~ em mãos!");
                return;
            }

            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"📥 DEPÓSITO BANCÁRIO: ${amount:N0}");
            System.Diagnostics.Debug.WriteLine($"   Bolso ANTES: ${_economy.Wallet.Balance:N2}");
            System.Diagnostics.Debug.WriteLine($"   Banco ANTES: ${_bankSystem.CurrentAccount.Balance:N2}");

            // 🔥 PASSO 1: Remove da carteira usando transação oficial
            var withdrawTx = new EconomyTransaction(
                amount: -amount,
                type: TransactionType.Adjustment, // Adjustment não mostra popup
                legality: TransactionLegality.Legal,
                origin: TransactionOrigin.Unknown,
                description: "Depósito bancário"
            );

            bool walletSuccess = _economy.Wallet.ApplyTransaction(withdrawTx);
            if (!walletSuccess)
            {
                Notification.Show("~r~[Banco]~w~ Erro ao processar!");
                System.Diagnostics.Debug.WriteLine("   ❌ Falha ao remover da carteira");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"   ✓ Removido da carteira: ${amount:N0}");

            // 🔥 PASSO 2: Adiciona no banco
            bool bankSuccess = _bankSystem.Deposit(amount);
            if (!bankSuccess)
            {
                // ROLLBACK: Devolve para a carteira
                var refundTx = new EconomyTransaction(
                    amount: amount,
                    type: TransactionType.Adjustment,
                    legality: TransactionLegality.Legal,
                    origin: TransactionOrigin.Unknown,
                    description: "Estorno de depósito"
                );
                _economy.Wallet.ApplyTransaction(refundTx);

                Notification.Show("~r~[Banco]~w~ Erro ao depositar!");
                System.Diagnostics.Debug.WriteLine("   ❌ Falha no banco - ROLLBACK executado");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"   ✓ Depositado no banco: ${amount:N0}");
            System.Diagnostics.Debug.WriteLine($"   Bolso DEPOIS: ${_economy.Wallet.Balance:N2}");
            System.Diagnostics.Debug.WriteLine($"   Banco DEPOIS: ${_bankSystem.CurrentAccount.Balance:N2}");
            System.Diagnostics.Debug.WriteLine("   ✅ DEPÓSITO COMPLETO!");
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");

            Notification.Show($"~g~[Banco]~w~ Depositado ~g~${amount:N0}");
            Notification.Show($"💰 Bolso: ~g~${_economy.Wallet.Balance:N0}~w~ | 🏦 Banco: ~b~${_bankSystem.CurrentAccount.Balance:N0}");

            _depositMenu.Visible = false;
            _mainMenu.Visible = true;
        }

        private void ProcessWithdraw(decimal amount)
        {
            if (amount <= 0)
            {
                Notification.Show("~r~[Banco]~w~ Valor inválido!");
                return;
            }

            if (_bankSystem.CurrentAccount == null || _bankSystem.CurrentAccount.Balance < amount)
            {
                decimal available = _bankSystem.CurrentAccount?.Balance ?? 0;
                Notification.Show($"~r~[Banco]~w~ Saldo insuficiente! Você tem ~b~${available:N0}");
                return;
            }

            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"📤 SAQUE BANCÁRIO: ${amount:N0}");
            System.Diagnostics.Debug.WriteLine($"   Bolso ANTES: ${_economy.Wallet.Balance:N2}");
            System.Diagnostics.Debug.WriteLine($"   Banco ANTES: ${_bankSystem.CurrentAccount.Balance:N2}");

            // 🔥 PASSO 1: Remove do banco
            bool bankSuccess = _bankSystem.Withdraw(amount);
            if (!bankSuccess)
            {
                Notification.Show("~r~[Banco]~w~ Erro ao sacar!");
                System.Diagnostics.Debug.WriteLine("   ❌ Falha ao sacar do banco");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"   ✓ Sacado do banco: ${amount:N0}");

            // 🔥 PASSO 2: Adiciona na carteira usando transação ADJUSTMENT (sem popup)
            // Usamos Adjustment para que o HUD não mostre popup
            var depositTx = new EconomyTransaction(
                amount: amount,
                type: TransactionType.Adjustment, // MUDADO: era Income, agora é Adjustment
                legality: TransactionLegality.Legal,
                origin: TransactionOrigin.Unknown,
                description: "Saque bancário"
            );

            bool walletSuccess = _economy.Wallet.ApplyTransaction(depositTx);
            if (!walletSuccess)
            {
                // ROLLBACK: Devolve para o banco
                _bankSystem.Deposit(amount);

                Notification.Show("~r~[Banco]~w~ Erro ao processar!");
                System.Diagnostics.Debug.WriteLine("   ❌ Falha ao adicionar na carteira - ROLLBACK executado");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"   ✓ Adicionado na carteira: ${amount:N0}");
            System.Diagnostics.Debug.WriteLine($"   Bolso DEPOIS: ${_economy.Wallet.Balance:N2}");
            System.Diagnostics.Debug.WriteLine($"   Banco DEPOIS: ${_bankSystem.CurrentAccount.Balance:N2}");
            System.Diagnostics.Debug.WriteLine("   ✅ SAQUE COMPLETO!");
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");

            Notification.Show($"~g~[Banco]~w~ Sacado ~g~${amount:N0}");
            Notification.Show($"💰 Bolso: ~g~${_economy.Wallet.Balance:N0}~w~ | 🏦 Banco: ~b~${_bankSystem.CurrentAccount.Balance:N0}");

            _withdrawMenu.Visible = false;
            _mainMenu.Visible = true;
        }

        private string GetBalanceInfo()
        {
            decimal pocket = _economy.Wallet.Balance;
            decimal bank = _bankSystem.CurrentAccount?.Balance ?? 0;
            decimal total = pocket + bank;

            return $"Bolso: ${pocket:N0} | Banco: ${bank:N0} | Total: ${total:N0}";
        }

        private void UpdateBalanceInfo(NativeItem item)
        {
            if (item != null)
            {
                item.AltTitle = GetBalanceInfo();
            }
        }

        public void Show()
        {
            _mainMenu.Visible = true;
        }

        public void Hide()
        {
            _mainMenu.Visible = false;
            _depositMenu.Visible = false;
            _withdrawMenu.Visible = false;
        }

        public bool IsVisible()
        {
            return _mainMenu.Visible || _depositMenu.Visible || _withdrawMenu.Visible;
        }
    }
}