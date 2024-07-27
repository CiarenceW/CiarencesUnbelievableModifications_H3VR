using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using System.CodeDom;

namespace CiarencesUnbelievableModifications.Patches
{
	public static class OptionGunCategoryBlacklister
	{
		public static SectionConfigList<FVRObject.OTagFirearmAction> VirtualStockBlacklistedTypes
		{
			get { return SettingsManager.sectionConfigListVirtualStockBlacklist ; }
		}

		public static SectionConfigList<FVRObject.OTagFirearmSize> EasyMagLoadingBlacklistedTypes
		{
			get { return SettingsManager.sectionConfigListEasyMagLoadingBlacklist ; }
		}

		public static bool IsNotVirtualStockBlacklisted(FVRPhysicalObject fVRPhysicalObject)
		{
			return IsNotInBlacklistedList(fVRPhysicalObject, SeparateConfigWeaponSpecificString(SettingsManager.configVirtualStockWeaponBlacklist.Value), SeparateConfigWeaponSpecificString(SettingsManager.configVirtualStockWeaponWhitelist.Value), true);
		}

		public static bool IsNotEasyMagLoadingBlacklisted(FVRPhysicalObject fVRPhysicalObject)
		{
			FVRPhysicalObject vRPhysicalObjectHelp = null;
			if (fVRPhysicalObject.m_hand.OtherHand.CurrentInteractable != null && fVRPhysicalObject.m_hand.OtherHand.CurrentInteractable is FVRFireArm fVRFireArm) vRPhysicalObjectHelp = fVRFireArm;
			return IsNotInBlacklistedList(vRPhysicalObjectHelp, SeparateConfigWeaponSpecificString(SettingsManager.configEasyMagLoadingWeaponBlacklist.Value), SeparateConfigWeaponSpecificString(SettingsManager.configEasyMagLoadingWeaponWhitelist.Value), false);
		}

		private static bool IsNotInBlacklistedList(FVRPhysicalObject fVRPhysicalObject, string[] weaponSpecificBlacklist, string[] weaponSpecificWhitelist, bool checkForVirtualStock)
		{
			if (fVRPhysicalObject is FVRFireArm fireArm)
			{
				if (fireArm.ObjectWrapper != null)
				{
					SettingsManager.LogVerboseInfo(fireArm.ObjectWrapper.DisplayName);
					if (weaponSpecificWhitelist.Contains(fireArm.ObjectWrapper.DisplayName.Trim(' ')))
					{
						SettingsManager.LogVerboseInfo("Is in whitelist");
						return true;
					}
					if (weaponSpecificBlacklist.Contains(fireArm.ObjectWrapper.DisplayName.Trim(' ')))
					{
						SettingsManager.LogVerboseInfo("Is in blacklist");
						return false;
					}

					if (checkForVirtualStock) return !VirtualStockBlacklistedTypes.configEntries[fireArm.ObjectWrapper.TagFirearmAction].Value;
					else return EasyMagLoadingBlacklistedTypes.configEntries[fireArm.ObjectWrapper.TagFirearmSize].Value;
				}

				return true;
			}
			else
			{
				return true;
			}
		}

		private static string[] SeparateConfigWeaponSpecificString(string weaponSpecificString)
		{
			var splitString = weaponSpecificString.Split('|');
			var trimmedSplitString = new string[splitString.Length];
			for (int stringIndex = 0; stringIndex < splitString.Length; stringIndex++)
			{
				trimmedSplitString[stringIndex] = splitString[stringIndex].Trim();
			}

			return trimmedSplitString;
		}

		internal static class Transpilers
		{
			[HarmonyPatch(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.FU))]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> TranspileVirtualStockBlacklist(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
			{
				CodeMatcher codeMatcher = new CodeMatcher(instructions, generator).MatchForward(true,
					new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(GM), nameof(GM.Options))),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GameOptions), nameof(GameOptions.ControlOptions))),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ControlOptions), nameof(ControlOptions.UseVirtualStock))),
					new CodeMatch(OpCodes.Brfalse));

				if (!codeMatcher.ReportFailure(__originalMethod, CiarencesUnbelievableModifications.Logger.LogError))
				{
					SettingsManager.LogVerboseLevelNameAndColor($"Patching {MethodBase.GetCurrentMethod().Name}", "CBC-Transpilers", System.ConsoleColor.Cyan);

					var brfalseInstruction = codeMatcher.Instruction;

					codeMatcher
						.Advance(1)
						.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OptionGunCategoryBlacklister), nameof(OptionGunCategoryBlacklister.IsNotVirtualStockBlacklisted))),
						brfalseInstruction)
						;
				}

				codeMatcher.MatchForward(true,
					new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(GM), nameof(GM.Options))),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GameOptions), nameof(GameOptions.ControlOptions))),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ControlOptions), nameof(ControlOptions.UseVirtualStock))),
					new CodeMatch(OpCodes.Brfalse));

				if (!codeMatcher.ReportFailure(__originalMethod, CiarencesUnbelievableModifications.Logger.LogError))
				{
					var brfalseInstruction = codeMatcher.Instruction;

					codeMatcher
						.Advance(1)
						.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OptionGunCategoryBlacklister), nameof(OptionGunCategoryBlacklister.IsNotVirtualStockBlacklisted))),
						brfalseInstruction)
						;
				}

				return codeMatcher.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.FVRFixedUpdate))]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> TranspileEasyMagLoadingBlacklist(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
			{
				CodeMatcher codeMatcher = new CodeMatcher(instructions, generator).MatchForward(true,
					new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(GM), nameof(GM.Options))),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GameOptions), nameof(GameOptions.ControlOptions))),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ControlOptions), nameof(ControlOptions.UseEasyMagLoading))),
					new CodeMatch(OpCodes.Brfalse));

				if (!codeMatcher.ReportFailure(__originalMethod, CiarencesUnbelievableModifications.Logger.LogError))
				{
					SettingsManager.LogVerboseLevelNameAndColor($"Patching {MethodBase.GetCurrentMethod().Name}", "CBC-Transpilers", System.ConsoleColor.Cyan);

					var brfalseInstruction = codeMatcher.Instruction;

					codeMatcher
						.Advance(1)
						.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OptionGunCategoryBlacklister), nameof(OptionGunCategoryBlacklister.IsNotEasyMagLoadingBlacklisted))),
						brfalseInstruction)
						;
				}

				return codeMatcher.InstructionEnumeration();
			}
		}
	}

	public struct SectionConfigList<T> where T : Enum
	{
		public string section;
		public Dictionary<T, ConfigEntry<bool>> configEntries;
    }
}
