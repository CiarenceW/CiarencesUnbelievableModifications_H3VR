using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CiarencesUnbelievableModifications.Patches
{
    public static class CylinderBulletCollector
    {
        public static void PlaceRoundIntoHand(FVRFireArmRound round, FVRViveHand hand)
        {
            //we don't want spent rounds, I don't even think that they can be palmed
            if (round.IsSpent)
            {
                return;
            }

            if (hand.CurrentInteractable is FVRFireArmRound roundInHand)
            {
                roundInHand.PalmRound(round, false, true);
            }
            else
            {
                //do this to prevent the cylinder from rotating a million times, doesn't actually work. Shit.
                if (hand.CurrentInteractable is FVRInteractiveObject cylinderInteractiveObject) 
                    cylinderInteractiveObject.EndInteraction(hand);

                //why isn't there a method that does both of those things
                hand.ForceSetInteractable(round);
                round.BeginInteraction(hand);
            }
        }

        public static bool TryPlaceRoundIntoHand(GameObject revolverGameObject, ref bool flag)
        {
            Revolver revolver = revolverGameObject.GetComponent<Revolver>();
            if (SettingsManager.Verbose) CiarencesUnbelievableModifications.Logger.LogInfo($"Revolver: {revolver}");
            if (revolver.Cylinder.m_hand != null && revolver.Cylinder.m_hand.Input.TriggerDown)
            {
                FVRViveHand hand = revolver.Cylinder.m_hand;
                for (int k = 0; k < revolver.Chambers.Length; k++)
                {
                    var currentChamber = revolver.Chambers[k];
                    if (currentChamber.IsFull)
                    {
                        flag = true;
                        if (revolver.AngInvert)
                        {
                            PlaceRoundIntoHand(currentChamber.EjectRound(currentChamber.transform.position + currentChamber.transform.forward * revolver.Cylinder.CartridgeLength, currentChamber.transform.forward, UnityEngine.Random.onUnitSphere, true), hand);
                        }
                        else
                        {
                            PlaceRoundIntoHand(currentChamber.EjectRound(currentChamber.transform.position + -currentChamber.transform.forward * revolver.Cylinder.CartridgeLength, -currentChamber.transform.forward, UnityEngine.Random.onUnitSphere, true), hand);
                        }
                    }

                }
                return true;
            }
            return false;
        }

        [HarmonyPatch(typeof(RevolverCylinder), nameof(RevolverCylinder.UpdateInteraction))]
        [HarmonyPostfix]
        private static void PatchRevolverCylinderUpdateInteraction(ref RevolverCylinder __instance, FVRViveHand hand)
        {
            //any way I can access the Ejector? I don't want to GetComponent<>()
            if (hand.Input.TriggerDown && !__instance.Revolver.isCylinderArmLocked)
            {
                __instance.Revolver.EjectChambers();
            }
        }

        public static class CylinderBulletCollectorTranspiler
        {
            [HarmonyPatch(typeof(Revolver), nameof(Revolver.EjectChambers))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> TranspileRevolverEjectChambers(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions, generator).MatchForward(true,
                    new CodeMatch(new CodeInstruction(OpCodes.Ldloc_0)),
                    new CodeMatch(new CodeInstruction(OpCodes.Brfalse))
                    );

                codeMatcher.CreateLabelAt(codeMatcher.Length - 1, out var labelEnd);
                codeMatcher.Operand = labelEnd;

                codeMatcher.MatchForward(true,
                     new CodeMatch(new CodeInstruction(OpCodes.Ldlen)),
                     new CodeMatch(new CodeInstruction(OpCodes.Conv_I4)),
                     new CodeMatch(new CodeInstruction(OpCodes.Blt)),
                     new CodeMatch(new CodeInstruction(OpCodes.Br))
                     );

                if (!codeMatcher.ReportFailure(__originalMethod, CiarencesUnbelievableModifications.Logger.LogError))
                {
                    if (SettingsManager.Verbose) CiarencesUnbelievableModifications.Logger.LogInfo($"Patching {MethodBase.GetCurrentMethod().Name}");

                    codeMatcher
                        .RemoveInstruction()
                        .SetAndAdvance(OpCodes.Ldarg_0, null)
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Component), "get_gameObject")))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloca_S, 0))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CylinderBulletCollector), nameof(CylinderBulletCollector.TryPlaceRoundIntoHand))))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_0))
                        ;

                    var pos = codeMatcher.Pos;

                    codeMatcher.MatchForward(true,
                        new CodeMatch(new CodeInstruction(OpCodes.Ldloc_0)),
                        new CodeMatch(new CodeInstruction(OpCodes.Brfalse))
                        );

                    codeMatcher.CreateLabelAt(codeMatcher.Pos, out var label);

                    codeMatcher.Advance(pos - codeMatcher.Pos);

                    codeMatcher
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Brtrue, label))
                        ;
                }

                return codeMatcher.InstructionEnumeration();
            }
        }
    }
}
