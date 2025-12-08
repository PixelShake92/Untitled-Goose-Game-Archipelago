using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace GooseGameAP
{
    /// <summary>
    /// Tracks first-time item pickups and drags
    /// </summary>
    public class ItemTracker
    {
        private static ManualLogSource Log => Plugin.Log;
        private Plugin plugin;
        
        // Tracking state
        private Prop lastHeldProp = null;
        private Prop lastDraggedProp = null;
        private object cachedDragger = null;
        private Goose cachedGoose = null;
        private System.Reflection.FieldInfo draggerHoldableField = null;
        private System.Reflection.FieldInfo draggerActiveField = null;
        private HashSet<string> firstTimePickups = new HashSet<string>();
        private HashSet<string> firstTimeDrags = new HashSet<string>();
        private float draggerRefreshTimer = 0f;
        
        public ItemTracker(Plugin plugin)
        {
            this.plugin = plugin;
        }
        
        public void ResetDraggerCache()
        {
            cachedDragger = null;
            cachedGoose = null;
            draggerHoldableField = null;
            draggerActiveField = null;
            lastDraggedProp = null;
            draggerRefreshTimer = 0f;
            Log.LogInfo("[DRAGGER] Cache reset");
        }
        
        public void Update()
        {
            TrackHeldItem();
        }
        
        private void TrackHeldItem()
        {
            if (GameManager.instance == null || GameManager.instance.allGeese == null) return;
            
            Goose goose = null;
            foreach (var g in GameManager.instance.allGeese)
            {
                if (g != null && g.isActiveAndEnabled)
                {
                    goose = g;
                    break;
                }
            }
            if (goose == null) return;
            
            // Check if goose changed (happens after area reset) - reset cache
            if (cachedGoose != null && cachedGoose != goose)
            {
                Log.LogInfo("[DRAGGER] Goose changed! Resetting cache...");
                ResetDraggerCache();
            }
            cachedGoose = goose;
            
            // === TRACK BEAK PICKUPS via Holder ===
            var holder = goose.GetComponent<Holder>();
            if (holder != null)
            {
                Prop currentProp = holder.holding;
                
                // Detect pickup (was null, now holding something)
                if (currentProp != null && lastHeldProp == null)
                {
                    string itemName = currentProp.gameObject.name;
                    string itemPath = GetGameObjectPath(currentProp.gameObject);
                    string itemKey = LocationMappings.CleanItemName(itemName);
                    
                    Log.LogInfo("[AUTO] Picked up: " + itemName);
                    
                    if (firstTimePickups.Add(itemKey))
                    {
                        Log.LogInfo("[AUTO] >>> FIRST TIME PICKUP: " + itemName + " <<<");
                        OnFirstTimePickup(itemName, itemPath);
                    }
                }
                // Detect drop (was holding, now null)
                else if (currentProp == null && lastHeldProp != null)
                {
                    Log.LogInfo("[AUTO] Dropped: " + lastHeldProp.gameObject.name);
                }
                // Detect swap (holding different item)
                else if (currentProp != null && lastHeldProp != null && currentProp != lastHeldProp)
                {
                    string itemName = currentProp.gameObject.name;
                    string itemKey = LocationMappings.CleanItemName(itemName);
                    
                    Log.LogInfo("[AUTO] Swapped to: " + itemName);
                    
                    if (firstTimePickups.Add(itemKey))
                    {
                        string itemPath = GetGameObjectPath(currentProp.gameObject);
                        Log.LogInfo("[AUTO] >>> FIRST TIME PICKUP: " + itemName + " <<<");
                        OnFirstTimePickup(itemName, itemPath);
                    }
                }
                
                lastHeldProp = currentProp;
            }
            
            // === TRACK DRAGS via Dragger ===
            // Periodic refresh to catch cases where reset doesn't change goose reference
            draggerRefreshTimer += Time.deltaTime;
            if (draggerRefreshTimer > 30f)
            {
                draggerRefreshTimer = 0f;
                if (cachedDragger != null)
                {
                    Log.LogInfo("[DRAGGER] Periodic cache refresh");
                    ResetDraggerCache();
                }
            }
            
            // Get dragger from Goose (refresh if invalid)
            bool needsRefresh = cachedDragger == null;
            
            // Check if cached dragger is still valid
            if (cachedDragger != null && !needsRefresh)
            {
                try
                {
                    if (cachedDragger is UnityEngine.Object unityObj)
                    {
                        if (unityObj == null)
                        {
                            Log.LogInfo("[DRAGGER] Cached dragger was destroyed, refreshing...");
                            needsRefresh = true;
                            ResetDraggerCache();
                        }
                    }
                    else
                    {
                        var test = cachedDragger.ToString();
                        if (test == null || test == "null" || test.Contains("null"))
                        {
                            needsRefresh = true;
                            ResetDraggerCache();
                        }
                    }
                    
                    if (!needsRefresh && draggerHoldableField != null)
                    {
                        var holdable = draggerHoldableField.GetValue(cachedDragger);
                    }
                }
                catch (Exception ex)
                {
                    Log.LogInfo("[DRAGGER] Cache validation failed: " + ex.Message);
                    needsRefresh = true;
                    ResetDraggerCache();
                }
            }
            
            if (needsRefresh)
            {
                Log.LogInfo("[DRAGGER] Attempting to find dragger...");
                
                // Method 1: Try GetComponent<Dragger>()
                var draggerComp = goose.GetComponent<Dragger>();
                if (draggerComp != null)
                {
                    cachedDragger = draggerComp;
                    Log.LogInfo("[DRAGGER] Found via GetComponent<Dragger>");
                }
                
                // Method 2: Look for "dragger" field on Goose component
                if (cachedDragger == null)
                {
                    foreach (var comp in goose.GetComponents<Component>())
                    {
                        if (comp.GetType().Name == "Goose")
                        {
                            var draggerField = comp.GetType().GetField("dragger",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            
                            if (draggerField != null)
                            {
                                cachedDragger = draggerField.GetValue(comp);
                                if (cachedDragger != null)
                                {
                                    Log.LogInfo("[DRAGGER] Found via Goose.dragger field");
                                }
                            }
                            break;
                        }
                    }
                }
                
                // Cache field accessors if we got the dragger
                if (cachedDragger != null)
                {
                    draggerHoldableField = cachedDragger.GetType().GetField("holdable",
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);
                        
                    draggerActiveField = cachedDragger.GetType().GetField("active",
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);
                    
                    if (draggerHoldableField != null && draggerActiveField != null)
                    {
                        Log.LogInfo("[DRAGGER] Successfully cached dragger fields");
                    }
                    else
                    {
                        Log.LogWarning("[DRAGGER] Could not find holdable/active fields!");
                    }
                }
                else
                {
                    Log.LogWarning("[DRAGGER] Could not find dragger!");
                }
            }
            
            if (cachedDragger != null && draggerHoldableField != null && draggerActiveField != null)
            {
                Prop currentDragProp = draggerHoldableField.GetValue(cachedDragger) as Prop;
                bool isActive = (bool)draggerActiveField.GetValue(cachedDragger);
                
                Prop effectiveDragProp = isActive ? currentDragProp : null;
                
                // Detect start drag
                if (effectiveDragProp != null && lastDraggedProp == null)
                {
                    string itemName = effectiveDragProp.gameObject.name;
                    string itemPath = GetGameObjectPath(effectiveDragProp.gameObject);
                    string itemKey = LocationMappings.CleanItemName(itemName);
                    
                    Log.LogInfo("[AUTO] Dragging: " + itemName);
                    
                    if (firstTimeDrags.Add(itemKey))
                    {
                        Log.LogInfo("[AUTO] >>> FIRST TIME DRAG: " + itemName + " <<<");
                        OnFirstTimeDrag(itemName, itemPath);
                    }
                }
                // Detect release
                else if (effectiveDragProp == null && lastDraggedProp != null)
                {
                    Log.LogInfo("[AUTO] Released: " + lastDraggedProp.gameObject.name);
                }
                // Detect swap drag
                else if (effectiveDragProp != null && lastDraggedProp != null && effectiveDragProp != lastDraggedProp)
                {
                    string itemName = effectiveDragProp.gameObject.name;
                    string itemKey = LocationMappings.CleanItemName(itemName);
                    
                    Log.LogInfo("[AUTO] Swapped drag to: " + itemName);
                    
                    if (firstTimeDrags.Add(itemKey))
                    {
                        string itemPath = GetGameObjectPath(effectiveDragProp.gameObject);
                        Log.LogInfo("[AUTO] >>> FIRST TIME DRAG: " + itemName + " <<<");
                        OnFirstTimeDrag(itemName, itemPath);
                    }
                }
                
                lastDraggedProp = effectiveDragProp;
            }
        }
        
        private void OnFirstTimePickup(string itemName, string itemPath)
        {
            plugin.UI.ShowNotification("First pickup: " + itemName);
            plugin.UI.AddChatMessage("Picked up: " + itemName);
            
            long? locationId = LocationMappings.GetItemLocationId(itemName);
            if (locationId.HasValue)
            {
                plugin.SendLocationCheck(locationId.Value);
                Log.LogInfo("[AUTO] Sent location check for item: " + itemName + " (ID: " + locationId.Value + ")");
            }
        }
        
        private void OnFirstTimeDrag(string itemName, string itemPath)
        {
            plugin.UI.ShowNotification("First drag: " + itemName);
            plugin.UI.AddChatMessage("Dragged: " + itemName);
            
            long? locationId = LocationMappings.GetDragLocationId(itemName);
            if (locationId.HasValue)
            {
                plugin.SendLocationCheck(locationId.Value);
                Log.LogInfo("[AUTO] Sent location check for drag: " + itemName + " (ID: " + locationId.Value + ")");
            }
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
