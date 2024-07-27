using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using CiarencesUnbelievableModifications.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace CiarencesUnbelievableModifications
{
    [BepInAutoPlugin]
	[BepInDependency("nrgill28.Sodalite", BepInDependency.DependencyFlags.SoftDependency)] //makes the plugin load after Sodalite if it's installed, since I'm patching it and shtuff
    [BepInProcess("h3vr.exe")]
    public partial class CiarencesUnbelievableModifications : BaseUnityPlugin
    {
        public static PluginInfo safehouseProgressionPlugin;

        public static PluginInfo betterHandsPlugin;
        public static bool isBetterHandsPalmingEnabled;

		internal static Harmony HarmonyInstance
		{
			get;
		} = new Harmony("CiarencesHarmonyInstanceIWILLKillYouInH3VRIfYouTouchIt"); //is this even useful to have idk

		private void Awake()
		{
			foreach (Type type in typeof(BepInPlugin).Assembly.GetTypes())
			{
				if (type.FullName == "BepInEx.ConsoleUtil.Kon")
				{
					ConsoleColourer.konType = type;
					break;
				}
			}

            Logger = base.Logger;

            SettingsManager.InitializeAndBindSettings(Config);

			CheckForIncompatibilites();

            PatchAll();

			if (UnityEngine.Random.Range(0, 10000) > 9999) //mr beast 666 the devil 
			{
				SaySomething();
			}

			#pragma warning disable
			ConsoleColourer.BackgroundColor = ConsoleColor.Red;
            Logger.LogMessageWithColor($"{Id} version {Version} prepped and ready", ConsoleColor.Green);
			ConsoleColourer.BackgroundColor = ConsoleColor.Black;
			#pragma warning restore
		}

		private void SaySomething()
		{
			#pragma warning disable
			ConsoleColourer.BackgroundColor = ConsoleColor.DarkMagenta;
			Logger.LogWithCustomLevelNameAndColor("FUCK YOU", "Evil Ciarence", ConsoleColor.Yellow);
			ConsoleColourer.BackgroundColor = ConsoleColor.Black;
			#pragma warning restore
		}

		private void PatchAll()
        {
            HarmonyInstance.PatchAll(typeof(MagRetentionTweaks.MagRetentionTweaksTranspilers));
			HarmonyInstance.PatchAll(typeof(MagRetentionTweaks.MagRetentionTweaksHarmonyFixes));

            HarmonyInstance.PatchAll(typeof(CylinderBulletCollector));
            HarmonyInstance.PatchAll(typeof(CylinderBulletCollector.CylinderBulletCollectorTranspiler));

            HarmonyInstance.PatchAll(typeof(BoltHandleLockSoundTweaks));
            HarmonyInstance.PatchAll(typeof(BoltHandleLockSoundTweaks.Transpilers));

            HarmonyInstance.PatchAll(typeof(ReverseMagHoldPos));

            HarmonyInstance.PatchAll(typeof(MagRetentionTweaks.MagPalmKeepOffsetPatch.MagPalmKeepOffsetHarmonyPatches));
            HarmonyInstance.PatchAll(typeof(MagRetentionTweaks.MagPalmKeepOffsetPatch.MagPalmKeepOffsetTranspilers));

            HarmonyInstance.PatchAll(typeof(FireArmTweaks.SpawnStockTweaks));
            HarmonyInstance.PatchAll(typeof(FireArmTweaks.BitchDontTouchMyGun));

            HarmonyInstance.PatchAll(typeof(InstitutionPreviewReenabler));

            HarmonyInstance.PatchAll(typeof(CompetitiveShellGrabbing.Patches));
            HarmonyInstance.PatchAll(typeof(CompetitiveShellGrabbing.Transpilers));

            HarmonyInstance.PatchAll(typeof(FireArmTweaks.KnockAKDrumOut));

			HarmonyInstance.PatchAll(typeof(OptionGunCategoryBlacklister.Transpilers));

			if (SettingsManager.configEnableSosigPuncher.Value) HarmonyInstance.PatchAll(typeof(SosigPunchTest));
        }

		static FieldInfo logColours;

        internal void CheckForIncompatibilites()
        {
			Logger.LogMessage("Suicide protocol engaged.");
            if (!Chainloader.PluginInfos.TryGetValue("NGA.SafehouseProgression", out safehouseProgressionPlugin))
            {
                Chainloader.PluginInfos.TryGetValue("NGA.SafehouseMP", out safehouseProgressionPlugin); //there's a different version for MP, ffs
            }

            if (Chainloader.PluginInfos.TryGetValue("maiq.BetterHand", out betterHandsPlugin))
            {
                if (betterHandsPlugin.Instance.Config.TryGetEntry<bool>("MagPalm Options", "aEnable", out var entry))
                {
                    isBetterHandsPalmingEnabled = entry.Value;

                    entry.SettingChanged += (sender, args) => { isBetterHandsPalmingEnabled = (sender as ConfigEntry<bool>).Value; }; //hope this works
                }
            }

			if (Chainloader.PluginInfos.TryGetValue("nrgill28.Sodalite", out var sodalitePlugin) && SettingsManager.configEnableSodaliteConsoleColorPatch.Value)
			{
				var modPanel = AccessTools.Field(sodalitePlugin.Instance.GetType(),	"_modPanelPrefab").GetValue(sodalitePlugin.Instance) as GameObject;

				var comps = modPanel.GetComponents<MonoBehaviour>(); //cursed

				object logPage = null;
				for (int i = 0; i < comps.Length; i++)
				{
					if (AccessTools.Field(comps[i].GetType(), "LogPage") != null)
					{
						logPage = AccessTools.Field(comps[i].GetType(), "LogPage").GetValue(comps[i]);
					}
				}

				if (logPage != null)
				{
					logColours = AccessTools.Field(logPage.GetType(), "LogColors");

					var logColoursDict = logColours.GetValue(logPage) as Dictionary<LogLevel, string>;

					logColoursDict.Add((LogLevel)696969, "pink");

					HarmonyMethod transpenis = new HarmonyMethod(AccessTools.Method(typeof(CiarencesUnbelievableModifications), nameof(CiarencesUnbelievableModifications.TranspileSodaliteConsoleUpdateText)));

					PatchProcessor hi = HarmonyInstance.CreateProcessor(AccessTools.Method(logPage.GetType(), "UpdateText", new[] { typeof(bool) }));

					hi.AddTranspiler(transpenis);

					hi.Patch();
				}
			}
			Logger.LogMessage("Suicide postponed.");
        }

		internal static Dictionary<ConsoleColor, Color> m_C2Cdict = new Dictionary<ConsoleColor, Color>()
		{
			[ConsoleColor.Black] = GetColorFromHTMLString("#000000"),
			[ConsoleColor.DarkBlue] = GetColorFromHTMLString("#00008B"),
			[ConsoleColor.DarkGreen] = GetColorFromHTMLString("#006400"),
			[ConsoleColor.DarkCyan] = GetColorFromHTMLString("#008B8B"),
			[ConsoleColor.DarkRed] = GetColorFromHTMLString("#8B0000"),
			[ConsoleColor.DarkMagenta] = GetColorFromHTMLString("#8B008B"),
			[ConsoleColor.DarkYellow] = GetColorFromHTMLString("#8B8803"),
			[ConsoleColor.Gray] = GetColorFromHTMLString("#808080"),
			[ConsoleColor.DarkGray] = GetColorFromHTMLString("#A9A9A9"),
			[ConsoleColor.Blue] = GetColorFromHTMLString("#0000FF"),
			[ConsoleColor.Green] = GetColorFromHTMLString("#008000"),
			[ConsoleColor.Cyan] = GetColorFromHTMLString("#00FFFF"),
			[ConsoleColor.Red] = GetColorFromHTMLString("#FF0000"),
			[ConsoleColor.Magenta] = GetColorFromHTMLString("#FF00FF"),
			[ConsoleColor.Yellow] = GetColorFromHTMLString("#FFFF00"),
			[ConsoleColor.White] = GetColorFromHTMLString("#FFFFFF")
		};

		internal static Color GetColorFromHTMLString(string piss)
		{
			if (ColorUtility.TryParseHtmlString(piss, out var color))
			{
				return color;
			}
			return Color.white;
		}

		public static Color GetColorFromConsoleColor(ConsoleColor consoleColor)
		{
			if (m_C2Cdict.TryGetValue(consoleColor, out var color))
			{
				return color;
			}
			return Color.white;
		}

		//no attribute because will dynamically patch later on, because I don't want to add a fucking dependency to this!!
		internal static IEnumerable<CodeInstruction> TranspileSodaliteConsoleUpdateText(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
		{
			CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);

			Debug.Log("penis");
			if (codeMatcher.TryMatchForward(false,
				__originalMethod,
				new CodeMatch(OpCodes.Ldsfld, logColours),
				new CodeMatch(OpCodes.Ldloc_S),
				new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(LogEventArgs), nameof(LogEventArgs.Level))),
				new CodeMatch(OpCodes.Callvirt) //operand is too long and annoying, fuck you.
				))
			{
				codeMatcher
					.RemoveInstructions(4)
					.Insert(
					new CodeInstruction(OpCodes.Ldstr, "#"),
					new CodeInstruction(OpCodes.Ldloc_S, 5),
					new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(LogEventArgs), nameof(LogEventArgs.Level))),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LogLevelExtensions), nameof(LogLevelExtensions.GetConsoleColor))),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CiarencesUnbelievableModifications), nameof(GetColorFromConsoleColor))),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ColorUtility), nameof(ColorUtility.ToHtmlStringRGB))),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(String), nameof(String.Concat), new[] { typeof(string), typeof(string) }))
					);
			}

			codeMatcher.Print();

			return codeMatcher.InstructionEnumeration();
		}

        internal new static ManualLogSource Logger { get; private set; }
    }
}
