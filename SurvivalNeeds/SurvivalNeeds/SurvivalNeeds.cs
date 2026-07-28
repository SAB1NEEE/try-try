using GTA;
using GTA.Native;
using GTA.UI;
using SurvivalNeeds.Systems;
using SurvivalNeeds.UI;
using SurvivalNeeds.Actions;
using SurvivalNeeds.Inventory;
using SurvivalNeeds.Loot;
using SurvivalNeeds.Managers;
using SurvivalNeeds.VehicleStorage;
using SurvivalNeeds.Phone;
using SurvivalNeeds.Vendors;
using SurvivalNeeds.BankingSystem;
using SurvivalNeeds.Police;
using System;
using System.Windows.Forms;

namespace SurvivalNeeds
{
    public class SurvivalNeeds : Script
    {
        private readonly HungerSystem hunger =
            new HungerSystem();

        private readonly ThirstSystem thirst =
            new ThirstSystem();

        private readonly StressSystem stress =
            new StressSystem();

        private readonly MoneySystem money =
            new MoneySystem();

        private readonly SurvivalEffectsSystem
            survivalEffects =
                new SurvivalEffectsSystem();

        private DeathPenaltySystem
            deathPenaltySystem;

        private ArrestSystem arrestSystem;

        private bool speedingAlreadyHandled = false;

        private readonly AnimationSystem
            animationSystem =
                new AnimationSystem();

        private readonly SaveSystem saveSystem =
            new SaveSystem();

        private readonly SaveManager saveManager =
            new SaveManager();

        private const int
            AutoSaveIntervalMilliseconds =
                30000;

        private int lastAutoSaveTime;

        private readonly InventoryManager
            inventory =
                new InventoryManager();

        private InventoryMenu inventoryMenu;

        private bool playerInventoryOpen =
            false;

        private readonly
            VehicleInventoryManager
            vehicleInventoryManager =
                new VehicleInventoryManager();

        private readonly VehicleStorageSystem
            vehicleStorageSystem =
                new VehicleStorageSystem();

        private VehicleStorageMenu
            vehicleStorageMenu;

        private Vehicle activeTrunkVehicle;

        private LootSystem lootSystem;

        private PersonalVehiclePhoneSystem
            personalVehiclePhoneSystem;

        private VendorsManager
        vendorsManager;

        private readonly HUD hud =
            new HUD();

        private readonly BankAccount
            bankAccount =
        new BankAccount();

        private ATMMenu
            atmMenu;

        private ATMSystem
            atmSystem;

        private bool
            inventoryKeyPressedLastFrame =
                false;

        private bool
            interactPressedLastFrame =
                false;

        private bool
            claimKeyPressedLastFrame =
                false;

        private void OnPlayerInventoryChanged()
        {
            try
            {
                saveManager.SaveInventory(
                    inventory
                );
            }
            catch (Exception ex)
            {
                Notification.Show(
                    "~r~Inventory save failed: " +
                    ex.Message,
                    false
                );
            }
        }

        public SurvivalNeeds()
        {
            Tick += OnTick;
            Aborted += OnAborted;

            Interval = 0;

            try
            {
                LoadGame();

                arrestSystem =
                    new ArrestSystem();

                inventoryMenu =
                    new InventoryMenu(
                        inventory,
                        hunger,
                        thirst,
                        stress,
                        animationSystem
                    );

                lootSystem =
                    new LootSystem(
                        inventory,
                        money
                    );
                deathPenaltySystem =
                    new DeathPenaltySystem(
                        inventory,
                        money,
                        hunger,
                        thirst,
                        stress
                    );

                
                personalVehiclePhoneSystem =
                    new PersonalVehiclePhoneSystem(
                    vehicleInventoryManager
                    );

                vendorsManager =
                    new VendorsManager(
                    inventory,
                    money
                    );

                atmMenu =
                    new ATMMenu(
                    bankAccount,
                    money
                    );

                atmSystem =
                    new ATMSystem(
                        atmMenu
                    );

                inventory.InventoryChanged +=
                    OnPlayerInventoryChanged;

                lastAutoSaveTime =
                    Game.GameTime;

                Notification.Show(
                    "~g~Survival Needs Loaded",
                    false
                );

                Notification.Show(
                    "~b~I: Inventory | E: Interact | K: Claim",
                    false
                );
            }
            catch (Exception ex)
            {
                Notification.Show(
                    "~r~Survival Needs Error: " +
                    ex.Message,
                    false
                );
            }
        }

        //====================================================
        // LOAD GAME
        //====================================================

        private void LoadGame()
        {
            saveSystem.Load(
            hunger,
            thirst,
            stress,
            money,
            bankAccount
            );

            bool inventoryLoaded =
                saveManager.LoadInventory(
                    inventory
                );

            if (inventoryLoaded)
            {
                Notification.Show(
                    "~g~Inventory loaded",
                    false
                );
            }
            else
            {
                Notification.Show(
                    "~y~No inventory save found",
                    false
                );
            }
        }

        //====================================================
        // MAIN TICK
        //====================================================

        private void OnTick(
            object sender,
            EventArgs e)
        {
            // Always update and draw survival HUD first.
            animationSystem.Update();
            ConsumableManager.Update();
            vehicleInventoryManager.Update();
            deathPenaltySystem?.Update();
            arrestSystem?.Update();
            UpdateSurvival();
            UpdateAutoSave();

            personalVehiclePhoneSystem?.Update();

            // Update world interaction systems.
            vendorsManager?.Update();
            atmSystem?.Update();

            bool vendorMenuOpen =
                vendorsManager != null &&
                vendorsManager.MenuVisible;

            bool atmMenuOpen =
                atmMenu != null &&
                atmMenu.Visible;

            // Process LemonUI ATM menu.
            atmMenu?.Process();

            // Prevent other interaction systems while a menu is open,
            // but do not stop the HUD from drawing.
            if (vendorMenuOpen ||
                atmMenuOpen)
            {
                DrawMenus();
                return;
            }

            HandleInventoryInput();
            HandleVehicleClaiming();
            HandleVehicleTrunk();
            UpdateLoot();
            DrawMenus();
        }

        //====================================================
        // SURVIVAL SYSTEMS
        //====================================================

        private void UpdateSurvival()
        {
            hunger.Update();
            thirst.Update();

            stress.Update(
                hunger.Value,
                thirst.Value
            );

            survivalEffects.Update(
                hunger.Value,
                thirst.Value,
                stress
            );

            hud.Draw(
                hunger.Value,
                thirst.Value,
                stress.Value,
                money.Cash
            );
        }

        //====================================================
        // AUTO SAVE
        //====================================================

        private void UpdateAutoSave()
        {
            int currentTime =
                Game.GameTime;

            if (currentTime -
                lastAutoSaveTime <
                AutoSaveIntervalMilliseconds)
            {
                return;
            }

            SaveGame();

            lastAutoSaveTime =
                currentTime;
        }

        //====================================================
        // SAVE EVERYTHING
        //====================================================

        private void SaveGame()
        {
            try
            {
                saveSystem.Save(
                hunger.Value,
                thirst.Value,
                stress.Value,
                money.Cash,
                bankAccount
                );

                saveManager.SaveInventory(
                    inventory
                );

                vehicleInventoryManager
                    .SaveAll();
            }
            catch (Exception ex)
            {
                Notification.Show(
                    "~r~Save failed: " +
                    ex.Message,
                    false
                );
            }
        }

        //====================================================
        // SCRIPT ABORTED
        //====================================================

        private void OnAborted(
            object sender,
            EventArgs e)
        {
            inventory.InventoryChanged -=
                OnPlayerInventoryChanged;

            vendorsManager?.Dispose();

            SaveGame();
        }

        //====================================================
        // INVENTORY INPUT
        //====================================================

        private void HandleInventoryInput()
        {
            bool inventoryPressed =
                Game.IsKeyPressed(
                    Keys.I
                );

            if (inventoryPressed &&
                !inventoryKeyPressedLastFrame)
            {
                bool trunkOpen =
                    vehicleStorageMenu != null &&
                    vehicleStorageMenu.Visible;

                if (!trunkOpen)
                {
                    inventoryMenu.Toggle();
                }
            }

            inventoryKeyPressedLastFrame =
                inventoryPressed;
        }

        //====================================================
        // CLAIM VEHICLE
        //====================================================

        private void HandleVehicleClaiming()
        {
            bool claimPressed =
                Game.IsKeyPressed(
                    Keys.K
                );

            bool inventoryOpen =
                inventoryMenu != null &&
                inventoryMenu.Visible;

            bool trunkOpen =
                vehicleStorageMenu != null &&
                vehicleStorageMenu.Visible;

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                !player.IsInVehicle())
            {
                claimKeyPressedLastFrame =
                    claimPressed;

                return;
            }

            Vehicle vehicle =
                player.CurrentVehicle;

            if (vehicle == null ||
                !vehicle.Exists())
            {
                claimKeyPressedLastFrame =
                    claimPressed;

                return;
            }

            /*
             * Only the driver can claim the vehicle.
             */
            if (vehicle.Driver == null ||
                !vehicle.Driver.Exists() ||
                vehicle.Driver.Handle !=
                    player.Handle)
            {
                claimKeyPressedLastFrame =
                    claimPressed;

                return;
            }

            if (!inventoryOpen &&
                !trunkOpen)
            {
                string existingClaimId =
                    vehicleInventoryManager
                        .GetClaimId(
                            vehicle
                        );

                if (string.IsNullOrWhiteSpace(
                    existingClaimId))
                {
                    GTA.UI.Screen
                        .ShowHelpTextThisFrame(
                            "Press ~y~K~s~ to claim this vehicle as your personal vehicle."
                        );
                }
                else
                {
                    GTA.UI.Screen
                        .ShowHelpTextThisFrame(
                            "~g~Personal Vehicle~s~ | ID: " +
                            existingClaimId
                        );
                }
            }

            if (claimPressed &&
                !claimKeyPressedLastFrame &&
                !inventoryOpen &&
                !trunkOpen)
            {
                ClaimCurrentVehicle(
                    vehicle
                );
            }

            claimKeyPressedLastFrame =
                claimPressed;
        }

        private void ClaimCurrentVehicle(
            Vehicle vehicle)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                Notification.Show(
                    "~r~No vehicle available",
                    false
                );

                return;
            }

            string claimId;
            bool alreadyClaimed;

            bool success =
                vehicleInventoryManager
                    .ClaimVehicle(
                        vehicle,
                        out claimId,
                        out alreadyClaimed
                    );

            if (!success)
            {
                Notification.Show(
                    "~r~Vehicle could not be claimed",
                    false
                );

                return;
            }

            if (alreadyClaimed)
            {
                Notification.Show(
                    "~y~This vehicle is already claimed: " +
                    claimId,
                    false
                );

                return;
            }

            Notification.Show(
                "~g~Vehicle claimed successfully",
                false
            );

            Notification.Show(
                "~b~Vehicle ID: " +
                claimId,
                false
            );
        }

        //====================================================
        // VEHICLE TRUNK
        //====================================================

        private void HandleVehicleTrunk()
        {
            bool trunkOpen =
                vehicleStorageMenu != null &&
                vehicleStorageMenu.Visible;

            Vehicle nearbyVehicle =
                vehicleStorageSystem
                    .GetNearbyVehicle();

            if (trunkOpen)
            {
                bool activeVehicleInvalid =
                    activeTrunkVehicle == null ||
                    !activeTrunkVehicle.Exists();

                bool playerMovedAway =
                    nearbyVehicle == null ||
                    activeTrunkVehicle == null ||
                    nearbyVehicle.Handle !=
                        activeTrunkVehicle.Handle;

                if (activeVehicleInvalid ||
                    playerMovedAway)
                {
                    vehicleStorageMenu.Close();

                    activeTrunkVehicle =
                        null;

                    interactPressedLastFrame =
                        false;

                    return;
                }
            }

            if (nearbyVehicle == null)
            {
                interactPressedLastFrame =
                    false;

                return;
            }

            GTA.UI.Screen
                .ShowHelpTextThisFrame(
                    trunkOpen
                        ? "Press ~INPUT_CONTEXT~ to close trunk"
                        : "Press ~INPUT_CONTEXT~ to open trunk"
                );

            bool interactPressed =
                Game.IsKeyPressed(
                    Keys.E
                );

            if (interactPressed &&
                !interactPressedLastFrame)
            {
                if (trunkOpen)
                {
                    CloseVehicleTrunk();
                }
                else
                {
                    OpenVehicleTrunk(
                        nearbyVehicle
                    );
                }
            }

            interactPressedLastFrame =
                interactPressed;
        }

        //====================================================
        // OPEN VEHICLE TRUNK
        //====================================================

        private void OpenVehicleTrunk(
            Vehicle vehicle)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            if (inventoryMenu.Visible)
            {
                inventoryMenu.Toggle();

                playerInventoryOpen =
                    false;
            }

            VehicleInventory trunkInventory =
                vehicleInventoryManager
                    .GetInventory(
                        vehicle
                    );

            if (trunkInventory == null)
            {
                return;
            }

            vehicleStorageMenu =
                new VehicleStorageMenu(
                    inventory,
                    trunkInventory
                );

            activeTrunkVehicle =
                vehicle;

            vehicleStorageMenu.Open();
        }

        //====================================================
        // CLOSE VEHICLE TRUNK
        //====================================================

        private void CloseVehicleTrunk()
        {
            if (vehicleStorageMenu != null)
            {
                vehicleStorageMenu.Close();
            }

            activeTrunkVehicle =
                null;

            SaveGame();
        }

        //====================================================
        // LOOT
        //====================================================

        private void UpdateLoot()
        {
            bool trunkOpen =
                vehicleStorageMenu != null &&
                vehicleStorageMenu.Visible;

            if (!trunkOpen)
            {
                lootSystem?.Update();
            }
        }


        //====================================================
        // DRAW MENUS
        //====================================================

        private void DrawMenus()
        {
            playerInventoryOpen =
                inventoryMenu.Visible;

            bool trunkOpen =
                vehicleStorageMenu != null &&
                vehicleStorageMenu.Visible;

            if (playerInventoryOpen &&
                !trunkOpen)
            {
                inventoryMenu.Draw();
            }

            if (trunkOpen)
            {
                vehicleStorageMenu.Draw();
            }
        }

    }
}