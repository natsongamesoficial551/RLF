namespace RLF.Core.Economy.Transactions
{
    public enum TransactionOrigin
    {
        Unknown,

        // Renda
        Salary,
        Bonus,
        PassiveIncome,

        // Gastos
        LivingCost,
        Transport,
        Tax,

        // Crimes
        Crime,          // ✅ ADICIONADO - Crime genérico
        RobberyNPC,
        RobberyATM,
        RobberyBank,
        RobberyStore,   // ✅ ADICIONADO - Roubo de loja
        HouseTheft,

        // Penalidades
        Fine,
        Interest,
        Confiscation
    }
}
