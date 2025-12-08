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
        
        // Area access flags
        public bool HasHighStreetAccess { get; set; } = false;
        public bool HasBackGardensAccess { get; set; } = false;
        public bool HasPubAccess { get; set; } = false;
        public bool HasModelVillageAccess { get; set; } = false;
        public bool HasGoldenBell { get; set; } = false;
        
        // Buff tracking
        public bool IsSilent { get; set; } = false;
        public int MegaHonkCount { get; set; } = 0;
        
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
            Log.LogInfo("Goose Game AP v1.0.0 - Refactored");
            
            // Initialize components
            UI = new UIManager();
            TrapManager = new TrapManager(this);
            GateManager = new GateManager(this);
            ItemTracker = new ItemTracker(this);
            Client = new ArchipelagoClient(this);
            
            harmony = new Harmony("com.archipelago.goosegame");
            harmony.PatchAll();
            Log.LogInfo("Harmony patches applied");
            
            LoadAccessFlags();
        }

        private void Update()
        {
            // Initialize gates once when game scene loads
            if (!hasInitializedGates && GameManager.instance != null && 
                GameManager.instance.allGeese != null && GameManager.instance.allGeese.Count > 0)
            {
                hasInitializedGates = true;
                Log.LogInfo("Game scene detected, starting initialization...");
                StartCoroutine(DelayedHubTeleport());
                StartCoroutine(DelayedGateInit());
            }
            
            // Toggle UI
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showUI = !showUI;
                Log.LogInfo("F1 pressed - UI is now: " + (showUI ? "VISIBLE" : "HIDDEN"));
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
            
            // Track pickups/drags
            ItemTracker?.Update();
        }
        
        private void HandleDebugKeys()
        {
            // F2-F5: Quick area unlocks
            if (Input.GetKeyDown(KeyCode.F2))
            {
                HasHighStreetAccess = true;
                GateManager?.OpenGatesForArea("HighStreet");
                UI?.ShowNotification("DEBUG: High Street unlocked!");
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                HasBackGardensAccess = true;
                GateManager?.OpenGatesForArea("Backyards");
                UI?.ShowNotification("DEBUG: Back Gardens unlocked!");
            }
            if (Input.GetKeyDown(KeyCode.F4))
            {
                HasPubAccess = true;
                GateManager?.OpenGatesForArea("Pub");
                UI?.ShowNotification("DEBUG: Pub unlocked!");
            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                HasModelVillageAccess = true;
                GateManager?.OpenGatesForArea("Finale");
                UI?.ShowNotification("DEBUG: Model Village unlocked!");
            }
            
            // F6: Position info
            if (Input.GetKeyDown(KeyCode.F6))
            {
                LogGoosePosition();
            }
            
            // F7: Hub teleport
            if (Input.GetKeyDown(KeyCode.F7))
            {
                GateManager?.TeleportGooseToWell();
                UI?.ShowNotification("Teleported to Hub!");
            }
            
            // F11: Open all gates
            if (Input.GetKeyDown(KeyCode.F11))
            {
                GateManager?.OpenAllGates();
            }
            
            // Ctrl keys for traps
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    TrapManager?.ActivateButterfingers(10f);
                }
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    TrapManager?.ActivateSuspicious(10f);
                }
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    TrapManager?.ActivateTired(30f);
                }
                if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    TrapManager?.ForceHonk();
                    UI?.ShowNotification("DEBUG: Force honk!");
                }
                if (Input.GetKeyDown(KeyCode.Alpha9))
                {
                    ItemTracker?.ResetDraggerCache();
                    UI?.ShowNotification("DEBUG: Dragger cache reset!");
                }
                if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    TrapManager?.ClearTraps();
                    UI?.ShowNotification("DEBUG: All traps cleared!");
                }
            }
        }
        
        private void HandleGateSyncTiming()
        {
            if (Client == null) return;
            
            // Handle pending gate sync
            if (Client.PendingGateSync && GameManager.instance != null && 
                GameManager.instance.allGeese != null && GameManager.instance.allGeese.Count > 0)
            {
                Client.GateSyncTimer += Time.deltaTime;
                if (Client.GateSyncTimer >= 2.0f)
                {
                    Client.PendingGateSync = false;
                    Client.GateSyncTimer = 0f;
                    GateManager?.SyncGatesFromAccessFlags();
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
            Log.LogInfo("Processing item offset: " + offset);
            
            switch (offset)
            {
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
                    MegaHonkCount++;
                    UI?.ShowNotification("MEGA HONK! (cosmetic - x" + MegaHonkCount + ")");
                    break;
                case 201:
                    TrapManager.SpeedyFeetCount++;
                    UI?.ShowNotification("Speedy Feet! (cosmetic)");
                    break;
                case 202:
                    IsSilent = true;
                    UI?.ShowNotification("Silent Steps! (cosmetic)");
                    break;
                case 203:
                    UI?.ShowNotification("What a nice day to be a goose!");
                    break;
                    
                case 300:
                    TrapManager?.ActivateTired(30f);
                    break;
                case 301:
                    TrapManager?.ActivateClumsy(30f);
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
                Log.LogInfo("Sent location check for goal: " + goalName + " (ID: " + locationId.Value + ")");
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
                case GoalListArea.Garden: return true; // Always accessible
                case GoalListArea.Shops: return HasHighStreetAccess;
                case GoalListArea.Backyards: return HasBackGardensAccess;
                case GoalListArea.Pub: return HasPubAccess;
                case GoalListArea.Finale: return HasModelVillageAccess;
                default: return true;
            }
        }
        
        public void ResetAllAccess()
        {
            HasHighStreetAccess = false;
            HasBackGardensAccess = false;
            HasPubAccess = false;
            HasModelVillageAccess = false;
            HasGoldenBell = false;
            Client?.ClearReceivedItems();
            checkedLocations.Clear();
            ClearSavedAccessFlags();
        }
        
        // === PERSISTENCE ===
        
        private void SaveAccessFlags()
        {
            PlayerPrefs.SetInt("AP_HighStreet", HasHighStreetAccess ? 1 : 0);
            PlayerPrefs.SetInt("AP_BackGardens", HasBackGardensAccess ? 1 : 0);
            PlayerPrefs.SetInt("AP_Pub", HasPubAccess ? 1 : 0);
            PlayerPrefs.SetInt("AP_ModelVillage", HasModelVillageAccess ? 1 : 0);
            PlayerPrefs.SetInt("AP_GoldenBell", HasGoldenBell ? 1 : 0);
            PlayerPrefs.Save();
            Log.LogInfo("[SAVE] Access flags saved to PlayerPrefs");
        }
        
        private void LoadAccessFlags()
        {
            if (PlayerPrefs.HasKey("AP_HighStreet"))
            {
                HasHighStreetAccess = PlayerPrefs.GetInt("AP_HighStreet") == 1;
                HasBackGardensAccess = PlayerPrefs.GetInt("AP_BackGardens") == 1;
                HasPubAccess = PlayerPrefs.GetInt("AP_Pub") == 1;
                HasModelVillageAccess = PlayerPrefs.GetInt("AP_ModelVillage") == 1;
                HasGoldenBell = PlayerPrefs.GetInt("AP_GoldenBell") == 1;
                Log.LogInfo("[LOAD] Access flags loaded from PlayerPrefs");
            }
        }
        
        private void ClearSavedAccessFlags()
        {
            PlayerPrefs.DeleteKey("AP_HighStreet");
            PlayerPrefs.DeleteKey("AP_BackGardens");
            PlayerPrefs.DeleteKey("AP_Pub");
            PlayerPrefs.DeleteKey("AP_ModelVillage");
            PlayerPrefs.DeleteKey("AP_GoldenBell");
            PlayerPrefs.Save();
            Log.LogInfo("[CLEAR] Saved access flags cleared");
        }
        
        // === COROUTINES ===
        
        private IEnumerator DelayedHubTeleport()
        {
            yield return new WaitForSeconds(3f);
            GateManager?.TeleportGooseToWell();
            UI?.ShowNotification("Welcome to the Hub!");
        }
        
        private IEnumerator DelayedGateInit()
        {
            yield return new WaitForSeconds(2f);
            Log.LogInfo("Running delayed gate initialization...");
            
            // Disable the hub-to-garden blocker
            var parkHubBlocker = GameObject.Find("highStreetDynamic/GROUP_Garage/irongate/GateSystem/GateExtraColliders/ParkHubGateExtraCollider");
            if (parkHubBlocker != null)
            {
                parkHubBlocker.SetActive(false);
                Log.LogInfo("Disabled ParkHubGateExtraCollider (hub-to-garden path)");
            }
            
            Log.LogInfo("Initial gate setup complete");
        }
        
        private void LogGoosePosition()
        {
            if (GameManager.instance?.allGeese == null) return;
            foreach (var goose in GameManager.instance.allGeese)
            {
                if (goose != null && goose.isActiveAndEnabled)
                {
                    var pos = goose.transform.position;
                    Log.LogInfo($"Goose position: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");
                    UI?.ShowNotification($"Pos: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");
                }
            }
        }
        
        private void OnDestroy()
        {
            Client?.Disconnect();
            harmony?.UnpatchSelf();
        }
    }
}
