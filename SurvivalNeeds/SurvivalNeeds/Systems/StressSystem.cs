using GTA;
using GTA.Native;

namespace SurvivalNeeds.Systems
{
    public class StressSystem
    {
        public float Value { get; private set; } = 0f;

        private int lastUpdateTime = 0;
        private int lastHealth = -1;
        private int lastShotStressTime = 0;

        private const float RunningStressPerSecond = 0.10f;
        private const float SprintingStressPerSecond = 0.18f;
        private const float LowNeedsStressPerSecond = 0.08f;
        private const float CriticalNeedsStressPerSecond = 0.18f;
        private const float CalmDownPerSecond = 0.05f;

        private const int ShotStressCooldown = 1200;

        public void Update(
            float hunger,
            float thirst)
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            int now =
                Game.GameTime;

            if (lastUpdateTime == 0)
            {
                lastUpdateTime = now;
                lastHealth = player.Health;
                return;
            }

            int elapsedMs =
                now - lastUpdateTime;

            if (elapsedMs <= 0)
            {
                return;
            }

            lastUpdateTime = now;

            float elapsedSeconds =
                elapsedMs / 1000.0f;

            ApplyMovementStress(
                player,
                elapsedSeconds
            );

            ApplyShootingStress(
                player,
                now
            );

            ApplyDamageStress(
                player
            );

            ApplyNeedsStress(
                hunger,
                thirst,
                elapsedSeconds
            );

            ApplyCalmDown(
                player,
                hunger,
                thirst,
                elapsedSeconds
            );

            Clamp();
        }

        private void ApplyMovementStress(
            Ped player,
            float elapsedSeconds)
        {
            bool sprinting =
                Function.Call<bool>(
                    Hash.IS_PED_SPRINTING,
                    player.Handle
                );

            bool running =
                Function.Call<bool>(
                    Hash.IS_PED_RUNNING,
                    player.Handle
                );

            if (sprinting)
            {
                Add(
                    SprintingStressPerSecond *
                    elapsedSeconds
                );
            }
            else if (running)
            {
                Add(
                    RunningStressPerSecond *
                    elapsedSeconds
                );
            }
        }

        private void ApplyShootingStress(
            Ped player,
            int now)
        {
            bool shooting =
                Function.Call<bool>(
                    Hash.IS_PED_SHOOTING,
                    player.Handle
                );

            if (!shooting)
            {
                return;
            }

            if (now - lastShotStressTime <
                ShotStressCooldown)
            {
                return;
            }

            Add(1.5f);

            lastShotStressTime = now;
        }

        private void ApplyDamageStress(
            Ped player)
        {
            if (lastHealth < 0)
            {
                lastHealth = player.Health;
                return;
            }

            if (player.Health < lastHealth)
            {
                int damageTaken =
                    lastHealth - player.Health;

                Add(
                    damageTaken * 0.35f
                );
            }

            lastHealth = player.Health;
        }

        private void ApplyNeedsStress(
            float hunger,
            float thirst,
            float elapsedSeconds)
        {
            bool lowNeeds =
                hunger <= 10f ||
                thirst <= 10f;

            bool criticalNeeds =
                hunger <= 5f ||
                thirst <= 5f;

            if (criticalNeeds)
            {
                Add(
                    CriticalNeedsStressPerSecond *
                    elapsedSeconds
                );
            }
            else if (lowNeeds)
            {
                Add(
                    LowNeedsStressPerSecond *
                    elapsedSeconds
                );
            }
        }

        private void ApplyCalmDown(
            Ped player,
            float hunger,
            float thirst,
            float elapsedSeconds)
        {
            bool running =
                Function.Call<bool>(
                    Hash.IS_PED_RUNNING,
                    player.Handle
                );

            bool sprinting =
                Function.Call<bool>(
                    Hash.IS_PED_SPRINTING,
                    player.Handle
                );

            bool shooting =
                Function.Call<bool>(
                    Hash.IS_PED_SHOOTING,
                    player.Handle
                );

            bool lowNeeds =
                hunger <= 10f ||
                thirst <= 10f;

            if (!running &&
                !sprinting &&
                !shooting &&
                !lowNeeds)
            {
                Reduce(
                    CalmDownPerSecond *
                    elapsedSeconds
                );
            }
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