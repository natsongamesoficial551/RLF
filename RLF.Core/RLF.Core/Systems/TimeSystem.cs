using System;
using RLF.Core.Events;
using RLF.Core.Events.EventArgs;
using RLF.Core.Logging;

namespace RLF.Core.Systems
{
    /// <summary>
    /// Sistema de tempo do framework.
    /// Fornece DeltaTime, TotalTime e TickCount.
    /// GTA-SAFE: NÃO dispara eventos por tick.
    /// </summary>
    public sealed class TimeSystem : SystemBase
    {
        #region Campos Privados

        private DateTime _lastTickTime;
        private DateTime _startTime;

        private double _totalTime;
        private long _tickCount;
        private float _deltaTime;

        private double _secondAccumulator;
        private double _minuteAccumulator;

        private float _maxDeltaSeconds;
        private bool _isFirstTick;

        #endregion

        #region Propriedades Públicas

        /// <summary>
        /// Tempo entre o último tick e o atual (em segundos).
        /// </summary>
        public float DeltaTime => _deltaTime;

        /// <summary>
        /// Tempo total desde o início do sistema (em segundos).
        /// </summary>
        public double TotalTime => _totalTime;

        /// <summary>
        /// Quantidade total de ticks executados.
        /// </summary>
        public long TickCount => _tickCount;

        #endregion

        #region Construtor

        public TimeSystem(Logger logger, EventManager eventManager)
            : base("TimeSystem", logger, eventManager, tickRate: 1)
        {
            // Proteção contra freeze / alt-tab
            _maxDeltaSeconds = 0.25f;
        }

        #endregion

        #region Ciclo de Vida

        protected override void OnStart()
        {
            _startTime = DateTime.UtcNow;
            _lastTickTime = _startTime;

            _totalTime = 0.0;
            _tickCount = 0;
            _deltaTime = 0.0f;

            _secondAccumulator = 0.0;
            _minuteAccumulator = 0.0;
            _isFirstTick = true;

            LoadConfig();

            Logger.Info($"{Name}: iniciado (MaxDeltaSeconds={_maxDeltaSeconds})");

            Events.Raise(
                "time:started",
                new RLFEventArgs<DateTime>(_startTime)
            );
        }

        protected override void OnStop()
        {
            Logger.Info(
                $"{Name}: parado (TotalTime={_totalTime:F2}s, Ticks={_tickCount})"
            );

            Events.Raise(
                "time:stopped",
                new RLFEventArgs<TimeStopData>(new TimeStopData
                {
                    TotalTime = _totalTime,
                    TickCount = _tickCount
                })
            );

            ResetState();
        }

        protected override void OnTick()
        {
            DateTime now = DateTime.UtcNow;

            if (_isFirstTick)
            {
                _deltaTime = 0.0f;
                _isFirstTick = false;
            }
            else
            {
                double rawDelta = (now - _lastTickTime).TotalSeconds;

                if (rawDelta > _maxDeltaSeconds)
                    rawDelta = _maxDeltaSeconds;

                _deltaTime = (float)rawDelta;
            }

            _lastTickTime = now;

            _totalTime += _deltaTime;
            _tickCount++;

            // Acumuladores de tempo
            _secondAccumulator += _deltaTime;
            _minuteAccumulator += _deltaTime;

            // Evento por segundo
            if (_secondAccumulator >= 1.0)
            {
                _secondAccumulator -= 1.0;

                Events.Raise(
                    "time:second",
                    new RLFEventArgs<TimeEventData>(new TimeEventData
                    {
                        TotalTime = _totalTime,
                        TickCount = _tickCount
                    })
                );
            }

            // Evento por minuto
            if (_minuteAccumulator >= 60.0)
            {
                _minuteAccumulator -= 60.0;

                Events.Raise(
                    "time:minute",
                    new RLFEventArgs<TimeEventData>(new TimeEventData
                    {
                        TotalTime = _totalTime,
                        TickCount = _tickCount
                    })
                );
            }
        }

        #endregion

        #region Configuração

        private void LoadConfig()
        {
            try
            {
                var core = RLFCore.Instance;
                if (core?.Config == null)
                    return;

                _maxDeltaSeconds = core.Config.GetFloat(
                    "TimeSystem",
                    "MaxDeltaSeconds",
                    _maxDeltaSeconds
                );

                if (_maxDeltaSeconds <= 0.0f)
                    _maxDeltaSeconds = 0.25f;
            }
            catch (Exception ex)
            {
                Logger.Warning(
                    $"{Name}: falha ao carregar config, usando defaults",
                    ex
                );
            }
        }

        private void ResetState()
        {
            _totalTime = 0.0;
            _tickCount = 0;
            _deltaTime = 0.0f;
            _secondAccumulator = 0.0;
            _minuteAccumulator = 0.0;
            _isFirstTick = true;
        }

        #endregion
    }

    #region DTOs de Evento

    public sealed class TimeStopData
    {
        public double TotalTime { get; set; }
        public long TickCount { get; set; }
    }

    public sealed class TimeEventData
    {
        public double TotalTime { get; set; }
        public long TickCount { get; set; }
    }

    #endregion
}
