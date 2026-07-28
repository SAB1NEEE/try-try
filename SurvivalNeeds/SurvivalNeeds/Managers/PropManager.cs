using GTA;
using GTA.Math;
using GTA.Native;

namespace SurvivalNeeds.Managers
{
    public static class PropManager
    {
        private static Prop currentProp;

        public static bool HasProp
        {
            get
            {
                return currentProp != null && currentProp.Exists();
            }
        }

        public static bool AttachProp(
            string modelName,
            Bone bone,
            Vector3 positionOffset,
            Vector3 rotationOffset)
        {
            RemoveProp();

            Ped player = Game.Player.Character;
            Model model = new Model(modelName);

            if (!model.IsValid || !model.IsInCdImage)
            {
                return false;
            }

            model.Request(5000);

            if (!model.IsLoaded)
            {
                return false;
            }

            currentProp = World.CreateProp(
                model,
                player.Position,
                false,
                false);

            model.MarkAsNoLongerNeeded();

            if (currentProp == null || !currentProp.Exists())
            {
                currentProp = null;
                return false;
            }

            int boneIndex = Function.Call<int>(
                Hash.GET_PED_BONE_INDEX,
                player,
                (int)bone);

            Function.Call(
                Hash.ATTACH_ENTITY_TO_ENTITY,
                currentProp,
                player,
                boneIndex,
                positionOffset.X,
                positionOffset.Y,
                positionOffset.Z,
                rotationOffset.X,
                rotationOffset.Y,
                rotationOffset.Z,
                false,
                false,
                false,
                false,
                2,
                true);

            return true;
        }

        public static void RemoveProp()
        {
            if (currentProp == null)
            {
                return;
            }

            if (currentProp.Exists())
            {
                Function.Call(
                    Hash.DETACH_ENTITY,
                    currentProp,
                    true,
                    true);

                currentProp.Delete();
            }

            currentProp = null;
        }
    }
}