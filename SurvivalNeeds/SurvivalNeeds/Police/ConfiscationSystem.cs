using GTA;
using SurvivalNeeds.Inventory;
using SurvivalNeeds.Systems;

namespace SurvivalNeeds.Police
{
    public class ConfiscationSystem
    {
        private readonly MoneySystem money;
        private readonly InventoryManager inventory;
        private readonly SaveSystem saveSystem;


        public ConfiscationSystem(
            MoneySystem money,
            InventoryManager inventory,
            SaveSystem saveSystem)
        {
            this.money = money;
            this.inventory = inventory;
            this.saveSystem = saveSystem;
        }

        public void ConfiscatePlayer()
        {
            money.ConfiscateCash();

            Game.Player.Character.Weapons.RemoveAll();

            saveSystem.MarkWeaponsConfiscated();

            inventory.Clear();
        }
    }
}