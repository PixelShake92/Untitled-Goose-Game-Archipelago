using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace GooseGameAP
{
    /// <summary>
    /// Manages gate states, area access, and teleportation
    /// </summary>
    public class GateManager
    {
        private static ManualLogSource Log => Plugin.Log;
        private Plugin plugin;
        
        public static readonly Vector3 WellPosition = new Vector3(-22f, 0.5f, 38f);
        
        private static readonly Dictionary<string, string[]> AreaGates = new Dictionary<string, string[]>
        {
            { "HighStreet", new[] { 
                "gardenDynamic/GROUP_Hammering/gateTall/gateTallOpenSystem",
                "overworldStatic/GROUP_Hub/HallToHubGateSystem/HallToHubGateMainSystem",
                "overworldStatic/GROUP_Hub/HallToHubGateSystem/HallToHubGateLockSystem"
            }},
            { "Backyards", new[] {
                "highStreetDynamic/GROUP_Garage/irongate/GateSystem"
            }},
            { "Pub", new[] {
                "pubDynamic/GROUP_pubItems/PubGateSystem",
                "overworldStatic/GROUP_BackyardToPub/SluiceGateSystem",
                "overworldStatic/GROUP_Hub/PubToHubGateSystem"
            }},
            { "Finale", new[] {
                "pubDynamic/GROUP_BucketOnHead/PubToFinaleGateSystem",
                "pubDynamic/GROUP_BucketOnHead/PubToFinaleGateSystem/gate",
                "pubDynamic/GROUP_BucketOnHead/PubToFinaleGateSystem/gate/gateMetal",
                "overworldStatic/GROUP_ParkToPub/FinaleToParkGateSystem"
            }},
            { "Garden", new[] {
                "gardenDynamic/GROUP_Gate/GardenGate/GateSystem",
                "overworldStatic/GROUP_Hub/HubGateSystem/HubGateMainSystem",
                "overworldStatic/GROUP_Hub/HubGateSystem/LockSystem"
            }}
        };
        
        public GateManager(Plugin plugin)
        {
            this.plugin = plugin;
        }
        
        public void SyncGatesFromAccessFlags()
        {
            Log.LogInfo("=== SYNCING GATES FROM ACCESS FLAGS ===");
            Log.LogInfo("  High Street: " + plugin.HasHighStreetAccess);
            Log.LogInfo("  Back Gardens: " + plugin.HasBackGardensAccess);
            Log.LogInfo("  Pub: " + plugin.HasPubAccess);
            Log.LogInfo("  Model Village: " + plugin.HasModelVillageAccess);
            
            if (plugin.HasHighStreetAccess)
            {
                OpenGatesForArea("HighStreet");
                Log.LogInfo("  Opened High Street gates");
            }
            if (plugin.HasBackGardensAccess)
            {
                OpenGatesForArea("Backyards");
                Log.LogInfo("  Opened Back Gardens gates");
            }
            if (plugin.HasPubAccess)
            {
                OpenGatesForArea("Pub");
                Log.LogInfo("  Opened Pub gates");
            }
            if (plugin.HasModelVillageAccess)
            {
                OpenGatesForArea("Finale");
                Log.LogInfo("  Opened Model Village gates");
            }
            
            // Always ensure hub paths are open
            DisableHubBlocker();
            
            plugin.UI.ShowNotification("Gates synced from server!");
            Log.LogInfo("=== GATE SYNC COMPLETE ===");
        }
        
        private void DisableHubBlocker()
        {
            var parkHubBlocker = GameObject.Find("highStreetDynamic/GROUP_Garage/irongate/GateSystem/GateExtraColliders/ParkHubGateExtraCollider");
            if (parkHubBlocker != null)
            {
                parkHubBlocker.SetActive(false);
                Log.LogInfo("  Disabled ParkHubGateExtraCollider");
            }
        }
        
        public void OpenGatesForArea(string areaName)
        {
            if (!AreaGates.ContainsKey(areaName))
            {
                Log.LogWarning("No gates defined for area: " + areaName);
                return;
            }

            Log.LogInfo("Opening gates for area: " + areaName);
            
            // Set save flags that track goal completion
            switch (areaName)
            {
                case "HighStreet":
                    SaveGameData.SetBoolValue("goalHammering", true, false);
                    break;
                case "Backyards":
                    SaveGameData.SetBoolValue("goalGarage", true, false);
                    break;
                case "Pub":
                    SaveGameData.SetBoolValue("goalPrune", true, false);
                    break;
                case "Finale":
                    SaveGameData.SetBoolValue("goalBucket", true, false);
                    break;
            }
            
            // Trigger relevant switch events
            string[] eventsToTry = GetEventsForArea(areaName);
            if (eventsToTry != null)
            {
                foreach (var evt in eventsToTry)
                {
                    try 
                    { 
                        SwitchEventManager.TriggerEvent(evt);
                        Log.LogInfo("  Triggered event: " + evt);
                    } 
                    catch { }
                }
            }
            
            // Disable area-specific invisible blockers
            DisableAreaBlockers(areaName);

            // Directly manipulate gate objects
            foreach (string gatePath in AreaGates[areaName])
            {
                ProcessGate(gatePath, areaName);
            }
        }
        
        private string[] GetEventsForArea(string areaName)
        {
            switch (areaName)
            {
                case "HighStreet": return new[] { "unlockHighStreet", "openHighStreet", "goalHammering" };
                case "Backyards": return new[] { "unlockBackyards", "openBackyards", "goalGarage" };
                case "Pub": return new[] { "unlockPub", "openPub", "goalPrune" };
                case "Finale": return new[] { "unlockFinale", "openFinale", "goalBucket" };
                default: return null;
            }
        }
        
        private void ProcessGate(string gatePath, string areaName)
        {
            GameObject gateObj = GameObject.Find(gatePath);
            if (gateObj == null)
            {
                // Try finding by partial path
                var allSwitches = UnityEngine.Object.FindObjectsOfType<SwitchSystem>();
                foreach (var sw in allSwitches)
                {
                    string path = GetGameObjectPath(sw.gameObject);
                    if (path.EndsWith(gatePath) || path == gatePath)
                    {
                        gateObj = sw.gameObject;
                        break;
                    }
                }
            }
            
            if (gateObj == null)
            {
                Log.LogWarning("  Could not find gate: " + gatePath);
                return;
            }
            
            // Set switch state
            var switchSystem = gateObj.GetComponent<SwitchSystem>();
            if (switchSystem != null)
            {
                // Disable auto-closer first
                Transform autoCloser = switchSystem.transform.Find("autoCloser");
                if (autoCloser != null)
                {
                    autoCloser.gameObject.SetActive(false);
                }
                
                switchSystem.SetState(1, null);
                Log.LogInfo("  Set switch state to 1 for: " + gatePath);
            }
            
            // Disable colliders
            DisableGateColliders(gateObj, areaName);
        }
        
        private void DisableGateColliders(GameObject gateObj, string areaName)
        {
            // Disable extra colliders
            Transform extraColliders = gateObj.transform.Find("GateExtraColliders");
            if (extraColliders != null)
            {
                foreach (Transform child in extraColliders)
                {
                    // Don't disable hub path blockers
                    if (!child.name.Contains("Hub"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
            
            // Disable wall colliders
            string[] wallNames = { "InvisibleWall", "invisibleWall", "Wall", "Blocker", "blocker" };
            foreach (string wallName in wallNames)
            {
                Transform wall = gateObj.transform.Find(wallName);
                if (wall != null)
                {
                    wall.gameObject.SetActive(false);
                }
            }
            
            // Recursively disable colliders on children named with "collider" or "blocker"
            DisableCollidersRecursive(gateObj.transform);
        }
        
        private void DisableCollidersRecursive(Transform parent)
        {
            foreach (Transform child in parent)
            {
                string lowerName = child.name.ToLower();
                if (lowerName.Contains("collider") || lowerName.Contains("blocker") || lowerName.Contains("invisible"))
                {
                    if (!lowerName.Contains("hub")) // Keep hub paths
                    {
                        child.gameObject.SetActive(false);
                    }
                }
                DisableCollidersRecursive(child);
            }
        }
        
        private void DisableAreaBlockers(string areaName)
        {
            switch (areaName)
            {
                case "HighStreet":
                    DisableObjectByPath("gardenDynamic/GROUP_Hammering/InvisibleWall");
                    break;
                case "Backyards":
                    DisableObjectByPath("highStreetDynamic/GROUP_Garage/InvisibleWall");
                    break;
                case "Pub":
                    DisableObjectByPath("overworldStatic/GROUP_BackyardToPub/InvisibleWall");
                    DisableObjectByPath("overworldStatic/GROUP_BackyardToPub/SluiceGateSystem/InvisibleWall");
                    break;
                case "Finale":
                    DisableObjectByPath("pubDynamic/GROUP_BucketOnHead/InvisibleWall");
                    break;
            }
        }
        
        private void DisableObjectByPath(string path)
        {
            var obj = GameObject.Find(path);
            if (obj != null)
            {
                obj.SetActive(false);
                Log.LogInfo("  Disabled: " + path);
            }
        }
        
        public void TeleportGooseToWell()
        {
            TeleportGoose(WellPosition);
            Log.LogInfo("Teleported goose to hub at: " + WellPosition);
        }
        
        public void TeleportGoose(Vector3 position)
        {
            try
            {
                if (GameManager.instance != null && GameManager.instance.allGeese != null)
                {
                    foreach (var goose in GameManager.instance.allGeese)
                    {
                        if (goose != null && goose.isActiveAndEnabled)
                        {
                            // Stop movement
                            if (goose.mover != null)
                            {
                                goose.mover.currentSpeed = 0f;
                            }
                            
                            // Stop physics
                            var rb = goose.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                rb.velocity = Vector3.zero;
                                rb.angularVelocity = Vector3.zero;
                            }
                            
                            goose.transform.position = position;
                            Log.LogInfo("Teleported goose to: " + position);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError("TeleportGoose error: " + ex.Message);
            }
        }
        
        public void TriggerAreaUnlock(string areaName)
        {
            string eventName = "unlock" + areaName;
            try
            {
                SwitchEventManager.TriggerEvent(eventName);
                Log.LogInfo("Triggered area unlock event: " + eventName);
            }
            catch (Exception ex)
            {
                Log.LogWarning("Failed to trigger event " + eventName + ": " + ex.Message);
            }
        }
        
        public void OpenAllGates()
        {
            Log.LogInfo("=== OPENING ALL GATES ===");
            foreach (var area in AreaGates.Keys)
            {
                OpenGatesForArea(area);
            }
            DisableHubBlocker();
            plugin.UI.ShowNotification("All gates opened!");
        }
        
        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "null";
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
