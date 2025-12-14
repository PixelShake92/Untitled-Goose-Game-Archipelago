using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace GooseGameAP
{
    [BepInPlugin("com.archipelago.goosegame", "Goose Game Archipelago", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        public static Plugin Instance;
        
        public const long BASE_ID = 119000000;
        
        private Harmony harmony;
        private bool showUI = false;
        private bool hasInitializedGates = false;
        
        // Component managers
        public UIManager UI { get; private set; }
        public ArchipelagoClient Client { get; private set; }
        public GateManager GateManager { get; private set; }
        public TrapManager TrapManager { get; private set; }
        public ItemTracker ItemTracker { get; private set; }
        public InteractionTracker InteractionTracker { get; private set; }
        public PositionTracker PositionTracker { get; private set; }
        public GooseColourManager GooseColour { get; private set; }
        
        // Area access flags (Hub is always accessible - starting area)
        public bool HasGardenAccess { get; set; } = false;
        public bool HasHighStreetAccess { get; set; } = false;
        public bool HasBackGardensAccess { get; set; } = false;
        public bool HasPubAccess { get; set; } = false;
        public bool HasModelVillageAccess { get; set; } = false;
        public bool HasGoldenBell { get; set; } = false;
        
        // Buff tracking
        public bool IsSilent => TrapManager?.IsSilent ?? false;
        public int MegaHonkCount => TrapManager?.MegaHonkCount ?? 0;
        
        // DeathLink
        public bool DeathLinkEnabled { get; set; } = false;
        private bool deathLinkPending = false;
        
        // Location tracking
        private HashSet<long> checkedLocations = new HashSet<long>();
        public int CheckedLocationCount => checkedLocations.Count;
        
        public bool IsConnected => Client?.IsConnected ?? false;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo("Goose Game AP v1.0.0");
            
            // Initialize components
            UI = new UIManager();
            TrapManager = new TrapManager(this);
            GateManager = new GateManager(this);
            ItemTracker = new ItemTracker(this);
            InteractionTracker = new InteractionTracker(this);
            PositionTracker = new PositionTracker();
            GooseColour = new GooseColourManager(this);
            Client = new ArchipelagoClient(this);
            
            harmony = new Harmony("com.archipelago.goosegame");
            harmony.PatchAll();
            
            LoadAccessFlags();
        }

        private void Update()
        {
            // Initialize gates once when game scene loads
            if (!hasInitializedGates && GameManager.instance != null && 
                GameManager.instance.allGeese != null && GameManager.instance.allGeese.Count > 0)
            {
                hasInitializedGates = true;
                // Only initialize gates, don't teleport - let player start where they are
                
                // Refresh goose color renderers
                GooseColour?.RefreshRenderers();
                
                // Scan and rename duplicate items EARLY before player can pick them up
                PositionTracker?.ScanAndCacheItems();
                
                StartCoroutine(DelayedGateInit());
            }
            
            // Toggle UI
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showUI = !showUI;
            }
            
            // Debug keys
            HandleDebugKeys();
            
            // Process AP messages
            Client?.ProcessQueuedMessages();
            
            // Handle gate sync timing
            HandleGateSyncTiming();
            
            // Handle DeathLink
            if (deathLinkPending)
            {
                deathLinkPending = false;
                GateManager?.TeleportGooseToWell();
                UI?.ShowNotification("DeathLink! Another player died!");
            }
            
            // Update traps
            TrapManager?.Update();
            
            // Update goose color (for rainbow mode)
            GooseColour?.Update();
            
            // Update notification timer
            UI?.UpdateNotification(Time.deltaTime);
            
            // Track pickups/drags
            ItemTracker?.Update();
        }
        
        private void LateUpdate()
        {
            // Confused feet now uses velocity inversion in MoverPatches
            // stickAim inversion didn't work - Mover reads input directly
        }
        
        private void HandleDebugKeys()
        {
            // G key: Use a stored Goose Day
            if (Input.GetKeyDown(KeyCode.G) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                TrapManager?.UseGooseDay(15f);
            }
            
            // C key: Cycle goose color
            if (Input.GetKeyDown(KeyCode.C) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                GooseColour?.CycleColour();
                UI?.ShowNotification($"Goose Colour: {GooseColour?.CurrentColourName}");
            }
            
            // Ctrl+C: Reset goose color to default
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            {
                GooseColour?.ResetToDefault();
                UI?.ShowNotification("Goose color reset");
            }
            
            // F9: Manual gate sync from access flags (recovery for disconnect issues)
            if (Input.GetKeyDown(KeyCode.F9))
            {
                LoadAccessFlags();
                ClearHubBlockers();
                GateManager?.SyncGatesFromAccessFlags();
                UI?.ShowNotification("Gates re-synced!");
            }
        }
        
        private void HandleGateSyncTiming()
        {
            if (Client == null) return;
            
            // Handle pending gate sync - now does multiple attempts
            if (Client.PendingGateSync && GameManager.instance != null && 
                GameManager.instance.allGeese != null && GameManager.instance.allGeese.Count > 0)
            {
                Client.GateSyncTimer += Time.deltaTime;
                if (Client.GateSyncTimer >= 2.0f)
                {
                    Client.GateSyncAttempts++;
                    Client.GateSyncTimer = 0f;
                    
                    // Clear hub blockers on each attempt
                    ClearHubBlockers();
                    
                    // Sync gates
                    GateManager?.SyncGatesFromAccessFlags();
                    
                    // Check if we've done all attempts
                    if (Client.GateSyncAttempts >= ArchipelagoClient.MAX_GATE_SYNC_ATTEMPTS)
                    {
                        Client.PendingGateSync = false;
                    }
                }
            }
            
            // Timeout for received items
            if (Client.WaitingForReceivedItems && Client.IsConnected)
            {
                Client.ReceivedItemsTimeout += Time.deltaTime;
                if (Client.ReceivedItemsTimeout >= 5.0f)
                {
                    Log.LogWarning("ReceivedItems timeout - syncing gates with current access flags");
                    Client.WaitingForReceivedItems = false;
                    Client.ReceivedItemsTimeout = 0f;
                    Client.PendingGateSync = true;
                    Client.GateSyncTimer = 0f;
                }
            }
        }

        private void OnGUI()
        {
            if (!showUI) return;
            UI?.DrawUI(this);
        }
        
        // === PUBLIC API FOR OTHER COMPONENTS ===
        
        public void Connect()
        {
            Client?.Connect(UI.ServerAddress, UI.ServerPort, UI.SlotName, UI.Password, DeathLinkEnabled);
        }
        
        public void Disconnect()
        {
            Client?.Disconnect();
        }
        
        public void SendLocationCheck(long locationId)
        {
            if (!checkedLocations.Contains(locationId))
            {
                checkedLocations.Add(locationId);
                Client?.SendLocationCheck(locationId);
            }
        }
        
        public void SendGoalComplete()
        {
            Client?.SendGoalComplete();
        }
        
        public void TriggerDeathLink()
        {
            if (DeathLinkEnabled)
            {
                deathLinkPending = true;
                UI?.AddChatMessage("DeathLink received!");
            }
        }
        
        public void TeleportGooseToWell()
        {
            GateManager?.TeleportGooseToWell();
        }
        
        public void ProcessReceivedItem(long itemId)
        {
            long offset = itemId - BASE_ID;
            
            switch (offset)
            {
                case 100:
                    HasGardenAccess = true;
                    GateManager?.TriggerAreaUnlock("Garden");
                    GateManager?.OpenGatesForArea("Garden");
                    UI?.ShowNotification("Garden is now accessible!");
                    SaveAccessFlags();
                    break;
                case 101:
                    HasHighStreetAccess = true;
                    GateManager?.TriggerAreaUnlock("HighStreet");
                    GateManager?.OpenGatesForArea("HighStreet");
                    UI?.ShowNotification("High Street is now accessible!");
                    SaveAccessFlags();
                    break;
                case 102:
                    HasBackGardensAccess = true;
                    GateManager?.TriggerAreaUnlock("Backyards");
                    GateManager?.OpenGatesForArea("Backyards");
                    UI?.ShowNotification("Back Gardens are now accessible!");
                    SaveAccessFlags();
                    break;
                case 103:
                    HasPubAccess = true;
                    GateManager?.TriggerAreaUnlock("Pub");
                    GateManager?.OpenGatesForArea("Pub");
                    UI?.ShowNotification("The Pub is now accessible!");
                    SaveAccessFlags();
                    break;
                case 104:
                    HasModelVillageAccess = true;
                    GateManager?.TriggerAreaUnlock("Finale");
                    GateManager?.OpenGatesForArea("Finale");
                    UI?.ShowNotification("Model Village is now accessible!");
                    SaveAccessFlags();
                    break;
                    
                case 200:
                    if (TrapManager.MegaHonkCount < 3)
                    {
                        TrapManager.MegaHonkCount++;
                        TrapManager.SaveProgressiveItems();
                        string[] levelDesc = { "", "NPCs will notice!", "Heard from further away!", "NPCs will drop items in fear!" };
                        UI?.ShowNotification($"MEGA HONK Level {TrapManager.MegaHonkLevel}! {levelDesc[TrapManager.MegaHonkLevel]}");
                    }
                    else
                    {
                        UI?.ShowNotification("MEGA HONK already at max level!");
                    }
                    break;
                case 201:
                    if (TrapManager.SpeedyFeetCount < 10)
                    {
                        TrapManager.SpeedyFeetCount++;
                        TrapManager.SaveProgressiveItems();
                        int speedBonus = Math.Min(TrapManager.SpeedyFeetCount * 5, 50);
                        UI?.ShowNotification($"SPEEDY FEET! +{speedBonus}% speed ({TrapManager.SpeedyFeetCount}/10)");
                    }
                    else
                    {
                        UI?.ShowNotification("SPEEDY FEET already at max!");
                    }
                    break;
                case 202:
                    TrapManager.IsSilent = true;
                    TrapManager.SaveProgressiveItems();
                    UI?.ShowNotification("SILENT STEPS! NPCs can't hear your footsteps!");
                    break;
                case 203:
                    TrapManager?.ActivateGooseDay(15f);
                    break;
                    
                case 300:
                    TrapManager?.ActivateTired(15f);
                    break;
                case 301:
                    TrapManager?.ActivateConfused(15f);
                    break;
                case 302:
                    TrapManager?.ActivateButterfingers(10f);
                    break;
                case 303:
                    TrapManager?.ActivateSuspicious(10f);
                    break;
                    
                case 999:
                    HasGoldenBell = true;
                    UI?.ShowNotification("You received the Golden Bell!");
                    SaveAccessFlags();
                    break;
            }
        }
        
        public void OnGoalCompleted(string goalName)
        {
            long? locationId = LocationMappings.GetGoalLocationId(goalName);
            if (locationId.HasValue)
            {
                SendLocationCheck(locationId.Value);
            }
        }
        
        public void OnAreaBlocked(GoalListArea blockedArea)
        {
            string areaName = blockedArea.ToString();
            UI?.ShowNotification("You need " + areaName + " Access to enter!");
        }
        
        public void OnGooseShooed()
        {
            // Could trigger DeathLink here if enabled
        }
        
        public bool CanEnterArea(GoalListArea area)
        {
            switch (area)
            {
                case GoalListArea.Garden: return HasGardenAccess;  // Now requires access!
                case GoalListArea.Shops: return HasHighStreetAccess;
                case GoalListArea.Backyards: return HasBackGardensAccess;
                case GoalListArea.Pub: return HasPubAccess;
                case GoalListArea.Finale: return HasModelVillageAccess;
                default: return true;  // Hub is always accessible
            }
        }
        
        public void ResetAllAccess()
        {
            HasGardenAccess = false;
            HasHighStreetAccess = false;
            HasBackGardensAccess = false;
            HasPubAccess = false;
            HasModelVillageAccess = false;
            HasGoldenBell = false;
            Client?.ClearReceivedItems();
            checkedLocations.Clear();
            TrapManager?.ClearTraps();  // Also clears and saves progressive items
            ClearSavedAccessFlags();
        }
        
        // === PERSISTENCE ===
        
        private void SaveAccessFlags()
        {
            PlayerPrefs.SetInt("AP_Garden", HasGardenAccess ? 1 : 0);
            PlayerPrefs.SetInt("AP_HighStreet", HasHighStreetAccess ? 1 : 0);
            PlayerPrefs.SetInt("AP_BackGardens", HasBackGardensAccess ? 1 : 0);
            PlayerPrefs.SetInt("AP_Pub", HasPubAccess ? 1 : 0);
            PlayerPrefs.SetInt("AP_ModelVillage", HasModelVillageAccess ? 1 : 0);
            PlayerPrefs.SetInt("AP_GoldenBell", HasGoldenBell ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        private void LoadAccessFlags()
        {
            if (PlayerPrefs.HasKey("AP_Garden"))
            {
                HasGardenAccess = PlayerPrefs.GetInt("AP_Garden") == 1;
                HasHighStreetAccess = PlayerPrefs.GetInt("AP_HighStreet") == 1;
                HasBackGardensAccess = PlayerPrefs.GetInt("AP_BackGardens") == 1;
                HasPubAccess = PlayerPrefs.GetInt("AP_Pub") == 1;
                HasModelVillageAccess = PlayerPrefs.GetInt("AP_ModelVillage") == 1;
                HasGoldenBell = PlayerPrefs.GetInt("AP_GoldenBell") == 1;
            }
        }
        
        /// <summary>
        /// Public method to reload access flags - called on reconnect
        /// </summary>
        public void ReloadAccessFlags()
        {
            LoadAccessFlags();
        }
        
        private void ClearSavedAccessFlags()
        {
            PlayerPrefs.DeleteKey("AP_Garden");
            PlayerPrefs.DeleteKey("AP_HighStreet");
            PlayerPrefs.DeleteKey("AP_BackGardens");
            PlayerPrefs.DeleteKey("AP_Pub");
            PlayerPrefs.DeleteKey("AP_ModelVillage");
            PlayerPrefs.DeleteKey("AP_GoldenBell");
            PlayerPrefs.DeleteKey("AP_LastItemIndex");  // Reset item tracking
            PlayerPrefs.DeleteKey("AP_SpeedyFeet");     // Reset progressive items
            PlayerPrefs.DeleteKey("AP_MegaHonk");
            PlayerPrefs.DeleteKey("AP_GooseDays");
            PlayerPrefs.DeleteKey("AP_SilentSteps");
            PlayerPrefs.Save();
        }
        
        // === COROUTINES ===
        
        private IEnumerator DelayedGateInit()
        {
            // Wait longer for game to fully initialize
            yield return new WaitForSeconds(3f);
            
            // Run clearing multiple times to make sure it sticks
            for (int attempt = 0; attempt < 3; attempt++)
            {
                ClearHubBlockers();
                yield return new WaitForSeconds(1f);
            }
            
            // Sync area gates based on current access flags
            GateManager?.SyncGatesFromAccessFlags();
        }
        
        private void ClearHubBlockers()
        {
            // Use GateManager's method which has the actual blocker paths
            GateManager?.ClearHubBlockers();
            
            // Also scan for any remaining GateExtraColliders and disable them
            var allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                
                // Disable any GateExtraColliders anywhere
                if (obj.name.Contains("ExtraCollider"))
                {
                    var cols = obj.GetComponentsInChildren<Collider>();
                    foreach (var c in cols) c.enabled = false;
                    obj.SetActive(false);
                }
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
        
        
        private void OnDestroy()
        {
            Client?.Disconnect();
            harmony?.UnpatchSelf();
        }
    }
}