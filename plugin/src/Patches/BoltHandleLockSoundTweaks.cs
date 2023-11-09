using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using FistVR;
using HarmonyLib;

namespace CiarencesUnbelievableModifications.Patches
{
    public static class BoltHandleLockSoundTweaks
    {
        public static FirearmAudioEventType GetHandleLockSound(ClosedBoltWeapon weapon)
        {
            FirearmAudioEventType handleAudioEventType = FirearmAudioEventType.HandleForward;
            if (weapon.AudioClipSet.HandleUp.Clips.Count > 0 || SettingsManager.configForceSilenceHitLock.Value)
            {
                handleAudioEventType = FirearmAudioEventType.HandleUp;
            }
            if (SettingsManager.Verbose) CiarencesUnbelievableModifications.Logger.LogInfo(handleAudioEventType);
            return handleAudioEventType;
        }

        internal static class Transpilers
        {
            [HarmonyPatch(typeof(ClosedBoltHandle), nameof(ClosedBoltHandle.Event_HitLockPosition))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> TranspileHitLockPositionSoundEvent(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
            {
                CodeMatch[] matches = {
                    new CodeMatch(new CodeInstruction(OpCodes.Ldarg_0)),
                    new CodeMatch(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ClosedBoltHandle), nameof(ClosedBoltHandle.Weapon)))),
                    new CodeMatch(new CodeInstruction(OpCodes.Ldc_I4_S))
                };

                if (TranspilerHelper.TryMatchForward(true, instructions, generator, out var codeMatcher, __originalMethod, CiarencesUnbelievableModifications.Logger.LogError, matches))
                {
                    if (SettingsManager.Verbose) CiarencesUnbelievableModifications.Logger.LogInfo($"Patching {MethodBase.GetCurrentMethod().Name}");

                    codeMatcher
                        .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ClosedBoltHandle), nameof(ClosedBoltHandle.Weapon))))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BoltHandleLockSoundTweaks), nameof(BoltHandleLockSoundTweaks.GetHandleLockSound))))
                        ;
                }

                return codeMatcher.InstructionEnumeration();
            }

            [HarmonyPatch(typeof(ClosedBoltHandle), nameof(ClosedBoltHandle.UpdateHandle))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> AddHandleDownSoundEvent(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
            {
                CodeMatch[] matches = {
                    new CodeMatch(new CodeInstruction(OpCodes.Ldarg_0)),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ClosedBoltHandle), nameof(ClosedBoltHandle.Weapon))),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ClosedBoltWeapon), nameof(ClosedBoltWeapon.Bolt))),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ClosedBolt), nameof(ClosedBolt.ReleaseBolt)))
                };

                if (TranspilerHelper.TryMatchForward(false, instructions, generator, out var codeMatcher, __originalMethod, CiarencesUnbelievableModifications.Logger.LogError, matches))
                {
                    if (SettingsManager.Verbose) CiarencesUnbelievableModifications.Logger.LogInfo($"Patching {MethodBase.GetCurrentMethod().Name}");

                    codeMatcher
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ClosedBoltHandle), nameof(ClosedBoltHandle.Weapon))))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4, 13))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, 1f))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FVRFireArm), nameof(FVRFireArm.PlayAudioEvent))))
                        ;
                }

                return codeMatcher.InstructionEnumeration();
            }
        }
    }
}
