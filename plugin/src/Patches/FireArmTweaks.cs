using FistVR;
using HarmonyLib;

namespace CiarencesUnbelievableModifications.Patches
{
    internal static class FireArmTweaks
    {
        internal static class SpawnStockTweaks //I want to be forced to do more things before a weapon becomes operational :)
        {
            [HarmonyPatch(typeof(FVRFireArm), nameof(FVRFireArm.Awake))]
            [HarmonyPostfix]
            private static void PatchFVRFireArmStartStock(FVRFireArm __instance)
            {
                if (!SettingsManager.configEnableStockFoldOnSpawn.Value) return;
                if (__instance.HasStockPos())
                {
                    if (__instance.GetStockPos() != null) //basically uh, fuck me
                    {
                        if (__instance.TryGetComponentInChildren<FVRFoldingStockYAxis>(out var result))
                        {
                            var eulers = result.Stock.localEulerAngles;
                            eulers.y = result.isMinClosed ? result.MinRot : result.MaxRot; //I do be preserving other axis' rotation doe
                            result.Stock.localEulerAngles = eulers;
                            SettingsManager.LogVerboseInfo(result.Stock.localEulerAngles);
                            result.m_curPos = FVRFoldingStockYAxis.StockPos.Closed;
                        }
                        else
                        {
                            SettingsManager.LogVerboseInfo("can't find FVRFoldingStockYAxis");
                        }
                    }
                    else
                    {
                        SettingsManager.LogVerboseInfo("StockPos is null");
                    }
                }
            }

            [HarmonyPatch(typeof(FVRFoldingStockXAxis), nameof(FVRFoldingStockXAxis.Start))] //aren't they all closed by default?
            [HarmonyPrefix]
            private static void PatchFVRStockXAxisStart(FVRFoldingStockXAxis __instance)
            {
                if (!SettingsManager.configEnableStockFoldOnSpawn.Value)
                {
                    var eulers = __instance.Stock.localEulerAngles;
                    eulers.y = __instance.isMinClosed ? __instance.MinRot : __instance.MaxRot;
                    __instance.Stock.localEulerAngles = eulers;
                    __instance.m_curPos = FVRFoldingStockXAxis.StockPos.Closed;
                }
            }
        }

        internal static class BitchDontTouchMyGun
        {
            [HarmonyPatch(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.BeginInteraction))]
            [HarmonyPrefix]
            private static bool SkipBeginInteractIfAlreadyHeld(FVRPhysicalObject __instance, FVRViveHand hand)
            {

                if (__instance is FVRFireArm gun && !((gun is not Handgun && SettingsManager.configOnlyHandguns.Value) || (!SettingsManager.configOnlyHandguns.Value)) && __instance.m_hand != null && !__instance.IsAltHeld && __instance.m_hand == hand.OtherHand && SettingsManager.configEnableFuckYouBitchDontGrabMyGun.Value) //you little fucker
                {
                    hand.CurrentInteractable = null;
                    return false;
                }
                return true;
            }
        }
    }
}
