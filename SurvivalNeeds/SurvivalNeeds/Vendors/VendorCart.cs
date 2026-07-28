using SurvivalNeeds.Inventory;
using System.Collections.Generic;

namespace SurvivalNeeds.Vendors
{
    public class VendorCartEntry
    {
        public VendorItem VendorItem
        {
            get;
            private set;
        }

        public int Quantity
        {
            get;
            private set;
        }

        public InventoryItem Item
        {
            get
            {
                if (VendorItem == null)
                {
                    return null;
                }

                return VendorItem.Item;
            }
        }

        public int UnitPrice
        {
            get
            {
                if (VendorItem == null)
                {
                    return 0;
                }

                return VendorItem.Price;
            }
        }

        public int TotalPrice
        {
            get
            {
                return UnitPrice *
                    Quantity;
            }
        }

        public VendorCartEntry(
            VendorItem vendorItem,
            int quantity)
        {
            VendorItem = vendorItem;

            Quantity =
                quantity < 1
                    ? 1
                    : quantity;
        }

        public void Add(int amount = 1)
        {
            if (amount <= 0)
            {
                return;
            }

            Quantity += amount;
        }

        public void Remove(int amount = 1)
        {
            if (amount <= 0)
            {
                return;
            }

            Quantity -= amount;

            if (Quantity < 0)
            {
                Quantity = 0;
            }
        }
    }

    public class VendorCart
    {
        private readonly List<VendorCartEntry>
            entries =
                new List<VendorCartEntry>();

        public IReadOnlyList<VendorCartEntry>
            Entries
        {
            get
            {
                return entries.AsReadOnly();
            }
        }

        public int EntryCount
        {
            get
            {
                return entries.Count;
            }
        }

        public int ItemCount
        {
            get
            {
                int total = 0;

                foreach (VendorCartEntry entry
                    in entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    total += entry.Quantity;
                }

                return total;
            }
        }

        public int TotalPrice
        {
            get
            {
                int total = 0;

                foreach (VendorCartEntry entry
                    in entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    total += entry.TotalPrice;
                }

                return total;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return entries.Count == 0;
            }
        }

        public bool AddItem(
            VendorItem vendorItem,
            int quantity = 1)
        {
            if (vendorItem == null ||
                !vendorItem.IsValid ||
                quantity <= 0)
            {
                return false;
            }

            VendorCartEntry existingEntry =
                FindEntry(
                    vendorItem.ItemId
                );

            if (existingEntry != null)
            {
                existingEntry.Add(
                    quantity
                );

                return true;
            }

            entries.Add(
                new VendorCartEntry(
                    vendorItem,
                    quantity
                )
            );

            return true;
        }

        public bool RemoveItem(
            string itemId,
            int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(
                    itemId) ||
                quantity <= 0)
            {
                return false;
            }

            VendorCartEntry entry =
                FindEntry(
                    itemId
                );

            if (entry == null)
            {
                return false;
            }

            entry.Remove(
                quantity
            );

            if (entry.Quantity <= 0)
            {
                entries.Remove(
                    entry
                );
            }

            return true;
        }

        public bool RemoveEntryAt(
            int index,
            int quantity = 1)
        {
            if (index < 0 ||
                index >= entries.Count ||
                quantity <= 0)
            {
                return false;
            }

            VendorCartEntry entry =
                entries[index];

            entry.Remove(
                quantity
            );

            if (entry.Quantity <= 0)
            {
                entries.RemoveAt(
                    index
                );
            }

            return true;
        }

        public VendorCartEntry GetEntry(
            int index)
        {
            if (index < 0 ||
                index >= entries.Count)
            {
                return null;
            }

            return entries[index];
        }

        public int GetQuantity(
            string itemId)
        {
            VendorCartEntry entry =
                FindEntry(
                    itemId
                );

            if (entry == null)
            {
                return 0;
            }

            return entry.Quantity;
        }

        public void Clear()
        {
            entries.Clear();
        }

        private VendorCartEntry FindEntry(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(
                    itemId))
            {
                return null;
            }

            foreach (VendorCartEntry entry
                in entries)
            {
                if (entry == null ||
                    entry.VendorItem == null)
                {
                    continue;
                }

                if (string.Equals(
                    entry.VendorItem.ItemId,
                    itemId,
                    System.StringComparison
                        .OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }
    }
}