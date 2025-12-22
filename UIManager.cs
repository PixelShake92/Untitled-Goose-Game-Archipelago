using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace GooseGameAP
{
    public class UIManager
    {
        private static ManualLogSource Log => Plugin.Log;
        
        private List<string> receivedItemNames = new List<string>();
        
        public string ServerAddress = "archipelago.gg";
        public string ServerPort = "38281";
        public string SlotName = "Goose";
        public string Password = "";
        public string Status = "Not connected";
        
        private string currentNotification = "";
        private float notificationTimer = 0f;
        
        // Soul tracker state
        private bool showSoulTracker = false;
        private Vector2 soulScrollPosition = Vector2.zero;
        
        // Soul tracker window position and dragging
        private Rect soulTrackerWindowRect;
        private bool isDraggingSoulTracker = false;
        private Vector2 dragOffset = Vector2.zero;
        private bool soulTrackerPositionInitialized = false;
        
        public void ShowNotification(string message)
        {
            currentNotification = message;
            notificationTimer = 4f;
        }
        
        public void AddReceivedItem(string itemName)
        {
            receivedItemNames.Add(itemName);
        }
        
        public void AddChatMessage(string message)
        {
            // Disabled - log removed
        }
        
        public void UpdateNotification(float deltaTime)
        {
            if (notificationTimer > 0)
                notificationTimer -= deltaTime;
        }
        
        public void ClearReceivedItems()
        {
            receivedItemNames.Clear();
        }
        
        public int ReceivedItemCount => receivedItemNames.Count;
        
        /// <summary>
        /// Toggle the soul tracker overlay (called from Plugin on F2)
        /// </summary>
        public void ToggleSoulTracker()
        {
            showSoulTracker = !showSoulTracker;
        }
        
        public bool IsSoulTrackerVisible => showSoulTracker;
        
        public void DrawUI(Plugin plugin)
        {
            // Window dimensions
            float winX = 15;
            float winY = 15;
            float winW = 750;
            float winH = 540;
            
            // Dark background
            GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.97f);
            GUI.DrawTexture(new Rect(winX, winY, winW, winH), Texture2D.whiteTexture);
            
            // Gold border
            GUI.color = new Color(0.7f, 0.55f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(winX, winY, winW, 3), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(winX, winY, 3, winH), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(winX, winY + winH - 3, winW, 3), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(winX + winW - 3, winY, 3, winH), Texture2D.whiteTexture);
            GUI.color = Color.white;
            
            float x = winX + 15;
            float y = winY + 12;
            float w = winW - 30;
            
            // === TITLE ===
            GUI.color = new Color(1f, 0.85f, 0.35f);
            GUI.Label(new Rect(x, y, w, 28), "<size=18><b>ARCHIPELAGO - UNTITLED GOOSE GAME</b></size>");
            GUI.color = Color.white;
            y += 32;
            
            // === CONNECTION ===
            GUI.Label(new Rect(x, y, 55, 24), "Server:");
            ServerAddress = GUI.TextField(new Rect(x + 60, y, 480, 24), ServerAddress);
            GUI.Label(new Rect(x + 545, y, 15, 24), ":");
            ServerPort = GUI.TextField(new Rect(x + 560, y, 100, 24), ServerPort);
            y += 28;
            
            GUI.Label(new Rect(x, y, 55, 24), "Slot:");
            SlotName = GUI.TextField(new Rect(x + 60, y, 600, 24), SlotName);
            y += 28;
            
            GUI.Label(new Rect(x, y, 70, 24), "Password:");
            Password = GUI.PasswordField(new Rect(x + 75, y, 585, 24), Password, '*');
            y += 32;
            
            // Buttons
            if (GUI.Button(new Rect(x, y, 150, 30), plugin.IsConnected ? "Connected!" : "Connect"))
            {
                if (!plugin.IsConnected) plugin.Connect();
            }
            if (GUI.Button(new Rect(x + 160, y, 140, 30), "Disconnect"))
            {
                plugin.Disconnect();
            }
            if (GUI.Button(new Rect(x + 310, y, 140, 30), "Sync Gates"))
            {
                plugin.GateManager?.SyncGatesFromAccessFlags();
                ShowNotification("Gates synced!");
            }
            if (GUI.Button(new Rect(x + 460, y, 100, 30), "Reset"))
            {
                plugin.ResetAllAccess();
                plugin.NPCManager?.RefreshNPCStates(); // Re-apply (all disabled now)
                ShowNotification("Progress reset!");
            }
            y += 36;
            
            // Status line
            GUI.color = plugin.IsConnected ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.5f, 0.3f);
            GUI.Label(new Rect(x, y, w, 22), "<size=13><b>" + Status + "</b></size>");
            GUI.color = Color.white;
            y += 22;
            
            // Notification
            if (notificationTimer > 0 && !string.IsNullOrEmpty(currentNotification))
            {
                GUI.color = new Color(1f, 0.9f, 0.2f);
                GUI.Label(new Rect(x, y, w, 20), "<size=12><b>▶ " + currentNotification + "</b></size>");
                GUI.color = Color.white;
            }
            y += 26;
            
            // === AREA ACCESS ===
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            GUI.Label(new Rect(x, y, w, 20), "<size=12><b>AREA ACCESS</b></size>");
            GUI.color = Color.white;
            y += 22;
            
            float col1 = x;
            float col2 = x + 240;
            float col3 = x + 480;
            
            DrawAccess(col1, y, "Garden", plugin.HasGardenAccess);
            DrawAccess(col2, y, "High Street", plugin.HasHighStreetAccess);
            DrawAccess(col3, y, "Back Gardens", plugin.HasBackGardensAccess);
            y += 20;
            DrawAccess(col1, y, "Pub", plugin.HasPubAccess);
            DrawAccess(col2, y, "Model Village", plugin.HasModelVillageAccess);
            DrawAccess(col3, y, "Golden Bell", plugin.HasGoldenBell);
            y += 26;
            
            // === BUFFS ===
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            GUI.Label(new Rect(x, y, w, 20), "<size=12><b>BUFFS & STATUS</b></size>");
            GUI.color = Color.white;
            y += 22;
            
            int speed = (int)(plugin.TrapManager.GetEffectiveSpeedMultiplier() * 100);
            string buffs = $"Speed: {speed}%  |  Silent: {(plugin.IsSilent ? "ON" : "OFF")}  |  Mega Honk: Lv{plugin.MegaHonkCount}";
            GUI.Label(new Rect(x, y, w, 18), buffs);
            y += 20;
            
            string trapText = plugin.TrapManager.GetActiveTrapText();
            if (!string.IsNullOrEmpty(trapText))
            {
                GUI.color = new Color(1f, 0.5f, 0.5f);
                GUI.Label(new Rect(x, y, w, 18), trapText);
                GUI.color = Color.white;
                y += 20;
            }
            
            GUI.Label(new Rect(x, y, w, 18), $"Locations: {plugin.CheckedLocationCount}  |  Items: {receivedItemNames.Count}");
            y += 26;
            
            // === RECENT ITEMS ===
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            GUI.Label(new Rect(x, y, w, 20), "<size=12><b>RECENT ITEMS</b></size>");
            GUI.color = Color.white;
            y += 22;
            
            int itemsToShow = Math.Min(4, receivedItemNames.Count);
            if (itemsToShow == 0)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUI.Label(new Rect(x + 8, y, w, 18), "No items yet");
                GUI.color = Color.white;
                y += 18;
            }
            else
            {
                for (int i = receivedItemNames.Count - itemsToShow; i < receivedItemNames.Count; i++)
                {
                    GUI.Label(new Rect(x + 8, y, w - 16, 18), "• " + receivedItemNames[i]);
                    y += 18;
                }
            }
            y += 10;
            
            // Controls
            GUI.color = new Color(0.55f, 0.55f, 0.55f);
            GUI.Label(new Rect(x, y, w, 18), "<size=11>G = Goose Day  |  C = Colour  |  Ctrl+C = Reset  |  F2 = Soul Tracker  |  F9 = Resync Gates</size>");
            GUI.color = Color.white;
        }
        
        /// <summary>
        /// Draw the soul tracker overlay (separate from main UI)
        /// </summary>
        public void DrawSoulTracker(Plugin plugin)
        {
            if (!showSoulTracker) return;
            
            // Window dimensions
            float winW = 380;
            float winH = 550;
            
            // Initialize position to top-right corner on first show
            if (!soulTrackerPositionInitialized)
            {
                soulTrackerWindowRect = new Rect(Screen.width - winW - 15, 15, winW, winH);
                soulTrackerPositionInitialized = true;
            }
            
            // Ensure window stays on screen
            soulTrackerWindowRect.x = Mathf.Clamp(soulTrackerWindowRect.x, 0, Screen.width - winW);
            soulTrackerWindowRect.y = Mathf.Clamp(soulTrackerWindowRect.y, 0, Screen.height - winH);
            soulTrackerWindowRect.width = winW;
            soulTrackerWindowRect.height = winH;
            
            // Handle window dragging
            HandleSoulTrackerDragging();
            
            float winX = soulTrackerWindowRect.x;
            float winY = soulTrackerWindowRect.y;
            
            // Semi-transparent dark background
            GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
            GUI.DrawTexture(new Rect(winX, winY, winW, winH), Texture2D.whiteTexture);
            
            // Purple/blue border for soul theme
            GUI.color = new Color(0.5f, 0.3f, 0.8f, 1f);
            GUI.DrawTexture(new Rect(winX, winY, winW, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(winX, winY, 2, winH), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(winX, winY + winH - 2, winW, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(winX + winW - 2, winY, 2, winH), Texture2D.whiteTexture);
            GUI.color = Color.white;
            
            float x = winX + 10;
            float y = winY + 8;
            float w = winW - 20;
            
            // Title bar (draggable area indicator)
            float titleBarHeight = 32;
            GUI.color = new Color(0.15f, 0.12f, 0.2f, 1f);
            GUI.DrawTexture(new Rect(winX + 2, winY + 2, winW - 4, titleBarHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
            
            // Title
            GUI.color = new Color(0.7f, 0.5f, 1f);
            GUI.Label(new Rect(x, y, w - 60, 28), "<size=18><b>👻 SOUL TRACKER</b></size>");
            GUI.color = Color.white;
            
            // Drag hint
            GUI.color = new Color(0.5f, 0.5f, 0.5f);
            GUI.Label(new Rect(winX + winW - 70, y + 4, 60, 20), "<size=10>[drag me]</size>");
            GUI.color = Color.white;
            
            y += 34;
            
            // Close hint
            GUI.color = new Color(0.5f, 0.5f, 0.5f);
            GUI.Label(new Rect(x, y, w, 18), "<size=12>Press F2 to close</size>");
            GUI.color = Color.white;
            y += 22;
            
            // Scroll view for souls
            float scrollHeight = winH - 70;
            float contentHeight = CalculateSoulContentHeight(plugin);
            
            soulScrollPosition = GUI.BeginScrollView(
                new Rect(winX + 5, y, winW - 10, scrollHeight),
                soulScrollPosition,
                new Rect(0, 0, winW - 30, contentHeight)
            );
            
            float scrollY = 0;
            
            // === NPC SOULS ===
            scrollY = DrawSoulSection(plugin, scrollY, w - 20, "NPC SOULS", GetNPCSouls(plugin));
            
            // === PROP SOULS BY AREA ===
            if (plugin.PropSoulsEnabled)
            {
                scrollY = DrawSoulSection(plugin, scrollY, w - 20, "GARDEN PROPS", GetGardenPropSouls(plugin));
                scrollY = DrawSoulSection(plugin, scrollY, w - 20, "HIGH STREET PROPS", GetHighStreetPropSouls(plugin));
                scrollY = DrawSoulSection(plugin, scrollY, w - 20, "BACK GARDENS PROPS", GetBackGardensPropSouls(plugin));
                scrollY = DrawSoulSection(plugin, scrollY, w - 20, "PUB PROPS", GetPubPropSouls(plugin));
                scrollY = DrawSoulSection(plugin, scrollY, w - 20, "MODEL VILLAGE PROPS", GetModelVillagePropSouls(plugin));
                scrollY = DrawSoulSection(plugin, scrollY, w - 20, "SHARED PROPS", GetSharedPropSouls(plugin));
            }
            else
            {
                GUI.color = new Color(0.4f, 0.8f, 0.4f);
                GUI.Label(new Rect(5, scrollY, w - 20, 24), "<size=14>Prop Souls: DISABLED (all props available)</size>");
                GUI.color = Color.white;
            }
            
            GUI.EndScrollView();
        }
        
        /// <summary>
        /// Handle mouse dragging for the soul tracker window
        /// </summary>
        private void HandleSoulTrackerDragging()
        {
            Event e = Event.current;
            
            // Title bar area for dragging
            float titleBarHeight = 36;
            Rect titleBarRect = new Rect(
                soulTrackerWindowRect.x, 
                soulTrackerWindowRect.y, 
                soulTrackerWindowRect.width, 
                titleBarHeight
            );
            
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (titleBarRect.Contains(e.mousePosition))
                {
                    isDraggingSoulTracker = true;
                    dragOffset = e.mousePosition - new Vector2(soulTrackerWindowRect.x, soulTrackerWindowRect.y);
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDraggingSoulTracker = false;
            }
            else if (e.type == EventType.MouseDrag && isDraggingSoulTracker)
            {
                soulTrackerWindowRect.x = e.mousePosition.x - dragOffset.x;
                soulTrackerWindowRect.y = e.mousePosition.y - dragOffset.y;
                e.Use();
            }
        }
        
        private float CalculateSoulContentHeight(Plugin plugin)
        {
            float height = 0;
            
            // NPC souls section (header 26 + 11 NPCs * 22 + spacing 10)
            height += 26 + (11 * 22) + 10;
            
            if (plugin.PropSoulsEnabled)
            {
                // Each prop section (header 26 + items * 22 + spacing 10)
                height += 26 + (20 * 22) + 10; // Garden (~20 props)
                height += 26 + (24 * 22) + 10; // High Street
                height += 26 + (25 * 22) + 10; // Back Gardens
                height += 26 + (22 * 22) + 10; // Pub
                height += 26 + (12 * 22) + 10; // Model Village
                height += 26 + (26 * 22) + 10; // Shared
            }
            else
            {
                height += 30;
            }
            
            return height + 50; // Extra padding
        }
        
        private float DrawSoulSection(Plugin plugin, float startY, float width, string title, List<SoulInfo> souls)
        {
            float y = startY;
            
            // Count collected
            int collected = 0;
            foreach (var soul in souls)
            {
                if (soul.HasSoul) collected++;
            }
            
            // Section header with count
            GUI.color = new Color(0.6f, 0.7f, 0.9f);
            GUI.Label(new Rect(5, y, width, 24), $"<size=14><b>{title}</b> ({collected}/{souls.Count})</size>");
            GUI.color = Color.white;
            y += 26;
            
            // Draw each soul
            foreach (var soul in souls)
            {
                if (soul.HasSoul)
                {
                    GUI.color = new Color(0.3f, 1f, 0.4f);
                    GUI.Label(new Rect(10, y, width - 10, 22), "<size=14>✓ " + soul.Name + "</size>");
                }
                else
                {
                    GUI.color = new Color(0.45f, 0.45f, 0.45f);
                    GUI.Label(new Rect(10, y, width - 10, 22), "<size=14>✗ " + soul.Name + "</size>");
                }
                GUI.color = Color.white;
                y += 22;
            }
            
            y += 8; // Section spacing
            return y;
        }
        
        private struct SoulInfo
        {
            public string Name;
            public bool HasSoul;
            
            public SoulInfo(string name, bool hasSoul)
            {
                Name = name;
                HasSoul = hasSoul;
            }
        }
        
        private List<SoulInfo> GetNPCSouls(Plugin plugin)
        {
            return new List<SoulInfo>
            {
                new SoulInfo("Groundskeeper", plugin.HasGroundskeeperSoul),
                new SoulInfo("Boy", plugin.HasBoySoul),
                new SoulInfo("TV Shop Owner", plugin.HasTVShopOwnerSoul),
                new SoulInfo("Market Lady", plugin.HasMarketLadySoul),
                new SoulInfo("Tidy Neighbour", plugin.HasTidyNeighbourSoul),
                new SoulInfo("Messy Neighbour", plugin.HasMessyNeighbourSoul),
                new SoulInfo("Burly Man", plugin.HasBurlyManSoul),
                new SoulInfo("Old Man", plugin.HasOldManSoul),
                new SoulInfo("Pub Lady", plugin.HasPubLadySoul),
                new SoulInfo("Fancy Ladies", plugin.HasFancyLadiesSoul),
                new SoulInfo("Cook", plugin.HasCookSoul),
            };
        }
        
        private List<SoulInfo> GetGardenPropSouls(Plugin plugin)
        {
            var propMgr = plugin.PropManager;
            return new List<SoulInfo>
            {
                new SoulInfo("Radio", propMgr?.HasSoul("Radio Soul") ?? false),
                new SoulInfo("Trowel", propMgr?.HasSoul("Trowel Soul") ?? false),
                new SoulInfo("Keys", propMgr?.HasSoul("Keys Soul") ?? false),
                new SoulInfo("Tulip", propMgr?.HasSoul("Tulip Soul") ?? false),
                new SoulInfo("Jam", propMgr?.HasSoul("Jam Soul") ?? false),
                new SoulInfo("Picnic Mug", propMgr?.HasSoul("Picnic Mug Soul") ?? false),
                new SoulInfo("Thermos", propMgr?.HasSoul("Thermos Soul") ?? false),
                new SoulInfo("Straw Hat", propMgr?.HasSoul("Straw Hat Soul") ?? false),
                new SoulInfo("Drink Can", propMgr?.HasSoul("Drink Can Soul") ?? false),
                new SoulInfo("Tennis Ball", propMgr?.HasSoul("Tennis Ball Soul") ?? false),
                new SoulInfo("Gardener Hat", propMgr?.HasSoul("Gardener Hat Soul") ?? false),
                new SoulInfo("Rake", propMgr?.HasSoul("Rake Soul") ?? false),
                new SoulInfo("Picnic Basket", propMgr?.HasSoul("Picnic Basket Soul") ?? false),
                new SoulInfo("Esky", propMgr?.HasSoul("Esky Soul") ?? false),
                new SoulInfo("Shovel", propMgr?.HasSoul("Shovel Soul") ?? false),
                new SoulInfo("Watering Can", propMgr?.HasSoul("Watering Can Soul") ?? false),
                new SoulInfo("Fence Bolt", propMgr?.HasSoul("Fence Bolt Soul") ?? false),
                new SoulInfo("Mallet", propMgr?.HasSoul("Mallet Soul") ?? false),
                new SoulInfo("Wooden Crate", propMgr?.HasSoul("Wooden Crate Soul") ?? false),
                new SoulInfo("Gardener Sign", propMgr?.HasSoul("Gardener Sign Soul") ?? false),
            };
        }
        
        private List<SoulInfo> GetHighStreetPropSouls(Plugin plugin)
        {
            var propMgr = plugin.PropManager;
            return new List<SoulInfo>
            {
                new SoulInfo("Boy's Glasses", propMgr?.HasSoul("Boy's Glasses Soul") ?? false),
                new SoulInfo("Horn-Rimmed Glasses", propMgr?.HasSoul("Horn-Rimmed Glasses Soul") ?? false),
                new SoulInfo("Red Glasses", propMgr?.HasSoul("Red Glasses Soul") ?? false),
                new SoulInfo("Sunglasses", propMgr?.HasSoul("Sunglasses Soul") ?? false),
                new SoulInfo("Toilet Paper", propMgr?.HasSoul("Toilet Paper Soul") ?? false),
                new SoulInfo("Toy Car", propMgr?.HasSoul("Toy Car Soul") ?? false),
                new SoulInfo("Hairbrush", propMgr?.HasSoul("Hairbrush Soul") ?? false),
                new SoulInfo("Toothbrush", propMgr?.HasSoul("Toothbrush Soul") ?? false),
                new SoulInfo("Stereoscope", propMgr?.HasSoul("Stereoscope Soul") ?? false),
                new SoulInfo("Dish Soap Bottle", propMgr?.HasSoul("Dish Soap Bottle Soul") ?? false),
                new SoulInfo("Spray Bottle", propMgr?.HasSoul("Spray Bottle Soul") ?? false),
                new SoulInfo("Weed Tool", propMgr?.HasSoul("Weed Tool Soul") ?? false),
                new SoulInfo("Lily Flower", propMgr?.HasSoul("Lily Flower Soul") ?? false),
                new SoulInfo("Fusilage", propMgr?.HasSoul("Fusilage Soul") ?? false),
                new SoulInfo("Coin", propMgr?.HasSoul("Coin Soul") ?? false),
                new SoulInfo("Chalk", propMgr?.HasSoul("Chalk Soul") ?? false),
                new SoulInfo("Dustbin Lid", propMgr?.HasSoul("Dustbin Lid Soul") ?? false),
                new SoulInfo("Shopping Basket", propMgr?.HasSoul("Shopping Basket Soul") ?? false),
                new SoulInfo("Push Broom", propMgr?.HasSoul("Push Broom Soul") ?? false),
                new SoulInfo("Broken Broom Head", propMgr?.HasSoul("Broken Broom Head Soul") ?? false),
                new SoulInfo("Dustbin", propMgr?.HasSoul("Dustbin Soul") ?? false),
                new SoulInfo("Baby Doll", propMgr?.HasSoul("Baby Doll Soul") ?? false),
                new SoulInfo("Pricing Gun", propMgr?.HasSoul("Pricing Gun Soul") ?? false),
                new SoulInfo("Adding Machine", propMgr?.HasSoul("Adding Machine Soul") ?? false),
            };
        }
        
        private List<SoulInfo> GetBackGardensPropSouls(Plugin plugin)
        {
            var propMgr = plugin.PropManager;
            return new List<SoulInfo>
            {
                new SoulInfo("Dummy", propMgr?.HasSoul("Dummy Soul") ?? false),
                new SoulInfo("Cricket Ball", propMgr?.HasSoul("Cricket Ball Soul") ?? false),
                new SoulInfo("Bust Pipe", propMgr?.HasSoul("Bust Pipe Soul") ?? false),
                new SoulInfo("Bust Hat", propMgr?.HasSoul("Bust Hat Soul") ?? false),
                new SoulInfo("Bust Glasses", propMgr?.HasSoul("Bust Glasses Soul") ?? false),
                new SoulInfo("Tea Cup", propMgr?.HasSoul("Tea Cup Soul") ?? false),
                new SoulInfo("Newspaper", propMgr?.HasSoul("Newspaper Soul") ?? false),
                new SoulInfo("Badminton Racket", propMgr?.HasSoul("Badminton Racket Soul") ?? false),
                new SoulInfo("Pot Stack", propMgr?.HasSoul("Pot Stack Soul") ?? false),
                new SoulInfo("Soap", propMgr?.HasSoul("Soap Soul") ?? false),
                new SoulInfo("Paintbrush", propMgr?.HasSoul("Paintbrush Soul") ?? false),
                new SoulInfo("Vase", propMgr?.HasSoul("Vase Soul") ?? false),
                new SoulInfo("Right Strap", propMgr?.HasSoul("Right Strap Soul") ?? false),
                new SoulInfo("Rose", propMgr?.HasSoul("Rose Soul") ?? false),
                new SoulInfo("Rose Box", propMgr?.HasSoul("Rose Box Soul") ?? false),
                new SoulInfo("Cricket Bat", propMgr?.HasSoul("Cricket Bat Soul") ?? false),
                new SoulInfo("Tea Pot", propMgr?.HasSoul("Tea Pot Soul") ?? false),
                new SoulInfo("Clippers", propMgr?.HasSoul("Clippers Soul") ?? false),
                new SoulInfo("Duck Statue", propMgr?.HasSoul("Duck Statue Soul") ?? false),
                new SoulInfo("Frog Statue", propMgr?.HasSoul("Frog Statue Soul") ?? false),
                new SoulInfo("Jeremy Fish", propMgr?.HasSoul("Jeremy Fish Soul") ?? false),
                new SoulInfo("Messy Sign", propMgr?.HasSoul("Messy Sign Soul") ?? false),
                new SoulInfo("Drawer", propMgr?.HasSoul("Drawer Soul") ?? false),
                new SoulInfo("Enamel Jug", propMgr?.HasSoul("Enamel Jug Soul") ?? false),
                new SoulInfo("Clean Sign", propMgr?.HasSoul("Clean Sign Soul") ?? false),
            };
        }
        
        private List<SoulInfo> GetPubPropSouls(Plugin plugin)
        {
            var propMgr = plugin.PropManager;
            return new List<SoulInfo>
            {
                new SoulInfo("Fishing Bobber", propMgr?.HasSoul("Fishing Bobber Soul") ?? false),
                new SoulInfo("Exit Letter", propMgr?.HasSoul("Exit Letter Soul") ?? false),
                new SoulInfo("Pint Glass", propMgr?.HasSoul("Pint Glass Soul") ?? false),
                new SoulInfo("Toy Boat", propMgr?.HasSoul("Toy Boat Soul") ?? false),
                new SoulInfo("Wooly Hat", propMgr?.HasSoul("Wooly Hat Soul") ?? false),
                new SoulInfo("Pepper Grinder", propMgr?.HasSoul("Pepper Grinder Soul") ?? false),
                new SoulInfo("Pub Cloth", propMgr?.HasSoul("Pub Cloth Soul") ?? false),
                new SoulInfo("Cork", propMgr?.HasSoul("Cork Soul") ?? false),
                new SoulInfo("Candlestick", propMgr?.HasSoul("Candlestick Soul") ?? false),
                new SoulInfo("Flower for Vase", propMgr?.HasSoul("Flower for Vase Soul") ?? false),
                new SoulInfo("Harmonica", propMgr?.HasSoul("Harmonica Soul") ?? false),
                new SoulInfo("Tackle Box", propMgr?.HasSoul("Tackle Box Soul") ?? false),
                new SoulInfo("Traffic Cone", propMgr?.HasSoul("Traffic Cone Soul") ?? false),
                new SoulInfo("Exit Parcel", propMgr?.HasSoul("Exit Parcel Soul") ?? false),
                new SoulInfo("Stealth Box", propMgr?.HasSoul("Stealth Box Soul") ?? false),
                new SoulInfo("No Goose Sign", propMgr?.HasSoul("No Goose Sign Soul") ?? false),
                new SoulInfo("Portable Stool", propMgr?.HasSoul("Portable Stool Soul") ?? false),
                new SoulInfo("Dartboard", propMgr?.HasSoul("Dartboard Soul") ?? false),
                new SoulInfo("Mop Bucket", propMgr?.HasSoul("Mop Bucket Soul") ?? false),
                new SoulInfo("Mop", propMgr?.HasSoul("Mop Soul") ?? false),
                new SoulInfo("Delivery Box", propMgr?.HasSoul("Delivery Box Soul") ?? false),
                new SoulInfo("Burly Mans Bucket", propMgr?.HasSoul("Burly Mans Bucket Soul") ?? false),
            };
        }
        
        private List<SoulInfo> GetModelVillagePropSouls(Plugin plugin)
        {
            var propMgr = plugin.PropManager;
            return new List<SoulInfo>
            {
                new SoulInfo("Mini Mail Pillar", propMgr?.HasSoul("Mini Mail Pillar Soul") ?? false),
                new SoulInfo("Mini Phone Door", propMgr?.HasSoul("Mini Phone Door Soul") ?? false),
                new SoulInfo("Mini Shovel", propMgr?.HasSoul("Mini Shovel Soul") ?? false),
                new SoulInfo("Poppy Flower", propMgr?.HasSoul("Poppy Flower Soul") ?? false),
                new SoulInfo("Timber Handle", propMgr?.HasSoul("Timber Handle Soul") ?? false),
                new SoulInfo("Birdbath", propMgr?.HasSoul("Birdbath Soul") ?? false),
                new SoulInfo("Easel", propMgr?.HasSoul("Easel Soul") ?? false),
                new SoulInfo("Mini Bench", propMgr?.HasSoul("Mini Bench Soul") ?? false),
                new SoulInfo("Mini Pump", propMgr?.HasSoul("Mini Pump Soul") ?? false),
                new SoulInfo("Mini Street Bench", propMgr?.HasSoul("Mini Street Bench Soul") ?? false),
                new SoulInfo("Sun Lounge", propMgr?.HasSoul("Sun Lounge Soul") ?? false),
                new SoulInfo("Golden Bell", propMgr?.HasSoul("Golden Bell Soul") ?? false),
            };
        }
        
        private List<SoulInfo> GetSharedPropSouls(Plugin plugin)
        {
            var propMgr = plugin.PropManager;
            return new List<SoulInfo>
            {
                new SoulInfo("Carrot", propMgr?.HasSoul("Carrot Soul") ?? false),
                new SoulInfo("Tomato", propMgr?.HasSoul("Tomato Soul") ?? false),
                new SoulInfo("Pumpkin", propMgr?.HasSoul("Pumpkin Soul") ?? false),
                new SoulInfo("Topsoil Bag", propMgr?.HasSoul("Topsoil Bag Soul") ?? false),
                new SoulInfo("Quoit", propMgr?.HasSoul("Quoit Soul") ?? false),
                new SoulInfo("Plate", propMgr?.HasSoul("Plate Soul") ?? false),
                new SoulInfo("Orange", propMgr?.HasSoul("Orange Soul") ?? false),
                new SoulInfo("Leek", propMgr?.HasSoul("Leek Soul") ?? false),
                new SoulInfo("Cucumber", propMgr?.HasSoul("Cucumber Soul") ?? false),
                new SoulInfo("Dart", propMgr?.HasSoul("Dart Soul") ?? false),
                new SoulInfo("Umbrella", propMgr?.HasSoul("Umbrella Soul") ?? false),
                new SoulInfo("Spray Can", propMgr?.HasSoul("Spray Can Soul") ?? false),
                new SoulInfo("Sock", propMgr?.HasSoul("Sock Soul") ?? false),
                new SoulInfo("Pint Bottle", propMgr?.HasSoul("Pint Bottle Soul") ?? false),
                new SoulInfo("Knife", propMgr?.HasSoul("Knife Soul") ?? false),
                new SoulInfo("Gumboot", propMgr?.HasSoul("Gumboot Soul") ?? false),
                new SoulInfo("Fork", propMgr?.HasSoul("Fork Soul") ?? false),
                new SoulInfo("Vase Piece", propMgr?.HasSoul("Vase Piece Soul") ?? false),
                new SoulInfo("Apple Core", propMgr?.HasSoul("Apple Core Soul") ?? false),
                new SoulInfo("Apple", propMgr?.HasSoul("Apple Soul") ?? false),
                new SoulInfo("Sandwich", propMgr?.HasSoul("Sandwich Soul") ?? false),
                new SoulInfo("Slipper", propMgr?.HasSoul("Slipper Soul") ?? false),
                new SoulInfo("Bow", propMgr?.HasSoul("Bow Soul") ?? false),
                new SoulInfo("Walkie Talkie", propMgr?.HasSoul("Walkie Talkie Soul") ?? false),
                new SoulInfo("Boot", propMgr?.HasSoul("Boot Soul") ?? false),
                new SoulInfo("Mini Person", propMgr?.HasSoul("Mini Person Soul") ?? false),
            };
        }
        
        private void DrawAccess(float x, float y, string label, bool hasAccess)
        {
            if (hasAccess)
            {
                GUI.color = new Color(0.3f, 1f, 0.3f);
                GUI.Label(new Rect(x, y, 230, 18), "✓ " + label);
            }
            else
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUI.Label(new Rect(x, y, 230, 18), "✗ " + label);
            }
            GUI.color = Color.white;
        }
    }
}