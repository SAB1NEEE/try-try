using GTA.Math;
using System.Collections.Generic;

namespace SurvivalNeeds.Vendors
{
    public static class ConvenienceStore
    {
        public static void AddStores(
            List<Vendor> vendors)
        {
            if (vendors == null)
                return;

            Add24SevenStores(vendors);
            AddLTDStores(vendors);
        }

        private static void Add24SevenStores(
            List<Vendor> vendors)
        {
            AddVendor(
                vendors,
                "24/7 STORE - STRAWBERRY",
                24.47f,
                -1346.62f,
                29.50f
            );

            AddVendor(
                vendors,
                "24/7 STORE - VINEWOOD",
                373.87f,
                325.89f,
                103.57f
            );

            AddVendor(
                vendors,
                "24/7 STORE - TATAVIAM",
                2557.18f,
                382.15f,
                108.62f
            );

            AddVendor(
                vendors,
                "24/7 STORE - CHUMASH",
                -3242.24f,
                1001.03f,
                12.83f
            );

            AddVendor(
                vendors,
                "24/7 STORE - HARMONY",
                549.35f,
                2671.35f,
                42.16f
            );

            AddVendor(
                vendors,
                "24/7 STORE - SANDY SHORES",
                1960.01f,
                3740.12f,
                32.34f
            );

            AddVendor(
                vendors,
                "24/7 STORE - MOUNT CHILIAD",
                2677.91f,
                3280.67f,
                55.24f
            );

            AddVendor(
                vendors,
                "24/7 STORE - PALETO BAY",
                1728.82f,
                6417.38f,
                35.04f
            );
        }

        private static void AddLTDStores(
            List<Vendor> vendors)
        {
            AddVendor(
                vendors,
                "LTD GASOLINE - GROVE STREET",
                -47.42f,
                -1758.67f,
                29.42f
            );

            AddVendor(
                vendors,
                "LTD GASOLINE - MIRROR PARK",
                1163.37f,
                -323.80f,
                69.21f
            );

            AddVendor(
                vendors,
                "LTD GASOLINE - RICHMAN GLEN",
                -1820.49f,
                792.52f,
                138.11f
            );

            AddVendor(
                vendors,
                "LTD GASOLINE - DAVIS",
                -706.08f,
                -914.44f,
                19.22f
            );

            AddVendor(
                vendors,
                "LTD GASOLINE - GRAPESEED",
                1698.23f,
                4924.28f,
                42.06f
            );
        }

        private static void AddVendor(
            List<Vendor> vendors,
            string name,
            float x,
            float y,
            float z)
        {
            vendors.Add(
                new Vendor(
                    name,
                    VendorType.ConvenienceStore,
                    new Vector3(
                        x,
                        y,
                        z
                    ),
                    VendorInventories
                        .CreateConvenienceItems(),
                    2.5f
                )
            );
        }
    }
}