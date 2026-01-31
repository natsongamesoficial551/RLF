namespace RLF.Core.Economy.Expenses
{
    public enum ExpenseType
    {
        LivingCost,     // custo de vida básico (ativo agora)
        Transport,      // transporte implícito (ativo agora)
        TaxBasic,       // taxa básica (ativo agora)

        HousingRent,    // moradia (futuro, flag)
        Utilities,      // contas (futuro, flag)
        LegalFees       // multas legais (futuro, flag)
    }
}
