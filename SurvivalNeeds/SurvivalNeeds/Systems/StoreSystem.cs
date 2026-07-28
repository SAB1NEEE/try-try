using GTA;
using GTA.Math;
using System.Collections.Generic;

namespace SurvivalNeeds.Shops
{
    public class StoreLocation
    {
        public string Name
        {
            get;
            private set;
        }

        public Vector3 Position
        {
            get;
            private set;
        }

        public StoreLocation(
            string name,
            Vector3 position)
        {
            Name = name;
            Position = position;
        }
    }

    public class StoreSystem
    {
        private readonly List<StoreLocation>
            stores =
                new List<StoreLocation>();

        public StoreSystem()
        {
            stores.Add(
                new StoreLocation(
                    "24/7 Strawberry",
                    new Vector3(
                        28.46f,
                        -1353.03f,
                        29.34f
                    )
                )
            );

            stores.Add(
                new StoreLocation(
                    "LTD Davis",
                    new Vector3(
                        -47.42f,
                        -1758.67f,
                        29.42f
                    )
                )
            );

            stores.Add(
                new StoreLocation(
                    "LTD Mirror Park",
                    new Vector3(
                        1163.40f,
                        -323.80f,
                        69.20f
                    )
                )
            );

            stores.Add(
                new StoreLocation(
                    "24/7 Banham Canyon",
                    new Vector3(
                        -3040.20f,
                        585.60f,
                        7.90f
                    )
                )
            );

            stores.Add(
                new StoreLocation(
                    "24/7 Chumash",
                    new Vector3(
                        -3242.20f,
                        1001.40f,
                        12.80f
                    )
                )
            );

            stores.Add(
                new StoreLocation(
                    "24/7 Sandy Shores",
                    new Vector3(
                        1961.40f,
                        3740.70f,
                        32.30f
                    )
                )
            );

            stores.Add(
                new StoreLocation(
                    "LTD Grapeseed",
                    new Vector3(
                        1698.40f,
                        4924.40f,
                        42.10f
                    )
                )
            );

            stores.Add(
                new StoreLocation(
                    "24/7 Paleto Bay",
                    new Vector3(
                        1729.20f,
                        6414.10f,
                        35.00f
                    )
                )
            );
        }

        public StoreLocation GetNearbyStore(
            float maximumDistance = 2.4f)
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return null;
            }

            StoreLocation nearest = null;
            float nearestDistance =
                maximumDistance;

            foreach (StoreLocation store
                in stores)
            {
                float distance =
                    player.Position.DistanceTo(
                        store.Position
                    );

                if (distance <= nearestDistance)
                {
                    nearest = store;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
    }
}
