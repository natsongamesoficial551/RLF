using System;
using RLF.Core.Events;
using RLF.Core.Events.EventArgs;
using RLF.Core.Logging;
using RLF.Core.Utilities;
using RLF.Core.Scheduling;

namespace RLF.Core.Systems
{
    /// <summary>
    /// Classe base para todos os sistemas do Real Life Framework.
    /// Implementa ISchedulable para integração automática com TaskScheduler.
    /// </summary>
    public abstract class SystemBase : ISchedulable
    {
        #region Enums

        public enum SystemState
        {
            Stopped,
            Running,
            Paused,
            Failed
        }

        #endregion

        #region Propriedades Públicas

        public string Name { get; }
        public SystemState State { get; private set; }

        public bool IsRunning => State == SystemState.Running;
        public bool IsPaused => State == SystemState.Paused;

        /// <summary>
        /// Intervalo em ticks entre execuções.
        /// 1 = todo tick | 60 = ~1x por segundo (60fps)
        /// </summary>
        public int TickRate { get; protected set; }

        /// <summary>
        /// Prioridade de execução no scheduler.
        /// </summary>
        public TaskPriority SchedulePriority { get; protected set; }

        #endregion

        #region ISchedulable

        string ISchedulable.ScheduleName => Name;
        TaskPriority ISchedulable.Priority => SchedulePriority;
        int ISchedulable.TickInterval => TickRate;
        bool ISchedulable.IsActive => State == SystemState.Running;

        void ISchedulable.ExecuteScheduled()
        {
            // Executa diretamente sem verificar TickRate (scheduler já controla)
            if (State != SystemState.Running)
                return;

            bool ok = SafeExecutor.Execute(
                () => OnTick(),
                $"{Name}.Tick"
            );

            if (!ok)
            {
                State = SystemState.Failed;
                Logger.Error($"{Name} entrou em estado FAILED");
                Events.Raise("system:failed", new RLFEventArgs<string>(Name));
            }
        }

        #endregion

        #region Campos Protegidos

        protected readonly Logger Logger;
        protected readonly EventManager Events;

        #endregion

        #region Campos Privados

        private int _tickCounter;
        private bool _useScheduler;

        #endregion

        #region Construtor

        protected SystemBase(
            string name,
            Logger logger,
            EventManager eventManager,
            int tickRate = 1,
            TaskPriority priority = TaskPriority.Normal)
        {
            Name = string.IsNullOrWhiteSpace(name) ? GetType().Name : name;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Events = eventManager ?? throw new ArgumentNullException(nameof(eventManager));

            TickRate = Math.Max(1, tickRate);
            SchedulePriority = priority;
            _tickCounter = 0;
            _useScheduler = false;

            State = SystemState.Stopped;
        }

        #endregion

        #region Ciclo de Vida Público

        public bool Start()
        {
            if (State != SystemState.Stopped)
                return false;

            bool ok = SafeExecutor.Execute(
                () =>
                {
                    OnStart();
                    State = SystemState.Running;

                    Logger.Info($"{Name} iniciado (TickRate={TickRate}, Priority={SchedulePriority})");
                    Events.Raise("system:started", new RLFEventArgs<string>(Name));
                },
                $"{Name}.Start"
            );

            return ok;
        }

        public bool Stop()
        {
            if (State == SystemState.Stopped)
                return false;

            bool ok = SafeExecutor.Execute(
                () =>
                {
                    OnStop();
                    State = SystemState.Stopped;

                    Logger.Info($"{Name} parado");
                    Events.Raise("system:stopped", new RLFEventArgs<string>(Name));
                },
                $"{Name}.Stop"
            );

            return ok;
        }

        public bool Pause()
        {
            if (State != SystemState.Running)
                return false;

            State = SystemState.Paused;

            Logger.Info($"{Name} pausado");
            Events.Raise("system:paused", new RLFEventArgs<string>(Name));

            return true;
        }

        public bool Resume()
        {
            if (State != SystemState.Paused)
                return false;

            State = SystemState.Running;

            Logger.Info($"{Name} retomado");
            Events.Raise("system:resumed", new RLFEventArgs<string>(Name));

            return true;
        }

        #endregion

        #region Tick (Fallback quando Scheduler desabilitado)

        /// <summary>
        /// Indica que este sistema será gerenciado pelo scheduler.
        /// </summary>
        internal void SetUseScheduler(bool useScheduler)
        {
            _useScheduler = useScheduler;
        }

        /// <summary>
        /// Tick chamado pelo SystemRegistry (fallback se scheduler desabilitado).
        /// </summary>
        public void Tick()
        {
            // Se scheduler está gerenciando, não faz nada aqui
            if (_useScheduler)
                return;

            if (State != SystemState.Running)
                return;

            _tickCounter++;

            if (_tickCounter % TickRate != 0)
                return;

            bool ok = SafeExecutor.Execute(
                () => OnTick(),
                $"{Name}.Tick"
            );

            if (!ok)
            {
                State = SystemState.Failed;
                Logger.Error($"{Name} entrou em estado FAILED");
                Events.Raise("system:failed", new RLFEventArgs<string>(Name));
            }
        }

        #endregion

        #region Métodos Abstratos

        protected abstract void OnStart();
        protected abstract void OnStop();
        protected abstract void OnTick();

        #endregion
    }
}