using CiarencesUnbelievableModifications.MonoBehaviours;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace CiarencesUnbelievableModifications.Patches
{
    public static class ReverseMagHoldPos
    {
        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.Awake))]
        [HarmonyPostfix]
        private static void AddMagazinePostExtenderComponent(FVRFireArmMagazine __instance)
        {
            if (!__instance.GetComponent<FVRMagazinePoseExtender>())
            {
                __instance.gameObject.AddComponent<FVRMagazinePoseExtender>();
            }
        }

        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.UpdateInteraction))]
        [HarmonyPostfix]
        private static void PatchUpdateInteracton(FVRFireArmMagazine __instance, ref FVRViveHand hand)
        {
            if (!SettingsManager.configEnableReverseMagHold.Value) return;

            var magPoseExtender = __instance.GetComponent<FVRMagazinePoseExtender>();

            if (magPoseExtender == null) return;

            if (!hand.IsInStreamlinedMode && hand.Input.TouchpadDown)
            {
                if (Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f || Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f)
                {
                    magPoseExtender.SwitchMagazinePose();
                }
            }

            if (hand.IsInStreamlinedMode && hand.Input.TriggerDown)
            {
                magPoseExtender.SwitchMagazinePose();
            }

            //reverse pose offset adjustment
            if (!hand.IsInStreamlinedMode && hand.Input.TriggerPressed && magPoseExtender.currentMagazinePose == FVRMagazinePoseExtender.CurrentMagazinePose.Reversed)
            {
                magPoseExtender.distance_override += Vector2.Dot(hand.Input.TouchpadAxes, Vector2.down) * 0.01f;
                SettingsManager.BindMagazineOffset(__instance).Value = magPoseExtender.distance_override; //persistent data stuff
                magPoseExtender.OffsetReverseHoldingPose();
            }

            /*if (magPoseExtender.currentMagazinePose == FVRMagazinePoseExtender.CurrentMagazinePose.Reversed && hand.CurrentHoveredQuickbeltSlot != null && hand.CurrentHoveredQuickbeltSlot.CurObject == null)
            {
                magPoseExtender.magazine.PoseOverride.localPosition = magPoseExtender.basePoseOverride.localPosition;
            }*/
        }

        //[HarmonyPatch(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.SetQuickBeltSlot))] not sure about what this does
        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.EndInteraction))]
        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.Load), new[] { typeof(AttachableFirearm) } )]
        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.Load), new[] { typeof(FVRFireArm) } )]
        [HarmonyPostfix]
        private static void PatchLoadMagazine(FVRFireArmMagazine __instance)
        {
            var magPoseExtender = __instance.GetComponent<FVRMagazinePoseExtender>();

            if (magPoseExtender != null)
            {
                if (magPoseExtender.currentMagazinePose == FVRMagazinePoseExtender.CurrentMagazinePose.Reversed)
                {
                    magPoseExtender.SwitchMagazinePose();
                }
            }
        }

        /*        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.UpdateInteraction))]
                [HarmonyTranspiler]
                private static IEnumerable<CodeInstruction> TranspileGrabbingMagFromQuickbelt(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
                {
                    CodeMatch[] matches = {
                        new CodeMatch(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.ClearQuickbeltState)))),
                        new CodeMatch(new CodeInstruction(OpCodes.Ldloc_S)),
                        new CodeMatch(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(FVRInteractiveObject), nameof(FVRInteractiveObject.ForceBreakInteraction)))),
                        new CodeMatch(new CodeInstruction(OpCodes.Ldloc_S)),
                        new CodeMatch(new CodeInstruction(OpCodes.Ldarg_0)),
                        new CodeMatch(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.SetMagParent)))),
                        new CodeMatch(new CodeInstruction(OpCodes.Ldc_I4_1))
                    };

                    if (TranspilerHelper.TryMatchForward(true, instructions, generator, out var codeMatcher, __originalMethod, CiarencesUnbelievableModifications.Logger.LogError, matches))
                    {
                        codeMatcher
                            .SetAndAdvance(OpCodes.Ldloc_S, 16)
                            .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Component), "get_gameObject")),
                            new CodeInstruction(OpCodes.Ldarg_1),
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ReverseMagHoldPos), nameof(ReverseMagHoldPos.HandleReverseMagGripFromQuickbelt))),
                            new CodeInstruction(OpCodes.Ldc_I4_1))
                            ;
                    }

                    var codeInstructions = codeMatcher.InstructionEnumeration();
                    foreach (CodeInstruction instruction in codeInstructions)
                    {
                        CiarencesUnbelievableModifications.Logger.LogInfo(instruction);
                    }

                    return codeMatcher.InstructionEnumeration();
                }*/

        /*[HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.DuplicateFromSpawnLock))]
        [HarmonyPostfix]
        private static void PatchDuplicateFromSpawnLock(FVRFireArmMagazine __instance, ref FVRFireArmMagazine __result)
        {
            var magPoseExtender = __instance.GetComponent<FVRMagazinePoseExtender>();

            if (magPoseExtender != null)
            {
                if (magPoseExtender.currentMagazinePose == FVRMagazinePoseExtender.CurrentMagazinePose.Reversed)
                {
                    __result.GetComponent<FVRMagazinePoseExtender>().currentMagazinePose = FVRMagazinePoseExtender.CurrentMagazinePose.Reversed;
                }
            }
        }*/

        /*[HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.SetMagParent))]
        [HarmonyPostfix]
        private static void PatchSetMagParent(ref FVRFireArmMagazine __instance, ref FVRFireArmMagazine magParent)
        {
            if (magParent != null)
            {
                Debug.Log(Vector3.Dot(magParent.m_hand.GetMagPose().up, __instance.PoseOverride.transform.up));
                if (Vector3.Dot(magParent.m_hand.GetMagPose().up, __instance.PoseOverride.transform.up) < 0)
                {
                    __instance.GetComponent<FVRMagazinePoseExtender>().SwitchMagazinePose();
                }
            }
        }*/

        [HarmonyPatch(typeof(FVRViveHand), nameof(FVRViveHand.DoInitialize))]
        [HarmonyPostfix]
        private static void PatchFVRViveHandInitialize(FVRViveHand __instance)
        {
            //otherwise I'd have to wait until the mag is in the hand to check the CMode and I can't be fucked I'm sorry
            FVRMagazinePoseExtender.CMode = __instance.CMode;
        }

        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.BeginInteraction))]
        [HarmonyPostfix]
        private static void PatchMagazineBeginInteraction(FVRFireArmMagazine __instance, FVRViveHand hand)
        {
            var magPoseExtender = __instance.GetComponent<FVRMagazinePoseExtender>();

            if (magPoseExtender != null)
            {
                if (!SettingsManager.configEnableReverseMagHold.Value) return;

                if (Vector3.Dot(hand.GetMagPose().up, __instance.QBPoseOverride.transform.up) > SettingsManager.configReverseMagGrabMinDotProduct.Value
                    && !__instance.m_isSpawnLock 
                    && (!SettingsManager.configReverseMagHoldHandgunOnly.Value || //if other hand's current interactible is a handgun and HandgunOnly mode is on and the current magazine is compatible with the gun
                        (__instance.m_hand.OtherHand.CurrentInteractable == null || __instance.m_hand.OtherHand.CurrentInteractable is Handgun gun &&
                        SettingsManager.configReverseMagHoldHandgunOnly.Value && 
                        __instance.MagazineType == gun.MagazineType)))
                {
                    magPoseExtender.SwitchMagazinePose();
                }
            }
        }
    }
}
