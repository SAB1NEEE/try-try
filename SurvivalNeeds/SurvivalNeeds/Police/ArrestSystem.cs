using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using SurvivalNeeds.Inventory;
using SurvivalNeeds.Systems;
using SurvivalNeeds.WeaponInventory;
using System;
using System.Windows.Forms;

namespace SurvivalNeeds.Police
{
    public class ArrestSystem
    {
        private enum ArrestState
        {
            Idle,
            Surrendering,
            OfficerApproaching,
            Handcuffing,
            Busted,
            Fading,
            Respawning
        }

        private ArrestState state = ArrestState.Idle;

        private Ped player;
        private Ped arrestingOfficer;

        private bool hWasPressed;
        private int stateStartTime;
        private int lastOfficerCommandTime;

        private const float HandcuffDistance = 2.2f;
        private const float OfficerSearchDistance = 80.0f;

        private readonly Vector3 policeStationPosition =
        new Vector3(425.10f, -979.50f, 30.70f);

        private const float PoliceStationHeading = 90.0f;

        private readonly ConfiscationSystem confiscationSystem;
        private readonly WeaponInventoryManager weaponInventoryManager;

        private readonly Action saveAfterConfiscation;
        public void Update()
        {
            player = Game.Player.Character;

            if (player == null || !player.Exists())
            {
                return;
            }

            bool hPressed = Game.IsKeyPressed(Keys.H);
            bool hJustPressed = hPressed && !hWasPressed;
            hWasPressed = hPressed;

            switch (state)
            {
                case ArrestState.Idle:
                    UpdateIdle(hJustPressed);
                    break;

                case ArrestState.Surrendering:
                case ArrestState.OfficerApproaching:
                    UpdateSurrender(hJustPressed);
                    break;

                case ArrestState.Handcuffing:
                    UpdateHandcuffing();
                    break;

                case ArrestState.Busted:
                    UpdateBusted();
                    break;

                case ArrestState.Fading:
                    UpdateFading();
                    break;

                case ArrestState.Respawning:
                    RespawnPlayer();
                    break;
            }
        }

        public ArrestSystem(
            MoneySystem money,
            InventoryManager inventory,
            WeaponInventoryManager weaponInventoryManager,
            SaveSystem saveSystem,
            Action saveAfterConfiscation)
        {
            this.weaponInventoryManager =
                weaponInventoryManager;

            this.saveAfterConfiscation =
                saveAfterConfiscation;

            confiscationSystem =
                new ConfiscationSystem(
                    money,
                    inventory,
                    saveSystem
                );
        }

        private void UpdateIdle(bool hJustPressed)
        {
            if (!hJustPressed)
            {
                return;
            }

            if (Game.Player.WantedLevel <= 0)
            {
                Notification.Show("You are not currently wanted.");
                return;
            }

            if (player.IsInVehicle())
            {
                Notification.Show("Exit the vehicle before surrendering.");
                return;
            }

            StartSurrender();
        }

        private void StartSurrender()
        {

            state = ArrestState.Surrendering;
            stateStartTime = Game.GameTime;

            player.Task.ClearAll();
            player.Weapons.Select(WeaponHash.Unarmed);

            Function.Call(
                Hash.TASK_HANDS_UP,
                player.Handle,
                -1,
                0,
                -1,
                true
            );

            arrestingOfficer = FindNearestPoliceOfficer();

            Notification.Show(
                "Surrendering~n~Press ~y~H~s~ again to cancel."
            );
        }

        private void UpdateSurrender(bool hJustPressed)
        {
            DisablePlayerActions();

            // The player may cancel only before handcuffing starts.
            if (hJustPressed)
            {
                CancelSurrender();
                return;
            }

            KeepHandsRaised();

            if (arrestingOfficer == null ||
                !arrestingOfficer.Exists() ||
                arrestingOfficer.IsDead)
            {
                arrestingOfficer = FindNearestPoliceOfficer();

                if (arrestingOfficer == null)
                {
                    GTA.UI.Screen.ShowSubtitle(
                        "Waiting for a police officer...",
                        1000
                    );

                    return;
                }
            }

            state = ArrestState.OfficerApproaching;

            float distance =
                arrestingOfficer.Position.DistanceTo(player.Position);

            if (distance <= HandcuffDistance)
            {
                StartHandcuffing();
                return;
            }

            CommandOfficerToApproach();

            GTA.UI.Screen.ShowSubtitle(
                "SURRENDERING~n~Press H to cancel",
                1000
            );
        }

        private void KeepHandsRaised()
        {
            if (!Function.Call<bool>(
                Hash.IS_ENTITY_PLAYING_ANIM,
                player.Handle,
                "random@mugging3",
                "handsup_standing_base",
                3
            ))
            {
                Function.Call(
                    Hash.TASK_HANDS_UP,
                    player.Handle,
                    -1,
                    0,
                    -1,
                    true
                );
            }
        }

        private void CommandOfficerToApproach()
        {
            if (Game.GameTime - lastOfficerCommandTime < 1000)
            {
                return;
            }

            lastOfficerCommandTime = Game.GameTime;

            arrestingOfficer.BlockPermanentEvents = true;

            Function.Call(
                Hash.SET_PED_KEEP_TASK,
                arrestingOfficer.Handle,
                true
            );

            Function.Call(
                Hash.TASK_GO_TO_ENTITY,
                arrestingOfficer.Handle,
                player.Handle,
                -1,
                1.3f,
                1.5f,
                1073741824,
                0
            );
        }

        private void StartHandcuffing()
        {
            /*
             * POINT OF NO RETURN:
             * Once this method starts, pressing H cannot cancel.
             */

            state = ArrestState.Handcuffing;
            stateStartTime = Game.GameTime;

            player.Task.ClearAllImmediately();

            if (arrestingOfficer != null &&
                arrestingOfficer.Exists())
            {
                arrestingOfficer.Task.ClearAllImmediately();
            }


            RequestAnimationDictionary("mp_arresting");

            Function.Call(
                Hash.TASK_PLAY_ANIM,
                player.Handle,
                "mp_arresting",
                "idle",
                8.0f,
                -8.0f,
                -1,
                49,
                0.0f,
                false,
                false,
                false
            );

            Function.Call(
                Hash.SET_ENABLE_HANDCUFFS,
                player.Handle,
                true
            );

            Function.Call(
                Hash.SET_CURRENT_PED_WEAPON,
                player.Handle,
                (uint)WeaponHash.Unarmed,
                true
            );

            Notification.Show(
                "You are being arrested.~n~Surrender can no longer be cancelled."
            );
        }

        private void UpdateHandcuffing()
        {
            DisablePlayerActions();

            // H is intentionally ignored in this state.
            if (Game.GameTime - stateStartTime < 3000)
            {
                GTA.UI.Screen.ShowSubtitle(
                    "YOU ARE BEING ARRESTED",
                    1000
                );

                return;
            }

            state = ArrestState.Busted;
            stateStartTime = Game.GameTime;

            Function.Call(
                Hash.PLAY_SOUND_FRONTEND,
                -1,
                "ScreenFlash",
                "MissionFailedSounds",
                true
            );
        }

        private void UpdateBusted()
        {
            DisablePlayerActions();

            DrawBustedText();

            if (Game.GameTime - stateStartTime < 2500)
            {
                return;
            }

            state = ArrestState.Fading;
            stateStartTime = Game.GameTime;

            Function.Call(Hash.DO_SCREEN_FADE_OUT, 1500);
        }

        private void UpdateFading()
        {
            DisablePlayerActions();

            if (Game.GameTime - stateStartTime < 1800)
            {
                return;
            }

            state = ArrestState.Respawning;
        }

        private void RespawnPlayer()
        {
            Game.Player.WantedLevel = 0;

            Function.Call(
                Hash.CLEAR_PLAYER_WANTED_LEVEL,
                Game.Player.Handle
            );

            Function.Call(
                Hash.REQUEST_COLLISION_AT_COORD,
                policeStationPosition.X,
                policeStationPosition.Y,
                policeStationPosition.Z
            );

            Function.Call(
                Hash.LOAD_SCENE,
                policeStationPosition.X,
                policeStationPosition.Y,
                policeStationPosition.Z
            );

            player.Task.ClearAllImmediately();

            Function.Call(
                Hash.SET_ENABLE_HANDCUFFS,
                player.Handle,
                false
            );

            Function.Call(
                Hash.SET_ENTITY_COORDS_NO_OFFSET,
                player.Handle,
                policeStationPosition.X,
                policeStationPosition.Y,
                policeStationPosition.Z,
                false,
                false,
                false
            );

            player.Heading = PoliceStationHeading;

            // Confiscate cash, normal inventory
            // and GTA weapons.
            confiscationSystem.ConfiscatePlayer();

            // Remove all weapons from the custom
            // persistent weapon inventory.
            if (weaponInventoryManager != null)
            {
                weaponInventoryManager.Clear();
            }

            // Immediately save the empty weapon inventory.
            // This prevents confiscated weapons from returning
            // after reloading the script or restarting GTA.
            saveAfterConfiscation?.Invoke();

            if (arrestingOfficer != null &&
                arrestingOfficer.Exists())
            {
                arrestingOfficer.BlockPermanentEvents = false;
                arrestingOfficer.Task.ClearAll();
            }

            arrestingOfficer = null;

            state = ArrestState.Idle;

            Function.Call(
                Hash.DO_SCREEN_FADE_IN,
                1500
            );

            Notification.Show(
            "~r~You were arrested.~n~~s~Cash, weapons and inventory confiscated."
            );
        }

        private void CancelSurrender()
        {
            player.Task.ClearAllImmediately();

            Function.Call(
                Hash.SET_ENABLE_HANDCUFFS,
                player.Handle,
                false
            );

            if (arrestingOfficer != null &&
                arrestingOfficer.Exists())
            {
                arrestingOfficer.BlockPermanentEvents = false;
                arrestingOfficer.Task.ClearAll();

                Function.Call(
                    Hash.TASK_COMBAT_PED,
                    arrestingOfficer.Handle,
                    player.Handle,
                    0,
                    16
                );
            }

            arrestingOfficer = null;
            state = ArrestState.Idle;

            Notification.Show(
                "Fake surrender! Police pursuit resumed."
            );
        }

        private Ped FindNearestPoliceOfficer()
        {
            Ped nearestOfficer = null;
            float nearestDistance = OfficerSearchDistance;

            Ped[] nearbyPeds =
                World.GetNearbyPeds(player, OfficerSearchDistance);

            foreach (Ped ped in nearbyPeds)
            {
                if (ped == null ||
                    !ped.Exists() ||
                    ped == player ||
                    ped.IsDead)
                {
                    continue;
                }

                int pedType = Function.Call<int>(
                Hash.GET_PED_TYPE,
                ped.Handle
                );

                // 6 = normal police officer
                // 27 = SWAT / tactical police
                if (pedType != 6 && pedType != 27)
                {
                    continue;
                }

                float distance =
                    ped.Position.DistanceTo(player.Position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestOfficer = ped;
                }
            }

            return nearestOfficer;
        }

        private void DisablePlayerActions()
        {
            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                24,
                true
            );

            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                25,
                true
            );

            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                37,
                true
            );

            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                44,
                true
            );

            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                75,
                true
            );

            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                140,
                true
            );

            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                141,
                true
            );

            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                142,
                true
            );

            if (state == ArrestState.Handcuffing ||
                state == ArrestState.Busted ||
                state == ArrestState.Fading)
            {
                Function.Call(
                    Hash.DISABLE_ALL_CONTROL_ACTIONS,
                    0
                );
            }
        }

        private void DrawBustedText()
        {
            Function.Call(
                Hash.SET_TEXT_FONT,
                7
            );

            Function.Call(
                Hash.SET_TEXT_SCALE,
                1.5f,
                1.5f
            );

            Function.Call(
                Hash.SET_TEXT_COLOUR,
                190,
                0,
                0,
                255
            );

            Function.Call(
                Hash.SET_TEXT_CENTRE,
                true
            );

            Function.Call(
                Hash.SET_TEXT_OUTLINE
            );

            Function.Call(
                Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT,
                "STRING"
            );

            Function.Call(
                Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME,
                "BUSTED"
            );

            Function.Call(
                Hash.END_TEXT_COMMAND_DISPLAY_TEXT,
                0.5f,
                0.42f
            );
        }

        private void RequestAnimationDictionary(string dictionary)
        {
            Function.Call(
                Hash.REQUEST_ANIM_DICT,
                dictionary
            );

            int timeout = Game.GameTime + 3000;

            while (!Function.Call<bool>(
                       Hash.HAS_ANIM_DICT_LOADED,
                       dictionary
                   ) &&
                   Game.GameTime < timeout)
            {
                Script.Yield();
            }
        }
    }
}