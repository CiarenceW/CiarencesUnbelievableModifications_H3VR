using UnityEngine;
using HarmonyLib;
using FistVR;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using CiarencesUnbelievableModifications.Libraries;
using BepInEx.Configuration;

namespace CiarencesUnbelievableModifications.Patches
{
	//most of the math is from https://stackoverflow.com/a/7869457
    internal static class TurnScopeDialWithJoystick
    {
#pragma warning disable CS0649
        static float prevAngle;
		static bool wasLastAngleValid;
#pragma warning restore CS0649

		private static float AngleFromVec(Vector2 stickPos)
        {
            stickPos = stickPos.normalized;

			var result = (Mathf.Acos(Vector2.Dot(stickPos, Vector2.up)) * Mathf.Rad2Deg);

			if (stickPos.x < 0)
			{
				result = 360 - result;
			}

			return result;
        }

        private static float CalculateDifferenceBetweenAngles(float a)
        {
            return CustomFModThing((a + 180f), 360f) - 180f;
        }

		static float CustomFModThing(float x, float y)
		{
			return x - Mathf.Floor(x / y) * y;
		}

        [HarmonyPatch(typeof(PIPScopeInteraction), nameof(PIPScopeInteraction.UpdateInteraction))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> MakeDialMovableWithJoystick(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

            // float curRot = this.m_curRot;
            if (codeMatcher.TryMatchForward(false, __originalMethod,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PIPScopeInteraction), nameof(PIPScopeInteraction.m_curRot))),
                new CodeMatch(OpCodes.Stloc_0)
            ))
            {
                var isOptionTurnedOffLabel = generator.DefineLabel();

                var wasLastAngleValidLabel = generator.DefineLabel();

				var isCurrentAngleValidLabel = generator.DefineLabel();

                var skipOver = generator.DefineLabel();

                var currentAngleLocal = generator.DeclareLocal(typeof(float));

                codeMatcher
                .AddLabels([isOptionTurnedOffLabel])
                .InsertAndAdvance(
                    //  if (SettingsManager.configEnableTurnScopeDialWithJoystick.Value)
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SettingsManager), nameof(SettingsManager.configEnableTurnScopeDialWithJoystick))),
					new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ConfigEntry<bool>), nameof(ConfigEntry<bool>.Value))),
                    new CodeInstruction(OpCodes.Brfalse_S, isOptionTurnedOffLabel),

					// && !hand.IsInStreamlinedMode
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.IsInStreamlinedMode))),
					new CodeInstruction(OpCodes.Brtrue_S, isOptionTurnedOffLabel),

					// 		float num = 0;
					new CodeInstruction(OpCodes.Ldc_R4, 0.0f),
					new CodeInstruction(OpCodes.Stloc_3),

                    //      if (Vector2.Distance(hand.Input.TouchpadAxes, Vector2.zero) > 0.8f)
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.Input))),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HandInput), nameof(HandInput.TouchpadAxes))),
                    new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector2), nameof(Vector2.zero))),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector2), nameof(Vector2.Distance))),
                    new CodeInstruction(OpCodes.Ldc_R4, 0.8f),
                    new CodeInstruction(OpCodes.Ble_Un, isCurrentAngleValidLabel),

                    //          var currentAngle = AngleFromVec(hand.Input.TouchpadAxes);
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.Input))),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HandInput), nameof(HandInput.TouchpadAxes))),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.AngleFromVec))),
                    new CodeInstruction(OpCodes.Stloc_S, currentAngleLocal.LocalIndex),

                    //          if (wasLastAngleValid)
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.wasLastAngleValid))),
                    new CodeInstruction(OpCodes.Brfalse_S, wasLastAngleValidLabel),

                    //              num = CalculateDifferenceBetweenAngles(prevAngle - currentAngle) * SettingsManager.configTurnScopeDialWithJoystickSensitivity.Value;
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.prevAngle))),
                    new CodeInstruction(OpCodes.Ldloc_S, currentAngleLocal.LocalIndex),
                    new CodeInstruction(OpCodes.Sub),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.CalculateDifferenceBetweenAngles))),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SettingsManager), nameof(SettingsManager.configTurnScopeDialWithJoystickSensitivity))),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ConfigEntry<float>), nameof(ConfigEntry<float>.Value))),
                    new CodeInstruction(OpCodes.Mul),
                    new CodeInstruction(OpCodes.Stloc_3),

                    //          prevAngle = currentAngle;
                    new CodeInstruction(OpCodes.Ldloc_S, currentAngleLocal.LocalIndex).WithLabels(wasLastAngleValidLabel),
                    new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.prevAngle))),

                    //          wasLastAngleValid = true;
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.wasLastAngleValid))),

                    new CodeInstruction(OpCodes.Br, skipOver),

					new CodeInstruction(OpCodes.Ldc_I4_0) { labels = [isCurrentAngleValidLabel] },
					new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.wasLastAngleValid))),

                    new CodeInstruction(OpCodes.Br, skipOver)
                )
                ;

                // if (this.RotationMagnitudePerOption < 0f)
                if (codeMatcher.TryMatchForward(false, __originalMethod,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PIPScopeInteraction), nameof(PIPScopeInteraction.RotationMagnitudePerOption))),
                    new CodeMatch(OpCodes.Ldc_R4, 0.0f),
                    new CodeMatch(OpCodes.Bge_Un)
                ))
                {
                    codeMatcher.Labels.Add(skipOver);
                }
            }

            return codeMatcher.InstructionEnumeration();
        }

		[HarmonyPatch(typeof(PIPScopeMagnificationInteraction), nameof(PIPScopeMagnificationInteraction.UpdateInteraction))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> MakeMagnificationThingMovableWithJoystickTooBecauseIGuessTheyJustNEEDEDToBeDifferentClasses(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

            // float curRot = this.m_curRot;
            if (codeMatcher.TryMatchForward(false, __originalMethod,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PIPScopeMagnificationInteraction), nameof(PIPScopeMagnificationInteraction.m_curRot))),
                new CodeMatch(OpCodes.Stloc_0)
            ))
            {
                var isOptionTurnedOffLabel = generator.DefineLabel();

                var wasLastAngleValidLabel = generator.DefineLabel();

				var isCurrentAngleValidLabel = generator.DefineLabel();

                var skipOver = generator.DefineLabel();

                var currentAngleLocal = generator.DeclareLocal(typeof(float));

                codeMatcher
                .AddLabels([isOptionTurnedOffLabel])
                .InsertAndAdvance(
                    //  if (SettingsManager.configEnableTurnScopeDialWithJoystick.Value)
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SettingsManager), nameof(SettingsManager.configEnableTurnScopeDialWithJoystick))),
					new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ConfigEntry<bool>), nameof(ConfigEntry<bool>.Value))),
                    new CodeInstruction(OpCodes.Brfalse_S, isOptionTurnedOffLabel),

					// && !hand.IsInStreamlinedMode
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.IsInStreamlinedMode))),
					new CodeInstruction(OpCodes.Brtrue_S, isOptionTurnedOffLabel),

					// 		float num = 0;
					new CodeInstruction(OpCodes.Ldc_R4, 0.0f),
					new CodeInstruction(OpCodes.Stloc_3),

                    //      if (Vector2.Distance(hand.Input.TouchpadAxes, Vector2.zero) > 0.8f)
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.Input))),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HandInput), nameof(HandInput.TouchpadAxes))),
                    new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector2), nameof(Vector2.zero))),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector2), nameof(Vector2.Distance))),
                    new CodeInstruction(OpCodes.Ldc_R4, 0.8f),
                    new CodeInstruction(OpCodes.Ble_Un, isCurrentAngleValidLabel),

                    //          var currentAngle = AngleFromVec(hand.Input.TouchpadAxes);
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.Input))),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HandInput), nameof(HandInput.TouchpadAxes))),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.AngleFromVec))),
                    new CodeInstruction(OpCodes.Stloc_S, currentAngleLocal.LocalIndex),

                    //          if (wasLastAngleValid)

                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.wasLastAngleValid))),
                    new CodeInstruction(OpCodes.Brfalse_S, wasLastAngleValidLabel),

                    //              num = CalculateDifferenceBetweenAngles(currentAngle - prevAngle) * SettingsManager.configTurnScopeDialWithJoystickSensitivity.Value;
                    new CodeInstruction(OpCodes.Ldloc_S, currentAngleLocal.LocalIndex),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.prevAngle))),
                    new CodeInstruction(OpCodes.Sub),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.CalculateDifferenceBetweenAngles))),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SettingsManager), nameof(SettingsManager.configTurnScopeDialWithJoystickSensitivity))),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ConfigEntry<float>), nameof(ConfigEntry<float>.Value))),
                    new CodeInstruction(OpCodes.Mul),
                    new CodeInstruction(OpCodes.Stloc_3),

                    //          prevAngle = currentAngle;
                    new CodeInstruction(OpCodes.Ldloc_S, currentAngleLocal.LocalIndex).WithLabels(wasLastAngleValidLabel),
                    new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.prevAngle))),

                    //          wasLastAngleValid = true;
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.wasLastAngleValid))),

                    new CodeInstruction(OpCodes.Br, skipOver),

					new CodeInstruction(OpCodes.Ldc_I4_0) { labels = [isCurrentAngleValidLabel] },
					new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.wasLastAngleValid))),

                    new CodeInstruction(OpCodes.Br, skipOver)
                )
                ;

                // if (this.RotationMagnitudePerOption < 0f)
                if (codeMatcher.TryMatchForward(false, __originalMethod,
                    new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PIPScopeMagnificationInteraction), nameof(PIPScopeMagnificationInteraction.m_curRot))),
					new CodeMatch(OpCodes.Ldloc_3),
					new CodeMatch(OpCodes.Sub)
                ))
                {
                    codeMatcher.Labels.Add(skipOver);
                }
            }

            return codeMatcher.InstructionEnumeration();
        }

        // [HarmonyPatch(typeof(PIPScopeInteraction), nameof(PIPScopeInteraction.UpdateInteraction))]
        // [HarmonyPostfix]
        // private static void DebugStuff(PIPScopeInteraction __instance, [LocalVariableAccessor(3)] float __localVariable_num, float ___m_curRot)
        // {
        //     CiarencesUnbelievableModifications.Logger.LogInfo($"previous angle: {prevAngle}");
        //     CiarencesUnbelievableModifications.Logger.LogInfo($"m_curRot: {___m_curRot}");
        //     CiarencesUnbelievableModifications.Logger.LogInfo($"num: {__localVariable_num}");
		// 	CiarencesUnbelievableModifications.Logger.LogInfo($"diff: {CalculateDifferenceBetweenAngles(prevAngle - __localVariable_num) * SettingsManager.configTurnScopeDialWithJoystickSensitivity.Value}");
        // }
    }
}