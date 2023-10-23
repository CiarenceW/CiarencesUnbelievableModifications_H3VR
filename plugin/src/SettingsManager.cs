using BepInEx;
using BepInEx.Configuration;
using CiarencesUnbelievableModifications.Patches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace CiarencesUnbelievableModifications
{
    internal static class SettingsManager
    {
        const string debugCatName = "Debug";
        const string magRetentionCatName = "Magazine Retention";
        const string cylinderBulletCollectorCatName = "Cylinder Bullet Collector";

        internal static bool Verbose
        {
            get { return SettingsManager.configVerbose.Value; }
        }

        internal static ConfigEntry<bool> configVerbose;

        internal static ConfigEntry<float> configMagRetentionMinimumDistanceThreshold;
        internal static ConfigEntry<float> configMagRetentionMinimumDotThreshold;

        internal static ConfigEntry<bool> configEnableQuickRetainedMagRelease;
        internal static ConfigEntry<bool> configEnableQuickRetainedMagReleaseMaximumHoldTime;
        internal static ConfigEntry<float> configQuickRetainedMagReleaseMaximumHoldTime;

        internal static ConfigEntry<bool> configEnableCylinderBulletCollector;

        internal static void InitializeAndBindSettings(ConfigFile config)
        {
            configVerbose = config.Bind(debugCatName,
                "EnableVerbose",
                false,
                "Adds logging information for debugging purposes");

            #region MagRetentionTweaks

            #region QuickRetainedMagRelease

            configEnableQuickRetainedMagRelease = config.Bind(magRetentionCatName,
                "EnableQuickRetainedMagRelease",
                true,
                "Allows you to drop a retained magazine simply by releasing the touchpad/thumbstick, nice for people who don't like the Quest 2's thumbsticks");

            configEnableQuickRetainedMagReleaseMaximumHoldTime = config.Bind(magRetentionCatName,
                "EnableQuickRetainedMagReleaseMaxHeldTime",
                true,
                "Allows you to keep a retained magazine without having to hold by pressing and releasing quickly the touchpad");

            configQuickRetainedMagReleaseMaximumHoldTime = config.Bind(magRetentionCatName,
                "QuickRetainedMagReleaseMaximumHoldTime",
                0.10f,
                "Maximum amount of time that the touchpad must be held down before the retained magazine can be released by letting go of the touchpad");

            #endregion

            #region Thresholds
            configMagRetentionMinimumDistanceThreshold = config.Bind(magRetentionCatName,
                "MagRetentionMinimumDistanceThreshold",
                0.2f,
                "The minimum distance between the gun's magazine and the magazine in the other hand needed for the gun's mag to be retained");

            configMagRetentionMinimumDistanceThreshold.SettingChanged += (s, e) =>
            {
                MagRetentionTweaks.magRetentionMinimumDistanceThreshold = configMagRetentionMinimumDistanceThreshold.Value;
            };

            MagRetentionTweaks.magRetentionMinimumDistanceThreshold = configMagRetentionMinimumDistanceThreshold.Value;

            configMagRetentionMinimumDotThreshold = config.Bind(magRetentionCatName,
                "MagRetentionMinimumDotThreshold",
                0.8f,
                "The closer the value is to 1, the closer the angles of the two magazines must match for the gun's mag to be retained (0 is perpendicular, 1 is exact)");

            configMagRetentionMinimumDotThreshold.SettingChanged += (s, e) =>
            {
                MagRetentionTweaks.magRetentionDotProductThreshold = configMagRetentionMinimumDotThreshold.Value;
            };

            MagRetentionTweaks.magRetentionDotProductThreshold = configMagRetentionMinimumDotThreshold.Value;
            #endregion

            #endregion

            #region CylinderBulletCollector

            configEnableCylinderBulletCollector = config.Bind(cylinderBulletCollectorCatName,
                "EnableCylinderBulletCollector",
                true,
                "Allows you to eject the cylinder of a revolver and keep its unspent rounds by grabbing it and pressing the trigger");

            #endregion
        }
    }
}
