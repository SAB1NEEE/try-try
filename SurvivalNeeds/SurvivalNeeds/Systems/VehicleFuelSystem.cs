using GTA;
using GTA.Native;
using GTA.UI;
using SurvivalNeeds.VehicleStorage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SurvivalNeeds.Systems
{
    public class VehicleFuelSystem
    {
        private readonly Dictionary<int, float> vehicleFuel =
            new Dictionary<int, float>();

        private readonly Dictionary<int, string> handleVehicleKeys =
            new Dictionary<int, string>();

        private readonly Dictionary<string, float> savedVehicleFuel =
            new Dictionary<string, float>(
                StringComparer.OrdinalIgnoreCase
            );

        private readonly HashSet<int> gasPumpModels =
            new HashSet<int>();

        private readonly MoneySystem money;

        private readonly VehicleInventoryManager
            vehicleInventoryManager;

        private readonly string fuelSavePath;

        private int lastUpdateTime;
        private int lastEmptyWarningTime;
        private int lastNoMoneyWarningTime;
        private int lastEmptyJerryCanWarningTime;

        private float pendingFuelAmount;

        private const float MaximumFuel = 100f;

        private const float RefuelDistance = 3.0f;
        private const float RefuelRatePerSecond = 12f;

        private const int RefuelPricePerPercent = 2;

        private const float JerryCanRefuelDistance = 3.0f;
        private const float JerryCanFuelPerSecond = 2.0f;

        private const int JerryCanUnitsPerFuelPercent = 180;
        private const int MaximumJerryCanUnits = 4500;

        // Current fuel-drain values.
        private const float IdleConsumption = 0.01f;
        private const float DrivingConsumption = 0.04f;
        private const float HighSpeedConsumption = 0.08f;

        public VehicleFuelSystem(
            MoneySystem money,
            VehicleInventoryManager vehicleInventoryManager)
        {
            this.money =
                money;

            this.vehicleInventoryManager =
                vehicleInventoryManager;

            fuelSavePath =
                Path.Combine(
                    GetSurvivalNeedsFolder(),
                    "vehicle_fuel.ini"
                );

            lastUpdateTime =
                Game.GameTime;

            RegisterGasPumpModels();
            LoadAll();
        }

        //====================================================
        // REGISTER GAS PUMPS
        //====================================================

        private void RegisterGasPumpModels()
        {
            AddGasPumpModel("prop_gas_pump_1c");
            AddGasPumpModel("prop_gas_pump_1a");
            AddGasPumpModel("prop_gas_pump_old2");
            AddGasPumpModel("prop_gas_pump_old3");
            AddGasPumpModel("prop_gas_pump_1d");
            AddGasPumpModel("prop_gas_pump_1b");
        }

        private void AddGasPumpModel(
            string modelName)
        {
            gasPumpModels.Add(
                Game.GenerateHash(
                    modelName
                )
            );
        }

        //====================================================
        // UPDATE
        //====================================================

        public void Update()
        {
            int currentTime =
                Game.GameTime;

            int elapsedMilliseconds =
                currentTime -
                lastUpdateTime;

            lastUpdateTime =
                currentTime;

            if (elapsedMilliseconds <= 0)
            {
                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                pendingFuelAmount = 0f;
                return;
            }

            /*
             * Allow the player to refuel a nearby vehicle
             * with a jerry can while standing outside.
             */
            if (!player.IsInVehicle())
            {
                UpdateJerryCanRefueling(
                    player,
                    elapsedMilliseconds
                );

                return;
            }

            Vehicle vehicle =
                player.CurrentVehicle;

            if (vehicle == null ||
                !vehicle.Exists())
            {
                pendingFuelAmount = 0f;
                return;
            }

            EnsureVehicleRegistered(
                vehicle
            );

            bool refueling =
                UpdateRefueling(
                    vehicle,
                    elapsedMilliseconds
                );

            if (refueling)
            {
                return;
            }

            int vehicleHandle =
                vehicle.Handle;

            float currentFuel =
                vehicleFuel[
                    vehicleHandle
                ];

            if (currentFuel <= 0f)
            {
                SetFuel(
                    vehicle,
                    0f
                );

                StopVehicleEngine(
                    vehicle
                );

                ShowEmptyWarning();

                return;
            }

            if (!vehicle.IsEngineRunning)
            {
                return;
            }

            float elapsedSeconds =
                elapsedMilliseconds /
                1000f;

            float consumption =
                GetFuelConsumption(
                    vehicle
                );

            currentFuel -=
                consumption *
                elapsedSeconds;

            SetFuel(
                vehicle,
                currentFuel
            );

            if (GetFuel(vehicle) <= 0f)
            {
                StopVehicleEngine(
                    vehicle
                );

                ShowEmptyWarning();
            }
        }

        //====================================================
        // REFUELING
        //====================================================

        private bool UpdateRefueling(
            Vehicle vehicle,
            int elapsedMilliseconds)
        {
            Prop nearestPump =
                FindNearestGasPump(
                    vehicle.Position
                );

            if (nearestPump == null ||
                !nearestPump.Exists())
            {
                pendingFuelAmount = 0f;
                return false;
            }

            float fuel =
                GetFuel(
                    vehicle
                );

            if (fuel >= MaximumFuel)
            {
                pendingFuelAmount = 0f;

                GTA.UI.Screen
                    .ShowHelpTextThisFrame(
                        "~g~Fuel tank is full"
                    );

                return false;
            }

            int fullRefuelPrice =
                (int)Math.Ceiling(
                    (MaximumFuel - fuel) *
                    RefuelPricePerPercent
                );

            GTA.UI.Screen
                .ShowHelpTextThisFrame(
                    "Hold ~y~ENTER~s~to refuel" +
                    "~n~Price: ~g~$" +
                    RefuelPricePerPercent +
                    "~s~ per 1%" +
                    "~n~Cost to fill: ~g~$" +
                    fullRefuelPrice
                );

            bool enterPressed =
                Game.IsKeyPressed(
        System.Windows.Forms.Keys.Enter
                );

            if (!enterPressed)
            {
                pendingFuelAmount = 0f;
                return false;
            }

            Function.Call(
                Hash.SET_VEHICLE_ENGINE_ON,
                vehicle.Handle,
                false,
                true,
                true
            );

            float elapsedSeconds =
                elapsedMilliseconds /
                1000f;

            pendingFuelAmount +=
                RefuelRatePerSecond *
                elapsedSeconds;

            int wholeFuelUnits =
                (int)pendingFuelAmount;

            if (wholeFuelUnits <= 0)
            {
                ShowRefuelingStatus(
                    vehicle
                );

                return true;
            }

            float remainingCapacity =
                MaximumFuel -
                fuel;

            int capacityUnits =
                (int)Math.Ceiling(
                    remainingCapacity
                );

            if (wholeFuelUnits >
                capacityUnits)
            {
                wholeFuelUnits =
                    capacityUnits;
            }

            if (wholeFuelUnits <= 0)
            {
                pendingFuelAmount = 0f;
                return true;
            }

            int affordableFuelUnits =
                money == null
                    ? 0
                    : money.Cash /
                        RefuelPricePerPercent;

            if (affordableFuelUnits <= 0)
            {
                pendingFuelAmount = 0f;

                ShowNoMoneyWarning();

                return false;
            }

            if (wholeFuelUnits >
                affordableFuelUnits)
            {
                wholeFuelUnits =
                    affordableFuelUnits;
            }

            int amountToCharge =
                wholeFuelUnits *
                RefuelPricePerPercent;

            if (money == null ||
                !money.TrySpendMoney(
                    amountToCharge
                ))
            {
                pendingFuelAmount = 0f;

                ShowNoMoneyWarning();

                return false;
            }

            pendingFuelAmount -=
                wholeFuelUnits;

            SetFuel(
                vehicle,
                fuel +
                wholeFuelUnits
            );

            Function.Call(
                Hash.SET_VEHICLE_UNDRIVEABLE,
                vehicle.Handle,
                false
            );

            ShowRefuelingStatus(
                vehicle
            );

            return true;
        }

        private void ShowRefuelingStatus(
            Vehicle vehicle)
        {
            float fuel =
                GetFuel(
                    vehicle
                );

            int currentFuel =
                (int)Math.Round(
                    fuel
                );

            int remainingPrice =
                (int)Math.Ceiling(
                    (MaximumFuel - fuel) *
                    RefuelPricePerPercent
                );

            GTA.UI.Screen.ShowSubtitle(
                "Refueling... " +
                currentFuel +
                "%~n~Remaining cost: ~g~$" +
                remainingPrice,
                500
            );
        }

        //====================================================
        // JERRY CAN REFUELING
        //====================================================

        private void UpdateJerryCanRefueling(
            Ped player,
            int elapsedMilliseconds)
        {
            if (player == null ||
                !player.Exists() ||
                player.IsInVehicle())
            {
                return;
            }

            uint selectedWeapon =
                Function.Call<uint>(
                    Hash.GET_SELECTED_PED_WEAPON,
                    player.Handle
                );

            uint petrolCanHash =
                unchecked(
                    (uint)WeaponHash.PetrolCan
                );

            if (selectedWeapon !=
                petrolCanHash)
            {
                return;
            }

            Vehicle nearestVehicle =
                FindNearestVehicle(
                    player.Position,
                    JerryCanRefuelDistance
                );

            if (nearestVehicle == null ||
                !nearestVehicle.Exists())
            {
                return;
            }

            float currentFuel =
                GetFuel(
                    nearestVehicle
                );

            if (currentFuel >= MaximumFuel)
            {
                GTA.UI.Screen
                    .ShowHelpTextThisFrame(
                        "~g~Vehicle fuel tank is full"
                    );

                return;
            }

            int jerryCanUnits =
                Function.Call<int>(
                    Hash.GET_AMMO_IN_PED_WEAPON,
                    player.Handle,
                    petrolCanHash
                );

            if (jerryCanUnits <= 0)
            {
                ShowEmptyJerryCanWarning();
                return;
            }

            int possibleFuelPercent =
                jerryCanUnits /
                JerryCanUnitsPerFuelPercent;

            GTA.UI.Screen
                .ShowHelpTextThisFrame(
                    "Hold ~y~ENTER~s~ to refuel vehicle" +
                    "~n~Jerry can: ~y~" +
                    jerryCanUnits +
                    "~s~ / " +
                    MaximumJerryCanUnits +
                    " units" +
                    "~n~Available fuel: ~g~" +
                    possibleFuelPercent +
                    "%"
                );

            bool enterPressed =
                Game.IsKeyPressed(
                System.Windows.Forms.Keys.Enter
            );

            if (!enterPressed)
            {
                pendingFuelAmount = 0f;
                return;
            }

            float elapsedSeconds =
                elapsedMilliseconds /
                1000f;

            pendingFuelAmount +=
                JerryCanFuelPerSecond *
                elapsedSeconds;

            int wholeFuelPercent =
                (int)pendingFuelAmount;

            if (wholeFuelPercent <= 0)
            {
                GTA.UI.Screen.ShowSubtitle(
                    "Refueling with jerry can...",
                    500
                );

                return;
            }

            int availableFuelPercent =
                jerryCanUnits /
                JerryCanUnitsPerFuelPercent;

            if (wholeFuelPercent >
                availableFuelPercent)
            {
                wholeFuelPercent =
                    availableFuelPercent;
            }

            int remainingVehicleCapacity =
                (int)Math.Ceiling(
                    MaximumFuel -
                    currentFuel
                );

            if (wholeFuelPercent >
                remainingVehicleCapacity)
            {
                wholeFuelPercent =
                    remainingVehicleCapacity;
            }

            if (wholeFuelPercent <= 0)
            {
                pendingFuelAmount = 0f;

                ShowEmptyJerryCanWarning();

                return;
            }

            int unitsToConsume =
                wholeFuelPercent *
                JerryCanUnitsPerFuelPercent;

            int newJerryCanUnits =
                jerryCanUnits -
                unitsToConsume;

            if (newJerryCanUnits < 0)
            {
                newJerryCanUnits = 0;
            }

            Function.Call(
                Hash.SET_PED_AMMO,
                player.Handle,
                petrolCanHash,
                newJerryCanUnits
            );

            AddFuel(
                nearestVehicle,
                wholeFuelPercent
            );

            pendingFuelAmount -=
                wholeFuelPercent;

            int displayedVehicleFuel =
                (int)Math.Round(
                    GetFuel(
                        nearestVehicle
                    )
                );

            GTA.UI.Screen.ShowSubtitle(
                "Refueling... " +
                displayedVehicleFuel +
                "%~n~Jerry can: " +
                newJerryCanUnits +
                " / " +
                MaximumJerryCanUnits,
                500
            );
        }

        //====================================================
        // FIND NEAREST VEHICLE
        //====================================================

        private Vehicle FindNearestVehicle(
            GTA.Math.Vector3 position,
            float maximumDistance)
        {
            Vehicle nearestVehicle =
                null;

            float nearestDistance =
                maximumDistance;

            Vehicle[] vehicles =
                World.GetAllVehicles();

            if (vehicles == null)
            {
                return null;
            }

            foreach (Vehicle vehicle
                in vehicles)
            {
                if (vehicle == null ||
                    !vehicle.Exists() ||
                    vehicle.IsDead)
                {
                    continue;
                }

                float distance =
                    position.DistanceTo(
                        vehicle.Position
                    );

                if (distance <
                    nearestDistance)
                {
                    nearestDistance =
                        distance;

                    nearestVehicle =
                        vehicle;
                }
            }

            return nearestVehicle;
        }



        //====================================================
        // FIND NEAREST GAS PUMP
        //====================================================

        private Prop FindNearestGasPump(
            GTA.Math.Vector3 position)
        {
            Prop nearestPump =
                null;

            float nearestDistance =
                RefuelDistance;

            foreach (
                Prop prop in World.GetAllProps())
            {
                if (prop == null ||
                    !prop.Exists())
                {
                    continue;
                }

                if (!gasPumpModels.Contains(
                    prop.Model.Hash
                ))
                {
                    continue;
                }

                float distance =
                    position.DistanceTo(
                        prop.Position
                    );

                if (distance <
                    nearestDistance)
                {
                    nearestDistance =
                        distance;

                    nearestPump =
                        prop;
                }
            }

            return nearestPump;
        }

        //====================================================
        // GET FUEL
        //====================================================

        public float GetFuel(
            Vehicle vehicle)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return 0f;
            }

            EnsureVehicleRegistered(
                vehicle
            );

            return vehicleFuel[
                vehicle.Handle
            ];
        }

        //====================================================
        // SET FUEL
        //====================================================

        public void SetFuel(
            Vehicle vehicle,
            float amount)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            EnsureVehicleRegistered(
                vehicle
            );

            float clampedFuel =
                ClampFuel(
                    amount
                );

            vehicleFuel[
                vehicle.Handle
            ] = clampedFuel;

            string vehicleKey =
                GetPersistentVehicleKey(
                    vehicle
                );

            if (!string.IsNullOrWhiteSpace(
                vehicleKey))
            {
                savedVehicleFuel[
                    vehicleKey
                ] = clampedFuel;

                handleVehicleKeys[
                    vehicle.Handle
                ] = vehicleKey;
            }

            if (clampedFuel > 0f)
            {
                Function.Call(
                    Hash.SET_VEHICLE_UNDRIVEABLE,
                    vehicle.Handle,
                    false
                );

                Function.Call(
                    Hash.SET_VEHICLE_ENGINE_ON,
                    vehicle.Handle,
                    true,
                    true,
                    false
                );

                vehicle.IsEngineRunning =
                    true;
            }
        }

        //====================================================
        // ADD FUEL
        //====================================================

        public void AddFuel(
            Vehicle vehicle,
            float amount)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            SetFuel(
                vehicle,
                GetFuel(vehicle) +
                amount
            );
        }

        //====================================================
        // REGISTER VEHICLE
        //====================================================

        private void EnsureVehicleRegistered(
            Vehicle vehicle)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            int handle =
                vehicle.Handle;

            string currentVehicleKey =
                GetPersistentVehicleKey(
                    vehicle
                );

            string previousVehicleKey;

            bool handleAlreadyRegistered =
                vehicleFuel.ContainsKey(
                    handle
                );

            bool keyAlreadyRegistered =
                handleVehicleKeys.TryGetValue(
                    handle,
                    out previousVehicleKey
                );

            /*
             * The vehicle may have just been claimed.
             * Its key changes from model/plate to VEHICLE_0001.
             * Move its current fuel to the new permanent key.
             */
            if (handleAlreadyRegistered &&
                keyAlreadyRegistered &&
                !string.Equals(
                    previousVehicleKey,
                    currentVehicleKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                float currentFuel =
                    vehicleFuel[
                        handle
                    ];

                handleVehicleKeys[
                    handle
                ] = currentVehicleKey;

                savedVehicleFuel[
                    currentVehicleKey
                ] = currentFuel;

                return;
            }

            if (handleAlreadyRegistered)
            {
                if (!keyAlreadyRegistered)
                {
                    handleVehicleKeys[
                        handle
                    ] = currentVehicleKey;
                }

                return;
            }

            float startingFuel;

            if (!string.IsNullOrWhiteSpace(
                    currentVehicleKey) &&
                savedVehicleFuel.TryGetValue(
                    currentVehicleKey,
                    out startingFuel))
            {
                startingFuel =
                    ClampFuel(
                        startingFuel
                    );
            }
            else
            {
                startingFuel =
                    GetRandomStartingFuel();

                if (!string.IsNullOrWhiteSpace(
                    currentVehicleKey))
                {
                    savedVehicleFuel[
                        currentVehicleKey
                    ] = startingFuel;
                }
            }

            vehicleFuel[
                handle
            ] = startingFuel;

            handleVehicleKeys[
                handle
            ] = currentVehicleKey;
        }

        //====================================================
        // VEHICLE SAVE KEY
        //====================================================

        private string GetPersistentVehicleKey(
            Vehicle vehicle)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return null;
            }

            if (vehicleInventoryManager != null)
            {
                string storageKey =
                    vehicleInventoryManager
                        .GetVehicleKey(
                            vehicle
                        );

                if (!string.IsNullOrWhiteSpace(
                    storageKey))
                {
                    return storageKey;
                }
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

            return vehicle.Model.Hash.ToString(
                CultureInfo.InvariantCulture
            ) +
            "_" +
            plate.Trim()
                .ToUpperInvariant();
        }

        private float GetRandomStartingFuel()
        {
            int seed =
                unchecked(
                    Game.GameTime *
                    397
                );

            Random random =
                new Random(
                    seed
                );

            return random.Next(
                45,
                101
            );
        }

        //====================================================
        // FUEL CONSUMPTION
        //====================================================

        private float GetFuelConsumption(
            Vehicle vehicle)
        {
            float speedMph =
                Math.Abs(
                    vehicle.Speed
                ) *
                2.23693629f;

            if (speedMph < 2f)
            {
                return IdleConsumption;
            }

            if (speedMph < 60f)
            {
                return DrivingConsumption;
            }

            return HighSpeedConsumption;
        }

        //====================================================
        // SAVE ALL FUEL
        //====================================================

        public void SaveAll()
        {
            try
            {
                foreach (
                    KeyValuePair<int, float>
                    pair in vehicleFuel)
                {
                    string vehicleKey;

                    if (!handleVehicleKeys.TryGetValue(
                        pair.Key,
                        out vehicleKey))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(
                        vehicleKey))
                    {
                        continue;
                    }

                    savedVehicleFuel[
                        vehicleKey
                    ] =
                        ClampFuel(
                            pair.Value
                        );
                }

                List<string> lines =
                    new List<string>();

                lines.Add(
                    "[VehicleFuel]"
                );

                foreach (
                    KeyValuePair<string, float>
                    pair in savedVehicleFuel)
                {
                    if (string.IsNullOrWhiteSpace(
                        pair.Key))
                    {
                        continue;
                    }

                    lines.Add(
                        EncodeKey(pair.Key) +
                        "=" +
                        ClampFuel(pair.Value)
                            .ToString(
                                "0.000",
                                CultureInfo.InvariantCulture
                            )
                    );
                }

                WriteFileSafely(
                    fuelSavePath,
                    lines
                );
            }
            catch
            {
                // Fuel-save failure should not crash GTA.
            }
        }

        //====================================================
        // LOAD ALL FUEL
        //====================================================

        private void LoadAll()
        {
            if (!File.Exists(
                fuelSavePath))
            {
                return;
            }

            try
            {
                string[] lines =
                    File.ReadAllLines(
                        fuelSavePath
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

                    string encodedKey =
                        line.Substring(
                            0,
                            equalsIndex
                        ).Trim();

                    string fuelText =
                        line.Substring(
                            equalsIndex + 1
                        ).Trim();

                    float fuel;

                    if (!float.TryParse(
                        fuelText,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out fuel))
                    {
                        continue;
                    }

                    string vehicleKey =
                        DecodeKey(
                            encodedKey
                        );

                    if (string.IsNullOrWhiteSpace(
                        vehicleKey))
                    {
                        continue;
                    }

                    savedVehicleFuel[
                        vehicleKey
                    ] =
                        ClampFuel(
                            fuel
                        );
                }
            }
            catch
            {
                savedVehicleFuel.Clear();
            }
        }

        //====================================================
        // SAFE FILE WRITING
        //====================================================

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

        private string EncodeKey(
            string value)
        {
            return Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    value
                )
            );
        }

        private string DecodeKey(
            string value)
        {
            try
            {
                return Encoding.UTF8.GetString(
                    Convert.FromBase64String(
                        value
                    )
                );
            }
            catch
            {
                return null;
            }
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

        //====================================================
        // STOP ENGINE
        //====================================================

        private void StopVehicleEngine(
            Vehicle vehicle)
        {
            Function.Call(
                Hash.SET_VEHICLE_ENGINE_ON,
                vehicle.Handle,
                false,
                true,
                true
            );

            Function.Call(
                Hash.SET_VEHICLE_UNDRIVEABLE,
                vehicle.Handle,
                true
            );
        }

        //====================================================
        // WARNINGS
        //====================================================

        private void ShowEmptyWarning()
        {
            if (Game.GameTime -
                lastEmptyWarningTime <
                3000)
            {
                return;
            }

            lastEmptyWarningTime =
                Game.GameTime;

            Notification.Show(
                "~r~OUT OF FUEL" +
                "~n~~s~The vehicle engine has stopped."
            );
        }

        private void ShowNoMoneyWarning()
        {
            if (Game.GameTime -
                lastNoMoneyWarningTime <
                2000)
            {
                return;
            }

            lastNoMoneyWarningTime =
                Game.GameTime;

            Notification.Show(
                "~r~Not enough money to refuel."
            );
        }

        private void ShowEmptyJerryCanWarning()
        {
            if (Game.GameTime -
                lastEmptyJerryCanWarningTime <
                2000)
            {
                return;
            }

            lastEmptyJerryCanWarningTime =
                Game.GameTime;

            Notification.Show(
                "~r~The jerry can is empty."
            );
        }

        //====================================================
        // CLAMP FUEL
        //====================================================

        private float ClampFuel(
            float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > MaximumFuel)
            {
                return MaximumFuel;
            }

            return value;
        }
    }
}