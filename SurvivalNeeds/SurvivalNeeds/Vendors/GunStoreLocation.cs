using GTA;
using GTA.Math;
using GTA.Native;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

public class GunStoreLocations
{
    private readonly List<Vector3> locations =
        new List<Vector3>()
        {
            new Vector3(22.09f, -1107.28f, 29.80f),
            new Vector3(252.11f, -50.20f, 69.94f),
            new Vector3(-1305.40f, -394.03f, 36.70f),
            new Vector3(-662.03f, -934.93f, 21.83f),
            new Vector3(810.20f, -2157.30f, 29.62f),
            new Vector3(1693.44f, 3760.16f, 34.71f),
            new Vector3(-1117.58f, 2698.61f, 18.55f),
            new Vector3(2567.69f, 294.38f, 108.73f)
        };

    private bool ePressedLastFrame;

    private const float MarkerDrawDistance =
        25.0f;

    private const float InteractionDistance =
        2.0f;

    public bool Update()
    {
        Ped player =
            Game.Player.Character;

        if (player == null ||
            !player.Exists())
        {
            return false;
        }

        bool ePressed =
            Game.IsKeyPressed(
                Keys.E
            );

        bool openedStore =
            false;

        foreach (Vector3 location
            in locations)
        {
            float distance =
                player.Position.DistanceTo(
                    location
                );

            if (distance <=
                MarkerDrawDistance)
            {
                World.DrawMarker(
                    MarkerType.VerticalCylinder,
                    location,
                    Vector3.Zero,
                    Vector3.Zero,
                    new Vector3(
                        0.80f,
                        0.80f,
                        0.25f
                    ),
                    Color.FromArgb(
                        180,
                        20,
                        225,
                        195
                    )
                );
            }

            if (distance <=
                InteractionDistance)
            {
                // Prevent GTA's default Ammu-Nation E action.
                Function.Call(
                    Hash.DISABLE_CONTROL_ACTION,
                    0,
                    38,
                    true
                );

                GTA.UI.Screen
                    .ShowHelpTextThisFrame(
                        "Press ~y~E~s~ to open the custom Ammu-Nation."
                    );

                if (ePressed &&
                    !ePressedLastFrame)
                {
                    openedStore = true;
                    break;
                }
            }
        }

        ePressedLastFrame =
            ePressed;

        return openedStore;
    }
}