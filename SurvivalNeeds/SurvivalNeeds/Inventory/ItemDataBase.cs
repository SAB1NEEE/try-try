using System.Collections.Generic;

namespace SurvivalNeeds.Inventory
{
    public static class ItemDatabase
    {
        public static readonly Dictionary<string, InventoryItem> Items =
            new Dictionary<string, InventoryItem>()
            {
                // ============================================
                // FOOD
                // ============================================

                {
                    "hotdog",
                    new InventoryItem(
                        "hotdog",
                        "Hot Dog",
                        "A delicious street hot dog.",
                        ItemCategory.Food,
                        0.35f, //weight
                        8, //price
                        5, //max stack
                        25, //hunger
                        0, //thirst
                        0, //stress
                        0, //health
                        "hotdog.png")
                },

                {
                    "burger",
                    new InventoryItem(
                        "burger",
                        "Burger",
                        "A juicy beef burger.",
                        ItemCategory.Food,
                        0.60f,
                        15,
                        5,
                        45,
                        0,
                        0,
                        0,
                        "burger.png")
                },

                {
                    "chips",
                    new InventoryItem(
                        "chips",
                        "Bag of Chips",
                        "A salty bag of potato chips.",
                        ItemCategory.Food,
                        0.20f,
                        4,
                        10,
                        12,
                        -2,
                        0,
                        0,
                        "chips.png")
                },

                {
                    "cannedbeans",
                    new InventoryItem(
                        "cannedbeans",
                        "Canned Beans",
                        "A sealed can of beans.",
                        ItemCategory.Food,
                        0.45f,
                        10,
                        6,
                        35,
                        0,
                        0, 
                        0,
                        "cannedbeans.png")
                },

                {
                    "chocolate",
                    new InventoryItem(
                        "chocolate",
                        "Chocolate Bar",
                        "A sweet chocolate bar.",
                        ItemCategory.Food,
                        0.12f,
                        6,
                        10,
                        18,
                        -1,
                        0,
                        0,
                        "chocolate.png")
                },

                // ============================================
                // DRINKS
                // ============================================

                {
                    "water",
                    new InventoryItem(
                        "water",
                        "Water Bottle",
                        "Fresh drinking water.",
                        ItemCategory.Drink,
                        0.50f,
                        5,
                        10,
                        0,
                        40,
                        0, 
                        0,
                        "water.png")
                },

                {
                    "coffee",
                    new InventoryItem(
                        "coffee",
                        "Coffee",
                        "Hot brewed coffee.",
                        ItemCategory.Drink,
                        0.30f,
                        6,
                        5,
                        0,
                        20,
                        0,
                        0,
                        "coffee.png")
                },

                {
                    "soda",
                    new InventoryItem(
                        "soda",
                        "Soda Can",
                        "A sugary carbonated drink.",
                        ItemCategory.Drink,
                        0.35f,
                        5,
                        10,
                        2,
                        25,
                        0, 
                        0,
                        "soda.png")
                },

                {
                    "energydrink",
                    new InventoryItem(
                        "energydrink",
                        "Energy Drink",
                        "A highly caffeinated energy drink.",
                        ItemCategory.Drink,
                        0.35f,
                        12,
                        6,
                        0,
                        30,
                        0,
                        0,
                        "energydrink.png")
                },

                // ============================================
                // MEDICAL
                // ============================================

                {
                    "bandage",
                    new InventoryItem(
                        "bandage",
                        "Bandage",
                        "A clean medical bandage.",
                        ItemCategory.Medical,
                        0.10f,
                        15,
                        10,
                        0,
                        0,
                        0,
                        15,
                        "bandage.png")
                },

                {
                    "painkillers",
                    new InventoryItem(
                        "painkillers",
                        "Painkillers",
                        "Tablets used to reduce pain.",
                        ItemCategory.Medical,
                        0.15f,
                        25,
                        5,
                        0,
                        0,
                        0,
                        25,
                        "painkillers.png")
                },

                {
                    "firstaidkit",
                    new InventoryItem(
                        "firstaidkit",
                        "First Aid Kit",
                        "A compact emergency medical kit.",
                        ItemCategory.Medical,
                        1.20f,
                        75,
                        2,
                        0,
                        0,
                        0,
                        75,
                        "firstaidkit.png")
                },

                // ============================================
                // TOOLS AND MATERIALS
                // ============================================

                {
                    "scrapmetal",
                    new InventoryItem(
                        "scrapmetal",
                        "Scrap Metal",
                        "Old metal that may be useful for crafting.",
                        ItemCategory.Tool,
                        1.50f,
                        8,
                        20,
                        0,
                        0,
                        0, 
                        0,
                        "scrapmetal.png")
                },

                {
                    "plastic",
                    new InventoryItem(
                        "plastic",
                        "Plastic Scrap",
                        "Discarded plastic that may be reusable.",
                        ItemCategory.Tool,
                        0.25f,
                        2,
                        25,
                        0,
                        0,
                        0, 
                        0,
                        "plastic.png")
                },

                {
                    "wires",
                    new InventoryItem(
                        "wires",
                        "Electrical Wires",
                        "A bundle of old electrical wires.",
                        ItemCategory.Tool,
                        0.30f,
                        10,
                        15,
                        0,
                        0,
                        0, 
                        0,
                        "wires.png")
                },

                {
                    "electronics",
                    new InventoryItem(
                        "electronics",
                        "Electronic Parts",
                        "Assorted reusable electronic components.",
                        ItemCategory.Tool,
                        0.60f,
                        20,
                        10,
                        0,
                        0,
                        0,
                        0,
                        "electronics.png")
                },

                {
                    "glassbottle",
                    new InventoryItem(
                        "glassbottle",
                        "Glass Bottle",
                        "An empty glass bottle.",
                        ItemCategory.Tool,
                        0.40f,
                        2,
                        10,
                        0,
                        0,
                        0,
                        0,
                        "glassbottle.png")
                },

                {
                    "cloth",
                    new InventoryItem(
                        "cloth",
                        "Cloth",
                        "A piece of fabric useful for crafting.",
                        ItemCategory.Tool,
                        0.15f,
                        4,
                        20,
                        0,
                        0,
                        0,
                        0,
                        "cloth.png")
                },

                // ============================================
                // MISCELLANEOUS AND VALUABLES
                // ============================================

                {
                    "oldwallet",
                    new InventoryItem(
                        "oldwallet",
                        "Old Wallet",
                        "An abandoned wallet that may have value.",
                        ItemCategory.Misc,
                        0.20f,
                        25,
                        5,
                        0,
                        0,
                        0,
                        0,
                        "oldwallet.png")
                },

                {
                    "ring",
                    new InventoryItem(
                        "ring",
                        "Ring",
                        "A small ring that could be sold.",
                        ItemCategory.Misc,
                        0.05f,
                        80,
                        5,
                        0,
                        0,
                        0,
                        0,
                        "ring.png")
                },

                {
                    "necklace",
                    new InventoryItem(
                        "necklace",
                        "Necklace",
                        "A valuable-looking necklace.",
                        ItemCategory.Misc,
                        0.10f,
                        120,
                        3,
                        0,
                        0,
                        0,
                        0,
                        "necklace.png")
                },

                {
                    "watch",
                    new InventoryItem(
                        "watch",
                        "Wristwatch",
                        "An old but valuable wristwatch.",
                        ItemCategory.Misc,
                        0.15f,
                        65,
                        5,
                        0,
                        0,
                        0,
                        0,
                        "watch.png")
                },

                {
                    "brokenphone",
                    new InventoryItem(
                        "brokenphone",
                        "Broken Phone",
                        "A damaged phone containing reusable parts.",
                        ItemCategory.Misc,
                        0.25f,
                        35,
                        5,
                        0,
                        0,
                        0,
                        0,
                        "brokenphone.png")
                },

                // ============================================
                // STRESS RELIEVER
                // ============================================
                {
                    "cigarette",
                    new InventoryItem(
                        "cigarette",
                        "Cigarette",
                        "A cigarette that helps reduce stress.",
                         ItemCategory.StressReliever,
                         0.02f,
                            3,
                        20,
                        0,
                        0,
                        20,
                        0,
                        "cigarette.png")
                },

                {
                    "cigar",
                    new InventoryItem(
                        "cigar",
                        "Cigar",
                        "A strong cigar that greatly reduces stress.",
                        ItemCategory.StressReliever,
                        0.05f,
                        12,
                        10,
                        0,
                        0,
                        40,
                        0,
                        "cigar.png")
                },
            };

        public static InventoryItem GetItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            InventoryItem item;

            if (Items.TryGetValue(itemId.ToLower(), out item))
            {
                return item;
            }

            return null;
        }

        public static bool ContainsItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            return Items.ContainsKey(itemId.ToLower());
        }
    }
}