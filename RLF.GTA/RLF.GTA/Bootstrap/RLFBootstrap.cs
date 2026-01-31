using System;
using GTA;
using RLF.Core;
using RLF.Core.Economy;
using RLF.Core.Economy.Bank;
using RLF.Core.Economy.Wallet;
using RLF.Core.Economy.Expenses;
using RLF.Core.Economy.Debt;
using RLF.Core.Economy.Events;
using RLF.Core.Economy.Transactions;
using RLF.GTA.CoreIntegration.UI;
using RLF.GTA.CoreIntegration;
using RLF.GTA.Vehicles;
using RLF.GTA.CoreIntegration.Weather;
using RLF.GTA.Phone;

namespace RLF.GTA.Bootstrap
{
    public class RLFBootstrap : Script
    {
        private EconomySystem _economy;
        private BankSystem _bankSystem;
        private MultiCharacterEconomyHUD _economyOverlay;
        private NeedsOverlay _needsOverlay;
        private WeatherIntegration _weatherIntegration;
        private PhoneMenuSimulator _phoneMenu;

        private bool _isInitialized;
        private bool _nativeMoneyTransferred = false;
        private int _blockTickCount = 0;
        private const int AGGRESSIVE_BLOCK_INTERVAL = 5;

        public RLFBootstrap()
        {
            Tick += OnTick;
            Aborted += OnAborted;

            Initialize();
        }

        private void Initialize()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("🎮 RLF BOOTSTRAP - INICIANDO");
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");

                // Detecta e remove dinheiro nativo ANTES de criar o sistema
                decimal gtaMoney = (decimal)Game.Player.Money;
                if (gtaMoney > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ DINHEIRO NATIVO DETECTADO: ${gtaMoney:N0}");
                    System.Diagnostics.Debug.WriteLine("   Removendo dinheiro do GTA...");
                    Game.Player.Money = 0;
                }

                // 🔥 Inicializa o Core (se existir)
                try
                {
                    GameBridge.Initialize();
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ GameBridge não disponível - modo standalone");
                }

                // 💰 Cria sistema de economia completo
                CreateEconomySystem();

                // 🏦 Cria sistema bancário
                _bankSystem = new BankSystem();
                _bankSystem.SetActiveCharacter("Player1");
                System.Diagnostics.Debug.WriteLine("✅ BankSystem inicializado");

                // Conecta economia ao bridge (se disponível)
                try
                {
                    EconomyBridge.Current = _economy;
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ EconomyBridge não disponível");
                }

                // 🎨 Cria overlay/HUD - APENAS UM!
                System.Diagnostics.Debug.WriteLine("📊 Criando HUD de economia...");
                _economyOverlay = new MultiCharacterEconomyHUD(_economy, _bankSystem);
                System.Diagnostics.Debug.WriteLine("✅ EconomyHUD inicializado (ÚNICO)");

                // 📱 Inicializa menu do celular
                try
                {
                    System.Diagnostics.Debug.WriteLine("📱 Inicializando PhoneMenuSimulator...");
                    _phoneMenu = new PhoneMenuSimulator(_economy, _bankSystem);
                    System.Diagnostics.Debug.WriteLine("✅ PhoneMenuSimulator inicializado!");
                    System.Diagnostics.Debug.WriteLine("   📱 Pressione N para abrir o celular");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Erro ao inicializar PhoneMenu: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                }

                try
                {
                    _needsOverlay = new NeedsOverlay();
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ NeedsOverlay não disponível");
                }

                try
                {
                    _weatherIntegration = new WeatherIntegration();
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ WeatherIntegration não disponível");
                }

                // ✅ Escuta eventos de dinheiro
                _economy.Wallet.OnMoneyChanged += OnMoneyChanged;

                _isInitialized = true;

                System.Diagnostics.Debug.WriteLine("✅ RLF Bootstrap inicializado!");
                System.Diagnostics.Debug.WriteLine($"   💰 Saldo inicial WALLET: ${_economy.Wallet.Balance:N2}");
                System.Diagnostics.Debug.WriteLine($"   🏦 Saldo inicial BANCO: ${_bankSystem.CurrentAccount.Balance:N2}");
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");

                global::GTA.UI.Notification.Show("~g~[RLF]~w~ Sistemas carregados!\nPressione ~b~N~w~ para abrir celular");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERRO ao inicializar RLFBootstrap: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Cria o sistema de economia com saldo ZERO
        /// </summary>
        private void CreateEconomySystem()
        {
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

            // 🔥 SALDO INICIAL: ZERO (não mais $500)
            decimal initialBalance = 0m;

            _economy = new EconomySystem(
                initialBalance,
                walletSettings,
                expenseSettings,
                debtSettings
            );

            System.Diagnostics.Debug.WriteLine($"💰 Sistema de Economia criado com ${initialBalance:N2}");
        }

        private void OnMoneyChanged(MoneyChangedEvent evt)
        {
            decimal delta = evt.NewBalance - evt.OldBalance;
            string deltaStr = delta > 0 ? $"+${delta:N0}" : $"-${Math.Abs(delta):N0}";

            System.Diagnostics.Debug.WriteLine($"💸 [BOOTSTRAP] Dinheiro alterado: {deltaStr} | Novo saldo: ${evt.NewBalance:N2}");

            if (evt.Transaction != null)
            {
                System.Diagnostics.Debug.WriteLine($"   Tipo: {evt.Transaction.Type} | Origem: {evt.Transaction.Origin}");
                if (!string.IsNullOrEmpty(evt.Transaction.Description))
                    System.Diagnostics.Debug.WriteLine($"   Descrição: {evt.Transaction.Description}");
            }

            BlockNativeMoneyAggressively();
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_isInitialized)
                return;

            try
            {
                _blockTickCount++;

                // 🔒 Bloqueia dinheiro nativo AGRESSIVAMENTE
                if (_blockTickCount >= AGGRESSIVE_BLOCK_INTERVAL)
                {
                    BlockNativeMoneyAggressively();
                    _blockTickCount = 0;
                }

                // 🔁 Core Tick (se disponível)
                try
                {
                    GameBridge.Tick();
                }
                catch { }

                // 💰 Atualiza HUD de economia - APENAS UM DRAW!
                if (_economyOverlay != null)
                {
                    _economyOverlay.Draw();
                }

                // 🍔 Atualiza HUD de necessidades
                _needsOverlay?.Draw();

                // 📱 Atualiza o menu do celular
                _phoneMenu?.Update();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro no Tick: {ex.Message}");
            }
        }

        private void BlockNativeMoneyAggressively()
        {
            try
            {
                int currentGtaMoney = Game.Player.Money;

                if (currentGtaMoney != 0)
                {
                    if (!_nativeMoneyTransferred && currentGtaMoney > 1000000)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ DINHEIRO NATIVO DETECTADO: ${currentGtaMoney:N0}");
                        System.Diagnostics.Debug.WriteLine("   🚫 ZERANDO...");
                        _nativeMoneyTransferred = true;
                    }

                    Game.Player.Money = 0;
                }
            }
            catch { }
        }

        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Finalizando RLF Bootstrap...");

                if (_economy?.Wallet != null)
                {
                    _economy.Wallet.OnMoneyChanged -= OnMoneyChanged;
                }

                _economyOverlay?.Dispose();
                _weatherIntegration?.Dispose();

                try
                {
                    GameBridge.Shutdown();
                }
                catch { }

                System.Diagnostics.Debug.WriteLine("✅ RLF Bootstrap finalizado!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao finalizar: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 🔧 API PÚBLICA
        // ═══════════════════════════════════════════════════════════════

        public void AddMoney(decimal amount, TransactionOrigin origin, string description = "")
        {
            if (_economy == null || amount <= 0)
                return;

            var transaction = new EconomyTransaction(
                amount: amount,
                type: TransactionType.Income,
                legality: TransactionLegality.Legal,
                origin: origin,
                description: description
            );

            _economy.ApplyTransaction(transaction);
        }

        public void RemoveMoney(decimal amount, TransactionOrigin origin, string description = "")
        {
            if (_economy == null || amount <= 0)
                return;

            var transaction = new EconomyTransaction(
                amount: -amount,
                type: TransactionType.Expense,
                legality: TransactionLegality.Legal,
                origin: origin,
                description: description
            );

            _economy.ApplyTransaction(transaction);
        }

        public void ApplyFine(decimal amount, string description = "Multa")
        {
            if (_economy == null || amount <= 0)
                return;

            var transaction = new EconomyTransaction(
                amount: -amount,
                type: TransactionType.Fine,
                legality: TransactionLegality.Legal,
                origin: TransactionOrigin.Fine,
                description: description
            );

            _economy.ApplyTransaction(transaction);
        }

        public decimal GetBalance()
        {
            return _economy?.Wallet?.Balance ?? 0m;
        }

        public bool IsInDebt()
        {
            return _economy?.Debt?.State?.InDebt ?? false;
        }

        public decimal GetDebtAmount()
        {
            return _economy?.Debt?.State?.CurrentDebt ?? 0m;
        }

        public EconomySystem GetEconomy()
        {
            return _economy;
        }

        public BankSystem GetBankSystem()
        {
            return _bankSystem;
        }
    }
}