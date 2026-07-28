using GTA.Math;
using System.Collections.Generic;

namespace SurvivalNeeds.Vendors
{
    public class Vendor
    {
        public string Name
        {
            get;
            private set;
        }

        public VendorType Type
        {
            get;
            private set;
        }

        public StreetVendorType? StreetType
        {
            get;
            private set;
        }

        public Vector3 Position
        {
            get;
            private set;
        }

        public float InteractionDistance
        {
            get;
            private set;
        }

        public List<VendorItem> Items
        {
            get;
        }

        public Vendor(
            string name,
            VendorType type,
            Vector3 position,
            List<VendorItem> items,
            float interactionDistance = 2.5f,
            StreetVendorType? streetType = null)
        {
            Name = name;
            Type = type;
            Position = position;
            StreetType = streetType;

            Items = items ??
                new List<VendorItem>();

            InteractionDistance =
                interactionDistance;
        }

        public bool IsPlayerNearby(
            Vector3 playerPosition)
        {
            float distance =
                playerPosition.DistanceTo(
                    Position
                );

            return distance <=
                InteractionDistance;
        }
    }
}