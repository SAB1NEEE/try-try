using GTA;
using GTA.Native;
using SurvivalNeeds.Inventory;
using System;

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

                bool inventoryHasWeapon =
                    InventoryContainsWeapon(
                        item.WeaponHash
                    );

                bool wheelHasWeapon =
                    player.Weapons.HasWeapon(
                        item.WeaponHash
                    );

                // Weapon exists in mod inventory,
                // but is missing from GTA's weapon wheel.
                if (inventoryHasWeapon &&
                    !wheelHasWeapon)
                {
                    player.Weapons.Give(
                        item.WeaponHash,
                        item.StartingAmmo,
                        false,
                        false
                    );

                    continue;
                }

                // Weapon does not exist in mod inventory,
                // but GTA still has it.
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
        // INVENTORY CONTAINS WEAPON
        //====================================================

        private bool InventoryContainsWeapon(
            WeaponHash weaponHash)
        {
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
                    return true;
                }
            }

            return false;
        }
    }
}