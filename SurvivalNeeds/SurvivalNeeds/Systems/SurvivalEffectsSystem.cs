using GTA;
using GTA.Native;
using GTA.UI;
using System;

namespace SurvivalNeeds.Systems
{
    public class SurvivalEffectsSystem
    {
        private int lastWarningTime = 0;
        private int lastStressIncreaseTime = 0;
        private int lastStressWarningTime = 0;
        private int lastDamageTime = 0;

        private string currentMovementStyle = "";

        private const int WarningCooldown = 10000;
        private const int StressIncreaseCooldown = 30000;
        private const int StressWarningCooldown = 15000;

        // At hunger or thirst 0:
        // Lose 1 HP every 5 seconds.
        private const int ZeroNeedDamageCooldown = 3000;
        private const int ZeroNeedDamageAmount = 2;

        private const string InjuredMovement =
            "move_m@injured";

        private const string DrunkMovement =
            "move_m@drunk@verydrunk";

        public void Update(
            float hunger,
            float thirst,
            StressSystem stress)
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            float stressValue =
                stress != null
                    ? stress.Value
                    : 0f;

            ApplyCriticalMovement(
                hunger,
                thirst
            );

            ApplyZeroNeedDamage(
                hunger,
                thirst
            );

            ApplyStressPenalty(
                hunger,
                thirst,
                stress
            );

            ShowNeedsWarning(
                hunger,
                thirst
            );

            ApplyStressEffects(
                stressValue
            );

            ShowStressWarning(
                stressValue
            );
        }

        private void ApplyCriticalMovement(
            float hunger,
            float thirst)
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            float lowestNeed =
                Math.Min(
                    hunger,
                    thirst
                );

            // Hunger and thirst above 5:
            // completely normal movement.
            if (lowestNeed > 5f)
            {
                ResetMovementStyle();
                return;
            }

            bool isRunning =
                Function.Call<bool>(
                    Hash.IS_PED_RUNNING,
                    player.Handle
                );

            bool isSprinting =
                Function.Call<bool>(
                    Hash.IS_PED_SPRINTING,
                    player.Handle
                );

            string wantedStyle;

            // Running or sprinting:
            // use very-drunk running animation.
            if (isRunning ||
                isSprinting)
            {
                wantedStyle =
                    DrunkMovement;
            }
            else
            {
                // Walking or standing:
                // use injured movement animation.
                wantedStyle =
                    InjuredMovement;
            }

            ApplyMovementStyle(
                wantedStyle
            );
        }

        private void ApplyMovementStyle(
            string style)
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                string.IsNullOrEmpty(style))
            {
                return;
            }

            if (currentMovementStyle ==
                style)
            {
                return;
            }

            Function.Call(
                Hash.REQUEST_ANIM_SET,
                style
            );

            bool loaded =
                Function.Call<bool>(
                    Hash.HAS_ANIM_SET_LOADED,
                    style
                );

            if (!loaded)
            {
                return;
            }

            Function.Call(
                Hash.SET_PED_MOVEMENT_CLIPSET,
                player.Handle,
                style,
                0.25f
            );

            currentMovementStyle =
                style;
        }

        private void ResetMovementStyle()
        {
            if (string.IsNullOrEmpty(
                currentMovementStyle))
            {
                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                currentMovementStyle = "";
                return;
            }

            Function.Call(
                Hash.RESET_PED_MOVEMENT_CLIPSET,
                player.Handle,
                0.25f
            );

            currentMovementStyle = "";
        }

        private void ApplyZeroNeedDamage(
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

            bool hungerEmpty =
                hunger <= 0f;

            bool thirstEmpty =
                thirst <= 0f;

            // Damage only starts when hunger
            // or thirst reaches exactly zero.
            if (!hungerEmpty &&
                !thirstEmpty)
            {
                lastDamageTime =
                    Game.GameTime;

                return;
            }

            int now =
                Game.GameTime;

            if (now - lastDamageTime <
                ZeroNeedDamageCooldown)
            {
                return;
            }

            player.Health =
                Math.Max(
                    0,
                    player.Health -
                    ZeroNeedDamageAmount
                );

            lastDamageTime =
                now;
        }

        private void ApplyStressPenalty(
            float hunger,
            float thirst,
            StressSystem stress)
        {
            if (stress == null)
            {
                return;
            }

            // Stress only increases at
            // critical hunger or thirst.
            if (hunger > 5f &&
                thirst > 5f)
            {
                return;
            }

            int now =
                Game.GameTime;

            if (now - lastStressIncreaseTime <
                StressIncreaseCooldown)
            {
                return;
            }

            float stressIncrease =
                1f;

            if (hunger <= 0f ||
                thirst <= 0f)
            {
                stressIncrease =
                    2f;
            }

            stress.Add(
                stressIncrease
            );

            lastStressIncreaseTime =
                now;
        }

        private void ShowNeedsWarning(
            float hunger,
            float thirst)
        {
            // No warnings above 5.
            if (hunger > 5f &&
                thirst > 5f)
            {
                return;
            }

            int now =
                Game.GameTime;

            if (now - lastWarningTime <
                WarningCooldown)
            {
                return;
            }

            if (thirst <= 0f)
            {
                Notification.Show(
                    "~r~You are taking damage from dehydration."
                );
            }
            else if (hunger <= 0f)
            {
                Notification.Show(
                    "~r~You are taking damage from starvation."
                );
            }
            else if (thirst <= 5f)
            {
                Notification.Show(
                    "~r~You are severely dehydrated."
                );
            }
            else if (hunger <= 5f)
            {
                Notification.Show(
                    "~r~You are severely starving."
                );
            }

            lastWarningTime =
                now;
        }

        private void ApplyStressEffects(
            float stressValue)
        {
            if (stressValue < 50f)
            {
                StopCameraShake();
                return;
            }

            float shake =
                0.15f;

            if (stressValue >= 90f)
            {
                shake =
                    0.70f;
            }
            else if (stressValue >= 75f)
            {
                shake =
                    0.40f;
            }

            bool shaking =
                Function.Call<bool>(
                    Hash.IS_GAMEPLAY_CAM_SHAKING
                );

            if (!shaking)
            {
                Function.Call(
                    Hash.SHAKE_GAMEPLAY_CAM,
                    "DRUNK_SHAKE",
                    shake
                );
            }
            else
            {
                Function.Call(
                    Hash.SET_GAMEPLAY_CAM_SHAKE_AMPLITUDE,
                    shake
                );
            }
        }

        private void ShowStressWarning(
            float stressValue)
        {
            if (stressValue < 50f)
            {
                return;
            }

            int now =
                Game.GameTime;

            if (now - lastStressWarningTime <
                StressWarningCooldown)
            {
                return;
            }

            if (stressValue >= 90f)
            {
                Notification.Show(
                    "~r~Extreme stress."
                );
            }
            else if (stressValue >= 75f)
            {
                Notification.Show(
                    "~o~High stress."
                );
            }
            else
            {
                Notification.Show(
                    "~y~Stress increasing."
                );
            }

            lastStressWarningTime =
                now;
        }

        private void StopCameraShake()
        {
            bool shaking =
                Function.Call<bool>(
                    Hash.IS_GAMEPLAY_CAM_SHAKING
                );

            if (shaking)
            {
                Function.Call(
                    Hash.STOP_GAMEPLAY_CAM_SHAKING,
                    true
                );
            }
        }
    }
}