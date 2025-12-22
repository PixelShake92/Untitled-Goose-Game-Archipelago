using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace GooseGameAP
{
    /// <summary>
    /// Manages prop visibility based on prop soul items.
    /// Props are disabled until their corresponding soul is received.
    /// When PropSoulsEnabled is false, all props are always accessible.
    /// </summary>
    public class PropManager
    {
        private static ManualLogSource Log => Plugin.Log;
        private Plugin plugin;
        
        // Track which souls we have received
        private HashSet<string> receivedSouls = new HashSet<string>();
        
        // Cache of props by soul type
        private Dictionary<string, List<GameObject>> propCache = new Dictionary<string, List<GameObject>>();
        
        private bool hasScannedProps = false;
        
        // Map item name patterns to soul names
        private static readonly Dictionary<string, string> PropToSoul = new Dictionary<string, string>
        {
            // Grouped souls - items with multiple instances
            { "carrot", "Carrot Soul" },
            { "tomato", "Tomato Soul" },
            { "pumpkin", "Pumpkin Soul" },
            { "topsoilbag", "Topsoil Bag Soul" },
            { "topsoil", "Topsoil Bag Soul" },
            { "fertiliser", "Topsoil Bag Soul" },
            { "fertliser", "Topsoil Bag Soul" },  // Typo variant
            { "fertilizer", "Topsoil Bag Soul" },
            { "quoit", "Quoit Soul" },
            { "plate", "Plate Soul" },
            { "orange", "Orange Soul" },
            { "leek", "Leek Soul" },
            { "cucumber", "Cucumber Soul" },
            { "dart", "Dart Soul" },
            { "umbrella", "Umbrella Soul" },
            { "bluecan", "Spray Can Soul" },
            { "orangecan", "Spray Can Soul" },
            { "yellowcan", "Spray Can Soul" },
            { "canblue", "Spray Can Soul" },
            { "canorange", "Spray Can Soul" },
            { "canyellow", "Spray Can Soul" },
            { "sock", "Sock Soul" },
            { "pintbottle", "Pint Bottle Soul" },
            { "knife", "Knife Soul" },
            { "gumboot", "Gumboot Soul" },
            { "fork", "Fork Soul" },
            { "brokenvasepiece", "Vase Piece Soul" },
            { "brokenbit", "Vase Piece Soul" },
            { "applecore", "Apple Core Soul" },
            { "apple", "Apple Soul" },
            { "sandwich", "Sandwich Soul" },
            { "slipper", "Slipper Soul" },
            { "bow", "Bow Soul" },
            { "walkietalkie", "Walkie Talkie Soul" },
            { "boot", "Boot Soul" },
            { "miniperson", "Mini Person Soul" },
            
            // Garden one-offs
            { "radio", "Radio Soul" },
            { "trowel", "Trowel Soul" },
            { "keys", "Keys Soul" },
            { "keyring", "Keys Soul" },
            { "carkeys", "Keys Soul" },
            { "tulip", "Tulip Soul" },
            { "jam", "Jam Soul" },
            { "picnicmug", "Picnic Mug Soul" },
            { "thermos", "Thermos Soul" },
            { "strawhat", "Straw Hat Soul" },
            { "sunhat", "Straw Hat Soul" },
            { "drinkcan", "Drink Can Soul" },
            { "tennisball", "Tennis Ball Soul" },
            { "gardenerhat", "Gardener Hat Soul" },
            { "gardenershat", "Gardener Hat Soul" },
            { "hatgardener", "Gardener Hat Soul" },
            { "gardenerssunhat", "Gardener Hat Soul" },
            { "rake", "Rake Soul" },
            { "picnicbasket", "Picnic Basket Soul" },
            { "picnicbaskethandle", "Picnic Basket Soul" },
            { "baskethandle", "Picnic Basket Soul" },
            { "esky", "Esky Soul" },
            { "coolbox", "Esky Soul" },
            { "shovel", "Shovel Soul" },
            { "wateringcan", "Watering Can Soul" },
            { "fencebolt", "Fence Bolt Soul" },
            { "boltbent", "Fence Bolt Soul" },
            { "mallet", "Mallet Soul" },
            { "woodencrate", "Wooden Crate Soul" },
            { "cratewooden", "Wooden Crate Soul" },
            { "gardenersign", "Gardener Sign Soul" },
            
            // High Street one-offs
            { "boysglasses", "Boy's Glasses Soul" },
            { "boyglasses", "Boy's Glasses Soul" },
            { "glassesboy", "Boy's Glasses Soul" },
            { "wimpglasses", "Boy's Glasses Soul" },
            { "hornrimmedglasses", "Horn-Rimmed Glasses Soul" },
            { "redglasses", "Red Glasses Soul" },
            { "sunglasses", "Sunglasses Soul" },
            { "toiletpaper", "Toilet Paper Soul" },
            { "toycar", "Toy Car Soul" },
            { "hairbrush", "Hairbrush Soul" },
            { "toothbrush", "Toothbrush Soul" },
            { "stereoscope", "Stereoscope Soul" },
            { "dishsoapbottle", "Dish Soap Bottle Soul" },
            { "dishwashbottle", "Dish Soap Bottle Soul" },
            { "spraybottle", "Spray Bottle Soul" },
            { "weedtool", "Weed Tool Soul" },
            { "lilyflower", "Lily Flower Soul" },
            { "fusilage", "Fusilage Soul" },
            { "coin", "Coin Soul" },
            { "chalk", "Chalk Soul" },
            { "dustbinlid", "Dustbin Lid Soul" },
            { "shoppingbasket", "Shopping Basket Soul" },
            { "shopbasket", "Shopping Basket Soul" },
            { "basketprop", "Picnic Basket Soul" },
            { "pushbroom", "Push Broom Soul" },
            { "brokenbroomhead", "Broken Broom Head Soul" },
            { "broomheadseperate", "Broken Broom Head Soul" },
            { "dustbin", "Dustbin Soul" },
            { "babydoll", "Baby Doll Soul" },
            { "pricinggun", "Pricing Gun Soul" },
            { "addingmachine", "Adding Machine Soul" },
            
            // Back Gardens one-offs
            { "bra", "Bra Soul" },
            { "dummy", "Dummy Soul" },
            { "cricketball", "Cricket Ball Soul" },
            { "bustpipe", "Bust Pipe Soul" },
            { "busthat", "Bust Hat Soul" },
            { "bustglasses", "Bust Glasses Soul" },
            { "teacup", "Tea Cup Soul" },
            { "newspaper", "Newspaper Soul" },
            { "badmintonracket", "Badminton Racket Soul" },
            { "potstack", "Pot Stack Soul" },
            { "soap", "Soap Soul" },
            { "paintbrush", "Paintbrush Soul" },
            { "vase", "Vase Soul" },
            { "rightstrap", "Right Strap Soul" },
            { "rose", "Rose Soul" },
            { "rosebox", "Rose Box Soul" },
            { "cricketbat", "Cricket Bat Soul" },
            { "teapot", "Tea Pot Soul" },
            { "clippers", "Clippers Soul" },
            { "duckstatue", "Duck Statue Soul" },
            { "duck", "Duck Statue Soul" },
            { "frogstatue", "Frog Statue Soul" },
            { "frog", "Frog Statue Soul" },
            { "jeremyfish", "Jeremy Fish Soul" },
            { "messysign", "Messy Sign Soul" },
            { "drawer", "Drawer Soul" },
            { "enameljug", "Enamel Jug Soul" },
            { "jugenamel", "Enamel Jug Soul" },
            { "cleansign", "Clean Sign Soul" },
            
            // Pub one-offs
            { "fishingbobber", "Fishing Bobber Soul" },
            { "exitletter", "Exit Letter Soul" },
            { "pintglass", "Pint Glass Soul" },
            { "toyboat", "Toy Boat Soul" },
            { "woolyhat", "Wooly Hat Soul" },
            { "woollyhat", "Wooly Hat Soul" },
            { "peppergrinder", "Pepper Grinder Soul" },
            { "pubcloth", "Pub Cloth Soul" },
            { "cork", "Cork Soul" },
            { "candlestick", "Candlestick Soul" },
            { "flowerforvase", "Flower for Vase Soul" },
            { "harmonica", "Harmonica Soul" },
            { "tacklebox", "Tackle Box Soul" },
            { "trafficcone", "Traffic Cone Soul" },
            { "coneprop", "Traffic Cone Soul" },
            { "exitparcel", "Exit Parcel Soul" },
            { "stealthbox", "Stealth Box Soul" },
            { "nogoosesign", "No Goose Sign Soul" },
            { "pubnogoose", "No Goose Sign Soul" },
            { "portablestool", "Portable Stool Soul" },
            { "dartboard", "Dartboard Soul" },
            { "mopbucket", "Mop Bucket Soul" },
            { "pail", "Burly Mans Bucket Soul" },
            { "bucket", "Mop Bucket Soul" },
            { "mop", "Mop Soul" },
            { "mophandle", "Mop Soul" },
            { "mophead", "Mop Soul" },
            { "deliverybox", "Delivery Box Soul" },
            
            // Model Village one-offs
            { "minimailpillar", "Mini Mail Pillar Soul" },
            { "miniphonedoor", "Mini Phone Door Soul" },
            { "minishovel", "Mini Shovel Soul" },
            { "poppyflower", "Poppy Flower Soul" },
            { "flowerpoppy", "Poppy Flower Soul" },
            { "timberhandle", "Timber Handle Soul" },
            { "birdbath", "Birdbath Soul" },
            { "easel", "Easel Soul" },
            { "minibench", "Mini Bench Soul" },
            { "minipump", "Mini Pump Soul" },
            { "ministreetbench", "Mini Street Bench Soul" },
            { "streetbench", "Mini Street Bench Soul" },
            { "benchstreet", "Mini Street Bench Soul" },
            { "sunlounge", "Sun Lounge Soul" },
            
            // Victory item
            { "goldenbell", "Golden Bell Soul" },
        };
        
        public PropManager(Plugin plugin)
        {
            this.plugin = plugin;
        }
        
        /// <summary>
        /// Check if prop souls are enabled for this session
        /// </summary>
        private bool PropSoulsEnabled => plugin.PropSoulsEnabled;
        
        // Props that should ALWAYS be enabled (needed for basic progression)
        private static readonly HashSet<string> AlwaysEnabledProps = new HashSet<string>
        {
            "Fence Bolt Soul"  // Needed to open intro gate
        };
        
        /// <summary>
        /// Called every frame from Plugin.Update()
        /// </summary>
        public void Update()
        {
            try
            {
                if (GameManager.instance == null) return;
                if (GameManager.instance.allGeese == null) return;
                if (GameManager.instance.allGeese.Count == 0) return;
                
                if (!hasScannedProps)
                {
                    Log.LogInfo("[Prop] Game loaded - scanning for props");
                    ScanAndCacheProps();
                    
                    // Only disable props if prop souls are enabled
                    if (PropSoulsEnabled)
                    {
                        Log.LogInfo("[Prop] Prop souls ENABLED - disabling props until souls received");
                        DisableAllProps();
                        ApplyAllPropStates();
                    }
                    else
                    {
                        Log.LogInfo("[Prop] Prop souls DISABLED - all props accessible");
                        EnableAllProps();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"[Prop] Update error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Disable ALL cached props
        /// </summary>
        private void DisableAllProps()
        {
            int count = 0;
            foreach (var kvp in propCache)
            {
                foreach (var prop in kvp.Value)
                {
                    if (prop != null)
                    {
                        prop.SetActive(false);
                        count++;
                    }
                }
            }
            Log.LogInfo($"[Prop] Disabled {count} props");
        }
        
        /// <summary>
        /// Enable ALL cached props (when souls are disabled)
        /// </summary>
        private void EnableAllProps()
        {
            int count = 0;
            foreach (var kvp in propCache)
            {
                foreach (var prop in kvp.Value)
                {
                    if (prop != null)
                    {
                        prop.SetActive(true);
                        count++;
                    }
                }
            }
            Log.LogInfo($"[Prop] Enabled {count} props (souls disabled)");
        }
        
        /// <summary>
        /// Scan the game world for props and cache them by soul type
        /// </summary>
        private void ScanAndCacheProps()
        {
            if (hasScannedProps) return;
            hasScannedProps = true;
            
            propCache.Clear();
            
            try
            {
                var allProps = UnityEngine.Object.FindObjectsOfType<Prop>();
                Log.LogInfo($"[Prop] Found {allProps.Length} props total");
                
                int logCount = 0;
                foreach (var prop in allProps)
                {
                    if (prop == null) continue;
                    if (logCount < 20)
                    {
                        string cleanName = CleanPropName(prop.name);
                        Log.LogInfo($"[Prop DEBUG] Raw: '{prop.name}' -> Clean: '{cleanName}'");
                        logCount++;
                    }
                    
                    // Debug logging for problematic props
                    string lowerName = prop.name.ToLower();
                    if (lowerName.Contains("mop") || lowerName.Contains("basket") || 
                        lowerName.Contains("top") || lowerName.Contains("duck"))
                    {
                        string hierarchy = GetHierarchy(prop.transform);
                        Log.LogInfo($"[Prop HIERARCHY] {prop.name}: {hierarchy}");
                    }
                }
                
                int matched = 0;
                int unmatched = 0;
                
                foreach (var prop in allProps)
                {
                    if (prop == null) continue;
                    
                    string cleanName = CleanPropName(prop.name);
                    string soul = GetSoulForProp(cleanName);
                    
                    if (soul != null)
                    {
                        if (!propCache.ContainsKey(soul))
                            propCache[soul] = new List<GameObject>();
                        
                        // Get the appropriate object to disable - check for parent containers
                        GameObject objToCache = GetDisableTarget(prop.gameObject, cleanName);
                        propCache[soul].Add(objToCache);
                        matched++;
                    }
                    else
                    {
                        unmatched++;
                        Log.LogWarning($"[Prop] No soul match for: '{prop.name}' (cleaned: '{cleanName}')");
                    }
                }
                
                Log.LogInfo($"[Prop] Matched {matched} props, unmatched {unmatched}");
                
                foreach (var kvp in propCache)
                {
                    Log.LogInfo($"[Prop] {kvp.Key}: {kvp.Value.Count} props");
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"[Prop] ScanAndCacheProps error: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Get the appropriate GameObject to disable for this prop.
        /// Some props have parent containers that include handles, meshes, etc.
        /// </summary>
        private GameObject GetDisableTarget(GameObject propObj, string cleanName)
        {
            // For certain props, we need to disable a parent container
            // to get all child parts (handles, mesh renderers, physics)
            
            Transform current = propObj.transform;
            
            // Check if parent has a meaningful name that suggests it's the container
            // e.g., "mopProp" might be under "mop" parent with handle as sibling
            if (current.parent != null)
            {
                string parentName = current.parent.name.ToLower();
                
                // Mop - the mopProp is the handle, we need parent for both parts
                if (cleanName.Contains("mop") && !cleanName.Contains("bucket"))
                {
                    // Check if parent has multiple children (mop head + handle)
                    if (current.parent.childCount > 1)
                    {
                        Log.LogInfo($"[Prop] Using parent '{current.parent.name}' for mop (has {current.parent.childCount} children)");
                        return current.parent.gameObject;
                    }
                }
                
                // Picnic basket - BasketProp might have handle as sibling
                if (cleanName.Contains("basket") && parentName.Contains("picnic"))
                {
                    if (current.parent.childCount > 1)
                    {
                        Log.LogInfo($"[Prop] Using parent '{current.parent.name}' for picnic basket");
                        return current.parent.gameObject;
                    }
                }
                
                // Topsoil bags - check if there's a parent with mesh/collider
                if (cleanName.Contains("top") && (parentName.Contains("top") || parentName.Contains("soil") || parentName.Contains("fertil")))
                {
                    Log.LogInfo($"[Prop] Using parent '{current.parent.name}' for topsoil");
                    return current.parent.gameObject;
                }
            }
            
            // Default - just use the prop's own GameObject
            return propObj;
        }
        
        private string CleanPropName(string name)
        {
            string clean = name.ToLower()
                .Replace("(clone)", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(" ", "")
                .Replace("_", "")
                .Trim();
            
            while (clean.Length > 0 && char.IsDigit(clean[clean.Length - 1]))
                clean = clean.Substring(0, clean.Length - 1);
            
            return clean;
        }
        
        private string GetHierarchy(Transform t)
        {
            List<string> parts = new List<string>();
            Transform current = t;
            int depth = 0;
            while (current != null && depth < 6)
            {
                parts.Add(current.name + $"[{current.childCount}ch]");
                current = current.parent;
                depth++;
            }
            parts.Reverse();
            return string.Join(" > ", parts);
        }
        
        private string GetSoulForProp(string cleanName)
        {
            if (PropToSoul.TryGetValue(cleanName, out string soul))
                return soul;
            
            foreach (var kvp in PropToSoul)
            {
                if (cleanName.StartsWith(kvp.Key) || cleanName.Contains(kvp.Key))
                    return kvp.Value;
            }
            
            return null;
        }
        
        /// <summary>
        /// Apply prop states based on received souls
        /// </summary>
        private void ApplyAllPropStates()
        {
            // If souls are disabled, enable everything
            if (!PropSoulsEnabled)
            {
                EnableAllProps();
                return;
            }
            
            foreach (var kvp in propCache)
            {
                string soul = kvp.Key;
                // Always enable props needed for progression, or if we have the soul
                bool shouldEnable = AlwaysEnabledProps.Contains(soul) || receivedSouls.Contains(soul);
                
                foreach (var prop in kvp.Value)
                {
                    if (prop != null)
                        prop.SetActive(shouldEnable);
                }
            }
            
            Log.LogInfo($"[Prop] Applied states for {receivedSouls.Count} received souls (+ always-enabled props)");
        }
        
        /// <summary>
        /// Called when a soul is received
        /// </summary>
        public void ReceiveSoul(string soulName)
        {
            if (receivedSouls.Contains(soulName))
                return;
            
            Log.LogInfo($"[Prop] Received soul: {soulName}");
            receivedSouls.Add(soulName);
            
            // Only enable specific props if souls are enabled
            // If souls disabled, props are already enabled
            if (!PropSoulsEnabled)
                return;
            
            if (propCache.TryGetValue(soulName, out var props))
            {
                foreach (var prop in props)
                {
                    if (prop != null)
                    {
                        prop.SetActive(true);
                        Log.LogInfo($"[Prop] Enabled: {prop.name}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if we have a specific soul (or if souls are disabled, or if it's always enabled)
        /// </summary>
        public bool HasSoul(string soulName)
        {
            // Always-enabled props don't need souls
            if (AlwaysEnabledProps.Contains(soulName))
                return true;
            
            // If souls are disabled, always return true
            if (!PropSoulsEnabled)
                return true;
            
            return receivedSouls.Contains(soulName);
        }
        
        /// <summary>
        /// Refresh prop states (called when reconnecting or settings change)
        /// </summary>
        public void RefreshPropStates()
        {
            if (!hasScannedProps) return;
            
            Log.LogInfo($"[Prop] RefreshPropStates called (PropSoulsEnabled={PropSoulsEnabled})");
            
            if (PropSoulsEnabled)
            {
                DisableAllProps();
                ApplyAllPropStates();
            }
            else
            {
                EnableAllProps();
            }
        }
        
        /// <summary>
        /// Reset for returning to menu
        /// </summary>
        public void Reset()
        {
            Log.LogInfo("[Prop] Reset called");
            hasScannedProps = false;
            propCache.Clear();
        }
        
        /// <summary>
        /// Clear all received souls (for new slot)
        /// </summary>
        public void ClearAllSouls()
        {
            Log.LogInfo("[Prop] Clearing all souls");
            receivedSouls.Clear();
        }
        
        /// <summary>
        /// Save received souls to PlayerPrefs
        /// </summary>
        public void SaveSouls()
        {
            string soulList = string.Join(",", receivedSouls);
            PlayerPrefs.SetString("AP_PropSouls", soulList);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Load received souls from PlayerPrefs
        /// </summary>
        public void LoadSouls()
        {
            receivedSouls.Clear();
            string soulList = PlayerPrefs.GetString("AP_PropSouls", "");
            if (!string.IsNullOrEmpty(soulList))
            {
                foreach (string soul in soulList.Split(','))
                {
                    if (!string.IsNullOrEmpty(soul))
                        receivedSouls.Add(soul);
                }
            }
            Log.LogInfo($"[Prop] Loaded {receivedSouls.Count} souls from save");
        }
        
        /// <summary>
        /// Clear saved souls from PlayerPrefs
        /// </summary>
        public void ClearSavedSouls()
        {
            PlayerPrefs.DeleteKey("AP_PropSouls");
            PlayerPrefs.Save();
        }
    }
}