namespace RLF.Core.Economy.Transactions
{
    public enum TransactionType
    {
        Income,        // dinheiro ganho
        Expense,       // dinheiro gasto
        Fine,          // multa
        DebtInterest,  // juros
        Confiscation,  // dinheiro apreendido
        Adjustment     // correção manual / sistema
    }
}