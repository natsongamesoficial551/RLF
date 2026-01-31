using System;

namespace RLF.Core.Scheduling
{
    /// <summary>
    /// Representa uma tarefa agendada no scheduler.
    /// GTA-SAFE: Imutável após criação, sem alocações no tick.
    /// </summary>
    public sealed class ScheduledTask
    {
        public string Name { get; }
        public TaskPriority Priority { get; }
        public int Interval { get; }

        private readonly Action _action;
        private readonly ISchedulable _schedulable;
        private readonly Func<bool> _condition;

        private int _tickCounter;
        private bool _isEnabled;
        private long _executionCount;
        private double _lastExecutionMs;

        public bool IsEnabled => _isEnabled;
        public long ExecutionCount => _executionCount;
        public double LastExecutionMs => _lastExecutionMs;
        public int TicksUntilNextRun => Interval - (_tickCounter % Interval);

        /// <summary>
        /// Cria uma tarefa a partir de uma Action.
        /// </summary>
        public ScheduledTask(
            string name,
            Action action,
            TaskPriority priority = TaskPriority.Normal,
            int interval = 1,
            Func<bool> condition = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _action = action ?? throw new ArgumentNullException(nameof(action));
            Priority = priority;
            Interval = Math.Max(1, interval);
            _condition = condition;

            _schedulable = null;
            _isEnabled = true;
            _tickCounter = 0;
            _executionCount = 0;
        }

        /// <summary>
        /// Cria uma tarefa a partir de um ISchedulable.
        /// </summary>
        public ScheduledTask(ISchedulable schedulable)
        {
            if (schedulable == null)
                throw new ArgumentNullException(nameof(schedulable));

            _schedulable = schedulable;
            Name = schedulable.ScheduleName;
            Priority = schedulable.Priority;
            Interval = Math.Max(1, schedulable.TickInterval);

            _action = null;
            _condition = null;
            _isEnabled = true;
            _tickCounter = 0;
            _executionCount = 0;
        }

        /// <summary>
        /// Verifica se a tarefa deve executar neste tick.
        /// </summary>
        public bool ShouldExecute()
        {
            if (!_isEnabled)
                return false;

            // Se tem schedulable, verifica IsActive
            if (_schedulable != null && !_schedulable.IsActive)
                return false;

            // Se tem condição, verifica
            if (_condition != null && !_condition())
                return false;

            // Verifica intervalo
            return (_tickCounter % Interval) == 0;
        }

        /// <summary>
        /// Executa a tarefa.
        /// </summary>
        public void Execute()
        {
            if (_schedulable != null)
            {
                _schedulable.ExecuteScheduled();
            }
            else
            {
                _action?.Invoke();
            }

            _executionCount++;
        }

        /// <summary>
        /// Avança o contador de ticks.
        /// </summary>
        public void AdvanceTick()
        {
            _tickCounter++;
        }

        /// <summary>
        /// Registra o tempo da última execução.
        /// </summary>
        public void RecordExecutionTime(double ms)
        {
            _lastExecutionMs = ms;
        }

        /// <summary>
        /// Ativa/desativa a tarefa.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }

        /// <summary>
        /// Reseta o contador de ticks.
        /// </summary>
        public void Reset()
        {
            _tickCounter = 0;
            _executionCount = 0;
            _lastExecutionMs = 0;
        }
    }
}