using GTA;

namespace SurvivalNeeds.WeaponInventory
{
    public class WeaponInventoryItem
    {
        public WeaponHash WeaponHash
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public float Weight
        {
            get;
            set;
        }

        public int Ammo
        {
            get;
            set;
        }

        public WeaponInventoryItem()
        {
        }

        public WeaponInventoryItem(
            WeaponHash weaponHash,
            string name,
            float weight,
            int ammo)
        {
            WeaponHash =
                weaponHash;

            Name =
                name;

            Weight =
                weight;

            Ammo =
                ammo;
        }
    }
}