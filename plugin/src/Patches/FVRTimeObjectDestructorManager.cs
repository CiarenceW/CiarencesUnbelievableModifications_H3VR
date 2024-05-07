using CiarencesUnbelievableModifications.MonoBehaviours;
using FistVR;
using HarmonyLib;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;

namespace CiarencesUnbelievableModifications.Patches
{
    internal static class FVRTimeObjectDestructorManager
    {
        internal static class Patches
        {
            [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.Awake))]
            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.Awake))]
            [HarmonyPostfix]
            private static void TryAddTimedDestructor(FVRPhysicalObject __instance)
            {
                if (GM.CurrentSceneSettings.IsSpawnLockingEnabled && SettingsManager.configEnableTimedObjectDestruction.Value)
                {
                    __instance.GetOrAddComponent<FVRTimedObjectDestructor>();
                }
            }

            [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.Awake))]
            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.Awake))]
            [HarmonyPostfix]
            private static void ResetOnPickup(FVRPhysicalObject __instance)
            {
                if (__instance.TryGetComponent<FVRTimedObjectDestructor>(out var fVRTimedObject))
                {
                    fVRTimedObject.OnPickup();
                }
            }

            [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.Awake))]
            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.Awake))]
            private static void StartTimer()
            {

            }
        }
    }
}
