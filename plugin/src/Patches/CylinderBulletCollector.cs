using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CiarencesUnbelievableModifications.Patches
{
    internal static class CylinderBulletCollector
    {
        [HarmonyPatch(typeof(FVRFireArmChamber), nameof(FVRFireArmChamber.EjectRound), new[] { typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(bool) })]
        [HarmonyPostfix]
        private static void PatchChamberEjectRound(FVRFireArmRound __result, FVRFireArmChamber __instance)
        {
            if (__instance.Firearm is Revolver revolver)
            {
                //useless nullcheck, I'm guessing
                if (revolver.m_hand != null)
                {
                    if (revolver.m_hand.OtherHand.CurrentInteractable is RevolverCylinder)
                    {
                        revolver.m_hand.OtherHand.ForceSetInteractable(__result);
                    }
                if (!__result.IsSpent ||
                    (__result.IsSpent &&
                      SettingsManager.configEnableCollectingSpentCartridges.Value == true))
                    {

                    }
                }
            }
        }

        [HarmonyPatch(typeof(RevolverCylinder), nameof(RevolverCylinder.UpdateInteraction))]
        [HarmonyPostfix]
        private static void PatchRevolverCylinderUpdateInteraction(ref RevolverCylinder __instance, FVRViveHand hand)
        {
            if (hand.Input.TriggerDown)
            {
                //I DON'T WANT TO TRANSPILEEEEEEEEEEE
                List<FVRFireArmRound> rounds = new();
                foreach (FVRFireArmChamber chamber in __instance.Revolver.Chambers)
                {
                    if (chamber.GetRound() != null)
                    {
                        var round = chamber.GetRound();
                        if (!round.IsSpent ||
                            (round.IsSpent &&
                              SettingsManager.configEnableCollectingSpentCartridges.Value == true))
                        {
                            rounds.Add(round);
                        }
                    }
                }

                __instance.Revolver.EjectChambers();
                __instance.EndInteraction(hand);

                if (rounds.Count == 0)
                {
                    return;
                }
                for (int i = 0; i < rounds.Count; i++)
                {
                    if (i == 0)
                    {
                        rounds[0].BeginInteraction(hand);
                    }
                    else
                    {
                        CiarencesUnbelievableModifications.Logger.LogInfo("yo");
                        (hand.CurrentInteractable as FVRFireArmRound).PalmRound(rounds[i], false, true);
                    }
                }
            }
            else
            {
                handGrabbingCylinder = null;
            }
        }
    }
}
