using SurvivalNeeds.Inventory;
using SurvivalNeeds.UI;

namespace SurvivalNeeds.Apartments
{
    public class ApartmentStorageMenu
    {
        private readonly VehicleStorageMenu
            storageMenu;

        public ApartmentStorageMenu(
            InventoryManager playerInventory,
            InventoryManager apartmentInventory)
        {
            storageMenu =
                new VehicleStorageMenu(
                    playerInventory,
                    apartmentInventory
                );
        }

        public bool Visible
        {
            get
            {
                return storageMenu != null &&
                       storageMenu.Visible;
            }
        }

        public void Open()
        {
            storageMenu?.Open();
        }

        public void Close()
        {
            storageMenu?.Close();
        }

        public void Draw()
        {
            storageMenu?.Draw();
        }
    }
}