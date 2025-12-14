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
                    int instanceId = currentProp.gameObject.GetInstanceID();
                    Vector3 pos = currentProp.transform.position;
                    
                    
                    // Check if this is a tracked item type that needs position-based identification
                    var posTracker = plugin.PositionTracker;
                    if (posTracker != null && posTracker.IsTrackedItem(itemName))
                    {
                        int itemIndex = posTracker.IdentifyItem(itemName, pos, instanceId);
                        if (itemIndex > 0)
                        {
                            // Use unique key with index (e.g., "carrot_1", "carrot_2")
                            itemKey = itemKey + "_" + itemIndex;
                        }
                    }
                    
                    // Debug: Log hierarchy and position for items that might need differentiation
                    string lowerName = itemName.ToLower();
                    if (lowerName.Contains("carrot") || lowerName.Contains("umbrella") || lowerName.Contains("fertili"))
                    {
                        var current = currentProp.transform;
                        string hierarchy = "";
                        for (int i = 0; i < 12 && current != null; i++)
                        {
                            hierarchy += $"[{i}]{current.gameObject?.name} | ";
                            current = current.parent;
                        }
                    }
                    
                    if (firstTimePickups.Add(itemKey))
                    {
                        OnFirstTimePickup(itemKey, itemPath);
                    }
                }
                // Detect drop (was holding, now null)
                else if (currentProp == null && lastHeldProp != null)
                {
                }
                // Detect swap (holding different item)
                else if (currentProp != null && lastHeldProp != null && currentProp != lastHeldProp)
                {
                    string itemName = currentProp.gameObject.name;
                    string itemKey = LocationMappings.CleanItemName(itemName);
                    int instanceId = currentProp.gameObject.GetInstanceID();
                    Vector3 pos = currentProp.transform.position;
                    
                    
                    // Check if this is a tracked item type that needs position-based identification
                    var posTracker = plugin.PositionTracker;
                    if (posTracker != null && posTracker.IsTrackedItem(itemName))
                    {
                        int itemIndex = posTracker.IdentifyItem(itemName, pos, instanceId);
                        if (itemIndex > 0)
                        {
                            itemKey = itemKey + "_" + itemIndex;
                        }
                    }
                    
                    // Debug: Log hierarchy for items that might need differentiation
                    string lowerName = itemName.ToLower();
                    if (lowerName.Contains("carrot") || lowerName.Contains("umbrella") || lowerName.Contains("fertili"))
                    {
                        var current = currentProp.transform;
                        string hierarchy = "";
                        for (int i = 0; i < 6 && current != null; i++)
                        {
                            hierarchy += current.gameObject?.name + " | ";
                            current = current.parent;
                        }
                        string itemPath = GetGameObjectPath(currentProp.gameObject);
                    }
                    
                    if (firstTimePickups.Add(itemKey))
                    {
                        string itemPath = GetGameObjectPath(currentProp.gameObject);
                        OnFirstTimePickup(itemKey, itemPath);
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
                catch (Exception)
                {
                    needsRefresh = true;
                    ResetDraggerCache();
                }
            }
            
            if (needsRefresh)
            {
                
                // Method 1: Try GetComponent<Dragger>()
                var draggerComp = goose.GetComponent<Dragger>();
                if (draggerComp != null)
                {
                    cachedDragger = draggerComp;
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
                    int instanceId = effectiveDragProp.gameObject.GetInstanceID();
                    Vector3 pos = effectiveDragProp.transform.position;
                    
                    
                    // Check if this is a tracked item type that needs position-based identification
                    var posTracker = plugin.PositionTracker;
                    if (posTracker != null && posTracker.IsTrackedItem(itemName))
                    {
                        int itemIndex = posTracker.IdentifyItem(itemName, pos, instanceId);
                        if (itemIndex > 0)
                        {
                            itemKey = itemKey + "_" + itemIndex;
                        }
                    }
                    
                    // Debug: Log hierarchy and position for items that might need differentiation
                    string lowerName = itemName.ToLower();
                    if (lowerName.Contains("carrot") || lowerName.Contains("umbrella") || lowerName.Contains("fertili"))
                    {
                        var current = effectiveDragProp.transform;
                        string hierarchy = "";
                        for (int i = 0; i < 6 && current != null; i++)
                        {
                            hierarchy += current.gameObject?.name + " | ";
                            current = current.parent;
                        }
                    }
                    
                    if (firstTimeDrags.Add(itemKey))
                    {
                        OnFirstTimeDrag(itemKey, itemPath);
                    }
                }
                // Detect release
                else if (effectiveDragProp == null && lastDraggedProp != null)
                {
                }
                // Detect swap drag
                else if (effectiveDragProp != null && lastDraggedProp != null && effectiveDragProp != lastDraggedProp)
                {
                    string itemName = effectiveDragProp.gameObject.name;
                    string itemKey = LocationMappings.CleanItemName(itemName);
                    int instanceId = effectiveDragProp.gameObject.GetInstanceID();
                    Vector3 pos = effectiveDragProp.transform.position;
                    
                    
                    // Check if this is a tracked item type that needs position-based identification
                    var posTracker = plugin.PositionTracker;
                    if (posTracker != null && posTracker.IsTrackedItem(itemName))
                    {
                        int itemIndex = posTracker.IdentifyItem(itemName, pos, instanceId);
                        if (itemIndex > 0)
                        {
                            itemKey = itemKey + "_" + itemIndex;
                        }
                    }
                    
                    // Debug: Log hierarchy for items that might need differentiation
                    string lowerName = itemName.ToLower();
                    if (lowerName.Contains("carrot") || lowerName.Contains("umbrella") || lowerName.Contains("fertili"))
                    {
                        var current = effectiveDragProp.transform;
                        string hierarchy = "";
                        for (int i = 0; i < 6 && current != null; i++)
                        {
                            hierarchy += current.gameObject?.name + " | ";
                            current = current.parent;
                        }
                    }
                    
                    if (firstTimeDrags.Add(itemKey))
                    {
                        string itemPath = GetGameObjectPath(effectiveDragProp.gameObject);
                        OnFirstTimeDrag(itemKey, itemPath);
                    }
                }
                
                lastDraggedProp = effectiveDragProp;
            }
        }
        
        private void OnFirstTimePickup(string itemName, string itemPath)
        {
            long? locationId = LocationMappings.GetItemLocationId(itemName);
            if (locationId.HasValue)
            {
                plugin.SendLocationCheck(locationId.Value);
            }
        }
        
        private void OnFirstTimeDrag(string itemName, string itemPath)
        {
            long? locationId = LocationMappings.GetDragLocationId(itemName);
            if (locationId.HasValue)
            {
                plugin.SendLocationCheck(locationId.Value);
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