namespace RLF.Core.Economy.Debt
{
    public class DebtSettings
    {
        /// <summary>Ativa o sistema de dívida.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Taxa de juros diária (ex: 0.02 = 2%).</summary>
        public decimal DailyInterestRate { get; set; } = 0.02m;

        /// <summary>Valor mínimo para considerar que existe dívida.</summary>
        public decimal DebtThreshold { get; set; } = -1m;

        /// <summary>Aplica juros automaticamente quando chamado.</summary>
        public bool AutoApplyInterest { get; set; } = true;
    }
}
