using GTA;
using GTA.UI;
using SurvivalNeeds.Inventory;
using SurvivalNeeds.Systems;
using System;

namespace SurvivalNeeds.Police
{
    public class ConfiscationSystem
    {
        private readonly MoneySystem money;
        private readonly InventoryManager inventory;
        private readonly SaveSystem saveSystem;

        public ConfiscationSystem(
            MoneySystem money,
            InventoryManager inventory,
            SaveSystem saveSystem)
        {
            this.money = money;
            this.inventory = inventory;
            this.saveSystem = saveSystem;
        }

        //====================================================
        // CONFISCATE PLAYER
        //====================================================

        public void ConfiscatePlayer(
            int wantedLevel)
        {
            if (wantedLevel < 1)
            {
                wantedLevel = 1;
            }

            if (wantedLevel > 5)
            {
                wantedLevel = 5;
            }

            int cashBefore =
                money != null
                    ? money.Cash
                    : 0;

            int confiscatedCash =
                ApplyMoneyPenalty(
                    wantedLevel
                );

            ConfiscateWeapons();

            bool fullInventoryConfiscated =
                wantedLevel >= 3;

            if (fullInventoryConfiscated &&
                inventory != null)
            {
                inventory.Clear();
            }

            if (saveSystem != null)
            {
                saveSystem.MarkWeaponsConfiscated();
            }

            ShowPenaltyNotification(
                wantedLevel,
                cashBefore,
                confiscatedCash,
                fullInventoryConfiscated
            );
        }

        //====================================================
        // MONEY PENALTY
        //====================================================

        private int ApplyMoneyPenalty(
            int wantedLevel)
        {
            if (money == null)
            {
                return 0;
            }

            int cashBefore =
                money.Cash;

            int cashAfter =
                cashBefore;

            switch (wantedLevel)
            {
                case 1:
                    cashAfter =
                        Math.Max(
                            0,
                            cashBefore - 1000
                        );
                    break;

                case 2:
                    cashAfter =
                        Math.Max(
                            0,
                            cashBefore - 2500
                        );
                    break;

                case 3:
                    cashAfter =
                        Math.Max(
                            0,
                            cashBefore - 5000
                        );
                    break;

                case 4:
                    cashAfter =
                        Math.Max(
                            0,
                            cashBefore - 10000
                        );

                    // Remove 75% of whatever remains
                    // after the fixed fine.
                    cashAfter =
                        (int)Math.Floor(
                            cashAfter * 0.25
                        );
                    break;

                case 5:
                    cashAfter = 0;
                    break;
            }

            money.Set(
                cashAfter
            );

            return cashBefore -
                cashAfter;
        }

        //====================================================
        // WEAPON CONFISCATION
        //====================================================

        private void ConfiscateWeapons()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            player.Weapons.RemoveAll();
        }

        //====================================================
        // PENALTY MESSAGE
        //====================================================

        private void ShowPenaltyNotification(
            int wantedLevel,
            int cashBefore,
            int confiscatedCash,
            bool fullInventoryConfiscated)
        {
            string message =
                "~r~ARREST PENALTY" +
                "~n~~s~Wanted level: " +
                wantedLevel +
                " star" +
                (wantedLevel == 1
                    ? string.Empty
                    : "s") +
                "~n~Cash confiscated: ~r~$" +
                confiscatedCash.ToString("N0") +
                "~s~ / $" +
                cashBefore.ToString("N0") +
                "~n~Weapons confiscated";

            if (fullInventoryConfiscated)
            {
                message +=
                    "~n~Full inventory confiscated";
            }

            Notification.Show(
                message,
                false
            );
        }
    }
}