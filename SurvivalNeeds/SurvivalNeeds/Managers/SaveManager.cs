using GTA;
using SurvivalNeeds.Inventory;
using SurvivalNeeds.WeaponInventory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SurvivalNeeds.Managers
{
    public class SaveManager
    {
        private readonly string saveDirectory;
        private readonly string inventorySavePath;
        private readonly string weaponInventorySavePath;

        public SaveManager()
        {
            saveDirectory =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "SurvivalNeeds"
                );

            inventorySavePath =
                Path.Combine(
                    saveDirectory,
                    "inventory.ini"
                );

            weaponInventorySavePath =
                Path.Combine(
                    saveDirectory,
                    "weapon_inventory.ini"
                );

            EnsureSaveDirectoryExists();
        }

        //====================================================
        // SAVE PLAYER INVENTORY
        //====================================================

        public void SaveInventory(
            InventoryManager inventory)
        {
            if (inventory == null ||
                inventory.Slots == null)
            {
                return;
            }

            EnsureSaveDirectoryExists();

            List<string> lines =
                new List<string>();

            lines.Add("[Inventory]");

            for (int i = 0;
                 i < inventory.Slots.Count;
                 i++)
            {
                InventorySlot slot =
                    inventory.Slots[i];

                if (slot == null ||
                    slot.IsEmpty ||
                    slot.Item == null ||
                    slot.Quantity <= 0)
                {
                    lines.Add(
                        "Slot" + i + "="
                    );

                    continue;
                }

                string itemId =
                    slot.Item.Id;

                int quantity =
                    slot.Quantity;

                lines.Add(
                    "Slot" +
                    i +
                    "=" +
                    itemId +
                    "|" +
                    quantity.ToString(
                        CultureInfo.InvariantCulture
                    )
                );
            }

            File.WriteAllLines(
                inventorySavePath,
                lines
            );
        }

        //====================================================
        // LOAD PLAYER INVENTORY
        //====================================================

        public bool LoadInventory(
            InventoryManager inventory)
        {
            if (inventory == null ||
                inventory.Slots == null)
            {
                return false;
            }

            if (!File.Exists(
                inventorySavePath))
            {
                return false;
            }

            string[] lines =
                File.ReadAllLines(
                    inventorySavePath
                );

            ClearInventory(
                inventory
            );

            bool loadedAnyItem =
                false;

            foreach (string rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(
                    rawLine))
                {
                    continue;
                }

                string line =
                    rawLine.Trim();

                if (line.StartsWith("[") ||
                    line.StartsWith("#") ||
                    line.StartsWith(";"))
                {
                    continue;
                }

                int equalsIndex =
                    line.IndexOf('=');

                if (equalsIndex <= 0)
                {
                    continue;
                }

                string key =
                    line.Substring(
                        0,
                        equalsIndex
                    ).Trim();

                string value =
                    line.Substring(
                        equalsIndex + 1
                    ).Trim();

                if (!key.StartsWith(
                    "Slot",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                    value))
                {
                    continue;
                }

                int slotIndex;

                string slotNumber =
                    key.Substring(4);

                if (!int.TryParse(
                    slotNumber,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out slotIndex))
                {
                    continue;
                }

                if (slotIndex < 0 ||
                    slotIndex >=
                    inventory.Slots.Count)
                {
                    continue;
                }

                string[] itemData =
                    value.Split('|');

                if (itemData.Length != 2)
                {
                    continue;
                }

                string itemId =
                    itemData[0]
                        .Trim()
                        .ToLowerInvariant();

                int quantity;

                if (!int.TryParse(
                    itemData[1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out quantity))
                {
                    continue;
                }

                if (quantity <= 0)
                {
                    continue;
                }

                InventoryItem item =
                    ItemDatabase.GetItem(
                        itemId
                    );

                if (item == null)
                {
                    continue;
                }

                if (quantity >
                    item.MaxStack)
                {
                    quantity =
                        item.MaxStack;
                }

                inventory.Slots[
                    slotIndex
                ].SetItem(
                    item,
                    quantity
                );

                loadedAnyItem =
                    true;
            }

            return loadedAnyItem;
        }

        //====================================================
        // SAVE WEAPON INVENTORY
        //====================================================

        public void SaveWeaponInventory(
            WeaponInventoryManager weaponInventory)
        {
            if (weaponInventory == null ||
                weaponInventory.Weapons == null)
            {
                return;
            }

            EnsureSaveDirectoryExists();

            List<string> lines =
                new List<string>();

            lines.Add("[WeaponInventory]");

            int index = 0;

            foreach (WeaponInventoryItem weapon
                in weaponInventory.Weapons)
            {
                if (weapon == null)
                    continue;

                lines.Add(
                    "Weapon" +
                    index +
                    "=" +
                    ((int)weapon.WeaponHash)
                        .ToString(
                            CultureInfo.InvariantCulture
                        ) +
                    "|" +
                    EncodeText(
                        weapon.Name
                    ) +
                    "|" +
                    weapon.Weight.ToString(
                        CultureInfo.InvariantCulture
                    ) +
                    "|" +
                    weapon.Ammo.ToString(
                        CultureInfo.InvariantCulture
                    )
                );

                index++;
            }

            File.WriteAllLines(
                weaponInventorySavePath,
                lines
            );
        }

        //====================================================
        // LOAD WEAPON INVENTORY
        //====================================================

        public bool LoadWeaponInventory(
            WeaponInventoryManager weaponInventory)
        {
            if (weaponInventory == null)
            {
                return false;
            }

            if (!File.Exists(
                weaponInventorySavePath))
            {
                return false;
            }

            string[] lines =
                File.ReadAllLines(
                    weaponInventorySavePath
                );

            List<WeaponInventoryItem>
                loadedWeapons =
                    new List<WeaponInventoryItem>();

            foreach (string rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(
                    rawLine))
                {
                    continue;
                }

                string line =
                    rawLine.Trim();

                if (line.StartsWith("[") ||
                    line.StartsWith("#") ||
                    line.StartsWith(";"))
                {
                    continue;
                }

                int equalsIndex =
                    line.IndexOf('=');

                if (equalsIndex <= 0)
                {
                    continue;
                }

                string key =
                    line.Substring(
                        0,
                        equalsIndex
                    ).Trim();

                string value =
                    line.Substring(
                        equalsIndex + 1
                    ).Trim();

                if (!key.StartsWith(
                    "Weapon",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                    value))
                {
                    continue;
                }

                string[] weaponData =
                    value.Split('|');

                if (weaponData.Length != 4)
                {
                    continue;
                }

                int weaponHashValue;

                if (!int.TryParse(
                    weaponData[0],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out weaponHashValue))
                {
                    continue;
                }

                string name =
                    DecodeText(
                        weaponData[1]
                    );

                float weight;

                if (!float.TryParse(
                    weaponData[2],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out weight))
                {
                    continue;
                }

                int ammo;

                if (!int.TryParse(
                    weaponData[3],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out ammo))
                {
                    continue;
                }

                if (weight < 0f)
                {
                    weight = 0f;
                }

                if (ammo < 0)
                {
                    ammo = 0;
                }

                WeaponInventoryItem weapon =
                    new WeaponInventoryItem(
                        (WeaponHash)weaponHashValue,
                        name,
                        weight,
                        ammo
                    );

                loadedWeapons.Add(
                    weapon
                );
            }

            weaponInventory.ReplaceAll(
                loadedWeapons
            );

            return loadedWeapons.Count > 0;
        }

        //====================================================
        // CLEAR PLAYER INVENTORY
        //====================================================

        private void ClearInventory(
            InventoryManager inventory)
        {
            foreach (InventorySlot slot
                in inventory.Slots)
            {
                if (slot != null)
                {
                    slot.Clear();
                }
            }
        }

        //====================================================
        // ENCODE TEXT
        //====================================================

        private string EncodeText(
            string value)
        {
            if (string.IsNullOrEmpty(
                value))
            {
                return string.Empty;
            }

            return Convert.ToBase64String(
                System.Text.Encoding.UTF8
                    .GetBytes(value)
            );
        }

        //====================================================
        // DECODE TEXT
        //====================================================

        private string DecodeText(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                return string.Empty;
            }

            try
            {
                byte[] bytes =
                    Convert.FromBase64String(
                        value
                    );

                return System.Text.Encoding.UTF8
                    .GetString(bytes);
            }
            catch
            {
                return value;
            }
        }

        //====================================================
        // SAVE DIRECTORY
        //====================================================

        private void EnsureSaveDirectoryExists()
        {
            if (!Directory.Exists(
                saveDirectory))
            {
                Directory.CreateDirectory(
                    saveDirectory
                );
            }
        }
    }
}