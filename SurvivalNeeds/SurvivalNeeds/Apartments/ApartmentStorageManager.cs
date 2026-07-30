using SurvivalNeeds.Inventory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SurvivalNeeds.Apartments
{
    public class ApartmentStorageManager
    {
        private const int StorageSlots = 30;
        private const float StorageMaximumWeight = 50f;

        private readonly string saveFolder;

        private readonly Dictionary<string, InventoryManager>
            storages =
                new Dictionary<string, InventoryManager>(
                    StringComparer.OrdinalIgnoreCase
                );

        public ApartmentStorageManager()
        {
            string baseFolder =
                AppDomain.CurrentDomain
                    .BaseDirectory
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar
                    );

            DirectoryInfo baseDirectory =
                new DirectoryInfo(
                    baseFolder
                );

            string scriptsFolder =
                baseDirectory.Name.Equals(
                    "scripts",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? baseDirectory.FullName
                    : Path.Combine(
                        baseDirectory.FullName,
                        "scripts"
                    );

            saveFolder =
                Path.Combine(
                    scriptsFolder,
                    "SurvivalNeeds",
                    "apartments"
                );

            Directory.CreateDirectory(
                saveFolder
            );
        }

        //====================================================
        // GET APARTMENT STORAGE
        //====================================================

        public InventoryManager GetStorage(
            string apartmentId,
            string profileId)
        {
            string storageKey =
                CreateStorageKey(
                    apartmentId,
                    profileId
                );

            InventoryManager storage;

            if (storages.TryGetValue(
                storageKey,
                out storage))
            {
                return storage;
            }

            storage =
                new InventoryManager(
                    StorageSlots,
                    StorageMaximumWeight
                );

            LoadStorage(
                apartmentId,
                profileId,
                storage
            );

            string capturedApartmentId =
                apartmentId;

            string capturedProfileId =
                profileId;

            storage.InventoryChanged +=
                delegate
                {
                    SaveStorage(
                        capturedApartmentId,
                        capturedProfileId,
                        storage
                    );
                };

            storages[
                storageKey
            ] = storage;

            return storage;
        }

        //====================================================
        // SAVE STORAGE
        //====================================================

        public void SaveStorage(
            string apartmentId,
            string profileId,
            InventoryManager storage)
        {
            if (storage == null ||
                storage.Slots == null)
            {
                return;
            }

            Directory.CreateDirectory(
                saveFolder
            );

            string savePath =
                GetSavePath(
                    apartmentId,
                    profileId
                );

            List<string> lines =
                new List<string>();

            lines.Add("[Inventory]");

            for (int i = 0;
                i < storage.Slots.Count;
                i++)
            {
                InventorySlot slot =
                    storage.Slots[i];

                if (slot == null ||
                    slot.IsEmpty ||
                    slot.Item == null ||
                    slot.Quantity <= 0)
                {
                    lines.Add(
                        "Slot" +
                        i +
                        "="
                    );

                    continue;
                }

                string value =
                    slot.Item.Id +
                    "|" +
                    slot.Quantity.ToString(
                        CultureInfo.InvariantCulture
                    );

                if (slot.Item.IsWeapon)
                {
                    value +=
                        "|" +
                        slot.Ammo.ToString(
                            CultureInfo.InvariantCulture
                        );
                }

                lines.Add(
                    "Slot" +
                    i +
                    "=" +
                    value
                );
            }

            File.WriteAllLines(
                savePath,
                lines
            );
        }

        //====================================================
        // LOAD STORAGE
        //====================================================

        private void LoadStorage(
            string apartmentId,
            string profileId,
            InventoryManager storage)
        {
            if (storage == null ||
                storage.Slots == null)
            {
                return;
            }

            string savePath =
                GetSavePath(
                    apartmentId,
                    profileId
                );

            if (!File.Exists(
                savePath))
            {
                return;
            }

            string[] lines =
                File.ReadAllLines(
                    savePath
                );

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
                    StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(
                        value))
                {
                    continue;
                }

                int slotIndex;

                if (!int.TryParse(
                    key.Substring(4),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out slotIndex))
                {
                    continue;
                }

                if (slotIndex < 0 ||
                    slotIndex >= storage.Slots.Count)
                {
                    continue;
                }

                string[] parts =
                    value.Split('|');

                if (parts.Length < 2 ||
                    parts.Length > 3)
                {
                    continue;
                }

                InventoryItem item =
                    ItemDatabase.GetItem(
                        parts[0]
                    );

                if (item == null)
                {
                    continue;
                }

                int quantity;

                if (!int.TryParse(
                    parts[1],
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

                if (quantity > item.MaxStack)
                {
                    quantity =
                        item.MaxStack;
                }

                if (item.IsWeapon)
                {
                    int ammo =
                        item.StartingAmmo;

                    if (parts.Length == 3)
                    {
                        int loadedAmmo;

                        if (int.TryParse(
                            parts[2],
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out loadedAmmo))
                        {
                            ammo =
                                loadedAmmo;
                        }
                    }

                    if (ammo < 0)
                    {
                        ammo = 0;
                    }

                    storage.Slots[
                        slotIndex
                    ].SetItem(
                        item,
                        quantity,
                        ammo
                    );
                }
                else
                {
                    storage.Slots[
                        slotIndex
                    ].SetItem(
                        item,
                        quantity
                    );
                }
            }
        }

        //====================================================
        // SAVE ALL
        //====================================================

        public void SaveAll()
        {
            foreach (
                KeyValuePair<string, InventoryManager>
                pair in storages)
            {
                string[] keyParts =
                    pair.Key.Split(
                        new[] { "::" },
                        StringSplitOptions.None
                    );

                if (keyParts.Length != 2)
                {
                    continue;
                }

                SaveStorage(
                    keyParts[0],
                    keyParts[1],
                    pair.Value
                );
            }
        }

        //====================================================
        // KEYS AND PATHS
        //====================================================

        private string CreateStorageKey(
            string apartmentId,
            string profileId)
        {
            return Normalize(
                apartmentId,
                "APARTMENT"
            ) +
            "::" +
            Normalize(
                profileId,
                "DEFAULT"
            );
        }

        private string GetSavePath(
            string apartmentId,
            string profileId)
        {
            return Path.Combine(
                saveFolder,
                "apartment_" +
                Normalize(
                    apartmentId,
                    "APARTMENT"
                ) +
                "_" +
                Normalize(
                    profileId,
                    "DEFAULT"
                ) +
                ".ini"
            );
        }

        private string Normalize(
            string value,
            string fallback)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                value =
                    fallback;
            }

            foreach (
                char invalidCharacter
                in Path.GetInvalidFileNameChars())
            {
                value =
                    value.Replace(
                        invalidCharacter,
                        '_'
                    );
            }

            return value;
        }
    }
}