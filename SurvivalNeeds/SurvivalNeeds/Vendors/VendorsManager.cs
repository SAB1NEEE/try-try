using GTA;
using GTA.Math;
using GTA.Native;
using SurvivalNeeds.Inventory;
using SurvivalNeeds.Systems;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SurvivalNeeds.Vendors
{
    public class VendorsManager
    {
        private const int Blip24Seven = 52;
        private const int BlipLiquorStore = 93;
        private const int BlipGasStation = 361;
        private const int BlipPharmacy = 403;

        private const int ColorRed = 1;
        private const int ColorGreen = 2;
        private const int ColorYellow = 5;
        private const int ColorOrange = 17;

        // Existing GTA V hotdog stand prop.
        private const string HotdogStandModel =
            "prop_hotdogstand_01";

        // How close the player must be to detect and use the stand.
        private const float HotdogDetectionRadius = 3.0f;

        private readonly InventoryManager inventory;
        private readonly MoneySystem money;
        private readonly VendorMenu vendorMenu;
        private readonly List<Vendor> vendors;

        private readonly List<Blip> vendorBlips =
            new List<Blip>();

        private Vendor nearbyVendor;

        private bool ePressedLastFrame;
        private bool playerWasNearVendor;
        private bool greetingShown;

        public VendorsManager(
            InventoryManager inventory,
            MoneySystem money)
        {
            this.inventory = inventory;
            this.money = money;

            vendorMenu = new VendorMenu(
                inventory,
                money
            );

            vendors =
            VendorDataBase.CreateVendors();

            AddJerryCanToGasStations();

            CreateVendorBlips();
        }

        public bool MenuVisible
        {
            get
            {
                return vendorMenu != null &&
                       vendorMenu.Visible;
            }
        }

        public VendorMenu Menu
        {
            get { return vendorMenu; }
        }

        public void Update()
        {
            if (vendorMenu.Visible)
            {
                DisableVanillaStoreInteraction();
                vendorMenu.Draw();

                ePressedLastFrame =
                    Game.IsKeyPressed(Keys.E);

                return;
            }

            nearbyVendor =
                FindNearestVendor();

            bool playerNearVendor =
                nearbyVendor != null;

            if (!playerNearVendor)
            {
                playerWasNearVendor = false;
                greetingShown = false;

                ePressedLastFrame =
                    Game.IsKeyPressed(Keys.E);

                return;
            }

            DisableVanillaStoreInteraction();
            ShowVendorPrompt(nearbyVendor);

            if (!playerWasNearVendor &&
                !greetingShown)
            {
                ShowVendorGreeting(nearbyVendor);
                greetingShown = true;
            }

            bool ePressed =
                Game.IsKeyPressed(Keys.E);

            if (ePressed &&
                !ePressedLastFrame)
            {
                vendorMenu.Open(nearbyVendor);
            }

            ePressedLastFrame = ePressed;
            playerWasNearVendor = true;
        }

        public void CloseMenu()
        {
            if (vendorMenu.Visible)
                vendorMenu.Close();
        }

        public void Dispose()
        {
            CloseMenu();
            DeleteExistingBlips();
        }

        private Vendor FindNearestVendor()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return null;
            }

            Vendor closestVendor = null;
            float closestDistance = float.MaxValue;

            foreach (Vendor vendor in vendors)
            {
                if (vendor == null)
                    continue;

                float distance =
                    player.Position.DistanceTo(
                        vendor.Position
                    );

                if (distance >
                    vendor.InteractionDistance)
                {
                    continue;
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestVendor = vendor;
                }
            }

            Vendor streetVendor =
                StreetVendors.FindNearbyVendor(
                    player.Position
                );

            if (streetVendor != null)
            {
                float streetDistance =
                    player.Position.DistanceTo(
                        streetVendor.Position
                    );

                if (closestVendor == null ||
                    streetDistance < closestDistance)
                {
                    closestVendor = streetVendor;
                }
            }

            return closestVendor;
        }



        private void ShowVendorPrompt(
            Vendor vendor)
        {
            if (vendor == null)
                return;

            string message =
                "Press ~INPUT_CONTEXT~ to browse " +
                vendor.Name;

            Function.Call(
                Hash.BEGIN_TEXT_COMMAND_DISPLAY_HELP,
                "STRING"
            );

            Function.Call(
                Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME,
                message
            );

            Function.Call(
                Hash.END_TEXT_COMMAND_DISPLAY_HELP,
                0,
                false,
                true,
                -1
            );
        }

        private void ShowVendorGreeting(
    Vendor vendor)
        {
            if (vendor == null)
                return;

            if (vendor.Type ==
                VendorType.StreetVendor)
            {
                if (vendor.StreetType ==
                    StreetVendorType.HotdogStand)
                {
                    GTA.UI.Screen.ShowSubtitle(
                        "~y~Vendor:~w~ Fresh hotdogs!",
                        2500
                    );

                    return;
                }

                if (vendor.StreetType ==
                    StreetVendorType.BurgerStand)
                {
                    GTA.UI.Screen.ShowSubtitle(
                        "~y~Vendor:~w~ Burgers are ready!",
                        2500
                    );

                    return;
                }

                // Vending machines should not speak.
                if (vendor.StreetType ==
                        StreetVendorType.SnackMachine ||
                    vendor.StreetType ==
                        StreetVendorType.DrinkMachine)
                {
                    return;
                }
            }

            GTA.UI.Screen.ShowSubtitle(
                "~b~Cashier:~w~ Need anything today?",
                2500
            );
        }

        private void DisableVanillaStoreInteraction()
        {
            // INPUT_CONTEXT / E
            DisableControlForGroups(51);

            // Alternative context control.
            DisableControlForGroups(38);
        }

        private void DisableControlForGroups(
            int control)
        {
            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                control,
                true
            );

            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                1,
                control,
                true
            );

            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                2,
                control,
                true
            );
        }

        private void CreateVendorBlips()
        {
            DeleteExistingBlips();

            foreach (Vendor vendor in vendors)
            {
                if (vendor == null)
                    continue;

                // Hotdog stands are detected dynamically
                // and are not stored as fixed vendors.
                if (vendor.Type ==
                    VendorType.StreetVendor)
                {
                    continue;
                }

                Blip blip =
                    Blip.Create(
                        vendor.Position
                    );

                if (blip == null ||
                    !blip.Exists())
                {
                    continue;
                }

                Function.Call(
                    Hash.SET_BLIP_SPRITE,
                    blip.Handle,
                    GetBlipSprite(vendor)
                );

                Function.Call(
                    Hash.SET_BLIP_COLOUR,
                    blip.Handle,
                    GetBlipColor(vendor)
                );

                Function.Call(
                    Hash.SET_BLIP_SCALE,
                    blip.Handle,
                    0.80f
                );

                Function.Call(
                    Hash.SET_BLIP_AS_SHORT_RANGE,
                    blip.Handle,
                    false
                );

                Function.Call(
                    Hash.SET_BLIP_DISPLAY,
                    blip.Handle,
                    3
                );

                SetBlipName(
                    blip,
                    vendor.Name
                );

                vendorBlips.Add(blip);
            }
        }

        private void DeleteExistingBlips()
        {
            foreach (Blip blip in vendorBlips)
            {
                if (blip != null &&
                    blip.Exists())
                {
                    blip.Delete();
                }
            }

            vendorBlips.Clear();
        }

        private int GetBlipSprite(
            Vendor vendor)
        {
            if (vendor == null)
                return Blip24Seven;

            switch (vendor.Type)
            {
                case VendorType.LiquorStore:
                    return BlipLiquorStore;

                case VendorType.Pharmacy:
                    return BlipPharmacy;

                case VendorType.ConvenienceStore:
                default:
                    if (IsGasStationVendor(vendor))
                        return BlipGasStation;

                    return Blip24Seven;
            }
        }

        private int GetBlipColor(
            Vendor vendor)
        {
            if (vendor == null)
                return ColorGreen;

            switch (vendor.Type)
            {
                case VendorType.LiquorStore:
                    return ColorOrange;

                case VendorType.Pharmacy:
                    return ColorRed;

                case VendorType.StreetVendor:
                    return ColorYellow;

                case VendorType.ConvenienceStore:
                default:
                    if (IsGasStationVendor(vendor))
                        return ColorYellow;

                    return ColorGreen;
            }
        }

        private bool IsGasStationVendor(
            Vendor vendor)
        {
            if (vendor == null ||
                string.IsNullOrWhiteSpace(
                    vendor.Name))
            {
                return false;
            }

            string name =
                vendor.Name.ToUpperInvariant();

            return
                name.Contains("LTD") ||
                name.Contains("GAS") ||
                name.Contains("FUEL");
        }

        private void AddJerryCanToGasStations()
        {
            if (vendors == null)
                return;

            foreach (Vendor vendor in vendors)
            {
                if (vendor == null ||
                    !IsGasStationVendor(vendor))
                {
                    continue;
                }

                bool alreadyAdded = false;

                foreach (VendorItem vendorItem in vendor.Items)
                {
                    if (vendorItem != null &&
                        vendorItem.ItemId ==
                            "weapon_petrolcan")
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (alreadyAdded)
                    continue;

                vendor.Items.Add(
                    new VendorItem(
                        "weapon_petrolcan",
                        VendorCategory.Vehicle,
                        120,
                        3
                    )
                );
            }
        }

        private void SetBlipName(
            Blip blip,
            string name)
        {
            if (blip == null ||
                !blip.Exists())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
                name = "Store";

            Function.Call(
                Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME,
                "STRING"
            );

            Function.Call(
                Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME,
                name
            );

            Function.Call(
                Hash.END_TEXT_COMMAND_SET_BLIP_NAME,
                blip.Handle
            );
        }
    }
}