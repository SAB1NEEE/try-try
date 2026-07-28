using SurvivalNeeds.Inventory;

namespace SurvivalNeeds.VehicleStorage
{
    public class VehicleInventory
    {
        public string VehicleKey
        {
            get;
            private set;
        }

        public InventoryManager Inventory
        {
            get;
            private set;
        }

        public VehicleInventory(
            string vehicleKey,
            int slotCount = 30)
        {
            VehicleKey =
                vehicleKey;

            Inventory =
                new InventoryManager(
                    slotCount
                );
        }
    }
}