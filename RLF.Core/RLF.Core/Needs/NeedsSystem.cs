using System;
using System.Collections.Generic;
using RLF.Core.Events;
using RLF.Core.Events.EventArgs;
using RLF.Core.Logging;
using RLF.Core.Systems;
using RLF.Core.Needs.Events;

namespace RLF.Core.Needs
{
    public sealed class NeedsSystem : SystemBase
    {
        private readonly NeedsSettings _settings;
        private readonly Dictionary<NeedType, NeedState> _needs;

        private DateTime _lastUpdateUtc;
        private bool _isFirstTick;

        // ===== Exposição pro GTA (integrações) =====
        public float StaminaDrainMultiplier => _settings.StaminaDrainMultiplier;
        public float StaminaRegenMultiplier => _settings.StaminaRegenMultiplier;
        public float MaxStaminaLimit => _settings.MaxStamina;

        public float WarningThreshold => _settings.WarningThreshold;
        public float CriticalThreshold => _settings.CriticalThreshold;

        public NeedsSystem(Logger logger, EventManager eventManager, NeedsSettings settings)
            : base("NeedsSystem", logger, eventManager, tickRate: 1)
        {
            _settings = settings ?? new NeedsSettings();

            _needs = new Dictionary<NeedType, NeedState>
            {
                { NeedType.Hunger,  new NeedState(NeedType.Hunger,  _settings.InitialHunger,  _settings.MaxHunger) },
                { NeedType.Thirst,  new NeedState(NeedType.Thirst,  _settings.InitialThirst,  _settings.MaxThirst) },
                { NeedType.Sleep,   new NeedState(NeedType.Sleep,   _settings.InitialSleep,   _settings.MaxSleep) },
                { NeedType.Stamina, new NeedState(NeedType.Stamina, _settings.InitialStamina, _settings.MaxStamina) }
            };

            _isFirstTick = true;
            _lastUpdateUtc = DateTime.UtcNow;
        }

        protected override void OnStart()
        {
            _isFirstTick = true;
            _lastUpdateUtc = DateTime.UtcNow;

            Logger.Info($"{Name}: iniciado");

            Events.Raise(
                "needs:started",
                new RLFEventArgs<string>(Name)
            );
        }

        protected override void OnStop()
        {
            Logger.Info($"{Name}: parado");

            Events.Raise(
                "needs:stopped",
                new RLFEventArgs<string>(Name)
            );
        }

        protected override void OnTick()
        {
            float dt = GetDeltaSeconds();
            if (dt <= 0.0f)
                return;

            ApplyDecay(dt);
            UpdatePenalties();
        }

        private float GetDeltaSeconds()
        {
            DateTime now = DateTime.UtcNow;

            if (_isFirstTick)
            {
                _isFirstTick = false;
                _lastUpdateUtc = now;
                return 0.0f;
            }

            double raw = (now - _lastUpdateUtc).TotalSeconds;
            _lastUpdateUtc = now;

            // Proteção contra alt-tab / freeze
            if (raw < 0) raw = 0;
            if (raw > 0.25) raw = 0.25;

            return (float)raw;
        }

        private void ApplyDecay(float deltaSeconds)
        {
            float hours = deltaSeconds / 3600f;

            Decay(NeedType.Hunger, _settings.HungerDecayPerHour * hours);
            Decay(NeedType.Thirst, _settings.ThirstDecayPerHour * hours);
            Decay(NeedType.Sleep, _settings.SleepDecayPerHour * hours);

            // Stamina não decai passivamente aqui:
            // ela é controlada pela integração do GTA (corrida/regen).
        }

        private void Decay(NeedType type, float amount)
        {
            if (amount <= 0f) return;

            var need = _needs[type];
            float oldValue = need.Value;

            need.Decrease(amount);

            if (Math.Abs(oldValue - need.Value) > 0.01f)
            {
                Events.Raise(
                    "needs:changed",
                    new NeedChangedEvent(type, oldValue, need.Value)
                );

                if (need.Value <= _settings.CriticalThreshold &&
                    oldValue > _settings.CriticalThreshold)
                {
                    Events.Raise(
                        "needs:critical",
                        new NeedCriticalEvent(type, need.Value)
                    );
                }
            }
        }

        public void Eat(float amount = 100f) => Restore(NeedType.Hunger, amount);
        public void Drink(float amount = 100f) => Restore(NeedType.Thirst, amount);

        public void Sleep(float hours)
        {
            if (hours <= 0f) return;
            float restore = hours * _settings.SleepRestorePerHour;
            Restore(NeedType.Sleep, restore);
        }

        public void RestoreStamina(float amount) => Restore(NeedType.Stamina, amount);

        public float GetNeedValue(NeedType type) => _needs[type].Value;

        private void Restore(NeedType type, float amount)
        {
            if (amount <= 0f) return;

            var need = _needs[type];
            float oldValue = need.Value;

            need.Increase(amount);

            if (Math.Abs(oldValue - need.Value) > 0.01f)
            {
                Events.Raise(
                    "needs:changed",
                    new NeedChangedEvent(type, oldValue, need.Value)
                );
            }
        }

        private void UpdatePenalties()
        {
            // Core mantém só estado / eventos.
            // Efeitos (corrida, tremor, etc) ficam no RLF.GTA (NeedsFeedbackIntegration).
        }
    }
}
