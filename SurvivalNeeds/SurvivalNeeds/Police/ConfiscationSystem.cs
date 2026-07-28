using GTA;
using SurvivalNeeds.Inventory;
using SurvivalNeeds.Systems;

namespace SurvivalNeeds.Police
{
    public class ConfiscationSystem
    {
        private readonly MoneySystem money;
        private readonly InventoryManager inventory;

        public ConfiscationSystem(
            MoneySystem money,
            InventoryManager inventory)
        {
            this.money = money;
            this.inventory = inventory;
        }

        public void ConfiscatePlayer()
        {
            // Pocket cash
            money.ConfiscateCash();

            // GTA weapons
            Game.Player.Character.Weapons.RemoveAll();

            // Survival inventory
            inventory.Clear();
        }
    }
}