using GTA;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SurvivalNeeds.Vendors
{
    public enum GunStoreCategory
    {
        Pistols,
        SubmachineGuns,
        Shotguns,
        Rifles,
        Melee
    }

    public class GunStoreItem
    {
        public string Name { get; }
        public string Description { get; }
        public GunStoreCategory Category { get; }
        public WeaponHash WeaponHash { get; }
        public int Price { get; }
        public int StartingAmmo { get; }
        public int AmmoPackAmount { get; }
        public int AmmoPackPrice { get; }
        public float Weight { get; }
        public string Icon { get; }
        public bool IsMelee { get; }

        public GunStoreItem(
            string name,
            string description,
            GunStoreCategory category,
            WeaponHash weaponHash,
            int price,
            int startingAmmo,
            int ammoPackAmount,
            int ammoPackPrice,
            float weight,
            string icon,
            bool isMelee = false)
        {
            Name = name;
            Description = description;
            Category = category;
            WeaponHash = weaponHash;
            Price = price;
            StartingAmmo = startingAmmo;
            AmmoPackAmount = ammoPackAmount;
            AmmoPackPrice = ammoPackPrice;
            Weight = weight;
            Icon = icon;
            IsMelee = isMelee;
        }
    }

    public class GunStore
    {
        private readonly List<GunStoreItem> items;

        public IReadOnlyList<GunStoreItem> Items
        {
            get
            {
                return items;
            }
        }

        public GunStore()
        {
            items = new List<GunStoreItem>();

            CreateStoreInventory();
        }

        //====================================================
        // STORE INVENTORY
        //====================================================

        private void CreateStoreInventory()
        {
            //================================================
            // PISTOLS
            //================================================

            items.Add(
                new GunStoreItem(
                    "Pistol",
                    "A standard semi-automatic handgun.",
                    GunStoreCategory.Pistols,
                    WeaponHash.Pistol,
                    500,
                    36,
                    24,
                    60,
                    1.00f,
                    "weapon_pistol.png"
                )
            );

            items.Add(
                new GunStoreItem(
                    "Combat Pistol",
                    "A reliable handgun designed for combat use.",
                    GunStoreCategory.Pistols,
                    WeaponHash.CombatPistol,
                    850,
                    36,
                    24,
                    75,
                    1.10f,
                    "weapon_combatpistol.png"
                )
            );

            //================================================
            // SUBMACHINE GUNS
            //================================================

            items.Add(
                new GunStoreItem(
                    "Micro SMG",
                    "A compact automatic weapon with a high rate of fire.",
                    GunStoreCategory.SubmachineGuns,
                    WeaponHash.MicroSMG,
                    2200,
                    90,
                    60,
                    180,
                    2.30f,
                    "weapon_microsmg.png"
                )
            );

            items.Add(
                new GunStoreItem(
                    "SMG",
                    "A full-size submachine gun suitable for close combat.",
                    GunStoreCategory.SubmachineGuns,
                    WeaponHash.SMG,
                    3200,
                    120,
                    60,
                    200,
                    2.80f,
                    "weapon_smg.png"
                )
            );

            //================================================
            // SHOTGUNS
            //================================================

            items.Add(
                new GunStoreItem(
                    "Pump Shotgun",
                    "A powerful pump-action shotgun.",
                    GunStoreCategory.Shotguns,
                    WeaponHash.PumpShotgun,
                    2800,
                    24,
                    12,
                    120,
                    3.60f,
                    "weapon_pumpshotgun.png"
                )
            );

            //================================================
            // RIFLES
            //================================================

            items.Add(
                new GunStoreItem(
                    "Assault Rifle",
                    "An automatic rifle with good stopping power.",
                    GunStoreCategory.Rifles,
                    WeaponHash.AssaultRifle,
                    6500,
                    120,
                    60,
                    240,
                    3.80f,
                    "weapon_assaultrifle.png"
                )
            );

            items.Add(
                new GunStoreItem(
                    "Carbine Rifle",
                    "An accurate automatic rifle with balanced performance.",
                    GunStoreCategory.Rifles,
                    WeaponHash.CarbineRifle,
                    7800,
                    120,
                    60,
                    260,
                    3.50f,
                    "weapon_carbinerifle.png"
                )
            );

            //================================================
            // MELEE
            //================================================

            items.Add(
                new GunStoreItem(
                    "Knife",
                    "A small and concealable melee weapon.",
                    GunStoreCategory.Melee,
                    WeaponHash.Knife,
                    150,
                    0,
                    0,
                    0,
                    0.40f,
                    "weapon_knife.png",
                    true
                )
            );

            items.Add(
                new GunStoreItem(
                    "Baseball Bat",
                    "A sturdy wooden baseball bat.",
                    GunStoreCategory.Melee,
                    WeaponHash.Bat,
                    200,
                    0,
                    0,
                    0,
                    1.20f,
                    "weapon_bat.png",
                    true
                )
            );

            items.Add(
                new GunStoreItem(
                    "Crowbar",
                    "A heavy metal crowbar.",
                    GunStoreCategory.Melee,
                    WeaponHash.Crowbar,
                    250,
                    0,
                    0,
                    0,
                    1.80f,
                    "weapon_crowbar.png",
                    true
                )
            );
        }

        //====================================================
        // GET ITEMS BY CATEGORY
        //====================================================

        public List<GunStoreItem> GetItemsByCategory(
            GunStoreCategory category)
        {
            return items
                .Where(
                    item =>
                        item != null &&
                        item.Category == category
                )
                .ToList();
        }

        //====================================================
        // GET ITEM
        //====================================================

        public GunStoreItem GetItem(
            WeaponHash weaponHash)
        {
            return items.FirstOrDefault(
                item =>
                    item != null &&
                    item.WeaponHash == weaponHash
            );
        }

        //====================================================
        // PLAYER OWNS WEAPON
        //====================================================

        public bool PlayerOwnsWeapon(
            GunStoreItem item)
        {
            if (item == null)
                return false;

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return false;
            }

            return player.Weapons.HasWeapon(
                item.WeaponHash
            );
        }

        //====================================================
        // PURCHASE WEAPON
        //====================================================

        public bool PurchaseWeapon(
            GunStoreItem item,
            Func<int, bool> trySpendMoney,
            Action saveAfterPurchase,
            out string resultMessage)
        {
            if (item == null)
            {
                resultMessage =
                    "Invalid gun store item.";

                return false;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                resultMessage =
                    "The player is unavailable.";

                return false;
            }

            if (PlayerOwnsWeapon(item))
            {
                resultMessage =
                    "You already own the " +
                    item.Name +
                    ".";

                return false;
            }

            if (trySpendMoney == null)
            {
                resultMessage =
                    "The money system is unavailable.";

                return false;
            }

            bool paymentSuccessful =
                trySpendMoney(
                    item.Price
                );

            if (!paymentSuccessful)
            {
                resultMessage =
                    "You cannot afford the " +
                    item.Name +
                    ".";

                return false;
            }

            try
            {
                player.Weapons.Give(
                    item.WeaponHash,
                    item.StartingAmmo,
                    false,
                    true
                );

                saveAfterPurchase?.Invoke();

                resultMessage =
                    "Purchased " +
                    item.Name +
                    " for $" +
                    item.Price.ToString("N0") +
                    ".";

                return true;
            }
            catch (Exception exception)
            {
                resultMessage =
                    "Weapon purchase failed: " +
                    exception.Message;

                return false;
            }
        }

        //====================================================
        // PURCHASE AMMUNITION
        //====================================================

        public bool PurchaseAmmo(
            GunStoreItem item,
            Func<int, bool> trySpendMoney,
            out int newAmmoAmount,
            out string resultMessage)
        {
            newAmmoAmount = 0;

            if (item == null)
            {
                resultMessage =
                    "Invalid gun store item.";

                return false;
            }

            if (item.IsMelee)
            {
                resultMessage =
                    item.Name +
                    " does not use ammunition.";

                return false;
            }

            if (item.AmmoPackAmount <= 0 ||
                item.AmmoPackPrice <= 0)
            {
                resultMessage =
                    "Ammunition is unavailable for " +
                    item.Name +
                    ".";

                return false;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                resultMessage =
                    "The player is unavailable.";

                return false;
            }

            if (!player.Weapons.HasWeapon(
                item.WeaponHash))
            {
                resultMessage =
                    "You do not own the " +
                    item.Name +
                    ".";

                return false;
            }

            if (trySpendMoney == null)
            {
                resultMessage =
                    "The money system is unavailable.";

                return false;
            }

            bool paymentSuccessful =
                trySpendMoney(
                    item.AmmoPackPrice
                );

            if (!paymentSuccessful)
            {
                resultMessage =
                    "You cannot afford this ammunition.";

                return false;
            }

            try
            {
                Weapon weapon =
                    player.Weapons[
                        item.WeaponHash
                    ];

                if (weapon == null)
                {
                    resultMessage =
                        "The weapon is unavailable.";

                    return false;
                }

                int currentAmmo =
                    weapon.Ammo;

                newAmmoAmount =
                    currentAmmo +
                    item.AmmoPackAmount;

                weapon.Ammo =
                    newAmmoAmount;

                resultMessage =
                    "Purchased " +
                    item.AmmoPackAmount +
                    " rounds for $" +
                    item.AmmoPackPrice.ToString("N0") +
                    ".";

                return true;
            }
            catch (Exception exception)
            {
                resultMessage =
                    "Ammunition purchase failed: " +
                    exception.Message;

                return false;
            }
        }
    }
}