using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using CiarencesUnbelievableModifications.Patches;
using FistVR;
using HarmonyLib;
using System;
using System.Linq;
using UnityEngine.EventSystems;

namespace CiarencesUnbelievableModifications
{
    [BepInAutoPlugin]
    [BepInProcess("h3vr.exe")]
    public partial class CiarencesUnbelievableModifications : BaseUnityPlugin
    {
        //is it me or the methods get patched again later on? for now reason??? what the fuck!!!!!!!!
        internal static bool patched = false;

        public static PluginInfo safehouseProgressionPlugin;

        public static PluginInfo betterHandsPlugin;
        public static bool isBetterHandsPalmingEnabled;

        private void Awake()
		{
			foreach (Type type in typeof(BepInPlugin).Assembly.GetTypes())
			{
				if (type.FullName == "BepInEx.ConsoleUtil.Kon")
				{
					ConsoleColourer.konType = type;
					break;
				}
			}

			CheckForIncompatibilites();

            Logger = base.Logger;

            SettingsManager.InitializeAndBindSettings(Config);

            PatchAll();
            patched = true;

			#pragma warning disable
			ConsoleColourer.BackgroundColor = ConsoleColor.Red;
            Logger.LogMessageWithColor($"{Id} version {Version} prepped and ready", ConsoleColor.Green);
			ConsoleColourer.BackgroundColor = ConsoleColor.Black;
			#pragma warning restore
		}

		private void SaySomething()
		{
			#pragma warning disable
			ConsoleColourer.BackgroundColor = ConsoleColor.DarkMagenta;
			Logger.LogWithCustomLevelNameAndColor("Hello World", "Test :D", ConsoleColor.Yellow);
			ConsoleColourer.BackgroundColor = ConsoleColor.Black;
			#pragma warning restore
		}

		private void PatchAll()
        {
            Harmony.CreateAndPatchAll(typeof(MagRetentionTweaks.MagRetentionTweaksTranspilers));
            Harmony.CreateAndPatchAll(typeof(MagRetentionTweaks.MagRetentionTweaksHarmonyFixes));

            Harmony.CreateAndPatchAll(typeof(CylinderBulletCollector));
            Harmony.CreateAndPatchAll(typeof(CylinderBulletCollector.CylinderBulletCollectorTranspiler));

            Harmony.CreateAndPatchAll(typeof(BoltHandleLockSoundTweaks));
            Harmony.CreateAndPatchAll(typeof(BoltHandleLockSoundTweaks.Transpilers));

            Harmony.CreateAndPatchAll(typeof(ReverseMagHoldPos));

            Harmony.CreateAndPatchAll(typeof(MagRetentionTweaks.MagPalmKeepOffsetPatch.MagPalmKeepOffsetHarmonyPatches));
            Harmony.CreateAndPatchAll(typeof(MagRetentionTweaks.MagPalmKeepOffsetPatch.MagPalmKeepOffsetTranspilers));

            Harmony.CreateAndPatchAll(typeof(FireArmTweaks.SpawnStockTweaks));
            Harmony.CreateAndPatchAll(typeof(FireArmTweaks.BitchDontTouchMyGun));

            Harmony.CreateAndPatchAll(typeof(InstitutionPreviewReenabler));

            Harmony.CreateAndPatchAll(typeof(CompetitiveShellGrabbing.Patches));
            Harmony.CreateAndPatchAll(typeof(CompetitiveShellGrabbing.Transpilers));

            Harmony.CreateAndPatchAll(typeof(FireArmTweaks.KnockAKDrumOut));

			Harmony.CreateAndPatchAll(typeof(OptionGunCategoryBlacklister.Transpilers));
        }

        internal void CheckForIncompatibilites()
        {
            if (!Chainloader.PluginInfos.TryGetValue("NGA.SafehouseProgression", out safehouseProgressionPlugin))
            {
                Chainloader.PluginInfos.TryGetValue("NGA.SafehouseMP", out safehouseProgressionPlugin); //there's a different version for MP, ffs
            }

            if (Chainloader.PluginInfos.TryGetValue("maiq.BetterHand", out betterHandsPlugin))
            {
                if (betterHandsPlugin.Instance.Config.TryGetEntry<bool>("MagPalm Options", "aEnable", out var entry))
                {
                    isBetterHandsPalmingEnabled = entry.Value;

                    entry.SettingChanged += (sender, args) => { isBetterHandsPalmingEnabled = (sender as ConfigEntry<bool>).Value; }; //hope this works
                }
            }
        }

        //Do I really need this? Can't I use Debug.Log? I like Debug.Log :(
        internal new static ManualLogSource Logger { get; private set; }
    }
}
