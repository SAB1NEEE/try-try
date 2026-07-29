using GTA;
using System;
using System.Collections.Generic;

namespace SurvivalNeeds.WeaponInventory
{
    public class WeaponInventoryManager
    {
        private readonly List<WeaponInventoryItem>
            weapons =
                new List<WeaponInventoryItem>();

        public IReadOnlyList<WeaponInventoryItem>
            Weapons
        {
            get
            {
                return weapons;
            }
        }

        public event Action WeaponsChanged;

        //====================================================
        // ADD WEAPON
        //====================================================

        public bool AddWeapon(
            WeaponHash weaponHash,
            string name,
            float weight,
            int ammo)
        {
            if (HasWeapon(
                weaponHash))
            {
                return false;
            }

            WeaponInventoryItem weapon =
                new WeaponInventoryItem(
                    weaponHash,
                    name,
                    weight,
                    ammo
                );

            weapons.Add(
                weapon
            );

            NotifyWeaponsChanged();

            return true;
        }

        //====================================================
        // REMOVE WEAPON
        //====================================================

        public bool RemoveWeapon(
            WeaponHash weaponHash)
        {
            WeaponInventoryItem weapon =
                GetWeapon(
                    weaponHash
                );

            if (weapon == null)
                return false;

            weapons.Remove(
                weapon
            );

            NotifyWeaponsChanged();

            return true;
        }

        //====================================================
        // HAS WEAPON
        //====================================================

        public bool HasWeapon(
            WeaponHash weaponHash)
        {
            return GetWeapon(
                weaponHash
            ) != null;
        }

        //====================================================
        // GET WEAPON
        //====================================================

        public WeaponInventoryItem GetWeapon(
            WeaponHash weaponHash)
        {
            foreach (WeaponInventoryItem weapon
                in weapons)
            {
                if (weapon == null)
                    continue;

                if (weapon.WeaponHash ==
                    weaponHash)
                {
                    return weapon;
                }
            }

            return null;
        }

        //====================================================
        // UPDATE AMMO
        //====================================================

        public bool SetAmmo(
            WeaponHash weaponHash,
            int ammo)
        {
            WeaponInventoryItem weapon =
                GetWeapon(
                    weaponHash
                );

            if (weapon == null)
                return false;

            if (ammo < 0)
            {
                ammo = 0;
            }

            weapon.Ammo =
                ammo;

            NotifyWeaponsChanged();

            return true;
        }

        //====================================================
        // TOTAL WEIGHT
        //====================================================

        public float GetTotalWeight()
        {
            float totalWeight = 0f;

            foreach (WeaponInventoryItem weapon
                in weapons)
            {
                if (weapon == null)
                    continue;

                totalWeight +=
                    weapon.Weight;
            }

            return totalWeight;
        }

        //====================================================
        // CLEAR
        //====================================================

        public void Clear()
        {
            weapons.Clear();

            NotifyWeaponsChanged();
        }

        //====================================================
        // REPLACE ALL
        //====================================================

        public void ReplaceAll(
            IEnumerable<WeaponInventoryItem>
                loadedWeapons)
        {
            weapons.Clear();

            if (loadedWeapons != null)
            {
                foreach (WeaponInventoryItem weapon
                    in loadedWeapons)
                {
                    if (weapon == null)
                        continue;

                    if (HasWeapon(
                        weapon.WeaponHash))
                    {
                        continue;
                    }

                    weapons.Add(
                        new WeaponInventoryItem(
                            weapon.WeaponHash,
                            weapon.Name,
                            weapon.Weight,
                            weapon.Ammo
                        )
                    );
                }
            }

            NotifyWeaponsChanged();
        }

        //====================================================
        // SYNC TO GTA PLAYER
        //====================================================

        public void SyncToPlayer()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            foreach (WeaponInventoryItem weapon
                in weapons)
            {
                if (weapon == null)
                    continue;

                if (!player.Weapons.HasWeapon(
                    weapon.WeaponHash))
                {
                    player.Weapons.Give(
                        weapon.WeaponHash,
                        weapon.Ammo,
                        false,
                        false
                    );
                }
            }
        }

        //====================================================
        // NOTIFY
        //====================================================

        private void NotifyWeaponsChanged()
        {
            WeaponsChanged?.Invoke();
        }
    }
}