using GTA;

namespace SurvivalNeeds.Inventory
{
    public class InventoryItem
    {
        public string Id
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public ItemCategory Category
        {
            get;
            set;
        }

        public float Weight
        {
            get;
            set;
        }

        public int Price
        {
            get;
            set;
        }

        public int MaxStack
        {
            get;
            set;
        }

        public float HungerRestore
        {
            get;
            set;
        }

        public float ThirstRestore
        {
            get;
            set;
        }

        public float StressRestore
        {
            get;
            set;
        }

        public float HealthRestore
        {
            get;
            set;
        }

        public string Icon
        {
            get;
            set;
        }

        //====================================================
        // WEAPON DATA
        //====================================================

        public bool IsWeapon
        {
            get;
            set;
        }

        public WeaponHash WeaponHash
        {
            get;
            set;
        }

        public int StartingAmmo
        {
            get;
            set;
        }

        public bool IsMeleeWeapon
        {
            get;
            set;
        }

        //====================================================
        // NORMAL ITEM CONSTRUCTOR
        //====================================================

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

            HungerRestore =
                hungerRestore;

            ThirstRestore =
                thirstRestore;

            StressRestore =
                stressRestore;

            HealthRestore =
                healthRestore;

            Icon = icon;

            IsWeapon = false;
            WeaponHash = WeaponHash.Unarmed;
            StartingAmmo = 0;
            IsMeleeWeapon = false;
        }

        //====================================================
        // WEAPON ITEM CONSTRUCTOR
        //====================================================

        public InventoryItem(
            string id,
            string name,
            string description,
            ItemCategory category,
            float weight,
            int price,
            string icon,
            WeaponHash weaponHash,
            int startingAmmo,
            bool isMeleeWeapon = false)
        {
            Id = id;
            Name = name;
            Description = description;
            Category = category;
            Weight = weight;
            Price = price;

            // Weapons cannot stack.
            MaxStack = 1;

            HungerRestore = 0f;
            ThirstRestore = 0f;
            StressRestore = 0f;
            HealthRestore = 0f;

            Icon = icon;

            IsWeapon = true;
            WeaponHash = weaponHash;
            StartingAmmo = startingAmmo;
            IsMeleeWeapon = isMeleeWeapon;
        }
    }
}