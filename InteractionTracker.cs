using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace GooseGameAP
{
    /// <summary>
    /// Tracks interaction-based locations (bike bell, windmill, wind chimes, etc.)
    /// </summary>
    public class InteractionTracker
    {
        private static ManualLogSource Log => Plugin.Log;
        private HashSet<string> completedInteractions = new HashSet<string>();
        private Plugin plugin;

        public InteractionTracker(Plugin plugin)
        {
            this.plugin = plugin;
        }

        /// <summary>
        /// Reset tracking (for new game)
        /// </summary>
        public void Reset()
        {
            completedInteractions.Clear();
            Log?.LogInfo("[INTERACT] Reset interaction tracking");
        }

        /// <summary>
        /// Check if an interaction has been completed
        /// </summary>
        public bool HasCompletedInteraction(string interactionName)
        {
            return completedInteractions.Contains(interactionName);
        }

        /// <summary>
        /// Mark an interaction as completed and send to AP if new
        /// </summary>
        public void OnInteraction(string interactionName)
        {
            if (string.IsNullOrEmpty(interactionName)) return;
            
            // Skip if already completed
            if (completedInteractions.Contains(interactionName))
            {
                return;
            }

            // Get location ID
            long? locationId = LocationMappings.GetInteractionLocationId(interactionName);
            if (locationId == null)
            {
                Log?.LogWarning($"[INTERACT] Unknown interaction: {interactionName}");
                return;
            }

            // Mark as completed
            completedInteractions.Add(interactionName);
            Log?.LogInfo($"[INTERACT] Completed interaction: {interactionName} (ID: {locationId})");

            // Send to Archipelago
            plugin?.SendLocationCheck(locationId.Value);
        }

        /// <summary>
        /// Called when the bike bell is rung
        /// </summary>
        public void OnBikeBellRung()
        {
            OnInteraction("BikeBell");
        }

        /// <summary>
        /// Called when the big garden bell is rung (makes man spit tea)
        /// </summary>
        public void OnGardenBellRung()
        {
            OnInteraction("GardenBell");
        }

        /// <summary>
        /// Called when the windmill is spun
        /// </summary>
        public void OnWindmillSpun()
        {
            OnInteraction("Windmill");
        }

        /// <summary>
        /// Called when the spinny flower is spun
        /// </summary>
        public void OnFlowerSpun()
        {
            OnInteraction("SpinFlower");
        }

        /// <summary>
        /// Called when the wind chimes are played
        /// </summary>
        public void OnWindChimesPlayed()
        {
            OnInteraction("WindChimes");
        }

        /// <summary>
        /// Called when the radio is unplugged
        /// </summary>
        public void OnRadioUnplugged()
        {
            OnInteraction("UnplugRadio");
        }

        /// <summary>
        /// Called when the boards are broken through
        /// </summary>
        public void OnBoardsBroken()
        {
            OnInteraction("BreakBoards");
        }
    }
}