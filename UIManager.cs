using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace GooseGameAP
{
    /// <summary>
    /// Handles all UI rendering and notifications
    /// </summary>
    public class UIManager
    {
        private static ManualLogSource Log => Plugin.Log;
        
        // Notification queue
        private List<string> chatMessages = new List<string>();
        private List<string> receivedItemNames = new List<string>();
        
        // Connection UI fields
        public string ServerAddress = "archipelago.gg";
        public string ServerPort = "38281";
        public string SlotName = "Goosel";
        public string Password = "";
        public string Status = "Not connected";
        
        public void ShowNotification(string message)
        {
            Log.LogInfo("[NOTIFICATION] " + message);
            chatMessages.Add(message);
            if (chatMessages.Count > 20) chatMessages.RemoveAt(0);
        }
        
        public void AddReceivedItem(string itemName)
        {
            receivedItemNames.Add(itemName);
        }
        
        public void AddChatMessage(string message)
        {
            chatMessages.Add(message);
            if (chatMessages.Count > 20) chatMessages.RemoveAt(0);
        }
        
        public void ClearReceivedItems()
        {
            receivedItemNames.Clear();
        }
        
        public int ReceivedItemCount => receivedItemNames.Count;
        
        public void DrawUI(Plugin plugin)
        {
            GUI.Box(new Rect(10, 10, 550, 620), "Archipelago - Goose Game v1.0");
            
            // Connection settings
            GUI.Label(new Rect(20, 40, 100, 20), "Server:");
            ServerAddress = GUI.TextField(new Rect(120, 40, 200, 20), ServerAddress);
            GUI.Label(new Rect(325, 40, 10, 20), ":");
            ServerPort = GUI.TextField(new Rect(340, 40, 60, 20), ServerPort);
            
            GUI.Label(new Rect(20, 65, 100, 20), "Slot Name:");
            SlotName = GUI.TextField(new Rect(120, 65, 280, 20), SlotName);
            
            GUI.Label(new Rect(20, 90, 100, 20), "Password:");
            Password = GUI.PasswordField(new Rect(120, 90, 280, 20), Password, '*');

            // Connection buttons
            if (GUI.Button(new Rect(20, 120, 180, 30), plugin.IsConnected ? "Connected!" : "Connect"))
            {
                if (!plugin.IsConnected) plugin.Connect();
            }
            
            if (GUI.Button(new Rect(210, 120, 90, 30), "Disconnect"))
            {
                plugin.Disconnect();
            }
            
            if (GUI.Button(new Rect(310, 120, 90, 30), "Sync Gates"))
            {
                plugin.GateManager.SyncGatesFromAccessFlags();
            }
            
            if (GUI.Button(new Rect(410, 120, 80, 30), "Reset"))
            {
                plugin.ResetAllAccess();
                ShowNotification("Access flags reset! Reconnect to sync.");
            }

            GUI.Label(new Rect(20, 158, 410, 20), Status);
            
            int y = 183;
            
            // Area access display
            GUI.Label(new Rect(20, y, 380, 20), "=== Area Access (Hub = Start) ===");
            y += 22;
            
            GUI.Label(new Rect(20, y, 380, 20), "Garden: " + (plugin.HasGardenAccess ? "YES" : "NO") + " | High Street: " + (plugin.HasHighStreetAccess ? "YES" : "NO"));
            y += 20;
            GUI.Label(new Rect(20, y, 380, 20), "Back Gardens: " + (plugin.HasBackGardensAccess ? "YES" : "NO") + " | Pub: " + (plugin.HasPubAccess ? "YES" : "NO"));
            y += 20;
            GUI.Label(new Rect(20, y, 380, 20), "Model Village: " + (plugin.HasModelVillageAccess ? "YES" : "NO") + " | Golden Bell: " + (plugin.HasGoldenBell ? "YES" : "NO"));
            
            // Buffs display
            y += 25;
            GUI.Label(new Rect(20, y, 380, 20), "=== Buffs/Effects ===");
            y += 22;
            GUI.Label(new Rect(20, y, 380, 20), "Speed: " + (plugin.TrapManager.GetEffectiveSpeedMultiplier() * 100).ToString("F0") + "% | Silent: " + plugin.IsSilent + " | Mega Honks: " + plugin.MegaHonkCount);
            y += 20;
            
            // Active traps
            string trapText = plugin.TrapManager.GetActiveTrapText();
            if (!string.IsNullOrEmpty(trapText))
            {
                GUI.Label(new Rect(20, y, 380, 20), trapText);
                y += 20;
            }
            
            // Stats
            y += 5;
            GUI.Label(new Rect(20, y, 380, 20), "Locations: " + plugin.CheckedLocationCount + " | Items: " + receivedItemNames.Count);
            
            // Recent items
            y += 25;
            GUI.Label(new Rect(20, y, 100, 20), "Recent Items:");
            y += 20;
            for (int i = Math.Max(0, receivedItemNames.Count - 4); i < receivedItemNames.Count; i++)
            {
                GUI.Label(new Rect(30, y, 370, 18), receivedItemNames[i]);
                y += 18;
            }
            
            // Chat messages
            y += 10;
            GUI.Label(new Rect(20, y, 100, 20), "Messages:");
            y += 20;
            for (int i = Math.Max(0, chatMessages.Count - 4); i < chatMessages.Count; i++)
            {
                GUI.Label(new Rect(30, y, 370, 18), chatMessages[i]);
                y += 18;
            }
            
            // Debug info
            y += 15;
            GUI.Label(new Rect(20, y, 420, 20), "=== Debug Keys ===");
            y += 20;
            GUI.Label(new Rect(20, y, 420, 20), "F6=Pos Shift+F6=Drag Alt+F6=Audio F7=Hub F8=Gates");
            y += 18;
            GUI.Label(new Rect(20, y, 420, 20), "Shift+F8=BlockerSearch Shift+F9=Draggables F9=Before F10=After");
            y += 18;
            GUI.Label(new Rect(20, y, 420, 20), "F11=OpenAll F12=TestPos Ctrl+1-5=Traps Ctrl+9=ResetDrag Ctrl+0=Clear");
        }
    }
}
