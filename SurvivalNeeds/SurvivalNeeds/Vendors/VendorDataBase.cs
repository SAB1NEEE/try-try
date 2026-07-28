using System.Collections.Generic;

namespace SurvivalNeeds.Vendors
{
    public static class VendorDataBase
    {
        public static List<Vendor>
            CreateVendors()
        {
            List<Vendor> vendors =
                new List<Vendor>();

            ConvenienceStore.AddStores(
                vendors
            );

            LiquorStores.AddStores(
                vendors
            );

            PharmacyStore.AddStores(
                vendors
            );

            StreetVendors.AddStores(
            vendors
            );

            return vendors;
        }

        public static List<VendorItem>
            CreateItemsForType(
                VendorType vendorType)
        {
            switch (vendorType)
            {
                case VendorType
                    .ConvenienceStore:

                    return VendorInventories
                        .CreateConvenienceItems();

                case VendorType
                    .LiquorStore:

                    return VendorInventories
                        .CreateLiquorItems();

                case VendorType
                    .Pharmacy:

                    return VendorInventories
                        .CreatePharmacyItems();

                case VendorType
                    .StreetVendor:

                    return VendorInventories
                        .CreateFoodVendorItems();

                default:
                    return new List<VendorItem>();
            }
        }
    }
}