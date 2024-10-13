using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib.Public.Patching;
using static System.Reflection.Emit.OpCodes;
using static CiarencesUnbelievableModifications.CiarencesUnbelievableModifications;
using CiarencesUnbelievableModifications.MonoBehaviours;
using UnityEngine;

namespace CiarencesUnbelievableModifications.Libraries
{
    /// <summary>
    ///		 Marks a Postfix as being authorized to use the __localVariable_X patch parameter. <br>This is more limiting than a transpiler, and is purely to access a variable after a method has run, you can only change the value of the variable's fields, and call its methods, anything else is useless.</br>
    /// </summary>
    /// <remarks>
    ///		<seealso href="https://github.com/pardeike/Harmony/issues/624#issuecomment-2380412813">
    ///			See explanation as to why I made it a mandatory attribute.
    ///		</seealso>
    /// </remarks>
    public class LocalVariableAccessorAttribute : Attribute
    {
    }

    internal static class BepInExFunnyThing
    {
        private static readonly string LOCAL_VARIABLE_PREFIX = "__localVariable_";

        private static void AddLocalVariablePatchArgument(MethodBase original, MethodInfo patch, bool allowFirsParamPassthrough)
        {
            var list = original.GetParameters().ToList();

            object[] __args = AccessTools.Method(typeof(BepInExFunnyThing), nameof(AddLocalVariablePatchArgument)).GetParameters();

            if (allowFirsParamPassthrough && patch.ReturnType != typeof(void) && list.Count > 0 && list[0].ParameterType == patch.ReturnType)
            {
                list.RemoveRange(0, 1);
            }

            foreach (ParameterInfo parameterInfo in list)
            {
                if (parameterInfo.Name.StartsWith(LOCAL_VARIABLE_PREFIX, StringComparison.Ordinal))
                {
                    if (Attribute.GetCustomAttribute(patch, typeof(HarmonyPostfix)) == null && patch.Name != "Postfix")
                    {
                        throw new Exception($"The {parameterInfo.Name} patch parameter is only useful on postfixes");
                    }

                    string[] allowedHarmonyInstances = [HarmonyInstance.Id];

                    var patchInfo = Harmony.GetPatchInfo(original);
                    foreach (var thing in patchInfo.Postfixes)
                    {
                        if (!allowedHarmonyInstances.Contains(thing.owner))
                        {
                            throw new Exception($"{patch.Name} wasn't patched by a valid Harmony instance, transpile it to the list :)");
                        }
                    }

                    var localIndexStr = parameterInfo.Name.Substring(LOCAL_VARIABLE_PREFIX.Length);

                    if (int.TryParse(localIndexStr, out var localIndex))
                    {
                        var methodLocalVars = original.GetMethodBody().LocalVariables;

                        if (methodLocalVars.Count == 0)
                        {
                            throw new Exception($"{original.Name} does not have any local variables");
                        }

                        if (localIndex > methodLocalVars.Count - 1)
                        {
                            throw new Exception($"{original.Name} does not have a local variable at index {localIndex}");
                        }

                        if (parameterInfo.ParameterType != methodLocalVars[localIndex].LocalType)
                        {
                            throw new Exception($"{parameterInfo.Name} has wrong type, is: {parameterInfo.ParameterType}, but according to local variable type, should be: {methodLocalVars[localIndex].LocalType}");
                        }

                        AccessTools.Method(__args[0].GetType(), "Emit", [typeof(OpCode), typeof(int)]).Invoke(__args[0], [Ldloc, localIndex]);
                    }
                    else
                    {
                        throw new Exception($"Parameter {parameterInfo.Name} does not contain a valid local variable index number");
                    }
                }
            }
        }

        /*internal static Mono.Cecil.Cil.OpCode lastOpCode;

		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(Mono.Cecil.TypeReference)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(Mono.Cecil.CallSite)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(Mono.Cecil.MethodReference)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(Mono.Cecil.FieldReference)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(string)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(sbyte)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(byte)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(int)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(long)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(float)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(double)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(Mono.Cecil.Cil.Instruction)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(Mono.Cecil.Cil.Instruction[])])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(Mono.Cecil.Cil.VariableDefinition)])]
		[HarmonyPatch(typeof(Mono.Cecil.Cil.Instruction), nameof(Mono.Cecil.Cil.Instruction.Create), [typeof(Mono.Cecil.Cil.OpCode), typeof(Mono.Cecil.ParameterDefinition)])]
		[HarmonyPrefix]
		private static void PleaseTellMeMore(Mono.Cecil.Cil.OpCode opcode)
		{
			lastOpCode = opcode;
			if (opcode.ToString() != "nop")
			{
				Logger.LogInfo(opcode.ToString());
			}
		}*/

        [HarmonyPatch(typeof(HarmonyManipulator), "EmitCallParameter")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TranspileLocalVariablePatchArgument(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

            if (codeMatcher.TryMatchForward(true, __originalMethod,
                [
                new CodeMatch(Callvirt, AccessTools.PropertyGetter(typeof(Type), nameof(Type.IsByRef))),
                new CodeMatch(Brtrue),
                new CodeMatch(Ldsfld),
                new CodeMatch(Br),
                new CodeMatch(Ldsfld),
                new CodeMatch(Stloc_S),
                new CodeMatch(Ldarg_0),
                new CodeMatch(Ldloc_S),
                new CodeMatch(Ldloc_S),
                new CodeMatch(Callvirt, AccessTools.Method(AccessTools.TypeByName("HarmonyLib.Internal.Util.ILEmitter"), "Emit", [typeof(Mono.Cecil.Cil.OpCode), typeof(Mono.Cecil.Cil.VariableDefinition)])),
                new CodeMatch(Br)
                ]
                ))
            {
                SettingsManager.LogVerboseLevelNameAndColor($"Patching {MethodBase.GetCurrentMethod().Name}", "BIEFT-Transpilers", ConsoleColor.Cyan);

                //Declare new locals
                var forSomeReasonItDoesThatItsTheCharThatIIndexBTW = generator.DeclareLocal(typeof(char));

                var localVariableNum = generator.DeclareLocal(typeof(int));

                var originalMethodLocalVariables = generator.DeclareLocal(typeof(IList<LocalVariableInfo>));

                //Get the label from the branches :)

                var thisIsTheBrThatGoesToTheLoopThing = (Label)codeMatcher.Instruction.operand;

                var gotYourNose = codeMatcher
                .Advance(1).Instruction.ExtractLabels();

                //New labels

                var thisIsTheBrThatGoesToTheOtherThing = generator.DefineLabel();

                codeMatcher.Instruction.WithLabels(thisIsTheBrThatGoesToTheOtherThing);

                var rightTypeLabel = generator.DefineLabel();

                var hasLocalVariable = generator.DefineLabel();

                var noVariableIndex = generator.DefineLabel();

                var hasLocalVariableAtIndexX = generator.DefineLabel();

                var isPostfixLabel = generator.DefineLabel();

                var hasLocalAccessorAttribute = generator.DefineLabel();

                codeMatcher.InsertAndAdvance(
                    [
							//         if (parameterInfo.Name.StartsWith("__localVariable_"))
							new CodeInstruction(Ldloc_S, 5).WithLabels(gotYourNose),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(ParameterInfo), nameof(ParameterInfo.Name))),
                            new CodeInstruction(Ldsfld, AccessTools.Field(typeof(BepInExFunnyThing), nameof(LOCAL_VARIABLE_PREFIX))),
                            new CodeInstruction(Ldc_I4_4),
                            new CodeInstruction(Callvirt, AccessTools.Method(typeof(string), nameof(string.StartsWith), [typeof(string), typeof(StringComparison)])),
                            new CodeInstruction(Brfalse, thisIsTheBrThatGoesToTheOtherThing),

                            new CodeInstruction(Ldarg_2),
                            new CodeInstruction(Ldtoken, typeof(HarmonyPostfix)),
                            new CodeInstruction(Call, AccessTools.Method(typeof(Type), nameof(Type.GetTypeFromHandle), [typeof(RuntimeTypeHandle)])),
                            new CodeInstruction(Call, AccessTools.Method(typeof(Attribute), nameof(Attribute.GetCustomAttribute), [typeof(MemberInfo), typeof(Type)])),
                            new CodeInstruction(Brtrue_S, isPostfixLabel),

                            new CodeInstruction(Ldarg_2),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(MemberInfo), nameof(MemberInfo.Name))),
                            new CodeInstruction(Ldstr, "Postfix"),
                            new CodeInstruction(Call, AccessTools.Method(typeof(string), "op_Inequality")),
                            new CodeInstruction(Brfalse_S, isPostfixLabel),

                            new CodeInstruction(Ldstr, "The "),
                            new CodeInstruction(Ldloc_S, 5),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(ParameterInfo), nameof(ParameterInfo.Name))),
                            new CodeInstruction(Ldstr, " patch parameter is only useful on postfixes"),
                            new CodeInstruction(Call, AccessTools.Method(typeof(string), nameof(string.Concat), [typeof(string), typeof(string), typeof(string)])),
                            new CodeInstruction(Newobj, AccessTools.Constructor(typeof(Exception), [typeof(string)])),
                            new CodeInstruction(Throw),

                            new CodeInstruction(Ldarg_2).WithLabels(isPostfixLabel),
                            new CodeInstruction(Ldtoken, typeof(LocalVariableAccessorAttribute)),
                            new CodeInstruction(Call, AccessTools.Method(typeof(Type), nameof(Type.GetTypeFromHandle), [typeof(RuntimeTypeHandle)])),
                            new CodeInstruction(Call, AccessTools.Method(typeof(Attribute), nameof(Attribute.GetCustomAttribute), [typeof(MemberInfo), typeof(Type)])),
                            new CodeInstruction(Brtrue_S, hasLocalAccessorAttribute),

                            new CodeInstruction(Ldstr, "You need a LocalVariableAccessor attribute on your postfix if you want to use the __localVariable_X patch parameter"),
                            new CodeInstruction(Newobj, AccessTools.Constructor(typeof(Exception), [typeof(string)])),
                            new CodeInstruction(Throw),

							//             string text = parameterInfo.Name.Substring("__localVariable_".Length);
							new CodeInstruction(Ldloc_S, 5).WithLabels(hasLocalAccessorAttribute),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(ParameterInfo), nameof(ParameterInfo.Name))),
                            new CodeInstruction(Ldsfld, AccessTools.Field(typeof(BepInExFunnyThing), nameof(LOCAL_VARIABLE_PREFIX))),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(string), nameof(string.Length))),
                            new CodeInstruction(Callvirt, AccessTools.Method(typeof(string), nameof(string.Substring), [typeof(int)])),
                            new CodeInstruction(Ldloca_S, localVariableNum.LocalIndex),
                            new CodeInstruction(Call, AccessTools.Method(typeof(int), nameof(Int32.TryParse), [typeof(string), typeof(int).MakeByRefType()])),
                            new CodeInstruction(Brfalse, noVariableIndex),

							//             IList<LocalVariableInfo> localVariables = original.GetMethodBody().LocalVariables;
							new CodeInstruction(Ldarg_1),
                            new CodeInstruction(Callvirt, AccessTools.Method(typeof(MethodBase), nameof(MethodBase.GetMethodBody))),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(MethodBody), nameof(MethodBody.LocalVariables))),
                            new CodeInstruction(Stloc, originalMethodLocalVariables.LocalIndex),

							//             if (localVariables.Count == 0)
							new CodeInstruction(Ldloc_S, originalMethodLocalVariables.LocalIndex),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(ICollection<LocalVariableInfo>), nameof(ICollection<LocalVariableInfo>.Count))),
                            new CodeInstruction(Brtrue_S, hasLocalVariable),

							//                 throw new Exception(original.Name + " does not have any local variables");
							new CodeInstruction(Ldarg_1).WithLabels(noVariableIndex),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(MemberInfo), nameof(MemberInfo.Name))),
                            new CodeInstruction(Ldstr, " does not have any local variables"),
                            new CodeInstruction(Call, AccessTools.Method(typeof(string), nameof(string.Concat), [typeof(string), typeof(string)])),
                            new CodeInstruction(Newobj, AccessTools.Constructor(typeof(Exception), [typeof(string)])),
                            new CodeInstruction(Throw),

							//             if (num > localVariables.Count + 1)
							new CodeInstruction(Ldloc_S, localVariableNum.LocalIndex).WithLabels(hasLocalVariable),
                            new CodeInstruction(Ldloc_S, originalMethodLocalVariables.LocalIndex),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(ICollection<LocalVariableInfo>), nameof(ICollection<LocalVariableInfo>.Count))),
                            new CodeInstruction(Ldc_I4_1),
                            new CodeInstruction(Add),
                            new CodeInstruction(Ble_S, hasLocalVariableAtIndexX),

							//                 throw new Exception(string.Format("{0} does not have a local variable at index {1}", original.Name, num));
							new CodeInstruction(Ldstr, "{0} does not have a local variable at index {1}"),
                            new CodeInstruction(Ldarg_1),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(MemberInfo), nameof(MemberInfo.Name))),
                            new CodeInstruction(Ldloc_S, localVariableNum.LocalIndex),
                            new CodeInstruction(Box, typeof(int)),
                            new CodeInstruction(Call, AccessTools.Method(typeof(string), nameof(string.Format), [typeof(string), typeof(object), typeof(object)])),
                            new CodeInstruction(Newobj, AccessTools.Constructor(typeof(Exception))),
                            new CodeInstruction(Throw),

							//             if (parameterInfo.ParameterType != localVariables[num].LocalType)
							new CodeInstruction(Ldloc_S, 5).WithLabels(hasLocalVariableAtIndexX),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(ParameterInfo), nameof(ParameterInfo.ParameterType))),
                            new CodeInstruction(Ldloc_S, originalMethodLocalVariables.LocalIndex),
                            new CodeInstruction(Ldloc_S, localVariableNum.LocalIndex),
                            new CodeInstruction(Callvirt, AccessTools.Method(typeof(IList<LocalVariableInfo>), "get_Item")),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(LocalVariableInfo), nameof(LocalVariableInfo.LocalType))),
                            new CodeInstruction(Beq_S, rightTypeLabel),

							//                 throw new Exception(string.Format("{0} does not have a local variable at index {1}", original.Name, num));
							new CodeInstruction(Ldstr, "{0} has wrong type, is: {1}, but according to found method's local variable type, should be: {2}"),
                            new CodeInstruction(Ldloc_S, 5),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(ParameterInfo), nameof(ParameterInfo.Name))),
                            new CodeInstruction(Ldloc_S, 5),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(ParameterInfo), nameof(ParameterInfo.ParameterType))),
                            new CodeInstruction(Ldloc_S, originalMethodLocalVariables.LocalIndex),
                            new CodeInstruction(Ldloc_S, localVariableNum.LocalIndex),
                            new CodeInstruction(Callvirt, AccessTools.Method(typeof(IList<LocalVariableInfo>), "get_Item")),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(LocalVariableInfo), nameof(LocalVariableInfo.LocalType))),
                            new CodeInstruction(Call, AccessTools.Method(typeof(string), nameof(string.Format), [typeof(string), typeof(object), typeof(object), typeof(object)])),
                            new CodeInstruction(Newobj, AccessTools.Constructor(typeof(Exception), [typeof(string)])),
                            new CodeInstruction(Throw),

							//                 il.Emit(Ldloc, localVariableNum);
							new CodeInstruction(Ldarg_0).WithLabels(rightTypeLabel),
                            new CodeInstruction(Ldsfld, AccessTools.Field(typeof(Mono.Cecil.Cil.OpCodes), nameof(Mono.Cecil.Cil.OpCodes.Ldloc))),
                            new CodeInstruction(Ldloc_S, localVariableNum.LocalIndex),
                            new CodeInstruction(Callvirt, AccessTools.Method(AccessTools.TypeByName("HarmonyLib.Internal.Util.ILEmitter"), "Emit", [typeof(Mono.Cecil.Cil.OpCode), typeof(int)])),
                            new CodeInstruction(Br_S, thisIsTheBrThatGoesToTheLoopThing),

							//                 throw new Exception(string.Format("Parameter {0} does not contain a valid local variable index, instead we had this: {1}", parameterInfo.Name, text[0]));
							new CodeInstruction(Ldstr, "Parameter "),
                            new CodeInstruction(Ldloc_S, 5),
                            new CodeInstruction(Callvirt, AccessTools.PropertyGetter(typeof(ParameterInfo), nameof(ParameterInfo.Name))),
                            new CodeInstruction(Ldstr, " does not contain a valid local variable index number"),
                            new CodeInstruction(Call, AccessTools.Method(typeof(string), nameof(string.Concat), [typeof(string), typeof(string), typeof(string)])),
                            new CodeInstruction(Newobj, AccessTools.Constructor(typeof(Exception), [typeof(string)])),
                            new CodeInstruction(Throw)
                    ]
                );
            }

            return codeMatcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(SteamVR_LoadLevel), nameof(SteamVR_LoadLevel.Begin))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Normal)]
        [LocalVariableAccessor]
        private static void TestIfMyShitWorks(SteamVR_LoadLevel __localVariable_0)
        {
            CiarencesUnbelievableModifications.Logger.LogMessageWithColor(__localVariable_0.backgroundColor, ConsoleColor.Cyan);
            __localVariable_0.backgroundColor = Color.red;
        }

        [HarmonyPatch(typeof(SteamVR_LoadLevel), nameof(SteamVR_LoadLevel.Begin))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [LocalVariableAccessor]
        private static void IAmStillTestingIfMyShitWorks(SteamVR_LoadLevel __localVariable_0)
        {
            CiarencesUnbelievableModifications.Logger.QuickLogInfo(nameof(SteamVR_LoadLevel), __localVariable_0.backgroundColor);
        }
    }
}
