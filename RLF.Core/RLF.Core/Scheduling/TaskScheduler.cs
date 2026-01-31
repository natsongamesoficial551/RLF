using System;
using System.Collections.Generic;
using System.Diagnostics;
using RLF.Core.Logging;
using RLF.Core.Performance;

namespace RLF.Core.Scheduling
{
    /// <summary>
    /// Scheduler unificado com prioridades e budget de tempo por tick.
    /// GTA-SAFE: Execução previsível, throttling automático, zero alocações no hot path.
    /// </summary>
    public sealed class TaskScheduler
    {
        private readonly List<ScheduledTask>[] _tasksByPriority;
        private readonly Dictionary<string, ScheduledTask> _taskLookup;
        private readonly Stopwatch _budgetWatch;
        private readonly Stopwatch _taskWatch;
        private readonly Logger _logger;

        private double _tickBudgetMs;
        private bool _isEnabled;
        private int _currentTick;

        // Métricas
        private int _tasksExecutedThisTick;
        private int _tasksSkippedThisTick;
        private double _timeUsedThisTick;

        // Estatísticas públicas
        public int CurrentTick => _currentTick;
        public int TasksExecutedLastTick => _tasksExecutedThisTick;
        public int TasksSkippedLastTick => _tasksSkippedThisTick;
        public double TimeUsedLastTickMs => _timeUsedThisTick;
        public double TickBudgetMs => _tickBudgetMs;
        public bool IsEnabled => _isEnabled;
        public int TotalTasks => _taskLookup.Count;

        /// <summary>
        /// Cria o scheduler com budget de tempo por tick.
        /// </summary>
        /// <param name="logger">Logger para debug</param>
        /// <param name="tickBudgetMs">Orçamento máximo por tick em ms (padrão: 12ms)</param>
        public TaskScheduler(Logger logger, double tickBudgetMs = 12.0)
        {
            _logger = logger;
            _tickBudgetMs = Math.Max(1.0, tickBudgetMs);

            // Inicializa listas por prioridade
            int priorityCount = Enum.GetValues(typeof(TaskPriority)).Length;
            _tasksByPriority = new List<ScheduledTask>[priorityCount];

            for (int i = 0; i < priorityCount; i++)
            {
                _tasksByPriority[i] = new List<ScheduledTask>();
            }

            _taskLookup = new Dictionary<string, ScheduledTask>(StringComparer.Ordinal);
            _budgetWatch = new Stopwatch();
            _taskWatch = new Stopwatch();

            _isEnabled = true;
            _currentTick = 0;
        }

        /// <summary>
        /// Registra uma tarefa no scheduler.
        /// </summary>
        public bool Register(ScheduledTask task)
        {
            if (task == null || string.IsNullOrEmpty(task.Name))
                return false;

            if (_taskLookup.ContainsKey(task.Name))
            {
                _logger?.Warning($"[Scheduler] Tarefa duplicada ignorada: {task.Name}");
                return false;
            }

            _taskLookup[task.Name] = task;
            _tasksByPriority[(int)task.Priority].Add(task);

            _logger?.Debug($"[Scheduler] Tarefa registrada: {task.Name} (Priority={task.Priority}, Interval={task.Interval})");
            return true;
        }

        /// <summary>
        /// Registra uma tarefa simples com Action.
        /// </summary>
        public bool Register(
            string name,
            Action action,
            TaskPriority priority = TaskPriority.Normal,
            int interval = 1)
        {
            var task = new ScheduledTask(name, action, priority, interval);
            return Register(task);
        }

        /// <summary>
        /// Registra um ISchedulable.
        /// </summary>
        public bool Register(ISchedulable schedulable)
        {
            if (schedulable == null)
                return false;

            var task = new ScheduledTask(schedulable);
            return Register(task);
        }

        /// <summary>
        /// Remove uma tarefa do scheduler.
        /// </summary>
        public bool Unregister(string taskName)
        {
            if (!_taskLookup.TryGetValue(taskName, out var task))
                return false;

            _taskLookup.Remove(taskName);
            _tasksByPriority[(int)task.Priority].Remove(task);

            _logger?.Debug($"[Scheduler] Tarefa removida: {taskName}");
            return true;
        }

        /// <summary>
        /// Obtém uma tarefa pelo nome.
        /// </summary>
        public ScheduledTask GetTask(string name)
        {
            _taskLookup.TryGetValue(name, out var task);
            return task;
        }

        /// <summary>
        /// Executa todas as tarefas elegíveis respeitando o budget.
        /// Chamado uma vez por tick do GTA.
        /// </summary>
        public void Tick(TickProfiler profiler = null)
        {
            if (!_isEnabled)
                return;

            _currentTick++;
            _tasksExecutedThisTick = 0;
            _tasksSkippedThisTick = 0;

            _budgetWatch.Restart();

            // Executa por ordem de prioridade
            for (int priority = 0; priority < _tasksByPriority.Length; priority++)
            {
                var tasks = _tasksByPriority[priority];
                bool isCritical = (priority == (int)TaskPriority.Critical);

                for (int i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];
                    task.AdvanceTick();

                    if (!task.ShouldExecute())
                        continue;

                    // Verifica budget (exceto Critical)
                    if (!isCritical && _budgetWatch.Elapsed.TotalMilliseconds >= _tickBudgetMs)
                    {
                        _tasksSkippedThisTick++;
                        continue;
                    }

                    // Executa com medição
                    _taskWatch.Restart();

                    try
                    {
                        profiler?.BeginSystem(task.Name);
                        task.Execute();
                        profiler?.EndSystem();
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error($"[Scheduler] Erro em tarefa '{task.Name}'", ex);
                        profiler?.EndSystem();
                    }

                    _taskWatch.Stop();
                    task.RecordExecutionTime(_taskWatch.Elapsed.TotalMilliseconds);
                    _tasksExecutedThisTick++;
                }
            }

            _budgetWatch.Stop();
            _timeUsedThisTick = _budgetWatch.Elapsed.TotalMilliseconds;
        }

        /// <summary>
        /// Altera o budget de tempo por tick.
        /// </summary>
        public void SetTickBudget(double budgetMs)
        {
            _tickBudgetMs = Math.Max(1.0, budgetMs);
        }

        /// <summary>
        /// Ativa/desativa o scheduler.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }

        /// <summary>
        /// Ativa/desativa uma tarefa específica.
        /// </summary>
        public bool SetTaskEnabled(string taskName, bool enabled)
        {
            if (_taskLookup.TryGetValue(taskName, out var task))
            {
                task.SetEnabled(enabled);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Limpa todas as tarefas.
        /// </summary>
        public void Clear()
        {
            _taskLookup.Clear();

            foreach (var list in _tasksByPriority)
            {
                list.Clear();
            }
        }

        /// <summary>
        /// Retorna estatísticas do scheduler.
        /// </summary>
        public string GetStats()
        {
            return $"[Scheduler] Tick={_currentTick} | " +
                   $"Tasks={TotalTasks} | " +
                   $"Executed={_tasksExecutedThisTick} | " +
                   $"Skipped={_tasksSkippedThisTick} | " +
                   $"TimeUsed={_timeUsedThisTick:F2}ms/{_tickBudgetMs:F1}ms";
        }
    }
}