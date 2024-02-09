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

        PluginInfo safehouseProgressionPlugin;

        string[] safehouseIDs;

        private void Awake()
        {
            /*if (Chainloader.PluginInfos.TryGetValue("NGA.SafehouseProgression", out safehouseProgressionPlugin))
            {
                var pluginConfig = safehouseProgressionPlugin.Instance.Config;

                //VALUES DOESN'T EXIST YOU PIECE OF SHIT
                var configEntries = from e in pluginConfig.GetConfigEntries() where e is ConfigEntry<string> select e;

                safehouseIDs = new string[configEntries.Count()];

                var configEntriesArray = (ConfigEntry<string>[])configEntries.ToArray();
                for (int i = 0; i < configEntriesArray.Length; i++)
                {
                    safehouseIDs[i] = configEntriesArray[i].Value;
                }

                pluginConfig.SettingChanged += OnSafehouseConfigChange;
            }*/

            Logger = base.Logger;

            SettingsManager.InitializeAndBindSettings(Config);

            PatchAll();
            patched = true;

            Logger.LogMessage($"{Id} version {Version} prepped and ready");
        }

        /*internal static class SafehouseProgressionCompatibility
        {
            static void Piss()
            {
                Harmony.GetPatchInfo
            }
        }*/

        internal void OnSafehouseConfigChange(object sender, SettingChangedEventArgs args)
        {
            if (args.ChangedSetting is ConfigEntry<string> sceneId)
            {
                if (int.TryParse(sceneId.Definition.Section.Last().ToString(), out var num))
                {
                    safehouseIDs[num] = sceneId.Value;
                }
            }
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
        }

        //Do I really need this? Can't I use Debug.Log? I like Debug.Log :(
        internal new static ManualLogSource Logger { get; private set; }
    }
}
