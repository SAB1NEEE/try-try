using GTA;
using GTA.Native;
using GTA.UI;
using SurvivalNeeds.Systems;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace SurvivalNeeds.UI
{
    public class HUD
    {
        private const float CenterX = 0.925f;
        private const float TopY = 0.052f;
        private const float MoneyY = 0.091f;
        private const float BottomY = 0.130f;
        private const float CircleSpacing = 0.027f;

        private const float OuterRadius = 15f;
        private const float InnerRadius = 13f;

        private readonly CustomSprite healthIcon;
        private readonly CustomSprite armorIcon;
        private readonly CustomSprite staminaIcon;
        private readonly CustomSprite stressIcon;
        private readonly CustomSprite breathIcon;
        private readonly CustomSprite foodIcon;
        private readonly CustomSprite waterIcon;
        private readonly CustomSprite moneyIcon;
        private readonly CustomSprite fuelIcon;

        private readonly VehicleFuelSystem
            vehicleFuelSystem;

        private readonly string shapeCacheFolder;
        private readonly string textCacheFolder;

        private readonly Dictionary<string, CustomSprite> shapeSprites =
            new Dictionary<string, CustomSprite>();
        private readonly Dictionary<string, CustomSprite> textSprites =
            new Dictionary<string, CustomSprite>();
        private readonly Dictionary<string, SizeF> textSpriteSizes =
            new Dictionary<string, SizeF>();
        private readonly Dictionary<int, CustomSprite> speedometerSprites =
            new Dictionary<int, CustomSprite>();

        public HUD(
            VehicleFuelSystem vehicleFuelSystem)
        {
            this.vehicleFuelSystem =
                vehicleFuelSystem;
            string iconFolder = GetIconFolder();

            shapeCacheFolder = Path.Combine(
                iconFolder,
                "_hud_shape_cache"
            );

            textCacheFolder = Path.Combine(
                iconFolder,
                "_hud_text_cache"
            );

            try
            {
                Directory.CreateDirectory(shapeCacheFolder);
                Directory.CreateDirectory(textCacheFolder);
            }
            catch
            {
            }

            healthIcon = LoadIcon(iconFolder, "health.png");
            armorIcon = LoadIcon(iconFolder, "armor.png");
            staminaIcon = LoadIcon(iconFolder, "stamina.png");
            stressIcon = LoadIcon(iconFolder, "stress.png");
            breathIcon = LoadIcon(iconFolder, "breath.png");
            foodIcon = LoadIcon(iconFolder, "food.png");
            waterIcon = LoadIcon(iconFolder, "thirst.png");
            moneyIcon = LoadIcon(iconFolder, "money.png");
            fuelIcon = LoadIcon(iconFolder, "fuel.png");
        }

        public void Draw(
            float hunger,
            float thirst,
            float stress,
            int cash)
        {
            Ped player = Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            float health = GetHealthPercent(player);
            float armor = Clamp(player.Armor);
            float stamina = GetStaminaPercent();

            hunger = Clamp(hunger);
            thirst = Clamp(thirst);
            stress = Clamp(stress);

            DrawStatusCircle(
                CenterX - CircleSpacing,
                TopY,
                "HP",
                health,
                Color.FromArgb(220, 55, 65),
                healthIcon
            );

            DrawStatusCircle(
                CenterX,
                TopY,
                "AR",
                armor,
                Color.FromArgb(50, 95, 185),
                armorIcon
            );

            DrawStatusCircle(
                CenterX + CircleSpacing,
                TopY,
                "ST",
                stamina,
                Color.FromArgb(225, 205, 45),
                staminaIcon
            );

            DrawMoneyPanel(cash);

            bool underwater =
                Function.Call<bool>(
                    Hash.IS_PED_SWIMMING_UNDER_WATER,
                    player.Handle
                );

            float breath =
                underwater ? GetBreathPercent() : 100f;

            DrawStatusCircle(
                CenterX - CircleSpacing * 1.5f,
                BottomY,
                "STR",
                stress,
                Color.FromArgb(105, 65, 190),
                stressIcon
            );

            DrawStatusCircle(
                CenterX - CircleSpacing * 0.5f,
                BottomY,
                "FOOD",
                hunger,
                Color.FromArgb(220, 120, 45),
                foodIcon
            );

            DrawStatusCircle(
                CenterX + CircleSpacing * 0.5f,
                BottomY,
                "H2O",
                thirst,
                Color.FromArgb(45, 150, 215),
                waterIcon
            );

            DrawStatusCircle(
                CenterX + CircleSpacing * 1.5f,
                BottomY,
                "AIR",
                breath,
                Color.FromArgb(125, 135, 150),
                breathIcon
            );

            DrawVehicleHud(player);
        }

        private void DrawStatusCircle(
            float x,
            float y,
            string label,
            float value,
            Color color,
            CustomSprite icon)
        {
            DrawCircle(
                x,
                y,
                OuterRadius,
                Color.FromArgb(205, 15, 15, 20)
            );

            Color displayColor = color;

            bool critical =
                label == "STR"
                    ? value >= 75f
                    : value <= 20f;

            if (critical)
            {
                displayColor =
                    Color.FromArgb(220, 55, 55);
            }

            DrawCircle(
                x,
                y,
                InnerRadius,
                Color.FromArgb(225, 28, 28, 34)
            );

            DrawCircleFill(
                x,
                y,
                InnerRadius,
                value,
                Color.FromArgb(
                    235,
                    displayColor.R,
                    displayColor.G,
                    displayColor.B
                )
            );

            if (icon != null)
            {
                DrawIcon(
                    icon,
                    x,
                    y,
                    15f
                );
            }
            else
            {
                DrawStaticText(
                    label,
                    x,
                    y - 0.008f,
                    0.16f,
                    Color.White
                );
            }
        }

        private void DrawMoneyPanel(int cash)
        {
            const float panelWidth = 0.108f;
            const float panelHeight = 0.028f;

            DrawRoundedRectangle(
                CenterX,
                MoneyY,
                panelWidth,
                panelHeight,
                Color.FromArgb(220, 100, 102, 112)
            );

            float iconX =
                CenterX - panelWidth / 2f + 0.015f;

            DrawCircle(
                iconX,
                MoneyY,
                12f,
                Color.FromArgb(240, 45, 195, 70)
            );

            if (moneyIcon != null)
            {
                DrawIcon(
                    moneyIcon,
                    iconX,
                    MoneyY,
                    13f
                );
            }
            else
            {
                DrawStaticText(
                    "$",
                    iconX,
                    MoneyY - 0.008f,
                    0.24f,
                    Color.White
                );
            }

            DrawDynamicText(
                cash.ToString(
                    "N0",
                    CultureInfo.InvariantCulture
                ),
                CenterX + 0.010f,
                MoneyY - 0.010f,
                0.29f,
                Color.White
            );
        }

        private void DrawVehicleHud(Ped player)
        {
            bool isInVehicle =
                Function.Call<bool>(
                    Hash.IS_PED_IN_ANY_VEHICLE,
                    player.Handle,
                    false
                );

            if (!isInVehicle)
            {
                return;
            }

            int vehicleHandle =
                Function.Call<int>(
                    Hash.GET_VEHICLE_PED_IS_IN,
                    player.Handle,
                    false
                );

            if (vehicleHandle == 0)
            {
                return;
            }

            Vehicle vehicle =
                Entity.FromHandle(vehicleHandle)
                as Vehicle;

            if (vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            const float speedometerX = 0.500f;
            const float speedometerY = 0.875f;

            float speedMph =
                Math.Abs(vehicle.Speed) *
                2.23693629f;

            int displayedSpeed =
                (int)Math.Round(speedMph);

            DrawSpeedometer(
                speedometerX,
                speedometerY,
                displayedSpeed
            );

            float fuel =
                GetVehicleFuel(vehicle);

            float fuelX =
                speedometerX - 0.065f;

            float fuelY =
                speedometerY + 0.072f;

            DrawStatusCircle(
                fuelX,
                fuelY,
                "GAS",
                fuel,
                Color.FromArgb(225, 135, 35),
                fuelIcon
            );
        }

        private void DrawSpeedometer(
    float centerX,
    float centerY,
    int speed)
        {
            if (speed < 0)
            {
                speed = 0;
            }

            if (speed > 999)
            {
                speed = 999;
            }

            CustomSprite sprite;

            if (!speedometerSprites.TryGetValue(
                speed,
                out sprite))
            {
                try
                {
                    Directory.CreateDirectory(
                        shapeCacheFolder
                    );

                    string texturePath =
                        Path.Combine(
                            shapeCacheFolder,
                            "speedometer_" +
                            speed +
                            ".png"
                        );

                    CreateSpeedometerTexture(
                        texturePath,
                        speed
                    );

                    sprite =
                        new CustomSprite(
                            texturePath,
                            new SizeF(94f, 94f),
                            new PointF(0f, 0f),
                            Color.White,
                            0f,
                            true
                        );

                    speedometerSprites[speed] =
                        sprite;
                }
                catch
                {
                    return;
                }
            }

            sprite.Position =
                new PointF(
                    centerX * 1280f,
                    centerY * 720f
                );

            sprite.Size =
                new SizeF(
                    94f,
                    94f
                );

            sprite.Color =
                Color.White;

            sprite.Draw();
        }

        private void CreateSpeedometerTexture(
    string texturePath,
    int speed)
        {
            if (File.Exists(texturePath))
            {
                return;
            }

            const int textureSize = 256;

            using (Bitmap bitmap =
                new Bitmap(
                    textureSize,
                    textureSize,
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
                            .SmoothingMode.AntiAlias;

                    graphics.CompositingQuality =
                        System.Drawing.Drawing2D
                            .CompositingQuality.HighQuality;

                    graphics.InterpolationMode =
                        System.Drawing.Drawing2D
                            .InterpolationMode.HighQualityBicubic;

                    graphics.PixelOffsetMode =
                        System.Drawing.Drawing2D
                            .PixelOffsetMode.HighQuality;

                    // Outer dark border.
                    using (SolidBrush outerBrush =
                        new SolidBrush(
                            Color.FromArgb(
                                225,
                                35,
                                35,
                                40
                            )
                        ))
                    {
                        graphics.FillEllipse(
                            outerBrush,
                            4f,
                            4f,
                            248f,
                            248f
                        );
                    }

                    // Red ring.
                    using (SolidBrush redBrush =
                        new SolidBrush(
                            Color.FromArgb(
                                245,
                                225,
                                25,
                                30
                            )
                        ))
                    {
                        graphics.FillEllipse(
                            redBrush,
                            13f,
                            13f,
                            230f,
                            230f
                        );
                    }

                    // Black center.
                    using (SolidBrush centerBrush =
                        new SolidBrush(
                            Color.FromArgb(
                                250,
                                12,
                                12,
                                16
                            )
                        ))
                    {
                        graphics.FillEllipse(
                            centerBrush,
                            29f,
                            29f,
                            198f,
                            198f
                        );
                    }

                    string speedText =
                        speed.ToString(
                            CultureInfo.InvariantCulture
                        );

                    using (System.Drawing.Font speedFont =
                        new System.Drawing.Font(
                            "Arial",
                            speed >= 100
                                ? 54f
                                : 64f,
                            FontStyle.Regular,
                            GraphicsUnit.Pixel
                        ))
                    using (System.Drawing.Font mphFont =
                        new System.Drawing.Font(
                            "Arial",
                            27f,
                            FontStyle.Bold,
                            GraphicsUnit.Pixel
                        ))
                    using (SolidBrush textBrush =
                        new SolidBrush(
                            Color.White
                        ))
                    using (StringFormat format =
                        new StringFormat())
                    {
                        format.Alignment =
                            StringAlignment.Center;

                        format.LineAlignment =
                            StringAlignment.Center;

                        graphics.DrawString(
                            speedText,
                            speedFont,
                            textBrush,
                            new RectangleF(
                                20f,
                                55f,
                                216f,
                                90f
                            ),
                            format
                        );

                        graphics.DrawString(
                            "MPH",
                            mphFont,
                            textBrush,
                            new RectangleF(
                                20f,
                                143f,
                                216f,
                                55f
                            ),
                            format
                        );
                    }
                }

                bitmap.Save(
                    texturePath,
                    System.Drawing.Imaging
                        .ImageFormat.Png
                );
            }
        }

        private float GetVehicleFuel(
            Vehicle vehicle)
        {
            if (vehicleFuelSystem == null ||
                vehicle == null ||
                !vehicle.Exists())
            {
                return 0f;
            }

            return Clamp(
                vehicleFuelSystem.GetFuel(
                    vehicle
                )
            );
        }

        private string GetIconFolder()
        {
            string baseFolder =
                AppDomain.CurrentDomain.BaseDirectory.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                );

            DirectoryInfo baseDirectory =
                new DirectoryInfo(baseFolder);

            string scriptsFolder;

            if (baseDirectory.Name.Equals(
                "scripts",
                StringComparison.OrdinalIgnoreCase))
            {
                scriptsFolder = baseDirectory.FullName;
            }
            else
            {
                scriptsFolder = Path.Combine(
                    baseDirectory.FullName,
                    "scripts"
                );
            }

            return Path.Combine(
                scriptsFolder,
                "SurvivalNeeds",
                "icons"
            );
        }

        private CustomSprite LoadIcon(
            string iconFolder,
            string filename)
        {
            try
            {
                string fullPath =
                    Path.Combine(iconFolder, filename);

                if (!File.Exists(fullPath))
                {
                    return null;
                }

                return new CustomSprite(
                    fullPath,
                    new SizeF(24f, 24f),
                    new PointF(0f, 0f),
                    Color.White,
                    0f,
                    true
                );
            }
            catch
            {
                return null;
            }
        }

        private void DrawIcon(
            CustomSprite icon,
            float x,
            float y,
            float size)
        {
            icon.Position =
                new PointF(
                    x * 1280f,
                    y * 720f
                );

            icon.Size =
                new SizeF(
                    size,
                    size
                );

            icon.Draw();
        }

        private float GetHealthPercent(Ped player)
        {
            if (player.MaxHealth <= 0)
            {
                return 0f;
            }

            return Clamp(
                player.Health /
                (float)player.MaxHealth *
                100f
            );
        }

        private float GetStaminaPercent()
        {
            float stamina =
                Function.Call<float>(
                    Hash.GET_PLAYER_SPRINT_STAMINA_REMAINING,
                    Game.Player.Handle
                );

            return Clamp(stamina);
        }

        private float GetBreathPercent()
        {
            const float maximumBreathSeconds = 10f;

            float remainingSeconds =
                Function.Call<float>(
                    Hash.GET_PLAYER_UNDERWATER_TIME_REMAINING,
                    Game.Player.Handle
                );

            return Clamp(
                remainingSeconds /
                maximumBreathSeconds *
                100f
            );
        }

        private void DrawRoundedRectangle(
            float centerX,
            float centerY,
            float width,
            float height,
            Color color)
        {
            float radiusPixels =
                height * Screen.Height / 2f;

            float radiusX =
                radiusPixels / Screen.Width;

            DrawRectangle(
                centerX,
                centerY,
                width - radiusX * 2f,
                height,
                color
            );

            DrawCircle(
                centerX - width / 2f + radiusX,
                centerY,
                radiusPixels,
                color
            );

            DrawCircle(
                centerX + width / 2f - radiusX,
                centerY,
                radiusPixels,
                color
            );
        }

        private void DrawPlainCircle(
            float centerX,
            float centerY,
            float radiusPixels,
            Color color)
        {
            float radiusX =
                radiusPixels / Screen.Width;

            float radiusY =
                radiusPixels / Screen.Height;

            // Smooth enough without exceeding GTA's HUD draw limit.
            const int slices = 64;

            float sliceHeight =
                radiusY * 2f / slices;

            for (int i = 0;
                i < slices;
                i++)
            {
                float t =
                    -1.0f +
                    ((i + 0.5f) / slices) *
                    2.0f;

                float inside =
                    1.0f - t * t;

                if (inside <= 0f)
                {
                    continue;
                }

                float halfWidth =
                    (float)Math.Sqrt(inside);

                float y =
                    centerY +
                    t * radiusY;

                float width =
                    radiusX *
                    2f *
                    halfWidth;

                DrawRectangle(
                    centerX,
                    y,
                    width,
                    sliceHeight + 0.0001f,
                    color
                );
            }
        }

        private void DrawCircle(
            float centerX,
            float centerY,
            float radiusPixels,
            Color color)
        {
            DrawCircleTexture(
                centerX,
                centerY,
                radiusPixels,
                100,
                color
            );
        }

        private void DrawCircleFill(
            float centerX,
            float centerY,
            float radiusPixels,
            float percentage,
            Color color)
        {
            int fillLevel =
                (int)Math.Round(
                    Clamp(percentage)
                );

            if (fillLevel <= 0)
            {
                return;
            }

            DrawCircleTexture(
                centerX,
                centerY,
                radiusPixels,
                fillLevel,
                color
            );
        }

        private void DrawCircleTexture(
            float centerX,
            float centerY,
            float radiusPixels,
            int fillLevel,
            Color color)
        {
            fillLevel =
                Math.Max(
                    0,
                    Math.Min(100, fillLevel)
                );

            string key =
                radiusPixels.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture
                ) +
                "_" +
                color.ToArgb() +
                "_" +
                fillLevel;

            CustomSprite sprite;

            if (!shapeSprites.TryGetValue(
                key,
                out sprite))
            {
                try
                {
                    Directory.CreateDirectory(
                        shapeCacheFolder
                    );

                    string texturePath =
                        Path.Combine(
                            shapeCacheFolder,
                            "circle_" + key + ".png"
                        );

                    CreateCircleTexture(
                        texturePath,
                        fillLevel,
                        color
                    );

                    sprite =
                        new CustomSprite(
                            texturePath,
                            new SizeF(
                                radiusPixels * 2f,
                                radiusPixels * 2f
                            ),
                            new PointF(0f, 0f),
                            Color.White,
                            0f,
                            true
                        );

                    shapeSprites[key] = sprite;
                }
                catch
                {
                    DrawRectangle(
                        centerX,
                        centerY,
                        radiusPixels * 2f / Screen.Width,
                        radiusPixels * 2f / Screen.Height,
                        color
                    );

                    return;
                }
            }

            sprite.Position =
                new PointF(
                    centerX * 1280f,
                    centerY * 720f
                );

            sprite.Size =
                new SizeF(
                    radiusPixels * 2f,
                    radiusPixels * 2f
                );

            sprite.Color = Color.White;
            sprite.Draw();
        }

        private void CreateCircleTexture(
            string texturePath,
            int fillLevel,
            Color color)
        {
            if (File.Exists(texturePath))
            {
                return;
            }

            const int textureSize = 128;

            using (Bitmap bitmap =
                new Bitmap(
                    textureSize,
                    textureSize,
                    System.Drawing.Imaging
                        .PixelFormat.Format32bppArgb
                ))
            {
                using (Graphics graphics =
                    Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.Transparent);

                    graphics.SmoothingMode =
                        System.Drawing.Drawing2D
                            .SmoothingMode.AntiAlias;

                    graphics.CompositingQuality =
                        System.Drawing.Drawing2D
                            .CompositingQuality.HighQuality;

                    graphics.InterpolationMode =
                        System.Drawing.Drawing2D
                            .InterpolationMode.HighQualityBicubic;

                    float fillHeight =
                        textureSize *
                        fillLevel /
                        100f;

                    graphics.SetClip(
                        new RectangleF(
                            0f,
                            textureSize - fillHeight,
                            textureSize,
                            fillHeight
                        )
                    );

                    using (SolidBrush brush =
                        new SolidBrush(color))
                    {
                        graphics.FillEllipse(
                            brush,
                            2f,
                            2f,
                            textureSize - 4f,
                            textureSize - 4f
                        );
                    }

                    graphics.ResetClip();
                }

                bitmap.Save(
                    texturePath,
                    System.Drawing.Imaging
                        .ImageFormat.Png
                );
            }
        }

        private void DrawRectangle(
            float centerX,
            float centerY,
            float width,
            float height,
            Color color)
        {
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

        private void DrawDynamicText(
            string text,
            float x,
            float y,
            float scale,
            Color color)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            TextElement element =
                new TextElement(
                    text,
                    new PointF(
                        x * Screen.Width,
                        y * Screen.Height
                    ),
                    scale,
                    color
                );

            element.Alignment =
                Alignment.Center;

            element.Outline = true;
            element.Draw();
        }

        private void DrawStaticText(
            string text,
            float x,
            float y,
            float scale,
            Color color)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string textureKey =
                "hud_v2|" +
                text +
                "|" +
                scale.ToString(
                    "R",
                    CultureInfo.InvariantCulture
                ) +
                "|" +
                color.ToArgb();

            CustomSprite sprite;
            SizeF displaySize;

            if (!textSprites.TryGetValue(
                textureKey,
                out sprite))
            {
                try
                {
                    Directory.CreateDirectory(
                        textCacheFolder
                    );

                    string texturePath =
                        Path.Combine(
                            textCacheFolder,
                            GetSafeTextFileName(
                                textureKey
                            ) + ".png"
                        );

                    displaySize =
                        CreateHudTextTexture(
                            texturePath,
                            text,
                            scale,
                            color
                        );

                    sprite =
                        new CustomSprite(
                            texturePath,
                            displaySize,
                            new PointF(0f, 0f),
                            Color.White,
                            0f,
                            false
                        );

                    textSprites[textureKey] = sprite;
                    textSpriteSizes[textureKey] = displaySize;
                }
                catch
                {
                    return;
                }
            }
            else if (!textSpriteSizes.TryGetValue(
                textureKey,
                out displaySize))
            {
                return;
            }

            sprite.Position =
                new PointF(
                    x * 1280f - displaySize.Width / 2f,
                    y * 720f
                );

            sprite.Size = displaySize;
            sprite.Color = Color.White;
            sprite.Draw();
        }

        private void DrawCenteredStaticText(
    string text,
    float x,
    float y,
    float scale,
    Color color)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string textureKey =
                "hud_v2|" +
                text +
                "|" +
                scale.ToString(
                    "R",
                    CultureInfo.InvariantCulture
                ) +
                "|" +
                color.ToArgb();

            CustomSprite sprite;
            SizeF displaySize;

            if (!textSprites.TryGetValue(
                textureKey,
                out sprite))
            {
                try
                {
                    Directory.CreateDirectory(
                        textCacheFolder
                    );

                    string texturePath =
                        Path.Combine(
                            textCacheFolder,
                            GetSafeTextFileName(
                                textureKey
                            ) + ".png"
                        );

                    displaySize =
                        CreateHudTextTexture(
                            texturePath,
                            text,
                            scale,
                            color
                        );

                    sprite =
                        new CustomSprite(
                            texturePath,
                            displaySize,
                            new PointF(0f, 0f),
                            Color.White,
                            0f,
                            false
                        );

                    textSprites[textureKey] =
                        sprite;

                    textSpriteSizes[textureKey] =
                        displaySize;
                }
                catch
                {
                    return;
                }
            }
            else if (!textSpriteSizes.TryGetValue(
                textureKey,
                out displaySize))
            {
                return;
            }

            sprite.Position =
                new PointF(
                    x * 1280f -
                        displaySize.Width / 2f,

                    y * 720f -
                        displaySize.Height / 2f
                );

            sprite.Size =
                displaySize;

            sprite.Color =
                Color.White;

            sprite.Draw();
        }

        private SizeF CreateHudTextTexture(
            string texturePath,
            string text,
            float scale,
            Color color)
        {
            const float resolutionScale = 2f;

            if (File.Exists(texturePath))
            {
                using (Image image =
                    Image.FromFile(texturePath))
                {
                    return new SizeF(
                        image.Width / resolutionScale,
                        image.Height / resolutionScale
                    );
                }
            }

            float fontSize =
                Math.Max(
                    10f,
                    scale * 54f
                ) *
                resolutionScale;

            using (System.Drawing.Font font =
                new System.Drawing.Font(
                    "Arial",
                    fontSize,
                    FontStyle.Bold,
                    GraphicsUnit.Pixel
                ))
            {
                SizeF measured;

                using (Bitmap measurement =
                    new Bitmap(1, 1))
                {
                    using (Graphics graphics =
                        Graphics.FromImage(
                            measurement
                        ))
                    {
                        measured =
                            graphics.MeasureString(
                                text,
                                font
                            );
                    }
                }

                int width =
                    Math.Max(
                        4,
                        (int)Math.Ceiling(
                            measured.Width
                        ) + 12
                    );

                int height =
                    Math.Max(
                        4,
                        (int)Math.Ceiling(
                            measured.Height
                        ) + 12
                    );

                using (Bitmap bitmap =
                    new Bitmap(
                        width,
                        height,
                        System.Drawing.Imaging
                            .PixelFormat.Format32bppArgb
                    ))
                {
                    using (Graphics graphics =
                        Graphics.FromImage(bitmap))
                    {
                        graphics.Clear(Color.Transparent);

                        graphics.SmoothingMode =
                            System.Drawing.Drawing2D
                                .SmoothingMode.AntiAlias;

                        graphics.TextRenderingHint =
                            System.Drawing.Text
                                .TextRenderingHint.AntiAliasGridFit;

                        using (SolidBrush outline =
                            new SolidBrush(
                                Color.FromArgb(
                                    245,
                                    0,
                                    0,
                                    0
                                )
                            ))
                        {
                            for (int offsetX = -2;
                                offsetX <= 2;
                                offsetX++)
                            {
                                for (int offsetY = -2;
                                    offsetY <= 2;
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
                                        outline,
                                        6f + offsetX,
                                        5f + offsetY
                                    );
                                }
                            }
                        }

                        using (SolidBrush brush =
                            new SolidBrush(color))
                        {
                            graphics.DrawString(
                                text,
                                font,
                                brush,
                                6f,
                                5f
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
                    width / resolutionScale,
                    height / resolutionScale
                );
            }
        }

        private string GetSafeTextFileName(
            string value)
        {
            using (System.Security.Cryptography.SHA1 sha1 =
                System.Security.Cryptography.SHA1.Create())
            {
                byte[] bytes =
                    System.Text.Encoding.UTF8.GetBytes(value);

                byte[] hash =
                    sha1.ComputeHash(bytes);

                return BitConverter
                    .ToString(hash)
                    .Replace("-", string.Empty);
            }
        }

        private float Clamp(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 100f)
            {
                return 100f;
            }

            return value;
        }
    }
}