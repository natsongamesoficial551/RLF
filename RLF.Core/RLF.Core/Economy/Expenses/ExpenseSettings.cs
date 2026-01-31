namespace RLF.Core.Economy.Expenses
{
    public class ExpenseSettings
    {
        // Valores (exemplos; ajuste depois)
        public decimal DailyLivingCost { get; set; } = 15m;
        public decimal DailyTransportCost { get; set; } = 5m;
        public decimal WeeklyBasicTax { get; set; } = 50m;

        // Flags (pontes futuras)
        public bool HasHouse { get; set; } = false;
        public bool PoliceEnabled { get; set; } = false;
    }
}
