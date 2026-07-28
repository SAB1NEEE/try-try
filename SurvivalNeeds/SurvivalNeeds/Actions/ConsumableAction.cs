namespace SurvivalNeeds.Actions
{
    public enum ConsumableType
    {
        Eat,
        Drink,
        Smoke
    }

    public abstract class ConsumableAction
    {
        public abstract bool Start();

        public abstract void Update();

        public abstract void Stop();

        public abstract bool IsRunning { get; }
    }
}