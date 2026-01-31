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
        Crime,          // Crime genérico
        RobberyNPC,
        RobberyATM,
        RobberyBank,
        RobberyStore,   // Roubo de loja
        HouseTheft,

        // Gangues
        GangMission,        // Missões de gangue
        GangTerritory,      // Renda de territórios
        GangActivity,       // Atividades de gangue
        GangRecruitment,    // Recrutamento

        // Penalidades
        Fine,
        Interest,
        Confiscation
    }
}