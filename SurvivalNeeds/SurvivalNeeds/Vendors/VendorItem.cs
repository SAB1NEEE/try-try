using SurvivalNeeds.Inventory;

namespace SurvivalNeeds.Vendors
{
    public class VendorItem
    {
        public string ItemId
        {
            get;
            private set;
        }

        public VendorCategory Category
        {
            get;
            private set;
        }

        public int CustomPrice
        {
            get;
            private set;
        }

        public InventoryItem Item
        {
            get
            {
                return ItemDatabase.GetItem(
                    ItemId
                );
            }
        }

        public int Price
        {
            get
            {
                if (CustomPrice >= 0)
                {
                    return CustomPrice;
                }

                InventoryItem inventoryItem =
                    Item;

                if (inventoryItem == null)
                {
                    return 0;
                }

                return inventoryItem.Price;
            }
        }

        public int Stock
        {
            get;
            set;
        }

        public int MaxStock
        {
            get;
            private set;
        }
        public bool IsValid
        {
            get
            {
                return Item != null;
            }
        }

        public VendorItem(
            string itemId,
            VendorCategory category,
            int customPrice = -1,
            int stock = 999)
        {
            ItemId = itemId;

            Category = category;

            CustomPrice = customPrice;

            Stock = stock;

            MaxStock = stock;
        }
    }
}