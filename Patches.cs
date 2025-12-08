using System;
using HarmonyLib;
using BepInEx.Logging;

namespace GooseGameAP
{
    /// <summary>
    /// Harmony patches for intercepting game events
    /// </summary>
    
    [HarmonyPatch]
    public static class GoalPatches
    {
        [HarmonyPatch(typeof(Goal), "AwardGoalDuringGame")]
        [HarmonyPostfix]
        static void OnGoalAwarded(Goal __instance)
        {
            try
            {
                string goalName = __instance.goalInfo.goalName;
                if (!string.IsNullOrEmpty(goalName))
                {
                    Plugin.Log.LogInfo("Goal completed: " + goalName);
                    Plugin.Instance?.OnGoalCompleted(goalName);
                    
                    if (goalName == "goalFinale")
                    {
                        Plugin.Instance?.SendGoalComplete();
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Goal patch error: " + ex.Message);
            }
        }
    }

    [HarmonyPatch]
    public static class AreaPatches
    {
        [HarmonyPatch(typeof(AreaTracker), "ChangeTo")]
        [HarmonyPrefix]
        static bool OnAreaChange(GoalListArea newArea, AreaTracker __instance)
        {
            try
            {
                if (Plugin.Instance == null) return true;
                
                if (!Plugin.Instance.CanEnterArea(newArea))
                {
                    Plugin.Log.LogInfo("Blocked entry to: " + newArea);
                    Plugin.Instance.OnAreaBlocked(newArea);
                    return false;
                }
                
                Plugin.Log.LogInfo("Allowed entry to: " + newArea);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Area patch error: " + ex.Message);
                return true;
            }
        }
    }

    [HarmonyPatch]
    public static class GoosePatches
    {
        [HarmonyPatch(typeof(Goose), "Shoo", typeof(UnityEngine.GameObject))]
        [HarmonyPostfix]
        static void OnGooseShooed(Goose __instance, UnityEngine.GameObject shooer)
        {
            try
            {
                Plugin.Log.LogInfo("Goose was shooed by: " + shooer?.name);
                Plugin.Instance?.OnGooseShooed();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Shoo patch error: " + ex.Message);
            }
        }
    }

    [HarmonyPatch]
    public static class SwitchEventPatches
    {
        [HarmonyPatch(typeof(SwitchEventManager), "TriggerEvent")]
        [HarmonyPrefix]
        static bool OnTriggerEvent(string id)
        {
            try
            {
                if (Plugin.Instance == null) return true;
                
                // Reset dragger cache on area transitions (helps after resets)
                if (id.StartsWith("enterArea") || id.StartsWith("resetArea") || id.Contains("Reset"))
                {
                    Plugin.Log.LogInfo("[EVENT] Area event detected: " + id + " - resetting dragger cache");
                    Plugin.Instance.ItemTracker?.ResetDraggerCache();
                }
                
                switch (id)
                {
                    case "enterAreaHighstreet":
                        if (!Plugin.Instance.HasHighStreetAccess)
                        {
                            Plugin.Log.LogInfo("Blocked event: " + id);
                            Plugin.Instance.OnAreaBlocked(GoalListArea.Shops);
                            Plugin.Instance.GateManager?.TeleportGooseToWell();
                            return false;
                        }
                        break;
                    case "enterAreaBackyards":
                        if (!Plugin.Instance.HasBackGardensAccess)
                        {
                            Plugin.Log.LogInfo("Blocked event: " + id);
                            Plugin.Instance.OnAreaBlocked(GoalListArea.Backyards);
                            Plugin.Instance.GateManager?.TeleportGooseToWell();
                            return false;
                        }
                        break;
                    case "enterAreaPub":
                        if (!Plugin.Instance.HasPubAccess)
                        {
                            Plugin.Log.LogInfo("Blocked event: " + id);
                            Plugin.Instance.OnAreaBlocked(GoalListArea.Pub);
                            Plugin.Instance.GateManager?.TeleportGooseToWell();
                            return false;
                        }
                        break;
                    case "enterAreaFinale":
                        if (!Plugin.Instance.HasModelVillageAccess)
                        {
                            Plugin.Log.LogInfo("Blocked event: " + id);
                            Plugin.Instance.OnAreaBlocked(GoalListArea.Finale);
                            Plugin.Instance.GateManager?.TeleportGooseToWell();
                            return false;
                        }
                        break;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("SwitchEvent patch error: " + ex.Message);
                return true;
            }
        }
    }

    [HarmonyPatch]
    public static class MoverPatches
    {
        // Speed modification is handled in Plugin.Update() directly
        // Keeping this class for potential future use
    }
    
    // === BUTTERFINGERS PATCHES ===
    // These intercept pickup/drag attempts when butterfingers trap is active
    
    [HarmonyPatch]
    public static class HolderPatches
    {
        [HarmonyPatch(typeof(Holder), "Grab")]
        [HarmonyPrefix]
        static bool OnGrabPrefix(Holder __instance, Prop prop)
        {
            try
            {
                if (Plugin.Instance?.TrapManager?.HasButterfingers == true)
                {
                    Plugin.Log.LogInfo("[BUTTERFINGERS] Blocked pickup attempt!");
                    return false;
                }
            }
            catch { }
            return true;
        }
    }
    
    [HarmonyPatch]
    public static class DraggerPatches
    {
        [HarmonyPatch(typeof(Dragger), "Grab")]
        [HarmonyPrefix]
        static bool OnDraggerGrabPrefix()
        {
            try
            {
                if (Plugin.Instance?.TrapManager?.HasButterfingers == true)
                {
                    Plugin.Log.LogInfo("[BUTTERFINGERS] Blocked drag attempt!");
                    return false;
                }
            }
            catch { }
            return true;
        }
    }
}
