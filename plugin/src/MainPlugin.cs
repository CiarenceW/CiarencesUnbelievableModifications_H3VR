using BepInEx;
using BepInEx.Logging;
using CiarencesUnbelievableModifications.Patches;
using HarmonyLib;

namespace CiarencesUnbelievableModifications
{
    [BepInAutoPlugin]
    [BepInProcess("h3vr.exe")]
    public partial class CiarencesUnbelievableModifications : BaseUnityPlugin
    {
        //is it me or the methods get patched again later on? for now reason??? what the fuck!!!!!!!!
        internal static bool patched = false;

        private void Awake()
        {
            Logger = base.Logger;

            SettingsManager.InitializeAndBindSettings(Config);

            PatchAll();
            patched = true;

            Logger.LogMessage($"{Id} version {Version} prepped and ready");
        }

        private void PatchAll()
        {
            Harmony.CreateAndPatchAll(typeof(MagRetentionTweaks.MagRetentionTweaksTranspilers));
            Harmony.CreateAndPatchAll(typeof(MagRetentionTweaks.MagRetentionTweaksHarmonyFixes));

            //another time maybe :)
            Harmony.CreateAndPatchAll(typeof(CylinderBulletCollector));
            Harmony.CreateAndPatchAll(typeof(CylinderBulletCollector.CylinderBulletCollectorTranspiler));
        }
        
        //Do I really need this? Can't I use Debug.Log? I like Debug.Log :(
        internal new static ManualLogSource Logger { get; private set; }
    }
}
