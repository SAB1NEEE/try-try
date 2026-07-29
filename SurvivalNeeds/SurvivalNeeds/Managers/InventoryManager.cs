using System;
using System.Collections.Generic;
using GTA;
using GTA.UI;
using SurvivalNeeds.Systems;

namespace SurvivalNeeds.Inventory
{
    public class InventoryManager
    {

        public const float MaximumWeight = 40f;
        public List<InventorySlot> Slots { get; private set; }

        public event Action InventoryChanged;

        public InventoryManager(int slotCount = 40)
        {
            Slots = new List<InventorySlot>();

            for (int i = 0; i < slotCount; i++)
            {
                Slots.Add(new InventorySlot());
            }
        }

        //====================================================
        // INVENTORY CHANGED
        //====================================================

        public void NotifyInventoryChanged()
        {
            InventoryChanged?.Invoke();
        }

        //====================================================
        // ADD ITEM
        //====================================================

        public bool AddItem(
            string itemId,
            int quantity = 1)
        {
            if (!ItemDatabase.Items.ContainsKey(itemId))
                return false;

            if (quantity <= 0)
                return false;

            InventoryItem item =
                ItemDatabase.Items[itemId];

            bool inventoryWasChanged =
                false;

            foreach (InventorySlot slot in Slots)
            {
                if (!slot.IsEmpty &&
                    slot.Item != null &&
                    slot.Item.Id == item.Id &&
                    slot.Quantity < item.MaxStack)
                {
                    int addAmount =
                        quantity;

                    if (slot.Quantity + addAmount >
                        item.MaxStack)
                    {
                        addAmount =
                            item.MaxStack -
                            slot.Quantity;
                    }

                    if (addAmount <= 0)
                        continue;

                    slot.SetItem(
                        item,
                        slot.Quantity + addAmount
                    );

                    quantity -=
                        addAmount;

                    inventoryWasChanged =
                        true;

                    if (quantity <= 0)
                    {
                        NotifyInventoryChanged();
                        return true;
                    }
                }
            }

            foreach (InventorySlot slot in Slots)
            {
                if (slot.IsEmpty)
                {
                    int addAmount =
                        quantity;

                    if (addAmount >
                        item.MaxStack)
                    {
                        addAmount =
                            item.MaxStack;
                    }

                    if (addAmount <= 0)
                        continue;

                    slot.SetItem(
                        item,
                        addAmount
                    );

                    quantity -=
                        addAmount;

                    inventoryWasChanged =
                        true;

                    if (quantity <= 0)
                    {
                        NotifyInventoryChanged();
                        return true;
                    }
                }
            }

            if (inventoryWasChanged)
            {
                NotifyInventoryChanged();
            }

            return quantity <= 0;
        }

        //====================================================
        // REMOVE ITEM
        //====================================================

        public bool RemoveItem(
            int slotIndex,
            int amount = 1)
        {
            if (slotIndex < 0 ||
                slotIndex >= Slots.Count)
            {
                return false;
            }

            InventorySlot slot =
                Slots[slotIndex];

            if (slot.IsEmpty ||
                slot.Item == null)
            {
                return false;
            }

            if (amount <= 0)
                return false;

            if (amount >
                slot.Quantity)
            {
                amount =
                    slot.Quantity;
            }

            slot.SetItem(
                slot.Item,
                slot.Quantity - amount
            );

            if (slot.Quantity <= 0)
            {
                slot.Clear();
            }

            NotifyInventoryChanged();

            return true;
        }


        //====================================================
        // CLEAR INVENTORY
        //====================================================

        public void Clear()
        {
            foreach (InventorySlot slot in Slots)
            {
                slot.Clear();
            }

            NotifyInventoryChanged();
        }


        //====================================================
        // MOVE ITEM
        //====================================================

        //====================================================
        // MOVE ITEM
        //====================================================

        public bool MoveItem(
            int slotIndex,
            InventoryManager target,
            int amount = 1)
        {
            if (target == null ||
                target.Slots == null)
            {
                return false;
            }

            if (slotIndex < 0 ||
                slotIndex >= Slots.Count)
            {
                return false;
            }

            InventorySlot sourceSlot =
                Slots[slotIndex];

            if (sourceSlot == null ||
                sourceSlot.IsEmpty ||
                sourceSlot.Item == null)
            {
                return false;
            }

            if (amount <= 0)
            {
                return false;
            }

            InventoryItem item =
                sourceSlot.Item;

            if (amount >
                sourceSlot.Quantity)
            {
                amount =
                    sourceSlot.Quantity;
            }

            //================================================
            // MOVE WEAPON WITH ITS EXACT AMMO
            //================================================

            if (item.IsWeapon)
            {
                InventorySlot emptyTargetSlot =
                    null;

                foreach (InventorySlot targetSlot
                    in target.Slots)
                {
                    if (targetSlot != null &&
                        targetSlot.IsEmpty)
                    {
                        emptyTargetSlot =
                            targetSlot;

                        break;
                    }
                }

                if (emptyTargetSlot == null)
                {
                    return false;
                }

                int savedAmmo =
                    sourceSlot.Ammo;

                if (savedAmmo < 0)
                {
                    savedAmmo = 0;
                }

                emptyTargetSlot.SetItem(
                    item,
                    1,
                    savedAmmo
                );

                RemoveItem(
                    slotIndex,
                    1
                );

                target.NotifyInventoryChanged();

                return true;
            }

            //================================================
            // MOVE NORMAL STACKABLE ITEM
            //================================================

            if (!target.AddItem(
                item.Id,
                amount))
            {
                return false;
            }

            RemoveItem(
                slotIndex,
                amount
            );

            return true;
        }

        //====================================================
        // CAN USE ITEM
        //====================================================

        public bool CanUseItem(
            int slotIndex)
        {
            if (slotIndex < 0 ||
                slotIndex >= Slots.Count)
            {
                return false;
            }

            InventorySlot slot =
                Slots[slotIndex];

            if (slot.IsEmpty ||
                slot.Item == null)
            {
                return false;
            }

            return
                slot.Item.Category ==
                    ItemCategory.Food ||

                slot.Item.Category ==
                    ItemCategory.Drink ||

                slot.Item.Category ==
                    ItemCategory.Medical ||

                slot.Item.Category ==
                    ItemCategory.StressReliever;
        }

        //====================================================
        // USE ITEM
        //====================================================

        public bool UseItem(
            int slotIndex,
            HungerSystem hunger,
            ThirstSystem thirst,
            StressSystem stress,
            AnimationSystem animationSystem)
        {
            if (slotIndex < 0 ||
                slotIndex >= Slots.Count)
            {
                return false;
            }

            InventorySlot slot =
                Slots[slotIndex];

            if (slot.IsEmpty ||
                slot.Item == null)
            {
                return false;
            }

            InventoryItem item =
                slot.Item;

            //========================
            // FOOD
            //========================

            if (item.Category ==
                ItemCategory.Food)
            {
                animationSystem.PlayEating(
                    item.Id,
                    () =>
                    {
                        hunger.Add(
                            item.HungerRestore
                        );

                        if (item.ThirstRestore != 0)
                        {
                            thirst.Add(
                                item.ThirstRestore
                            );
                        }

                        RemoveItem(
                            slotIndex,
                            1
                        );
                    }
                );

                return true;
            }

            //========================
            // DRINK
            //========================

            if (item.Category ==
                ItemCategory.Drink)
            {
                animationSystem.PlayDrinking(
                    item.Id,
                    () =>
                    {
                        thirst.Add(
                            item.ThirstRestore
                        );

                        if (item.HungerRestore != 0)
                        {
                            hunger.Add(
                                item.HungerRestore
                            );
                        }

                        RemoveItem(
                            slotIndex,
                            1
                        );
                    }
                );

                return true;
            }

            //========================
            // MEDICAL
            //========================

            if (item.Category ==
                ItemCategory.Medical)
            {
                Ped player =
                    Game.Player.Character;

                if (player == null ||
                    !player.Exists())
                {
                    return false;
                }

                int heal =
                    (int)item.HealthRestore;

                player.Health +=
                    heal;

                if (player.Health >
                    player.MaxHealth)
                {
                    player.Health =
                        player.MaxHealth;
                }

                RemoveItem(
                    slotIndex,
                    1
                );

                Notification.Show(
                    "~g~Used " +
                    item.Name
                );

                return true;
            }

            //========================
            // STRESS RELIEVER
            //========================

            if (item.Category ==
                ItemCategory.StressReliever)
            {
                if (stress == null)
                    return false;

                float reduceAmount =
                    item.StressRestore;

                if (reduceAmount <= 0)
                    return false;

                animationSystem.PlaySmoking(
                    item.Id,
                    () =>
                    {
                        stress.Reduce(
                            reduceAmount
                        );

                        RemoveItem(
                            slotIndex,
                            1
                        );

                        Notification.Show(
                            "~g~Stress reduced by " +
                            reduceAmount.ToString("0")
                        );
                    }
                );

                return true;
            }

            return false;

        }

        //====================================================
        // INVENTORY WEIGHT
        //====================================================

        public float GetCurrentWeight()
        {
            float totalWeight = 0f;

            foreach (InventorySlot slot in Slots)
            {
                if (slot == null ||
                    slot.IsEmpty ||
                    slot.Item == null ||
                    slot.Quantity <= 0)
                {
                    continue;
                }

                totalWeight +=
                    slot.Item.Weight *
                    slot.Quantity;
            }
            return totalWeight;
        }

        public bool CanCarryWeight(
            float additionalWeight,
            float maximumWeight = MaximumWeight)
        {
            if (additionalWeight < 0f)
            {
                return false;
            }

            return GetCurrentWeight() +
                additionalWeight <=
                maximumWeight;
        }

        public float GetRemainingWeight(
            float maximumWeight = MaximumWeight)
        {
            float remainingWeight =
                maximumWeight -
                GetCurrentWeight();

            if (remainingWeight < 0f)
            {
                remainingWeight = 0f;
            }

            return remainingWeight;
        }
    }
}