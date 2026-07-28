namespace SurvivalNeeds.Inventory
{
    public class InventorySlot
    {
        public InventoryItem Item { get; private set; }

        public int Quantity { get; private set; }

        public bool IsEmpty => Item == null;

        public void SetItem(InventoryItem item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }

        public void Clear()
        {
            Item = null;
            Quantity = 0;
        }
    }
}