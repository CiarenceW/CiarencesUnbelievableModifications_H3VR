using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using CiarencesUnbelievableModifications.Libraries;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace CiarencesUnbelievableModifications.Patches
{
    internal static class FireArmTweaks
    {
        internal static class SpawnStockTweaks //I want to be forced to do more things before a weapon becomes operational :)
        {
            [HarmonyPatch(typeof(FVRFireArm), nameof(FVRFireArm.Awake))]
            [HarmonyPostfix]
            private static void PatchFVRFireArmStartStock(FVRFireArm __instance)
            {
                if (!SettingsManager.configEnableStockFoldOnSpawn.Value || GM.IsAsyncLoading) return;
                if (__instance.HasStockPos())
                {
                    if (__instance.GetStockPos() != null) //basically uh, fuck me
                    {
                        if (__instance.TryGetComponentInChildren<FVRFoldingStockYAxis>(out var result))
                        {
                            var eulers = result.Stock.localEulerAngles;
                            eulers.y = result.isMinClosed ? result.MinRot : result.MaxRot; //I do be preserving other axis' rotation doe
                            result.Stock.localEulerAngles = eulers;
                            SettingsManager.LogVerboseInfo(result.Stock.localEulerAngles);
                            result.m_curPos = FVRFoldingStockYAxis.StockPos.Closed;
                        }
                        else
                        {
                            SettingsManager.LogVerboseInfo("can't find FVRFoldingStockYAxis");
                        }
                    }
                    else
                    {
                        SettingsManager.LogVerboseInfo("StockPos is null");
                    }
                }
            }

            [HarmonyPatch(typeof(FVRFoldingStockXAxis), nameof(FVRFoldingStockXAxis.Start))] //aren't they all closed by default?
            [HarmonyPrefix]
            private static void PatchFVRStockXAxisStart(FVRFoldingStockXAxis __instance)
            {
                if (!SettingsManager.configEnableStockFoldOnSpawn.Value || GM.IsAsyncLoading)
                {
                    var eulers = __instance.Stock.localEulerAngles;
                    eulers.y = __instance.isMinClosed ? __instance.MinRot : __instance.MaxRot;
                    __instance.Stock.localEulerAngles = eulers;
                    __instance.m_curPos = FVRFoldingStockXAxis.StockPos.Closed;
                }
            }
        }

        internal static class BitchDontTouchMyGun
        {
            [HarmonyPatch(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.BeginInteraction))]
            [HarmonyPrefix]
            private static bool SkipBeginInteractIfAlreadyHeld(FVRPhysicalObject __instance, FVRViveHand hand)
			{

				if (__instance is FVRFireArm gun && ((gun is Handgun && SettingsManager.configOnlyHandguns.Value) || (!SettingsManager.configOnlyHandguns.Value)) && __instance.m_hand != null && !__instance.IsAltHeld && __instance.m_hand == hand.OtherHand && SettingsManager.configEnableFuckYouBitchDontGrabMyGun.Value) //you little fucker
				{
					hand.CurrentInteractable = null;
					return false;
				}
				return true;
			}
		}

		internal static class AddChamberLoadingForMoreFireArms
		{
			[HarmonyPatch(typeof(Handgun), nameof(Handgun.Awake))]
			[HarmonyPostfix]
			private static void AddChamberLoading(Handgun __instance)
			{
				if (__instance.AudioClipSet.ChamberManual.Clips.Count == 0)
				{
					__instance.AudioClipSet.ChamberManual.Clips = __instance.AudioClipSet.MagazineInsertRound.Clips;
					__instance.AudioClipSet.ChamberManual.VolumeRange = Vector2.one * 0.6f;
					__instance.AudioClipSet.ChamberManual.PitchRange = new Vector2(1.2f, 0.8f);
				}

				if (!__instance.Chamber.TryGetComponent<BoxCollider>(out var _)) //Maybe there's already one, idk whatever I don't care
				{
					var chamberLoadingTrigger = __instance.Chamber.AddComponent<BoxCollider>();
					chamberLoadingTrigger.isTrigger = true;
					chamberLoadingTrigger.size = new Vector3(0.01f, 0.01f, 0.1f);
				}
			}

			[HarmonyPatch(typeof(Handgun), nameof(Handgun.UpdateDisplayRoundPositions))]
			[HarmonyPostfix]
			private static void SetNotManuallyAccessibleIfMagInserted(Handgun __instance)
			{
				if (__instance.Chamber.IsAccessible)
				{
					__instance.Chamber.IsAccessible = __instance.Magazine == null;
				}
			}

			[HarmonyPatch(typeof(ClosedBoltWeapon), nameof(ClosedBoltWeapon.Awake))]
			[HarmonyPostfix]
			private static void AddChamberLoading(ClosedBoltWeapon __instance)
			{
				__instance.Chamber.IsManuallyChamberable = true;

				if (!__instance.Chamber.TryGetComponent<BoxCollider>(out var _)) //Maybe there's already one, idk whatever I don't care
				{
					var chamberLoadingTrigger = __instance.Chamber.AddComponent<BoxCollider>();
					chamberLoadingTrigger.isTrigger = true;
					chamberLoadingTrigger.size = new Vector3(0.01f, 0.01f, 0.1f);
				}
			}

			[HarmonyPatch(typeof(ClosedBolt), nameof(ClosedBolt.UpdateBolt))]
			[HarmonyPostfix]
			private static void AddChamberLoadingSupport(ClosedBolt __instance)
			{
				if (__instance.CurPos == ClosedBolt.BoltPos.Forward) //Handguns already do this, so all they need is a trigger for the bullet, closed bolt weapons don't have the accessible flag set anywhere
				{
					__instance.Weapon.Chamber.IsAccessible = false;
				}
				else
				{
					__instance.Weapon.Chamber.IsAccessible = true;
				}
			}

			internal static class BullshitDoubleFeedPatchedSMIDGEONWHYDIDYOUMAKETHISSOCOMPLICATED
			{
				internal static AccessTools.FieldRef<object, float> fuckingDoubleFeedChanceFuckingFieldRefFucking;

				[HarmonyPatch(typeof(HandgunSlide), nameof(HandgunSlide.SlideEvent_ExtractRoundFromMag))]
				[HarmonyPrefix]
				private static void MakeDoubleFeedHappenInTheMostConvolutedWayPossible(HandgunSlide __instance, out ThisIsJustATuple_Really<bool, float, object> __state)
				{
					__state = new();
					if (__instance.Handgun.Chamber.GetRound() != null)
					{
						foreach (MonoBehaviour behaviour in __instance.GetComponents<MonoBehaviour>())
						{
							if (behaviour.GetType().Name.Contains("DoubleFeedData"))
							{
								if (fuckingDoubleFeedChanceFuckingFieldRefFucking == null) fuckingDoubleFeedChanceFuckingFieldRefFucking = AccessTools.FieldRefAccess<float>(behaviour.GetType(), "doubleFeedChance");
								__state.a = true;
								__state.b = fuckingDoubleFeedChanceFuckingFieldRefFucking.Invoke(behaviour);
								__state.c = behaviour;
								fuckingDoubleFeedChanceFuckingFieldRefFucking.Invoke(behaviour) = 1f; //FUCKKKKKKKKKKKKKKKKKKKKKKKKKKKKKK
								break;
							}
						}
					}
				}

				[HarmonyPatch(typeof(HandgunSlide), nameof(HandgunSlide.SlideEvent_ExtractRoundFromMag))]
				[HarmonyPostfix]
				private static void ResetDoubleFeedChances(ThisIsJustATuple_Really<bool, float, object> __state)
				{
					if (__state.a)
					{
						fuckingDoubleFeedChanceFuckingFieldRefFucking.Invoke(__state.c) = __state.b;
					}
				}
			}
		}

		internal static class KnockAKDrumOut
		{
			[HarmonyPatch(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.OnCollisionEnter))]
			[HarmonyPostfix]
			private static void CheckIfCanKnockDrumOut(FVRPhysicalObject __instance, Collision col)
			{
				if (!SettingsManager.configEnableKnockAKDrumOut.Value) return;

				if (__instance is not FVRFireArmMagazine magazine) return;

				if (col.transform.TryGetComponent<FVRFireArm>(out var gun))
				{
					SettingsManager.LogVerboseInfo("Is gun");
					if (__instance.m_hand != null)
					{
						SettingsManager.LogVerboseInfo("Hand isn't null");

						if ((gun.TryGetComponentInChildren<PhysicalMagazineReleaseLatch>(out var _) || (SettingsManager.configForAllNonEjectableGuns.Value && ((gun is OpenBoltReceiver openBolt && !openBolt.HasMagReleaseButton) || (gun is ClosedBoltWeapon closedBolt && !closedBolt.HasMagReleaseButton) || (gun is Handgun handgun && !handgun.HasMagReleaseButton) || (gun is BoltActionRifle boltAction && !boltAction.HasMagEjectionButton)))) && __instance.GetComponent<Rigidbody>() != null && __instance.GetComponent<Rigidbody>().velocity.magnitude > 1f)
						{
							if (gun.Magazine != null && !gun.Magazine.IsIntegrated && !gun.Magazine.GetCanPalm())
							{
								SettingsManager.LogVerboseInfo("Has PhysicalMagazineReleaseLatch");
								if (gun.m_hand != null)
								{
									bool shouldEject = false;

									if (gun.m_hand.IsInStreamlinedMode)
									{
										if (gun.m_hand.Input.AXButtonPressed)
										{
											shouldEject = true;
										}
									}
									else
									{
										if (gun.m_hand.Input.TouchpadPressed && Vector2.Distance(gun.m_hand.Input.TouchpadAxes, Vector2.down) < 45f)
										{
											shouldEject = true;
										}

										SettingsManager.LogVerboseInfo(Vector2.Distance(gun.m_hand.Input.TouchpadAxes, Vector2.down) < 45f);
									}

									if (shouldEject)
									{
										SettingsManager.LogVerboseInfo("Bump ejecting mag!");
										gun.EjectMag(true);
									}
								}
							}
						}
					}
				}
			}
		}

		internal static class IncrementalSmoothing
		{
			[HarmonyPatch(typeof(FVRPhysicalObject), "FU")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> AddIncrementalSmoothingThing(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
			{
				CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

				if (codeMatcher.TryMatchForward(true, __originalMethod,
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.AltGrip))),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FVRInteractiveObject), nameof(FVRInteractiveObject.m_hand))),
					new CodeMatch(OpCodes.Ldflda, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.Input))),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(HandInput), nameof(HandInput.TriggerPressed))),
					new CodeMatch(OpCodes.Brfalse)
					))
				{
					var triggerPressedBrTrue = generator.DefineLabel();

					var thisIsForWhenItsFalse = (Label)codeMatcher.Operand;

					codeMatcher.InsertAndAdvance(
						new CodeInstruction(OpCodes.Brtrue_S, triggerPressedBrTrue),

						new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SettingsManager), nameof(SettingsManager.configEnableIncrementalHandSmoothing))),
						new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ConfigEntry<bool>), nameof(ConfigEntry<bool>.Value)))
					)
					.Advance(1)
					.InsertAndAdvance(	
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.AltGrip))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRInteractiveObject), nameof(FVRInteractiveObject.m_hand))),
						new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.Input))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HandInput), nameof(HandInput.TriggerFloat))),
						new CodeInstruction(OpCodes.Ldc_R4, 0f),
						new CodeInstruction(OpCodes.Ble_Un, thisIsForWhenItsFalse)
					)
					.Labels.Add(triggerPressedBrTrue)
					;

					if (codeMatcher.TryMatchForward(false, __originalMethod,
						new CodeMatch(OpCodes.Ldloc_S, null),
						new CodeMatch(OpCodes.Ldc_R4, 1.35f),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Pow), [typeof(float), typeof(float)])),
						new CodeMatch(OpCodes.Stloc_S, null)
						))
					{

						var brTrueLabel = generator.DefineLabel();
						var brLabel = generator.DefineLabel();

						codeMatcher
						.InsertAndAdvance(
							new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SettingsManager), nameof(SettingsManager.configEnableIncrementalHandSmoothing))),
							new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ConfigEntry<bool>), nameof(ConfigEntry<bool>.Value))),
							new CodeInstruction(OpCodes.Brtrue_S, brTrueLabel)
						)
						.Advance(4)
						.InsertAndAdvance(
							new CodeInstruction(OpCodes.Br_S, brLabel),

							new CodeInstruction(OpCodes.Ldloc_S, 69).WithLabels(brTrueLabel), //omg!!!!!!! dude!!!!! it's the SEX number!!!! HAHAHAHAHAHAH this is so fucking funny!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
							new CodeInstruction(OpCodes.Ldarg_0),
							new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.AltGrip))),
							new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRInteractiveObject), nameof(FVRInteractiveObject.m_hand))),
							new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.Input))),
							new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HandInput), nameof(HandInput.TriggerFloat))),
							new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SettingsManager), nameof(SettingsManager.configIncrementalHandSmoothingMaxStrength))),
							new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ConfigEntry<float>), nameof(ConfigEntry<float>.Value))),
							new CodeInstruction(OpCodes.Mul),
							new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Pow), [typeof(float), typeof(float)])),
							new CodeInstruction(OpCodes.Stloc_S, 69)
						)
						.Labels.Add(brLabel)
						;
					}
				}

				codeMatcher.Print();

				return codeMatcher.InstructionEnumeration();
			}

			/*[HarmonyPatch(typeof(FVRPhysicalObject), "FU")]
			[LocalVariableAccessor] //I fucking love this
			[HarmonyPostfix]
			private static void LogThing(FVRPhysicalObject __instance, float __localVariable_69)
			{
				if (__instance.AltGrip && __instance.AltGrip.m_hand) 
				{
					var input = __instance.AltGrip.m_hand.Input;

					CiarencesUnbelievableModifications.Logger.LogInfo($"GetMinWhatever: {__instance.GetMinStabilizationAllowanceFactor()}");
					CiarencesUnbelievableModifications.Logger.LogInfo($"__localVariable69: {__localVariable_69}");
					CiarencesUnbelievableModifications.Logger.LogInfo($"Is trigger pressed: {input.TriggerPressed}");
					CiarencesUnbelievableModifications.Logger.LogInfo($"TriggerFloat: {input.TriggerFloat}");
				}
			}*/
		}

		internal struct ThisIsJustATuple_Really<A, B, C>()
		{
			public A a; 
			public B b;
			public C c;
		}
	}
}
