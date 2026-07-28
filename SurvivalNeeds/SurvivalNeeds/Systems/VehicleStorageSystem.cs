using GTA;
using GTA.Math;

namespace SurvivalNeeds.VehicleStorage
{
    public class VehicleStorageSystem
    {
        private const float TrunkInteractionDistance = 2.0f;

        public Vehicle GetNearbyVehicle()
        {
            Ped player = Game.Player.Character;

            if (player == null || !player.Exists())
                return null;

            Vehicle closestVehicle = null;
            float closestDistance = TrunkInteractionDistance;

            foreach (Vehicle vehicle in World.GetAllVehicles())
            {
                if (vehicle == null || !vehicle.Exists())
                    continue;

                // Cars only:
                // excludes motorcycles, bicycles, boats, planes,
                // helicopters and other non-car vehicles.
                if (!vehicle.Model.IsCar)
                    continue;

                // Do not access a trunk while sitting inside the vehicle.
                if (player.IsInVehicle(vehicle))
                    continue;

                Vector3 trunkPosition = vehicle.GetOffsetPosition(
                    new Vector3(0f, -2.5f, 0f)
                );

                float distance = player.Position.DistanceTo(
                    trunkPosition
                );

                if (distance < closestDistance)
                {
                    closestVehicle = vehicle;
                    closestDistance = distance;
                }
            }

            return closestVehicle;
        }
    }
}