using SurvivalNeeds.Actions;

namespace SurvivalNeeds.Managers
{
    public static class ConsumableManager
    {
        private static ConsumableAction currentAction;

        public static bool IsBusy
        {
            get
            {
                return currentAction != null && currentAction.IsRunning;
            }
        }

        public static bool Start(ConsumableAction action)
        {
            if (IsBusy)
                return false;

            currentAction = action;

            return currentAction.Start();
        }

        public static void Update()
        {
            if (!IsBusy)
                return;

            currentAction.Update();

            if (!currentAction.IsRunning)
            {
                currentAction = null;
            }
        }

        public static void Cancel()
        {
            if (!IsBusy)
                return;

            currentAction.Stop();
            currentAction = null;
        }
    }
}