using GTA;
using GTA.Math;
using GTA.Native;
using System;

namespace SurvivalNeeds.BankingSystem
{
    public class ATMSystem
    {
        private readonly ATMMenu atmMenu;

        private Blip nearbyATMBlip;
        private Prop currentATM;

        private bool ePressedLastFrame;

        private const float BlipVisibleDistance = 150f;
        private const float InteractionDistance = 2.0f;

        public ATMSystem(ATMMenu atmMenu)
        {
            this.atmMenu = atmMenu;
        }

        public void Update()
        {
            Ped player = Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                RemoveATMBlip();
                return;
            }

            Prop nearestATM = FindNearestATM(
                player.Position,
                BlipVisibleDistance
            );

            if (nearestATM == null ||
                !nearestATM.Exists())
            {
                RemoveATMBlip();
                return;
            }

            float distance =
                player.Position.DistanceTo(
                    nearestATM.Position
                );

            if (distance <= BlipVisibleDistance)
            {
                CreateOrUpdateATMBlip(
                    nearestATM
                );
            }
            else
            {
                RemoveATMBlip();
            }

            if (distance <= InteractionDistance)
            {
                GTA.UI.Screen.ShowHelpTextThisFrame(
                    "Press ~INPUT_CONTEXT~ to use the ATM."
                );

                bool pressed =
                    Game.IsControlPressed(
                        Control.Context
                    );

                if (pressed &&
                    !ePressedLastFrame &&
                    !atmMenu.Visible)
                {
                    atmMenu.Open();
                }

                ePressedLastFrame =
                    pressed;
            }
            else
            {
                ePressedLastFrame = false;
            }
        }

        private Prop FindNearestATM(
            Vector3 playerPosition,
            float searchRadius)
        {
            Prop closestATM = null;
            float closestDistance =
                searchRadius;

            foreach (Model model in
                 ATMLocations.ATMModels)
            {
                Prop[] props =
                    World.GetNearbyProps(
                        playerPosition,
                        searchRadius,
                        model
                    );

                foreach (Prop prop in props)
                {
                    if (prop == null ||
                        !prop.Exists())
                    {
                        continue;
                    }

                    float distance =
                        playerPosition.DistanceTo(
                            prop.Position
                        );

                    if (distance <
                        closestDistance)
                    {
                        closestDistance =
                            distance;

                        closestATM =
                            prop;
                    }
                }
            }

            return closestATM;
        }

        private void CreateOrUpdateATMBlip(
            Prop atm)
        {
            if (atm == null ||
                !atm.Exists())
            {
                RemoveATMBlip();
                return;
            }

            bool needsNewBlip =
                nearbyATMBlip == null ||
                !nearbyATMBlip.Exists() ||
                currentATM == null ||
                !currentATM.Exists() ||
                currentATM.Handle != atm.Handle;

            if (!needsNewBlip)
            {
                return;
            }

            RemoveATMBlip();

            nearbyATMBlip =
                World.CreateBlip(
                    atm.Position
                );

            if (nearbyATMBlip == null ||
                !nearbyATMBlip.Exists())
            {
                return;
            }

            nearbyATMBlip.Sprite =
                BlipSprite.DollarSign;

            nearbyATMBlip.Name =
                "ATM";

            nearbyATMBlip.Scale =
                0.75f;

            nearbyATMBlip.IsShortRange =
                true;

            currentATM =
                atm;
        }

        private void RemoveATMBlip()
        {
            if (nearbyATMBlip != null &&
                nearbyATMBlip.Exists())
            {
                nearbyATMBlip.Delete();
            }

            nearbyATMBlip =
                null;

            currentATM =
                null;
        }
    }
}