using RLF.Core.Events;
using RLF.Core.Events.EventArgs;
using RLF.Core.Logging;
using RLF.Core.Utilities;
using System;

namespace RLF.Core.Systems
{
    /// <summary>
    /// Classe base para todos os sistemas do Real Life Framework.
    /// </summary>
    public abstract class SystemBase
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

        #endregion

        #region Campos Protegidos

        protected readonly Logger Logger;
        protected readonly EventManager Events;

        #endregion

        #region Campos Privados

        private int _tickCounter;

        #endregion

        #region Construtor

        protected SystemBase(
            string name,
            Logger logger,
            EventManager eventManager,
            int tickRate = 1)
        {
            Name = string.IsNullOrWhiteSpace(name) ? GetType().Name : name;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Events = eventManager ?? throw new ArgumentNullException(nameof(eventManager));

            TickRate = Math.Max(1, tickRate);
            _tickCounter = 0;

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

                    Logger.Info($"{Name} iniciado (TickRate={TickRate})");
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

        #region Tick

        /// <summary>
        /// Tick chamado pelo SystemRegistry.
        /// </summary>
        public void Tick()
        {
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
