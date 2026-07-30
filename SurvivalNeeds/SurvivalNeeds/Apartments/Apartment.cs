using GTA.Math;

namespace SurvivalNeeds.Apartments
{
    public enum ApartmentClass
    {
        LowEnd,
        MidEnd,
        HighEnd
    }

    public class Apartment
    {
        public string Id
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public ApartmentClass Class
        {
            get;
            private set;
        }

        public int Price
        {
            get;
            private set;
        }

        public Vector3 ExteriorEntrance
        {
            get;
            private set;
        }

        public float ExteriorHeading
        {
            get;
            private set;
        }

        public Vector3 InteriorSpawn
        {
            get;
            private set;
        }

        public float InteriorHeading
        {
            get;
            private set;
        }

        public Vector3 InteriorExit
        {
            get;
            private set;
        }

        public Vector3 BedPosition
        {
            get;
            private set;
        }

        public Vector3 StoragePosition
        {
            get;
            private set;
        }

        public Apartment(
            string id,
            string name,
            ApartmentClass apartmentClass,
            int price,
            Vector3 exteriorEntrance,
            float exteriorHeading,
            Vector3 interiorSpawn,
            float interiorHeading,
            Vector3 interiorExit,
            Vector3 bedPosition,
            Vector3 storagePosition)
        {
            Id =
                id;

            Name =
                name;

            Class =
                apartmentClass;

            Price =
                price;

            ExteriorEntrance =
                exteriorEntrance;

            ExteriorHeading =
                exteriorHeading;

            InteriorSpawn =
                interiorSpawn;

            InteriorHeading =
                interiorHeading;

            InteriorExit =
                interiorExit;

            BedPosition =
                bedPosition;

            StoragePosition =
                storagePosition;
        }

        public string GetClassDisplayName()
        {
            switch (Class)
            {
                case ApartmentClass.LowEnd:
                    return "Low-End";

                case ApartmentClass.MidEnd:
                    return "Mid-End";

                case ApartmentClass.HighEnd:
                    return "High-End";

                default:
                    return "Apartment";
            }
        }
    }
}