namespace SurvivalNeeds.Inventory
{
    public class InventorySlot
    {
        public InventoryItem Item
        {
            get;
            private set;
        }

        public int Quantity
        {
            get;
            private set;
        }

        // Current ammunition stored for weapon items.
        public int Ammo
        {
            get;
            private set;
        }

        public bool IsEmpty
        {
            get
            {
                return Item == null;
            }
        }

        //====================================================
        // SET NORMAL ITEM
        //====================================================

        public void SetItem(
            InventoryItem item,
            int quantity)
        {
            Item =
                item;

            Quantity =
                quantity;

            if (item == null ||
                !item.IsWeapon)
            {
                Ammo = 0;
            }
            else if (Ammo <= 0)
            {
                Ammo =
                    item.StartingAmmo;
            }
        }

        //====================================================
        // SET WEAPON ITEM WITH AMMO
        //====================================================

        public void SetItem(
            InventoryItem item,
            int quantity,
            int ammo)
        {
            Item =
                item;

            Quantity =
                quantity;

            if (item == null ||
                !item.IsWeapon)
            {
                Ammo = 0;
                return;
            }

            if (ammo < 0)
            {
                ammo = 0;
            }

            Ammo =
                ammo;
        }

        //====================================================
        // SET AMMO
        //====================================================

        public void SetAmmo(
            int ammo)
        {
            if (Item == null ||
                !Item.IsWeapon)
            {
                Ammo = 0;
                return;
            }

            if (ammo < 0)
            {
                ammo = 0;
            }

            Ammo =
                ammo;
        }

        //====================================================
        // CLEAR
        //====================================================

        public void Clear()
        {
            Item = null;
            Quantity = 0;
            Ammo = 0;
        }
    }
}