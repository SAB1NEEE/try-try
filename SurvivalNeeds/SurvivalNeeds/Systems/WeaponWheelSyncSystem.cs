using GTA;
using GTA.Native;
using SurvivalNeeds.Inventory;

namespace SurvivalNeeds.Systems
{
    public class WeaponWheelSyncSystem
    {
        private readonly InventoryManager inventory;

        public WeaponWheelSyncSystem(
            InventoryManager inventory)
        {
            this.inventory =
                inventory;
        }

        //====================================================
        // SYNC WEAPON WHEEL
        //====================================================

        public void Sync()
        {
            if (inventory == null ||
                inventory.Slots == null)
            {
                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            foreach (InventoryItem item
                in ItemDatabase.Items.Values)
            {
                if (item == null ||
                    !item.IsWeapon ||
                    item.WeaponHash ==
                        WeaponHash.Unarmed)
                {
                    continue;
                }

                InventorySlot weaponSlot =
                    GetWeaponSlot(
                        item.WeaponHash
                    );

                bool inventoryHasWeapon =
                    weaponSlot != null;

                bool wheelHasWeapon =
                    player.Weapons.HasWeapon(
                        item.WeaponHash
                    );

                //============================================
                // INVENTORY HAS WEAPON, WHEEL DOES NOT
                //============================================

                if (inventoryHasWeapon &&
                    !wheelHasWeapon)
                {
                    int ammoToRestore =
                        weaponSlot.Ammo;

                    if (ammoToRestore < 0)
                    {
                        ammoToRestore = 0;
                    }

                    // Give the weapon with zero ammo first.
                    player.Weapons.Give(
                        item.WeaponHash,
                        0,
                        false,
                        false
                    );

                    // Force GTA's ammunition to exactly
                    // match the ammo saved in the inventory slot.
                    Weapon restoredWeapon =
                        player.Weapons[
                            item.WeaponHash
                        ];

                    if (restoredWeapon != null)
                    {
                        restoredWeapon.Ammo =
                            ammoToRestore;
                    }

                    continue;
                }

                //============================================
                // INVENTORY AND WHEEL BOTH HAVE WEAPON
                //============================================

                if (inventoryHasWeapon &&
                    wheelHasWeapon)
                {
                    Weapon gtaWeapon =
                        player.Weapons[
                            item.WeaponHash
                        ];

                    if (gtaWeapon != null)
                    {
                        int currentAmmo =
                            gtaWeapon.Ammo;

                        if (currentAmmo < 0)
                        {
                            currentAmmo = 0;
                        }

                        if (weaponSlot.Ammo !=
                            currentAmmo)
                        {
                            weaponSlot.SetAmmo(
                                currentAmmo
                            );
                        }
                    }

                    continue;
                }

                //============================================
                // INVENTORY DOES NOT HAVE WEAPON
                //============================================

                if (!inventoryHasWeapon &&
                    wheelHasWeapon)
                {
                    Function.Call(
                        Hash.REMOVE_WEAPON_FROM_PED,
                        player.Handle,
                        (uint)item.WeaponHash
                    );
                }
            }
        }

        //====================================================
        // GET WEAPON SLOT
        //====================================================

        private InventorySlot GetWeaponSlot(
            WeaponHash weaponHash)
        {
            if (inventory == null ||
                inventory.Slots == null)
            {
                return null;
            }

            foreach (InventorySlot slot
                in inventory.Slots)
            {
                if (slot == null ||
                    slot.IsEmpty ||
                    slot.Item == null ||
                    slot.Quantity <= 0)
                {
                    continue;
                }

                if (!slot.Item.IsWeapon)
                {
                    continue;
                }

                if (slot.Item.WeaponHash ==
                    weaponHash)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}