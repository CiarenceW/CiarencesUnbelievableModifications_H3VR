using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using FistVR;
using HarmonyLib;
using UnityEngine;
using CiarencesUnbelievableModifications.MonoBehaviours;
using System;
using BepInEx.Configuration;
using CiarencesUnbelievableModifications.Libraries;

namespace CiarencesUnbelievableModifications.Patches
{
    //2025 ciarence here, christ this code sucks
    public static class MagRetentionTweaks
    {
		private static float timeTouchpadHeldDown;

        //I hate labels and branches, rather do this instead
        //and I fucking hate you, what the fuck is this? moron
        public static bool CheckForQuickReleaseEligibility(FVRViveHand hand)
        {
            //the logic is here is that, if you press the palm mag button really quickly, it won't activate the quick release
            var heldDownCheck = (SettingsManager.configEnableQuickRetainedMagReleaseMaximumHoldTime.Value == true && timeTouchpadHeldDown >= SettingsManager.configQuickRetainedMagReleaseMaximumHoldTime.Value) || SettingsManager.configEnableQuickRetainedMagReleaseMaximumHoldTime.Value == false;

            //this is awful, like I can't overstate how fucking mad this code made me, from the shit inaccurate comment that made me remove these, to the bad variable naming, 2023 ciarence needs to ermmmmmmmmmmmmmmmmmmmmmmmmm I can't say it :)
            //anyways this checks if you're still holding shit, yeah
            var streamlinedCheck = (hand.IsInStreamlinedMode && hand.Input.AXButtonUp); 
            var classicCheck = (!hand.IsInStreamlinedMode && hand.Input.TouchpadUp);

            SettingsManager.LogVerboseInfo("Max time enabled: " + SettingsManager.configEnableQuickRetainedMagReleaseMaximumHoldTime.Value);
            SettingsManager.LogVerboseInfo("Time held: " + timeTouchpadHeldDown);
            SettingsManager.LogVerboseInfo($"thus, heldDownCheck is {heldDownCheck}");

            var result = SettingsManager.configEnableQuickRetainedMagRelease.Value && heldDownCheck && (streamlinedCheck || classicCheck);
            return result;
        }

        internal static class MagRetentionTweaksHarmonyFixes
        {
            [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.UpdateInteraction))]
			[HarmonyWrapSafe]
			[HarmonyPostfix]
            private static void PatchUpdateInteractionHoldingTouchPadDown(FVRViveHand hand)
            {
                if (hand.IsInStreamlinedMode && hand.Input.AXButtonPressed)
                {
                    timeTouchpadHeldDown += Time.deltaTime;
                }
                else if (!hand.IsInStreamlinedMode && hand.Input.TouchpadPressed && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.down) < 55f)
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
                    //it probably doesn't work because one of the opcodes from the base was changed because harmony hates me
                    //new CodeMatch(OpCodes.Bge_Un_S),
                    //new CodeMatch(OpCodes.Ldc_I4_1),
                    //new CodeMatch(OpCodes.Stloc_0)
                    );
                if (!codeMatcher.ReportFailure(__originalMethod, CiarencesUnbelievableModifications.Logger.LogError))
                {
                    SettingsManager.LogVerboseLevelNameAndColor($"Patching {MethodBase.GetCurrentMethod().Name}", "MRT-Transpilers", System.ConsoleColor.Cyan);

                    codeMatcher
                        .Advance(1)
                        //if (this.m_magChild != null)
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.m_magChild))))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldnull))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality", new[] { typeof(UnityEngine.Object), typeof(UnityEngine.Object) })))
                  /*->*///adding a branch here later on. Could do it right now but I'm superstitious. no you're a fucking moron
                  /*|*/
                  /*|*/ //flag = MagRetentionTweaks.CheckForQuickReleaseEligibility(hand);
                  /*|*/ .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1))
                  /*|*/ .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MagRetentionTweaks), nameof(CheckForQuickReleaseEligibility), new[] { typeof(FVRViveHand) })))
                  /*|*/ .InsertAndAdvance(new CodeInstruction(OpCodes.Stloc_0))
                  /*|*/ .CreateLabel(out Label label)
                  /*|*/ 
                  /*|*/ .Advance(-3)
                  /*<-*/.InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, label))
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
					SettingsManager.LogVerboseLevelNameAndColor($"Patching {MethodBase.GetCurrentMethod().Name}", "MRT-Transpilers", System.ConsoleColor.Cyan);

					codeMatcher.SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(SettingsManager), nameof(SettingsManager.configMagRetentionMinimumDistanceThreshold)))
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<>).MakeGenericType(typeof(float)), nameof(ConfigEntry<float>.Value))));
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
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SettingsManager), nameof(SettingsManager.configMagRetentionMinimumDotThreshold))),
						new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<>).MakeGenericType(typeof(float)), nameof(ConfigEntry<float>.Value))))

                        //... and we paste that bitch right here so we don't have to bother :)
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ble_Un_S, branchOperand))
                        ;
                }
                return codeMatcher.InstructionEnumeration();
            }
        }

        internal static class MagPalmKeepOffsetPatch
        {
            internal static class MagPalmKeepOffsetHarmonyPatches
            {
                [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.SetMagParent))]
                [HarmonyPostfix]
                private static void PatchSetMagParent(FVRFireArmMagazine __instance, FVRFireArmMagazine magParent)
                {
                    //Szikaka magic
                    if (magParent != null)
                    {
                        var magPoseExtender = __instance.GetComponent<FVRMagazinePoseExtender>();
                        magPoseExtender.relativeForward = magParent.transform.InverseTransformDirection(__instance.transform.forward);
                        magPoseExtender.relativeUp = magParent.transform.InverseTransformDirection(__instance.transform.up); //bitch
                    }
                }
            }

            internal static class MagPalmKeepOffsetTranspilers
            {
                [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.FVRFixedUpdate))]
                [HarmonyTranspiler]
                private static IEnumerable<CodeInstruction> TranspilePalmedMagazineRotation(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
                {
                    CodeMatcher codeMatcher = new CodeMatcher(instructions, generator).MatchForward(false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.m_magParent))),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Component), "get_transform")),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_rotation")),
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(FVRPhysicalObject), "set_PivotLockRot"))
                    );

					if (!codeMatcher.ReportFailure(__originalMethod, CiarencesUnbelievableModifications.Logger.LogError))
					{
						SettingsManager.LogVerboseLevelNameAndColor($"Patching {MethodBase.GetCurrentMethod().Name}", "MPKO-Transpilers", System.ConsoleColor.Cyan);

						codeMatcher
							.CreateLabelAt(codeMatcher.Pos, out var falseLabel)
							.CreateBranchAtMatch(true,
								out var endLabel,
								new CodeMatch(OpCodes.Ldarg_0),
								new CodeMatch(OpCodes.Ldarg_0),
								new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.m_magParent))),
								new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Component), "get_transform")),
								new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_rotation")),
								new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(FVRPhysicalObject), "set_PivotLockRot")),
								new CodeMatch(OpCodes.Br))
							.InsertAndAdvance(
								new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SettingsManager), nameof(SettingsManager.configEnableMagPalmKeepOffset))),
								new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ConfigEntry<>).MakeGenericType(typeof(bool)), nameof(ConfigEntry<bool>.Value))),
								new CodeInstruction(OpCodes.Brfalse_S, falseLabel),
								new CodeInstruction(OpCodes.Ldarg_0),
								new CodeInstruction(OpCodes.Ldarg_0),
								new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Component), nameof(Component.GetComponent), null, new[] { typeof(FVRMagazinePoseExtender) })),
								new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(FVRMagazinePoseExtender), nameof(FVRMagazinePoseExtender.GetRotation))),
								new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.PivotLockRot))),
								new CodeInstruction(OpCodes.Br_S, endLabel)
								);
					}

					return codeMatcher.InstructionEnumeration();
				}
			}
        }
    }
}
