namespace RLF.Core.Jobs.Payment
{
    public sealed class PaymentSettings
    {
        public decimal BasePayPerTask { get; set; }
        public decimal ShiftCompletionBonus { get; set; }
        public bool EnableBonuses { get; set; }

        public PaymentSettings()
        {
            BasePayPerTask = 25m;
            ShiftCompletionBonus = 100m;
            EnableBonuses = true;
        }
    }
}