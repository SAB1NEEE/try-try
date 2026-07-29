using System.Collections.Generic;

namespace SurvivalNeeds.Vendors
{
    public static class VendorInventories
    {
        public static List<VendorItem>
            CreateConvenienceItems()
        {
            return new List<VendorItem>()
            {
                new VendorItem(
                    "water",
                    VendorCategory.Drinks
                ),

                new VendorItem(
                    "coffee",
                    VendorCategory.Drinks
                ),

                new VendorItem(
                    "soda",
                    VendorCategory.Drinks
                ),

                new VendorItem(
                    "energydrink",
                    VendorCategory.Drinks
                ),

                new VendorItem(
                    "chips",
                    VendorCategory.Food
                ),

                new VendorItem(
                    "chocolate",
                    VendorCategory.Food
                ),

                new VendorItem(
                    "cannedbeans",
                    VendorCategory.Food
                ),

                new VendorItem(
                    "cigarette",
                    VendorCategory.StressRelief
                ),

                new VendorItem(
                    "cigar",
                    VendorCategory.StressRelief
                )
            };
        }

        public static readonly List<VendorItem> HotdogStand =
            new List<VendorItem>()
        {
    new VendorItem("hotdog", VendorCategory.Food, 12),
    new VendorItem("water", VendorCategory.Drinks, 5),
    new VendorItem("soda", VendorCategory.Drinks, 6)
        };

        public static readonly List<VendorItem> BurgerStand =
            new List<VendorItem>()
        {
    new VendorItem("burger", VendorCategory.Food, 18),
    new VendorItem("chips", VendorCategory.Food, 8),
    new VendorItem("water", VendorCategory.Drinks, 5),
    new VendorItem("soda", VendorCategory.Drinks, 6)
        };

        public static readonly List<VendorItem> SnackMachine =
            new List<VendorItem>()
        {
    new VendorItem("chips", VendorCategory.Food, 8),
    new VendorItem("chocolate", VendorCategory.Food, 10)
        };

        public static readonly List<VendorItem> DrinkMachine =
            new List<VendorItem>()
        {
    new VendorItem("water", VendorCategory.Drinks, 5),
    new VendorItem("soda", VendorCategory.Drinks, 6),
    new VendorItem("energydrink", VendorCategory.Drinks, 15)
        };

        public static List<VendorItem>
            CreateLiquorItems()
        {
            return new List<VendorItem>()
            {
                new VendorItem(
                    "water",
                    VendorCategory.Drinks
                ),

                new VendorItem(
                    "soda",
                    VendorCategory.Drinks
                ),

                new VendorItem(
                    "energydrink",
                    VendorCategory.Drinks
                ),

                new VendorItem(
                    "chips",
                    VendorCategory.Food
                ),

                new VendorItem(
                    "cigarette",
                    VendorCategory.StressRelief
                ),

                new VendorItem(
                    "cigar",
                    VendorCategory.StressRelief
                )
            };
        }

        public static List<VendorItem>
            CreatePharmacyItems()
        {
            return new List<VendorItem>()
            {
                new VendorItem(
                    "bandage",
                    VendorCategory.Medical
                ),

                new VendorItem(
                    "painkillers",
                    VendorCategory.Medical
                ),

                new VendorItem(
                    "firstaidkit",
                    VendorCategory.Medical
                ),

                new VendorItem(
                    "water",
                    VendorCategory.Drinks
                )
            };
        }

        public static List<VendorItem>
            CreateFoodVendorItems()
        {
            return new List<VendorItem>()
            {
                new VendorItem(
                    "hotdog",
                    VendorCategory.Food
                ),

                new VendorItem(
                    "burger",
                    VendorCategory.Food
                ),

                new VendorItem(
                    "chips",
                    VendorCategory.Food
                ),

                new VendorItem(
                    "water",
                    VendorCategory.Drinks
                ),

                new VendorItem(
                    "soda",
                    VendorCategory.Drinks
                )
            };
        }
    }
}