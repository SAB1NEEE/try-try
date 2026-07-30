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

        private readonly ApartmentStorageManager
            storageManager;

        private readonly Func<string>
            getCurrentProfileId;

        private readonly Action
            saveGame;

        private readonly List<Apartment>
            apartments =
                new List<Apartment>();

        private Apartment
            currentApartment;

        private ApartmentStorageMenu
            storageMenu;

        private bool ePressedLastFrame;

        public ApartmentManager(
            InventoryManager playerInventory,
            HungerSystem hunger,
            ThirstSystem thirst,
            StressSystem stress,
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

            this.getCurrentProfileId =
                getCurrentProfileId;

            this.saveGame =
                saveGame;

            storageManager =
                new ApartmentStorageManager();

            CreateApartments();
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

                    // Exterior entrance
                    new Vector3(
                        326.45f,
                        -212.10f,
                        54.09f
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
            // CARSON AVENUE APARTMENT
            //================================================

            apartments.Add(
                new Apartment(
                    "CARSON_APARTMENT",
                    "Carson Avenue Apartment",

                    /*
                     * Temporary exterior position near the
                     * Chamberlain Hills apartment complex.
                     * We can adjust this to your chosen door.
                     */
                    new Vector3(
                        -24.40f,
                        -1440.35f,
                        30.65f
                    ),

                    180f,

                    /*
                     * Both apartments currently reuse the
                     * same GTA interior.
                     */
                    new Vector3(
                        151.36f,
                        -1007.88f,
                        -99.00f
                    ),

                    180f,

                    new Vector3(
                        151.45f,
                        -1007.66f,
                        -99.00f
                    ),

                    new Vector3(
                        154.05f,
                        -1004.65f,
                        -99.00f
                    ),

                    new Vector3(
                        151.90f,
                        -1002.80f,
                        -99.00f
                    )
                )
            );
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

            DrawMarker(
                nearbyApartment.ExteriorEntrance
            );

            GTA.UI.Screen.ShowHelpTextThisFrame(
                "Press ~INPUT_CONTEXT~ to enter " +
                nearbyApartment.Name +
                "."
            );

            if (ePressed &&
                !ePressedLastFrame)
            {
                EnterApartment(
                    nearbyApartment,
                    player
                );
            }
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

            player.Position =
                apartmentBeingLeft
                    .ExteriorEntrance +
                new Vector3(
                    0f,
                    -1.5f,
                    0f
                );

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

            InventoryManager
                apartmentInventory =
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

            hunger?.Set(
                20f
            );

            thirst?.Set(
                20f
            );

            stress?.Set(
                0f
            );

            player.Health =
                player.MaxHealth;

            Function.Call(
                Hash.RESTORE_PLAYER_STAMINA,
                Game.Player.Handle,
                1.0f
            );

            saveGame?.Invoke();

            Script.Wait(
                700
            );

            Function.Call(
                Hash.DO_SCREEN_FADE_IN,
                1000
            );

            Notification.Show(
                "~g~You slept for 8 hours.",
                false
            );
        }

        //====================================================
        // SAVE ALL
        //====================================================

        public void SaveAll()
        {
            storageManager.SaveAll();
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
            Vector3 position)
        {
            Function.Call(
                Hash.DRAW_MARKER,
                1,
                position.X,
                position.Y,
                position.Z - 1.0f,
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