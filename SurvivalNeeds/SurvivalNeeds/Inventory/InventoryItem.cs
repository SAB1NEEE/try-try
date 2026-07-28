namespace SurvivalNeeds.Inventory
{
    public class InventoryItem
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ItemCategory Category { get; set; }

        public float Weight { get; set; }

        public int Price { get; set; }

        public int MaxStack { get; set; }

        public float HungerRestore { get; set; }

        public float ThirstRestore { get; set; }

        public float StressRestore { get; set; }

        public float HealthRestore { get; set; }

        public string Icon { get; set; }

        public InventoryItem(
        string id,
        string name,
        string description,
        ItemCategory category,
        float weight,
        int price,
        int maxStack,
        float hungerRestore,
        float thirstRestore,
        float stressRestore,
        float healthRestore,
        string icon)
        {
            Id = id;
            Name = name;
            Description = description;
            Category = category;
            Weight = weight;
            Price = price;
            MaxStack = maxStack;

            HungerRestore = hungerRestore;
            ThirstRestore = thirstRestore;
            StressRestore = stressRestore;
            HealthRestore = healthRestore;

            Icon = icon;
        }
    }
}