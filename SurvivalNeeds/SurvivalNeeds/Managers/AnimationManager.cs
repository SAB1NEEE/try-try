using GTA;
using GTA.Native;

namespace SurvivalNeeds.Managers
{
    public static class AnimationManager
    {
        public static bool Play(string dictionary, string animation, int duration = -1)
        {
            Ped player = Game.Player.Character;

            if (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, dictionary))
            {
                Function.Call(Hash.REQUEST_ANIM_DICT, dictionary);

                int timeout = Game.GameTime + 5000;

                while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, dictionary))
                {
                    Script.Yield();

                    if (Game.GameTime > timeout)
                        return false;
                }
            }

            player.Task.PlayAnimation(
                dictionary,
                animation,
                8.0f,
                -8.0f,
                duration,
                AnimationFlags.Loop,
                0.0f);

            return true;
        }

        public static void Stop()
        {
            Game.Player.Character.Task.ClearAll();
        }

        public static bool IsPlaying(string dictionary, string animation)
        {
            return Function.Call<bool>(
                Hash.IS_ENTITY_PLAYING_ANIM,
                Game.Player.Character,
                dictionary,
                animation,
                3);
        }
    }
}