using GTA;
using RLF.Core.Needs;

namespace RLF.GTA.CoreIntegration.Needs
{
    public class EatDrinkIntegration
    {
        private readonly NeedsSystem _needs;

        private int _lastMoney;

        public EatDrinkIntegration(NeedsSystem needs)
        {
            _needs = needs;
            _lastMoney = Game.Player.Money;
        }

        public void Tick()
        {
            int currentMoney = Game.Player.Money;

            // Detecta gasto de dinheiro (comida/bebida)
            if (currentMoney < _lastMoney)
            {
                int spent = _lastMoney - currentMoney;

                // Gastos pequenos → bebida
                if (spent <= 50)
                {
                    _needs.Drink();
                }
                // Gastos maiores → comida
                else
                {
                    _needs.Eat();
                }
            }

            _lastMoney = currentMoney;
        }
    }
}
