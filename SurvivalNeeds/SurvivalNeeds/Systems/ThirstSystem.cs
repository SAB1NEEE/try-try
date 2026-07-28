using GTA.Native;

namespace SurvivalNeeds.Systems
{
    public class ThirstSystem
    {
        public float Value { get; private set; } = 100f;

        private int lastTotalMinutes = -1;

        private const float DrainPerGameHour = 6.0f;
        private const float MinutesPerGameHour = 60.0f;

        public void Update()
        {
            int currentHour =
                Function.Call<int>(
                    Hash.GET_CLOCK_HOURS
                );

            int currentMinute =
                Function.Call<int>(
                    Hash.GET_CLOCK_MINUTES
                );

            int currentTotalMinutes =
                currentHour * 60 +
                currentMinute;

            if (lastTotalMinutes == -1)
            {
                lastTotalMinutes = currentTotalMinutes;
                return;
            }

            int elapsedMinutes =
                currentTotalMinutes -
                lastTotalMinutes;

            if (elapsedMinutes < 0)
            {
                elapsedMinutes += 24 * 60;
            }

            if (elapsedMinutes <= 0)
            {
                return;
            }

            lastTotalMinutes = currentTotalMinutes;

            float drainPerMinute =
                DrainPerGameHour /
                MinutesPerGameHour;

            Value -=
                drainPerMinute *
                elapsedMinutes;

            Clamp();
        }

        public void Add(float amount)
        {
            Value += amount;
            Clamp();
        }

        public void Reduce(float amount)
        {
            Value -= amount;
            Clamp();
        }

        public void Set(float amount)
        {
            Value = amount;
            Clamp();
        }

        private void Clamp()
        {
            if (Value < 0f)
            {
                Value = 0f;
            }

            if (Value > 100f)
            {
                Value = 100f;
            }
        }
    }
}