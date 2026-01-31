using GTA;
using GTA.UI;
using RLF.Core.Economy;
using RLF.Core.Economy.Bank;
using RLF.Core.Economy.Events;
using RLF.Core.Economy.Transactions;
using System;
using System.Drawing;

namespace RLF.GTA.CoreIntegration.UI
{
    /// <summary>
    /// HUD de economia - VERSÃO DEFINITIVA
    /// - Mostra APENAS dinheiro em mãos
    /// - Ignora transações bancárias (sem popup)
    /// - UM ÚNICO HUD (não duplica)
    /// </summary>
    public class MultiCharacterEconomyHUD
    {
        private readonly EconomySystem _economy;
        private readonly BankSystem _bankSystem;

        private string _currentCharacterId;

        // Popup de transação
        private string _popupText;
        private DateTime _popupEndTime;
        private bool _showPopup;
        private Color _popupColor;

        // Layout (canto superior direito, mesmo estilo GTA)
        private readonly Point _moneySymbolPos = new Point(20, 18);
        private readonly Point _balanceTextPos = new Point(40, 20);
        private readonly Point _popupTextPos = new Point(40, 45);

        // Configurações visuais
        private readonly Color _positiveColor = Color.FromArgb(255, 80, 220, 120);
        private readonly Color _negativeColor = Color.FromArgb(255, 220, 80, 80);
        private readonly Color _symbolColor = Color.FromArgb(255, 80, 200, 120);

        // Controle de bloqueio
        private int _tickCount = 0;
        private const int BLOCK_INTERVAL = 10;

        // Flag para evitar múltiplas instâncias
        private static bool _instanceExists = false;

        public MultiCharacterEconomyHUD(EconomySystem economy, BankSystem bankSystem)
        {
            if (_instanceExists)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ AVISO: Tentativa de criar SEGUNDO HUD! Ignorando...");
                throw new InvalidOperationException("Já existe uma instância de MultiCharacterEconomyHUD!");
            }

            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _bankSystem = bankSystem ?? throw new ArgumentNullException(nameof(bankSystem));

            _currentCharacterId = "";

            // Eventos
            _economy.Wallet.OnMoneyChanged += OnMoneyChanged;

            // Bloqueia imediatamente
            BlockNativeMoneyAggressively();

            _instanceExists = true;

            System.Diagnostics.Debug.WriteLine("✅ [HUD] Inicializado - ÚNICA INSTÂNCIA");
            System.Diagnostics.Debug.WriteLine($"   Saldo inicial: ${_economy.Wallet.Balance:N0}");
        }

        public void SetActiveCharacter(string characterId, decimal initialPocketMoney)
        {
            if (string.IsNullOrEmpty(characterId))
                return;

            _currentCharacterId = characterId;

            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"💰 [HUD] PERSONAGEM ATIVO: {characterId}");
            System.Diagnostics.Debug.WriteLine($"   Bolso: ${initialPocketMoney:N2}");
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");

            BlockNativeMoneyAggressively();
        }

        private void OnMoneyChanged(MoneyChangedEvent evt)
        {
            decimal delta = evt.NewBalance - evt.OldBalance;
            if (delta == 0)
                return;

            // 🔥 IGNORA COMPLETAMENTE transações bancárias
            if (evt.Transaction != null)
            {
                // Ignora TODAS as transações tipo Adjustment
                if (evt.Transaction.Type == TransactionType.Adjustment)
                {
                    System.Diagnostics.Debug.WriteLine($"💸 [HUD] IGNORADO (Adjustment): {delta:+0;-0} → ${evt.NewBalance:N0}");
                    return;
                }
            }

            // Cria popup para outras transações (salário, multas, roubos, etc)
            if (delta > 0)
            {
                _popupText = $"+${Math.Abs(delta):N0}";
                _popupColor = _positiveColor;
                System.Diagnostics.Debug.WriteLine($"💸 [HUD] POPUP VERDE: {_popupText}");
            }
            else
            {
                _popupText = $"-${Math.Abs(delta):N0}";
                _popupColor = _negativeColor;
                System.Diagnostics.Debug.WriteLine($"💸 [HUD] POPUP VERMELHO: {_popupText}");
            }

            _popupEndTime = DateTime.UtcNow.AddSeconds(2.5);
            _showPopup = true;
        }

        public void Draw()
        {
            _tickCount++;

            // Bloqueia dinheiro nativo frequentemente
            if (_tickCount >= BLOCK_INTERVAL)
            {
                BlockNativeMoneyAggressively();
                _tickCount = 0;
            }

            // Desenha saldo em mãos
            DrawPocketMoney();

            // Desenha popup se ativo
            if (_showPopup)
            {
                DrawPopup();

                if (DateTime.UtcNow >= _popupEndTime)
                {
                    _showPopup = false;
                    System.Diagnostics.Debug.WriteLine($"💸 [HUD] Popup escondido");
                }
            }
        }

        private void DrawPocketMoney()
        {
            // 🔥 SEMPRE pega o saldo DIRETO da Wallet
            decimal pocketMoney = _economy.Wallet.Balance;

            // Símbolo $
            new TextElement(
                "$",
                _moneySymbolPos,
                0.55f,
                _symbolColor
            ).Draw();

            // Valor (APENAS bolso)
            new TextElement(
                pocketMoney.ToString("N0"),
                _balanceTextPos,
                0.45f,
                Color.White
            ).Draw();
        }

        private void DrawPopup()
        {
            new TextElement(
                _popupText,
                _popupTextPos,
                0.40f,
                Color.FromArgb(200, _popupColor.R, _popupColor.G, _popupColor.B)
            ).Draw();
        }

        private void BlockNativeMoneyAggressively()
        {
            try
            {
                int currentGtaMoney = Game.Player.Money;

                if (currentGtaMoney > 0)
                {
                    if (currentGtaMoney > 1000000)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ [HUD] DINHEIRO NATIVO: ${currentGtaMoney:N0} → ZERANDO");
                    }

                    Game.Player.Money = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [HUD] Erro ao bloquear dinheiro: {ex.Message}");
            }
        }

        public void ForceUpdate()
        {
            BlockNativeMoneyAggressively();
        }

        public decimal GetTotalMoney()
        {
            decimal pocket = _economy.Wallet.Balance;
            decimal bank = _bankSystem.CurrentAccount?.Balance ?? 0;
            return pocket + bank;
        }

        public void ShowFullBalance()
        {
            decimal pocket = _economy.Wallet.Balance;
            decimal bank = _bankSystem.CurrentAccount?.Balance ?? 0;
            decimal total = pocket + bank;

            Notification.Show(
                $"💰 Bolso: ~g~${pocket:N0}~w~\n" +
                $"🏦 Banco: ~b~${bank:N0}~w~\n" +
                $"📊 Total: ~y~${total:N0}"
            );
        }

        public void Dispose()
        {
            if (_economy != null && _economy.Wallet != null)
            {
                _economy.Wallet.OnMoneyChanged -= OnMoneyChanged;
            }

            _instanceExists = false;
            System.Diagnostics.Debug.WriteLine("🔄 [HUD] Disposed - instância liberada");
        }
    }
}