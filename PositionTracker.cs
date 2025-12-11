using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Logging;

namespace GooseGameAP
{
    /// <summary>
    /// Tracks original positions of duplicate items (carrots, umbrellas, fertilizer)
    /// to give them unique identifiers based on where they started in the scene.
    /// </summary>
    public class PositionTracker
    {
        private static ManualLogSource Log => Plugin.Log;
        
        // Cache of item positions by type
        // Key: item type (e.g., "carrot"), Value: list of (position, index) tuples
        private Dictionary<string, List<CachedItem>> cachedItems = new Dictionary<string, List<CachedItem>>();
        
        // Items we want to track as unique
        private static readonly string[] TrackedItemTypes = new string[]
        {
            "carrot",
            "umbrella",
            "fertiliser",  // UK spelling used in game
            "fertilizer"   // Just in case
        };
        
        // Distance threshold for matching - items within this distance are considered the same
        private const float MATCH_DISTANCE = 2.0f;
        
        private class CachedItem
        {
            public Vector3 OriginalPosition;
            public int Index;
            public int InstanceId;
            public bool Collected;
            
            public CachedItem(Vector3 pos, int index, int instanceId)
            {
                OriginalPosition = pos;
                Index = index;
                InstanceId = instanceId;
                Collected = false;
            }
        }
        
        /// <summary>
        /// Scan the scene for duplicate items and cache their positions.
        /// Call this when entering a new area.
        /// </summary>
        public void ScanAndCacheItems()
        {
            Log?.LogInfo("[POSITION] Scanning scene for duplicate items...");
            cachedItems.Clear();
            
            // Find all Prop objects in scene
            var allProps = UnityEngine.Object.FindObjectsOfType<Prop>();
            Log?.LogInfo($"[POSITION] Found {allProps.Length} total Props in scene");
            
            foreach (var prop in allProps)
            {
                if (prop == null || prop.gameObject == null) continue;
                
                string itemName = prop.gameObject.name.ToLower().Replace("(clone)", "").Trim();
                
                // Special handling for carrots - only track "carrot", not "carrotnogreen"
                if (itemName == "carrot")
                {
                    CacheItem("carrot", prop);
                    continue;
                }
                
                // Skip carrotnogreen - these are shop carrots with their own IDs
                if (itemName.Contains("carrotnogreen"))
                {
                    continue;
                }
                
                // Check other tracked item types
                foreach (string trackedType in TrackedItemTypes)
                {
                    if (trackedType == "carrot") continue; // Already handled above
                    
                    if (itemName.Contains(trackedType))
                    {
                        CacheItem(trackedType, prop);
                        break;
                    }
                }
            }
            
            // Log summary
            foreach (var kvp in cachedItems)
            {
                Log?.LogInfo($"[POSITION] Cached {kvp.Value.Count} {kvp.Key}(s):");
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    var item = kvp.Value[i];
                    Log?.LogInfo($"[POSITION]   {kvp.Key} #{item.Index}: ({item.OriginalPosition.x:F2}, {item.OriginalPosition.y:F2}, {item.OriginalPosition.z:F2})");
                }
            }
        }
        
        private void CacheItem(string itemType, Prop prop)
        {
            if (!cachedItems.ContainsKey(itemType))
            {
                cachedItems[itemType] = new List<CachedItem>();
            }
            
            Vector3 pos = prop.transform.position;
            int index = cachedItems[itemType].Count + 1; // 1-indexed
            int instanceId = prop.gameObject.GetInstanceID();
            
            cachedItems[itemType].Add(new CachedItem(pos, index, instanceId));
        }
        
        /// <summary>
        /// Try to identify which specific item this is based on its position or instance ID.
        /// Returns the index (1-based) or 0 if not found.
        /// </summary>
        public int IdentifyItem(string itemName, Vector3 currentPosition, int instanceId)
        {
            string itemType = GetItemType(itemName);
            if (itemType == null || !cachedItems.ContainsKey(itemType))
            {
                Log?.LogInfo($"[POSITION] Item type '{itemName}' not tracked");
                return 0;
            }
            
            var items = cachedItems[itemType];
            
            // First try to match by instance ID (most accurate)
            foreach (var item in items)
            {
                if (item.InstanceId == instanceId && !item.Collected)
                {
                    Log?.LogInfo($"[POSITION] Matched {itemType} #{item.Index} by InstanceID {instanceId}");
                    item.Collected = true;
                    return item.Index;
                }
            }
            
            // Fall back to position matching
            float closestDist = float.MaxValue;
            CachedItem closestItem = null;
            
            foreach (var item in items)
            {
                if (item.Collected) continue;
                
                float dist = Vector3.Distance(item.OriginalPosition, currentPosition);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestItem = item;
                }
            }
            
            if (closestItem != null && closestDist < MATCH_DISTANCE)
            {
                Log?.LogInfo($"[POSITION] Matched {itemType} #{closestItem.Index} by position (dist: {closestDist:F2})");
                closestItem.Collected = true;
                return closestItem.Index;
            }
            
            Log?.LogInfo($"[POSITION] Could not match {itemType} at position ({currentPosition.x:F2}, {currentPosition.y:F2}, {currentPosition.z:F2})");
            return 0;
        }
        
        /// <summary>
        /// Check if we're tracking this item type
        /// </summary>
        public bool IsTrackedItem(string itemName)
        {
            return GetItemType(itemName) != null;
        }
        
        private string GetItemType(string itemName)
        {
            string lower = itemName.ToLower();
            
            // Special handling for carrots - only track "carrot", not "carrotnogreen"
            if (lower.Contains("carrotnogreen"))
            {
                return null; // Shop carrots have their own IDs
            }
            
            // Check if it's exactly "carrot" (with possible _N suffix from our tracking)
            if (lower == "carrot" || lower.StartsWith("carrot_"))
            {
                return "carrot";
            }
            
            // Check other tracked types
            foreach (string trackedType in TrackedItemTypes)
            {
                if (trackedType == "carrot") continue; // Already handled above
                
                if (lower.Contains(trackedType))
                {
                    return trackedType;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Reset tracking (call on area reset or when re-entering area)
        /// </summary>
        public void Reset()
        {
            cachedItems.Clear();
            Log?.LogInfo("[POSITION] Position cache cleared");
        }
        
        /// <summary>
        /// Get how many of an item type we've cached
        /// </summary>
        public int GetCachedCount(string itemType)
        {
            if (cachedItems.ContainsKey(itemType.ToLower()))
            {
                return cachedItems[itemType.ToLower()].Count;
            }
            return 0;
        }
    }
}