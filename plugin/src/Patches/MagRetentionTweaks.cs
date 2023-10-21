using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace CiarencesUnbelievableModifications.Patches
{
    public static class MagRetentionTweaks
    {
        public static float magRetentionMinimumDistanceThreshold;

        //probably should use Vector3.Axis instead of Vector3.Dot, lol
        public static float magRetentionDotProductThreshold;

        public static float timeTouchpadHeldDown;

        //I hate labels and branches, rather do this instead
        public static bool CheckForQuickReleaseEligibility(FVRViveHand hand)
        {
            //these are from when I was debugging. I don't want to change any of it because
            //I'm scared it'll somehow not work anymore even though I fixed the thing that wasn't working
            var streamlined = (hand.IsInStreamlinedMode && hand.Input.AXButtonUp);

            var notstreamlined = (!hand.IsInStreamlinedMode && hand.Input.TouchpadUp);

            var heldDownCheck = (SettingsManager.configEnableQuickRetainedMagReleaseMaximumHoldTime.Value == true && 
                    (timeTouchpadHeldDown >= SettingsManager.configQuickRetainedMagReleaseMaximumHoldTime.Value)) ||
                        SettingsManager.configEnableQuickRetainedMagReleaseMaximumHoldTime.Value == false;

            CiarencesUnbelievableModifications.Logger.LogInfo("Max time enabled: " + SettingsManager.configEnableQuickRetainedMagReleaseMaximumHoldTime.Value);
            CiarencesUnbelievableModifications.Logger.LogInfo("Time held: " + SettingsManager.configQuickRetainedMagReleaseMaximumHoldTime.Value);
            CiarencesUnbelievableModifications.Logger.LogInfo($"thus, heldDownCheck is {heldDownCheck}");

            var result = (SettingsManager.configEnableQuickRetainedMagRelease.Value && (streamlined || notstreamlined)) && heldDownCheck;
            return result;
        }

        internal static class MagRetentionTweaksHarmonyFixes
        {
            [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.UpdateInteraction))]
            [HarmonyPostfix]
            private static void PatchUpdateInteractionHoldingTouchPadDown(FVRViveHand hand)
            {
                if (hand.IsInStreamlinedMode && hand.Input.AXButtonPressed)
                {
                    timeTouchpadHeldDown += Time.deltaTime;
                }
                else if (!hand.IsInStreamlinedMode && hand.Input.TouchpadPressed)
                {
                    timeTouchpadHeldDown += Time.deltaTime;
                }
                else
                {
                    timeTouchpadHeldDown = 0f;
                }
            }
        }

        internal static class MagRetentionTweaksTranspilers
        {
            [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.UpdateInteraction))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> TranspileQuickRetainedMagazineRelease(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions, generator).MatchForward(true,
                    //the first instance of this opcode is the one we need B)
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    new CodeMatch(OpCodes.Stloc_0)

                    //aren't these supposed to be like "oh hey who cares about the operand let's just check the opcode"? if so, why doesn't it fucking work
                    //new CodeMatch(OpCodes.Bge_Un_S),
                    //new CodeMatch(OpCodes.Ldc_I4_1),
                    //new CodeMatch(OpCodes.Stloc_0)
                    );
                if (!codeMatcher.ReportFailure(__originalMethod, CiarencesUnbelievableModifications.Logger.LogError))
                {
                    if (SettingsManager.Verbose) CiarencesUnbelievableModifications.Logger.LogInfo($"Patching {MethodBase.GetCurrentMethod().Name}");

                    codeMatcher
                        .Advance(1)
                        //if (this.m_magChild != null)
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.m_magChild))))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldnull))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality", new[] { typeof(UnityEngine.Object), typeof(UnityEngine.Object) })))
                        //adding a branch here later on. Could do it right here but I'm superstitious.

                        //flag = MagRetentionTweaks.CheckForQuickReleaseEligibility(hand);
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MagRetentionTweaks), nameof(CheckForQuickReleaseEligibility), new[] { typeof(FVRViveHand) })))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Stloc_0))
                        .CreateLabel(out Label label)

                        .Advance(-3)
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, label))
                        ;
                }

                //Instruction Printer
                //for (int i = 0; i < codeMatcher.InstructionEnumeration().Count(); i++)
                //{
                //    CiarencesUnbelievableModifications.Logger.LogInfo(codeMatcher.InstructionEnumeration().ToList()[i]);
                //}
                return codeMatcher.InstructionEnumeration();
            }

            [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.Release))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> TranspileMagazineRetentionThresholds(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions, generator).MatchForward(true,
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.IsBeltBox))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(FVRFireArm), nameof(FVRFireArm.GetMagMountPos))),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_position")),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Vector3), nameof(Vector3.Distance))),
                new CodeMatch(OpCodes.Ldc_R4, null)
                );

                if (!codeMatcher.ReportFailure(__originalMethod, CiarencesUnbelievableModifications.Logger.LogError))
                {
                    if (SettingsManager.Verbose) CiarencesUnbelievableModifications.Logger.LogInfo($"Patching {MethodBase.GetCurrentMethod().Name}");

                    codeMatcher.SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(MagRetentionTweaks), nameof(magRetentionMinimumDistanceThreshold)));
                    //thanks Szikaka (we copy the label of the current branch...)
                    var branchOperand = codeMatcher.Operand;

                    // && Vector3.Dot(otherHand.CurrentInteractable.transform.up, this.FireArm.GetMagEjectPos(this.IsBeltBox).up) > 0.7f

                    codeMatcher
                        .Advance(1)
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_1))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(FVRViveHand), "get_CurrentInteractable")))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_transform")))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_up")))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.FireArm))))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.IsBeltBox))))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(FVRFireArm), nameof(FVRFireArm.GetMagEjectPos), new[] { typeof(bool) })))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_up")))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector3), nameof(Vector3.Dot), new[] { typeof(Vector3), typeof(Vector3) })))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MagRetentionTweaks), nameof(magRetentionDotProductThreshold))))

                        //... and we paste that bitch right here so we don't have to bother :)
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ble_Un_S, branchOperand))
                        ;
                }
                return codeMatcher.InstructionEnumeration();
            }
        }
    }
}
