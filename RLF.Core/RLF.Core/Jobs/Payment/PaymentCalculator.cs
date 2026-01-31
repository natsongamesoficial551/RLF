namespace RLF.Core.Jobs.Payment
{
    public static class PaymentCalculator
    {
        public static decimal CalculateShiftPayment(
            int tasksCompleted,
            PaymentSettings settings)
        {
            decimal taskPay = tasksCompleted * settings.BasePayPerTask;
            decimal total = taskPay;

            if (settings.EnableBonuses)
            {
                total += settings.ShiftCompletionBonus;
            }

            return total;
        }
    }
}