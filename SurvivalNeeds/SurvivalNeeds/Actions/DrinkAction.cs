using GTA;
using GTA.Math;
using SurvivalNeeds.Managers;
using SurvivalNeeds.Animations;

namespace SurvivalNeeds.Actions
{
    public class DrinkAction : ConsumableAction
    {
        private readonly string propModel;
        private readonly int durationMilliseconds;
        private readonly System.Action onFinished;

        private int startTime;
        private bool isRunning;

        public override bool IsRunning
        {
            get { return isRunning; }
        }

        public DrinkAction(
            string propModel,
            int durationMilliseconds,
            System.Action onFinished)
        {
            this.propModel = propModel;
            this.durationMilliseconds = durationMilliseconds;
            this.onFinished = onFinished;
        }

        public override bool Start()
        {
            if (isRunning)
                return false;

            bool propAttached = PropManager.AttachProp(
                propModel,
                Bone.SkelRightHand,
                new Vector3(0.10f, 0.02f, 0.00f),
                new Vector3(10.0f, 90.0f, 0.0f));

            if (!propAttached)
                return false;

            bool animationStarted = AnimationManager.Play(
                AnimationLibrary.DrinkDictionary,
                AnimationLibrary.DrinkAnimation,
                durationMilliseconds);

            if (!animationStarted)
            {
                PropManager.RemoveProp();
                return false;
            }

            startTime = Game.GameTime;
            isRunning = true;

            return true;
        }

        public override void Update()
        {
            if (!isRunning)
                return;

            if (!CanContinue())
            {
                Stop();
                return;
            }

            if (Game.GameTime - startTime >= durationMilliseconds)
            {
                Finish();
            }
        }

        public override void Stop()
        {
            if (!isRunning)
                return;

            AnimationManager.Stop();
            PropManager.RemoveProp();

            isRunning = false;
        }

        private void Finish()
        {
            AnimationManager.Stop();
            PropManager.RemoveProp();

            isRunning = false;

            if (onFinished != null)
            {
                onFinished();
            }
        }

        private bool CanContinue()
        {
            Ped player = Game.Player.Character;

            if (player == null || !player.Exists())
                return false;

            if (player.IsDead)
                return false;

            if (player.IsRagdoll)
                return false;

            if (player.IsInVehicle())
                return false;

            return true;
        }
    }
}