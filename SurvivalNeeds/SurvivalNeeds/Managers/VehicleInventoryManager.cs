using GTA;
using GTA.Math;
using GTA.Native;
using SurvivalNeeds.Inventory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SurvivalNeeds.VehicleStorage
{
    public class VehicleInventoryManager
    {
        private const int DefaultTrunkSlots = 30;

        private readonly Dictionary<string, VehicleInventory> vehicles =
            new Dictionary<string, VehicleInventory>();

        private readonly Dictionary<string, string> physicalVehicleToClaimId =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase
            );

        private readonly Dictionary<string, ClaimedVehicleRecord> claimedVehicles =
            new Dictionary<string, ClaimedVehicleRecord>(
                StringComparer.OrdinalIgnoreCase
            );

        private readonly string vehicleSaveFolder;
        private readonly string claimedVehiclesSavePath;

        private int nextClaimNumber = 1;

        private Vehicle activePersonalVehicle;
        private Blip activePersonalVehicleBlip;
        private string activePersonalVehicleClaimId;

        public Dictionary<string, VehicleInventory> Vehicles
        {
            get
            {
                return vehicles;
            }
        }

        public VehicleInventoryManager()
        {
            string survivalNeedsFolder =
                GetSurvivalNeedsFolder();

            vehicleSaveFolder =
                Path.Combine(
                    survivalNeedsFolder,
                    "vehicles"
                );

            claimedVehiclesSavePath =
                Path.Combine(
                    survivalNeedsFolder,
                    "claimed_vehicles.ini"
                );

            Directory.CreateDirectory(
                vehicleSaveFolder
            );

            LoadClaimedVehicles();
        }

        //====================================================
        // CLAIM VEHICLE
        //====================================================

        public bool ClaimVehicle(
            Vehicle vehicle,
            out string claimId,
            out bool alreadyClaimed)
        {
            claimId = null;
            alreadyClaimed = false;

            if (vehicle == null ||
                !vehicle.Exists())
            {
                return false;
            }

            string currentClaimId =
                GetClaimId(vehicle);

            if (!string.IsNullOrWhiteSpace(
                currentClaimId))
            {
                claimId =
                    currentClaimId;

                alreadyClaimed =
                    true;

                ClaimedVehicleRecord existingRecord;

                if (claimedVehicles.TryGetValue(
                    currentClaimId,
                    out existingRecord))
                {
                    ReturnOtherClaimedVehiclesToGarage(
                        currentClaimId
                    );

                    SetActivePersonalVehicle(
                        vehicle,
                        currentClaimId,
                        existingRecord.Plate
                    );
                }

                return true;
            }

            /*
             * Save the original identity before changing
             * the vehicle's plate.
             */
            string oldVehicleKey =
                GetRawVehicleKey(
                    vehicle
                );

            VehicleInventory oldInventory =
                GetInventory(
                    vehicle
                );

            claimId =
                CreateNextClaimId();

            string permanentPlate =
                CreatePermanentPlate(
                    claimId
                );

            Function.Call(
                Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,
                vehicle.Handle,
                permanentPlate
            );

            string newPhysicalKey =
                GetRawVehicleKey(
                    vehicle
                );

            ClaimedVehicleRecord record =
                new ClaimedVehicleRecord();

            record.ClaimId =
                claimId;

            record.ModelHash =
                vehicle.Model.Hash;

            record.Plate =
                permanentPlate;

            claimedVehicles[
                claimId
            ] = record;

            physicalVehicleToClaimId[
                newPhysicalKey
            ] = claimId;

            VehicleInventory claimedInventory =
                new VehicleInventory(
                    claimId,
                    DefaultTrunkSlots
                );

            /*
             * Move the temporary trunk contents into
             * the permanent claimed inventory.
             */
            CopyInventory(
                oldInventory,
                claimedInventory
            );

            SubscribeToInventoryChanges(
                claimedInventory
            );

            vehicles[
                claimId
            ] = claimedInventory;

            SaveInventory(
                claimedInventory
            );

            SaveClaimedVehicles();

            /*
             * Remove the old temporary trunk file.
             */
            if (!string.IsNullOrWhiteSpace(
                oldVehicleKey))
            {
                vehicles.Remove(
                    oldVehicleKey
                );

                DeleteVehicleSave(
                    oldVehicleKey
                );
            }

            /*
             * Only one personal vehicle may be active.
             */
            ReturnOtherClaimedVehiclesToGarage(
                claimId
            );

            SetActivePersonalVehicle(
                vehicle,
                claimId,
                permanentPlate
            );

            return true;
        }

        //====================================================
        // UPDATE
        //====================================================

        public void Update()
        {
            if (activePersonalVehicle == null)
            {
                RemoveActiveVehicleBlip();

                activePersonalVehicleClaimId =
                    null;

                return;
            }

            if (!activePersonalVehicle.Exists() ||
                activePersonalVehicle.IsDead)
            {
                RemoveActiveVehicleBlip();

                activePersonalVehicle =
                    null;

                activePersonalVehicleClaimId =
                    null;
            }
        }

        //====================================================
        // CHECK CLAIMED VEHICLE
        //====================================================

        public bool IsClaimed(
            Vehicle vehicle)
        {
            return !string.IsNullOrWhiteSpace(
                GetClaimId(vehicle)
            );
        }

        public string GetClaimId(
            Vehicle vehicle)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return null;
            }

            string physicalKey =
                GetRawVehicleKey(
                    vehicle
                );

            string claimId;

            if (physicalVehicleToClaimId.TryGetValue(
                physicalKey,
                out claimId))
            {
                return claimId;
            }

            return null;
        }

        //====================================================
        // GET OR CREATE VEHICLE INVENTORY
        //====================================================

        public VehicleInventory GetInventory(
            Vehicle vehicle)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return null;
            }

            string vehicleKey =
                GetVehicleKey(
                    vehicle
                );

            if (string.IsNullOrWhiteSpace(
                vehicleKey))
            {
                return null;
            }

            VehicleInventory vehicleInventory;

            if (vehicles.TryGetValue(
                vehicleKey,
                out vehicleInventory))
            {
                return vehicleInventory;
            }

            vehicleInventory =
                new VehicleInventory(
                    vehicleKey,
                    DefaultTrunkSlots
                );

            LoadInventory(
                vehicleInventory
            );

            SubscribeToInventoryChanges(
                vehicleInventory
            );

            vehicles[
                vehicleKey
            ] = vehicleInventory;

            return vehicleInventory;
        }

        private void SubscribeToInventoryChanges(
            VehicleInventory vehicleInventory)
        {
            if (vehicleInventory == null ||
                vehicleInventory.Inventory == null)
            {
                return;
            }

            vehicleInventory.Inventory.InventoryChanged +=
                delegate
                {
                    SaveInventory(
                        vehicleInventory
                    );
                };
        }

        //====================================================
        // COPY INVENTORY
        //====================================================

        private void CopyInventory(
            VehicleInventory source,
            VehicleInventory destination)
        {
            if (source == null ||
                destination == null ||
                source.Inventory == null ||
                destination.Inventory == null)
            {
                return;
            }

            int slotCount =
                Math.Min(
                    source.Inventory.Slots.Count,
                    destination.Inventory.Slots.Count
                );

            for (int i = 0;
                i < slotCount;
                i++)
            {
                InventorySlot sourceSlot =
                    source.Inventory.Slots[i];

                if (sourceSlot == null ||
                    sourceSlot.IsEmpty ||
                    sourceSlot.Item == null ||
                    sourceSlot.Quantity <= 0)
                {
                    continue;
                }

                destination.Inventory
                    .Slots[i]
                    .SetItem(
                        sourceSlot.Item,
                        sourceSlot.Quantity
                    );
            }
        }

        //====================================================
        // SAVE ONE VEHICLE INVENTORY
        //====================================================

        public void SaveInventory(
            VehicleInventory vehicleInventory)
        {
            if (vehicleInventory == null ||
                vehicleInventory.Inventory == null ||
                string.IsNullOrWhiteSpace(
                    vehicleInventory.VehicleKey))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(
                    vehicleSaveFolder
                );

                string filePath =
                    GetVehicleSavePath(
                        vehicleInventory.VehicleKey
                    );

                List<string> lines =
                    new List<string>();

                lines.Add("[Vehicle]");

                lines.Add(
                    "Key=" +
                    vehicleInventory.VehicleKey
                );

                lines.Add(
                    string.Empty
                );

                lines.Add("[Inventory]");

                for (int i = 0;
                    i < vehicleInventory.Inventory.Slots.Count;
                    i++)
                {
                    InventorySlot slot =
                        vehicleInventory.Inventory.Slots[i];

                    string value =
                        string.Empty;

                    if (slot != null &&
                        !slot.IsEmpty &&
                        slot.Item != null &&
                        slot.Quantity > 0)
                    {
                        value =
                            slot.Item.Id +
                            "|" +
                            slot.Quantity.ToString(
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

                WriteFileSafely(
                    filePath,
                    lines
                );
            }
            catch
            {
                /*
                 * Saving failure should not crash GTA.
                 */
            }
        }

        //====================================================
        // SAVE ALL
        //====================================================

        public void SaveAll()
        {
            foreach (
                KeyValuePair<string, VehicleInventory>
                pair in vehicles)
            {
                SaveInventory(
                    pair.Value
                );
            }

            SaveClaimedVehicles();
        }

        //====================================================
        // LOAD VEHICLE INVENTORY
        //====================================================

        private void LoadInventory(
            VehicleInventory vehicleInventory)
        {
            if (vehicleInventory == null ||
                vehicleInventory.Inventory == null)
            {
                return;
            }

            string filePath =
                GetVehicleSavePath(
                    vehicleInventory.VehicleKey
                );

            if (!File.Exists(
                filePath))
            {
                return;
            }

            try
            {
                string[] lines =
                    File.ReadAllLines(
                        filePath
                    );

                foreach (
                    string rawLine in lines)
                {
                    if (string.IsNullOrWhiteSpace(
                        rawLine))
                    {
                        continue;
                    }

                    string line =
                        rawLine.Trim();

                    if (!line.StartsWith(
                        "Slot",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    int equalsIndex =
                        line.IndexOf('=');

                    if (equalsIndex <= 4)
                    {
                        continue;
                    }

                    string slotText =
                        line.Substring(
                            4,
                            equalsIndex - 4
                        );

                    int slotIndex;

                    if (!int.TryParse(
                        slotText,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out slotIndex))
                    {
                        continue;
                    }

                    if (slotIndex < 0 ||
                        slotIndex >=
                        vehicleInventory.Inventory.Slots.Count)
                    {
                        continue;
                    }

                    string value =
                        line.Substring(
                            equalsIndex + 1
                        ).Trim();

                    if (string.IsNullOrWhiteSpace(
                        value))
                    {
                        continue;
                    }

                    string[] parts =
                        value.Split('|');

                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    string itemId =
                        parts[0]
                            .Trim()
                            .ToLowerInvariant();

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

                    InventoryItem item =
                        ItemDatabase.GetItem(
                            itemId
                        );

                    if (item == null)
                    {
                        continue;
                    }

                    if (quantity > item.MaxStack)
                    {
                        quantity =
                            item.MaxStack;
                    }

                    vehicleInventory.Inventory
                        .Slots[slotIndex]
                        .SetItem(
                            item,
                            quantity
                        );
                }
            }
            catch
            {
                /*
                 * Leave inventory empty if loading fails.
                 */
            }
        }

        //====================================================
        // SAVE CLAIMED VEHICLES
        //====================================================

        private void SaveClaimedVehicles()
        {
            try
            {
                List<string> lines =
                    new List<string>();

                lines.Add("[Settings]");

                lines.Add(
                    "NextClaimNumber=" +
                    nextClaimNumber.ToString(
                        CultureInfo.InvariantCulture
                    )
                );

                lines.Add(
                    string.Empty
                );

                lines.Add("[ClaimedVehicles]");

                foreach (
                    KeyValuePair<string, ClaimedVehicleRecord>
                    pair in claimedVehicles)
                {
                    ClaimedVehicleRecord record =
                        pair.Value;

                    if (record == null)
                    {
                        continue;
                    }

                    lines.Add(
                        record.ClaimId +
                        "=" +
                        record.ModelHash.ToString(
                            CultureInfo.InvariantCulture
                        ) +
                        "|" +
                        record.Plate
                    );
                }

                WriteFileSafely(
                    claimedVehiclesSavePath,
                    lines
                );
            }
            catch
            {
                /*
                 * Ownership saving failure should not crash GTA.
                 */
            }
        }

        //====================================================
        // LOAD CLAIMED VEHICLES
        //====================================================

        private void LoadClaimedVehicles()
        {
            if (!File.Exists(
                claimedVehiclesSavePath))
            {
                return;
            }

            try
            {
                string[] lines =
                    File.ReadAllLines(
                        claimedVehiclesSavePath
                    );

                foreach (
                    string rawLine in lines)
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

                    if (key.Equals(
                        "NextClaimNumber",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        int loadedNextNumber;

                        if (int.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out loadedNextNumber))
                        {
                            if (loadedNextNumber > 0)
                            {
                                nextClaimNumber =
                                    loadedNextNumber;
                            }
                        }

                        continue;
                    }

                    if (!key.StartsWith(
                        "VEHICLE_",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string[] parts =
                        value.Split('|');

                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    int modelHash;

                    if (!int.TryParse(
                        parts[0],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out modelHash))
                    {
                        continue;
                    }

                    string plate =
                        parts[1]
                            .Trim()
                            .ToUpperInvariant();

                    if (string.IsNullOrWhiteSpace(
                        plate))
                    {
                        continue;
                    }

                    ClaimedVehicleRecord record =
                        new ClaimedVehicleRecord();

                    record.ClaimId =
                        key;

                    record.ModelHash =
                        modelHash;

                    record.Plate =
                        plate;

                    claimedVehicles[
                        record.ClaimId
                    ] = record;

                    string physicalKey =
                        CreateRawVehicleKey(
                            modelHash,
                            plate
                        );

                    physicalVehicleToClaimId[
                        physicalKey
                    ] = record.ClaimId;
                }
            }
            catch
            {
                claimedVehicles.Clear();

                physicalVehicleToClaimId.Clear();

                nextClaimNumber =
                    1;
            }
        }

        //====================================================
        // GET CLAIMED VEHICLE LIST
        //====================================================

        public List<ClaimedVehicleInfo> GetClaimedVehicles()
        {
            List<ClaimedVehicleInfo> result =
                new List<ClaimedVehicleInfo>();

            foreach (
                KeyValuePair<string, ClaimedVehicleRecord>
                pair in claimedVehicles)
            {
                ClaimedVehicleRecord record =
                    pair.Value;

                if (record == null)
                {
                    continue;
                }

                ClaimedVehicleInfo info =
                    new ClaimedVehicleInfo();

                info.ClaimId =
                    record.ClaimId;

                info.ModelHash =
                    record.ModelHash;

                info.Plate =
                    record.Plate;

                result.Add(
                    info
                );
            }

            result.Sort(
                delegate (
                    ClaimedVehicleInfo first,
                    ClaimedVehicleInfo second)
                {
                    return string.Compare(
                        first.ClaimId,
                        second.ClaimId,
                        StringComparison.OrdinalIgnoreCase
                    );
                }
            );

            return result;
        }

        //====================================================
        // FIND SPAWNED CLAIMED VEHICLE
        //====================================================

        public Vehicle FindSpawnedClaimedVehicle(
            string claimId)
        {
            if (string.IsNullOrWhiteSpace(
                claimId))
            {
                return null;
            }

            if (activePersonalVehicle != null &&
                activePersonalVehicle.Exists() &&
                string.Equals(
                    activePersonalVehicleClaimId,
                    claimId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return activePersonalVehicle;
            }

            Vehicle[] worldVehicles =
                World.GetAllVehicles();

            if (worldVehicles == null)
            {
                return null;
            }

            foreach (
                Vehicle vehicle in worldVehicles)
            {
                if (vehicle == null ||
                    !vehicle.Exists())
                {
                    continue;
                }

                string vehicleClaimId =
                    GetClaimId(
                        vehicle
                    );

                if (string.Equals(
                    vehicleClaimId,
                    claimId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return vehicle;
                }
            }

            return null;
        }

        //====================================================
        // SPAWN CLAIMED VEHICLE
        //====================================================

        public Vehicle SpawnClaimedVehicle(
            string claimId,
            out bool alreadySpawned)
        {
            alreadySpawned =
                false;

            if (string.IsNullOrWhiteSpace(
                claimId))
            {
                return null;
            }

            ClaimedVehicleRecord record;

            if (!claimedVehicles.TryGetValue(
                claimId,
                out record))
            {
                return null;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return null;
            }

            Vehicle existingRequestedVehicle =
                FindSpawnedClaimedVehicle(
                    claimId
                );

            if (existingRequestedVehicle != null &&
                existingRequestedVehicle.Exists())
            {
                SetActivePersonalVehicle(
                    existingRequestedVehicle,
                    claimId,
                    record.Plate
                );

                alreadySpawned =
                    true;

                return existingRequestedVehicle;
            }

            ReturnOtherClaimedVehiclesToGarage(
                claimId
            );

            Model vehicleModel =
                new Model(
                    record.ModelHash
                );

            if (!vehicleModel.IsInCdImage ||
                !vehicleModel.IsVehicle)
            {
                return null;
            }

            vehicleModel.Request(
                3000
            );

            if (!vehicleModel.IsLoaded)
            {
                vehicleModel.MarkAsNoLongerNeeded();

                return null;
            }

            Vector3 desiredPosition =
                player.Position +
                player.ForwardVector * 12.0f;

            Vector3 spawnPosition =
                World.GetNextPositionOnStreet(
                    desiredPosition
                );

            if (spawnPosition == Vector3.Zero)
            {
                spawnPosition =
                    desiredPosition;
            }

            Vehicle spawnedVehicle =
                World.CreateVehicle(
                    vehicleModel,
                    spawnPosition,
                    player.Heading
                );

            if (spawnedVehicle == null ||
                !spawnedVehicle.Exists())
            {
                vehicleModel.MarkAsNoLongerNeeded();

                return null;
            }

            Function.Call(
                Hash.SET_VEHICLE_NUMBER_PLATE_TEXT,
                spawnedVehicle.Handle,
                record.Plate
            );

            Function.Call<bool>(
                Hash.SET_VEHICLE_ON_GROUND_PROPERLY,
                spawnedVehicle.Handle
            );

            spawnedVehicle.IsPersistent =
                true;

            string physicalKey =
                GetRawVehicleKey(
                    spawnedVehicle
                );

            physicalVehicleToClaimId[
                physicalKey
            ] = claimId;

            /*
             * Reconnect the permanent trunk inventory.
             */
            GetInventory(
                spawnedVehicle
            );

            SetActivePersonalVehicle(
                spawnedVehicle,
                claimId,
                record.Plate
            );

            vehicleModel.MarkAsNoLongerNeeded();

            return spawnedVehicle;
        }

        //====================================================
        // RETURN OTHER VEHICLES TO GARAGE
        //====================================================

        private void ReturnOtherClaimedVehiclesToGarage(
            string requestedClaimId)
        {
            Vehicle[] worldVehicles =
                World.GetAllVehicles();

            if (worldVehicles != null)
            {
                foreach (
                    Vehicle vehicle in worldVehicles)
                {
                    if (vehicle == null ||
                        !vehicle.Exists())
                    {
                        continue;
                    }

                    string vehicleClaimId =
                        GetClaimId(
                            vehicle
                        );

                    if (string.IsNullOrWhiteSpace(
                        vehicleClaimId))
                    {
                        continue;
                    }

                    if (string.Equals(
                        vehicleClaimId,
                        requestedClaimId,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    VehicleInventory vehicleInventory =
                        GetInventory(
                            vehicle
                        );

                    if (vehicleInventory != null)
                    {
                        SaveInventory(
                            vehicleInventory
                        );
                    }

                    if (activePersonalVehicle != null &&
                        activePersonalVehicle.Exists() &&
                        vehicle.Handle ==
                        activePersonalVehicle.Handle)
                    {
                        RemoveActiveVehicleBlip();

                        activePersonalVehicle =
                            null;

                        activePersonalVehicleClaimId =
                            null;
                    }

                    vehicle.IsPersistent =
                        false;

                    vehicle.Delete();
                }
            }

            /*
             * Clean the active reference if the vehicle
             * was not returned by World.GetAllVehicles().
             */
            if (activePersonalVehicle != null &&
                activePersonalVehicle.Exists() &&
                !string.Equals(
                    activePersonalVehicleClaimId,
                    requestedClaimId,
                    StringComparison.OrdinalIgnoreCase))
            {
                VehicleInventory vehicleInventory =
                    GetInventory(
                        activePersonalVehicle
                    );

                if (vehicleInventory != null)
                {
                    SaveInventory(
                        vehicleInventory
                    );
                }

                RemoveActiveVehicleBlip();

                activePersonalVehicle.IsPersistent =
                    false;

                activePersonalVehicle.Delete();

                activePersonalVehicle =
                    null;

                activePersonalVehicleClaimId =
                    null;
            }
        }

        //====================================================
        // SET ACTIVE PERSONAL VEHICLE
        //====================================================

        private void SetActivePersonalVehicle(
            Vehicle vehicle,
            string claimId,
            string plate)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            bool sameVehicle =
                activePersonalVehicle != null &&
                activePersonalVehicle.Exists() &&
                activePersonalVehicle.Handle ==
                vehicle.Handle;

            activePersonalVehicle =
                vehicle;

            activePersonalVehicleClaimId =
                claimId;

            vehicle.IsPersistent =
                true;

            if (!sameVehicle ||
                activePersonalVehicleBlip == null ||
                !activePersonalVehicleBlip.Exists())
            {
                CreateActiveVehicleBlip(
                    vehicle,
                    plate
                );
            }
        }

        //====================================================
        // CREATE ACTIVE VEHICLE BLIP
        //====================================================

        private void CreateActiveVehicleBlip(
            Vehicle vehicle,
            string plate)
        {
            RemoveActiveVehicleBlip();

            if (vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            activePersonalVehicleBlip =
                vehicle.AddBlip();

            if (activePersonalVehicleBlip == null ||
                !activePersonalVehicleBlip.Exists())
            {
                return;
            }

            activePersonalVehicleBlip.Sprite =
                (BlipSprite)225;

            activePersonalVehicleBlip.Color =
                BlipColor.Blue;

            activePersonalVehicleBlip.Name =
                string.IsNullOrWhiteSpace(
                    plate)
                    ? "Personal Vehicle"
                    : plate;

            activePersonalVehicleBlip.IsShortRange =
                false;

            activePersonalVehicleBlip.Scale =
                0.85f;
        }

        //====================================================
        // REMOVE ACTIVE VEHICLE BLIP
        //====================================================

        private void RemoveActiveVehicleBlip()
        {
            if (activePersonalVehicleBlip != null &&
                activePersonalVehicleBlip.Exists())
            {
                activePersonalVehicleBlip.Delete();
            }

            activePersonalVehicleBlip =
                null;
        }

        //====================================================
        // VEHICLE IDENTITY
        //====================================================

        public string GetVehicleKey(
            Vehicle vehicle)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return null;
            }

            string rawVehicleKey =
                GetRawVehicleKey(
                    vehicle
                );

            string claimId;

            if (physicalVehicleToClaimId.TryGetValue(
                rawVehicleKey,
                out claimId))
            {
                return claimId;
            }

            return rawVehicleKey;
        }

        private string GetRawVehicleKey(
            Vehicle vehicle)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return null;
            }

            string plate =
                Function.Call<string>(
                    Hash.GET_VEHICLE_NUMBER_PLATE_TEXT,
                    vehicle.Handle
                );

            if (string.IsNullOrWhiteSpace(
                plate))
            {
                plate =
                    "NO_PLATE";
            }

            plate =
                plate.Trim()
                    .ToUpperInvariant();

            return CreateRawVehicleKey(
                vehicle.Model.Hash,
                plate
            );
        }

        private string CreateRawVehicleKey(
            int modelHash,
            string plate)
        {
            if (string.IsNullOrWhiteSpace(
                plate))
            {
                plate =
                    "NO_PLATE";
            }

            return modelHash.ToString(
                CultureInfo.InvariantCulture
            ) +
            "_" +
            plate.Trim()
                .ToUpperInvariant();
        }

        private string CreateNextClaimId()
        {
            string claimId;

            do
            {
                claimId =
                    "VEHICLE_" +
                    nextClaimNumber.ToString(
                        "0000",
                        CultureInfo.InvariantCulture
                    );

                nextClaimNumber++;
            }
            while (claimedVehicles.ContainsKey(
                claimId));

            return claimId;
        }

        private string CreatePermanentPlate(
            string claimId)
        {
            string numberText =
                claimId.Replace(
                    "VEHICLE_",
                    string.Empty
                );

            int number;

            if (!int.TryParse(
                numberText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out number))
            {
                number =
                    nextClaimNumber;
            }

            if (number > 99)
            {
                number =
                    99;
            }

            return "SAB1NE" +
                number.ToString(
                    "00",
                    CultureInfo.InvariantCulture
                );
        }

        //====================================================
        // DELETE TEMPORARY VEHICLE SAVE
        //====================================================

        private void DeleteVehicleSave(
            string vehicleKey)
        {
            try
            {
                string path =
                    GetVehicleSavePath(
                        vehicleKey
                    );

                if (File.Exists(
                    path))
                {
                    File.Delete(
                        path
                    );
                }
            }
            catch
            {
            }
        }

        //====================================================
        // SAVE PATHS
        //====================================================

        private string GetVehicleSavePath(
            string vehicleKey)
        {
            string safeFileName =
                MakeSafeFileName(
                    vehicleKey
                );

            return Path.Combine(
                vehicleSaveFolder,
                safeFileName + ".ini"
            );
        }

        private string GetSurvivalNeedsFolder()
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

            string scriptsFolder;

            if (baseDirectory.Name.Equals(
                "scripts",
                StringComparison.OrdinalIgnoreCase))
            {
                scriptsFolder =
                    baseDirectory.FullName;
            }
            else
            {
                scriptsFolder =
                    Path.Combine(
                        baseDirectory.FullName,
                        "scripts"
                    );
            }

            return Path.Combine(
                scriptsFolder,
                "SurvivalNeeds"
            );
        }

        private void WriteFileSafely(
            string filePath,
            List<string> lines)
        {
            string directory =
                Path.GetDirectoryName(
                    filePath
                );

            if (!string.IsNullOrWhiteSpace(
                directory))
            {
                Directory.CreateDirectory(
                    directory
                );
            }

            string temporaryPath =
                filePath + ".tmp";

            File.WriteAllLines(
                temporaryPath,
                lines.ToArray(),
                Encoding.UTF8
            );

            File.Copy(
                temporaryPath,
                filePath,
                true
            );

            File.Delete(
                temporaryPath
            );
        }

        private string MakeSafeFileName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                return "UNKNOWN_VEHICLE";
            }

            char[] invalidCharacters =
                Path.GetInvalidFileNameChars();

            StringBuilder result =
                new StringBuilder();

            foreach (
                char character in value)
            {
                bool invalid =
                    false;

                foreach (
                    char invalidCharacter
                    in invalidCharacters)
                {
                    if (character ==
                        invalidCharacter)
                    {
                        invalid =
                            true;

                        break;
                    }
                }

                result.Append(
                    invalid
                        ? '_'
                        : character
                );
            }

            return result.ToString();
        }

        //====================================================
        // CLAIMED VEHICLE RECORD
        //====================================================

        private class ClaimedVehicleRecord
        {
            public string ClaimId
            {
                get;
                set;
            }

            public int ModelHash
            {
                get;
                set;
            }

            public string Plate
            {
                get;
                set;
            }
        }
    }
}