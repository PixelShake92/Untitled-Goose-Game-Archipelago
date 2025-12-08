using System;
using BepInEx.Logging;
using UnityEngine;

namespace GooseGameAP
{
    /// <summary>
    /// Handles all trap effects: Butterfingers, Suspicious, Tired, Clumsy
    /// </summary>
    public class TrapManager
    {
        private static ManualLogSource Log => Plugin.Log;
        private Plugin plugin;
        
        // Trap timers
        private float trapTimer = 0f;
        private float butterFingersTimer = 0f;
        private float suspiciousTimer = 0f;
        private float suspiciousHonkTimer = 0f;
        
        // Trap states
        private bool isTired = false;
        private bool isClumsy = false;
        private bool hasButterfingers = false;
        private bool isSuspicious = false;
        
        // Buff tracking
        public int SpeedyFeetCount { get; set; } = 0;
        
        public bool HasButterfingers => hasButterfingers;
        public bool IsTired => isTired;
        public bool IsClumsy => isClumsy;
        public bool IsSuspicious => isSuspicious;
        
        public TrapManager(Plugin plugin)
        {
            this.plugin = plugin;
        }
        
        public float GetEffectiveSpeedMultiplier()
        {
            if (isTired)
                return 0.5f;
            // 5% speed per Speedy Feet, capped at 50% bonus (10 items)
            float bonus = SpeedyFeetCount * 0.05f;
            if (bonus > 0.5f) bonus = 0.5f;
            return 1.0f + bonus;
        }
        
        public string GetActiveTrapText()
        {
            if (!isTired && !isClumsy && !hasButterfingers && !isSuspicious)
                return null;
            
            string trapText = "TRAP: ";
            if (isTired) trapText += "Tired(" + trapTimer.ToString("F0") + "s) ";
            if (isClumsy) trapText += "Clumsy(" + trapTimer.ToString("F0") + "s) ";
            if (hasButterfingers) trapText += "Butterfingers(" + butterFingersTimer.ToString("F0") + "s) ";
            if (isSuspicious) trapText += "Suspicious(" + suspiciousTimer.ToString("F0") + "s) ";
            return trapText;
        }
        
        public void ActivateTired(float duration = 30f)
        {
            isTired = true;
            trapTimer = duration;
            plugin.UI.ShowNotification("Tired Goose! (cosmetic only)");
        }
        
        public void ActivateClumsy(float duration = 30f)
        {
            isClumsy = true;
            trapTimer = duration;
            plugin.UI.ShowNotification("Clumsy Feet! (cosmetic only)");
        }
        
        public void ActivateButterfingers(float duration = 10f)
        {
            hasButterfingers = true;
            butterFingersTimer = duration;
            plugin.UI.ShowNotification("BUTTERFINGERS! Can't hold items for " + duration + "s!");
        }
        
        public void ActivateSuspicious(float duration = 10f)
        {
            isSuspicious = true;
            suspiciousTimer = duration;
            suspiciousHonkTimer = 0.5f;
            plugin.UI.ShowNotification("SUSPICIOUS! Honking uncontrollably for " + duration + "s!");
        }
        
        public void ClearTraps()
        {
            bool hadTraps = isTired || isClumsy || hasButterfingers || isSuspicious;
            isTired = false;
            isClumsy = false;
            hasButterfingers = false;
            isSuspicious = false;
            butterFingersTimer = 0f;
            suspiciousTimer = 0f;
            trapTimer = 0f;
            SpeedyFeetCount = 0;
            
            if (hadTraps)
                plugin.UI.ShowNotification("Trap effects have worn off!");
        }
        
        public void Update()
        {
            // Handle trap timers
            if (trapTimer > 0)
            {
                trapTimer -= Time.deltaTime;
                if (trapTimer <= 0)
                {
                    isTired = false;
                    isClumsy = false;
                }
            }
            
            // Handle butterfingers - drop items and prevent pickup
            if (butterFingersTimer > 0)
            {
                butterFingersTimer -= Time.deltaTime;
                ForceDropItems();
                
                if (butterFingersTimer <= 0)
                {
                    hasButterfingers = false;
                    plugin.UI.ShowNotification("Butterfingers wore off!");
                }
            }
            
            // Handle suspicious goose - force honking
            if (suspiciousTimer > 0)
            {
                suspiciousTimer -= Time.deltaTime;
                suspiciousHonkTimer -= Time.deltaTime;
                
                // Honk every 0.3-0.8 seconds
                if (suspiciousHonkTimer <= 0)
                {
                    ForceHonk();
                    suspiciousHonkTimer = UnityEngine.Random.Range(0.3f, 0.8f);
                }
                
                if (suspiciousTimer <= 0)
                {
                    isSuspicious = false;
                    plugin.UI.ShowNotification("Suspicious behavior stopped!");
                }
            }
        }
        
        private void ForceDropItems()
        {
            if (GameManager.instance == null || GameManager.instance.allGeese == null) return;
            
            foreach (var goose in GameManager.instance.allGeese)
            {
                if (goose != null && goose.isActiveAndEnabled)
                {
                    // Drop held items (beak)
                    var holder = goose.GetComponent<Holder>();
                    if (holder != null && holder.holding != null)
                    {
                        try
                        {
                            var dropMethod = holder.GetType().GetMethod("Drop", 
                                System.Reflection.BindingFlags.Public | 
                                System.Reflection.BindingFlags.Instance);
                            if (dropMethod != null)
                            {
                                dropMethod.Invoke(holder, null);
                                Log.LogInfo("[BUTTERFINGERS] Forced drop (held)!");
                            }
                        }
                        catch { }
                    }
                    
                    // Drop dragged items
                    var dragger = goose.GetComponent<Dragger>();
                    if (dragger != null)
                    {
                        try
                        {
                            var activeField = dragger.GetType().GetField("active",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            
                            bool isActive = activeField != null && (bool)activeField.GetValue(dragger);
                            
                            if (isActive)
                            {
                                // Try Drop method
                                var dropMethod = dragger.GetType().GetMethod("Drop",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                
                                if (dropMethod != null)
                                {
                                    dropMethod.Invoke(dragger, null);
                                    Log.LogInfo("[BUTTERFINGERS] Forced drop (dragged)!");
                                }
                                else
                                {
                                    // Try Release method
                                    var releaseMethod = dragger.GetType().GetMethod("Release",
                                        System.Reflection.BindingFlags.Public |
                                        System.Reflection.BindingFlags.NonPublic |
                                        System.Reflection.BindingFlags.Instance);
                                    
                                    if (releaseMethod != null)
                                    {
                                        releaseMethod.Invoke(dragger, null);
                                        Log.LogInfo("[BUTTERFINGERS] Forced release (dragged)!");
                                    }
                                    else if (activeField != null)
                                    {
                                        activeField.SetValue(dragger, false);
                                        Log.LogInfo("[BUTTERFINGERS] Set drag active=false!");
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
        }
        
        public void ForceHonk()
        {
            try
            {
                if (GameManager.instance == null || GameManager.instance.allGeese == null) return;
                
                foreach (var goose in GameManager.instance.allGeese)
                {
                    if (goose != null && goose.isActiveAndEnabled)
                    {
                        var honkerField = goose.GetType().GetField("gooseHonker",
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance);
                        
                        if (honkerField != null)
                        {
                            var honker = honkerField.GetValue(goose);
                            if (honker != null)
                            {
                                // Try Honk method
                                var honkMethod = honker.GetType().GetMethod("Honk",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                
                                if (honkMethod != null)
                                {
                                    var parameters = honkMethod.GetParameters();
                                    if (parameters.Length == 0)
                                    {
                                        honkMethod.Invoke(honker, null);
                                    }
                                    else
                                    {
                                        object[] args = new object[parameters.Length];
                                        for (int i = 0; i < parameters.Length; i++)
                                            args[i] = parameters[i].DefaultValue;
                                        honkMethod.Invoke(honker, args);
                                    }
                                    return;
                                }
                                
                                // Try StartHonk
                                honkMethod = honker.GetType().GetMethod("StartHonk",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                
                                if (honkMethod != null)
                                {
                                    honkMethod.Invoke(honker, null);
                                    return;
                                }
                                
                                // Try Play
                                honkMethod = honker.GetType().GetMethod("Play",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                
                                if (honkMethod != null)
                                {
                                    honkMethod.Invoke(honker, null);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError("ForceHonk error: " + ex.Message);
            }
        }
    }
}
