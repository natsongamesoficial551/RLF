using System;
using RLF.Core.Events;
using RLF.Core.Events.EventArgs;
using RLF.Core.Logging;

namespace RLF.Core.Systems
{
    /// <summary>
    /// Sistema que monitora o ciclo de vida do Core.
    /// Publica eventos de start, heartbeat e stop.
    /// </summary>
    public sealed class CoreLifecycleSystem : SystemBase
    {
        private int _tickCount;
        private DateTime _startTime;

        public CoreLifecycleSystem(Logger logger, EventManager eventManager)
            : base("CoreLifecycleSystem", logger, eventManager)
        {
            _tickCount = 0;
        }

        protected override void OnStart()
        {
            _startTime = DateTime.Now;
            _tickCount = 0;

            Logger.Info($"{Name}: Sistema iniciado");

            Events.Raise(
                "core:lifecycle:started",
                new RLFEventArgs<DateTime>(_startTime)
            );
        }

        protected override void OnTick()
        {
            _tickCount++;

            // A cada 60 ticks (≈ 1s em 60 FPS)
            if (_tickCount % 60 == 0)
            {
                var uptime = DateTime.Now - _startTime;

                Logger.Debug(
                    $"{Name}: Tick {_tickCount} | Uptime {uptime.TotalSeconds:F1}s"
                );

                Events.Raise(
                    "core:lifecycle:heartbeat",
                    new RLFEventArgs<int>(_tickCount)
                    {
                        CustomData = uptime
                    }
                );
            }
        }

        protected override void OnStop()
        {
            var totalUptime = DateTime.Now - _startTime;

            Logger.Info(
                $"{Name}: Sistema parado | Ticks {_tickCount} | Uptime {totalUptime.TotalSeconds:F1}s"
            );

            Events.Raise(
                "core:lifecycle:stopped",
                new RLFEventArgs<int>(_tickCount)
                {
                    CustomData = totalUptime
                }
            );

            _tickCount = 0;
        }
    }
}
