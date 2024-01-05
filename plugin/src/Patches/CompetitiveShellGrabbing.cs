using FistVR;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using CiarencesUnbelievableModifications.MonoBehaviours;

namespace CiarencesUnbelievableModifications.Patches
{
    //I'm about to fucking die
    public static class CompetitiveShellGrabbing
    {
        public static Vector3 CalculateShellPositions(Vector3 basePos, Vector3 baseRadius, Transform transform, int j)
        {
            if (!SettingsManager.configEnableCompetitiveShellGrabbing.Value)
            {
                return basePos + baseRadius * (j + 1);
            }
            else
            {
                var radius = transform.GetComponent<CapsuleCollider>().radius;
                var baseHeight = -transform.up * (radius * 1.75f);
                return basePos + baseRadius * ((j + 1) % 2) + baseHeight * ((j + 1) / 2); //cleanest bit of math I've ever done
            }
        }

        public static Vector3 CalculateQBShellPositions(Vector3 basePos, Vector3 baseUp, Transform transform, int k)
        {
            if (!SettingsManager.configEnableCompetitiveShellGrabbing.Value || AM.GetRoundPower(transform.GetComponent<FVRFireArmRound>().RoundType) != FVRObject.OTagFirearmRoundPower.Shotgun)
            {
                return basePos + baseUp * (k + 2);
            }
            else
            {
                var baseLength = -transform.forward * (transform.GetComponent<CapsuleCollider>().height * 1.02f);
                return basePos + baseUp * ((k + 1) / 2) + baseLength * ((k + 1) % 2);
            }
        }

        public static bool ShouldBeInline(Transform transform) //boolean bouillon
        {
            var round = transform.GetComponent<FVRFireArmRound>();

            var vanillaCheck = (round.ProxyRounds.Count == 1 && (!SettingsManager.configEnableCompetitiveShellGrabbing.Value));
            SettingsManager.LogVerboseInfo("Vanilla check: " + vanillaCheck);
            var hasLeverAction = (round.m_hand.OtherHand.CurrentInteractable != null && round.m_hand.OtherHand.CurrentInteractable is LeverActionFirearm);
            SettingsManager.LogVerboseInfo("has LeverAction: " + hasLeverAction);
            var hasRightAmount = (round.ProxyRounds.Count + 1) <= SettingsManager.configMaxShellsInHand.Value;
            SettingsManager.LogVerboseInfo("HasRightAmount: " + hasRightAmount);
            var noOneCaresAboutAmount = (round.ProxyRounds.Count + 1 > SettingsManager.configMaxShellsInHand.Value && !SettingsManager.configRevertToNormalGrabbingWhenAboveX.Value);
            SettingsManager.LogVerboseInfo("NoOneCarres About fuck you: " + noOneCaresAboutAmount);
            var shouldBeInline = (hasRightAmount || noOneCaresAboutAmount) && SettingsManager.configEnableCompetitiveShellGrabbing.Value && (!hasLeverAction || (hasLeverAction && !SettingsManager.configNoLeverAction.Value));
            SettingsManager.LogVerboseInfo("shouldBeInline: " + shouldBeInline);

            return vanillaCheck || shouldBeInline;
        }

        public static bool ShouldSpeedInsert(Transform transform)
        {
            var round = transform.GetComponent<FVRFireArmRound>();

            var vanillaCheck = (round.ProxyRounds.Count < 1 && (!SettingsManager.configEnableCompetitiveShellGrabbing.Value));
            //SettingsManager.LogVerboseInfo("Vanilla check: " + vanillaCheck);
            var hasLeverAction = (round.m_hand.OtherHand.CurrentInteractable != null && round.m_hand.OtherHand.CurrentInteractable is LeverActionFirearm); ;
            //SettingsManager.LogVerboseInfo("has LeverAction: " + hasLeverAction);
            var hasRightAmount = (round.ProxyRounds.Count % 2 == 0 && (round.ProxyRounds.Count + 1) <= SettingsManager.configMaxShellsInHand.Value && SettingsManager.configEnableCompetitiveShellGrabbing.Value && (!hasLeverAction || (hasLeverAction && !SettingsManager.configNoLeverAction.Value)));
            //SettingsManager.LogVerboseInfo("HasRightAmount: " + hasRightAmount);

            return vanillaCheck || hasRightAmount;
        }

        //anton decided to not reuse the "GetNumRoundsPulled" method for some reason
        //and just copied everything in it and added some shit for the DuplicateFromSpawnLock
        //so I have to do this
        public static void ForceNumRoundsPulled(FVRFireArmRound __instance, ref int roundNum, FVRViveHand hand) 
        {
            if (AM.GetRoundPower(__instance.RoundType) == FVRObject.OTagFirearmRoundPower.Shotgun && SettingsManager.configOnlyGrabXFromQB.Value && SettingsManager.configEnableCompetitiveShellGrabbing.Value)
            {
                if (roundNum > SettingsManager.configMaxShellsInHand.Value)
                {
                    if (hand.OtherHand.CurrentInteractable == null)
                    {
                        roundNum = Mathf.Min(SettingsManager.configMaxShellsInHand.Value, __instance.ProxyRounds.Count);
                    }
                    else if (hand.OtherHand.CurrentInteractable != null && hand.OtherHand.CurrentInteractable is FVRFireArm gun && gun.Magazine != null)
                    {
                        if (gun.RoundType == __instance.RoundType)
                        {
                            roundNum = Mathf.Min(SettingsManager.configMaxShellsInHand.Value, gun.Magazine.m_capacity - gun.Magazine.m_numRounds);
                        }
                    }
                }
            }
        }

        //"Reference to type 'FVRFireArmRound' claims it is defined in 'Assembly-CSharp', but it could not be found"
        public static void ForceNumRoundsPulledTrans(Transform bitch, ref int roundNum, FVRViveHand hand)
        {
            ForceNumRoundsPulled(bitch.GetComponent<FVRFireArmRound>(), ref roundNum, hand);
        }

        internal static class Patches
        {
            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.Awake))]
            [HarmonyPostfix]
            private static void AddPoseExtender(FVRFireArmRound __instance)
            {
                if (AM.GetRoundPower(__instance.RoundType) == FVRObject.OTagFirearmRoundPower.Shotgun)
                {
                    __instance.gameObject.AddComponent<FVRShotgunRoundPoseExtender>();
                }
            }

            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.BeginInteraction))]
            [HarmonyPostfix]
            private static void TryAddPoseExtenderPoses(FVRFireArmRound __instance) 
            {
                if (__instance.TryGetComponent<FVRShotgunRoundPoseExtender>(out var result))
                {
                    result.SetOverrideTransforms();
                    result.SwitchTransform();
                }
            }

            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.GetNumRoundsPulled))] //too lazy to transpile sorry
            [HarmonyPostfix]
            private static void PatchForceNumRoundsPulled(FVRFireArmRound __instance, ref int __result, FVRViveHand hand)
            {
                ForceNumRoundsPulled(__instance, ref __result, hand);
            }

            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.UpdateProxyPositions))]
            [HarmonyPostfix]
            private static void CheckIfShouldSwitchPos(FVRFireArmRound __instance)
            {
                if (__instance.TryGetComponent<FVRShotgunRoundPoseExtender>(out var result))
                {
                    if (__instance.m_hand != null && !ShouldBeInline(__instance.transform))
                    {
                        result.SwitchTransform(true);
                    }
                }
            }
        }

        internal static class Transpilers
        {
            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.DuplicateFromSpawnLock))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> AddForceNumRoundsPulled(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

                if (codeMatcher.TryMatchForward(true, __originalMethod,
                    new CodeMatch(new CodeInstruction(OpCodes.Ldloc_S)),
                    new CodeMatch(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmClip), nameof(FVRFireArmClip.m_capacity)))),
                    new CodeMatch(new CodeInstruction(OpCodes.Ldloc_S)),
                    new CodeMatch(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmClip), nameof(FVRFireArmClip.m_numRounds)))),
                    new CodeMatch(new CodeInstruction(OpCodes.Sub)),
                    new CodeMatch(new CodeInstruction(OpCodes.Stloc_2))
                    ))
                {
                    SettingsManager.LogVerboseInfo($"Patching {MethodBase.GetCurrentMethod().Name}");

                    //CompetitiveShellGrabbing.ForceNumRoundsPulled(base.transform, ref num, hand);
                    codeMatcher
                        .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Component), "get_transform")),
                        new CodeInstruction(OpCodes.Ldloca_S, 2), //there's not "Ldloca_2", interesting
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CompetitiveShellGrabbing), nameof(CompetitiveShellGrabbing.ForceNumRoundsPulledTrans)))
                        )
                        ;
                }

                return codeMatcher.InstructionEnumeration();
            }

            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.FVRFixedUpdate))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> ChangeCheckForSpeedierRoundLoad(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

                if (codeMatcher.TryMatchForward(false, __originalMethod,
                    new CodeMatch(new CodeInstruction(OpCodes.Ldarg_0)),
                    new CodeMatch(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmRound), nameof(FVRFireArmRound.ProxyRounds)))),
                    new CodeMatch(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<>).MakeGenericType(typeof(FVRFireArmRound.ProxyRound)), "get_Count"))),
                    new CodeMatch(new CodeInstruction(OpCodes.Ldc_I4_1)),
                    new CodeMatch(new CodeInstruction(OpCodes.Bge))
                    ))
                {
                    SettingsManager.LogVerboseInfo($"Patching {MethodBase.GetCurrentMethod().Name}");

                    codeMatcher
                        .RemoveInstructions(4)
                        .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Component), "get_transform")),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CompetitiveShellGrabbing), nameof(CompetitiveShellGrabbing.ShouldSpeedInsert)))
                        )
                        .SetOpcodeAndAdvance(OpCodes.Brfalse)
                        ;
                }

                return codeMatcher.InstructionEnumeration();
            }

            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.UpdateProxyPositions))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> MakeAlternativeShellHoldingPose(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

                if (codeMatcher.TryMatchForward(false, __originalMethod,
                    new CodeMatch(new CodeInstruction(OpCodes.Ldarg_0)),
                    new CodeMatch(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmRound), nameof(FVRFireArmRound.ProxyRounds)))),
                    new CodeMatch(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(System.Collections.Generic.List<>).MakeGenericType(typeof(FVRFireArmRound.ProxyRound)), "get_Count"))),
                    new CodeMatch(new CodeInstruction(OpCodes.Ldc_I4_1)),
                    new CodeMatch(new CodeInstruction(OpCodes.Bne_Un))
                    ))
                {
                    SettingsManager.LogVerboseInfo($"Patching {MethodBase.GetCurrentMethod().Name}");

                    codeMatcher
                        .RemoveInstructions(4) //yeet round count check
                        .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Component), "get_transform")),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CompetitiveShellGrabbing), nameof(CompetitiveShellGrabbing.ShouldBeInline)))
                        )
                        .SetOpcodeAndAdvance(OpCodes.Brfalse)
                        ;

                    if (codeMatcher.TryMatchForward(true, __originalMethod,
                        new CodeMatch(new CodeInstruction(OpCodes.Ldloc_0)),
                        new CodeMatch(new CodeInstruction(OpCodes.Ldloc_2)),
                        new CodeMatch(new CodeInstruction(OpCodes.Ldloc_3))
                        ))
                    {
                        codeMatcher
                            //.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, vector1.LocalIndex))
                            .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Component), "get_transform")))
                            .Advance(1)
                            .RemoveInstructions(5)
                            .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CompetitiveShellGrabbing), nameof(CompetitiveShellGrabbing.CalculateShellPositions))))
                            ;

                        if (codeMatcher.TryMatchForward(true, __originalMethod, 
                            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Multiply", new[] {typeof(Vector3), typeof(float)} )),
                            new CodeMatch(OpCodes.Stloc_S),
                            new CodeMatch(OpCodes.Ldc_I4_0),
                            new CodeMatch(OpCodes.Stloc_S),
                            new CodeMatch(OpCodes.Br),
                            new CodeMatch(OpCodes.Ldarg_0),
                            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmRound), nameof(FVRFireArmRound.ProxyRounds))),
                            new CodeMatch(OpCodes.Ldloc_S),
                            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(List<>).MakeGenericType(typeof(FVRFireArmRound.ProxyRound)), "get_Item")),
                            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmRound.ProxyRound), nameof(FVRFireArmRound.ProxyRound.GO))),
                            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), "get_transform")),
                            new CodeMatch(OpCodes.Ldloc_S),
                            new CodeMatch(OpCodes.Ldloc_S),
                            new CodeMatch(OpCodes.Ldloc_S)
                            ))
                        {
                            codeMatcher
                            .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Component), "get_transform")))
                            .Advance(1)
                            .RemoveInstructions(5)
                            .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CompetitiveShellGrabbing), nameof(CompetitiveShellGrabbing.CalculateQBShellPositions))))
                            ;
                        }
                    }
                }

                codeMatcher.Print();

                return codeMatcher.InstructionEnumeration();
            }
        }
    }
}