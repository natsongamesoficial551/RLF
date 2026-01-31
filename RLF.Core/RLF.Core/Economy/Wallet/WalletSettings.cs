namespace RLF.Core.Economy.Wallet
{
    public class WalletSettings
    {
        /// <summary>
        /// Permite saldo negativo (dívida).
        /// </summary>
        public bool AllowNegativeBalance { get; set; } = true;

        /// <summary>
        /// Limite máximo de dívida (ex: -5000).
        /// </summary>
        public decimal MinBalanceLimit { get; set; } = -10000m;

        /// <summary>
        /// Limite máximo positivo (opcional).
        /// </summary>
        public decimal MaxBalanceLimit { get; set; } = decimal.MaxValue;
    }
}
