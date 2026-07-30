
using GTA;
using GTA.Native;
using GTA.UI;
using SurvivalNeeds.Inventory;
using SurvivalNeeds.VehicleStorage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SurvivalNeeds.UI
{
    public class VehicleStorageMenu
    {
        private readonly InventoryManager playerInventory;
        private readonly VehicleInventory vehicleInventory;

        private readonly Dictionary<string, CustomSprite>
            iconSprites =
                new Dictionary<string, CustomSprite>();

        private readonly string iconsFolder;

        private bool visible;

        // false = player inventory
        // true = vehicle trunk
        private bool trunkSideSelected;

        private int playerSelectedIndex;
        private int trunkSelectedIndex;

        private bool tabPressedLastFrame;
        private bool wPressedLastFrame;
        private bool sPressedLastFrame;
        private bool aPressedLastFrame;
        private bool dPressedLastFrame;
        private bool enterPressedLastFrame;

        private const int Columns = 5;
        private const int Rows = 3;
        private const int ItemsPerPage = Columns * Rows;

        private const float PlayerMaximumWeight = 40f;
        private const float TrunkMaximumWeight = 100f;

        private readonly Color overlayColor =
            Color.FromArgb(155, 5, 7, 15);

        private readonly Color panelColor =
            Color.FromArgb(225, 25, 26, 43);

        private readonly Color headerColor =
            Color.FromArgb(235, 32, 32, 51);

        private readonly Color slotColor =
            Color.FromArgb(230, 55, 56, 73);

        private readonly Color selectedSlotColor =
            Color.FromArgb(245, 70, 72, 92);

        private readonly Color cyanColor =
            Color.FromArgb(255, 20, 225, 195);

        private readonly Color mutedTextColor =
            Color.FromArgb(255, 190, 192, 205);

        public VehicleStorageMenu(
            InventoryManager playerInventory,
            VehicleInventory vehicleInventory)
        {
            this.playerInventory =
                playerInventory;

            this.vehicleInventory =
                vehicleInventory;

            iconsFolder =
                GetIconsFolder();
        }

        public bool Visible
        {
            get { return visible; }
        }

        public void Open()
        {
            visible = true;

            trunkSideSelected = false;
            playerSelectedIndex = 0;
            trunkSelectedIndex = 0;

            ResetInput();
        }

        public void Close()
        {
            visible = false;
            ResetInput();
        }

        public void Toggle()
        {
            if (visible)
                Close();
            else
                Open();
        }

        public void Draw()
        {
            if (!visible)
                return;

            List<int> playerSlots =
                GetOccupiedSlotIndexes(
                    playerInventory
                );

            List<int> trunkSlots =
                GetOccupiedSlotIndexes(
                    vehicleInventory.Inventory
                );

            ClampSelections(
                playerSlots.Count,
                trunkSlots.Count
            );

            HandleInput(
                playerSlots,
                trunkSlots
            );

            // Refresh after transferring an item.
            playerSlots =
                GetOccupiedSlotIndexes(
                    playerInventory
                );

            trunkSlots =
                GetOccupiedSlotIndexes(
                    vehicleInventory.Inventory
                );

            ClampSelections(
                playerSlots.Count,
                trunkSlots.Count
            );

            DisableGameplayControls();

            DrawRectangle(
                0f,
                0f,
                1f,
                1f,
                overlayColor
            );

            DrawInventoryPanel(
                playerInventory,
                playerSlots,
                0.025f,
                0.105f,
                0.46f,
                0.74f,
                "PLAYER INVENTORY",
                !trunkSideSelected,
                playerSelectedIndex,
                PlayerMaximumWeight
            );

            DrawInventoryPanel(
                vehicleInventory.Inventory,
                trunkSlots,
                0.515f,
                0.105f,
                0.46f,
                0.74f,
                "VEHICLE TRUNK",
                trunkSideSelected,
                trunkSelectedIndex,
                TrunkMaximumWeight
            );

            DrawFooter();
        }

        private void DrawInventoryPanel(
            InventoryManager inventory,
            List<int> occupiedSlots,
            float panelX,
            float panelY,
            float panelWidth,
            float panelHeight,
            string title,
            bool active,
            int selectedIndex,
            float maximumWeight)
        {
            if (active)
            {
                DrawRectangle(
                    panelX - 0.003f,
                    panelY - 0.003f,
                    panelWidth + 0.006f,
                    panelHeight + 0.006f,
                    cyanColor
                );
            }

            DrawRectangle(
                panelX,
                panelY,
                panelWidth,
                panelHeight,
                panelColor
            );

            DrawRectangle(
                panelX,
                panelY,
                panelWidth,
                0.063f,
                headerColor
            );

            DrawText(
                title,
                panelX + 0.012f,
                panelY + 0.014f,
                0.44f,
                active
                    ? cyanColor
                    : Color.White
            );

            float currentWeight =
                GetInventoryWeight(
                    inventory
                );

            DrawRightText(
                currentWeight.ToString("0.00") +
                " / " +
                maximumWeight.ToString("0.00") +
                " KG",
                panelX + panelWidth - 0.012f,
                panelY + 0.016f,
                0.32f,
                Color.White
            );

            DrawWeightBar(
                panelX + 0.012f,
                panelY + 0.052f,
                panelWidth - 0.024f,
                0.006f,
                currentWeight,
                maximumWeight
            );

            DrawItemGrid(
                inventory,
                occupiedSlots,
                panelX + 0.012f,
                panelY + 0.080f,
                panelWidth - 0.024f,
                0.535f,
                active,
                selectedIndex
            );

            DrawPanelInformation(
                inventory,
                occupiedSlots,
                panelX + 0.012f,
                panelY + panelHeight - 0.105f,
                panelWidth - 0.024f,
                active,
                selectedIndex
            );

            int page =
                occupiedSlots.Count == 0
                    ? 1
                    : selectedIndex /
                      ItemsPerPage + 1;

            int pageCount =
                occupiedSlots.Count == 0
                    ? 1
                    : (occupiedSlots.Count +
                       ItemsPerPage - 1) /
                      ItemsPerPage;

            DrawRightText(
                "PAGE " +
                page +
                " / " +
                pageCount,
                panelX + panelWidth - 0.012f,
                panelY + panelHeight - 0.031f,
                0.28f,
                mutedTextColor
            );
        }

        private void DrawItemGrid(
            InventoryManager inventory,
            List<int> occupiedSlots,
            float gridX,
            float gridY,
            float gridWidth,
            float gridHeight,
            bool active,
            int selectedIndex)
        {
            const float horizontalGap =
                0.004f;

            const float verticalGap =
                0.006f;

            float slotWidth =
                (gridWidth -
                 horizontalGap *
                 (Columns - 1)) /
                Columns;

            float slotHeight =
                (gridHeight -
                 verticalGap *
                 (Rows - 1)) /
                Rows;

            int page =
                selectedIndex /
                ItemsPerPage;

            int firstItem =
                page *
                ItemsPerPage;

            for (int row = 0;
                row < Rows;
                row++)
            {
                for (int column = 0;
                    column < Columns;
                    column++)
                {
                    int pagePosition =
                        row * Columns +
                        column;

                    int displayIndex =
                        firstItem +
                        pagePosition;

                    float x =
                        gridX +
                        column *
                        (slotWidth +
                         horizontalGap);

                    float y =
                        gridY +
                        row *
                        (slotHeight +
                         verticalGap);

                    DrawItemSlot(
                        inventory,
                        occupiedSlots,
                        x,
                        y,
                        slotWidth,
                        slotHeight,
                        active,
                        selectedIndex,
                        displayIndex
                    );
                }
            }
        }

        private void DrawItemSlot(
            InventoryManager inventory,
            List<int> occupiedSlots,
            float x,
            float y,
            float width,
            float height,
            bool active,
            int selectedIndex,
            int displayIndex)
        {
            bool hasItem =
                displayIndex >= 0 &&
                displayIndex <
                occupiedSlots.Count;

            bool selected =
                active &&
                hasItem &&
                displayIndex ==
                selectedIndex;

            DrawRectangle(
                x,
                y,
                width,
                height,
                selected
                    ? selectedSlotColor
                    : slotColor
            );

            if (selected)
            {
                DrawBorder(
                    x,
                    y,
                    width,
                    height,
                    0.002f,
                    cyanColor
                );
            }

            if (!hasItem)
                return;

            int actualSlotIndex =
                occupiedSlots[
                    displayIndex
                ];

            InventorySlot slot =
                inventory.Slots[
                    actualSlotIndex
                ];

            if (slot == null ||
                slot.IsEmpty ||
                slot.Item == null)
            {
                return;
            }

            InventoryItem item =
                slot.Item;

            DrawText(
                ShortenText(
                    item.Name,
                    12
                ),
                x + 0.004f,
                y + 0.004f,
                0.27f,
                Color.White
            );

            bool iconDrawn =
                DrawItemIcon(
                    item,
                    x + 0.014f,
                    y + 0.032f,
                    width - 0.028f,
                    height - 0.067f
                );

            if (!iconDrawn)
            {
                DrawCenteredText(
                    GetCategoryLabel(item),
                    x + width / 2f,
                    y + height * 0.46f,
                    0.36f,
                    GetCategoryColor(item)
                );
            }

            DrawText(
                slot.Quantity + "x",
                x + 0.005f,
                y + height - 0.026f,
                0.29f,
                Color.White
            );

            DrawRightText(
                (item.Weight *
                 slot.Quantity)
                    .ToString("0.0"),
                x + width - 0.005f,
                y + height - 0.026f,
                0.27f,
                mutedTextColor
            );

        }

        private bool DrawItemIcon(
    InventoryItem item,
    float x,
    float y,
    float width,
    float height)
        {
            if (item == null ||
                string.IsNullOrWhiteSpace(item.Icon))
            {
                return false;
            }

            string iconPath =
                Path.Combine(
                    iconsFolder,
                    item.Icon
                );

            if (!File.Exists(iconPath))
                return false;

            try
            {
                float boxWidth =
                    width * GTA.UI.Screen.Width;

                float boxHeight =
                    height * GTA.UI.Screen.Height;

                // Keep square PNG proportions.
                float iconSize =
                    Math.Min(
                        boxWidth,
                        boxHeight
                    );

                float positionX =
                    x * GTA.UI.Screen.Width +
                    (boxWidth - iconSize) / 2f;

                float positionY =
                    y * GTA.UI.Screen.Height +
                    (boxHeight - iconSize) / 2f;

                CustomSprite sprite;

                if (!iconSprites.TryGetValue(
                    iconPath,
                    out sprite))
                {
                    sprite =
                        new CustomSprite(
                            iconPath,
                            new SizeF(
                                iconSize,
                                iconSize
                            ),
                            new PointF(
                                positionX,
                                positionY
                            ),
                            Color.White,
                            0f,
                            false
                        );

                    iconSprites.Add(
                        iconPath,
                        sprite
                    );
                }

                sprite.Position =
                    new PointF(
                        positionX,
                        positionY
                    );

                sprite.Size =
                    new SizeF(
                        iconSize,
                        iconSize
                    );

                sprite.Draw();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void DrawPanelInformation(
            InventoryManager inventory,
            List<int> occupiedSlots,
            float x,
            float y,
            float width,
            bool active,
            int selectedIndex)
        {
            DrawRectangle(
                x,
                y,
                width,
                0.065f,
                headerColor
            );

            if (occupiedSlots.Count == 0)
            {
                DrawCenteredText(
                    "EMPTY",
                    x + width / 2f,
                    y + 0.018f,
                    0.34f,
                    mutedTextColor
                );

                return;
            }

            if (selectedIndex < 0 ||
                selectedIndex >=
                occupiedSlots.Count)
            {
                return;
            }

            InventorySlot slot =
                inventory.Slots[
                    occupiedSlots[
                        selectedIndex
                    ]
                ];

            if (slot == null ||
                slot.IsEmpty ||
                slot.Item == null)
            {
                return;
            }

            DrawText(
                slot.Item.Name,
                x + 0.008f,
                y + 0.009f,
                0.32f,
                active
                    ? cyanColor
                    : Color.White
            );

            string itemInformation =
    slot.Item.Description;

            if (slot.Item.IsWeapon)
            {
                if (slot.Item.IsMeleeWeapon)
                {
                    itemInformation +=
                        " | MELEE WEAPON";
                }
                else
                {
                    itemInformation +=
                        " | AMMO: " +
                        slot.Ammo;
                }
            }

            DrawText(
                ShortenText(
                    itemInformation,
                    55
                ),
                x + 0.008f,
                y + 0.035f,
                0.25f,
                mutedTextColor
            );
        }

        private void DrawFooter()
        {
            DrawRectangle(
                0.025f,
                0.865f,
                0.95f,
                0.065f,
                headerColor
            );

            DrawText(
                "TAB - SWITCH SIDE",
                0.045f,
                0.883f,
                0.32f,
                mutedTextColor
            );

            DrawCenteredText(
                "W/A/S/D - SELECT",
                0.37f,
                0.883f,
                0.32f,
                mutedTextColor
            );

            DrawCenteredText(
                "ENTER - TRANSFER ONE",
                0.64f,
                0.883f,
                0.32f,
                mutedTextColor
            );

            DrawRightText(
                "E - CLOSE",
                0.955f,
                0.883f,
                0.32f,
                mutedTextColor
            );
        }

        private void HandleInput(
            List<int> playerSlots,
            List<int> trunkSlots)
        {
            bool tabPressed =
                Game.IsKeyPressed(Keys.Tab);

            bool wPressed =
                Game.IsKeyPressed(Keys.W);

            bool sPressed =
                Game.IsKeyPressed(Keys.S);

            bool aPressed =
                Game.IsKeyPressed(Keys.A);

            bool dPressed =
                Game.IsKeyPressed(Keys.D);

            bool enterPressed =
                Game.IsKeyPressed(Keys.Enter);

            if (tabPressed &&
                !tabPressedLastFrame)
            {
                trunkSideSelected =
                    !trunkSideSelected;
            }

            int itemCount =
                trunkSideSelected
                    ? trunkSlots.Count
                    : playerSlots.Count;

            if (wPressed &&
                !wPressedLastFrame)
            {
                MoveSelection(
                    -Columns,
                    itemCount
                );
            }

            if (sPressed &&
                !sPressedLastFrame)
            {
                MoveSelection(
                    Columns,
                    itemCount
                );
            }

            if (aPressed &&
                !aPressedLastFrame)
            {
                MoveSelection(
                    -1,
                    itemCount
                );
            }

            if (dPressed &&
                !dPressedLastFrame)
            {
                MoveSelection(
                    1,
                    itemCount
                );
            }

            if (enterPressed &&
                !enterPressedLastFrame)
            {
                TransferSelectedItem(
                    playerSlots,
                    trunkSlots
                );
            }

            tabPressedLastFrame =
                tabPressed;

            wPressedLastFrame =
                wPressed;

            sPressedLastFrame =
                sPressed;

            aPressedLastFrame =
                aPressed;

            dPressedLastFrame =
                dPressed;

            enterPressedLastFrame =
                enterPressed;
        }

        private void MoveSelection(
            int amount,
            int itemCount)
        {
            if (itemCount <= 0)
                return;

            if (trunkSideSelected)
            {
                trunkSelectedIndex +=
                    amount;

                while (trunkSelectedIndex < 0)
                {
                    trunkSelectedIndex +=
                        itemCount;
                }

                while (trunkSelectedIndex >=
                    itemCount)
                {
                    trunkSelectedIndex -=
                        itemCount;
                }
            }
            else
            {
                playerSelectedIndex +=
                    amount;

                while (playerSelectedIndex < 0)
                {
                    playerSelectedIndex +=
                        itemCount;
                }

                while (playerSelectedIndex >=
                    itemCount)
                {
                    playerSelectedIndex -=
                        itemCount;
                }
            }
        }

        private void TransferSelectedItem(
            List<int> playerSlots,
            List<int> trunkSlots)
        {
            if (trunkSideSelected)
            {
                TransferFromTrunkToPlayer(
                    trunkSlots
                );
            }
            else
            {
                TransferFromPlayerToTrunk(
                    playerSlots
                );
            }
        }

        private void TransferFromPlayerToTrunk(
            List<int> playerSlots)
        {
            if (playerSlots.Count == 0)
            {
                Notification.Show(
                    "~r~Player inventory is empty"
                );

                return;
            }

            ClampPlayerSelection(
                playerSlots.Count
            );

            int actualSlotIndex =
                playerSlots[
                    playerSelectedIndex
                ];

            InventorySlot slot =
                playerInventory.Slots[
                    actualSlotIndex
                ];

            string itemName =
                slot.Item.Name;

            bool moved =
                playerInventory.MoveItem(
                    actualSlotIndex,
                    vehicleInventory.Inventory,
                    1
                );

            if (moved)
            {
                Notification.Show(
                    "~g~Stored " +
                    itemName +
                    " in vehicle trunk"
                );
            }
            else
            {
                Notification.Show(
                    "~r~Vehicle trunk is full"
                );
            }
        }

        private void TransferFromTrunkToPlayer(
            List<int> trunkSlots)
        {
            if (trunkSlots.Count == 0)
            {
                Notification.Show(
                    "~r~Vehicle trunk is empty"
                );

                return;
            }

            ClampTrunkSelection(
                trunkSlots.Count
            );

            int actualSlotIndex =
                trunkSlots[
                    trunkSelectedIndex
                ];

            InventorySlot slot =
                vehicleInventory.Inventory
                    .Slots[actualSlotIndex];

            string itemName =
                slot.Item.Name;

            bool moved =
                vehicleInventory.Inventory
                    .MoveItem(
                        actualSlotIndex,
                        playerInventory,
                        1
                    );

            if (moved)
            {
                Notification.Show(
                    "~g~Took " +
                    itemName +
                    " from vehicle trunk"
                );
            }
            else
            {
                Notification.Show(
                    "~r~Player inventory is full"
                );
            }
        }

        private List<int>
            GetOccupiedSlotIndexes(
                InventoryManager inventory)
        {
            List<int> occupiedIndexes =
                new List<int>();

            for (int i = 0;
                i < inventory.Slots.Count;
                i++)
            {
                InventorySlot slot =
                    inventory.Slots[i];

                if (slot == null ||
                    slot.IsEmpty ||
                    slot.Item == null)
                {
                    continue;
                }

                occupiedIndexes.Add(i);
            }

            return occupiedIndexes;
        }

        private void ClampSelections(
            int playerItemCount,
            int trunkItemCount)
        {
            ClampPlayerSelection(
                playerItemCount
            );

            ClampTrunkSelection(
                trunkItemCount
            );
        }

        private void ClampPlayerSelection(
            int itemCount)
        {
            if (itemCount <= 0)
            {
                playerSelectedIndex = 0;
                return;
            }

            if (playerSelectedIndex >=
                itemCount)
            {
                playerSelectedIndex =
                    itemCount - 1;
            }

            if (playerSelectedIndex < 0)
                playerSelectedIndex = 0;
        }

        private void ClampTrunkSelection(
            int itemCount)
        {
            if (itemCount <= 0)
            {
                trunkSelectedIndex = 0;
                return;
            }

            if (trunkSelectedIndex >=
                itemCount)
            {
                trunkSelectedIndex =
                    itemCount - 1;
            }

            if (trunkSelectedIndex < 0)
                trunkSelectedIndex = 0;
        }

        private float GetInventoryWeight(
            InventoryManager inventory)
        {
            float totalWeight = 0f;

            foreach (InventorySlot slot
                in inventory.Slots)
            {
                if (slot == null ||
                    slot.IsEmpty ||
                    slot.Item == null)
                {
                    continue;
                }

                totalWeight +=
                    slot.Item.Weight *
                    slot.Quantity;
            }

            return totalWeight;
        }

        private void DrawWeightBar(
            float x,
            float y,
            float width,
            float height,
            float currentWeight,
            float maximumWeight)
        {
            DrawRectangle(
                x,
                y,
                width,
                height,
                Color.FromArgb(
                    220,
                    52,
                    53,
                    66
                )
            );

            float percentage =
                maximumWeight <= 0f
                    ? 0f
                    : currentWeight /
                      maximumWeight;

            if (percentage < 0f)
                percentage = 0f;

            if (percentage > 1f)
                percentage = 1f;

            Color color =
                currentWeight >
                maximumWeight
                    ? Color.FromArgb(
                        255,
                        220,
                        55,
                        55
                    )
                    : cyanColor;

            DrawRectangle(
                x,
                y,
                width * percentage,
                height,
                color
            );
        }

        private string GetIconsFolder()
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

            string scriptsFolder =
                baseDirectory.Name.Equals(
                    "scripts",
                    StringComparison
                        .OrdinalIgnoreCase
                )
                    ? baseDirectory.FullName
                    : Path.Combine(
                        baseDirectory.FullName,
                        "scripts"
                    );

            return Path.Combine(
                scriptsFolder,
                "SurvivalNeeds",
                "icons"
            );
        }

        private string GetCategoryLabel(
            InventoryItem item)
        {
            if (item == null)
                return "ITEM";

            switch (item.Category)
            {
                case ItemCategory.Food:
                    return "FOOD";

                case ItemCategory.Drink:
                    return "DRINK";

                case ItemCategory.Medical:
                    return "MED";

                case ItemCategory.Tool:
                    return "TOOL";

                case ItemCategory.Misc:
                    return "MISC";

                default:
                    return "ITEM";
            }
        }

        private Color GetCategoryColor(
            InventoryItem item)
        {
            if (item == null)
                return mutedTextColor;

            switch (item.Category)
            {
                case ItemCategory.Food:
                    return Color.FromArgb(
                        255, 245, 175, 55
                    );

                case ItemCategory.Drink:
                    return Color.FromArgb(
                        255, 70, 175, 255
                    );

                case ItemCategory.Medical:
                    return Color.FromArgb(
                        255, 235, 70, 80
                    );

                case ItemCategory.Tool:
                    return Color.FromArgb(
                        255, 220, 150, 70
                    );

                default:
                    return Color.FromArgb(
                        255, 205, 205, 215
                    );
            }
        }

        private string ShortenText(
            string text,
            int maximumLength)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (text.Length <= maximumLength)
                return text;

            return text.Substring(
                0,
                maximumLength - 3
            ) + "...";
        }

        private void ResetInput()
        {
            tabPressedLastFrame =
                Game.IsKeyPressed(Keys.Tab);

            wPressedLastFrame =
                Game.IsKeyPressed(Keys.W);

            sPressedLastFrame =
                Game.IsKeyPressed(Keys.S);

            aPressedLastFrame =
                Game.IsKeyPressed(Keys.A);

            dPressedLastFrame =
                Game.IsKeyPressed(Keys.D);

            enterPressedLastFrame =
                Game.IsKeyPressed(Keys.Enter);
        }

        private void DisableGameplayControls()
        {
            // Disable movement.
            for (int control = 30;
                control <= 35;
                control++)
            {
                Function.Call(
                    Hash.DISABLE_CONTROL_ACTION,
                    0,
                    control,
                    true
                );
            }

            // Disable GTA weapon selection used by TAB.
            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                37,
                true
            );

            // Disable shooting and aiming.
            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                24,
                true
            );

            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                25,
                true
            );
        }

        private void DrawRectangle(
    float x,
    float y,
    float width,
    float height,
    Color color)
        {
            float centerX =
                x + width / 2f;

            float centerY =
                y + height / 2f;

            Function.Call(
                Hash.DRAW_RECT,
                centerX,
                centerY,
                width,
                height,
                color.R,
                color.G,
                color.B,
                color.A
            );
        }

        private void DrawBorder(
            float x,
            float y,
            float width,
            float height,
            float thickness,
            Color color)
        {
            DrawRectangle(
                x,
                y,
                width,
                thickness,
                color
            );

            DrawRectangle(
                x,
                y + height - thickness,
                width,
                thickness,
                color
            );

            DrawRectangle(
                x,
                y,
                thickness,
                height,
                color
            );

            DrawRectangle(
                x + width - thickness,
                y,
                thickness,
                height,
                color
            );
        }

        private void DrawText(
            string text,
            float x,
            float y,
            float scale,
            Color color)
        {
            DrawTextSprite(
                text,
                x,
                y,
                scale,
                color,
                Alignment.Left
            );
        }

        private void DrawCenteredText(
            string text,
            float x,
            float y,
            float scale,
            Color color)
        {
            DrawTextSprite(
                text,
                x,
                y,
                scale,
                color,
                Alignment.Center
            );
        }

        private void DrawRightText(
            string text,
            float x,
            float y,
            float scale,
            Color color)
        {
            DrawTextSprite(
                text,
                x,
                y,
                scale,
                color,
                Alignment.Right
            );
        }

        private void DrawTextSprite(
    string text,
    float x,
    float y,
    float scale,
    Color color,
    Alignment alignment)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            TextElement element =
                new TextElement(
                    text,
                    new PointF(
                        x * GTA.UI.Screen.Width,
                        y * GTA.UI.Screen.Height
                    ),
                    scale,
                    color
                );

            element.Alignment =
                alignment;

            element.Outline =
                true;

            element.Shadow =
                true;

            element.Draw();
        }
    }
}

