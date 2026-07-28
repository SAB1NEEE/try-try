using GTA.Math;
using System.Collections.Generic;

namespace SurvivalNeeds.Vendors
{
    public static class LiquorStores
    {
        public static void AddStores(
            List<Vendor> vendors)
        {
            if (vendors == null)
                return;

            AddVendor(
                vendors,
                "ROB'S LIQUOR - VESPUCCI",
                -1222.91f,
                -907.98f,
                12.33f
            );

            AddVendor(
                vendors,
                "ROB'S LIQUOR - MURRIETA HEIGHTS",
                1134.18f,
                -982.44f,
                46.42f
            );

            AddVendor(
                vendors,
                "ROB'S LIQUOR - MORNINGWOOD",
                -1486.61f,
                -377.76f,
                40.16f
            );

            AddVendor(
                vendors,
                "ROB'S LIQUOR - GRAND SENORA",
                1165.96f,
                2709.45f,
                38.16f
            );

            AddVendor(
                vendors,
                "ROB'S LIQUOR - GREAT CHAPARRAL",
                -2966.39f,
                390.86f,
                15.04f
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
                    VendorType.LiquorStore,
                    new Vector3(
                        x,
                        y,
                        z
                    ),
                    VendorInventories
                        .CreateLiquorItems(),
                    2.5f
                )
            );
        }
    }
}