using FistVR;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using CiarencesUnbelievableModifications.MonoBehaviours;
using Steamworks;

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

            var vanillaCheck = (round.ProxyRounds.Count == 1 && (!SettingsManager.configEnableCompetitiveShellGrabbing.Value || round.GetComponent<FVRShotgunRoundPoseExtender>().shouldPez));
            SettingsManager.LogVerboseInfo("Vanilla check: " + vanillaCheck);
            var hasLeverAction = (round.m_hand.OtherHand.CurrentInteractable != null && round.m_hand.OtherHand.CurrentInteractable is LeverActionFirearm);
            SettingsManager.LogVerboseInfo("has LeverAction: " + hasLeverAction);
            var hasRightAmount = (round.ProxyRounds.Count + 1) <= SettingsManager.configMaxShellsInHand.Value;
            SettingsManager.LogVerboseInfo("HasRightAmount: " + hasRightAmount);
            var noOneCaresAboutAmount = (round.ProxyRounds.Count + 1 > SettingsManager.configMaxShellsInHand.Value && !SettingsManager.configRevertToNormalGrabbingWhenAboveX.Value);
            SettingsManager.LogVerboseInfo("NoOneCarres About fuck you: " + noOneCaresAboutAmount);
            var shouldBeInline = (hasRightAmount || noOneCaresAboutAmount) && SettingsManager.configEnableCompetitiveShellGrabbing.Value && (!hasLeverAction || (hasLeverAction && !SettingsManager.configNoLeverAction.Value));
            SettingsManager.LogVerboseInfo("shouldBeInline: " + shouldBeInline);

            return vanillaCheck || (shouldBeInline && !round.GetComponent<FVRShotgunRoundPoseExtender>().shouldPez);
        }

        public static bool ShouldSpeedInsert(Transform transform)
        {
            var round = transform.GetComponent<FVRFireArmRound>();

            var vanillaCheck = (round.ProxyRounds.Count < 1 && (!SettingsManager.configEnableCompetitiveShellGrabbing.Value || round.GetComponent<FVRShotgunRoundPoseExtender>().shouldPez));
            //SettingsManager.LogVerboseInfo("Vanilla check: " + vanillaCheck);
            var hasLeverAction = (round.m_hand.OtherHand.CurrentInteractable != null && round.m_hand.OtherHand.CurrentInteractable is LeverActionFirearm); ;
            //SettingsManager.LogVerboseInfo("has LeverAction: " + hasLeverAction);
            var hasRightAmount = (round.ProxyRounds.Count % 2 == 0 && (round.ProxyRounds.Count + 1) <= SettingsManager.configMaxShellsInHand.Value && SettingsManager.configEnableCompetitiveShellGrabbing.Value && (!hasLeverAction || (hasLeverAction && !SettingsManager.configNoLeverAction.Value)));
            //SettingsManager.LogVerboseInfo("HasRightAmount: " + hasRightAmount);

            return vanillaCheck || (hasRightAmount && !round.GetComponent<FVRShotgunRoundPoseExtender>().shouldPez);
        }

        //anton decided to not reuse the "GetNumRoundsPulled" method for some reason
        //and just copied everything in it and added some shit for the DuplicateFromSpawnLock
        //so I have to do this
        public static void ForceNumRoundsPulled(FVRFireArmRound __instance, ref int roundNum, FVRViveHand hand)
        {
            if (SettingsManager.configEnableCompetitiveShellGrabbing.Value)
            {
                if (AM.GetRoundPower(__instance.RoundType) == FVRObject.OTagFirearmRoundPower.Shotgun && SettingsManager.configOnlyGrabXFromQB.Value)
                {
                    if (roundNum > SettingsManager.configMaxShellsInHand.Value)
                    {
                        /*if (hand.OtherHand.CurrentInteractable == null)
                        {
                            roundNum = Mathf.Min(SettingsManager.configMaxShellsInHand.Value, __instance.ProxyRounds.Count);
                        }
                        else */
                        if (hand.OtherHand.CurrentInteractable != null && hand.OtherHand.CurrentInteractable is FVRFireArm gun && gun.Magazine != null)
                        {
                            if (gun.RoundType == __instance.RoundType)
                            {
                                roundNum = Mathf.Min(SettingsManager.configMaxShellsInHand.Value, gun.Magazine.m_capacity - gun.Magazine.m_numRounds);
                            }
                        }
                    }
                }

                if ((!hand.Input.IsGrabbing && hand.Input.TriggerDown && SettingsManager.configGrabOneShellOnTrigger.Value) || (hand.Input.IsGrabbing && !hand.Input.TriggerDown && SettingsManager.configReverseGrabAndTrigger.Value))
                {
                    roundNum = 1;
                }
            }
        }

        //"Reference to type 'FVRFireArmRound' claims it is defined in 'Assembly-CSharp', but it could not be found"
        public static void ForceNumRoundsPulledTrans(Transform bitch, ref int roundNum, FVRViveHand hand)
        {
            ForceNumRoundsPulled(bitch.GetComponent<FVRFireArmRound>(), ref roundNum, hand);
        }

        public static void TransferShotgunPoseExtenderProperties(GameObject oldGo, GameObject newGo)
        {
            if (oldGo.transform.TryGetComponent<FVRShotgunRoundPoseExtender>(out var oldShell))
            {
                if (newGo.transform.TryGetComponent<FVRShotgunRoundPoseExtender>(out var newShell))
                {
                    SettingsManager.LogVerboseInfo("New shell: " + newShell.shouldPez);

                    newShell.shouldPez = oldShell.shouldPez;

                    newShell.hasAncestor = true;

                    SettingsManager.LogVerboseInfo("Old shell: " + oldShell.shouldPez);

                    SettingsManager.LogVerboseInfo("THIS BITCH IS GETTING TRANSFERED");
                }
            }
        }

        public static void ResetShouldPez(GameObject go)
        {
            if (go.TryGetComponent<FVRShotgunRoundPoseExtender>(out var extender))
            {
                extender.shouldPez = false;
                extender.hasAncestor = false;
            }
        }

        /*public static bool ShouldPickupSingleShell(FVRViveHand hand, GameObject go)
        {
            if (go.TryGetComponent<FVRFireArmRound>(out var round))
            {
                this.CurrentInteractable = this.ClosestPossibleInteractable;
                this.m_state = FVRViveHand.HandState.GripInteracting;
                this.CurrentInteractable.BeginInteraction(this);
                this.Buzz(this.Buzzer.Buzz_BeginInteraction);
                flag5 = true;
            }
        }*/

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

            private static FieldInfo m_hoverOverReloadTriggerFieldInfo;

            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.BeginInteraction))]
            [HarmonyPostfix]
            private static void TryAddPoseExtenderPoses(FVRFireArmRound __instance)
            {
                if (__instance.TryGetComponent<FVRShotgunRoundPoseExtender>(out var result))
                {
                    result.SetOverrideTransforms();
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

            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.DuplicateFromSpawnLock))]
            [HarmonyPostfix]
            private static void DestroyDuplicatesIfSingleGrab(FVRFireArmRound __instance, ref GameObject __result, FVRViveHand hand)
            {
                if ((!hand.Input.IsGrabbing && hand.Input.TriggerDown && SettingsManager.configGrabOneShellOnTrigger.Value) || (hand.Input.IsGrabbing && !hand.Input.TriggerDown && SettingsManager.configReverseGrabAndTrigger.Value))
                {
                    var round = __result.GetComponent<FVRFireArmRound>();

                    for (int i = round.ProxyRounds.Count - 1; i >= 0; i--)
                    {
                        global::UnityEngine.Object.Destroy(round.ProxyRounds[i].GO);
                        round.ProxyRounds[i].GO = null;
                        round.ProxyRounds[i].Filter = null;
                        round.ProxyRounds[i].Renderer = null;
                        round.ProxyRounds[i].ObjectWrapper = null;
                    }
                    round.ProxyRounds.Clear();
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

            [HarmonyPatch(typeof(FVRViveHand), "Update")]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> ResetShouldPez(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

                if (codeMatcher.TryMatchForward(true, __originalMethod,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldflda),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Brfalse),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Stloc_S),
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Brfalse)
                    ))
                {
                    SettingsManager.LogVerboseInfo($"Patching {MethodBase.GetCurrentMethod().Name}");

                    codeMatcher
                        .Advance(1)
                        .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(FVRViveHand), nameof(FVRViveHand.CurrentInteractable))),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject))),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CompetitiveShellGrabbing), nameof(CompetitiveShellGrabbing.ResetShouldPez)))
                        )
                        ;
                }

                return codeMatcher.InstructionEnumeration();
            }

            [HarmonyPatch(typeof(FVRViveHand), "Update")]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> TryGetSingleShell(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

                if (codeMatcher.TryMatchForward(true, __originalMethod,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(FVRViveHand), nameof(FVRViveHand.ClosestPossibleInteractable))),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(FVRInteractiveObject), nameof(FVRInteractiveObject.SimpleInteraction))),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Stloc_S),
                    new CodeMatch(OpCodes.Ldc_I4_0)
                    ))
                {
                    SettingsManager.LogVerboseInfo($"Patching {MethodBase.GetCurrentMethod().Name}");

                    codeMatcher
                        .SetAndAdvance(OpCodes.Ldarg_0, null)
                        .CreateLabelAt(codeMatcher.Pos, out var label)
                        .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(FVRViveHand), nameof(FVRViveHand.ClosestPossibleInteractable))),
                        new CodeInstruction(OpCodes.Ldnull),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Object), "op_Inequality")),
                        new CodeInstruction(OpCodes.Brfalse_S, label),

                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(FVRViveHand), nameof(FVRViveHand.ClosestPossibleInteractable))),
                        new CodeInstruction(OpCodes.Isinst, typeof(FVRFireArmRound)),
                        new CodeInstruction(OpCodes.Ldnull),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Object), "op_Inequality")),
                        new CodeInstruction(OpCodes.Brfalse_S, label),

                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(FVRViveHand), nameof(FVRViveHand.ClosestPossibleInteractable))),
                        new CodeInstruction(OpCodes.Call, AccessTools.PropertySetter(typeof(FVRViveHand), nameof(FVRViveHand.CurrentInteractable))),

                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.m_state))),

                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(FVRViveHand), nameof(FVRViveHand.CurrentInteractable))),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(FVRInteractiveObject), nameof(FVRInteractiveObject.BeginInteraction))),

                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.Buzzer))),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRHaptics), nameof(FVRHaptics.Buzz_BeginInteraction))),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FVRViveHand), nameof(FVRViveHand.Buzz))),

                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Stloc_S, 14)
                        );

                        var stlocInst = new CodeInstruction(codeMatcher.Instruction);
                        stlocInst.ExtractLabels();
                        codeMatcher
                        .SetAndAdvance(OpCodes.Ldc_I4_0, null)
                        .InsertAndAdvance(stlocInst)
                        ;
                }

                codeMatcher.Print();

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
                            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Multiply", new[] { typeof(Vector3), typeof(float) })),
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

                return codeMatcher.InstructionEnumeration();
            }


            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.CycleToProxy))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> AddPropertyTransferPoint(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

                if (codeMatcher.TryMatchForward(true, __originalMethod,
                    new CodeMatch(new CodeInstruction(OpCodes.Ldarg_2)),
                    new CodeMatch(new CodeInstruction(OpCodes.Brtrue)),
                    new CodeMatch(new CodeInstruction(OpCodes.Ldarg_0)),
                    new CodeMatch(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmRound), nameof(FVRFireArmRound.ProxyRounds)))),
                    new CodeMatch(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<>).MakeGenericType(typeof(FVRFireArmRound.ProxyRound)), "Clear")))
                    ))
                {
                    SettingsManager.LogVerboseInfo($"Patching {MethodBase.GetCurrentMethod().Name}");

                    codeMatcher
                        .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject))),
                        new CodeInstruction(OpCodes.Ldloc_2),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CompetitiveShellGrabbing), nameof(CompetitiveShellGrabbing.TransferShotgunPoseExtenderProperties)))
                        )
                        ;
                }

                return codeMatcher.InstructionEnumeration();
            }
        }
    }
}