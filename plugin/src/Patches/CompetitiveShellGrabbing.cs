using FistVR;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using CiarencesUnbelievableModifications.MonoBehaviours;
using BepInEx.Configuration;
using CiarencesUnbelievableModifications.Libraries;

namespace CiarencesUnbelievableModifications.Patches
{
    //Bounty idea by LiquidRemidi
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

		public static bool IsSingleShellGrabAction(FVRViveHand hand)
		{
			if (!SettingsManager.configGrabOneShellOnTrigger.Value 
				|| GM.Options.ControlOptions.GripButtonToHoldOverride == ControlOptions.GripButtonToHoldOverrideMode.ViveOverride //some people are weird
				|| hand.CMode == ControlMode.Vive) //some people have bad controllers, sorry
			{
				return false;
			}

			if (SettingsManager.configReverseGrabAndTrigger.Value)
			{
				if (hand.Input.IsGrabDown) return true;
			}
			else
			{
				if (hand.Input.TriggerDown) return true;
			}

			return false;
		}

		public static bool ShouldBeInline(Transform transform) //boolean bouillon
		{
			var round = transform.GetComponent<FVRFireArmRound>();

			if (!transform.TryGetComponent<FVRShotgunRoundPoseExtender>(out var extender)) return round.ProxyRounds.Count == 1;

			var vanillaCheck = (round.ProxyRounds.Count == 1 && (!SettingsManager.configEnableCompetitiveShellGrabbing.Value || extender.shouldPez));
			SettingsManager.LogVerboseInfo("Vanilla check: " + vanillaCheck);
			var hasLeverAction = (round.m_hand.OtherHand.CurrentInteractable != null && round.m_hand.OtherHand.CurrentInteractable is LeverActionFirearm);
			SettingsManager.LogVerboseInfo("has LeverAction: " + hasLeverAction);
			var hasRightAmount = (round.ProxyRounds.Count + 1) <= SettingsManager.configMaxShellsInHand.Value;
			SettingsManager.LogVerboseInfo("HasRightAmount: " + hasRightAmount);
			var noOneCaresAboutAmount = (round.ProxyRounds.Count + 1 > SettingsManager.configMaxShellsInHand.Value && !SettingsManager.configRevertToNormalGrabbingWhenAboveX.Value);
			SettingsManager.LogVerboseInfo("NoOneCarres About fuck you: " + noOneCaresAboutAmount);
			var shouldBeInline = (hasRightAmount || noOneCaresAboutAmount) && SettingsManager.configEnableCompetitiveShellGrabbing.Value && (!hasLeverAction || (hasLeverAction && !SettingsManager.configNoLeverAction.Value));
			SettingsManager.LogVerboseInfo("shouldBeInline: " + shouldBeInline);

			var result = vanillaCheck || (shouldBeInline && !extender.shouldPez) || SettingsManager.configForceUnconditionalCompetitiveShellGrabbing.Value;
			SettingsManager.LogVerboseInfo("Result: " + result);
			return result;
		}

		public static bool ShouldSpeedInsert(Transform transform)
		{
			var round = transform.GetComponent<FVRFireArmRound>();

			if (!transform.TryGetComponent<FVRShotgunRoundPoseExtender>(out var extender)) return round.ProxyRounds.Count < 1;

			var vanillaCheck = (round.ProxyRounds.Count < 1 && (!SettingsManager.configEnableCompetitiveShellGrabbing.Value || extender.shouldPez));
			//SettingsManager.LogVerboseInfo("Vanilla check: " + vanillaCheck);
			var hasLeverAction = (round.m_hand.OtherHand.CurrentInteractable != null && round.m_hand.OtherHand.CurrentInteractable is LeverActionFirearm); ;
			//SettingsManager.LogVerboseInfo("has LeverAction: " + hasLeverAction);
			var hasRightAmount = (round.ProxyRounds.Count % 2 == 0 && (round.ProxyRounds.Count + 1) <= SettingsManager.configMaxShellsInHand.Value && SettingsManager.configEnableCompetitiveShellGrabbing.Value && (!hasLeverAction || (hasLeverAction && !SettingsManager.configNoLeverAction.Value)));
			//SettingsManager.LogVerboseInfo("HasRightAmount: " + hasRightAmount);

			return vanillaCheck || (hasRightAmount && (!extender.shouldPez || SettingsManager.configForceUnconditionalCompetitiveShellGrabbing.Value));
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

				if (SettingsManager.configOnlyGrabPairAmountOfShells.Value && roundNum % 2 != 0)
				{
					if (roundNum - 1 == 0)
					{
						roundNum = 2;
					}
					else
					{
						roundNum++;
					}
				}
			}

			if (hand.OtherHand.CurrentInteractable is TubeFedShotgun fireArm)
			{
				if (fireArm.RoundType == __instance.RoundType)
				{
					if (fireArm.GetChambers().Count == 1 && SettingsManager.configOnlyGrabOneWhenChamberOpen.Value)
					{
						var fvrfireArmChamber = fireArm.GetChambers()[0];
						if (fvrfireArmChamber.IsManuallyChamberable && !fvrfireArmChamber.IsFull && fvrfireArmChamber.IsAccessible)
						{
							roundNum = 1;
						}
					}
				}
			}

			if (IsSingleShellGrabAction(hand))
			{
				roundNum = 1;
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
			[HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.BeginInteraction))]
			[HarmonyPrefix]
			private static bool GrabSingularIfSmartPalmingOff(ref FVRFireArmRound __instance, FVRViveHand hand)
			{
				if (!__instance.m_isSpawnLock && __instance.QuickbeltSlot != null && GM.Options.ControlOptions.SmartAmmoPalming == ControlOptions.SmartAmmoPalmingMode.Disabled && __instance.ObjectWrapper != null && (SettingsManager.configGrabOneWhenSmartPalmingOff.Value || IsSingleShellGrabAction(hand)) && __instance.ProxyRounds.Count > 0)
				{
					FVRQuickBeltSlot quickbeltSlot = __instance.QuickbeltSlot;
					var roundToGrabGO = Object.Instantiate<GameObject>(__instance.ObjectWrapper.GetGameObject(), __instance.transform.position, __instance.transform.rotation);

					var roundToGrabRound = roundToGrabGO.GetComponent<FVRFireArmRound>();
					if (__instance.m_canAnimate)
					{
						roundToGrabRound.BeginAnimationFrom(__instance.transform.position, __instance.transform.rotation);
					}

					var roundToQBGO = Object.Instantiate<GameObject>(__instance.ProxyRounds[0].ObjectWrapper.GetGameObject(), __instance.transform.position, __instance.transform.rotation);

					var roundToQBRound = roundToQBGO.GetComponent<FVRFireArmRound>();
					for (int proxyIndex = 1; proxyIndex < __instance.ProxyRounds.Count; proxyIndex++)
					{
						roundToQBRound.AddProxy(__instance.ProxyRounds[proxyIndex].Class, __instance.ProxyRounds[proxyIndex].ObjectWrapper);
					}
					__instance.ClearQuickbeltState();
					roundToQBRound.SetQuickBeltSlot(quickbeltSlot);
					__instance.DestroyAllProxies();
					roundToGrabRound.BeginInteraction(hand);
					hand.ForceSetInteractable(roundToGrabRound);
					roundToQBRound.UpdateProxyDisplay();
					UnityEngine.Object.Destroy(__instance.gameObject);

					return false;
				}
				else
				{
					return true;
				}
			}

			[HarmonyPatch(typeof(FVRFireArmRound), nameof(FVRFireArmRound.UpdateProxyPositions))]
			[HarmonyPostfix]
			private static void ResetProxyPoseMode(FVRFireArmRound __instance)
			{
				//otherwise 1/2 times it'll be picked up in standard mode, counter-intuitive I know.
				__instance.ProxyPose = FVRFireArmRound.ProxyPositionMode.Standard;
			}

			[HarmonyPatch(typeof(TubeFedShotgun), nameof(TubeFedShotgun.Awake))]
			[HarmonyPostfix]
			private static void IncreaseShotgunRoundInsertTriggerZone(TubeFedShotgun __instance)
			{
				if (SettingsManager.configIncreaseRoundInsertTriggerZone.Value)
				{
					var triggers = __instance.GetComponentsInChildren<FVRFireArmMagazineReloadTrigger>(); //multiple components for the KSG or whatever, hope it uses it, lol

					if (triggers != null && triggers.Length > 0)
					{
						for (int triggerIndex = 0; triggerIndex < triggers.Length; triggerIndex++)
						{
							var collider = triggers[triggerIndex].GetComponent<Collider>();

							if (collider is SphereCollider sphere) sphere.radius *= SettingsManager.configTriggerZoneMultiplier.Value;
							else if (collider is BoxCollider box)
							{
								var boxSize = box.size;
								boxSize.z *= SettingsManager.configTriggerZoneMultiplier.Value;

								box.size = boxSize;
							}
							else if (collider is CapsuleCollider capsule) capsule.height *= SettingsManager.configTriggerZoneMultiplier.Value;

							Debug.Log("increased trigger size");
						}
					}
				}
			}

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
				if (GM.Options.ControlOptions.SmartAmmoPalming == ControlOptions.SmartAmmoPalmingMode.Disabled)
				{
					if (__instance.TryGetComponent<FVRShotgunRoundPoseExtender>(out var _))
					{
						bool deleteProxies = false;

						if (IsSingleShellGrabAction(hand))
						{
							deleteProxies = true;
						}

						/*if (hand.OtherHand.CurrentInteractable is FVRFireArm fireArm)
						{
							if (fireArm.RoundType == __instance.RoundType)
							{
								if (fireArm.GetChambers().Count == 1 && SettingsManager.configOnlyGrabOneWhenChamberOpen.Value)
								{
									var fvrfireArmChamber = fireArm.GetChambers()[0];
									if (fvrfireArmChamber.IsManuallyChamberable && !fvrfireArmChamber.IsFull && fvrfireArmChamber.IsAccessible)
									{
										deleteProxies = true;
									}
								}
							}
						}*/

						if (deleteProxies)
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
					new CodeMatch(new CodeInstruction(OpCodes.Ldloc_3)),
					new CodeMatch(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(FVRFireArm), nameof(FVRFireArm.GetChambers)))),
					new CodeMatch(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<>).MakeGenericType(typeof(FVRFireArmChamber)), "get_Count"))),
					new CodeMatch(new CodeInstruction(OpCodes.Blt))
					))
				{
					SettingsManager.LogVerboseLevelNameAndColor($"Patching {MethodBase.GetCurrentMethod().Name}", "CSG-Transpilers", System.ConsoleColor.Cyan);

					//CompetitiveShellGrabbing.ForceNumRoundsPulled(base.transform, ref num, hand);
					codeMatcher
						.Advance(1)
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
					SettingsManager.LogVerboseLevelNameAndColor($"Patching {MethodBase.GetCurrentMethod().Name}", "CSG-Transpilers", System.ConsoleColor.Cyan);

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
					new CodeMatch(null, AccessTools.PropertyGetter(typeof(FVRViveHand), nameof(FVRViveHand.ClosestPossibleInteractable))), //this pisses me off, but H3VRUtils for some reasons replaces the Call OpCodes by Callvirt, maybe a side effect of using fucking MonoMod
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(FVRInteractiveObject), nameof(FVRInteractiveObject.SimpleInteraction))),
					new CodeMatch(OpCodes.Ldc_I4_1),
					new CodeMatch(OpCodes.Stloc_S),
					new CodeMatch(OpCodes.Ldc_I4_0)
					))
				{
					SettingsManager.LogVerboseLevelNameAndColor($"Patching {MethodBase.GetCurrentMethod().Name}", "CSG-Transpilers", System.ConsoleColor.Cyan);

					codeMatcher
						.SetAndAdvance(OpCodes.Ldloc_S, 13)
						.CreateLabelAt(codeMatcher.Pos, out var label)
						.InsertAndAdvance(
						new CodeInstruction(OpCodes.Brtrue, label),

						new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SettingsManager), nameof(SettingsManager.configGrabOneShellOnTrigger))),
						new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<>).MakeGenericType(typeof(bool)), nameof(ConfigEntry<bool>.Value))),
						new CodeInstruction(OpCodes.Brfalse, label),

						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.Input))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HandInput), nameof(HandInput.TriggerDown))),
						new CodeInstruction(OpCodes.Brfalse, label),

						new CodeInstruction(OpCodes.Ldarg_0),
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
					SettingsManager.LogVerboseLevelNameAndColor($"Patching {MethodBase.GetCurrentMethod().Name}", "CSG-Transpilers", System.ConsoleColor.Cyan);

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
					SettingsManager.LogVerboseLevelNameAndColor($"Patching {MethodBase.GetCurrentMethod().Name}", "CSG-Transpilers", System.ConsoleColor.Cyan);

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
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmRound), nameof(FVRFireArmRound.RoundType))),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(AM), nameof(AM.GetRoundPower))),
						new CodeMatch(OpCodes.Ldc_I4_3),
						new CodeMatch(OpCodes.Bne_Un)
						))
					{
						codeMatcher
							.Advance(1)
							.CreateBranchAtMatch(false, out var label,
							new CodeMatch(OpCodes.Ldarg_0),
							new CodeMatch(OpCodes.Ldc_I4_1),
							new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(FVRFireArmRound), nameof(FVRFireArmRound.ProxyPose)))
							)
							.InsertAndAdvance(
							new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SettingsManager), nameof(SettingsManager.configForceUnconditionalCompetitiveShellGrabbing))),
							new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ConfigEntry<>).MakeGenericType(typeof(bool)), nameof(ConfigEntry<bool>.Value))),
							new CodeInstruction(OpCodes.Brtrue, label)
							)
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
					SettingsManager.LogVerboseLevelNameAndColor($"Patching {MethodBase.GetCurrentMethod().Name}", "CSG-Transpilers", System.ConsoleColor.Cyan);

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