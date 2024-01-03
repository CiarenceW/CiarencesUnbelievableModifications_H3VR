using BepInEx;
using BepInEx.Configuration;
using CiarencesUnbelievableModifications.Patches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using CiarencesUnbelievableModifications.MonoBehaviours;
using FistVR;
using BepInEx.Logging;

namespace CiarencesUnbelievableModifications
{
    internal static class SettingsManager
    {
        const string debugCatName = "Debug";

        const string magRetentionCatName = "Magazine Retention";
        const string cylinderBulletCollectorCatName = "Cylinder Bullet Collector";
        const string reverseMagHoldPosCatName = "Reverse Magazine Hold";
        const string reverseMagHoldOffset = reverseMagHoldPosCatName + " | Custom Offsets";
        const string boltHandleSoundTweaksCatName = "Bolt Handle Sounds";
        const string bitchDontGrabMyGunCatName = "Bitch Dont Grab My Gun";
        const string foldStockOnSpawn = "Fold Stock On Spawn";
        const string competitiveShellGrabbing = "Competitive Shell Grabbing";

        internal static bool Verbose
        {
            get { return SettingsManager.configVerbose.Value; }
        }

        internal static ConfigEntry<bool> configVerbose;

        internal static ConfigEntry<float> configMagRetentionMinimumDistanceThreshold;
        internal static ConfigEntry<float> configMagRetentionMinimumDotThreshold;

        internal static ConfigEntry<bool> configEnableMagPalmKeepOffset;

        internal static ConfigEntry<bool> configEnableQuickRetainedMagRelease;
        internal static ConfigEntry<bool> configEnableQuickRetainedMagReleaseMaximumHoldTime;
        internal static ConfigEntry<float> configQuickRetainedMagReleaseMaximumHoldTime;

        internal static ConfigEntry<bool> configEnableCylinderBulletCollector;

        internal static ConfigEntry<bool> configEnableReverseMagHold;
        internal static ConfigEntry<float> configReverseMagGrabMinDotProduct;
        internal static ConfigEntry<float> configReverseMagHoldPositionDistance;
        internal static ConfigEntry<bool> configReverseMagHoldHandgunOnly;

        internal static ConfigEntry<bool> configEnableFuckYouBitchDontGrabMyGun;

        internal static ConfigEntry<bool> configEnableStockFoldOnSpawn;

        internal static ConfigEntry<bool> configForceSilenceHitLock;

        internal static ConfigEntry<bool> configEnableCompetitiveShellGrabbing;
        internal static ConfigEntry<bool> configOnlyGrabXFromQB;
        internal static ConfigEntry<bool> configRevertToNormalGrabbingWhenAboveX;
        internal static ConfigEntry<int> configMaxShellsInHand;
        internal static ConfigEntry<bool> configNoLeverAction;

        internal static ConfigFile configFile;

        internal static void InitializeAndBindSettings(ConfigFile config)
        {
            configFile = config;

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

            #region MagPalmKeepOffset

            configEnableMagPalmKeepOffset = config.Bind(magRetentionCatName,
                "EnableMagPalmKeepOffset",
                true,
                "Keeps the offset of the palmed magazine");

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
                "The closer the value is to 1, the closer the angles of the two magazines must match for the gun's mag to be retained (0 is perpendicular, 1 is exact, -1 to disable)");

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

            #region ReverseMagHoldPos

            configEnableReverseMagHold = config.Bind(reverseMagHoldPosCatName,
                "EnableReverseMagHold",
                true,
                "Allows you to grab a magazine upside-down");

            configReverseMagGrabMinDotProduct = config.Bind(reverseMagHoldPosCatName,
                "ReverseMagHoldMinDotProduct",
                0.4f,
                "The minimum difference between the hand and the magazine for the magazine to be grabbed upside-down (a value of 1 means disabled)");

            configReverseMagHoldPositionDistance = config.Bind(reverseMagHoldPosCatName,
                "ReverseMagHoldPositionDistance",
                0.15f,
                "The offset between the center of your hand and the magazine's reversed position");

            configReverseMagHoldHandgunOnly = config.Bind(reverseMagHoldPosCatName,
                "ReverseMagHoldHandgunOnly",
                false,
                "Only allow handgun magazines to be held upside down");

            configReverseMagHoldPositionDistance.SettingChanged += (s, e) =>
            {
                var magPoseExtenders = UnityEngine.Object.FindObjectsOfType<FVRMagazinePoseExtender>();

                foreach (FVRMagazinePoseExtender magPoseExtender in magPoseExtenders)
                {
                    magPoseExtender.OffsetReverseHoldingPose();
                }
            };

            #endregion

            #region BoltHandleLockSoundTweaks

            configForceSilenceHitLock = config.Bind(boltHandleSoundTweaksCatName,
                "ForceSilenceHitLock",
                false,
                "Mutes the Handle Forward sound when the rotation charging handle is locked");

            #endregion

            #region FuckYouGunGrabTweak

            configEnableFuckYouBitchDontGrabMyGun = config.Bind(bitchDontGrabMyGunCatName,
                "EnableBitchDontGrabMyGun",
                true,
                "Prevents your other hand from instantly grabbing the gun you're currently holding");

            #endregion

            #region FoldStockOnSpawn

            configEnableStockFoldOnSpawn = config.Bind(foldStockOnSpawn,
                "EnableStockFoldOnSpawn",
                true,
                "Makes the foldable stocks of guns be folded when spawned");

            #endregion

            #region CompetitiveShellGrabing

            configEnableCompetitiveShellGrabbing = config.Bind(competitiveShellGrabbing,
                "EnableCompetitiveShellGrabbing",
                true,
                "Enables grabbing shotgun shells competitive-shooting style");

            configOnlyGrabXFromQB = config.Bind(competitiveShellGrabbing,
                "OnlyGrabXFromQB",
                true,
                "Only grab X amount (MaxShellsInPalm) of shells from a quickbelt slot");

            configRevertToNormalGrabbingWhenAboveX = config.Bind(competitiveShellGrabbing,
                "RevertToNormalProxyPositionWhenAboveX",
                false,
                "If amount of shells palmed in hand is above X (MaxShellsInPalm), revert to \"pez dispenser\" holding style");

            configMaxShellsInHand = config.Bind(competitiveShellGrabbing,
                "MaxShellsInPalm",
                4,
                "The max amount of shells that can be palmed in a competitive-shooting style");

            configNoLeverAction = config.Bind(competitiveShellGrabbing,
                "NoLeverAction",
                true,
                "Prevents competitively grabbing shells while holding a lever-action");

            #endregion
        }

        internal static void LogVerboseInfo(object data, bool forceLog = false)
        {
            if (Verbose || forceLog)
            {
                CiarencesUnbelievableModifications.Logger.LogInfo(data);
            }
        }

        internal static void LogVerboseWarning(object data, bool forceLog = false)
        {
            if (Verbose || forceLog) 
            { 
                CiarencesUnbelievableModifications.Logger.LogWarning(data);
            } 
        }

        internal static void LogVerboseError(object data, bool forceLog = false)
        {
            if (Verbose || forceLog)
            {
                CiarencesUnbelievableModifications.Logger.LogError(data);
            }
        }

        internal static void LogVerboseMessage(object data, bool forceLog = false)
        {
            if (Verbose || forceLog)
            {
                CiarencesUnbelievableModifications.Logger.LogMessage(data);
            }
        }

        internal static void LogVerboseFatal(object data, bool forceLog = false)
        {
            if (Verbose || forceLog)
            {
                CiarencesUnbelievableModifications.Logger.LogFatal(data);
            }
        }

        internal static void LogVerbose(LogLevel logLevel, object data, bool forceLog = false)
        {
            if (Verbose || forceLog)
            {
                CiarencesUnbelievableModifications.Logger.Log(logLevel, data);
            }
        }

        internal static ConfigEntry<float> BindMagazineOffset(FVRFireArmMagazine magazine)
        {
            return configFile.Bind(reverseMagHoldOffset, magazine.ObjectWrapper.ItemID, 0f);
        }
    }
}
