using CiarencesUnbelievableModifications.MonoBehaviours;
using FistVR;
using HarmonyLib;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using Mono.Cecil;
using System.Text;
using CiarencesUnbelievableModifications.Libraries;

namespace CiarencesUnbelievableModifications.Patches
{
    internal static class FVRTimedObjectDestructorManager
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
					//__instance.GetOrAddComponent<FVRTimedObjectDestructor>();

					__instance.GetOrAddComponent<FVRTimedObjectDestructor>();
                }
            }

            [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.Awake))]
            [HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.Awake))]
			[HarmonyPatch(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.EndInteractionIntoInventorySlot))]
            [HarmonyPostfix]
            private static void ResetOnPickup(FVRPhysicalObject __instance)
            {
                if (__instance.TryGetComponent<FVRTimedObjectDestructor>(out var fVRTimedObject))
                {
                    fVRTimedObject.OnPickup();
                }
            }

			/*[HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.RemoveRound), [typeof(bool)])]
			[HarmonyPrefix]
			private static void StartTimerOnRemoveRound(FVRFireArmMagazine __instance)
			{
				typeof(FVRFireArmMagazine).GetMethod("fuck").GetMethodBody()

				var local = AccessTools.Method(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.RemoveRound)).Ge
			}*/

			[HarmonyPatch(typeof(FVRFireArmChamber), nameof(FVRFireArmChamber.EjectRound), [typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(bool)])]
			[HarmonyPatch(typeof(FVRFireArmChamber), nameof(FVRFireArmChamber.EjectRound), [typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Quaternion), typeof(bool)])]
			[HarmonyPostfix]
			private static void StartTimerOnRoundEject(FVRFireArmRound __result)
			{
				if (SettingsManager.configEnableTimedObjectDestruction.Value && GM.CurrentSceneSettings.IsSpawnLockingEnabled && SettingsManager.configTODEnableRounds.Value && __result != null)
				{
					__result.GetOrAddComponent<FVRTimedObjectDestructor>().OnDrop();
				}
			}

			[HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.Release))]
			[HarmonyPrefix]
			private static void StartTimerOnMagEject(FVRFireArmMagazine __instance)
			{
				if (SettingsManager.configTODEnableMagazines.Value && (!SettingsManager.configTODEmptyMagazinesOnly.Value || (SettingsManager.configTODEmptyMagazinesOnly.Value && __instance.m_numRounds == 0)) && __instance.TryGetComponent<FVRTimedObjectDestructor>(out var fVRTimedObject) && __instance.m_hand != null)
				{
					fVRTimedObject.OnDrop();
				}
			}

            [HarmonyPatch(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.EndInteraction))]
			[HarmonyPostfix]
            private static void StartTimer(FVRPhysicalObject __instance)
            {
				if (__instance.QuickbeltSlot == null && __instance.TryGetComponent<FVRTimedObjectDestructor>(out var fVRTimedObject))
				{
					if (__instance is FVRFireArmRound && SettingsManager.configTODEnableRounds.Value)
					{
						fVRTimedObject.OnDrop();
						return;
					}

					if (__instance is FVRFireArm && SettingsManager.configTODEnableFirearms.Value)
					{
						fVRTimedObject.OnDrop();
						return;
					}

					if (__instance is FVRFireArmMagazine magazine && SettingsManager.configTODEnableMagazines.Value)
					{
						if ((SettingsManager.configTODEmptyMagazinesOnly.Value && magazine.m_numRounds == 0) || !SettingsManager.configTODEmptyMagazinesOnly.Value)
						{
							fVRTimedObject.OnDrop();
							return;
						}
					}

					if (SettingsManager.configTODEnableAll.Value && (__instance != null)
						&& __instance.gameObject.activeSelf
						&& !__instance.IsHeld
						&& (__instance.QuickbeltSlot == null)
						&& (__instance.gameObject.transform.parent == null)
						&& (__instance.GetIsSaveLoadable())
						&& IM.HasSpawnedID(__instance.ObjectWrapper.SpawnedFromId))
					{
						fVRTimedObject.OnDrop();
					}
				}

			}
        }
    }
}
