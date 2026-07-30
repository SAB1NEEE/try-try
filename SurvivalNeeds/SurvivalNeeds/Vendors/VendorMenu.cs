using GTA;
using GTA.Native;
using GTA.UI;
using SurvivalNeeds.Inventory;
using SurvivalNeeds.Systems;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SurvivalNeeds.Vendors
{
    public class VendorMenu
    {
        private class CartEntry
        {
            public VendorItem VendorItem;
            public int Quantity;

            public int TotalPrice
            {
                get
                {
                    if (VendorItem == null)
                        return 0;

                    return VendorItem.Price *
                           Quantity;
                }
            }
        }

        private readonly InventoryManager inventory;
        private readonly MoneySystem money;

        private readonly List<CartEntry> cart =
            new List<CartEntry>();

        private readonly Dictionary<string, CustomSprite>
            iconSprites =
                new Dictionary<string, CustomSprite>();

        private readonly string iconsFolder;

        private Vendor currentVendor;

        private bool visible;

        // false = store products
        // true = shopping cart
        private bool cartSelected;

        private int productSelectedIndex;
        private int cartSelectedIndex;

        private bool tabPressedLastFrame;
        private bool wPressedLastFrame;
        private bool sPressedLastFrame;
        private bool aPressedLastFrame;
        private bool dPressedLastFrame;
        private bool enterPressedLastFrame;
        private bool backPressedLastFrame;
        private bool ePressedLastFrame;
        private bool escapePressedLastFrame;

        private const int ProductColumns = 4;
        private const int ProductRows = 3;

        private const int ProductsPerPage =
            ProductColumns *
            ProductRows;

        private const int CartRows = 8;

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

        private readonly Color greenColor =
            Color.FromArgb(
                255,
                70,
                220,
                125
            );

        private readonly Color redColor =
            Color.FromArgb(
                255,
                235,
                70,
                80
            );

        private readonly Color mutedTextColor =
            Color.FromArgb(
                255,
                190,
                192,
                205
            );

        public VendorMenu(
            InventoryManager inventory,
            MoneySystem money)
        {
            this.inventory = inventory;
            this.money = money;

            iconsFolder =
                GetIconsFolder();
        }

        public bool Visible
        {
            get
            {
                return visible;
            }
        }

        public Vendor CurrentVendor
        {
            get
            {
                return currentVendor;
            }
        }

        //====================================================
        // OPEN
        //====================================================

        public void Open(
            Vendor vendor)
        {
            if (vendor == null)
                return;

            currentVendor = vendor;

            cart.Clear();

            cartSelected = false;

            productSelectedIndex = 0;
            cartSelectedIndex = 0;

            visible = true;

            ResetInput();
        }

        //====================================================
        // CLOSE
        //====================================================

        public void Close()
        {
            visible = false;

            cart.Clear();

            currentVendor = null;

            productSelectedIndex = 0;
            cartSelectedIndex = 0;

            ResetInput();
        }

        //====================================================
        // DRAW
        //====================================================

        public void Draw()
        {
            if (!visible ||
                currentVendor == null)
            {
                return;
            }

            ClampSelections();

            HandleInput();

            if (!visible ||
                currentVendor == null)
            {
                return;
            }

            ClampSelections();

            DisableGameplayControls();

            DrawRectangle(
                0f,
                0f,
                1f,
                1f,
                overlayColor
            );

            DrawStorePanel();
            DrawCartPanel();
            DrawDetailsPanel();
            DrawFooter();
        }

        //====================================================
        // STORE PANEL
        //====================================================

        private void DrawStorePanel()
        {
            const float panelX = 0.025f;
            const float panelY = 0.075f;
            const float panelWidth = 0.59f;
            const float panelHeight = 0.68f;

            if (!cartSelected)
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
                0.070f,
                headerColor
            );

            DrawText(
                currentVendor.Name,
                panelX + 0.015f,
                panelY + 0.014f,
                0.49f,
                !cartSelected
                    ? cyanColor
                    : Color.White
            );

            DrawRightText(
                GetVendorTypeName(
                    currentVendor.Type
                ),
                panelX + panelWidth - 0.015f,
                panelY + 0.018f,
                0.31f,
                mutedTextColor
            );

            DrawProductGrid(
                panelX + 0.015f,
                panelY + 0.090f,
                panelWidth - 0.030f,
                0.490f
            );

            int productCount =
                currentVendor.Items == null
                    ? 0
                    : currentVendor.Items.Count;

            int page =
                productCount <= 0
                    ? 1
                    : productSelectedIndex /
                      ProductsPerPage + 1;

            int pageCount =
                productCount <= 0
                    ? 1
                    : (productCount +
                       ProductsPerPage - 1) /
                      ProductsPerPage;

            DrawRectangle(
                panelX,
                panelY + panelHeight - 0.075f,
                panelWidth,
                0.075f,
                headerColor
            );

            DrawText(
                "ENTER - ADD ONE",
                panelX + 0.015f,
                panelY + panelHeight - 0.050f,
                0.31f,
                mutedTextColor
            );

            DrawRightText(
                "PAGE " +
                page +
                " / " +
                pageCount,
                panelX + panelWidth - 0.015f,
                panelY + panelHeight - 0.050f,
                0.31f,
                mutedTextColor
            );
        }

        //====================================================
        // PRODUCT GRID
        //====================================================

        private void DrawProductGrid(
            float gridX,
            float gridY,
            float gridWidth,
            float gridHeight)
        {
            const float horizontalGap =
                0.006f;

            const float verticalGap =
                0.008f;

            float slotWidth =
                (gridWidth -
                 horizontalGap *
                 (ProductColumns - 1)) /
                ProductColumns;

            float slotHeight =
                (gridHeight -
                 verticalGap *
                 (ProductRows - 1)) /
                ProductRows;

            int page =
                productSelectedIndex /
                ProductsPerPage;

            int firstItem =
                page *
                ProductsPerPage;

            for (int row = 0;
                row < ProductRows;
                row++)
            {
                for (int column = 0;
                    column < ProductColumns;
                    column++)
                {
                    int pagePosition =
                        row *
                        ProductColumns +
                        column;

                    int productIndex =
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

                    DrawProductSlot(
                        x,
                        y,
                        slotWidth,
                        slotHeight,
                        productIndex
                    );
                }
            }
        }

        //====================================================
        // PRODUCT SLOT
        //====================================================

        private void DrawProductSlot(
            float x,
            float y,
            float width,
            float height,
            int productIndex)
        {
            bool hasProduct =
                currentVendor.Items != null &&
                productIndex >= 0 &&
                productIndex <
                currentVendor.Items.Count;

            bool selected =
                !cartSelected &&
                hasProduct &&
                productIndex ==
                productSelectedIndex;

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

            if (!hasProduct)
                return;

            VendorItem vendorItem =
                currentVendor.Items[
                    productIndex
                ];

            if (vendorItem == null ||
                !vendorItem.IsValid ||
                vendorItem.Item == null)
            {
                DrawCenteredText(
                    "INVALID ITEM",
                    x + width / 2f,
                    y + height / 2f,
                    0.29f,
                    redColor
                );

                return;
            }

            InventoryItem item =
                vendorItem.Item;

            DrawText(
                ShortenText(
                    item.Name,
                    15
                ),
                x + 0.006f,
                y + 0.006f,
                0.29f,
                Color.White
            );

            bool iconDrawn =
                DrawItemIcon(
                    item,
                    x + 0.014f,
                    y + 0.036f,
                    width - 0.028f,
                    height - 0.078f
                );

            if (!iconDrawn)
            {
                DrawCenteredText(
                    GetCategoryLabel(item),
                    x + width / 2f,
                    y + height * 0.46f,
                    0.39f,
                    GetCategoryColor(item)
                );
            }

            DrawText(
                "$" +
                vendorItem.Price,
                x + 0.007f,
                y + height - 0.031f,
                0.32f,
                greenColor
            );

            int cartQuantity =
                GetCartQuantity(
                    vendorItem.ItemId
                );

            if (cartQuantity > 0)
            {
                DrawRightText(
                    cartQuantity +
                    " IN CART",
                    x + width - 0.007f,
                    y + height - 0.030f,
                    0.25f,
                    cyanColor
                );
            }
        }

        //====================================================
        // CART PANEL
        //====================================================

        private void DrawCartPanel()
        {
            const float panelX = 0.635f;
            const float panelY = 0.075f;
            const float panelWidth = 0.34f;
            const float panelHeight = 0.68f;

            if (cartSelected)
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
                0.070f,
                headerColor
            );

            DrawText(
                "SHOPPING CART",
                panelX + 0.015f,
                panelY + 0.014f,
                0.45f,
                cartSelected
                    ? cyanColor
                    : Color.White
            );

            DrawRightText(
                GetTotalCartQuantity() +
                " ITEMS",
                panelX + panelWidth - 0.015f,
                panelY + 0.019f,
                0.29f,
                mutedTextColor
            );

            DrawCartRows(
                panelX + 0.015f,
                panelY + 0.090f,
                panelWidth - 0.030f,
                0.415f
            );

            DrawCartSummary(
                panelX + 0.015f,
                panelY + 0.525f,
                panelWidth - 0.030f
            );
        }

        //====================================================
        // CART ROWS
        //====================================================

        private void DrawCartRows(
            float x,
            float y,
            float width,
            float height)
        {
            float gap = 0.005f;

            float rowHeight =
                (height -
                 gap *
                 (CartRows - 1)) /
                CartRows;

            int firstEntry =
                GetCartFirstVisibleIndex();

            for (int row = 0;
                row < CartRows;
                row++)
            {
                int cartIndex =
                    firstEntry +
                    row;

                float rowY =
                    y +
                    row *
                    (rowHeight + gap);

                DrawCartRow(
                    x,
                    rowY,
                    width,
                    rowHeight,
                    cartIndex
                );
            }
        }

        //====================================================
        // CART ROW
        //====================================================

        private void DrawCartRow(
            float x,
            float y,
            float width,
            float height,
            int cartIndex)
        {
            bool hasEntry =
                cartIndex >= 0 &&
                cartIndex < cart.Count;

            bool selected =
                cartSelected &&
                hasEntry &&
                cartIndex ==
                cartSelectedIndex;

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

            if (!hasEntry)
                return;

            CartEntry entry =
                cart[cartIndex];

            if (entry == null ||
                entry.VendorItem == null ||
                entry.VendorItem.Item == null)
            {
                return;
            }

            InventoryItem item =
                entry.VendorItem.Item;

            DrawText(
                ShortenText(
                    item.Name,
                    18
                ),
                x + 0.008f,
                y + 0.007f,
                0.28f,
                Color.White
            );

            DrawText(
                entry.Quantity +
                " x $" +
                entry.VendorItem.Price,
                x + 0.008f,
                y + height - 0.026f,
                0.25f,
                mutedTextColor
            );

            DrawRightText(
                "$" +
                entry.TotalPrice,
                x + width - 0.008f,
                y + height - 0.027f,
                0.29f,
                greenColor
            );
        }

        //====================================================
        // CART SUMMARY
        //====================================================

        private void DrawCartSummary(
            float x,
            float y,
            float width)
        {
            DrawRectangle(
                x,
                y,
                width,
                0.045f,
                headerColor
            );

            DrawText(
                "CASH",
                x + 0.010f,
                y + 0.011f,
                0.29f,
                mutedTextColor
            );

            DrawRightText(
                "$" +
                money.Cash,
                x + width - 0.010f,
                y + 0.010f,
                0.32f,
                Color.White
            );

            DrawRectangle(
                x,
                y + 0.052f,
                width,
                0.052f,
                headerColor
            );

            DrawText(
                "TOTAL",
                x + 0.010f,
                y + 0.067f,
                0.35f,
                Color.White
            );

            DrawRightText(
                "$" +
                GetCartTotal(),
                x + width - 0.010f,
                y + 0.064f,
                0.41f,
                CanAffordCart()
                    ? greenColor
                    : redColor
            );

            DrawActionButton(
                x,
                y + 0.116f,
                width,
                0.052f,
                cart.Count == 0
                    ? "CART IS EMPTY"
                    : CanAffordCart()
                        ? "E - PURCHASE"
                        : "NOT ENOUGH CASH",
                cart.Count > 0 &&
                CanAffordCart()
            );
        }

        //====================================================
        // DETAILS PANEL
        //====================================================

        private void DrawDetailsPanel()
        {
            const float panelX = 0.025f;
            const float panelY = 0.775f;
            const float panelWidth = 0.95f;
            const float panelHeight = 0.115f;

            DrawRectangle(
                panelX,
                panelY,
                panelWidth,
                panelHeight,
                panelColor
            );

            InventoryItem selectedItem =
                GetSelectedInventoryItem();

            if (selectedItem == null)
            {
                DrawCenteredText(
                    cartSelected
                        ? "SHOPPING CART EMPTY"
                        : "NO PRODUCTS AVAILABLE",
                    panelX + panelWidth / 2f,
                    panelY + 0.036f,
                    0.39f,
                    mutedTextColor
                );

                return;
            }

            float iconWidth = 0.065f;

            bool iconDrawn =
                DrawItemIcon(
                    selectedItem,
                    panelX + 0.012f,
                    panelY + 0.012f,
                    iconWidth,
                    panelHeight - 0.024f
                );

            if (!iconDrawn)
            {
                DrawCenteredText(
                    GetCategoryLabel(
                        selectedItem
                    ),
                    panelX + 0.044f,
                    panelY + 0.042f,
                    0.35f,
                    GetCategoryColor(
                        selectedItem
                    )
                );
            }

            DrawText(
                selectedItem.Name,
                panelX + 0.090f,
                panelY + 0.014f,
                0.38f,
                Color.White
            );

            DrawText(
                ShortenText(
                    selectedItem.Description,
                    75
                ),
                panelX + 0.090f,
                panelY + 0.046f,
                0.27f,
                mutedTextColor
            );

            DrawText(
                "WEIGHT: " +
                selectedItem.Weight
                    .ToString("0.00") +
                " KG",
                panelX + 0.090f,
                panelY + 0.075f,
                0.25f,
                mutedTextColor
            );

            DrawText(
                "CATEGORY: " +
                selectedItem.Category,
                panelX + 0.245f,
                panelY + 0.075f,
                0.25f,
                mutedTextColor
            );

            string effects =
                GetItemEffects(
                    selectedItem
                );

            DrawRightText(
                effects,
                panelX + panelWidth - 0.015f,
                panelY + 0.075f,
                0.25f,
                cyanColor
            );
        }

        //====================================================
        // FOOTER
        //====================================================

        private void DrawFooter()
        {
            DrawRectangle(
                0.025f,
                0.905f,
                0.95f,
                0.060f,
                headerColor
            );

            DrawText(
                "TAB - SWITCH PANEL",
                0.043f,
                0.923f,
                0.29f,
                mutedTextColor
            );

            DrawCenteredText(
                "W/A/S/D - SELECT",
                0.33f,
                0.923f,
                0.29f,
                mutedTextColor
            );

            DrawCenteredText(
                cartSelected
                    ? "ENTER - ADD  |  BACKSPACE - REMOVE"
                    : "ENTER - ADD TO CART",
                0.61f,
                0.923f,
                0.29f,
                mutedTextColor
            );

            DrawRightText(
                "E - BUY  |  ESC - CLOSE",
                0.957f,
                0.923f,
                0.29f,
                mutedTextColor
            );
        }

        //====================================================
        // INPUT
        //====================================================

        private void HandleInput()
        {
            bool tabPressed =
                Game.IsKeyPressed(
                    Keys.Tab
                );

            bool wPressed =
                Game.IsKeyPressed(
                    Keys.W
                );

            bool sPressed =
                Game.IsKeyPressed(
                    Keys.S
                );

            bool aPressed =
                Game.IsKeyPressed(
                    Keys.A
                );

            bool dPressed =
                Game.IsKeyPressed(
                    Keys.D
                );

            bool enterPressed =
                Game.IsKeyPressed(
                    Keys.Enter
                );

            bool backPressed =
                Game.IsKeyPressed(
                    Keys.Back
                );

            bool ePressed =
                Game.IsKeyPressed(
                    Keys.E
                );

            bool escapePressed =
                Game.IsKeyPressed(
                    Keys.Escape
                );

            if (tabPressed &&
                !tabPressedLastFrame)
            {
                cartSelected =
                    !cartSelected;

                ClampSelections();
            }

            if (cartSelected)
            {
                if (wPressed &&
                    !wPressedLastFrame)
                {
                    MoveCartSelection(-1);
                }

                if (sPressed &&
                    !sPressedLastFrame)
                {
                    MoveCartSelection(1);
                }

                if (aPressed &&
                    !aPressedLastFrame)
                {
                    RemoveSelectedCartItem();
                }

                if (dPressed &&
                    !dPressedLastFrame)
                {
                    AddSelectedCartItem();
                }

                if (enterPressed &&
                    !enterPressedLastFrame)
                {
                    AddSelectedCartItem();
                }

                if (backPressed &&
                    !backPressedLastFrame)
                {
                    RemoveSelectedCartItem();
                }
            }
            else
            {
                if (wPressed &&
                    !wPressedLastFrame)
                {
                    MoveProductSelection(
                        -ProductColumns
                    );
                }

                if (sPressed &&
                    !sPressedLastFrame)
                {
                    MoveProductSelection(
                        ProductColumns
                    );
                }

                if (aPressed &&
                    !aPressedLastFrame)
                {
                    MoveProductSelection(-1);
                }

                if (dPressed &&
                    !dPressedLastFrame)
                {
                    MoveProductSelection(1);
                }

                if (enterPressed &&
                    !enterPressedLastFrame)
                {
                    AddSelectedProduct();
                }
            }

            if (ePressed &&
                !ePressedLastFrame)
            {
                PurchaseCart();
            }

            if (escapePressed &&
                !escapePressedLastFrame)
            {
                Close();
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

            backPressedLastFrame =
                backPressed;

            ePressedLastFrame =
                ePressed;

            escapePressedLastFrame =
                escapePressed;
        }

        //====================================================
        // PRODUCT NAVIGATION
        //====================================================

        private void MoveProductSelection(
            int amount)
        {
            int count =
                currentVendor.Items == null
                    ? 0
                    : currentVendor.Items.Count;

            if (count <= 0)
                return;

            productSelectedIndex +=
                amount;

            while (productSelectedIndex < 0)
            {
                productSelectedIndex +=
                    count;
            }

            while (productSelectedIndex >=
                count)
            {
                productSelectedIndex -=
                    count;
            }
        }

        //====================================================
        // CART NAVIGATION
        //====================================================

        private void MoveCartSelection(
            int amount)
        {
            if (cart.Count <= 0)
                return;

            cartSelectedIndex +=
                amount;

            while (cartSelectedIndex < 0)
            {
                cartSelectedIndex +=
                    cart.Count;
            }

            while (cartSelectedIndex >=
                cart.Count)
            {
                cartSelectedIndex -=
                    cart.Count;
            }
        }

        //====================================================
        // ADD SELECTED PRODUCT
        //====================================================

        private void AddSelectedProduct()
        {
            if (currentVendor.Items == null ||
                currentVendor.Items.Count == 0)
            {
                return;
            }

            if (productSelectedIndex < 0 ||
                productSelectedIndex >=
                currentVendor.Items.Count)
            {
                return;
            }

            VendorItem vendorItem =
                currentVendor.Items[
                    productSelectedIndex
                ];

            AddToCart(
                vendorItem
            );
        }

        //====================================================
        // ADD SELECTED CART ITEM
        //====================================================

        private void AddSelectedCartItem()
        {
            if (cart.Count == 0)
                return;

            ClampCartSelection();

            CartEntry entry =
                cart[
                    cartSelectedIndex
                ];

            if (entry == null ||
                entry.VendorItem == null)
            {
                return;
            }

            entry.Quantity++;

            Notification.Show(
                "~g~Added another " +
                entry.VendorItem.Item.Name
            );
        }

        //====================================================
        // ADD TO CART
        //====================================================

        private void AddToCart(
            VendorItem vendorItem)
        {
            if (vendorItem == null ||
                !vendorItem.IsValid ||
                vendorItem.Item == null)
            {
                Notification.Show(
                    "~r~This product is unavailable"
                );

                return;
            }

            CartEntry existingEntry =
                FindCartEntry(
                    vendorItem.ItemId
                );

            if (existingEntry != null)
            {
                existingEntry.Quantity++;
            }
            else
            {
                cart.Add(
                    new CartEntry()
                    {
                        VendorItem =
                            vendorItem,

                        Quantity = 1
                    }
                );

                cartSelectedIndex =
                    cart.Count - 1;
            }

            Notification.Show(
                "~g~Added " +
                vendorItem.Item.Name +
                " to cart"
            );
        }

        //====================================================
        // REMOVE SELECTED CART ITEM
        //====================================================

        private void RemoveSelectedCartItem()
        {
            if (cart.Count == 0)
                return;

            ClampCartSelection();

            CartEntry entry =
                cart[
                    cartSelectedIndex
                ];

            if (entry == null)
                return;

            entry.Quantity--;

            if (entry.Quantity <= 0)
            {
                cart.RemoveAt(
                    cartSelectedIndex
                );
            }

            ClampCartSelection();
        }

        //====================================================
        // PURCHASE
        //====================================================

        private void PurchaseCart()
        {
            if (cart.Count == 0)
            {
                Notification.Show(
                    "~r~Your cart is empty"
                );

                return;
            }

            int total =
                GetCartTotal();

            if (!money.CanAfford(total))
            {
                Notification.Show(
                    "~r~You do not have enough cash"
                );

                return;
            }

            if (!CanInventoryFitCart())
            {
                Notification.Show(
                    "~r~Not enough inventory space or carrying capacity"
                );

                return;
            }

            if (!money.SpendMoney(total))
            {
                Notification.Show(
                    "~r~Purchase failed"
                );

                return;
            }

            foreach (CartEntry entry
                in cart)
            {
                if (entry == null ||
                    entry.VendorItem == null ||
                    entry.VendorItem.Item == null ||
                    entry.Quantity <= 0)
                {
                    continue;
                }

                inventory.AddItem(
                    entry.VendorItem.ItemId,
                    entry.Quantity
                );
            }

            int purchasedItems =
                GetTotalCartQuantity();

            cart.Clear();

            cartSelectedIndex = 0;

            Notification.Show(
                "~g~Purchased " +
                purchasedItems +
                " item" +
                (purchasedItems == 1
                    ? ""
                    : "s") +
                " for $" +
                total
            );
        }

        //====================================================
        // CHECK INVENTORY CAPACITY
        //====================================================

        private bool CanInventoryFitCart()
        {
            float cartWeight = 0f;

            foreach (CartEntry entry in cart)
            {
                if (entry == null ||
                    entry.VendorItem == null ||
                    entry.VendorItem.Item == null ||
                    entry.Quantity <= 0)
                {
                    continue;
                }

                cartWeight +=
                    entry.VendorItem.Item.Weight *
                    entry.Quantity;
            }

            if (!inventory.CanCarryWeight(
                cartWeight))
            {
                return false;
            }

            Dictionary<string, int>
                quantitiesToAdd =
                    new Dictionary<string, int>();

            foreach (CartEntry entry
                in cart)
            {
                if (entry == null ||
                    entry.VendorItem == null ||
                    entry.VendorItem.Item == null ||
                    entry.Quantity <= 0)
                {
                    continue;
                }

                string itemId =
                    entry.VendorItem.ItemId;

                if (quantitiesToAdd.ContainsKey(
                    itemId))
                {
                    quantitiesToAdd[itemId] +=
                        entry.Quantity;
                }
                else
                {
                    quantitiesToAdd.Add(
                        itemId,
                        entry.Quantity
                    );
                }
            }

            int emptySlots = 0;

            foreach (InventorySlot slot
                in inventory.Slots)
            {
                if (slot == null ||
                    slot.IsEmpty ||
                    slot.Item == null)
                {
                    emptySlots++;
                }
            }

            foreach (KeyValuePair<string, int>
                pair in quantitiesToAdd)
            {
                InventoryItem item =
                    ItemDatabase.GetItem(
                        pair.Key
                    );

                if (item == null)
                    return false;

                int remaining =
                    pair.Value;

                foreach (InventorySlot slot
                    in inventory.Slots)
                {
                    if (slot == null ||
                        slot.IsEmpty ||
                        slot.Item == null)
                    {
                        continue;
                    }

                    if (slot.Item.Id !=
                        item.Id)
                    {
                        continue;
                    }

                    int availableSpace =
                        item.MaxStack -
                        slot.Quantity;

                    if (availableSpace <= 0)
                        continue;

                    remaining -=
                        availableSpace;

                    if (remaining <= 0)
                        break;
                }

                if (remaining <= 0)
                    continue;

                int neededSlots =
                    (remaining +
                     item.MaxStack - 1) /
                    item.MaxStack;

                if (neededSlots >
                    emptySlots)
                {
                    return false;
                }

                emptySlots -=
                    neededSlots;
            }

            return true;
        }

        //====================================================
        // CART HELPERS
        //====================================================

        private CartEntry FindCartEntry(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(
                itemId))
            {
                return null;
            }

            foreach (CartEntry entry
                in cart)
            {
                if (entry == null ||
                    entry.VendorItem == null)
                {
                    continue;
                }

                if (entry.VendorItem.ItemId ==
                    itemId)
                {
                    return entry;
                }
            }

            return null;
        }

        private int GetCartQuantity(
            string itemId)
        {
            CartEntry entry =
                FindCartEntry(
                    itemId
                );

            if (entry == null)
                return 0;

            return entry.Quantity;
        }

        private int GetCartTotal()
        {
            int total = 0;

            foreach (CartEntry entry
                in cart)
            {
                if (entry == null)
                    continue;

                total +=
                    entry.TotalPrice;
            }

            return total;
        }

        private int GetTotalCartQuantity()
        {
            int total = 0;

            foreach (CartEntry entry
                in cart)
            {
                if (entry == null)
                    continue;

                total +=
                    entry.Quantity;
            }

            return total;
        }

        private bool CanAffordCart()
        {
            int total =
                GetCartTotal();

            return total > 0 &&
                   money.CanAfford(total);
        }

        private int GetCartFirstVisibleIndex()
        {
            if (cartSelectedIndex <
                CartRows)
            {
                return 0;
            }

            return cartSelectedIndex -
                   CartRows +
                   1;
        }

        //====================================================
        // SELECTION HELPERS
        //====================================================

        private void ClampSelections()
        {
            ClampProductSelection();
            ClampCartSelection();
        }

        private void ClampProductSelection()
        {
            int count =
                currentVendor == null ||
                currentVendor.Items == null
                    ? 0
                    : currentVendor.Items.Count;

            if (count <= 0)
            {
                productSelectedIndex = 0;
                return;
            }

            if (productSelectedIndex >=
                count)
            {
                productSelectedIndex =
                    count - 1;
            }

            if (productSelectedIndex < 0)
            {
                productSelectedIndex = 0;
            }
        }

        private void ClampCartSelection()
        {
            if (cart.Count <= 0)
            {
                cartSelectedIndex = 0;
                return;
            }

            if (cartSelectedIndex >=
                cart.Count)
            {
                cartSelectedIndex =
                    cart.Count - 1;
            }

            if (cartSelectedIndex < 0)
            {
                cartSelectedIndex = 0;
            }
        }

        private InventoryItem
            GetSelectedInventoryItem()
        {
            if (cartSelected)
            {
                if (cart.Count == 0)
                    return null;

                ClampCartSelection();

                CartEntry entry =
                    cart[
                        cartSelectedIndex
                    ];

                if (entry == null ||
                    entry.VendorItem == null)
                {
                    return null;
                }

                return entry.VendorItem.Item;
            }

            if (currentVendor == null ||
                currentVendor.Items == null ||
                currentVendor.Items.Count == 0)
            {
                return null;
            }

            ClampProductSelection();

            VendorItem vendorItem =
                currentVendor.Items[
                    productSelectedIndex
                ];

            if (vendorItem == null)
                return null;

            return vendorItem.Item;
        }

        //====================================================
        // ITEM EFFECT TEXT
        //====================================================

        private string GetItemEffects(
            InventoryItem item)
        {
            if (item == null)
                return string.Empty;

            List<string> effects =
                new List<string>();

            if (item.HungerRestore != 0f)
            {
                effects.Add(
                    "HUNGER " +
                    FormatEffect(
                        item.HungerRestore
                    )
                );
            }

            if (item.ThirstRestore != 0f)
            {
                effects.Add(
                    "THIRST " +
                    FormatEffect(
                        item.ThirstRestore
                    )
                );
            }

            if (item.StressRestore != 0f)
            {
                effects.Add(
                    "STRESS -" +
                    Math.Abs(
                        item.StressRestore
                    ).ToString("0")
                );
            }

            if (item.HealthRestore != 0f)
            {
                effects.Add(
                    "HEALTH " +
                    FormatEffect(
                        item.HealthRestore
                    )
                );
            }

            if (effects.Count == 0)
            {
                return "NO USABLE EFFECT";
            }

            return string.Join(
                "   ",
                effects.ToArray()
            );
        }

        private string FormatEffect(
            float value)
        {
            if (value > 0f)
            {
                return "+" +
                    value.ToString("0");
            }

            return value.ToString("0");
        }

        //====================================================
        // VENDOR TYPE NAME
        //====================================================

        private string GetVendorTypeName(
            VendorType vendorType)
        {
            switch (vendorType)
            {
                case VendorType.ConvenienceStore:
                    return "CONVENIENCE STORE";

                case VendorType.LiquorStore:
                    return "LIQUOR STORE";

                case VendorType.Pharmacy:
                    return "PHARMACY";

                case VendorType.StreetVendor:
                    return "STREET VENDOR";

                default:
                    return "STORE";
            }
        }

        //====================================================
        // ACTION BUTTON
        //====================================================

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
                y + 0.014f,
                0.31f,
                enabled
                    ? Color.White
                    : mutedTextColor
            );
        }

        //====================================================
        // ITEM ICON
        //====================================================

        private bool DrawItemIcon(
            InventoryItem item,
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

                float iconSize =
                    Math.Min(
                        boxWidth,
                        boxHeight
                    );

                float positionX =
                    x *
                    GTA.UI.Screen.Width +
                    (boxWidth - iconSize) /
                    2f;

                float positionY =
                    y *
                    GTA.UI.Screen.Height +
                    (boxHeight - iconSize) /
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

        //====================================================
        // CATEGORY LABEL
        //====================================================

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

        //====================================================
        // CATEGORY COLOR
        //====================================================

        private Color GetCategoryColor(
            InventoryItem item)
        {
            if (item == null)
                return mutedTextColor;

            switch (item.Category)
            {
                case ItemCategory.Food:
                    return Color.FromArgb(
                        255,
                        245,
                        175,
                        55
                    );

                case ItemCategory.Drink:
                    return Color.FromArgb(
                        255,
                        70,
                        175,
                        255
                    );

                case ItemCategory.Medical:
                    return Color.FromArgb(
                        255,
                        235,
                        70,
                        80
                    );

                case ItemCategory.Tool:
                    return Color.FromArgb(
                        255,
                        220,
                        150,
                        70
                    );

                case ItemCategory.StressReliever:
                    return Color.FromArgb(
                        255,
                        180,
                        95,
                        255
                    );

                default:
                    return Color.FromArgb(
                        255,
                        205,
                        205,
                        215
                    );
            }
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
            tabPressedLastFrame =
                Game.IsKeyPressed(
                    Keys.Tab
                );

            wPressedLastFrame =
                Game.IsKeyPressed(
                    Keys.W
                );

            sPressedLastFrame =
                Game.IsKeyPressed(
                    Keys.S
                );

            aPressedLastFrame =
                Game.IsKeyPressed(
                    Keys.A
                );

            dPressedLastFrame =
                Game.IsKeyPressed(
                    Keys.D
                );

            enterPressedLastFrame =
                Game.IsKeyPressed(
                    Keys.Enter
                );

            backPressedLastFrame =
                Game.IsKeyPressed(
                    Keys.Back
                );

            ePressedLastFrame =
                Game.IsKeyPressed(
                    Keys.E
                );

            escapePressedLastFrame =
                Game.IsKeyPressed(
                    Keys.Escape
                );
        }

        //====================================================
        // DISABLE CONTROLS
        //====================================================

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

            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                37,
                true
            );

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

            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                38,
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
        // TEXT
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