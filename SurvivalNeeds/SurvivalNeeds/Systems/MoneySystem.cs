using GTA;

namespace SurvivalNeeds.Systems
{
    public class MoneySystem
    {
        public int Cash
        {
            get
            {
                int gtaCash =
                    Game.Player.Money;

                return gtaCash < 0
                    ? 0
                    : gtaCash;
            }
        }

        public MoneySystem(
            int startingCash = 100)
        {
            Set(startingCash);
        }

        public int ConfiscateCash()
        {
            int confiscatedAmount =
                Cash;

            Set(0);

            return confiscatedAmount;
        }

        public void AddMoney(
            int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Set(
                Cash + amount
            );
        }

        public bool SpendMoney(
            int amount)
        {
            if (amount <= 0 ||
                Cash < amount)
            {
                return false;
            }

            Set(
                Cash - amount
            );

            return true;
        }

        public bool TrySpendMoney(
            int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            return SpendMoney(
                amount
            );
        }

        public bool CanAfford(
            int amount)
        {
            return amount >= 0 &&
                Cash >= amount;
        }

        public void Set(
            int amount)
        {
            Game.Player.Money =
                amount < 0
                    ? 0
                    : amount;
        }
    }
}