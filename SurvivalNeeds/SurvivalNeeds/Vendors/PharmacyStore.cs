using System;
using System.Collections.Generic;

namespace SurvivalNeeds.Vendors
{
    public static class PharmacyStore
    {
        private static readonly string[]
            pharmacyPartners =
            {
                "24/7 STORE - STRAWBERRY",
                "24/7 STORE - VINEWOOD",
                "24/7 STORE - HARMONY",
                "24/7 STORE - SANDY SHORES",
                "LTD GASOLINE - GRAPESEED",
                "24/7 STORE - PALETO BAY"
            };

        public static void AddStores(
            List<Vendor> vendors)
        {
            if (vendors == null)
                return;

            foreach (Vendor vendor in vendors)
            {
                if (vendor == null ||
                    vendor.Items == null)
                {
                    continue;
                }

                if (!IsPharmacyPartner(
                    vendor.Name))
                {
                    continue;
                }

                AddMedicalItems(
                    vendor
                );
            }
        }

        private static bool IsPharmacyPartner(
            string vendorName)
        {
            if (string.IsNullOrWhiteSpace(
                vendorName))
            {
                return false;
            }

            foreach (string partnerName
                in pharmacyPartners)
            {
                if (string.Equals(
                    vendorName,
                    partnerName,
                    StringComparison
                        .OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddMedicalItems(
            Vendor vendor)
        {
            List<VendorItem> medicalItems =
                VendorInventories
                    .CreatePharmacyItems();

            foreach (VendorItem medicalItem
                in medicalItems)
            {
                if (medicalItem == null ||
                    string.IsNullOrWhiteSpace(
                        medicalItem.ItemId))
                {
                    continue;
                }

                if (ContainsItem(
                    vendor.Items,
                    medicalItem.ItemId))
                {
                    continue;
                }

                vendor.Items.Add(
                    medicalItem
                );
            }
        }

        private static bool ContainsItem(
            List<VendorItem> items,
            string itemId)
        {
            if (items == null ||
                string.IsNullOrWhiteSpace(
                    itemId))
            {
                return false;
            }

            foreach (VendorItem item
                in items)
            {
                if (item == null)
                    continue;

                if (string.Equals(
                    item.ItemId,
                    itemId,
                    StringComparison
                        .OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}