namespace RLF.Core.Scheduling
{
    /// <summary>
    /// Interface para objetos que podem ser agendados pelo TaskScheduler.
    /// </summary>
    public interface ISchedulable
    {
        /// <summary>
        /// Nome único para identificação e debug.
        /// </summary>
        string ScheduleName { get; }

        /// <summary>
        /// Prioridade de execução.
        /// </summary>
        TaskPriority Priority { get; }

        /// <summary>
        /// Intervalo em ticks entre execuções.
        /// 1 = todo tick, 60 = ~1x por segundo a 60fps.
        /// </summary>
        int TickInterval { get; }

        /// <summary>
        /// Indica se está ativo e deve ser executado.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Executa a lógica agendada.
        /// </summary>
        void ExecuteScheduled();
    }
}