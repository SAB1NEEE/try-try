using GTA;
using GTA.Native;
using GTA.UI;
using SurvivalNeeds.Vendors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using SurvivalNeeds.Inventory;
using SurvivalNeeds.VehicleStorage;
using SurvivalNeeds.Loot;
using SurvivalNeeds.Systems;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

public class GunStoreMenu
{
    private enum GunStoreMode
    {
        Weapons,
        Ammo
    }

    private readonly GunStore gunStore;
    private readonly InventoryManager inventory;
    private readonly VehicleInventoryManager vehicleInventoryManager;
    private readonly MoneySystem money;
    private readonly Action saveAfterPurchase;

    private string statusMessage =
    string.Empty;

    private Color statusMessageColor =
        Color.White;

    private int statusMessageEndTime =
        0;

    private bool visible;
    private GunStoreCategory selectedCategory;
    private int selectedWeapon;
    private GunStoreMode selectedMode =
    GunStoreMode.Weapons;

    public bool Visible
    {
        get
        {
            return visible;
        }
    }

    //====================================================
    // UI CACHE
    //====================================================

    private readonly Dictionary<string, CustomSprite> iconSprites =
    new Dictionary<string, CustomSprite>();

    private readonly List<CustomSprite> rectangleSprites =
        new List<CustomSprite>();

    private readonly Dictionary<string, CustomSprite> textSprites =
        new Dictionary<string, CustomSprite>();

    private readonly Dictionary<string, SizeF> textSpriteSizes =
        new Dictionary<string, SizeF>();

    private readonly string iconsFolder;

    private string rectangleTexturePath;
    private int rectangleDrawIndex;

    //====================================================
    // INPUT STATE
    //====================================================

    private bool aPressedLastFrame;
    private bool dPressedLastFrame;

    private bool upPressedLastFrame;
    private bool downPressedLastFrame;
    private bool leftPressedLastFrame;
    private bool rightPressedLastFrame;

    private bool ePressedLastFrame;
    private bool tabPressedLastFrame;
    private bool escapePressedLastFrame;

    //====================================================
    // GRID SETTINGS
    //====================================================

    private const int GridColumns = 8;
    private const int GridRows = 2;

    private const int WeaponsPerPage =
        GridColumns * GridRows;

    //====================================================
    // COLORS
    //====================================================

    private readonly Color overlayColor =
        Color.FromArgb(
            155,
            5,
            7,
            15
        );

    private readonly Color panelColor =
        Color.FromArgb(
            225,
            25,
            26,
            43
        );

    private readonly Color headerColor =
        Color.FromArgb(
            235,
            32,
            32,
            51
        );

    private readonly Color slotColor =
        Color.FromArgb(
            230,
            55,
            56,
            73
        );

    private readonly Color selectedColor =
        Color.FromArgb(
            245,
            70,
            72,
            92
        );

    private readonly Color cyanColor =
        Color.FromArgb(
            255,
            20,
            225,
            195
        );

    private readonly Color mutedTextColor =
        Color.FromArgb(
            255,
            190,
            192,
            205
        );

    private readonly Color greenColor =
        Color.FromArgb(
            255,
            70,
            220,
            125
        );

    //====================================================
    // CONSTRUCTOR
    //====================================================

    public GunStoreMenu(
    GunStore gunStore,
    InventoryManager inventory,
    VehicleInventoryManager vehicleInventoryManager,
    MoneySystem money,
    Action saveAfterPurchase)
    {
        this.gunStore =
            gunStore;

        this.inventory =
            inventory;

        this.vehicleInventoryManager =
            vehicleInventoryManager;

        this.money =
            money;

        this.saveAfterPurchase =
            saveAfterPurchase;

        selectedCategory =
            GunStoreCategory.Pistols;

        selectedWeapon = 0;
        visible = false;

        iconsFolder =
            GetIconsFolder();
    }

    //====================================================
    // OPEN
    //====================================================

    public void Open()
    {
        visible = true;
        selectedWeapon = 0;

        selectedMode =
            GunStoreMode.Weapons;

        statusMessage =
            string.Empty;

        ResetInput();
    }

    //====================================================
    // CLOSE
    //====================================================

    public void Close()
    {
        visible = false;
        selectedWeapon = 0;

        ResetInput();
    }

    //====================================================
    // UPDATE
    //====================================================

    public void Update()
    {
        if (!visible)
            return;

        rectangleDrawIndex = 0;

        HandleInput();

        if (!visible)
            return;

        DisableGameplayControls();
        Draw();
    }

    //====================================================
    // MAIN DRAW
    //====================================================

    private void Draw()
    {
        DrawRectangle(
            0f,
            0f,
            1f,
            1f,
            overlayColor
        );

        DrawMainPanel();
        DrawCategoryBar();
        DrawWeaponGrid();
        DrawDetailsPanel();
        DrawFooter();
    }

    //====================================================
    // MAIN PANEL
    //====================================================

    private void DrawMainPanel()
    {
        const float panelX = 0.025f;
        const float panelY = 0.055f;
        const float panelWidth = 0.95f;
        const float panelHeight = 0.69f;

        DrawRectangle(
            panelX - 0.003f,
            panelY - 0.003f,
            panelWidth + 0.006f,
            panelHeight + 0.006f,
            cyanColor
        );

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
            0.075f,
            headerColor
        );

        DrawCenteredText(
            "AMMU-NATION",
            0.50f,
            panelY + 0.017f,
            0.58f,
            Color.White
        );

        DrawText(
    selectedMode ==
        GunStoreMode.Weapons
            ? "WEAPONS STORE"
            : "AMMUNITION STORE",
            panelX + 0.015f,
            panelY + 0.024f,
            0.27f,
            selectedMode ==
            GunStoreMode.Weapons
            ? Color.White
            : cyanColor
        );

        DrawRightText(
            "AUTHORIZED DEALER",
            panelX + panelWidth - 0.015f,
            panelY + 0.024f,
            0.27f,
            mutedTextColor
        );
    }

    //====================================================
    // CATEGORY BAR
    //====================================================

    private void DrawCategoryBar()
    {
        const float barX = 0.040f;
        const float barY = 0.150f;
        const float barWidth = 0.920f;
        const float barHeight = 0.065f;

        DrawRectangle(
            barX,
            barY,
            barWidth,
            barHeight,
            headerColor
        );

        Array categoryValues =
            Enum.GetValues(
                typeof(GunStoreCategory)
            );

        if (categoryValues.Length == 0)
            return;

        float categoryWidth =
            barWidth /
            categoryValues.Length;

        for (int index = 0;
            index < categoryValues.Length;
            index++)
        {
            GunStoreCategory category =
                (GunStoreCategory)
                categoryValues.GetValue(index);

            float x =
                barX +
                index *
                categoryWidth;

            bool selected =
                category.Equals(
                    selectedCategory
                );

            if (selected)
            {
                DrawRectangle(
                    x,
                    barY,
                    categoryWidth,
                    barHeight,
                    selectedColor
                );

                DrawRectangle(
                    x,
                    barY +
                    barHeight -
                    0.004f,
                    categoryWidth,
                    0.004f,
                    cyanColor
                );
            }

            DrawCenteredText(
                FormatCategoryName(
                    category.ToString()
                ),
                x +
                categoryWidth / 2f,
                barY + 0.018f,
                0.31f,
                selected
                    ? cyanColor
                    : Color.White
            );
        }

        DrawText(
            "A",
            barX + 0.008f,
            barY + 0.019f,
            0.28f,
            cyanColor
        );

        DrawRightText(
            "D",
            barX + barWidth - 0.008f,
            barY + 0.019f,
            0.28f,
            cyanColor
        );
    }

    //====================================================
    // WEAPON GRID
    //====================================================

    private void DrawWeaponGrid()
    {
        const float gridX = 0.040f;
        const float gridY = 0.235f;
        const float gridWidth = 0.920f;
        const float gridHeight = 0.475f;

        const float horizontalGap = 0.007f;
        const float verticalGap = 0.010f;

        float slotWidth =
            (
                gridWidth -
                horizontalGap *
                (GridColumns - 1)
            ) /
            GridColumns;

        float slotHeight =
            (
                gridHeight -
                verticalGap *
                (GridRows - 1)
            ) /
            GridRows;

        for (int row = 0;
            row < GridRows;
            row++)
        {
            for (int column = 0;
                column < GridColumns;
                column++)
            {
                int weaponIndex =
                    row *
                    GridColumns +
                    column;

                float x =
                    gridX +
                    column *
                    (
                        slotWidth +
                        horizontalGap
                    );

                float y =
                    gridY +
                    row *
                    (
                        slotHeight +
                        verticalGap
                    );

                DrawWeaponSlot(
                    x,
                    y,
                    slotWidth,
                    slotHeight,
                    weaponIndex
                );
            }
        }
    }

    //====================================================
    // WEAPON SLOT
    //====================================================

    private void DrawWeaponSlot(
        float x,
        float y,
        float width,
        float height,
        int weaponIndex)
    {
        List<GunStoreItem> categoryItems =
            GetCurrentCategoryItems();

        bool hasWeapon =
            weaponIndex >= 0 &&
            weaponIndex < categoryItems.Count;

        bool selected =
            hasWeapon &&
            weaponIndex == selectedWeapon;

        DrawRectangle(
            x,
            y,
            width,
            height,
            hasWeapon
                ? selected
                    ? selectedColor
                    : slotColor
                : Color.FromArgb(
                    150,
                    38,
                    39,
                    52
                )
        );

        if (selected)
        {
            DrawBorder(
                x,
                y,
                width,
                height,
                0.003f,
                cyanColor
            );
        }

        if (!hasWeapon)
            return;

        GunStoreItem item =
            categoryItems[weaponIndex];

        if (item == null)
            return;

        DrawCenteredText(
            ShortenText(
                item.Name.ToUpperInvariant(),
                18
            ),
            x + width / 2f,
            y + 0.010f,
            0.26f,
            selected
                ? cyanColor
                : Color.White
        );

        bool iconDrawn =
            DrawWeaponIcon(
                item,
                x + 0.010f,
                y + 0.040f,
                width - 0.020f,
                height - 0.102f
            );

        if (!iconDrawn)
        {
            DrawCenteredText(
                "NO ICON",
                x + width / 2f,
                y + height * 0.46f,
                0.27f,
                mutedTextColor
            );
        }

        DrawRectangle(
            x,
            y + height - 0.055f,
            width,
            0.055f,
            headerColor
        );

        DrawText(
            "$" +
            item.Price.ToString("N0"),
            x + 0.006f,
            y + height - 0.038f,
            0.27f,
            greenColor
        );

        DrawRightText(
            item.Weight.ToString("0.00") +
            " KG",
            x + width - 0.006f,
            y + height - 0.038f,
            0.24f,
            selected
                ? cyanColor
                : mutedTextColor
        );

        string inventoryItemId =
    GetWeaponInventoryItemId(
        item.WeaponHash
    );

        bool carried =
            InventoryContainsItem(
                inventoryItemId
            );

        bool storedInTrunk =
            VehicleTrunksContainItem(
                inventoryItemId
            );

        bool owned =
            carried ||
            storedInTrunk;

        if (selectedMode ==
            GunStoreMode.Weapons)
        {
            if (carried)
            {
                DrawCenteredText(
                    "OWNED",
                    x + width / 2f,
                    y + height - 0.020f,
                    0.21f,
                    cyanColor
                );
            }
            else if (storedInTrunk)
            {
                DrawCenteredText(
                    "OWNED IN TRUNK",
                    x + width / 2f,
                    y + height - 0.020f,
                    0.18f,
                    Color.Gold
                );
            }
        }
        else
        {
            if (item.IsMelee)
            {
                DrawCenteredText(
                    "NO AMMO",
                    x + width / 2f,
                    y + height - 0.020f,
                    0.21f,
                    mutedTextColor
                );
            }
            else if (!carried)
            {
                DrawCenteredText(
                    "NOT OWNED",
                    x + width / 2f,
                    y + height - 0.020f,
                    0.21f,
                    Color.IndianRed
                );
            }
            else
            {
                DrawCenteredText(
                    item.AmmoPackAmount +
                    " ROUNDS - $" +
                    item.AmmoPackPrice.ToString("N0"),
                    x + width / 2f,
                    y + height - 0.020f,
                    0.18f,
                    greenColor
                );
            }
        }
    }

    //====================================================
    // DETAILS PANEL
    //====================================================

    private void DrawDetailsPanel()
    {
        const float panelX = 0.025f;
        const float panelY = 0.765f;
        const float panelWidth = 0.95f;
        const float panelHeight = 0.125f;

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
            0.008f,
            panelHeight,
            cyanColor
        );

        GunStoreItem item =
            GetSelectedWeaponItem();

        if (item == null)
        {
            DrawCenteredText(
                "NO WEAPONS AVAILABLE IN THIS CATEGORY",
                panelX + panelWidth / 2f,
                panelY + 0.043f,
                0.36f,
                mutedTextColor
            );

            return;
        }

        const float iconWidth = 0.075f;

        bool iconDrawn =
            DrawWeaponIcon(
                item,
                panelX + 0.018f,
                panelY + 0.015f,
                iconWidth,
                panelHeight - 0.030f
            );

        if (!iconDrawn)
        {
            DrawCenteredText(
                "NO ICON",
                panelX + 0.055f,
                panelY + 0.049f,
                0.24f,
                mutedTextColor
            );
        }

        DrawText(
            item.Name,
            panelX + 0.110f,
            panelY + 0.012f,
            0.43f,
            Color.White
        );

        DrawText(
            ShortenText(
                item.Description,
                65
            ),
            panelX + 0.110f,
            panelY + 0.047f,
            0.27f,
            mutedTextColor
        );

        DrawText(
            "PRICE: $" +
            item.Price.ToString("N0"),
            panelX + 0.110f,
            panelY + 0.082f,
            0.27f,
            greenColor
        );

        DrawText(
            "WEIGHT: " +
            item.Weight.ToString("0.00") +
            " KG",
            panelX + 0.245f,
            panelY + 0.082f,
            0.27f,
            mutedTextColor
        );

        string ammunitionText =
            item.IsMelee
                ? "MELEE WEAPON"
                : "STARTING AMMO: " +
                  item.StartingAmmo;

        DrawText(
            ammunitionText,
            panelX + 0.400f,
            panelY + 0.082f,
            0.27f,
            mutedTextColor
        );

        string inventoryItemId =
    GetWeaponInventoryItemId(
        item.WeaponHash
    );

        bool carried =
            InventoryContainsItem(
                inventoryItemId
            );

        bool storedInTrunk =
            VehicleTrunksContainItem(
                inventoryItemId
            );

        bool owned =
            carried ||
            storedInTrunk;

        string actionText;
        Color actionColor;

        if (selectedMode ==
            GunStoreMode.Weapons)
        {
            if (carried)
            {
                actionText =
                    "ALREADY OWNED";

                actionColor =
                    cyanColor;
            }
            else if (storedInTrunk)
            {
                actionText =
                    "OWNED IN VEHICLE TRUNK";

                actionColor =
                    Color.Gold;
            }
            else
            {
                actionText =
                    "PRESS E TO BUY";

                actionColor =
                    greenColor;
            }
        }
        else
        {
            if (item.IsMelee)
            {
                actionText =
                    "NO AMMUNITION";

                actionColor =
                    mutedTextColor;
            }
            else if (!carried)
            {
                actionText =
                    storedInTrunk
                        ? "WEAPON IS IN VEHICLE TRUNK"
                        : "WEAPON NOT OWNED";

                actionColor =
                    storedInTrunk
                        ? Color.Gold
                        : Color.IndianRed;
            }
            else
            {
                actionText =
                    "E: BUY " +
                    item.AmmoPackAmount +
                    " ROUNDS - $" +
                    item.AmmoPackPrice.ToString("N0");

                actionColor =
                    greenColor;
            }
        }

        DrawRightText(
            actionText,
            panelX + panelWidth - 0.025f,
            panelY + 0.045f,
            0.35f,
            actionColor
        );
    }

    //====================================================
    // FOOTER
    //====================================================

    private void DrawFooter()
    {
        const float footerX = 0.025f;
        const float footerY = 0.910f;
        const float footerWidth = 0.95f;
        const float footerHeight = 0.060f;

        DrawRectangle(
            footerX,
            footerY,
            footerWidth,
            footerHeight,
            headerColor
        );

        DrawText(
            "TAB - WEAPONS / AMMO",
            footerX + 0.018f,
            footerY + 0.018f,
            0.29f,
            mutedTextColor
        );

        DrawCenteredText(
            "ARROW KEYS - SELECT WEAPON",
            0.43f,
            footerY + 0.018f,
            0.29f,
            mutedTextColor
        );

        DrawCenteredText(
    selectedMode ==
        GunStoreMode.Weapons
            ? "E - BUY WEAPON"
            : "E - BUY AMMO",
            0.73f,
            footerY + 0.018f,
            0.29f,
            cyanColor
        );

        DrawRightText(
            "ESC - CLOSE",
            footerX + footerWidth - 0.018f,
            footerY + 0.018f,
            0.29f,
            mutedTextColor
        );

        if (!string.IsNullOrWhiteSpace(
            statusMessage) &&
            Game.GameTime <
            statusMessageEndTime)
        {
            DrawCenteredText(
                statusMessage,
                0.50f,
                footerY - 0.030f,
                0.30f,
                statusMessageColor
            );
        }
    }

    //====================================================
    // INPUT
    //====================================================

    private void HandleInput()
    {
        bool aPressed =
            Game.IsKeyPressed(
                Keys.A
            );

        bool dPressed =
            Game.IsKeyPressed(
                Keys.D
            );

        bool upPressed =
            Game.IsKeyPressed(
                Keys.Up
            );

        bool downPressed =
            Game.IsKeyPressed(
                Keys.Down
            );

        bool leftPressed =
            Game.IsKeyPressed(
                Keys.Left
            );

        bool rightPressed =
            Game.IsKeyPressed(
                Keys.Right
            );

        bool ePressed =
            Game.IsKeyPressed(
                Keys.E
            );

        bool tabPressed =
            Game.IsKeyPressed(
                Keys.Tab
            );

        bool escapePressed =
            Game.IsKeyPressed(
                Keys.Escape
            );

        if (aPressed &&
            !aPressedLastFrame)
        {
            MoveCategory(-1);
        }

        if (dPressed &&
            !dPressedLastFrame)
        {
            MoveCategory(1);
        }

        if (upPressed &&
            !upPressedLastFrame)
        {
            MoveWeaponSelection(
                -GridColumns
            );
        }

        if (downPressed &&
            !downPressedLastFrame)
        {
            MoveWeaponSelection(
                GridColumns
            );
        }

        if (leftPressed &&
            !leftPressedLastFrame)
        {
            MoveWeaponSelection(-1);
        }

        if (rightPressed &&
            !rightPressedLastFrame)
        {
            MoveWeaponSelection(1);
        }

        if (tabPressed &&
            !tabPressedLastFrame)
        {
            ToggleStoreMode();
        }

        if (ePressed &&
            !ePressedLastFrame)
        {
            PurchaseCurrentSelection();
        }

        if (escapePressed &&
            !escapePressedLastFrame)
        {
            Close();
        }

        aPressedLastFrame =
            aPressed;

        dPressedLastFrame =
            dPressed;

        upPressedLastFrame =
            upPressed;

        downPressedLastFrame =
            downPressed;

        leftPressedLastFrame =
            leftPressed;

        rightPressedLastFrame =
            rightPressed;

        ePressedLastFrame =
            ePressed;

        tabPressedLastFrame =
            tabPressed;

        escapePressedLastFrame =
            escapePressed;
    }

    //====================================================
    // TOGGLE STORE MODE
    //====================================================

    private void ToggleStoreMode()
    {
        if (selectedMode ==
            GunStoreMode.Weapons)
        {
            selectedMode =
                GunStoreMode.Ammo;

            ShowStoreMessage(
                "AMMUNITION TAB",
                cyanColor
            );
        }
        else
        {
            selectedMode =
                GunStoreMode.Weapons;

            ShowStoreMessage(
                "WEAPONS TAB",
                cyanColor
            );
        }

        selectedWeapon = 0;
    }

    //====================================================
    // PURCHASE CURRENT SELECTION
    //====================================================

    private void PurchaseCurrentSelection()
    {
        GunStoreItem item =
            GetSelectedWeaponItem();

        if (item == null)
        {
            ShowStoreMessage(
                "No item selected.",
                Color.IndianRed
            );

            return;
        }

        if (selectedMode ==
            GunStoreMode.Weapons)
        {
            PurchaseSelectedWeapon();
        }
        else
        {
            PurchaseSelectedAmmo(
                item
            );
        }
    }

    //====================================================
    // PURCHASE SELECTED WEAPON
    //====================================================

    private void PurchaseSelectedWeapon()
    {
        GunStoreItem item =
            GetSelectedWeaponItem();

        if (item == null)
        {
            Notification.Show(
                "~r~No weapon selected."
            );

            return;
        }

        if (gunStore == null)
        {
            Notification.Show(
                "~r~Gun store unavailable."
            );

            return;
        }

        if (inventory == null)
        {
            Notification.Show(
                "~r~Inventory unavailable."
            );

            return;
        }

        if (money == null)
        {
            Notification.Show(
                "~r~Money system unavailable."
            );

            return;
        }

        string inventoryItemId =
    GetWeaponInventoryItemId(
        item.WeaponHash
    );

        if (string.IsNullOrWhiteSpace(
            inventoryItemId))
        {
            ShowStoreMessage(
                "This weapon has no inventory item.",
                Color.IndianRed
            );

            return;
        }

        if (InventoryContainsItem(
            inventoryItemId))
        {
            ShowStoreMessage(
                "You are already carrying the " +
                item.Name +
                ".",
                Color.Gold
            );

            return;
        }

        if (VehicleTrunksContainItem(
            inventoryItemId))
        {
            ShowStoreMessage(
                "The " +
                item.Name +
                " is stored in a vehicle trunk.",
                Color.Gold
            );

            return;
        }

        if (!HasEmptyInventorySlot())
        {
            ShowStoreMessage(
                "Your inventory has no empty slots.",
                Color.IndianRed
            );

            return;
        }

        float currentWeight =
            GetTotalCarriedWeight();

        float weightAfterPurchase =
            currentWeight +
            item.Weight;

        if (weightAfterPurchase >
            InventoryManager.MaximumWeight)
        {
            Notification.Show(
                "~r~Cannot buy " +
                item.Name +
                ". Weight would be " +
                weightAfterPurchase
                    .ToString("0.00") +
                " / " +
                InventoryManager.MaximumWeight
                    .ToString("0.00") +
                " KG."
            );

            return;
        }

        if (!money.CanAfford(
            item.Price))
        {
            Notification.Show(
                "~r~You cannot afford the " +
                item.Name +
                "."
            );

            return;
        }

        string resultMessage;

        bool purchased =
        gunStore.PurchaseWeapon(
        item,
        price =>
            money.SpendMoney(
                price
            ),
        null,
        out resultMessage
        );

        if (purchased)
        {
            bool addedToInventory =
                inventory.AddItem(
                    inventoryItemId,
                    1
                );

            if (!addedToInventory)
            {
                ShowStoreMessage(
                    "Weapon purchase succeeded, but it could not be added to inventory.",
                    Color.IndianRed
                );

                return;
            }

            saveAfterPurchase?.Invoke();

            PlayPurchaseSound();

            ShowStoreMessage(
                resultMessage,
                greenColor
            );
        }
        else
        {
            ShowStoreMessage(
                resultMessage,
                Color.IndianRed
            );
        }
    }

    //====================================================
    // PURCHASE SELECTED AMMUNITION
    //====================================================

    private void PurchaseSelectedAmmo(
    GunStoreItem item)
    {
        if (item == null)
        {
            Notification.Show(
                "~r~No weapon selected."
            );

            return;
        }

        if (item.IsMelee)
        {
            ShowStoreMessage(
                item.Name +
                " does not use ammunition.",
                Color.Gold
            );

            return;
        }

        if (money == null)
        {
            Notification.Show(
                "~r~Money system unavailable."
            );

            return;
        }

        if (inventory == null ||
            inventory.Slots == null)
        {
            Notification.Show(
                "~r~Inventory unavailable."
            );

            return;
        }

        string inventoryItemId =
            GetWeaponInventoryItemId(
                item.WeaponHash
            );

        if (string.IsNullOrWhiteSpace(
            inventoryItemId))
        {
            ShowStoreMessage(
                "This weapon has no inventory item.",
                Color.IndianRed
            );

            return;
        }

        InventorySlot weaponSlot =
            FindInventorySlot(
                inventoryItemId
            );

        if (weaponSlot == null)
        {
            ShowStoreMessage(
                "Weapon not found in player inventory.",
                Color.IndianRed
            );

            return;
        }

        string resultMessage;
        int newAmmoAmount;

        bool purchased =
            gunStore.PurchaseAmmo(
                item,
                price =>
                    money.SpendMoney(
                        price
                    ),
                out newAmmoAmount,
                out resultMessage
            );

        if (!purchased)
        {
            ShowStoreMessage(
                resultMessage,
                Color.IndianRed
            );

            return;
        }

        if (newAmmoAmount < 0)
        {
            newAmmoAmount = 0;
        }

        // Store the exact new GTA ammo count
        // in the matching inventory weapon slot.
        weaponSlot.SetAmmo(
            newAmmoAmount
        );

        // Save only after the inventory slot
        // contains the updated ammo count.
        saveAfterPurchase?.Invoke();

        PlayPurchaseSound();

        ShowStoreMessage(
            resultMessage,
            greenColor
        );
    }

    //====================================================
    // STORE STATUS MESSAGE
    //====================================================

    private void ShowStoreMessage(
        string message,
        Color color,
        int duration = 3000)
    {
        statusMessage =
            message ?? string.Empty;

        statusMessageColor =
            color;

        statusMessageEndTime =
            Game.GameTime +
            duration;
    }

    //====================================================
// PLAY PURCHASE SOUND
//====================================================

private void PlayPurchaseSound()
{
    Function.Call(
        Hash.PLAY_SOUND_FRONTEND,
        -1,
        "PURCHASE",
        "HUD_LIQUOR_STORE_SOUNDSET",
        true
    );
}

    //====================================================
    // WEAPON INVENTORY ITEM ID
    //====================================================

    private string GetWeaponInventoryItemId(
        WeaponHash weaponHash)
    {
        switch (weaponHash)
        {
            case WeaponHash.Pistol:
                return "weapon_pistol";

            case WeaponHash.CombatPistol:
                return "weapon_combatpistol";

            case WeaponHash.MicroSMG:
                return "weapon_microsmg";

            case WeaponHash.SMG:
                return "weapon_smg";

            case WeaponHash.PumpShotgun:
                return "weapon_pumpshotgun";

            case WeaponHash.AssaultRifle:
                return "weapon_assaultrifle";

            case WeaponHash.CarbineRifle:
                return "weapon_carbinerifle";

            case WeaponHash.Knife:
                return "weapon_knife";

            case WeaponHash.Bat:
                return "weapon_bat";

            case WeaponHash.Crowbar:
                return "weapon_crowbar";

            default:
                return null;
        }
    }

    //====================================================
    // INVENTORY CONTAINS ITEM
    //====================================================

    private bool InventoryContainsItem(
        string itemId)
    {
        if (inventory == null ||
            inventory.Slots == null ||
            string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        foreach (InventorySlot slot
            in inventory.Slots)
        {
            if (slot == null ||
                slot.IsEmpty ||
                slot.Item == null)
            {
                continue;
            }

            if (string.Equals(
                slot.Item.Id,
                itemId,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    //====================================================
    // VEHICLE TRUNKS CONTAIN ITEM
    //====================================================

    private bool VehicleTrunksContainItem(
        string itemId)
    {
        if (vehicleInventoryManager == null ||
            vehicleInventoryManager.Vehicles == null ||
            string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        foreach (
            KeyValuePair<string, VehicleInventory>
            vehiclePair in
            vehicleInventoryManager.Vehicles)
        {
            VehicleInventory vehicleInventory =
                vehiclePair.Value;

            if (vehicleInventory == null ||
                vehicleInventory.Inventory == null ||
                vehicleInventory.Inventory.Slots == null)
            {
                continue;
            }

            foreach (InventorySlot slot
                in vehicleInventory.Inventory.Slots)
            {
                if (slot == null ||
                    slot.IsEmpty ||
                    slot.Item == null)
                {
                    continue;
                }

                if (string.Equals(
                    slot.Item.Id,
                    itemId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    //====================================================
    // FIND INVENTORY SLOT
    //====================================================

    private InventorySlot FindInventorySlot(
        string itemId)
    {
        if (inventory == null ||
            inventory.Slots == null ||
            string.IsNullOrWhiteSpace(
                itemId))
        {
            return null;
        }

        foreach (InventorySlot slot
            in inventory.Slots)
        {
            if (slot == null ||
                slot.IsEmpty ||
                slot.Item == null)
            {
                continue;
            }

            if (string.Equals(
                slot.Item.Id,
                itemId,
                StringComparison.OrdinalIgnoreCase))
            {
                return slot;
            }
        }

        return null;
    }

    //====================================================
    // EMPTY INVENTORY SLOT
    //====================================================

    private bool HasEmptyInventorySlot()
    {
        if (inventory == null ||
            inventory.Slots == null)
        {
            return false;
        }

        foreach (InventorySlot slot
            in inventory.Slots)
        {
            if (slot != null &&
                slot.IsEmpty)
            {
                return true;
            }
        }

        return false;
    }

    //====================================================
    // TOTAL CARRIED WEIGHT
    //====================================================

    private float GetTotalCarriedWeight()
    {
        if (inventory == null)
            return 0f;

        return inventory.GetCurrentWeight();
    }

    //====================================================
    // CATEGORY NAVIGATION
    //====================================================

    private void MoveCategory(
        int amount)
    {
        Array categoryValues =
            Enum.GetValues(
                typeof(GunStoreCategory)
            );

        if (categoryValues.Length == 0)
            return;

        int currentIndex =
            Array.IndexOf(
                categoryValues,
                selectedCategory
            );

        if (currentIndex < 0)
            currentIndex = 0;

        currentIndex += amount;

        while (currentIndex < 0)
        {
            currentIndex +=
                categoryValues.Length;
        }

        while (currentIndex >=
            categoryValues.Length)
        {
            currentIndex -=
                categoryValues.Length;
        }

        selectedCategory =
            (GunStoreCategory)
            categoryValues.GetValue(
                currentIndex
            );

        selectedWeapon = 0;
    }

    //====================================================
    // WEAPON NAVIGATION
    //====================================================

    private void MoveWeaponSelection(
        int amount)
    {
        List<GunStoreItem> categoryItems =
            GetCurrentCategoryItems();

        int count =
            categoryItems.Count;

        if (count <= 0)
        {
            selectedWeapon = 0;
            return;
        }

        int currentColumn =
            selectedWeapon %
            GridColumns;

        int currentRow =
            selectedWeapon /
            GridColumns;

        if (amount == -1)
        {
            selectedWeapon--;

            if (selectedWeapon < 0)
            {
                selectedWeapon =
                    count - 1;
            }
        }
        else if (amount == 1)
        {
            selectedWeapon++;

            if (selectedWeapon >= count)
            {
                selectedWeapon = 0;
            }
        }
        else if (amount == -GridColumns)
        {
            int targetIndex =
                selectedWeapon -
                GridColumns;

            if (targetIndex >= 0)
            {
                selectedWeapon =
                    targetIndex;
            }
            else
            {
                int lastRow =
                    (count - 1) /
                    GridColumns;

                int bottomIndex =
                    lastRow *
                    GridColumns +
                    currentColumn;

                if (bottomIndex >= count)
                {
                    bottomIndex =
                        count - 1;
                }

                selectedWeapon =
                    bottomIndex;
            }
        }
        else if (amount == GridColumns)
        {
            int targetIndex =
                selectedWeapon +
                GridColumns;

            if (targetIndex < count)
            {
                selectedWeapon =
                    targetIndex;
            }
            else
            {
                int topIndex =
                    currentColumn;

                if (topIndex >= count)
                {
                    topIndex = 0;
                }

                selectedWeapon =
                    topIndex;
            }
        }

        ClampWeaponSelection();
    }

    //====================================================
    // CURRENT CATEGORY ITEMS
    //====================================================

    private List<GunStoreItem>
        GetCurrentCategoryItems()
    {
        if (gunStore == null)
        {
            return new List<GunStoreItem>();
        }

        List<GunStoreItem> categoryItems =
            gunStore.GetItemsByCategory(
                selectedCategory
            );

        return categoryItems ??
            new List<GunStoreItem>();
    }

    //====================================================
    // SELECTED WEAPON
    //====================================================

    private GunStoreItem
        GetSelectedWeaponItem()
    {
        List<GunStoreItem> categoryItems =
            GetCurrentCategoryItems();

        if (categoryItems.Count <= 0)
            return null;

        ClampWeaponSelection();

        return categoryItems[
            selectedWeapon
        ];
    }

    //====================================================
    // CLAMP WEAPON SELECTION
    //====================================================

    private void ClampWeaponSelection()
    {
        List<GunStoreItem> categoryItems =
            GetCurrentCategoryItems();

        int count =
            categoryItems.Count;

        if (count <= 0)
        {
            selectedWeapon = 0;
            return;
        }

        if (selectedWeapon < 0)
        {
            selectedWeapon = 0;
        }

        if (selectedWeapon >= count)
        {
            selectedWeapon =
                count - 1;
        }
    }

    //====================================================
    // FORMAT CATEGORY
    //====================================================

    private string FormatCategoryName(
        string categoryName)
    {
        if (string.IsNullOrWhiteSpace(
            categoryName))
        {
            return "WEAPONS";
        }

        StringBuilder builder =
            new StringBuilder();

        for (int index = 0;
            index < categoryName.Length;
            index++)
        {
            char character =
                categoryName[index];

            if (index > 0 &&
                char.IsUpper(character) &&
                !char.IsUpper(
                    categoryName[index - 1]
                ))
            {
                builder.Append(' ');
            }

            builder.Append(character);
        }

        return builder
            .ToString()
            .ToUpperInvariant();
    }

    //====================================================
    // SHORTEN TEXT
    //====================================================

    private string ShortenText(
        string text,
        int maximumLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (maximumLength <= 3)
            return text;

        if (text.Length <=
            maximumLength)
        {
            return text;
        }

        return text.Substring(
            0,
            maximumLength - 3
        ) + "...";
    }

    //====================================================
    // RESET INPUT
    //====================================================

    private void ResetInput()
    {
        aPressedLastFrame =
            Game.IsKeyPressed(
                Keys.A
            );

        dPressedLastFrame =
            Game.IsKeyPressed(
                Keys.D
            );

        upPressedLastFrame =
            Game.IsKeyPressed(
                Keys.Up
            );

        downPressedLastFrame =
            Game.IsKeyPressed(
                Keys.Down
            );

        leftPressedLastFrame =
            Game.IsKeyPressed(
                Keys.Left
            );

        rightPressedLastFrame =
            Game.IsKeyPressed(
                Keys.Right
            );

        ePressedLastFrame =
            Game.IsKeyPressed(
                Keys.E
            );

        tabPressedLastFrame =
            Game.IsKeyPressed(
                Keys.Tab
            );

        escapePressedLastFrame =
            Game.IsKeyPressed(
                Keys.Escape
            );
    }

    //====================================================
    // DISABLE GAMEPLAY CONTROLS
    //====================================================

    private void DisableGameplayControls()
    {
        // Movement and camera.
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

        // Attack and aim.
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

        // Weapon wheel.
        Function.Call(
            Hash.DISABLE_CONTROL_ACTION,
            0,
            37,
            true
        );

        // Interaction controls.
        Function.Call(
            Hash.DISABLE_CONTROL_ACTION,
            0,
            38,
            true
        );

        Function.Call(
            Hash.DISABLE_CONTROL_ACTION,
            0,
            51,
            true
        );

        Function.Call(
            Hash.DISABLE_CONTROL_ACTION,
            0,
            52,
            true
        );

        // Phone controls.
        for (int control = 172;
            control <= 177;
            control++)
        {
            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                control,
                true
            );
        }

        // Phone open and close.
        Function.Call(
            Hash.DISABLE_CONTROL_ACTION,
            0,
            27,
            true
        );

        Function.Call(
            Hash.DISABLE_CONTROL_ACTION,
            0,
            177,
            true
        );

        // Pause menu.
        Function.Call(
            Hash.DISABLE_CONTROL_ACTION,
            0,
            199,
            true
        );

        Function.Call(
            Hash.DISABLE_CONTROL_ACTION,
            0,
            200,
            true
        );
    }

    //====================================================
    // WEAPON ICON
    //====================================================

    private bool DrawWeaponIcon(
        GunStoreItem item,
        float x,
        float y,
        float width,
        float height)
    {
        if (item == null ||
            string.IsNullOrWhiteSpace(
                item.Icon))
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
                width *
                GTA.UI.Screen.Width;

            float boxHeight =
                height *
                GTA.UI.Screen.Height;

            using (Image image =
                Image.FromFile(
                    iconPath))
            {
                float imageAspect =
                    image.Width /
                    (float)image.Height;

                float drawWidth =
                    boxWidth;

                float drawHeight =
                    drawWidth /
                    imageAspect;

                if (drawHeight > boxHeight)
                {
                    drawHeight =
                        boxHeight;

                    drawWidth =
                        drawHeight *
                        imageAspect;
                }

                float positionX =
                    x *
                    GTA.UI.Screen.Width +
                    (
                        boxWidth -
                        drawWidth
                    ) /
                    2f;

                float positionY =
                    y *
                    GTA.UI.Screen.Height +
                    (
                        boxHeight -
                        drawHeight
                    ) /
                    2f;

                CustomSprite sprite;

                if (!iconSprites.TryGetValue(
                    iconPath,
                    out sprite))
                {
                    sprite =
                        new CustomSprite(
                            iconPath,
                            new SizeF(
                                drawWidth,
                                drawHeight
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
                        drawWidth,
                        drawHeight
                    );

                sprite.Color =
                    Color.White;

                sprite.Draw();

                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    //====================================================
    // ICONS FOLDER
    //====================================================

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
                StringComparison.OrdinalIgnoreCase
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

    //====================================================
    // RECTANGLE
    //====================================================

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
                            Graphics.FromImage(
                                bitmap))
                        {
                            graphics.Clear(
                                Color.White
                            );
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
            ? (int)color.A
            : Math.Min(
            (int)color.A,
            190
            );

        rectangleSprite.Position =
            new PointF(
                x *
                GTA.UI.Screen.Width,
                y *
                GTA.UI.Screen.Height
            );

        rectangleSprite.Size =
            new SizeF(
                width *
                GTA.UI.Screen.Width,
                height *
                GTA.UI.Screen.Height
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

    //====================================================
    // BORDER
    //====================================================

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

    //====================================================
    // TEXT HELPERS
    //====================================================

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

    //====================================================
    // TEXT SPRITE
    //====================================================

    private void DrawTextSprite(
        string text,
        float x,
        float y,
        float scale,
        Color color,
        Alignment alignment)
    {
        if (string.IsNullOrEmpty(text))
            return;

        string textureKey =
            "gun-store-v1|" +
            text +
            "|" +
            scale.ToString(
                "R",
                CultureInfo.InvariantCulture
            ) +
            "|" +
            color.ToArgb();

        string spriteKey =
            textureKey +
            "|" +
            x.ToString(
                "R",
                CultureInfo.InvariantCulture
            ) +
            "|" +
            y.ToString(
                "R",
                CultureInfo.InvariantCulture
            ) +
            "|" +
            (int)alignment +
            "|" +
            GTA.UI.Screen.Width +
            "|" +
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
            x *
            GTA.UI.Screen.Width;

        float screenY =
            y *
            GTA.UI.Screen.Height;

        if (alignment ==
            Alignment.Center)
        {
            screenX -=
                textureSize.Width / 2f;
        }
        else if (alignment ==
            Alignment.Right)
        {
            screenX -=
                textureSize.Width;
        }

        sprite.Position =
            new PointF(
                screenX,
                screenY
            );

        sprite.Size =
            textureSize;

        sprite.Color =
            Color.White;

        sprite.Draw();
    }

    //====================================================
    // CREATE TEXT TEXTURE
    //====================================================

    private SizeF CreateTextTexture(
        string texturePath,
        string text,
        float scale,
        Color color)
    {
        if (File.Exists(
            texturePath))
        {
            using (Image existingImage =
                Image.FromFile(
                    texturePath))
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
                        measurementBitmap))
                {
                    measuredSize =
                        measurementGraphics
                            .MeasureString(
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
                        .PixelFormat
                        .Format32bppArgb
                ))
            {
                using (Graphics graphics =
                    Graphics.FromImage(
                        bitmap))
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
                            )))
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
                        new SolidBrush(
                            color))
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

    //====================================================
    // STABLE HASH
    //====================================================

    private string GetStableHash(
        string value)
    {
        using (SHA1 sha1 =
            SHA1.Create())
        {
            byte[] bytes =
                Encoding.UTF8.GetBytes(
                    value
                );

            byte[] hash =
                sha1.ComputeHash(
                    bytes
                );

            return BitConverter
                .ToString(hash)
                .Replace(
                    "-",
                    string.Empty
                );
        }
    }
}