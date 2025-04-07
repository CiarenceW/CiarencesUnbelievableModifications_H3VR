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
    internal static class TurnScopeDialWithJoystick
    {
        static float prevAngle;
        static bool wasLastAngleValid;

        private static float AngleFromVec(Vector2 stickPos)
        {
            stickPos = stickPos.normalized;

            float s = Mathf.Asin(stickPos.x);
            float c = Mathf.Acos(stickPos.y) * Mathf.Rad2Deg;

            return s > 0 ? c : Mathf.PI - c;
        }

        private static float CalculateDifferenceBetweenAngles(float a)
        {
            return (a + 180f) % 360f - 180f;
        }

        private static void Templace(FVRViveHand hand)
        {
            float num = 0;

            if (SettingsManager.configEnableTurnScopeDialWithJoystick.Value)
            {
                if (Vector2.Distance(hand.Input.TouchpadAxes, Vector2.zero) > .8f)
                {
                    var currentAngle = AngleFromVec(hand.Input.TouchpadAxes);

                    if (wasLastAngleValid)
                    {
                        num = prevAngle - currentAngle;
                    }

                    prevAngle = currentAngle;

                    wasLastAngleValid = true;
                }
            }

            AngleFromVec(Vector2.zero * num);
        }

        [HarmonyPatch(typeof(PIPScopeInteraction), nameof(PIPScopeInteraction.UpdateInteraction))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> MakeDialMovableWithJoystick(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

            if (codeMatcher.TryMatchForward(true, __originalMethod,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PIPScopeInteraction), nameof(PIPScopeInteraction.m_curRot))),
                new CodeMatch(OpCodes.Stloc_0)
            ))
            {
                var isOptionTurnedOffLabel = generator.DefineLabel();

                var wasLastAngleValidLabel = generator.DefineLabel();

                var skipOver = generator.DefineLabel();

                var currentAngleLocal = generator.DeclareLocal(typeof(float));

                codeMatcher
                .Advance(1)
                .AddLabels([isOptionTurnedOffLabel])
                .InsertAndAdvance(
                    //  if (SettingsManager.configEnableTurnScopeDialWithJoystick.Value)
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SettingsManager), nameof(SettingsManager.configEnableTurnScopeDialWithJoystick))),
					new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ConfigEntry<bool>), nameof(ConfigEntry<bool>.Value))),
                    new CodeInstruction(OpCodes.Brfalse_S, isOptionTurnedOffLabel),

                    //      if (Vector2.Distance(hand.Input.TouchpadAxes, Vector2.zero) > 0.8f)
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.Input))),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HandInput), nameof(HandInput.TouchpadAxes))),
                    new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector2), nameof(Vector2.zero))),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector2), nameof(Vector2.Distance))),
                    new CodeInstruction(OpCodes.Ldc_R4, 0.8f),
                    new CodeInstruction(OpCodes.Ble_Un, skipOver),

                    //          var currentAngle = AngleFromVec(hand.Input.TouchpadAxes);
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(FVRViveHand), nameof(FVRViveHand.Input))),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HandInput), nameof(HandInput.TouchpadAxes))),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.AngleFromVec))),
                    new CodeInstruction(OpCodes.Stloc_S, currentAngleLocal.LocalIndex),

                    //          if (wasLastAngleValid)
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TurnScopeDialWithJoystick), nameof(TurnScopeDialWithJoystick.wasLastAngleValid))),
                    new CodeInstruction(OpCodes.Brfalse_S, wasLastAngleValidLabel),

                    //              num = CalculateDifferenceBetweenAngles((prevAngle - currentAngle) * SettingsManager.configTurnScopeDialWithJoystickSensitivity.Value);
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

                    new CodeInstruction(OpCodes.Br, skipOver)
                )
                ;

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

            codeMatcher.Print();

            return codeMatcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(PIPScopeInteraction), nameof(PIPScopeInteraction.UpdateInteraction))]
        [HarmonyPostfix]
        private static void DebugStuff(PIPScopeInteraction __instance, [LocalVariableAccessor(3)] float __localVariable_num, float ___m_curRot)
        {
            CiarencesUnbelievableModifications.Logger.LogInfo($"previous angle: {prevAngle}");
            CiarencesUnbelievableModifications.Logger.LogInfo($"m_curRot: {___m_curRot}");
            CiarencesUnbelievableModifications.Logger.LogInfo($"num: {__localVariable_num}");
        }
    }
}