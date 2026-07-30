using GTA.Math;

namespace SurvivalNeeds.Apartments
{
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
            Vector3 exteriorEntrance,
            float exteriorHeading,
            Vector3 interiorSpawn,
            float interiorHeading,
            Vector3 interiorExit,
            Vector3 bedPosition,
            Vector3 storagePosition)
        {
            Id = id;
            Name = name;

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
    }
}