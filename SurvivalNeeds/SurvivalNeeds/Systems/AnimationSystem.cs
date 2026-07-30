using GTA;
using GTA.Math;
using GTA.Native;
using SurvivalNeeds.Assets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace SurvivalNeeds.Systems
{
    public class AnimationSystem
    {
        private const int LeftHandBoneId = 18905;
        private const int RightHandBoneId = 57005;

        private readonly string settingsPath =
            Path.Combine(
                "scripts",
                "SurvivalNeeds",
                "PropAlignment.ini"
            );

        private readonly Dictionary<string, AlignmentData> alignments =
            new Dictionary<string, AlignmentData>();

        private readonly Dictionary<Keys, bool> previousKeyStates =
            new Dictionary<Keys, bool>();

        private readonly float[] positionSteps =
        {
            0.001f,
            0.005f,
            0.010f
        };

        private readonly float[] rotationSteps =
        {
            1.0f,
            5.0f,
            10.0f
        };

        private bool isPlaying;
        private bool editorOpen;

        private int finishTime;
        private int precisionIndex = 1;

        private Action finishedCallback;

        private string loadedDictionary;
        private string playingAnimation;

        private string currentItemId;
        private string currentPropName;

        private int currentBoneId;

        private Vector3 currentPosition;
        private Vector3 currentRotation;

        private Prop heldProp;

        public bool IsPlaying
        {
            get
            {
                return isPlaying;
            }
        }

        public bool IsEditorOpen
        {
            get
            {
                return editorOpen;
            }
        }

        public AnimationSystem()
        {
            LoadAllAlignments();
        }

        //====================================================
        // UPDATE
        //====================================================

        public void Update()
        {
            UpdateEditorToggle();

            if (editorOpen)
            {
                UpdateEditor();
                DrawEditor();

                // Keep the item active while editing.
                finishTime = Game.GameTime + 1000;
            }

            if (!isPlaying)
                return;

            Ped player = Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                Cancel();
                return;
            }

            if (editorOpen)
                return;

            if (Game.GameTime < finishTime)
                return;

            Action callback = finishedCallback;

            StopCurrentAnimation();
            ClearState();

            try
            {
                callback?.Invoke();
            }
            catch
            {
            }
        }

        //====================================================
        // EATING
        //====================================================

        public bool PlayEating(
            string itemId,
            Action onFinished = null)
        {
            if (isPlaying)
                return false;

            Ped player = GetPlayer();

            if (player == null)
                return false;

            string normalizedId =
                NormalizeItemId(itemId);

            string propName =
                GetFoodProp(normalizedId);

            Vector3 defaultPosition =
                GetDefaultPosition(normalizedId);

            Vector3 defaultRotation =
                GetDefaultRotation(normalizedId);

            AlignmentData alignment =
                GetAlignment(
                    normalizedId,
                    defaultPosition,
                    defaultRotation
                );

            int duration =
                GetFoodDuration(normalizedId);

            bool started = StartAnimation(
                player,
                "mp_player_inteat@burger",
                "mp_player_int_eat_burger",
                duration,
                onFinished
            );

            if (!started)
                return false;

            SetCurrentItem(
                normalizedId,
                propName,
                LeftHandBoneId,
                alignment.Position,
                alignment.Rotation
            );

            CreateAndAttachProp(
                player,
                propName,
                LeftHandBoneId,
                currentPosition,
                currentRotation
            );

            return true;
        }

        //====================================================
        // DRINKING
        //====================================================

        public bool PlayDrinking(
            string itemId,
            Action onFinished = null)
        {
            if (isPlaying)
                return false;

            Ped player = GetPlayer();

            if (player == null)
                return false;

            string normalizedId =
                NormalizeItemId(itemId);

            string propName =
                GetDrinkProp(normalizedId);

            Vector3 defaultPosition =
                GetDefaultPosition(normalizedId);

            Vector3 defaultRotation =
                GetDefaultRotation(normalizedId);

            AlignmentData alignment =
                GetAlignment(
                    normalizedId,
                    defaultPosition,
                    defaultRotation
                );

            int duration =
                GetDrinkDuration(normalizedId);

            bool started = StartAnimation(
                player,
                "mp_player_intdrink",
                "loop_bottle",
                duration,
                onFinished
            );

            if (!started)
                return false;

            SetCurrentItem(
                normalizedId,
                propName,
                LeftHandBoneId,
                alignment.Position,
                alignment.Rotation
            );

            CreateAndAttachProp(
                player,
                propName,
                LeftHandBoneId,
                currentPosition,
                currentRotation
            );

            return true;
        }

        //====================================================
        // SMOKING
        //====================================================

        public bool PlaySmoking(
    string itemId,
    Action onFinished = null)
        {
            if (isPlaying)
                return false;

            Ped player = GetPlayer();

            if (player == null)
                return false;

            string normalizedId =
                NormalizeItemId(itemId);

            string propName =
                GetSmokingProp(normalizedId);

            Vector3 defaultPosition =
                GetDefaultPosition(normalizedId);

            Vector3 defaultRotation =
                GetDefaultRotation(normalizedId);

            AlignmentData alignment =
                GetAlignment(
                    normalizedId,
                    defaultPosition,
                    defaultRotation
                );

            int duration =
                GetSmokingDuration(normalizedId);

            bool started = StartAnimation(
                player,
                "amb@world_human_smoking@male@male_a@base",
                "base",
                duration,
                onFinished
            );

            if (!started)
                return false;

            SetCurrentItem(
                normalizedId,
                propName,
                RightHandBoneId,
                alignment.Position,
                alignment.Rotation
            );

            CreateAndAttachProp(
                player,
                propName,
                RightHandBoneId,
                currentPosition,
                currentRotation
            );

            return true;
        }

        //====================================================
        // SET CURRENT ITEM
        //====================================================

        private void SetCurrentItem(
            string itemId,
            string propName,
            int boneId,
            Vector3 position,
            Vector3 rotation)
        {
            currentItemId = itemId;
            currentPropName = propName;
            currentBoneId = boneId;
            currentPosition = position;
            currentRotation = rotation;
        }

        //====================================================
        // START ANIMATION
        //====================================================

        private bool StartAnimation(
            Ped player,
            string dictionary,
            string animation,
            int duration,
            Action onFinished)
        {
            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return false;
            }

            if (!LoadAnimationDictionary(dictionary))
                return false;

            DeleteHeldProp();

            loadedDictionary = dictionary;
            playingAnimation = animation;

            finishedCallback = onFinished;
            finishTime = Game.GameTime + duration;

            isPlaying = true;
            editorOpen = false;

            PlayCurrentAnimation(player, duration);

            return true;
        }

        //====================================================
        // PLAY CURRENT ANIMATION
        //====================================================

        private void PlayCurrentAnimation(
            Ped player,
            int duration)
        {
            if (player == null ||
                !player.Exists() ||
                string.IsNullOrWhiteSpace(loadedDictionary) ||
                string.IsNullOrWhiteSpace(playingAnimation))
            {
                return;
            }

            Function.Call(
                Hash.TASK_PLAY_ANIM,
                player.Handle,
                loadedDictionary,
                playingAnimation,
                4.0f,
                -4.0f,
                duration,
                49,
                0.0f,
                false,
                false,
                false
            );
        }

        //====================================================
        // EDITOR TOGGLE
        //====================================================

        private void UpdateEditorToggle()
        {
            if (WasKeyPressed(Keys.F6))
            {
                if (editorOpen)
                {
                    CloseEditor();
                }
                else
                {
                    OpenEditor();
                }
            }

            if (editorOpen &&
                WasKeyPressed(Keys.Escape))
            {
                CloseEditor();
            }
        }

        //====================================================
        // OPEN EDITOR
        //====================================================

        private void OpenEditor()
        {
            if (!isPlaying ||
                heldProp == null ||
                !heldProp.Exists() ||
                string.IsNullOrWhiteSpace(currentItemId))
            {
                GTA.UI.Screen.ShowSubtitle(
                    "Use an item first, then press F6.",
                    2500
                );

                return;
            }

            editorOpen = true;

            // Keep the current animation alive without restarting it.
            finishTime = Game.GameTime + 600000;

            GTA.UI.Notification.Show(
                "Alignment editor opened for: " +
                currentItemId
            );
        }

        //====================================================
        // CLOSE EDITOR
        //====================================================

        private void CloseEditor()
        {
            if (!editorOpen)
                return;

            editorOpen = false;

            // Finish shortly after closing the editor.
            finishTime = Game.GameTime + 1000;
        }

        //====================================================
        // UPDATE EDITOR
        //====================================================

        private void UpdateEditor()
        {
            float positionStep =
                positionSteps[precisionIndex];

            float rotationStep =
                rotationSteps[precisionIndex];

            bool changed = false;

            // Position X
            if (WasKeyPressed(Keys.D1))
            {
                currentPosition.X -= positionStep;
                changed = true;
            }

            if (WasKeyPressed(Keys.D2))
            {
                currentPosition.X += positionStep;
                changed = true;
            }

            // Position Y
            if (WasKeyPressed(Keys.D3))
            {
                currentPosition.Y -= positionStep;
                changed = true;
            }

            if (WasKeyPressed(Keys.D4))
            {
                currentPosition.Y += positionStep;
                changed = true;
            }

            // Position Z
            if (WasKeyPressed(Keys.D5))
            {
                currentPosition.Z -= positionStep;
                changed = true;
            }

            if (WasKeyPressed(Keys.D6))
            {
                currentPosition.Z += positionStep;
                changed = true;
            }

            // Rotation X
            if (WasKeyPressed(Keys.Q))
            {
                currentRotation.X -= rotationStep;
                changed = true;
            }

            if (WasKeyPressed(Keys.E))
            {
                currentRotation.X += rotationStep;
                changed = true;
            }

            // Rotation Y
            if (WasKeyPressed(Keys.A))
            {
                currentRotation.Y -= rotationStep;
                changed = true;
            }

            if (WasKeyPressed(Keys.D))
            {
                currentRotation.Y += rotationStep;
                changed = true;
            }

            // Rotation Z
            if (WasKeyPressed(Keys.Z))
            {
                currentRotation.Z -= rotationStep;
                changed = true;
            }

            if (WasKeyPressed(Keys.C))
            {
                currentRotation.Z += rotationStep;
                changed = true;
            }

            // Increase precision step
            if (WasKeyPressed(Keys.PageUp))
            {
                precisionIndex++;

                if (precisionIndex >= positionSteps.Length)
                {
                    precisionIndex =
                        positionSteps.Length - 1;
                }
            }

            // Decrease precision step
            if (WasKeyPressed(Keys.PageDown))
            {
                precisionIndex--;

                if (precisionIndex < 0)
                {
                    precisionIndex = 0;
                }
            }

            // Save current item
            if (WasKeyPressed(Keys.Return))
            {
                SaveCurrentAlignment();

                GTA.UI.Screen.ShowSubtitle(
                    "~g~Alignment saved for " +
                    currentItemId,
                    1500
                );
            }

            // Reset current item
            if (WasKeyPressed(Keys.Back))
            {
                ResetCurrentAlignment();
                changed = true;

                GTA.UI.Screen.ShowSubtitle(
                    "~y~Alignment reset for " +
                    currentItemId,
                    1500
                );
            }

            if (changed)
            {
                UpdateAlignmentInMemory();
                ReattachHeldProp();
            }
        }

        //====================================================
        // DRAW EDITOR
        //====================================================

        private void DrawEditor()
        {
            string text =
                "~b~PROP ALIGNMENT EDITOR~s~" +
                " | Item: ~y~" + currentItemId + "~s~" +

                "~n~Position" +
                " X:" + currentPosition.X.ToString("0.000") +
                " Y:" + currentPosition.Y.ToString("0.000") +
                " Z:" + currentPosition.Z.ToString("0.000") +

                "~n~Rotation" +
                " X:" + currentRotation.X.ToString("0.0") +
                " Y:" + currentRotation.Y.ToString("0.0") +
                " Z:" + currentRotation.Z.ToString("0.0") +

                "~n~1/2 Pos X | 3/4 Pos Y | 5/6 Pos Z" +
                "~n~Q/E Rot X | A/D Rot Y | Z/C Rot Z" +
                "~n~Page Up/Down: Step | Enter: Save" +
                "~n~Backspace: Reset | F6: Close" +

                "~n~Position Step: " +
                positionSteps[precisionIndex].ToString("0.000") +

                " | Rotation Step: " +
                rotationSteps[precisionIndex].ToString("0.0");

            GTA.UI.Screen.ShowSubtitle(
                text,
                1000
            );
        }

        //====================================================
        // REATTACH HELD PROP
        //====================================================

        private void ReattachHeldProp()
        {
            Ped player = Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                heldProp == null ||
                !heldProp.Exists())
            {
                return;
            }

            try
            {
                heldProp.Detach(
                    false,
                    false
                );
            }
            catch
            {
            }

            AttachHeldProp(
                player,
                currentBoneId,
                currentPosition,
                currentRotation
            );
        }

        //====================================================
        // CANCEL
        //====================================================

        public void Cancel()
        {
            if (!isPlaying &&
                heldProp == null)
            {
                return;
            }

            editorOpen = false;

            StopCurrentAnimation();
            ClearState();
        }

        //====================================================
        // STOP CURRENT ANIMATION
        //====================================================

        private void StopCurrentAnimation()
        {
            Ped player = Game.Player.Character;

            if (player != null &&
                player.Exists())
            {
                if (!string.IsNullOrWhiteSpace(
                    loadedDictionary) &&
                    !string.IsNullOrWhiteSpace(
                    playingAnimation))
                {
                    try
                    {
                        Function.Call(
                            Hash.STOP_ANIM_TASK,
                            player.Handle,
                            loadedDictionary,
                            playingAnimation,
                            -4.0f
                        );
                    }
                    catch
                    {
                    }
                }

                try
                {
                    Function.Call(
                        Hash.CLEAR_PED_SECONDARY_TASK,
                        player.Handle
                    );
                }
                catch
                {
                }
            }

            DeleteHeldProp();

            if (!string.IsNullOrWhiteSpace(
                loadedDictionary))
            {
                try
                {
                    Function.Call(
                        Hash.REMOVE_ANIM_DICT,
                        loadedDictionary
                    );
                }
                catch
                {
                }
            }
        }

        //====================================================
        // CLEAR STATE
        //====================================================

        private void ClearState()
        {
            isPlaying = false;
            editorOpen = false;

            finishTime = 0;

            finishedCallback = null;

            loadedDictionary = null;
            playingAnimation = null;

            currentItemId = null;
            currentPropName = null;
            currentBoneId = 0;

            currentPosition = Vector3.Zero;
            currentRotation = Vector3.Zero;
        }

        //====================================================
        // PLAYER
        //====================================================

        private Ped GetPlayer()
        {
            Ped player = Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead ||
                player.IsInVehicle())
            {
                return null;
            }

            return player;
        }

        //====================================================
        // LOAD ANIMATION DICTIONARY
        //====================================================

        private bool LoadAnimationDictionary(
            string dictionary)
        {
            if (string.IsNullOrWhiteSpace(dictionary))
                return false;

            Function.Call(
                Hash.REQUEST_ANIM_DICT,
                dictionary
            );

            int timeout =
                Game.GameTime + 3000;

            while (!Function.Call<bool>(
                Hash.HAS_ANIM_DICT_LOADED,
                dictionary))
            {
                Script.Yield();

                if (Game.GameTime >= timeout)
                    return false;
            }

            return true;
        }

        //====================================================
        // CREATE AND ATTACH PROP
        //====================================================

        private void CreateAndAttachProp(
            Ped player,
            string propName,
            int boneId,
            Vector3 position,
            Vector3 rotation)
        {
            DeleteHeldProp();

            if (player == null ||
                !player.Exists() ||
                string.IsNullOrWhiteSpace(propName))
            {
                return;
            }

            Model model = new Model(propName);

            try
            {
                if (!model.IsInCdImage ||
                    !model.IsValid)
                {
                    return;
                }

                if (!model.Request(3000))
                    return;

                heldProp = World.CreateProp(
                    model,
                    player.Position,
                    false,
                    false
                );

                if (heldProp == null ||
                    !heldProp.Exists())
                {
                    heldProp = null;
                    return;
                }

                heldProp.IsPersistent = true;
                heldProp.IsCollisionEnabled = false;

                AttachHeldProp(
                    player,
                    boneId,
                    position,
                    rotation
                );
            }
            catch
            {
                DeleteHeldProp();
            }
            finally
            {
                try
                {
                    model.MarkAsNoLongerNeeded();
                }
                catch
                {
                }
            }
        }

        //====================================================
        // ATTACH HELD PROP
        //====================================================

        private void AttachHeldProp(
            Ped player,
            int boneId,
            Vector3 position,
            Vector3 rotation)
        {
            if (player == null ||
                !player.Exists() ||
                heldProp == null ||
                !heldProp.Exists())
            {
                return;
            }

            int boneIndex =
                Function.Call<int>(
                    Hash.GET_PED_BONE_INDEX,
                    player.Handle,
                    boneId
                );

            Function.Call(
                Hash.ATTACH_ENTITY_TO_ENTITY,
                heldProp.Handle,
                player.Handle,
                boneIndex,
                position.X,
                position.Y,
                position.Z,
                rotation.X,
                rotation.Y,
                rotation.Z,
                false,
                false,
                false,
                false,
                2,
                true
            );
        }

        //====================================================
        // DELETE PROP
        //====================================================

        private void DeleteHeldProp()
        {
            if (heldProp == null)
                return;

            try
            {
                if (heldProp.Exists())
                {
                    try
                    {
                        heldProp.Detach(
                            false,
                            false
                        );
                    }
                    catch
                    {
                    }

                    try
                    {
                        heldProp.IsCollisionEnabled = false;
                    }
                    catch
                    {
                    }

                    try
                    {
                        heldProp.Delete();
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                heldProp = null;
            }
        }

        //====================================================
        // SETTINGS
        //====================================================

        private void LoadAllAlignments()
        {
            EnsureSettingsFolder();

            string[] itemIds =
{
                "burger",
                "hotdog",
                "chips",
                "cannedbeans",
                "chocolate",
                "water",
                "soda",
                "coffee",
                "energydrink",
                "cigarette",
                "cigar"
             };

            ScriptSettings settings =
                ScriptSettings.Load(settingsPath);

            foreach (string itemId in itemIds)
            {
                Vector3 defaultPosition =
                    GetDefaultPosition(itemId);

                Vector3 defaultRotation =
                    GetDefaultRotation(itemId);

                AlignmentData data =
                    new AlignmentData();

                data.Position = new Vector3(
                    settings.GetValue(
                        itemId,
                        "PositionX",
                        defaultPosition.X
                    ),
                    settings.GetValue(
                        itemId,
                        "PositionY",
                        defaultPosition.Y
                    ),
                    settings.GetValue(
                        itemId,
                        "PositionZ",
                        defaultPosition.Z
                    )
                );

                data.Rotation = new Vector3(
                    settings.GetValue(
                        itemId,
                        "RotationX",
                        defaultRotation.X
                    ),
                    settings.GetValue(
                        itemId,
                        "RotationY",
                        defaultRotation.Y
                    ),
                    settings.GetValue(
                        itemId,
                        "RotationZ",
                        defaultRotation.Z
                    )
                );

                alignments[itemId] = data;
            }
        }

        private AlignmentData GetAlignment(
            string itemId,
            Vector3 defaultPosition,
            Vector3 defaultRotation)
        {
            AlignmentData data;

            if (alignments.TryGetValue(
                itemId,
                out data))
            {
                return data;
            }

            data = new AlignmentData
            {
                Position = defaultPosition,
                Rotation = defaultRotation
            };

            alignments[itemId] = data;

            return data;
        }

        private void UpdateAlignmentInMemory()
        {
            if (string.IsNullOrWhiteSpace(currentItemId))
                return;

            AlignmentData data;

            if (!alignments.TryGetValue(
                currentItemId,
                out data))
            {
                data = new AlignmentData();
                alignments[currentItemId] = data;
            }

            data.Position = currentPosition;
            data.Rotation = currentRotation;
        }

        private void SaveCurrentAlignment()
        {
            if (string.IsNullOrWhiteSpace(currentItemId))
                return;

            UpdateAlignmentInMemory();
            EnsureSettingsFolder();

            ScriptSettings settings =
                ScriptSettings.Load(settingsPath);

            settings.SetValue(
                currentItemId,
                "PositionX",
                currentPosition.X
            );

            settings.SetValue(
                currentItemId,
                "PositionY",
                currentPosition.Y
            );

            settings.SetValue(
                currentItemId,
                "PositionZ",
                currentPosition.Z
            );

            settings.SetValue(
                currentItemId,
                "RotationX",
                currentRotation.X
            );

            settings.SetValue(
                currentItemId,
                "RotationY",
                currentRotation.Y
            );

            settings.SetValue(
                currentItemId,
                "RotationZ",
                currentRotation.Z
            );

            settings.Save();
        }

        private void ResetCurrentAlignment()
        {
            if (string.IsNullOrWhiteSpace(currentItemId))
                return;

            currentPosition =
                GetDefaultPosition(currentItemId);

            currentRotation =
                GetDefaultRotation(currentItemId);

            UpdateAlignmentInMemory();
        }

        private void EnsureSettingsFolder()
        {
            string directory =
                Path.GetDirectoryName(settingsPath);

            if (!string.IsNullOrWhiteSpace(directory) &&
                !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        //====================================================
        // DEFAULT POSITIONS
        //====================================================

        private Vector3 GetDefaultPosition(string itemId)
        {
            switch (itemId)
            {
                // ==========================
                // FOOD
                // ==========================

                case "burger":
                    return new Vector3(
                        -0.005f,
                        0.060f,
                        -0.005f
                    );

                case "hotdog":
                    return new Vector3(
                        0.045f,
                        0.015f,
                        0.005f
                    );

                case "chips":
                    return new Vector3(
                        0.020f,
                        0.040f,
                        -0.010f
                    );

                case "cannedbeans":
                    return new Vector3(
                        0.030f,
                        0.020f,
                        -0.020f
                    );

                case "chocolate":
                    return new Vector3(
                        0.025f,
                        0.035f,
                        -0.005f
                    );

                // ==========================
                // DRINKS
                // ==========================

                case "water":
                    return new Vector3(
                        -0.010f,
                        0.055f,
                        -0.015f
                    );

                case "soda":
                    return new Vector3(
                        0.035f,
                        0.005f,
                        -0.015f
                    );

                case "coffee":
                    return new Vector3(
                        0.040f,
                        0.015f,
                        -0.025f
                    );

                case "energydrink":
                    return new Vector3(
                        0.035f,
                        0.005f,
                        -0.015f
                    );

                // ==========================
                // SMOKING
                // ==========================

                case "cigar":
                    return new Vector3(
                        0.015f,
                        0.010f,
                        -0.005f
                    );

                case "cigarette":
                    return new Vector3(
                        0.015f,
                        0.005f,
                        -0.005f
                    );

                default:
                    return Vector3.Zero;
            }
        }

        //====================================================
        // DEFAULT ROTATIONS
        //====================================================

        private Vector3 GetDefaultRotation(string itemId)
        {
            switch (itemId)
            {
                // ==========================
                // FOOD
                // ==========================

                case "burger":
                    return new Vector3(
                        -95.0f,
                        5.0f,
                        -10.0f
                    );

                case "hotdog":
                    return new Vector3(
                        -75.0f,
                        5.0f,
                        90.0f
                    );

                case "chips":
                    return new Vector3(
                        -80.0f,
                        0.0f,
                        10.0f
                    );

                case "cannedbeans":
                    return new Vector3(
                        -90.0f,
                        5.0f,
                        5.0f
                    );

                case "chocolate":
                    return new Vector3(
                        -80.0f,
                        0.0f,
                        5.0f
                    );

                // ==========================
                // DRINKS
                // ==========================

                case "water":
                    return new Vector3(
                        -90.0f,
                        0.0f,
                        -5.0f
                    );

                case "soda":
                    return new Vector3(
                        -90.0f,
                        5.0f,
                        5.0f
                    );

                case "coffee":
                    return new Vector3(
                        -90.0f,
                        5.0f,
                        10.0f
                    );

                case "energydrink":
                    return new Vector3(
                        -90.0f,
                        5.0f,
                        5.0f
                    );

                // ==========================
                // SMOKING
                // ==========================

                case "cigar":
                    return new Vector3(
                        0.0f,
                        90.0f,
                        10.0f
                    );

                case "cigarette":
                    return new Vector3(
                        0.0f,
                        90.0f,
                        0.0f
                    );

                default:
                    return Vector3.Zero;
            }
        }

        //====================================================
        // FOOD PROPS
        //====================================================

        private string GetFoodProp(string itemId)
        {
            switch (itemId)
            {
                case "hotdog":
                    return PropLibrary.Hotdog;

                case "chips":
                    return PropLibrary.Chips;

                case "cannedbeans":
                    return PropLibrary.Burger;

                case "chocolate":
                    return PropLibrary.Chocolate;

                case "burger":
                default:
                    return PropLibrary.Burger;
            }
        }

        //====================================================
        // FOOD DURATION
        //====================================================

        private int GetFoodDuration(
            string itemId)
        {
            switch (itemId)
            {
                case "hotdog":
                    return 8000;

                case "burger":
                default:
                    return 10000;
            }
        }

        //====================================================
        // DRINK PROPS
        //====================================================

        private string GetDrinkProp(string itemId)
        {
            switch (itemId)
            {
                case "soda":
                    return PropLibrary.SodaCan;

                case "coffee":
                    return PropLibrary.CoffeeCup;

                case "energydrink":
                    return PropLibrary.EnergyDrink;

                case "water":
                default:
                    return PropLibrary.WaterBottle;
            }
        }

        //====================================================
        // DRINK DURATION
        //====================================================

        private int GetDrinkDuration(
            string itemId)
        {
            switch (itemId)
            {
                case "coffee":
                    return 7000;

                case "soda":
                    return 5000;

                case "water":
                default:
                    return 5000;
            }
        }

        //====================================================
        // SMOKING PROPS
        //====================================================

        private string GetSmokingProp(
            string itemId)
        {
            switch (itemId)
            {
                case "cigar":
                    return PropLibrary.Cigar;

                case "cigarette":
                default:
                    return PropLibrary.Cigarette;
            }
        }

        //====================================================
        // SMOKING DURATION
        //====================================================

        private int GetSmokingDuration(
            string itemId)
        {
            switch (itemId)
            {
                case "cigar":
                    return 60000;

                case "cigarette":
                default:
                    return 30000;
            }
        }

        //====================================================
        // KEY PRESS
        //====================================================

        private bool WasKeyPressed(
            Keys key)
        {
            bool currentlyPressed =
                Game.IsKeyPressed(key);

            bool previouslyPressed = false;

            previousKeyStates.TryGetValue(
                key,
                out previouslyPressed
            );

            previousKeyStates[key] =
                currentlyPressed;

            return currentlyPressed &&
                   !previouslyPressed;
        }

        //====================================================
        // NORMALIZE ITEM ID
        //====================================================

        private string NormalizeItemId(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return string.Empty;

            return itemId
                .Trim()
                .ToLowerInvariant();
        }

        //====================================================
        // ALIGNMENT DATA
        //====================================================

        private class AlignmentData
        {
            public Vector3 Position;
            public Vector3 Rotation;
        }
    }
}