using GTA;
using GTA.UI;
using iFruitAddon2;
using SurvivalNeeds.VehicleStorage;
using System;
using System.Collections.Generic;

namespace SurvivalNeeds.Phone
{
    public class PersonalVehiclePhoneSystem
    {
        private readonly CustomiFruit iFruit;

        private readonly
            VehicleInventoryManager
            vehicleInventoryManager;

        private readonly HashSet<string>
            addedClaimIds =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

        private int lastContactRefreshTime;

        private const int
            ContactRefreshIntervalMilliseconds =
                2000;

        public PersonalVehiclePhoneSystem(
            VehicleInventoryManager
                vehicleInventoryManager)
        {
            this.vehicleInventoryManager =
                vehicleInventoryManager;

            iFruit =
                new CustomiFruit();

            AddMissingVehicleContacts();
        }

        public void Update()
        {
            iFruit.Update();

            int currentTime =
                Game.GameTime;

            if (currentTime -
                lastContactRefreshTime <
                ContactRefreshIntervalMilliseconds)
            {
                return;
            }

            AddMissingVehicleContacts();

            lastContactRefreshTime =
                currentTime;
        }

        private void AddMissingVehicleContacts()
        {
            List<ClaimedVehicleInfo>
                claimedVehicles =
                    vehicleInventoryManager
                        .GetClaimedVehicles();

            if (claimedVehicles == null)
            {
                return;
            }

            foreach (
                ClaimedVehicleInfo vehicleInfo
                in claimedVehicles)
            {
                if (vehicleInfo == null ||
                    string.IsNullOrWhiteSpace(
                        vehicleInfo.ClaimId) ||
                    addedClaimIds.Contains(
                        vehicleInfo.ClaimId))
                {
                    continue;
                }

                AddVehicleContact(
                    vehicleInfo
                );

                addedClaimIds.Add(
                    vehicleInfo.ClaimId
                );
            }
        }

        private void AddVehicleContact(
            ClaimedVehicleInfo vehicleInfo)
        {
            ClaimedVehicleInfo selectedVehicle =
                vehicleInfo;

            iFruitContact contact =
                new iFruitContact(
                    "Personal Vehicle - " +
                    selectedVehicle.Plate
                )
                {
                    DialTimeout = 1500,
                    Active = true,
                    Icon = ContactIcon.Blank,
                    Bold = true
                };

            contact.Answered +=
                delegate
                {
                    RequestVehicle(
                        selectedVehicle
                    );
                };

            iFruit.Contacts.Add(
                contact
            );
        }

        private void RequestVehicle(
            ClaimedVehicleInfo vehicleInfo)
        {
            /*
             * Close the GTA phone shortly after
             * the mechanic answers.
             */
            iFruit.Close(
                1000
            );

            if (vehicleInfo == null)
            {
                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                Notification.Show(
                    "~r~Player is unavailable",
                    false
                );

                return;
            }

            if (player.IsInVehicle())
            {
                Notification.Show(
                    "~y~Exit your current vehicle first",
                    false
                );

                return;
            }

            bool alreadySpawned;

            Vehicle requestedVehicle =
                vehicleInventoryManager
                    .SpawnClaimedVehicle(
                        vehicleInfo.ClaimId,
                        out alreadySpawned
                    );

            if (requestedVehicle == null ||
                !requestedVehicle.Exists())
            {
                Notification.Show(
                    "~r~The mechanic could not deliver " +
                    vehicleInfo.Plate,
                    false
                );

                return;
            }

            if (alreadySpawned)
            {
                Notification.Show(
                    "~y~" +
                    vehicleInfo.Plate +
                    " is already in the world",
                    false
                );

                return;
            }

            Notification.Show(
                "~g~Your personal vehicle has been delivered",
                false
            );

            Notification.Show(
                "~b~Plate: " +
                vehicleInfo.Plate,
                false
            );
        }
    }
}