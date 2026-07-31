using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using SurvivalNeeds.Inventory;
using SurvivalNeeds.Systems;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SurvivalNeeds.Apartments
{
    public class ApartmentManager
    {
        private const float InteractionDistance =
            1.8f;

        private const int SleepHours =
            8;

        private readonly InventoryManager
            playerInventory;

        private readonly HungerSystem
            hunger;

        private readonly ThirstSystem
            thirst;

        private readonly StressSystem
            stress;

        private readonly MoneySystem
            money;

        private readonly ApartmentStorageManager
            storageManager;

        private readonly ApartmentOwnershipManager
            ownershipManager;

        private readonly Func<string>
            getCurrentProfileId;

        private readonly Action
            saveGame;

        private readonly List<Apartment>
            apartments =
                new List<Apartment>();

        private readonly List<Blip>
            apartmentBlips =
                new List<Blip>();

        private Apartment
            currentApartment;

        private ApartmentStorageMenu
            storageMenu;

        private bool
            ePressedLastFrame;

        private int
    wakeNeedsResetUntil;

        public ApartmentManager(
            InventoryManager playerInventory,
            HungerSystem hunger,
            ThirstSystem thirst,
            StressSystem stress,
            MoneySystem money,
            Func<string> getCurrentProfileId,
            Action saveGame)
        {
            this.playerInventory =
                playerInventory;

            this.hunger =
                hunger;

            this.thirst =
                thirst;

            this.stress =
                stress;

            this.money =
                money;

            this.getCurrentProfileId =
                getCurrentProfileId;

            this.saveGame =
                saveGame;

            storageManager =
                new ApartmentStorageManager();
            ownershipManager =
                new ApartmentOwnershipManager();

            CreateApartments();

            CreateApartmentBlips();
        }

        public bool MenuVisible
        {
            get
            {
                return storageMenu != null &&
                       storageMenu.Visible;
            }
        }

        public bool PlayerInsideApartment
        {
            get
            {
                return currentApartment != null;
            }
        }

        //====================================================
        // CREATE APARTMENTS
        //====================================================

        private void CreateApartments()
        {
            apartments.Clear();

            //================================================
            // PINK CAGE MOTEL
            //================================================

            apartments.Add(
                new Apartment(
                    "PINK_CAGE",

                    "Pink Cage Apartment",

                    ApartmentClass.LowEnd,

                    25000,

                    // Exterior entrance
                    new Vector3(
                        329.3502f,
                        -224.9914f,
                        57.0306f
                    ),

                    160f,

                    // Interior spawn
                    new Vector3(
                        151.36f,
                        -1007.88f,
                        -99.00f
                    ),

                    180f,

                    // Interior exit
                    new Vector3(
                        151.45f,
                        -1007.66f,
                        -99.00f
                    ),

                    // Bed
                    new Vector3(
                        154.05f,
                        -1004.65f,
                        -99.00f
                    ),

                    // Storage
                    new Vector3(
                        151.90f,
                        -1002.80f,
                        -99.00f
                    )
                )
            );

            //================================================
            // NEW LOW-END APARTMENT
            //================================================

            apartments.Add(
                new Apartment(
                    "STRAWBERRY_APARTMENT",

                    "Strawberry Apartment",

                    ApartmentClass.LowEnd,

                    25000,

                    // Exterior entrance
                    new Vector3(
                        -112.7480f,
                        -1479.199f,
                        36.8371f
                    ),

                    180f,

                    // Interior spawn
                    new Vector3(
                        151.36f,
                        -1007.88f,
                        -99.00f
                    ),

                    180f,

                    // Interior exit
                    new Vector3(
                        151.45f,
                        -1007.66f,
                        -99.00f
                    ),

                    // Bed
                    new Vector3(
                        154.05f,
                        -1004.65f,
                        -99.00f
                    ),

                    // Storage
                    new Vector3(
                        151.90f,
                        -1002.80f,
                        -99.00f
                    )
                )
            );
        }



        //====================================================
        // CREATE MAP BLIPS
        //====================================================

        private void CreateApartmentBlips()
        {
            DeleteApartmentBlips();

            foreach (
                Apartment apartment
                in apartments)
            {
                if (apartment == null)
                {
                    continue;
                }

                Blip blip =
                    Blip.Create(
                        apartment.ExteriorEntrance
                    );

                if (blip == null ||
                    !blip.Exists())
                {
                    continue;
                }

                blip.Sprite =
                    BlipSprite.Safehouse;

                blip.Color =
                    BlipColor.Green;

                blip.Name =
                    apartment.Name;

                blip.Scale =
                    0.8f;

                blip.IsShortRange =
                    false;

                apartmentBlips.Add(
                    blip
                );
            }
        }

        //====================================================
        // DELETE MAP BLIPS
        //====================================================

        private void DeleteApartmentBlips()
        {
            foreach (
                Blip blip
                in apartmentBlips)
            {
                if (blip != null &&
                    blip.Exists())
                {
                    blip.Delete();
                }
            }

            apartmentBlips.Clear();
        }

        //====================================================
        // UPDATE
        //====================================================

        public void Update()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            if (Game.GameTime <=
                wakeNeedsResetUntil)
            {
                hunger?.Set(
                    20f
                );

                thirst?.Set(
                    20f
                );

                stress?.Set(
                    0f
                );
            }

            bool ePressed =
                Game.IsKeyPressed(
                    Keys.E
                );

            if (storageMenu != null &&
                storageMenu.Visible)
            {
                storageMenu.Draw();

                if (ePressed &&
                    !ePressedLastFrame)
                {
                    CloseStorage();
                }

                ePressedLastFrame =
                    ePressed;

                return;
            }

            if (currentApartment == null)
            {
                UpdateExterior(
                    player,
                    ePressed
                );
            }
            else
            {
                UpdateInterior(
                    player,
                    ePressed
                );
            }

            ePressedLastFrame =
                ePressed;
        }

        //====================================================
        // EXTERIOR
        //====================================================

        private void UpdateExterior(
    Ped player,
    bool ePressed)
        {
            Apartment nearbyApartment =
                FindNearbyExterior(
                    player.Position
                );

            if (nearbyApartment == null)
            {
                return;
            }

            float markerOffset =
                nearbyApartment.Id == "STRAWBERRY_APARTMENT"
                ? -0.95f
                : 0f;

            DrawMarker(
                nearbyApartment.ExteriorEntrance,
                markerOffset
            );

            string profileId =
                GetProfileId();

            bool isOwned =
                ownershipManager.IsOwned(
                    profileId,
                    nearbyApartment.Id
                );

            string className =
                nearbyApartment
                    .GetClassDisplayName();

            if (isOwned)
            {
                GTA.UI.Screen.ShowHelpTextThisFrame(
                    nearbyApartment.Name +
                    "~n~" +
                    className +
                    " apartment" +
                    "~n~~g~Owned" +
                    "~n~Press ~INPUT_CONTEXT~ to enter."
                );
            }
            else
            {
                GTA.UI.Screen.ShowHelpTextThisFrame(
                    nearbyApartment.Name +
                    "~n~" +
                    className +
                    " apartment" +
                    "~n~Price: ~g~$" +
                    nearbyApartment.Price.ToString(
                        "N0"
                    ) +
                    "~s~~n~Press ~INPUT_CONTEXT~ to purchase."
                );
            }

            if (!ePressed ||
                ePressedLastFrame)
            {
                return;
            }

            if (isOwned)
            {
                EnterApartment(
                    nearbyApartment,
                    player
                );

                return;
            }

            PurchaseApartment(
                nearbyApartment,
                profileId
            );
        }

        //====================================================
        // PURCHASE APARTMENT
        //====================================================

        private void PurchaseApartment(
            Apartment apartment,
            string profileId)
        {
            if (apartment == null ||
                money == null)
            {
                return;
            }

            if (ownershipManager.IsOwned(
                profileId,
                apartment.Id))
            {
                Notification.Show(
                    "~y~You already own this apartment.",
                    false
                );

                return;
            }

            if (money.Cash <
                apartment.Price)
            {
                int missingAmount =
                    apartment.Price -
                    money.Cash;

                Notification.Show(
                    "~r~You cannot afford this apartment.~n~" +
                    "You need $" +
                    missingAmount.ToString(
                        "N0"
                    ) +
                    " more.",
                    false
                );

                return;
            }

            bool paymentSucceeded =
    money.SpendMoney(
        apartment.Price
    );

            if (!paymentSucceeded)
            {
                Notification.Show(
                    "~r~Apartment purchase failed.",
                    false
                );

                return;
            }

            bool ownershipAdded =
                ownershipManager.AddOwnership(
                    profileId,
                    apartment.Id
                );

            if (!ownershipAdded)
            {
                /*
                 * Ownership was not added, so return the money.
                 * This protects the player from losing cash if
                 * saving ownership unexpectedly fails.
                 */
                money.AddMoney(
                    apartment.Price
                );

                Notification.Show(
                    "~r~Apartment purchase failed.",
                    false
                );

                return;
            }

            saveGame?.Invoke();

            Notification.Show(
                "~g~Apartment purchased!~n~" +
                apartment.Name +
                "~n~Price: $" +
                apartment.Price.ToString(
                    "N0"
                ),
                false
            );
        }

        //====================================================
        // INTERIOR
        //====================================================

        private void UpdateInterior(
            Ped player,
            bool ePressed)
        {
            if (currentApartment == null)
            {
                return;
            }

            float exitDistance =
                player.Position.DistanceTo(
                    currentApartment.InteriorExit
                );

            float bedDistance =
                player.Position.DistanceTo(
                    currentApartment.BedPosition
                );

            float storageDistance =
                player.Position.DistanceTo(
                    currentApartment.StoragePosition
                );

            if (exitDistance <=
                InteractionDistance)
            {
                DrawMarker(
                    currentApartment.InteriorExit
                );

                GTA.UI.Screen.ShowHelpTextThisFrame(
                    "Press ~INPUT_CONTEXT~ to leave the apartment."
                );

                if (ePressed &&
                    !ePressedLastFrame)
                {
                    ExitApartment(
                        player
                    );

                    return;
                }
            }

            if (bedDistance <=
                InteractionDistance)
            {
                DrawMarker(
                    currentApartment.BedPosition
                );

                GTA.UI.Screen.ShowHelpTextThisFrame(
                    "Press ~INPUT_CONTEXT~ to sleep for 8 hours."
                );

                if (ePressed &&
                    !ePressedLastFrame)
                {
                    Sleep(
                        player
                    );

                    return;
                }
            }

            if (storageDistance <=
                InteractionDistance)
            {
                DrawMarker(
                    currentApartment.StoragePosition
                );

                GTA.UI.Screen.ShowHelpTextThisFrame(
                    "Press ~INPUT_CONTEXT~ to open apartment storage."
                );

                if (ePressed &&
                    !ePressedLastFrame)
                {
                    OpenStorage();

                    return;
                }
            }
        }

        //====================================================
        // FIND NEARBY APARTMENT
        //====================================================

        private Apartment FindNearbyExterior(
            Vector3 playerPosition)
        {
            Apartment closestApartment =
                null;

            float closestDistance =
                float.MaxValue;

            foreach (
                Apartment apartment
                in apartments)
            {
                if (apartment == null)
                {
                    continue;
                }

                float distance =
                    playerPosition.DistanceTo(
                        apartment.ExteriorEntrance
                    );

                if (distance >
                    InteractionDistance)
                {
                    continue;
                }

                if (distance <
                    closestDistance)
                {
                    closestDistance =
                        distance;

                    closestApartment =
                        apartment;
                }
            }

            return closestApartment;
        }

        //====================================================
        // ENTER APARTMENT
        //====================================================

        private void EnterApartment(
            Apartment apartment,
            Ped player)
        {
            if (apartment == null ||
                player == null ||
                !player.Exists())
            {
                return;
            }

            Function.Call(
                Hash.DO_SCREEN_FADE_OUT,
                500
            );

            Script.Wait(
                600
            );

            Function.Call(
                Hash.REQUEST_COLLISION_AT_COORD,
                apartment.InteriorSpawn.X,
                apartment.InteriorSpawn.Y,
                apartment.InteriorSpawn.Z
            );

            player.Position =
                apartment.InteriorSpawn;

            player.Heading =
                apartment.InteriorHeading;

            currentApartment =
                apartment;

            Script.Wait(
                300
            );

            Function.Call(
                Hash.DO_SCREEN_FADE_IN,
                500
            );

            Notification.Show(
                "~g~Entered " +
                apartment.Name,
                false
            );
        }

        //====================================================
        // EXIT APARTMENT
        //====================================================

        private void ExitApartment(
            Ped player)
        {
            if (currentApartment == null ||
                player == null ||
                !player.Exists())
            {
                return;
            }

            CloseStorage();

            Apartment apartmentBeingLeft =
                currentApartment;

            Function.Call(
                Hash.DO_SCREEN_FADE_OUT,
                500
            );

            Script.Wait(
                600
            );

            Function.Call(
                Hash.REQUEST_COLLISION_AT_COORD,
                apartmentBeingLeft
                    .ExteriorEntrance.X,
                apartmentBeingLeft
                    .ExteriorEntrance.Y,
                apartmentBeingLeft
                    .ExteriorEntrance.Z
            );

            if (apartmentBeingLeft.Id == "PINK_CAGE")
            {
                player.Position =
                    new Vector3(
                        329.5549f,
                        -224.8002f,
                        58.2241f
                    );
            }
            else if (
                apartmentBeingLeft.Id == "STRAWBERRY_APARTMENT")
            {
                player.Position =
                    new Vector3(
                        -112.7480f,
                        -1479.199f,
                        36.8371f
                    );
            }
            else
            {
                player.Position =
                    apartmentBeingLeft.ExteriorEntrance;
            }

            player.Heading =
                apartmentBeingLeft
                    .ExteriorHeading;

            currentApartment =
                null;

            Script.Wait(
                300
            );

            Function.Call(
                Hash.DO_SCREEN_FADE_IN,
                500
            );

            Notification.Show(
                "~b~Left " +
                apartmentBeingLeft.Name,
                false
            );
        }

        //====================================================
        // OPEN STORAGE
        //====================================================

        private void OpenStorage()
        {
            if (currentApartment == null ||
                playerInventory == null)
            {
                return;
            }

            string profileId =
                GetProfileId();

            InventoryManager apartmentInventory =
                storageManager.GetStorage(
                    currentApartment.Id,
                    profileId
                );

            storageMenu =
                new ApartmentStorageMenu(
                    playerInventory,
                    apartmentInventory
                );

            storageMenu.Open();
        }

        //====================================================
        // CLOSE STORAGE
        //====================================================

        public void CloseStorage()
        {
            if (storageMenu != null &&
                storageMenu.Visible)
            {
                storageMenu.Close();
            }

            storageManager.SaveAll();
        }

        //====================================================
        // SLEEP
        //====================================================

        private void Sleep(
    Ped player)
        {
            if (player == null ||
                !player.Exists())
            {
                return;
            }

            Function.Call(
                Hash.DO_SCREEN_FADE_OUT,
                1000
            );

            Script.Wait(
                1200
            );

            Function.Call(
                Hash.ADD_TO_CLOCK_TIME,
                SleepHours,
                0,
                0
            );

            Script.Wait(
                1000
            );

            player.Health =
                player.MaxHealth;

            Function.Call(
                Hash.RESTORE_PLAYER_STAMINA,
                Game.Player.Handle,
                1.0f
            );

            Function.Call(
                Hash.DO_SCREEN_FADE_IN,
                1000
            );

            Script.Wait(
                1200
            );

            // The character has now fully woken up.
            hunger?.Set(
                20f
            );

            thirst?.Set(
                20f
            );

            stress?.Set(
                0f
            );

            /*
             * Keep the values at 20 briefly because the needs
             * system may still process the 8-hour clock change.
             */
            wakeNeedsResetUntil =
                Game.GameTime + 2000;

            saveGame?.Invoke();

            Notification.Show(
                "~g~You slept for 8 hours.~n~" +
                "Hunger and thirst are now 20%.",
                false
            );
        }

        //====================================================
        // SAVE ALL
        //====================================================

        public void SaveAll()
        {
            storageManager.SaveAll();

            ownershipManager.SaveAll();
        }

        //====================================================
        // PROFILE
        //====================================================

        private string GetProfileId()
        {
            if (getCurrentProfileId == null)
            {
                return "DEFAULT";
            }

            string profileId =
                getCurrentProfileId();

            if (string.IsNullOrWhiteSpace(
                profileId))
            {
                return "DEFAULT";
            }

            return profileId;
        }

        //====================================================
        // DRAW MARKER
        //====================================================

        private void DrawMarker(
    Vector3 position,
    float zOffset = -1.0f)
        {
            Function.Call(
                Hash.DRAW_MARKER,
                1,
                position.X,
                position.Y,
                position.Z + zOffset,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0.75f,
                0.75f,
                0.30f,
                20,
                225,
                195,
                170,
                false,
                false,
                2,
                false,
                null,
                null,
                false
            );
        }
    }
}