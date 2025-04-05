using BepInEx.Configuration;
using CiarencesUnbelievableModifications.Patches;
using System;
using System.Collections.Generic;
using UnityEngine;
using CiarencesUnbelievableModifications.MonoBehaviours;
using FistVR;
using BepInEx.Logging;
using CiarencesUnbelievableModifications.Libraries;
using System.Drawing;

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
		const string timedObjectDestructionCatName = "Timed Object Destruction";
		const string knockAKDrumOutCatName = "Knock AK Drums out";
		const string easyMagLoadingCategoryBlacklistCatName = "Easy Mag Loading Category Blacklist";
		const string virtualStockCategoryBlacklistCatName = "Virtual Stock Category Blacklist";
		const string experimentalCatName = "Experimental stuff, might not work ^^ (probably requires restart)";

		const string incrementalGunSmoothingCatName = "Incremental Gun Smoothing";

		internal static bool Verbose
		{
			get
			{
				return SettingsManager.configVerbose.Value;
			}
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
		internal static ConfigEntry<bool> configOnlyHandguns;

		internal static ConfigEntry<bool> configEnableStockFoldOnSpawn;

		internal static ConfigEntry<bool> configForceSilenceHitLock;

		internal static ConfigEntry<bool> configEnableCompetitiveShellGrabbing;
		internal static ConfigEntry<bool> configOnlyGrabXFromQB;
		internal static ConfigEntry<bool> configOnlyGrabPairAmountOfShells;
		internal static ConfigEntry<bool> configRevertToNormalGrabbingWhenAboveX;
		internal static ConfigEntry<int> configMaxShellsInHand;
		internal static ConfigEntry<bool> configNoLeverAction;
		internal static ConfigEntry<bool> configForceUnconditionalCompetitiveShellGrabbing;
		internal static ConfigEntry<Vector3> configCompetitiveShellPoseOverridePosition;
		internal static ConfigEntry<Vector3> configCompetitiveShellPoseOverrideRotation;
		internal static ConfigEntry<bool> configIncreaseRoundInsertTriggerZone;
		internal static ConfigEntry<float> configTriggerZoneMultiplier;
		internal static ConfigEntry<bool> configPezOnGrabOneShell;
		internal static ConfigEntry<bool> configGrabOneShellOnTrigger;
		internal static ConfigEntry<bool> configReverseGrabAndTrigger;
		internal static ConfigEntry<bool> configOnlyGrabOneWhenChamberOpen;
		internal static ConfigEntry<bool> configGrabOneWhenSmartPalmingOff;

		internal static ConfigEntry<bool> configEnableKnockAKDrumOut;
		internal static ConfigEntry<bool> configForAllNonEjectableGuns;

		internal static SectionConfigList<FVRObject.OTagFirearmAction> sectionConfigListVirtualStockBlacklist;
		internal static SectionConfigList<FVRObject.OTagFirearmSize> sectionConfigListEasyMagLoadingBlacklist;

		internal static ConfigEntry<string> configVirtualStockWeaponBlacklist;
		internal static ConfigEntry<string> configVirtualStockWeaponWhitelist;

		internal static ConfigEntry<string> configEasyMagLoadingWeaponBlacklist;
		internal static ConfigEntry<string> configEasyMagLoadingWeaponWhitelist;

		internal static ConfigEntry<bool> configEnableSodaliteConsoleColorPatch;

		internal static ConfigFile configFile;
		internal static ConfigEntry<bool> configEnableTimedObjectDestruction;
		internal static ConfigEntry<float> configTODTimeToDestroy;
		internal static ConfigEntry<bool> configTODEnableFirearms;
		internal static ConfigEntry<bool> configTODEnableMagazines;
		internal static ConfigEntry<bool> configTODEmptyMagazinesOnly;
		internal static ConfigEntry<bool> configTODEnableRounds;
		internal static ConfigEntry<bool> configTODEnableAll;

		internal static ConfigEntry<bool> configEnableIncrementalHandSmoothing;

		internal static ConfigEntry<float> configIncrementalHandSmoothingMaxStrength;

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

			configMagRetentionMinimumDotThreshold = config.Bind(magRetentionCatName,
				"MagRetentionMinimumDotThreshold",
				0.8f,
				"The closer the value is to 1, the closer the angles of the two magazines must match for the gun's mag to be retained (0 is perpendicular, 1 is exact, -1 to disable)");
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
				false,
				"Allows you to grab a magazine upside-down (Disabled automatically when BetterHands' mag palming is on)");

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
				false,
				"Prevents your other hand from instantly grabbing the gun you're currently holding");

			configOnlyHandguns = config.Bind(bitchDontGrabMyGunCatName,
				"OnlyHandguns",
				true,
				"Only enables the gun snatching prevention for handguns");

			#endregion

			#region FoldStockOnSpawn

			configEnableStockFoldOnSpawn = config.Bind(foldStockOnSpawn,
				"EnableStockFoldOnSpawn",
				(CiarencesUnbelievableModifications.safehouseProgressionPlugin == null) ? true : false, //if player is using safehouse mod, then make it false
				"Makes the foldable stocks of guns be folded when spawned");

			#endregion

			#region CompetitiveShellGrabing

			configEnableCompetitiveShellGrabbing = config.Bind(competitiveShellGrabbing,
				"EnableCompetitiveShellGrabbing",
				false,
				"Enables grabbing shotgun shells competitive-shooting style");

			configOnlyGrabXFromQB = config.Bind(competitiveShellGrabbing,
				"OnlyGrabXFromQB",
				true,
				"Only grab X amount (MaxShellsInPalm) of shells from a non-spawnlocked quickbelt slot");

			configRevertToNormalGrabbingWhenAboveX = config.Bind(competitiveShellGrabbing,
				"RevertToNormalProxyPositionWhenAboveX",
				false,
				"If amount of shells palmed in hand is above X (MaxShellsInPalm), revert to \"pez dispenser\" holding style");

			configMaxShellsInHand = config.Bind(competitiveShellGrabbing,
				"MaxShellsInPalm",
				4,
				"The max amount of shells that can be palmed in a competitive-shooting style");

			configOnlyGrabPairAmountOfShells = config.Bind(competitiveShellGrabbing,
				"OnlyGrabPairAmountOfShells",
				false,
				"Forces the amount of grabbed shells in limited ammo mode to be pair");

			configNoLeverAction = config.Bind(competitiveShellGrabbing,
				"NoLeverAction",
				true,
				"Prevents competitively grabbing shells while holding a lever-action");

			configForceUnconditionalCompetitiveShellGrabbing = config.Bind(competitiveShellGrabbing,
				"ForceUnconditionalCompetitiveShellGrabbing",
				false,
				"If true, will always grab shotgun shells in a competitive-shooting style");

			configCompetitiveShellPoseOverridePosition = config.Bind(competitiveShellGrabbing,
				"CompetitiveShellPoseOverridePosition",
				new Vector3(0, -0.025f, -0.1f),
				"The position offset from the normal way shotgun shells are held, change if shells are jittery when colliding");

			configCompetitiveShellPoseOverrideRotation = config.Bind(competitiveShellGrabbing,
				"configCompetitiveShellPoseOverrideRotation",
				new Vector3(0, 180, 90),
				"The rotation offset from the normal way shotgun shells are held");

			configIncreaseRoundInsertTriggerZone = config.Bind(competitiveShellGrabbing,
				"configIncreaseRoundInsertTriggerZone",
				false,
				"Increases the size of the trigger zone where rounds will be loaded, makes reloading easier");

			configTriggerZoneMultiplier = config.Bind(competitiveShellGrabbing,
				"configTriggerZoneMultiplier",
				2f,
				"The value by which the trigger zone's bounds will be mutliplied by");

			configPezOnGrabOneShell = config.Bind(competitiveShellGrabbing,
				"configPezOnGrabOneShell",
				true,
				"If only one shell is grabbed (for example using Trigger), shell will be in pez form");

			configGrabOneShellOnTrigger = config.Bind(competitiveShellGrabbing,
				"configGrabOneShellOnTrigger",
				true,
				"If trigger is pressed on a Quickbelt slot, grab a single shell");

			configReverseGrabAndTrigger = config.Bind(competitiveShellGrabbing,
				"configReverseGrabAndTrigger",
				false,
				"Press trigger to grab a whole stack of shells, press grab to grab a single one");

			configOnlyGrabOneWhenChamberOpen = config.Bind(competitiveShellGrabbing,
				"OnlyGrabOneWhenChamberOpen",
				false,
				"Only grabs one shell when the chamber is opened and accessible");

			configGrabOneWhenSmartPalmingOff = config.Bind(competitiveShellGrabbing,
				"GrabOneWhenSmartPalmingOff",
				false,
				"Only grab one shell when Smart Palming is off");

			#endregion

			#region KnockAKDrumOut

			configEnableKnockAKDrumOut = config.Bind(knockAKDrumOutCatName,
				"KnockAKDrumOut",
				true,
				"Enables you to knock a drum magazine from a gun with a physical magazine latch by knocking it with another mag, while pressing touchpad down on the controller with the gun");

			configForAllNonEjectableGuns = config.Bind(knockAKDrumOutCatName,
				"EnableForAllNonEjectableGuns",
				false,
				"Enables knocking out drum mags for guns that don't have an eject button");

			#endregion

			#region EasyMagLoadingBlacklist

			sectionConfigListEasyMagLoadingBlacklist = new SectionConfigList<FVRObject.OTagFirearmSize>() { section = easyMagLoadingCategoryBlacklistCatName, configEntries = BindAllTypesOfFireArms<FVRObject.OTagFirearmSize>(config, easyMagLoadingCategoryBlacklistCatName) };

			configEasyMagLoadingWeaponBlacklist = config.Bind(easyMagLoadingCategoryBlacklistCatName,
				"EasyMagLoadingWeaponSpecificBlacklist",
				string.Empty,
				@"Specific weapons that should be banned from Easy Mag Loading. Needs to be the name that displays on the Wrist Menu. Format goes as follows: GunName|GunName ");

			configEasyMagLoadingWeaponWhitelist = config.Bind(easyMagLoadingCategoryBlacklistCatName,
				"EasyMagLoadingWeaponSpecificWhitelist",
				string.Empty,
				@"Specific weapons that should always have Easy Mag Loading on, takes priority over any blacklist. Needs to be the name that displays on the Wrist Menu. Format goes as follows: GunName|GunName ");

			#endregion

			#region VirtualStockBlacklist

			sectionConfigListVirtualStockBlacklist = new SectionConfigList<FVRObject.OTagFirearmAction>() { section = virtualStockCategoryBlacklistCatName, configEntries = BindAllTypesOfFireArms<FVRObject.OTagFirearmAction>(config, virtualStockCategoryBlacklistCatName) };

			configVirtualStockWeaponBlacklist = config.Bind(virtualStockCategoryBlacklistCatName,
				"VirtualStockWeaponSpecificBlacklist",
				string.Empty,
				@"Specific weapons that should be banned from Virtual Stock. Needs to be the name that displays on the Wrist Menu. Format goes as follows: GunName|GunName ");

			configVirtualStockWeaponWhitelist = config.Bind(virtualStockCategoryBlacklistCatName,
				"VirtualStockWeaponSpecificWhitelist",
				string.Empty,
				@"Specific weapons that should always have Virtual Stock on, takes priority over any blacklist. Needs to be the name that displays on the Wrist Menu. Format goes as follows: GunName|GunName ");

			#endregion

			#region TimedObjectDestruction

			const string todCatName = "Timed Object Destruction";

			configEnableTimedObjectDestruction = config.Bind(todCatName,
				"EnabledTimedObjectDestruction",
				false,
				"Enable time object destruction");

			configTODTimeToDestroy = config.Bind(todCatName,
				"TimedObjectDestructionTimeBeforeDestroy",
				50f,
				"How long will an object stay before being destroyed");

			configTODEnableFirearms = config.Bind(todCatName,
				"EnableFirearmsTimedObjectDestruction",
				false,
				"Starts destruction countdown timer for firearms");

			configTODEnableMagazines = config.Bind(todCatName,
				"EnableMagazinesTimedObjectDestruction",
				true,
				"Starts destruction countdown timer for magazines");

			configTODEmptyMagazinesOnly = config.Bind(todCatName,
				"TODEmptyMagazinesOnly",
				true,
				"Destruction countdown timer will only start for empty magazines");

			configTODEnableRounds = config.Bind(todCatName,
				"EnableRoundsTimedObjectDestruction",
				true,
				"Starts destruction countdown timer for unspent rounds (vanilla H3VR already has one for spent shells)");

			configTODEnableAll = config.Bind(todCatName,
				"EnableAllTimedObjectDestruction",
				false,
				"Starts destruction countdown timer for everything that gets deleted by the \"All Spawnables\" option in the hand menu");

			#endregion

			#region IncrementalSmoothing

			configEnableIncrementalHandSmoothing = config.Bind(incrementalGunSmoothingCatName,
				"EnableIncrementalHandSmoothing",
				false,
				"Allows you to control with the trigger the amount of smoothing applied when stabilising long arms");

			configIncrementalHandSmoothingMaxStrength = config.Bind(incrementalGunSmoothingCatName,
				"IncrementalHandSmoothingMaxStrength",
				1f,
				"How strong the effect will be when the trigger is fully pressed (this is just a markiplier)");

			#endregion

			#region experimental

			configEnableSodaliteConsoleColorPatch = config.Bind(experimentalCatName,
				"EnableSodaliteConsoleColorPatch",
				false,
				"Transpiles the Sodalite console so that CUM's custom console colour feature works, currently just recolours the entire console if one of CUM's extensions is used, oops");

			
			#endregion
		}

		internal static Dictionary<T, ConfigEntry<bool>> BindAllTypesOfFireArms<T>(ConfigFile config, string section) where T : Enum
		{
			Dictionary<T, ConfigEntry<bool>> fireArmActionConfigEntries = new();
			foreach (var enums in ExtensionsToMakeMyLifeLessShit.GetEnumList<T>())
			{
				fireArmActionConfigEntries.Add(enums, config.Bind(section,
					enums.ToString(),
					false,
					$"Should guns from the {enums.ToString()} category be excluded from {section}?"));
			}
			return fireArmActionConfigEntries;
		}

		internal static void LogVerboseLevelNameAndColor(object data, string levelName, ConsoleColor color, bool forceLog = false)
		{
			if (Verbose || forceLog)
			{
				CiarencesUnbelievableModifications.Logger.LogWithCustomLevelNameAndColor(data, levelName, color);
			}
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
