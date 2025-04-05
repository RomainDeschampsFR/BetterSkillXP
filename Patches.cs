using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using MelonLoader;
using Il2CppTLD.Cooking;
using Il2CppTLD.Gear;
using System.Dynamic;
using Il2CppRewired.Libraries.SharpDX.RawInput;
using Il2CppSWS;

namespace BetterSkillXP
{
    class Patches
    {
        public static bool allowPointToBeEarned = false;

        public static float accumulatedMendingTime = 0;
        public static float mendingStartTime;
        public static float mendingEndTime;

        public static float accumulatedCookingTime = 0;

        public static float accumulatedStartingFireTime = 0;
        public static float startingFireStartTime;
        public static float startingFireEndTime;

        public static float accumulatedGunsmithingTime = 0;
        public static float gunsmithingStartTime;
        public static float gunsmithingEndTime;

        [HarmonyPatch(typeof(SkillsManager), nameof(SkillsManager.IncrementPointsAndNotify))]
        class SkillsManager_IncrementPointsAndNotify_BetterSkillXP
        {
        // ---------------------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------ SKIP VANILLA INCREMENT ---------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------

            static bool Prefix(SkillsManager __instance, SkillType skillType, int numPoints, SkillsManager.PointAssignmentMode mode)
            {

                // UNTOUCHED : Points earned through knowledge books won't be skipped by this prefix (Meaning books have to provide at least 2 points...)
                // Not compatible with SKILL MANAGER MOD
                if (numPoints == 1 && ((skillType == SkillType.ClothingRepair && Settings.settings.enableMending) || (skillType == SkillType.Cooking && Settings.settings.enableCooking) || (skillType == SkillType.Firestarting && Settings.settings.enableFireStarting) || (skillType == SkillType.Gunsmithing && Settings.settings.enableGunsmithing)))
                {
                    if (allowPointToBeEarned)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }

            static void Postfix(SkillsManager __instance, SkillType skillType, int numPoints, SkillsManager.PointAssignmentMode mode)
            {
                allowPointToBeEarned = false;
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------
        // --------------------------------------------------- MENDING PATCHES -------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------

        [HarmonyPatch(typeof(Panel_Inventory_Examine), nameof(Panel_Inventory_Examine.StartRepair))]
        class Panel_Inventory_Examine_StartRepair_MENDING
        {
            static void Postfix(Panel_Inventory_Examine __instance, int durationMinutes, string repairAudio)
            {
                mendingStartTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            }
        }

        [HarmonyPatch(typeof(Panel_Inventory_Examine), nameof(Panel_Inventory_Examine.RepairFinished))]
        class Panel_Inventory_Examine_RepairFinished_MENDING
        {
            static void Postfix(Panel_Inventory_Examine __instance)
            {
                if (!Settings.settings.enableMending) return;

                if (__instance.RepairRequiresSewingKit())
                {
                    mendingEndTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                    accumulatedMendingTime += mendingEndTime - mendingStartTime;
                    accumulatedMendingTime = Main.MaybeIncrementPoints(accumulatedMendingTime, SkillType.ClothingRepair);
                    Main.SaveParameters();
                }
            }
        }

        [HarmonyPatch(typeof(Panel_Crafting), nameof(Panel_Crafting.CraftingStart))]
        class Panel_Crafting_CraftingStart_MENDING
        {
            static void Postfix(Panel_Crafting __instance)
            {
                mendingStartTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            }
        }

        [HarmonyPatch(typeof(Panel_Crafting), nameof(Panel_Crafting.CraftingEnd))]
        class Panel_Crafting_CraftingEnd_MENDING
        {
            static void Postfix(Panel_Crafting __instance)
            {
                if (!Settings.settings.enableMending) return;

                if (__instance.m_RequirementContainer.GetSelectedToolPrefab())
                {
                    if (__instance.m_RequirementContainer.GetSelectedToolPrefab().name == "GEAR_SewingKit" || __instance.m_RequirementContainer.GetSelectedToolPrefab().name == "GEAR_HookAndLine")
                    {
                        mendingEndTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                        accumulatedMendingTime += mendingEndTime - mendingStartTime;
                        accumulatedMendingTime = Main.MaybeIncrementPoints(accumulatedMendingTime, SkillType.ClothingRepair);
                        Main.SaveParameters();
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------
        // --------------------------------------------------- COOKING PATCHES -------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------

        [HarmonyPatch(typeof(CookingPotItem), nameof(CookingPotItem.PickUpCookedGearItem))]
        class CookingPotItem_PickUpCookedGearItem_COOKING
        {
            static void Postfix(CookingPotItem __instance, bool addToInventory)
            {
                if (!Settings.settings.enableCooking) return;

                if (__instance.GetCookingState() != CookingPotItem.CookingState.Ready) return;
                GearItem gi = __instance.m_GearItemBeingCooked.m_Cookable.m_CookedPrefab;
                if (gi == null) return;

                float cookingTime = __instance.ModifiedCookTimeMinutes()/60;

                RecipeData recipe = GameObject.Find("SCRIPT_PlayerSystems").GetComponent<RecipeBook>().FindUnlockedRecipeFromCookedItem(gi);
                int recipeLvl = 0;
                if (recipe != null)
                {
                    recipeLvl = recipe.RequiredSkillLevel;
                    cookingTime += recipe.m_DishBlueprint.m_DurationMinutes/60;
                    cookingTime *= (1 + ((Settings.settings.cookingBonus / 100) * recipeLvl));
                    MelonLogger.Msg(recipeLvl);
                }


                if (__instance.m_GearItem.name == "GEAR_CookingPotDummy" || __instance.m_GearItem.name == "GEAR_RecycledCan") cookingTime *= (1 - (Settings.settings.cookingPenalty / 100));

                accumulatedCookingTime += cookingTime;
                accumulatedCookingTime = Main.MaybeIncrementPoints(accumulatedCookingTime, SkillType.Cooking);
                Main.SaveParameters();
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------ FIRE STARTING PATCHES ----------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------

        [HarmonyPatch(typeof(Panel_FireStart), nameof(Panel_FireStart.OnStartFire))]
        class Panel_FireStart_OnStartFire_FIRE
        {
            static void Postfix(Panel_FireStart __instance)
            {
                startingFireStartTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            }
        }

        [HarmonyPatch(typeof(Panel_FireStart), nameof(Panel_FireStart.OnDoneStartingFire))]
        class Panel_FireStart_OnDoneStartingFire_FIRE
        {
            static void Postfix(Panel_FireStart __instance)
            {
                if (!Settings.settings.enableFireStarting) return;

                startingFireEndTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                accumulatedStartingFireTime += startingFireEndTime - startingFireStartTime;
                MelonLogger.Msg(startingFireEndTime - startingFireStartTime);
                accumulatedStartingFireTime = Main.MaybeIncrementPoints(accumulatedStartingFireTime, SkillType.Firestarting);
                Main.SaveParameters();
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------- GUNSMITHING PATCHES -----------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------

        [HarmonyPatch(typeof(Panel_Inventory_Examine), nameof(Panel_Inventory_Examine.StartRepair))]
        class Panel_Inventory_Examine_StartRepair_GUNSMITHING
        {
            static void Postfix(Panel_Inventory_Examine __instance, int durationMinutes, string repairAudio)
            {
                gunsmithingStartTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            }
        }

        [HarmonyPatch(typeof(Panel_Inventory_Examine), nameof(Panel_Inventory_Examine.RepairFinished))]
        class Panel_Inventory_Examine_RepairFinished_GUNSMITHING
        {
            static void Postfix(Panel_Inventory_Examine __instance)
            {
                if (!Settings.settings.enableGunsmithing) return;

                if (__instance.GearItem.m_GunItem)
                {
                    gunsmithingEndTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                    accumulatedGunsmithingTime += (gunsmithingEndTime - gunsmithingStartTime) * (1 + (Settings.settings.gunsmithingRepairBonus / 100));
                    accumulatedGunsmithingTime = Main.MaybeIncrementPoints(accumulatedGunsmithingTime, SkillType.Gunsmithing);
                    Main.SaveParameters();
                }
            }
        }

        [HarmonyPatch(typeof(Panel_Inventory_Examine), nameof(Panel_Inventory_Examine.StartHarvest))]
        class Panel_Inventory_Examine_StartHarvest_GUNSMITHING
        {
            static void Postfix(Panel_Inventory_Examine __instance)
            {
                gunsmithingStartTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            }
        }

        [HarmonyPatch(typeof(Panel_Inventory_Examine), nameof(Panel_Inventory_Examine.HarvestSuccessful))]
        class Panel_Inventory_Examine_HarvestSuccessful_GUNSMITHING
        {
            static void Postfix(Panel_Inventory_Examine __instance)
            {
                if (!Settings.settings.enableGunsmithing) return;
                
                foreach (var item in __instance.GearItem.m_Harvest.m_YieldPowder)
                {
                    if (item.name.ToLowerInvariant().Contains("gunpowder"))
                    {
                        gunsmithingEndTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                        accumulatedGunsmithingTime += (gunsmithingEndTime - gunsmithingStartTime) * (1 - Settings.settings.gunsmithingPenalty);
                        accumulatedGunsmithingTime = Main.MaybeIncrementPoints(accumulatedGunsmithingTime, SkillType.Gunsmithing);
                        Main.SaveParameters();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Panel_Crafting), nameof(Panel_Crafting.CraftingStart))]
        class Panel_Crafting_CraftingStart_GUNSMITHING
        {
            static void Postfix(Panel_Crafting __instance)
            {
                gunsmithingStartTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            }
        }

        [HarmonyPatch(typeof(Panel_Crafting), nameof(Panel_Crafting.CraftingEnd))]
        class Panel_Crafting_CraftingEnd_GUNSMITHING
        {
            static void Postfix(Panel_Crafting __instance)
            {
                if (!Settings.settings.enableGunsmithing) return;

                if (__instance.SelectedBPI.m_CraftedResultGear != null && (__instance.SelectedBPI.m_CraftedResultGear.name.ToLowerInvariant().Contains("bullet") || __instance.SelectedBPI.m_CraftedResultGear.name.ToLowerInvariant().Contains("gunpowder")))
                {
                    gunsmithingEndTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                    accumulatedGunsmithingTime += (gunsmithingEndTime - gunsmithingStartTime) * (1 + (Settings.settings.gunsmithingCraftBonus / 100));
                    accumulatedGunsmithingTime = Main.MaybeIncrementPoints(accumulatedGunsmithingTime, SkillType.Gunsmithing);
                    Main.SaveParameters();
                }
                else
                {
                    foreach (var item in __instance.SelectedBPI.m_RequiredPowder)
                    {
                        if (item.m_Powder.name.ToLowerInvariant().Contains("gunpowder"))
                        {
                            gunsmithingEndTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                            accumulatedGunsmithingTime += (gunsmithingEndTime - gunsmithingStartTime) * (1 + (Settings.settings.gunsmithingCraftBonus / 100));
                            accumulatedGunsmithingTime = Main.MaybeIncrementPoints(accumulatedGunsmithingTime, SkillType.Gunsmithing);
                            Main.SaveParameters();
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Panel_Milling), nameof(Panel_Milling.BeginRepair))]
        class Panel_Milling_BeginRepair_GUNSMITHING
        {
            static void Postfix(Panel_Milling __instance)
            {
                gunsmithingStartTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            }
        }

        [HarmonyPatch(typeof(Panel_Milling), nameof(Panel_Milling.EndRepair))]
        class Panel_Milling_EndRepair_GUNSMITHING
        {
            static void Postfix(Panel_Milling __instance)
            {
                if (!Settings.settings.enableGunsmithing) return;

                if (__instance.GetSelected() != null && __instance.GetSelected().m_GunItem != null)
                {
                    gunsmithingEndTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                    accumulatedGunsmithingTime += (gunsmithingEndTime - gunsmithingStartTime) * (1 + (Settings.settings.gunsmithingRepairBonus / 100));
                    accumulatedGunsmithingTime = Main.MaybeIncrementPoints(accumulatedGunsmithingTime, SkillType.Gunsmithing);
                    Main.SaveParameters();
                }
            }
        }
    }
}

