using RLF.Core.Logging;
using RLF.Core.Needs;
using RLF.Core.Systems;

namespace RLF.GTA.CoreIntegration.Needs
{
    public class NeedsIntegrationSystem : SystemBase
    {
        private readonly StaminaIntegration _stamina;
        private readonly EatDrinkIntegration _eatDrink;
        private readonly SleepIntegration _sleep;
        private readonly NeedsFeedbackIntegration _feedback;
        private readonly NeedsWarningIntegration _warnings;

        public NeedsIntegrationSystem(
            Logger logger,
            NeedsSystem needsSystem)
            : base("NeedsIntegrationSystem", logger, null, tickRate: 1)
        {
            _stamina = new StaminaIntegration(needsSystem);
            _eatDrink = new EatDrinkIntegration(needsSystem);
            _sleep = new SleepIntegration(needsSystem);
            _feedback = new NeedsFeedbackIntegration(needsSystem);
            _warnings = new NeedsWarningIntegration(needsSystem);
        }

        protected override void OnStart()
        {
            Logger.Info($"{Name}: iniciado");
        }

        protected override void OnStop()
        {
            Logger.Info($"{Name}: parado");
        }

        protected override void OnTick()
        {
            _stamina.Tick();
            _eatDrink.Tick();
            _sleep.Tick();
            _feedback.Tick();
            _warnings.Tick();
        }
    }
}
