

using SurvivalNeeds.Inventory;
using SurvivalNeeds.Systems;
using GTA;
using GTA.Native;
using GTA.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace SurvivalNeeds.UI
{
    public class InventoryMenu
    {
        private readonly InventoryManager inventory;
        private readonly HungerSystem hunger;
        private readonly ThirstSystem thirst;
        private readonly StressSystem stress;
        private readonly AnimationSystem animationSystem;

        private readonly List<int> itemSlots =
            new List<int>();

        private readonly Dictionary<string, CustomSprite>
            iconSprites =
                new Dictionary<string, CustomSprite>();

        private readonly List<CustomSprite>
            rectangleSprites =
                new List<CustomSprite>();

        private readonly Dictionary<string, CustomSprite>
            textSprites =
                new Dictionary<string, CustomSprite>();

        private readonly Dictionary<string, SizeF>
            textSpriteSizes =
                new Dictionary<string, SizeF>();

        private int rectangleDrawIndex;
        private string rectangleTexturePath;

        private readonly string iconsFolder;

        private bool visible;
        private int selectedIndex;

        private const int Columns = 5;
        private const int Rows = 4;
        private const int ItemsPerPage =
            Columns * Rows;

        private const float MaximumWeight = 40f;

        private bool wPressedLastFrame;
        private bool sPressedLastFrame;
        private bool aPressedLastFrame;
        private bool dPressedLastFrame;
        private bool enterPressedLastFrame;

        private readonly Color overlayColor =
            Color.FromArgb(150, 5, 7, 15);

        private readonly Color panelColor =
            Color.FromArgb(225, 25, 26, 43);

        private readonly Color headerColor =
            Color.FromArgb(235, 32, 32, 51);

        private readonly Color slotColor =
            Color.FromArgb(230, 55, 56, 73);

        private readonly Color selectedColor =
            Color.FromArgb(245, 70, 72, 92);

        private readonly Color cyanColor =
            Color.FromArgb(255, 20, 225, 195);

        private readonly Color mutedTextColor =
            Color.FromArgb(255, 190, 192, 205);

        public InventoryMenu(
            InventoryManager inventory,
            HungerSystem hunger,
            ThirstSystem thirst,
            StressSystem stress,
            AnimationSystem animationSystem)
        {
            this.inventory = inventory;
            this.hunger = hunger;
            this.thirst = thirst;
            this.stress = stress;
            this.animationSystem = animationSystem;

            iconsFolder = GetIconsFolder();
        }

        public void Toggle()
        {
            visible = !visible;

            if (visible)
            {
                UpdateItemList();
                ResetInput();
            }
        }


        public bool Visible
        {
            get
            {
                return visible;
            }
        }

        public void Draw()
        {
            if (!visible)
                return;

            rectangleDrawIndex = 0;

            UpdateItemList();
            HandleInput();
            DisableGameplayControls();

            DrawRectangle(
                0f,
                0f,
                1f,
                1f,
                overlayColor
            );

            DrawInventoryPanel();
            DrawDetailsPanel();
        }

        private void DrawInventoryPanel()
        {
            const float panelX = 0.035f;
            const float panelY = 0.105f;
            const float panelWidth = 0.625f;
            const float panelHeight = 0.79f;

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
                0.065f,
                headerColor
            );

            DrawText(
                "PLAYER INVENTORY",
                panelX + 0.012f,
                panelY + 0.014f,
                0.48f,
                Color.White
            );

            float currentWeight =
                GetCurrentWeight();

            DrawRightText(
                currentWeight.ToString("0.00") +
                " / " +
                MaximumWeight.ToString("0.00") +
                " KG",
                panelX + panelWidth - 0.012f,
                panelY + 0.015f,
                0.36f,
                Color.White
            );

            DrawWeightBar(
                panelX + 0.012f,
                panelY + 0.052f,
                panelWidth - 0.024f,
                0.007f,
                currentWeight
            );

            DrawGrid(
                panelX + 0.012f,
                panelY + 0.080f,
                panelWidth - 0.024f,
                0.615f
            );

            DrawFooter(
                panelX,
                panelY + panelHeight - 0.075f,
                panelWidth,
                0.075f
            );
        }

        private void DrawGrid(
            float gridX,
            float gridY,
            float gridWidth,
            float gridHeight)
        {
            const float horizontalGap = 0.004f;
            const float verticalGap = 0.006f;

            float slotWidth =
                (gridWidth -
                 horizontalGap * (Columns - 1)) /
                Columns;

            float slotHeight =
                (gridHeight -
                 verticalGap * (Rows - 1)) /
                Rows;

            int page =
                selectedIndex / ItemsPerPage;

            int firstItem =
                page * ItemsPerPage;

            for (int row = 0;
                row < Rows;
                row++)
            {
                for (int column = 0;
                    column < Columns;
                    column++)
                {
                    int pagePosition =
                        row * Columns + column;

                    int itemIndex =
                        firstItem + pagePosition;

                    float x =
                        gridX +
                        column *
                        (slotWidth + horizontalGap);

                    float y =
                        gridY +
                        row *
                        (slotHeight + verticalGap);

                    DrawSlot(
                        x,
                        y,
                        slotWidth,
                        slotHeight,
                        itemIndex
                    );
                }
            }
        }

        private void DrawSlot(
            float x,
            float y,
            float width,
            float height,
            int itemIndex)
        {
            bool hasItem =
                itemIndex >= 0 &&
                itemIndex < itemSlots.Count;

            bool selected =
                hasItem &&
                itemIndex == selectedIndex;

            DrawRectangle(
                x,
                y,
                width,
                height,
                selected
                    ? selectedColor
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

            InventorySlot slot =
                inventory.Slots[
                    itemSlots[itemIndex]
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
                ShortenText(item.Name, 17),
                x + 0.005f,
                y + 0.005f,
                0.30f,
                Color.White
            );

            bool iconDrawn =
                DrawItemIcon(
                    item,
                    x + 0.018f,
                    y + 0.033f,
                    width - 0.036f,
                    height - 0.069f
                );

            if (!iconDrawn)
            {
                DrawCenteredText(
                    GetCategoryLabel(item),
                    x + width / 2f,
                    y + height * 0.45f,
                    0.42f,
                    GetCategoryColor(item)
                );
            }

            DrawText(
                slot.Quantity + "x",
                x + 0.006f,
                y + height - 0.028f,
                0.31f,
                Color.White
            );

            DrawRightText(
                (item.Weight * slot.Quantity)
                    .ToString("0.0"),
                x + width - 0.006f,
                y + height - 0.028f,
                0.29f,
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

        private void DrawDetailsPanel()
        {
            const float panelX = 0.68f;
            const float panelY = 0.105f;
            const float panelWidth = 0.285f;
            const float panelHeight = 0.79f;

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
                0.065f,
                headerColor
            );

            DrawText(
                "ITEM DETAILS",
                panelX + 0.015f,
                panelY + 0.015f,
                0.48f,
                Color.White
            );

            if (itemSlots.Count == 0)
            {
                DrawCenteredText(
                    "INVENTORY EMPTY",
                    panelX + panelWidth / 2f,
                    panelY + 0.36f,
                    0.48f,
                    mutedTextColor
                );

                return;
            }

            if (selectedIndex < 0 ||
                selectedIndex >= itemSlots.Count)
            {
                return;
            }

            InventorySlot slot =
                inventory.Slots[
                    itemSlots[selectedIndex]
                ];

            if (slot == null ||
                slot.IsEmpty ||
                slot.Item == null)
            {
                return;
            }

            InventoryItem item =
                slot.Item;

            bool largeIconDrawn =
                DrawItemIcon(
                    item,
                    panelX + 0.085f,
                    panelY + 0.085f,
                    0.115f,
                    0.145f
                );

            if (!largeIconDrawn)
            {
                DrawCenteredText(
                    GetCategoryLabel(item),
                    panelX + panelWidth / 2f,
                    panelY + 0.145f,
                    0.70f,
                    GetCategoryColor(item)
                );
            }

            DrawCenteredText(
                item.Name,
                panelX + panelWidth / 2f,
                panelY + 0.245f,
                0.49f,
                Color.White
            );

            DrawCenteredText(
                ShortenText(
                    item.Description,
                    45
                ),
                panelX + panelWidth / 2f,
                panelY + 0.285f,
                0.29f,
                mutedTextColor
            );

            DrawInformationRow(
                panelX,
                panelY + 0.345f,
                panelWidth,
                "CATEGORY",
                item.Category.ToString()
            );

            DrawInformationRow(
                panelX,
                panelY + 0.395f,
                panelWidth,
                "QUANTITY",
                slot.Quantity.ToString()
            );

            DrawInformationRow(
                panelX,
                panelY + 0.445f,
                panelWidth,
                "UNIT WEIGHT",
                item.Weight.ToString("0.00") +
                " KG"
            );

            DrawInformationRow(
                panelX,
                panelY + 0.495f,
                panelWidth,
                "TOTAL WEIGHT",
                (item.Weight * slot.Quantity)
                    .ToString("0.00") +
                " KG"
            );

            float informationRowY =
    panelY + 0.345f;

            DrawInformationRow(
                panelX,
                informationRowY,
                panelWidth,
                "CATEGORY",
                item.IsWeapon
                    ? "Weapon"
                    : item.Category.ToString()
            );

            informationRowY += 0.050f;

            DrawInformationRow(
                panelX,
                informationRowY,
                panelWidth,
                "QUANTITY",
                slot.Quantity.ToString()
            );

            informationRowY += 0.050f;

            // Show exact ammunition for weapons.
            if (item.IsWeapon)
            {
                DrawInformationRow(
                    panelX,
                    informationRowY,
                    panelWidth,
                    item.IsMeleeWeapon
                        ? "WEAPON TYPE"
                        : "AMMUNITION",
                    item.IsMeleeWeapon
                        ? "MELEE"
                        : slot.Ammo.ToString()
                );

                informationRowY += 0.050f;
            }

            DrawInformationRow(
                panelX,
                informationRowY,
                panelWidth,
                "UNIT WEIGHT",
                item.Weight.ToString("0.00") +
                " KG"
            );

            informationRowY += 0.050f;

            DrawInformationRow(
                panelX,
                informationRowY,
                panelWidth,
                "TOTAL WEIGHT",
                (item.Weight * slot.Quantity)
                    .ToString("0.00") +
                " KG"
            );

            informationRowY += 0.050f;

            float effectRowY =
                informationRowY;

            if (item.HungerRestore != 0f)
            {
                DrawInformationRow(
                    panelX,
                    effectRowY,
                    panelWidth,
                    item.HungerRestore > 0f
                        ? "HUNGER RESTORE"
                        : "HUNGER EFFECT",
                    FormatRestoreValue(
                        item.HungerRestore
                    )
                );

                effectRowY += 0.050f;
            }

            if (item.ThirstRestore != 0f)
            {
                DrawInformationRow(
                    panelX,
                    effectRowY,
                    panelWidth,
                    item.ThirstRestore > 0f
                        ? "THIRST RESTORE"
                        : "THIRST EFFECT",
                    FormatRestoreValue(
                        item.ThirstRestore
                    )
                );

                effectRowY += 0.050f;
            }

            if (item.StressRestore != 0f)
            {
                DrawInformationRow(
                    panelX,
                    effectRowY,
                    panelWidth,
                    "STRESS RELIEF",
                    FormatRestoreValue(
                        item.StressRestore
                    )
                );

                effectRowY += 0.050f;
            }

            if (item.HealthRestore != 0f)
            {
                DrawInformationRow(
                    panelX,
                    effectRowY,
                    panelWidth,
                    "HEALTH RESTORE",
                    FormatRestoreValue(
                        item.HealthRestore
                    )
                );
            }

            bool usable =
                inventory.CanUseItem(
                    itemSlots[selectedIndex]
                );

            DrawActionButton(
                panelX + 0.025f,
                panelY + panelHeight - 0.135f,
                panelWidth - 0.050f,
                0.050f,
                usable
                    ? "ENTER - USE ITEM"
                    : "ITEM CANNOT BE USED",
                usable
            );

            DrawActionButton(
                panelX + 0.025f,
                panelY + panelHeight - 0.075f,
                panelWidth - 0.050f,
                0.050f,
                "I - CLOSE",
                true
            );
        }

        private void DrawInformationRow(
            float panelX,
            float y,
            float panelWidth,
            string label,
            string value)
        {
            DrawRectangle(
                panelX + 0.015f,
                y,
                panelWidth - 0.030f,
                0.042f,
                Color.FromArgb(
                    180,
                    57,
                    58,
                    75
                )
            );

            DrawText(
                label,
                panelX + 0.025f,
                y + 0.009f,
                0.29f,
                mutedTextColor
            );

            DrawRightText(
                value,
                panelX + panelWidth - 0.025f,
                y + 0.009f,
                0.30f,
                Color.White
            );
        }

        private void DrawActionButton(
            float x,
            float y,
            float width,
            float height,
            string text,
            bool enabled)
        {
            DrawRectangle(
                x,
                y,
                width,
                height,
                enabled
                    ? Color.FromArgb(
                        230,
                        78,
                        79,
                        99
                    )
                    : Color.FromArgb(
                        180,
                        48,
                        49,
                        62
                    )
            );

            if (enabled)
            {
                DrawRectangle(
                    x,
                    y + height - 0.004f,
                    width,
                    0.004f,
                    cyanColor
                );
            }

            DrawCenteredText(
                text,
                x + width / 2f,
                y + 0.013f,
                0.33f,
                enabled
                    ? Color.White
                    : mutedTextColor
            );
        }

        private void DrawFooter(
            float x,
            float y,
            float width,
            float height)
        {
            DrawRectangle(
                x,
                y,
                width,
                height,
                headerColor
            );

            int page =
                itemSlots.Count == 0
                    ? 1
                    : selectedIndex /
                      ItemsPerPage + 1;

            int totalPages =
                itemSlots.Count == 0
                    ? 1
                    : (itemSlots.Count +
                       ItemsPerPage - 1) /
                      ItemsPerPage;

            DrawText(
                "W/A/S/D - SELECT",
                x + 0.015f,
                y + 0.021f,
                0.31f,
                mutedTextColor
            );

            DrawCenteredText(
                "ENTER - USE",
                x + width / 2f,
                y + 0.021f,
                0.31f,
                mutedTextColor
            );

            DrawRightText(
                "PAGE " +
                page +
                " / " +
                totalPages,
                x + width - 0.015f,
                y + 0.021f,
                0.31f,
                mutedTextColor
            );
        }

        private void DrawWeightBar(
            float x,
            float y,
            float width,
            float height,
            float currentWeight)
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
                currentWeight /
                MaximumWeight;

            if (percentage < 0f)
                percentage = 0f;

            if (percentage > 1f)
                percentage = 1f;

            DrawRectangle(
                x,
                y,
                width * percentage,
                height,
                currentWeight > MaximumWeight
                    ? Color.FromArgb(
                        255,
                        220,
                        55,
                        55
                    )
                    : cyanColor
            );
        }

        private void HandleInput()
        {
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

            if (itemSlots.Count > 0)
            {
                if (wPressed &&
                    !wPressedLastFrame)
                {
                    MoveSelection(-Columns);
                }

                if (sPressed &&
                    !sPressedLastFrame)
                {
                    MoveSelection(Columns);
                }

                if (aPressed &&
                    !aPressedLastFrame)
                {
                    MoveSelection(-1);
                }

                if (dPressed &&
                    !dPressedLastFrame)
                {
                    MoveSelection(1);
                }

                if (enterPressed &&
                    !enterPressedLastFrame)
                {
                    UseSelectedItem();
                }
            }

            wPressedLastFrame = wPressed;
            sPressedLastFrame = sPressed;
            aPressedLastFrame = aPressed;
            dPressedLastFrame = dPressed;

            enterPressedLastFrame =
                enterPressed;
        }

        private void MoveSelection(int amount)
        {
            if (itemSlots.Count == 0)
                return;

            selectedIndex += amount;

            while (selectedIndex < 0)
            {
                selectedIndex +=
                    itemSlots.Count;
            }

            while (selectedIndex >=
                itemSlots.Count)
            {
                selectedIndex -=
                    itemSlots.Count;
            }
        }

        private void UseSelectedItem()
        {
            if (selectedIndex < 0 ||
                selectedIndex >= itemSlots.Count)
            {   
                return;
            }


            int slotIndex =
                itemSlots[selectedIndex];


            bool used =
                inventory.UseItem(
                    slotIndex,
                    hunger,
                    thirst,
                    stress,
                    animationSystem
                );


            if (used)
            {
                // close inventory immediately
                visible = false;

                UpdateItemList();
            }
        }

        private void UpdateItemList()
        {
            itemSlots.Clear();

            for (int i = 0;
                i < inventory.Slots.Count;
                i++)
            {
                InventorySlot slot =
                    inventory.Slots[i];

                if (slot != null &&
                    !slot.IsEmpty &&
                    slot.Item != null)
                {
                    itemSlots.Add(i);
                }
            }

            if (itemSlots.Count == 0)
            {
                selectedIndex = 0;
                return;
            }

            if (selectedIndex >=
                itemSlots.Count)
            {
                selectedIndex =
                    itemSlots.Count - 1;
            }

            if (selectedIndex < 0)
                selectedIndex = 0;
        }

        private float GetCurrentWeight()
        {
            float weight = 0f;

            foreach (InventorySlot slot
                in inventory.Slots)
            {
                if (slot == null ||
                    slot.IsEmpty ||
                    slot.Item == null)
                {
                    continue;
                }

                weight +=
                    slot.Item.Weight *
                    slot.Quantity;
            }

            return weight;
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

                case ItemCategory.StressReliever:
                    return "STRESS";

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

                case ItemCategory.StressReliever:
                    return Color.FromArgb(
                        255, 180, 95, 255
                    );

                default:
                    return Color.FromArgb(
                        255, 205, 205, 215
                    );
            }
        }

        private string FormatRestoreValue(
            float value)
        {
            if (value > 0f)
            {
                return "+" +
                    value.ToString("0");
            }

            return value.ToString("0");
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
        }

        private void DrawRectangle(
            float x,
            float y,
            float width,
            float height,
            Color color)
        {
            if (string.IsNullOrWhiteSpace(
                rectangleTexturePath))
            {
                rectangleTexturePath =
                    Path.Combine(
                        iconsFolder,
                        "_ui_rectangle.png"
                    );

                try
                {
                    Directory.CreateDirectory(
                        iconsFolder
                    );

                    if (!File.Exists(
                        rectangleTexturePath))
                    {
                        using (Bitmap bitmap =
                            new Bitmap(4, 4))
                        {
                            using (Graphics graphics =
                                Graphics.FromImage(bitmap))
                            {
                                graphics.Clear(Color.White);
                            }

                            bitmap.Save(
                                rectangleTexturePath,
                                System.Drawing.Imaging
                                    .ImageFormat.Png
                            );
                        }
                    }
                }
                catch
                {
                    rectangleTexturePath = null;
                    return;
                }
            }

            CustomSprite rectangleSprite;

            if (rectangleDrawIndex >=
                rectangleSprites.Count)
            {
                rectangleSprite =
                    new CustomSprite(
                        rectangleTexturePath,
                        new SizeF(1f, 1f),
                        new PointF(0f, 0f),
                        Color.White,
                        0f,
                        false
                    );

                rectangleSprites.Add(
                    rectangleSprite
                );
            }
            else
            {
                rectangleSprite =
                    rectangleSprites[
                        rectangleDrawIndex
                    ];
            }

            rectangleDrawIndex++;

            bool accentColor =
                color.G >= 180 &&
                color.B >= 140;

            int visibleAlpha =
                accentColor
                    ? color.A
                    : Math.Min(
                        (int)color.A,
                        190
                    );

            rectangleSprite.Position =
                new PointF(
                    x * GTA.UI.Screen.Width,
                    y * GTA.UI.Screen.Height
                );

            rectangleSprite.Size =
                new SizeF(
                    width * GTA.UI.Screen.Width,
                    height * GTA.UI.Screen.Height
                );

            rectangleSprite.Color =
                Color.FromArgb(
                    visibleAlpha,
                    color.R,
                    color.G,
                    color.B
                );

            rectangleSprite.Draw();
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

            string textureKey =
                "v4|" + text + "|" +
                scale.ToString(
                    "R",
                    CultureInfo.InvariantCulture
                ) + "|" +
                color.ToArgb();

            string spriteKey =
                textureKey + "|" +
                x.ToString(
                    "R",
                    CultureInfo.InvariantCulture
                ) + "|" +
                y.ToString(
                    "R",
                    CultureInfo.InvariantCulture
                ) + "|" +
                (int)alignment + "|" +
                GTA.UI.Screen.Width + "|" +
                GTA.UI.Screen.Height;

            CustomSprite sprite;
            SizeF textureSize;

            if (!textSprites.TryGetValue(
                spriteKey,
                out sprite))
            {
                string cacheFolder =
                    Path.Combine(
                        iconsFolder,
                        "_ui_text_cache"
                    );

                try
                {
                    Directory.CreateDirectory(
                        cacheFolder
                    );

                    string texturePath =
                        Path.Combine(
                            cacheFolder,
                            GetStableHash(
                                textureKey
                            ) + ".png"
                        );

                    textureSize =
                        CreateTextTexture(
                            texturePath,
                            text,
                            scale,
                            color
                        );

                    sprite =
                        new CustomSprite(
                            texturePath,
                            textureSize,
                            new PointF(0f, 0f),
                            Color.White,
                            0f,
                            false
                        );

                    textSprites[spriteKey] =
                        sprite;

                    textSpriteSizes[spriteKey] =
                        textureSize;
                }
                catch
                {
                    return;
                }
            }
            else if (!textSpriteSizes.TryGetValue(
                spriteKey,
                out textureSize))
            {
                return;
            }

            float screenX =
                x * GTA.UI.Screen.Width;

            float screenY =
                y * GTA.UI.Screen.Height;

            if (alignment == Alignment.Center)
            {
                screenX -= textureSize.Width / 2f;
            }
            else if (alignment == Alignment.Right)
            {
                screenX -= textureSize.Width;
            }

            sprite.Position =
                new PointF(
                    screenX,
                    screenY
                );

            sprite.Size = textureSize;
            sprite.Color = Color.White;
            sprite.Draw();
        }

        private SizeF CreateTextTexture(
            string texturePath,
            string text,
            float scale,
            Color color)
        {
            if (File.Exists(texturePath))
            {
                using (Image existingImage =
                    Image.FromFile(texturePath))
                {
                    return new SizeF(
                        existingImage.Width,
                        existingImage.Height
                    );
                }
            }

            float fontSize =
                Math.Max(
                    9f,
                    scale * 30f
                );

            using (System.Drawing.Font font =
                new System.Drawing.Font(
                    "Arial",
                    fontSize,
                    FontStyle.Bold,
                    GraphicsUnit.Pixel
                ))
            {
                SizeF measuredSize;

                using (Bitmap measurementBitmap =
                    new Bitmap(1, 1))
                {
                    using (Graphics measurementGraphics =
                        Graphics.FromImage(
                            measurementBitmap
                        ))
                    {
                        measuredSize =
                            measurementGraphics.MeasureString(
                                text,
                                font
                            );
                    }
                }

                int bitmapWidth =
                    Math.Max(
                        4,
                        (int)Math.Ceiling(
                            measuredSize.Width
                        ) + 10
                    );

                int bitmapHeight =
                    Math.Max(
                        4,
                        (int)Math.Ceiling(
                            measuredSize.Height
                        ) + 10
                    );

                using (Bitmap bitmap =
                    new Bitmap(
                        bitmapWidth,
                        bitmapHeight,
                        System.Drawing.Imaging
                            .PixelFormat.Format32bppArgb
                    ))
                {
                    using (Graphics graphics =
                        Graphics.FromImage(bitmap))
                    {
                        graphics.Clear(
                            Color.Transparent
                        );

                        graphics.SmoothingMode =
                            System.Drawing.Drawing2D
                                .SmoothingMode
                                .AntiAlias;

                        graphics.TextRenderingHint =
                            System.Drawing.Text
                                .TextRenderingHint
                                .AntiAliasGridFit;

                        using (SolidBrush outlineBrush =
                            new SolidBrush(
                                Color.FromArgb(
                                    245,
                                    0,
                                    0,
                                    0
                                )
                            ))
                        {
                            for (int offsetX = -1;
                                offsetX <= 1;
                                offsetX++)
                            {
                                for (int offsetY = -1;
                                    offsetY <= 1;
                                    offsetY++)
                                {
                                    if (offsetX == 0 &&
                                        offsetY == 0)
                                    {
                                        continue;
                                    }

                                    graphics.DrawString(
                                        text,
                                        font,
                                        outlineBrush,
                                        5f + offsetX,
                                        4f + offsetY
                                    );
                                }
                            }
                        }

                        using (SolidBrush textBrush =
                            new SolidBrush(color))
                        {
                            graphics.DrawString(
                                text,
                                font,
                                textBrush,
                                5f,
                                4f
                            );
                        }
                    }

                    bitmap.Save(
                        texturePath,
                        System.Drawing.Imaging
                            .ImageFormat.Png
                    );
                }

                return new SizeF(
                    bitmapWidth,
                    bitmapHeight
                );
            }
        }

        private string GetStableHash(
            string value)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] bytes =
                    Encoding.UTF8.GetBytes(value);

                byte[] hash =
                    sha1.ComputeHash(bytes);

                return BitConverter
                    .ToString(hash)
                    .Replace("-", string.Empty);
            }
        }
    }
}
