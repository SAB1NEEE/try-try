using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using SurvivalNeeds.Inventory;
using SurvivalNeeds.Systems;
using System;
using System.Collections.Generic;

namespace SurvivalNeeds.Loot
{
    public class LootSystem
    {
        private readonly InventoryManager inventory;
        private readonly MoneySystem money;

        private bool searching = false;
        private int searchStartTime = 0;

        private const int SearchDuration = 3000;
        private const string SearchAnimDictionary =
            "amb@prop_human_bum_bin@base";

        private const string SearchAnimName =
            "base";
        private const float SearchDistance = 2.2f;

        private Prop currentProp;

        private readonly Random random =
            new Random();

        private bool ePressedLastFrame = false;

        private readonly List<Vector3> searchedTrashPositions =
            new List<Vector3>();

        private readonly HashSet<int> lootableModels =
            new HashSet<int>();

        public LootSystem(
            InventoryManager inventory,
            MoneySystem money)
        {
            this.inventory = inventory;
            this.money = money;

            RegisterLootableModels();
        }

        //====================================================
        // Register Trash Models
        //====================================================

        private void RegisterLootableModels()
        {
            AddLootableModel("prop_bin_01a");
            AddLootableModel("prop_bin_02a");
            AddLootableModel("prop_bin_03a");
            AddLootableModel("prop_bin_04a");
            AddLootableModel("prop_bin_05a");
            AddLootableModel("prop_bin_06a");
            AddLootableModel("prop_bin_07a");
            AddLootableModel("prop_bin_07b");
            AddLootableModel("prop_bin_07c");
            AddLootableModel("prop_bin_07d");
            AddLootableModel("prop_bin_08a");
            AddLootableModel("prop_bin_08open");
            AddLootableModel("prop_bin_09a");

            AddLootableModel("prop_bin_10a");
            AddLootableModel("prop_bin_10b");
            AddLootableModel("prop_bin_11a");
            AddLootableModel("prop_bin_11b");
            AddLootableModel("prop_bin_12a");
            AddLootableModel("prop_bin_13a");
            AddLootableModel("prop_bin_14a");
            AddLootableModel("prop_bin_14b");

            AddLootableModel("prop_bin_beach_01a");
            AddLootableModel("prop_bin_beach_01d");
            AddLootableModel("prop_bin_delpiero");

            AddLootableModel("prop_dumpster_01a");
            AddLootableModel("prop_dumpster_02a");
            AddLootableModel("prop_dumpster_02b");
            AddLootableModel("prop_dumpster_3a");
            AddLootableModel("prop_dumpster_3step");
            AddLootableModel("prop_dumpster_4a");
            AddLootableModel("prop_dumpster_4b");

            AddLootableModel("prop_skip_01a");
            AddLootableModel("prop_skip_02a");
            AddLootableModel("prop_skip_03");
            AddLootableModel("prop_skip_04");
            AddLootableModel("prop_skip_05a");
            AddLootableModel("prop_skip_05b");
            AddLootableModel("prop_skip_06a");
            AddLootableModel("prop_skip_08a");
            AddLootableModel("prop_skip_10a");
        }

        private void AddLootableModel(
            string modelName)
        {
            lootableModels.Add(
                Game.GenerateHash(modelName)
            );
        }

        //====================================================
        // Main Update
        //====================================================

        public void Update()
        {
            if (searching)
            {
                UpdateSearch();
                return;
            }

            Prop nearest =
                GetNearestTrash();

            if (nearest == null)
            {
                ePressedLastFrame = false;
                return;
            }

            if (HasTrashBeenSearched(nearest))
            {
                GTA.UI.Screen.ShowHelpTextThisFrame(
                    "~r~This trash has already been searched"
                );

                ePressedLastFrame =
                    Game.IsKeyPressed(
                        System.Windows.Forms.Keys.E
                    );

                return;
            }

            GTA.UI.Screen.ShowHelpTextThisFrame(
                "Press ~INPUT_CONTEXT~ to Search Trash"
            );

            bool ePressed =
                Game.IsKeyPressed(
                    System.Windows.Forms.Keys.E
                );

            if (ePressed &&
                !ePressedLastFrame)
            {
                StartSearch(nearest);
            }

            ePressedLastFrame = ePressed;
        }

        //====================================================
        // Start Search
        //====================================================

        private void StartSearch(
            Prop prop)
        {
            if (prop == null ||
                !prop.Exists())
            {
                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            searching = true;
            currentProp = prop;
            searchStartTime = Game.GameTime;

            Vector3 direction =
                prop.Position -
                player.Position;

            player.Heading =
                Function.Call<float>(
                    Hash.GET_HEADING_FROM_VECTOR_2D,
                    direction.X,
                    direction.Y
                );

            StartSearchAnimation();
        }

        //====================================================
        // Update Search
        //====================================================

        private void UpdateSearch()
        {
            if (currentProp == null ||
                !currentProp.Exists())
            {
                CancelSearch();
                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                CancelSearch();
                return;
            }

            DisableSearchControls();

            float playerDistance =
                player.Position.DistanceTo(
                    currentProp.Position
                );

            if (playerDistance >
                SearchDistance + 1.0f)
            {
                Notification.Show(
                    "~r~Search cancelled"
                );

                CancelSearch();
                return;
            }

            int elapsed =
                Game.GameTime -
                searchStartTime;

            float percent =
                (float)elapsed /
                SearchDuration;

            if (percent > 1.0f)
            {
                percent = 1.0f;
            }

            GTA.UI.Screen.ShowSubtitle(
                "Searching... " +
                (int)(percent * 100.0f) +
                "%"
            );

            if (elapsed >= SearchDuration)
            {
                Vector3 searchedPosition =
                    currentProp.Position;

                StopSearchAnimation();

                GiveLoot();

                AddSearchedTrashPosition(
                    searchedPosition
                );

                searching = false;
                currentProp = null;
                ePressedLastFrame = true;
            }
        }

        //====================================================
        // Cancel Search
        //====================================================

        private void CancelSearch()
        {
            StopSearchAnimation();

            searching = false;
            currentProp = null;
            ePressedLastFrame = true;
        }

        //====================================================
        // Disable Controls While Searching
        //====================================================

        private void DisableSearchControls()
        {
            Game.DisableControlThisFrame(
                GTA.Control.MoveUpOnly
            );

            Game.DisableControlThisFrame(
                GTA.Control.MoveDownOnly
            );

            Game.DisableControlThisFrame(
                GTA.Control.MoveLeftOnly
            );

            Game.DisableControlThisFrame(
                GTA.Control.MoveRightOnly
            );

            Game.DisableControlThisFrame(
                GTA.Control.Sprint
            );

            Game.DisableControlThisFrame(
                GTA.Control.Jump
            );

            Game.DisableControlThisFrame(
                GTA.Control.Attack
            );

            Game.DisableControlThisFrame(
                GTA.Control.Aim
            );

            Game.DisableControlThisFrame(
                GTA.Control.MeleeAttack1
            );

            Game.DisableControlThisFrame(
                GTA.Control.MeleeAttack2
            );

            Game.DisableControlThisFrame(
                GTA.Control.SelectWeapon
            );

            Game.DisableControlThisFrame(
                GTA.Control.Enter
            );
        }

        //====================================================
        // Give Loot
        //====================================================

        private void GiveLoot()
        {
            if (inventory == null)
            {
                Notification.Show(
                    "~r~Inventory system missing"
                );

                return;
            }

            int mainRoll =
                random.Next(100);

            // 5% chance to find money.
            if (mainRoll < 5)
            {
                GiveMoneyLoot();
                return;
            }

            // 30% chance to find nothing.
            if (mainRoll < 35)
            {
                Notification.Show(
                    "~r~The trash was empty."
                );

                return;
            }

            string[] commonLoot =
            {
                "water",
                "coffee",
                "chips",
                "soda",
                "plastic",
                "cloth",
                "glassbottle"
            };

            string[] uncommonLoot =
            {
                "hotdog",
                "burger",
                "cannedbeans",
                "chocolate",
                "bandage",
                "scrapmetal",
                "wires"
            };

            string[] rareLoot =
            {
                "painkillers",
                "electronics",
                "brokenphone",
                "watch",
                "oldwallet"
            };

            string[] epicLoot =
            {
                "ring",
                "necklace",
                "firstaidkit"
            };

            int lootCount =
                random.Next(1, 4);

            int foundCount = 0;

            for (int i = 0;
                i < lootCount;
                i++)
            {
                string itemId =
                    PickLootItem(
                        commonLoot,
                        uncommonLoot,
                        rareLoot,
                        epicLoot
                    );

                InventoryItem item =
                    ItemDatabase.GetItem(itemId);

                if (item == null)
                {
                    continue;
                }

                bool added =
                    inventory.AddItem(itemId);

                if (!added)
                {
                    Notification.Show(
                        "~r~Inventory Full"
                    );

                    break;
                }

                foundCount++;

                Notification.Show(
                    "~g~Found " +
                    item.Name
                );
            }

            if (foundCount <= 0)
            {
                Notification.Show(
                    "~r~Nothing useful found."
                );
            }
        }

        private void GiveMoneyLoot()
        {
            if (money == null)
            {
                Notification.Show(
                    "~r~Money system missing"
                );

                return;
            }

            int cashFound =
                random.Next(1, 11);

            money.AddMoney(cashFound);

            Notification.Show(
                "~g~Found $" +
                cashFound
            );
        }

        private string PickLootItem(
            string[] commonLoot,
            string[] uncommonLoot,
            string[] rareLoot,
            string[] epicLoot)
        {
            int rarityRoll =
                random.Next(100);

            if (rarityRoll < 60)
            {
                return commonLoot[
                    random.Next(commonLoot.Length)
                ];
            }

            if (rarityRoll < 85)
            {
                return uncommonLoot[
                    random.Next(uncommonLoot.Length)
                ];
            }

            if (rarityRoll < 97)
            {
                return rareLoot[
                    random.Next(rareLoot.Length)
                ];
            }

            return epicLoot[
                random.Next(epicLoot.Length)
            ];
        }

        //====================================================
        // Find Nearest Trash
        //====================================================

        private Prop GetNearestTrash()
        {
            Prop closest = null;

            float closestDistance =
                SearchDistance;

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return null;
            }

            foreach (Prop prop in
                World.GetAllProps())
            {
                if (prop == null ||
                    !prop.Exists())
                {
                    continue;
                }

                if (!lootableModels.Contains(
                    prop.Model.Hash))
                {
                    continue;
                }

                float distance =
                    player.Position.DistanceTo(
                        prop.Position
                    );

                if (distance <
                    closestDistance)
                {
                    closest = prop;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private bool LoadAnimationDictionary(
            string dictionary)
        {
            if (Function.Call<bool>(
                Hash.HAS_ANIM_DICT_LOADED,
                dictionary))
            {
                return true;
            }

            Function.Call(
                Hash.REQUEST_ANIM_DICT,
                dictionary
            );

            return Function.Call<bool>(
                Hash.HAS_ANIM_DICT_LOADED,
                dictionary
            );
        }

        private void StartSearchAnimation()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            if (!LoadAnimationDictionary(
                SearchAnimDictionary))
            {
                return;
            }

            player.Task.PlayAnimation(
                SearchAnimDictionary,
                SearchAnimName,
                8.0f,
                -1,
                AnimationFlags.Loop
            );
        }

        private void StopSearchAnimation()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            player.Task.ClearAnimation(
                SearchAnimDictionary,
                SearchAnimName
            );
        }

        //====================================================
        // Check If Trash Was Searched
        //====================================================

        private bool HasTrashBeenSearched(
            Prop prop)
        {
            if (prop == null ||
                !prop.Exists())
            {
                return false;
            }

            foreach (Vector3 searchedPosition in
                searchedTrashPositions)
            {
                if (prop.Position.DistanceTo(
                    searchedPosition) <= 0.75f)
                {
                    return true;
                }
            }

            return false;
        }

        //====================================================
        // Mark Trash As Searched
        //====================================================

        private void AddSearchedTrashPosition(
            Vector3 position)
        {
            foreach (Vector3 searchedPosition in
                searchedTrashPositions)
            {
                if (position.DistanceTo(
                    searchedPosition) <= 0.75f)
                {
                    return;
                }
            }

            searchedTrashPositions.Add(
                position
            );
        }
    }
}