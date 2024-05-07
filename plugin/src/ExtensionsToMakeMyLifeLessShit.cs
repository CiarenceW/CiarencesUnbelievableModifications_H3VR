using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace CiarencesUnbelievableModifications
{
    internal static class ExtensionsToMakeMyLifeLessShit
    {
        public static bool TryGetComponent<T>(this Component component, out T result) where T : Component
        {
            return (result = component.GetComponent<T>()) != null; //wow!!!! all that in one line!!! cool right?
        }

        public static bool TryGetComponent<T>(this GameObject go, out T result) where T : Component
        {
            return (result = go.GetComponent<T>()) != null; //wow!!!! all that in one line!!! cool right?
        }

        public static bool TryGetComponentInParent<T>(this Component component, out T result) where T : Component
        {
            return (result = component.GetComponentInParent<T>()) != null;
        }

        public static bool TryGetComponentInChildren<T>(this Component component, out T result) where T: Component
        {
            return (result = component.GetComponentInChildren<T>()) != null;
        }

        public static bool TryFind(this Transform transform, string path, out Transform outTransform)
        {
            return (outTransform = transform.Find(path)) != null;
        }

        public static bool HasComponent<T>(this Transform transform) where T : Component
        {
            return transform.GetComponent<T>() != null;
        }

        public static bool HasComponent<T>(this Component component) where T : Component
        {
            return component.GetComponent<T>() != null;
        }

        public static T AddComponent<T>(this Transform transform) where T : Component
        {
            return transform.gameObject.AddComponent<T>();
        }

        public static T AddComponent<T>(this Component component) where T : Component
        {
            return component.gameObject.AddComponent<T>();
        }

        public static T GetOrAddComponent<T>(this Transform transform) where T : Component
        {
            return (transform.TryGetComponent<T>(out var result)) ? result : transform.AddComponent<T>();
        }

        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            return (component.TryGetComponent<T>(out var result)) ? result : component.AddComponent<T>();
        }

		public static List<CodeInstruction> ToCodeInstructions(this System.Reflection.MethodInfo methodInfo, Dictionary<CodeInstruction, CodeInstruction> replaceInstructionWith = null)
		{
			var methodIL = PatchProcessor.GetOriginalInstructions(methodInfo);

			if (replaceInstructionWith != null)
			{
				for (int instructionIndex = 0; instructionIndex < methodIL.Count; instructionIndex++)
				{
					if (replaceInstructionWith.TryGetValue(methodIL[instructionIndex], out var replacementInstrution))
					{
						methodIL[instructionIndex] = replacementInstrution;
					}
				}
			}

			return methodIL;
		}

		public static List<CodeInstruction> ToCodeInstructionsClipLast(this System.Reflection.MethodInfo methodInfo, out List<Label> extractedLabels, Dictionary<CodeInstruction, CodeInstruction> replaceInstructionWith = null)
		{
			var methodIL = PatchProcessor.GetOriginalInstructions(methodInfo);

			if (replaceInstructionWith != null)
			{
				for (int instructionIndex = 0; instructionIndex < methodIL.Count; instructionIndex++)
				{
					if (replaceInstructionWith.TryGetValue(methodIL[instructionIndex], out var replacementInstrution))
					{
						methodIL[instructionIndex] = replacementInstrution;
					}
				}
			}

			extractedLabels = (methodIL.Last().ExtractLabels());
			methodIL.Remove(methodIL.Last());

			return methodIL;
		}

		//from https://stackoverflow.com/a/801058
		public static List<T> GetEnumList<T>()
		{
			List<T> list = new List<T>();
			list.AddRange(((T[])Enum.GetValues(typeof(T))));
			return list;
		}
    }
}
