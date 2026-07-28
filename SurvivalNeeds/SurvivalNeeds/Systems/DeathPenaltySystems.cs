using GTA;
using GTA.UI;
using SurvivalNeeds.Inventory;
using System;

namespace SurvivalNeeds.Systems
{
    public class DeathPenaltySystem
    {
        private readonly InventoryManager inventory;
        private readonly MoneySystem money;
        private readonly HungerSystem hunger;
        private readonly ThirstSystem thirst;
        private readonly StressSystem stress;

        private bool wasDead;

        public DeathPenaltySystem(
            InventoryManager inventory,
            MoneySystem money,
            HungerSystem hunger,
            ThirstSystem thirst,
            StressSystem stress)
        {
            this.inventory =
                inventory ??
                throw new ArgumentNullException(
                    nameof(inventory)
                );

            this.money =
                money ??
                throw new ArgumentNullException(
                    nameof(money)
                );

            this.hunger =
                hunger ??
                throw new ArgumentNullException(
                    nameof(hunger)
                );

            this.thirst =
                thirst ??
                throw new ArgumentNullException(
                    nameof(thirst)
                );

            this.stress =
                stress ??
                throw new ArgumentNullException(
                    nameof(stress)
                );
        }

        public void Update()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            // Player is currently dead.
            // Remember the death, but do not apply the penalty yet.
            if (player.IsDead)
            {
                wasDead = true;
                return;
            }

            // The player was dead and is now alive again.
            // This means the player has respawned.
            if (wasDead)
            {
                ApplyRespawnPenalty();

                wasDead = false;
            }
        }

        private void ApplyRespawnPenalty()
        {
            money.Set(0);

            inventory.Clear();

            hunger.Set(75f);
            thirst.Set(75f);
            stress.Set(10f);

            Notification.Show(
                "~r~You died. Your on-hand cash and carried items were lost.",
                false
            );

            Notification.Show(
                "~g~Hunger and thirst restored to 75%. Stress set to 10%.",
                false
            );

            Notification.Show(
                "~g~Your bank account and vehicle storage were not affected.",
                false
            );
        }
    }
}