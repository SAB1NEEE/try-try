using GTA;
using GTA.Math;
using GTA.Native;
using System.Collections.Generic;

namespace SurvivalNeeds.Vendors
{
    public static class StreetVendors
    {
        private const float DetectionRadius = 4.0f;

        private static readonly string[] HotdogStandModels =
        {
            "prop_hotdogstand_01"
        };

        private static readonly string[] BurgerStandModels =
        {
            "prop_burgerstand_01",
            "prop_food_bs_juice01",
            "prop_food_bs_juice02",
            "prop_food_burg1",
            "prop_food_burg2"
        };

        private static readonly string[] SnackMachineModels =
        {
            "prop_vend_snak_01",
            "prop_vend_snak_01_tu"
        };

        private static readonly string[] DrinkMachineModels =
        {
            "prop_vend_soda_01",
            "prop_vend_soda_02",
            "prop_vend_fridge01",
            "prop_vend_water_01"
        };

        public static void AddStores(
            List<Vendor> vendors)
        {
            // Street vendors are detected dynamically.
            // No fixed coordinates are added here.
        }

        public static Vendor FindNearbyVendor(
            Vector3 playerPosition)
        {
            Vendor vendor;

            vendor = FindVendorFromModels(
                playerPosition,
                HotdogStandModels,
                "Hotdog Stand",
                StreetVendorType.HotdogStand,
                VendorInventories.HotdogStand,
                3.0f
            );

            if (vendor != null)
                return vendor;

            vendor = FindVendorFromModels(
                playerPosition,
                BurgerStandModels,
                "Burger Stand",
                StreetVendorType.BurgerStand,
                VendorInventories.BurgerStand,
                4.0f
            );

            if (vendor != null)
                return vendor;

            vendor = FindVendorFromModels(
                playerPosition,
                SnackMachineModels,
                "Snack Vending Machine",
                StreetVendorType.SnackMachine,
                VendorInventories.SnackMachine,
                2.0f
            );

            if (vendor != null)
                return vendor;

            vendor = FindVendorFromModels(
                playerPosition,
                DrinkMachineModels,
                "Drink Vending Machine",
                StreetVendorType.DrinkMachine,
                VendorInventories.DrinkMachine,
                2.0f
            );

            return vendor;
        }

        private static Vendor FindVendorFromModels(
            Vector3 playerPosition,
            string[] modelNames,
            string vendorName,
            StreetVendorType streetType,
            List<VendorItem> items,
            float interactionDistance)
        {
            if (modelNames == null)
                return null;

            Vendor closestVendor = null;
            float closestDistance = float.MaxValue;

            foreach (string modelName in modelNames)
            {
                if (string.IsNullOrWhiteSpace(modelName))
                    continue;

                int modelHash =
                    Game.GenerateHash(modelName);

                int objectHandle =
                    Function.Call<int>(
                        Hash.GET_CLOSEST_OBJECT_OF_TYPE,
                        playerPosition.X,
                        playerPosition.Y,
                        playerPosition.Z,
                        DetectionRadius,
                        modelHash,
                        false,
                        false,
                        false
                    );

                if (objectHandle == 0)
                    continue;

                bool exists =
                    Function.Call<bool>(
                        Hash.DOES_ENTITY_EXIST,
                        objectHandle
                    );

                if (!exists)
                    continue;

                Vector3 objectPosition =
                    Function.Call<Vector3>(
                        Hash.GET_ENTITY_COORDS,
                        objectHandle,
                        true
                    );

                float distance =
                    playerPosition.DistanceTo(
                        objectPosition
                    );

                if (distance >
                    interactionDistance)
                {
                    continue;
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;

                    closestVendor =
                        new Vendor(
                            vendorName,
                            VendorType.StreetVendor,
                            objectPosition,
                            items,
                            interactionDistance,
                            streetType
                        );
                }
            }

            return closestVendor;
        }
    }
}