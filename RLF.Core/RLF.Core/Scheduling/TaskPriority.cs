namespace RLF.Core.Scheduling
{
    /// <summary>
    /// Prioridade de execução de tarefas.
    /// Tarefas de maior prioridade executam primeiro e têm garantia de budget.
    /// </summary>
    public enum TaskPriority
    {
        /// <summary>
        /// Crítico: Sempre executa, ignora budget (ex: input, UI essencial).
        /// </summary>
        Critical = 0,

        /// <summary>
        /// Alta: Executa primeiro dentro do budget (ex: física, colisão).
        /// </summary>
        High = 1,

        /// <summary>
        /// Normal: Execução padrão (ex: sistemas de gameplay).
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Baixa: Executa se sobrar budget (ex: efeitos visuais secundários).
        /// </summary>
        Low = 3,

        /// <summary>
        /// Background: Pode ser adiada para próximos ticks (ex: cleanup, telemetria).
        /// </summary>
        Background = 4
    }
}