using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace GooseGameAP
{
    /// <summary>
    /// Manages NPC visibility based on soul items.
    /// NPCs are disabled immediately when game loads, then enabled when souls are received.
    /// When NPCSoulsEnabled is false, all NPCs are always active.
    /// </summary>
    public class NPCManager
    {
        private static ManualLogSource Log => Plugin.Log;
        private Plugin plugin;
        
        // Cache of NPC GameObjects we've found
        private Dictionary<string, GameObject> npcCache = new Dictionary<string, GameObject>();
        private bool hasScannedNPCs = false;
        
        // Confirmed NPC paths from scan
        private static readonly Dictionary<string, string> NpcPaths = new Dictionary<string, string>
        {
            { "Groundskeeper", "boilerRoom/ObjectManager/GeneratedGroups/people/gardener brain" },
            { "Boy", "boilerRoom/ObjectManager/GeneratedGroups/people/wimp brain" },
            { "TVShopOwner", "boilerRoom/ObjectManager/GeneratedGroups/people/tvshop brain" },
            { "MarketLady", "boilerRoom/ObjectManager/GeneratedGroups/people/shopkeeper brain" },
            { "TidyNeighbour", "boilerRoom/ObjectManager/GeneratedGroups/people/neighbourClean brain" },
            { "MessyNeighbour", "boilerRoom/ObjectManager/GeneratedGroups/people/neighbourMessyFixed brain" },
            { "BurlyMan", "boilerRoom/ObjectManager/GeneratedGroups/people/pub man brain" },
            { "OldMan", "boilerRoom/ObjectManager/GeneratedGroups/people/oldMan brain" },
            { "PubLady", "boilerRoom/ObjectManager/GeneratedGroups/people/pub woman brain" },
            { "FancyLadies1", "boilerRoom/ObjectManager/GeneratedGroups/people/gossip1 brain" },
            { "FancyLadies2", "boilerRoom/ObjectManager/GeneratedGroups/people/gossip2 brain" },
            { "Cook", "boilerRoom/ObjectManager/GeneratedGroups/people/cook brain" },
        };
        
        public NPCManager(Plugin plugin)
        {
            this.plugin = plugin;
        }
        
        /// <summary>
        /// Check if NPC souls are enabled for this session
        /// </summary>
        private bool NPCSoulsEnabled => plugin.NPCSoulsEnabled;
        
        /// <summary>
        /// Called every frame from Plugin.Update()
        /// </summary>
        public void Update()
        {
            if (!hasScannedNPCs && GameManager.instance?.allGeese != null && GameManager.instance.allGeese.Count > 0)
            {
                Log.LogInfo("[NPC] Game loaded - finding NPCs");
                FindAndCacheNPCs();
                
                // Only disable NPCs if NPC souls are enabled
                if (NPCSoulsEnabled)
                {
                    Log.LogInfo("[NPC] NPC souls ENABLED - disabling NPCs until souls received");
                    DisableAllNPCs();
                    ApplyNPCStatesFromFlags();
                }
                else
                {
                    Log.LogInfo("[NPC] NPC souls DISABLED - all NPCs active");
                    EnableAllNPCs();
                }
            }
        }
        
        /// <summary>
        /// Find NPCs by path and cache them
        /// </summary>
        private void FindAndCacheNPCs()
        {
            if (hasScannedNPCs) return;
            hasScannedNPCs = true;
            
            Log.LogInfo("[NPC] Finding NPCs by path...");
            
            foreach (var kvp in NpcPaths)
            {
                string npcType = kvp.Key;
                string path = kvp.Value;
                
                GameObject npc = GameObject.Find(path);
                if (npc != null)
                {
                    npcCache[npcType] = npc;
                    Log.LogInfo($"[NPC] Found {npcType}");
                }
                else
                {
                    Log.LogWarning($"[NPC] Could not find {npcType} at {path}");
                }
            }
            
            Log.LogInfo($"[NPC] Cached {npcCache.Count} NPCs");
        }
        
        /// <summary>
        /// Disable ALL NPCs
        /// </summary>
        private void DisableAllNPCs()
        {
            Log.LogInfo("[NPC] Disabling all NPCs");
            foreach (var kvp in npcCache)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// Enable ALL NPCs (when souls are disabled)
        /// </summary>
        private void EnableAllNPCs()
        {
            Log.LogInfo("[NPC] Enabling all NPCs (souls disabled)");
            foreach (var kvp in npcCache)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(true);
                }
            }
        }
        
        /// <summary>
        /// Apply NPC states based on current soul flags in Plugin
        /// </summary>
        private void ApplyNPCStatesFromFlags()
        {
            // If souls are disabled, enable everything
            if (!NPCSoulsEnabled)
            {
                EnableAllNPCs();
                return;
            }
            
            Log.LogInfo("[NPC] Applying NPC states from flags");
            
            SetNPCActive("Groundskeeper", plugin.HasGroundskeeperSoul);
            SetNPCActive("Boy", plugin.HasBoySoul);
            SetNPCActive("TVShopOwner", plugin.HasTVShopOwnerSoul);
            SetNPCActive("MarketLady", plugin.HasMarketLadySoul);
            SetNPCActive("TidyNeighbour", plugin.HasTidyNeighbourSoul);
            SetNPCActive("MessyNeighbour", plugin.HasMessyNeighbourSoul);
            SetNPCActive("BurlyMan", plugin.HasBurlyManSoul);
            SetNPCActive("OldMan", plugin.HasOldManSoul);
            SetNPCActive("PubLady", plugin.HasPubLadySoul);
            SetNPCActive("FancyLadies1", plugin.HasFancyLadiesSoul);
            SetNPCActive("FancyLadies2", plugin.HasFancyLadiesSoul);
            SetNPCActive("Cook", plugin.HasCookSoul);
        }
        
        // Set a single NPC's active state
        private void SetNPCActive(string npcType, bool active)
        {
            if (npcCache.TryGetValue(npcType, out GameObject npc) && npc != null)
            {
                bool wasActive = npc.activeSelf;
                npc.SetActive(active);
                if (active && !wasActive) WakeNPC(npc);
                Log.LogInfo($"[NPC] {npcType} set to {(active ? "ACTIVE" : "INACTIVE")}");
            }
        }
        
        // Culling leaves a re-enabled NPC asleep until it is woken
        private void WakeNPC(GameObject npc)
        {
            var brain = npc.GetComponentInChildren<Brain>(true);
            if (brain != null && CullingManager.Instance != null)
                CullingManager.Instance.WakeSpecificPerson(brain);
        }
        
        // Enable an NPC when their soul is received
        public void EnableNPC(string npcType)
        {
            // If souls are disabled, NPCs are already enabled
            if (!NPCSoulsEnabled)
                return;
            
            Log.LogInfo($"[NPC] EnableNPC called for {npcType}");
            
            if (npcType == "FancyLadies")
            {
                EnableSingleNPC("FancyLadies1");
                EnableSingleNPC("FancyLadies2");
                return;
            }
            
            EnableSingleNPC(npcType);
        }
        
        private void EnableSingleNPC(string npcType)
        {
            if (npcCache.TryGetValue(npcType, out GameObject npc) && npc != null)
            {
                bool wasActive = npc.activeSelf;
                npc.SetActive(true);
                if (!wasActive) WakeNPC(npc);
                Log.LogInfo($"[NPC] {npcType} is now ACTIVE");
            }
            else
            {
                Log.LogWarning($"[NPC] {npcType} not in cache, searching...");
                if (NpcPaths.TryGetValue(npcType, out string path))
                {
                    var foundNpc = GameObject.Find(path);
                    if (foundNpc != null)
                    {
                        npcCache[npcType] = foundNpc;
                        foundNpc.SetActive(true);
                        WakeNPC(foundNpc);
                        Log.LogInfo($"[NPC] Found and enabled {npcType}");
                    }
                }
            }
        }
        
        // Re-apply all NPC states (called when reconnecting or settings change)
        public void RefreshNPCStates()
        {
            if (!hasScannedNPCs) return;
            
            Log.LogInfo($"[NPC] RefreshNPCStates called (NPCSoulsEnabled={NPCSoulsEnabled})");
            
            if (NPCSoulsEnabled)
            {
                ApplyNPCStatesFromFlags();
            }
            else
            {
                EnableAllNPCs();
            }
        }
        
        /// <summary>
        /// Reset for returning to menu
        /// </summary>
        public void Reset()
        {
            Log.LogInfo("[NPC] Reset called - clearing cache");
            hasScannedNPCs = false;
            npcCache.Clear();
        }
    }
}