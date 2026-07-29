using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using SurvivalNeeds.Inventory;
using SurvivalNeeds.Systems;
using System.Collections.Generic;
using System;
using System.Windows.Forms;

namespace SurvivalNeeds.Police
{
    public class ArrestSystem
    {
        private enum ArrestState
        {
            Idle,
            ArmedWarning,
            Surrendering,
            OfficerApproaching,
            TaserResponse,
            TasedArrest,
            Handcuffing,
            Busted,
            Fading,
            Respawning
        }

        private ArrestState state = ArrestState.Idle;

        private Ped player;
        private Ped arrestingOfficer;
        private readonly List<Ped> surrenderOfficers =
            new List<Ped>();

        private bool hWasPressed;
        private int stateStartTime;
        private int lastOfficerCommandTime;
        private int arrestWantedLevel;
        private int armedWarningStartTime;
        private int lastArmedWarningMessageTime;
        private Ped taserOfficer;
        private int taserResponseStartTime;
        private bool taserShotOrdered;
        private int tasedArrestStartTime;
        private int lastTasedOfficerCommandTime;

        private const float HandcuffDistance = 2.2f;
        private const float OfficerSearchDistance = 80.0f;

        private readonly Vector3 policeStationPosition =
        new Vector3(425.10f, -979.50f, 30.70f);

        private const float PoliceStationHeading = 90.0f;

        private readonly ConfiscationSystem confiscationSystem;
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
                case ArrestState.ArmedWarning:
                    UpdateArmedWarning();
                    break;

                case ArrestState.Surrendering:
                case ArrestState.OfficerApproaching:
                    UpdateSurrender(hJustPressed);
                    break;

                case ArrestState.TaserResponse:
                    UpdateTaserResponse();
                    break;

                case ArrestState.TasedArrest:
                    UpdateTasedArrest();
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
            SaveSystem saveSystem)
        {
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
                Notification.Show(
                    "Exit the vehicle before surrendering."
                );

                return;
            }

            WeaponHash currentWeapon =
                GetPlayerCurrentWeapon();

            if (currentWeapon !=
                WeaponHash.Unarmed)
            {
                StartArmedWarning(
                    currentWeapon
                );

                return;
            }

            StartSurrender();
        }

        //====================================================
        // START ARMED SUSPECT WARNING
        //====================================================

        private void StartArmedWarning(
            WeaponHash weaponHash)
        {
            arrestWantedLevel =
                Game.Player.WantedLevel;

            if (arrestWantedLevel < 1)
            {
                arrestWantedLevel = 1;
            }

            if (arrestWantedLevel > 5)
            {
                arrestWantedLevel = 5;
            }

            state =
                ArrestState.ArmedWarning;

            armedWarningStartTime =
                Game.GameTime;

            lastArmedWarningMessageTime =
                0;

            arrestingOfficer =
                FindNearestPoliceOfficer();

            ControlArmedWarningOfficers();


            if (IsMeleeWeapon(
                weaponHash))
            {
                Notification.Show(
                    "~y~POLICE WARNING~n~~s~Drop the melee weapon and put your hands up."
                );
            }
            else
            {
                Notification.Show(
                    "~r~POLICE WARNING~n~~s~Holster your firearm and put your hands up."
                );
            }
        }

        //====================================================
        // UPDATE ARMED SUSPECT WARNING
        //====================================================

        private void UpdateArmedWarning()
        {
            WeaponHash currentWeapon =
                GetPlayerCurrentWeapon();

            // The player complied.
            if (currentWeapon ==
                WeaponHash.Unarmed)
            {
                RestoreSurrenderOfficers(
                    false
                );

                arrestingOfficer =
                    null;

                StartSurrender();

                return;
            }

            ControlArmedWarningOfficers();

            if (IsPlayerDownFromTaser())
            {
                StartTasedArrest();
                return;
            }

            bool attackPressed =
                Game.IsControlPressed(
                    GTA.Control.Attack
                );

            bool aimPressed =
                Game.IsControlPressed(
                    GTA.Control.Aim
                );

            bool meleeAttackPressed =
                Game.IsControlPressed(
                    GTA.Control.MeleeAttack1
                ) ||
                Game.IsControlPressed(
                    GTA.Control.MeleeAttack2
                );

            bool playerIsShooting =
                player.IsShooting;

            if (attackPressed ||
                aimPressed ||
                meleeAttackPressed ||
                playerIsShooting)
            {
                CancelArmedWarningForThreat();

                return;
            }

            if (Game.GameTime -
                lastArmedWarningMessageTime >=
                1500)
            {
                lastArmedWarningMessageTime =
                    Game.GameTime;

                if (IsMeleeWeapon(
                    currentWeapon))
                {
                    GTA.UI.Screen.ShowSubtitle(
                        "~y~DROP THE WEAPON AND PUT YOUR HANDS UP",
                        1300
                    );
                }
                else
                {
                    GTA.UI.Screen.ShowSubtitle(
                        "~r~HOLSTER THE GUN AND PUT YOUR HANDS UP",
                        1300
                    );
                }
            }
        }

        //====================================================
        // CONTROL ARMED-WARNING OFFICERS
        //====================================================

        private void ControlArmedWarningOfficers()
        {
            Ped[] nearbyPeds =
                World.GetNearbyPeds(
                    player,
                    OfficerSearchDistance
                );

            if (nearbyPeds == null)
            {
                return;
            }

            foreach (Ped officer in nearbyPeds)
            {
                if (!IsValidPoliceOfficer(
                    officer))
                {
                    continue;
                }

                if (!surrenderOfficers.Contains(
                    officer))
                {
                    surrenderOfficers.Add(
                        officer
                    );

                    officer.Task
                        .ClearAllImmediately();
                }

                officer.BlockPermanentEvents =
                    true;

                Function.Call(
                    Hash.SET_PED_KEEP_TASK,
                    officer.Handle,
                    true
                );

                Function.Call(
                    Hash.SET_PED_CAN_SWITCH_WEAPON,
                    officer.Handle,
                    true
                );

                WeaponHash warningWeapon;

                int officerType =
                    Function.Call<int>(
                        Hash.GET_PED_TYPE,
                        officer.Handle
                    );

                bool tacticalOfficer =
                    officerType == 27;

                // Normal officers use tasers first.
                // Tactical officers keep rifles ready.
                if (!tacticalOfficer &&
                    arrestWantedLevel <= 3)
                {
                    warningWeapon =
                        WeaponHash.StunGun;
                }
                else
                {
                    warningWeapon =
                        WeaponHash.CarbineRifle;
                }

                if (!officer.Weapons.HasWeapon(
                    warningWeapon))
                {
                    officer.Weapons.Give(
                        warningWeapon,
                        warningWeapon ==
                        WeaponHash.StunGun
                        ? 10
                        : 120,
                        false,
                        true
                    );
                }

                officer.Weapons.Select(
                    warningWeapon
                );

                if (warningWeapon ==
                    WeaponHash.StunGun)
                {
                    Function.Call(
                        Hash.TASK_SHOOT_AT_ENTITY,
                        officer.Handle,
                        player.Handle,
                        1200,
                        unchecked(
                            (uint)0x5D60E4E0
                        )
                    );
                }
                else
                {
                    Function.Call(
                        Hash.TASK_AIM_GUN_AT_ENTITY,
                        officer.Handle,
                        player.Handle,
                        1000,
                        false
                    );
                }
            }
        }

        //====================================================
        // ARMED SUSPECT BECAME A THREAT
        //====================================================

        private void CancelArmedWarningForThreat()
        {
            RestoreSurrenderOfficers(
                true
            );

            arrestingOfficer =
                null;

            state =
                ArrestState.Idle;

            Notification.Show(
                "~r~LETHAL FORCE AUTHORIZED~n~~s~The suspect threatened police."
            );
        }

        //====================================================
        // CURRENT PLAYER WEAPON
        //====================================================

        private WeaponHash GetPlayerCurrentWeapon()
        {
            if (player == null ||
                !player.Exists() ||
                player.Weapons == null)
            {
                return WeaponHash.Unarmed;
            }

            Weapon currentWeapon =
                player.Weapons.Current;

            if (currentWeapon == null)
            {
                return WeaponHash.Unarmed;
            }

            return currentWeapon.Hash;
        }

        //====================================================
        // CHECK MELEE WEAPON
        //====================================================

        private bool IsMeleeWeapon(
            WeaponHash weaponHash)
        {
            if (weaponHash ==
                WeaponHash.Unarmed)
            {
                return false;
            }

            uint weaponGroup =
                Function.Call<uint>(
                    Hash.GET_WEAPONTYPE_GROUP,
                    (uint)weaponHash
                );

            uint meleeGroup =
                unchecked(
                    (uint)Game.GenerateHash(
                        "GROUP_MELEE"
                    )
                );

            return weaponGroup ==
                meleeGroup;
        }

        private void StartSurrender()
        {
            arrestWantedLevel =
            Game.Player.WantedLevel;

            if (arrestWantedLevel < 1)
            {
                arrestWantedLevel = 1;
            }

            if (arrestWantedLevel > 5)
            {
                arrestWantedLevel = 5;
            }

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

            // If another script or action equips a weapon
            // during surrender, police resume combat.
            WeaponHash currentWeapon =
                GetPlayerCurrentWeapon();

            if (currentWeapon !=
                WeaponHash.Unarmed)
            {
                CancelSurrenderForWeapon(
                    currentWeapon
                );

                return;
            }

            CalmNearbyPolice();

            // Pressing H again is an intentional
            // fake surrender.
            if (hJustPressed)
            {
                CancelSurrender();
                return;
            }

            // Give the player one second to release
            // movement keys after starting surrender.
            bool gracePeriodFinished =
                Game.GameTime -
                stateStartTime >= 1000;

            if (gracePeriodFinished &&
                IsUnarmedResistanceInput())
            {
                StartTaserResponse();
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

        //====================================================
        // CHECK UNARMED RESISTANCE
        //====================================================

        private bool IsUnarmedResistanceInput()
        {
            if (player == null ||
                !player.Exists())
            {
                return false;
            }

            if (GetPlayerCurrentWeapon() !=
                WeaponHash.Unarmed)
            {
                return false;
            }

            bool movementPressed =
                Game.IsKeyPressed(Keys.W) ||
                Game.IsKeyPressed(Keys.A) ||
                Game.IsKeyPressed(Keys.S) ||
                Game.IsKeyPressed(Keys.D);

            bool sprintPressed =
                Game.IsKeyPressed(
                    Keys.LShiftKey
                );

            bool jumpPressed =
                Game.IsKeyPressed(
                    Keys.Space
                );

            bool attackPressed =
                Game.IsKeyPressed(
                    Keys.LButton
                );

            bool aimPressed =
                Game.IsKeyPressed(
                    Keys.RButton
                );

            return movementPressed ||
                sprintPressed ||
                jumpPressed ||
                attackPressed ||
                aimPressed;
        }

        //====================================================
        // START TASER RESPONSE
        //====================================================

        private void StartTaserResponse()
        {
            state =
                ArrestState.TaserResponse;

            taserResponseStartTime =
                Game.GameTime;

            taserShotOrdered =
                false;

            player.Task.ClearAllImmediately();

            taserOfficer =
                arrestingOfficer;

            if (taserOfficer == null ||
                !taserOfficer.Exists() ||
                taserOfficer.IsDead)
            {
                taserOfficer =
                    FindNearestPoliceOfficer();
            }

            if (taserOfficer != null &&
                taserOfficer.Exists())
            {
                arrestingOfficer =
                    taserOfficer;

                if (!surrenderOfficers.Contains(
                    taserOfficer))
                {
                    surrenderOfficers.Add(
                        taserOfficer
                    );
                }

                taserOfficer.BlockPermanentEvents =
                    true;

                taserOfficer.Task
                    .ClearAllImmediately();

                Function.Call(
                    Hash.SET_PED_CAN_SWITCH_WEAPON,
                    taserOfficer.Handle,
                    true
                );

                if (!taserOfficer.Weapons.HasWeapon(
                    WeaponHash.StunGun))
                {
                    taserOfficer.Weapons.Give(
                        WeaponHash.StunGun,
                        10,
                        false,
                        true
                    );
                }

                taserOfficer.Weapons.Select(
                    WeaponHash.StunGun
                );
            }

            Notification.Show(
                "~r~STOP RESISTING!~n~~s~Police are deploying a taser."
            );
        }

        //====================================================
        // CHECK IF PLAYER WAS TASED OR KNOCKED DOWN
        //====================================================

        private bool IsPlayerDownFromTaser()
        {
            if (player == null ||
                !player.Exists())
            {
                return false;
            }

            bool beingStunned =
                Function.Call<bool>(
                    Hash.IS_PED_BEING_STUNNED,
                    player.Handle,
                    0
                );

            return beingStunned ||
                player.IsRagdoll ||
                player.IsGettingUp;
        }

        //====================================================
        // START ARREST AFTER SUCCESSFUL TASER
        //====================================================

        private void StartTasedArrest()
        {
            state =
                ArrestState.TasedArrest;

            tasedArrestStartTime =
                Game.GameTime;

            lastTasedOfficerCommandTime =
                0;

            Ped selectedOfficer =
                taserOfficer;

            if (selectedOfficer == null ||
                !selectedOfficer.Exists() ||
                selectedOfficer.IsDead)
            {
                selectedOfficer =
                    FindNearestPoliceOfficer();
            }

            arrestingOfficer =
                selectedOfficer;

            if (arrestingOfficer != null &&
                arrestingOfficer.Exists())
            {
                if (!surrenderOfficers.Contains(
                    arrestingOfficer))
                {
                    surrenderOfficers.Add(
                        arrestingOfficer
                    );
                }

                arrestingOfficer.BlockPermanentEvents =
                    true;

                arrestingOfficer.Task
                    .ClearAllImmediately();

                arrestingOfficer.Weapons.Select(
                    WeaponHash.Unarmed
                );
            }

            player.Weapons.Select(
                WeaponHash.Unarmed
            );

            Notification.Show(
                "~r~TASER DEPLOYED~n~~s~Officers are moving in to arrest you."
            );
        }

        //====================================================
        // UPDATE ARREST AFTER TASER
        //====================================================

        private void UpdateTasedArrest()
        {
            DisablePlayerActions();

            // Keep supporting officers covering the arrest
            // without allowing them to fire.
            HoldCoverOfficersDuringTaser();

            player.Weapons.Select(
                WeaponHash.Unarmed
            );

            if (arrestingOfficer == null ||
                !arrestingOfficer.Exists() ||
                arrestingOfficer.IsDead)
            {
                arrestingOfficer =
                    FindNearestPoliceOfficer();

                if (arrestingOfficer == null)
                {
                    GTA.UI.Screen.ShowSubtitle(
                        "WAITING FOR AN OFFICER",
                        1000
                    );

                    return;
                }
            }

            arrestingOfficer.BlockPermanentEvents =
                true;

            arrestingOfficer.Weapons.Select(
                WeaponHash.Unarmed
            );

            float distance =
                arrestingOfficer.Position.DistanceTo(
                    player.Position
                );

            if (distance <= HandcuffDistance)
            {
                StartHandcuffing();
                return;
            }

            if (Game.GameTime -
                lastTasedOfficerCommandTime >=
                750)
            {
                lastTasedOfficerCommandTime =
                    Game.GameTime;

                arrestingOfficer.Task
                    .ClearAllImmediately();

                arrestingOfficer.Weapons.Select(
                    WeaponHash.Unarmed
                );

                Function.Call(
                    Hash.TASK_GO_TO_ENTITY,
                    arrestingOfficer.Handle,
                    player.Handle,
                    -1,
                    1.15f,
                    1.2f,
                    1073741824,
                    0
                );
            }

            GTA.UI.Screen.ShowSubtitle(
                "~r~STAY DOWN~n~~s~YOU ARE UNDER ARREST",
                1000
            );

            // Safety fallback if the officer cannot reach
            // the player after ten seconds.
            if (Game.GameTime -
                tasedArrestStartTime >=
                10000)
            {
                StartHandcuffing();
            }
        }

        //====================================================
        // UPDATE TASER RESPONSE
        //====================================================

        private void UpdateTaserResponse()
        {
            DisablePlayerActions();

            HoldCoverOfficersDuringTaser();

            int elapsedTime =
                Game.GameTime -
                taserResponseStartTime;

            if (taserOfficer == null ||
                !taserOfficer.Exists() ||
                taserOfficer.IsDead)
            {
                taserOfficer =
                    FindNearestPoliceOfficer();

                if (taserOfficer != null &&
                    taserOfficer.Exists())
                {
                    arrestingOfficer =
                        taserOfficer;
                }
            }

            if (taserOfficer != null &&
                taserOfficer.Exists())
            {
                taserOfficer.BlockPermanentEvents =
                    true;

                Function.Call(
                    Hash.SET_PED_CAN_SWITCH_WEAPON,
                    taserOfficer.Handle,
                    true
                );

                if (!taserOfficer.Weapons.HasWeapon(
                    WeaponHash.StunGun))
                {
                    taserOfficer.Weapons.Give(
                        WeaponHash.StunGun,
                        10,
                        false,
                        true
                    );
                }

                taserOfficer.Weapons.Select(
                    WeaponHash.StunGun
                );

                if (!taserShotOrdered &&
                    elapsedTime >= 500)
                {
                    taserShotOrdered =
                        true;

                    Function.Call(
                        Hash.TASK_SHOOT_AT_ENTITY,
                        taserOfficer.Handle,
                        player.Handle,
                        1200,
                        unchecked(
                            (uint)0x5D60E4E0
                        )
                    );
                }
            }

            GTA.UI.Screen.ShowSubtitle(
                "~r~STOP RESISTING",
                1000
            );

            // As soon as the player is stunned or falls,
            // officers move in to arrest.
            if (IsPlayerDownFromTaser())
            {
                StartTasedArrest();
                return;
            }

            if (elapsedTime < 4000)
            {
                return;
            }

            // The taser may have missed.
            // Resume the normal pursuit.
            FinishTaserResponse();
        }

        //====================================================
        // FINISH TASER RESPONSE
        //====================================================

        private void FinishTaserResponse()
        {
            RestoreSurrenderOfficers(
                false
            );

            taserOfficer =
                null;

            arrestingOfficer =
                null;

            taserShotOrdered =
                false;

            state =
                ArrestState.Idle;

            Notification.Show(
                "~y~You were tased for resisting.~n~~s~Police pursuit remains active."
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
            if (arrestingOfficer == null ||
                !arrestingOfficer.Exists() ||
                arrestingOfficer.IsDead)
            {
                return;
            }

            if (Game.GameTime -
                lastOfficerCommandTime <
                1000)
            {
                return;
            }

            lastOfficerCommandTime =
                Game.GameTime;

            arrestingOfficer
                .BlockPermanentEvents =
                    true;

            Function.Call(
                Hash.SET_PED_KEEP_TASK,
                arrestingOfficer.Handle,
                true
            );

            ConfigureArrestingOfficer(
                arrestingOfficer
            );

            Function.Call(
                Hash.TASK_GO_TO_ENTITY,
                arrestingOfficer.Handle,
                player.Handle,
                -1,
                1.3f,
                1.4f,
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
                arrestingOfficer.Task
                    .ClearAllImmediately();

                arrestingOfficer.Weapons.Select(
                    WeaponHash.Unarmed
                );
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

            confiscationSystem.ConfiscatePlayer(
             arrestWantedLevel
                );

            RestoreSurrenderOfficers(
                false
                );

            arrestingOfficer = null;
            taserOfficer = null;
            taserShotOrdered = false;
            tasedArrestStartTime = 0;
            lastTasedOfficerCommandTime = 0;

            state = ArrestState.Idle;

            Function.Call(
                Hash.DO_SCREEN_FADE_IN,
                1500
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
            RestoreSurrenderOfficers(
            true
            );

            arrestingOfficer = null;
            state = ArrestState.Idle;

            Notification.Show(
                "Fake surrender! Police pursuit resumed."
            );
        }

        //====================================================
        // CANCEL SURRENDER: WEAPON EQUIPPED
        //====================================================

        private void CancelSurrenderForWeapon(
            WeaponHash weaponHash)
        {
            player.Task.ClearAllImmediately();

            Function.Call(
                Hash.SET_ENABLE_HANDCUFFS,
                player.Handle,
                false
            );

            RestoreSurrenderOfficers(
                true
            );

            arrestingOfficer =
                null;

            state =
                ArrestState.Idle;

            if (IsMeleeWeapon(
                weaponHash))
            {
                Notification.Show(
                    "~r~Surrender cancelled.~n~~s~You raised a melee weapon."
                );
            }
            else
            {
                Notification.Show(
                    "~r~Surrender cancelled.~n~~s~You equipped a firearm."
                );
            }
        }

        private Ped FindNearestPoliceOfficer()
        {
            Ped preferredOfficer = null;
            Ped fallbackOfficer = null;

            float preferredDistance =
                OfficerSearchDistance;

            float fallbackDistance =
                OfficerSearchDistance;

            Ped[] nearbyPeds =
                World.GetNearbyPeds(
                    player,
                    OfficerSearchDistance
                );

            if (nearbyPeds == null)
            {
                return null;
            }

            foreach (Ped ped in nearbyPeds)
            {
                if (!IsValidPoliceOfficer(ped))
                {
                    continue;
                }

                int pedType =
                    Function.Call<int>(
                        Hash.GET_PED_TYPE,
                        ped.Handle
                    );

                float distance =
                    ped.Position.DistanceTo(
                        player.Position
                    );

                bool tacticalOfficer =
                    pedType == 27;

                bool preferredType;

                if (arrestWantedLevel >= 4)
                {
                    // Prefer SWAT or tactical police.
                    preferredType =
                        tacticalOfficer;
                }
                else
                {
                    // Prefer normal patrol officers.
                    preferredType =
                        !tacticalOfficer;
                }

                if (preferredType &&
                    distance < preferredDistance)
                {
                    preferredDistance =
                        distance;

                    preferredOfficer =
                        ped;
                }
                else if (distance <
                    fallbackDistance)
                {
                    fallbackDistance =
                        distance;

                    fallbackOfficer =
                        ped;
                }
            }

            return preferredOfficer ??
                fallbackOfficer;
        }

        //====================================================
        // VALID POLICE OFFICER
        //====================================================

        private bool IsValidPoliceOfficer(
            Ped ped)
        {
            if (ped == null ||
                !ped.Exists() ||
                ped == player ||
                ped.IsDead)
            {
                return false;
            }

            int pedType =
                Function.Call<int>(
                    Hash.GET_PED_TYPE,
                    ped.Handle
                );

            return pedType == 6 ||
                pedType == 27;
        }

        //====================================================
        // CONTROL POLICE DURING SURRENDER
        //====================================================

        private void CalmNearbyPolice()
        {
            Ped[] nearbyPeds =
                World.GetNearbyPeds(
                    player,
                    OfficerSearchDistance
                );

            if (nearbyPeds == null)
            {
                return;
            }

            foreach (Ped officer in nearbyPeds)
            {
                if (!IsValidPoliceOfficer(
                    officer))
                {
                    continue;
                }

                bool newlyControlled =
                    !surrenderOfficers.Contains(
                        officer
                    );

                if (newlyControlled)
                {
                    surrenderOfficers.Add(
                        officer
                    );

                    officer.BlockPermanentEvents =
                        true;

                    officer.Task.ClearAllImmediately();

                    Function.Call(
                        Hash.SET_PED_KEEP_TASK,
                        officer.Handle,
                        true
                    );

                    Function.Call(
                        Hash.SET_PED_COMBAT_ATTRIBUTES,
                        officer.Handle,
                        46,
                        false
                    );

                    Function.Call(
                        Hash.SET_PED_CAN_SWITCH_WEAPON,
                        officer.Handle,
                        true
                    );
                }

                if (officer ==
                    arrestingOfficer)
                {
                    ConfigureArrestingOfficer(
                        officer
                    );
                }
                else
                {
                    ConfigureCoverOfficer(
                        officer
                    );
                }
            }
        }

        //====================================================
        // ARRESTING OFFICER ROLE
        //====================================================

        private void ConfigureArrestingOfficer(
            Ped officer)
        {
            if (officer == null ||
                !officer.Exists())
            {
                return;
            }

            WeaponHash arrestWeapon;

            if (arrestWantedLevel <= 3)
            {
                arrestWeapon =
                    WeaponHash.StunGun;
            }
            else
            {
                // Tactical arrests keep a firearm ready,
                // but the officer will not shoot while
                // the player is compliant.
                arrestWeapon =
                    WeaponHash.CarbineRifle;
            }

            if (!officer.Weapons.HasWeapon(
                arrestWeapon))
            {
                officer.Weapons.Give(
                    arrestWeapon,
                    arrestWeapon ==
                        WeaponHash.StunGun
                            ? 10
                            : 120,
                    false,
                    true
                );
            }

            officer.Weapons.Select(
                arrestWeapon
            );
        }

        //====================================================
        // COVER OFFICER ROLE
        //====================================================

        private void ConfigureCoverOfficer(
            Ped officer)
        {
            if (officer == null ||
                !officer.Exists())
            {
                return;
            }

            WeaponHash coverWeapon;

            if (arrestWantedLevel <= 2)
            {
                coverWeapon =
                    WeaponHash.Pistol;
            }
            else if (arrestWantedLevel == 3)
            {
                coverWeapon =
                    WeaponHash.CombatPistol;
            }
            else
            {
                coverWeapon =
                    WeaponHash.CarbineRifle;
            }

            if (!officer.Weapons.HasWeapon(
                coverWeapon))
            {
                officer.Weapons.Give(
                    coverWeapon,
                    coverWeapon ==
                        WeaponHash.CarbineRifle
                            ? 120
                            : 36,
                    false,
                    true
                );
            }

            officer.Weapons.Select(
                coverWeapon
            );

            // Aim at the surrendered suspect,
            // but do not use a combat task.
            Function.Call(
                Hash.TASK_AIM_GUN_AT_ENTITY,
                officer.Handle,
                player.Handle,
                1500,
                false
            );
        }

        //====================================================
        // HOLD COVER OFFICERS DURING TASER RESPONSE
        //====================================================

        //====================================================
        // HOLD ALL COVER OFFICERS DURING TASER RESPONSE
        //====================================================

        private void HoldCoverOfficersDuringTaser()
        {
            if (player == null ||
                !player.Exists())
            {
                return;
            }

            Ped[] nearbyPeds =
                World.GetNearbyPeds(
                    player,
                    OfficerSearchDistance
                );

            if (nearbyPeds == null)
            {
                return;
            }

            foreach (Ped officer in nearbyPeds)
            {
                if (!IsValidPoliceOfficer(
                    officer) ||
                    officer == taserOfficer)
                {
                    continue;
                }

                bool newlyControlled =
                    !surrenderOfficers.Contains(
                        officer
                    );

                if (newlyControlled)
                {
                    surrenderOfficers.Add(
                        officer
                    );

                    officer.Task
                        .ClearAllImmediately();
                }

                officer.BlockPermanentEvents =
                    true;

                Function.Call(
                    Hash.SET_PED_KEEP_TASK,
                    officer.Handle,
                    true
                );

                Function.Call(
                    Hash.SET_PED_COMBAT_ATTRIBUTES,
                    officer.Handle,
                    46,
                    false
                );

                WeaponHash coverWeapon;

                if (arrestWantedLevel <= 2)
                {
                    coverWeapon =
                        WeaponHash.Pistol;
                }
                else if (arrestWantedLevel == 3)
                {
                    coverWeapon =
                        WeaponHash.CombatPistol;
                }
                else
                {
                    coverWeapon =
                        WeaponHash.CarbineRifle;
                }

                if (!officer.Weapons.HasWeapon(
                    coverWeapon))
                {
                    officer.Weapons.Give(
                        coverWeapon,
                        coverWeapon ==
                            WeaponHash.CarbineRifle
                                ? 120
                                : 36,
                        false,
                        true
                    );
                }

                officer.Weapons.Select(
                    coverWeapon
                );

                // Prevent GTA AI from changing weapons.
                Function.Call(
                    Hash.SET_PED_CAN_SWITCH_WEAPON,
                    officer.Handle,
                    false
                );

                // Aim only. This is not a combat task.
                Function.Call(
                    Hash.TASK_AIM_GUN_AT_ENTITY,
                    officer.Handle,
                    player.Handle,
                    750,
                    false
                );
            }
        }

        //====================================================
        // RESTORE POLICE AFTER SURRENDER
        //====================================================

        private void RestoreSurrenderOfficers(
            bool attackPlayer)
        {
            foreach (Ped officer
                in surrenderOfficers)
            {
                if (officer == null ||
                    !officer.Exists() ||
                    officer.IsDead)
                {
                    continue;
                }

                officer.BlockPermanentEvents =
                    false;

                Function.Call(
                    Hash.SET_PED_CAN_SWITCH_WEAPON,
                    officer.Handle,
                    true
                );

                officer.Task.ClearAll();

                if (attackPlayer &&
                    player != null &&
                    player.Exists())
                {
                    Function.Call(
                        Hash.TASK_COMBAT_PED,
                        officer.Handle,
                        player.Handle,
                        0,
                        16
                    );
                }
            }

            surrenderOfficers.Clear();
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

            if (state == ArrestState.TaserResponse ||
                state == ArrestState.TasedArrest ||
                state == ArrestState.Handcuffing ||
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