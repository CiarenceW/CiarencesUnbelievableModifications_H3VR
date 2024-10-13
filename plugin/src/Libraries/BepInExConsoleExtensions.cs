using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CiarencesUnbelievableModifications.Libraries
{
    public static class LoggerExtensions
    {
        public static Harmony getConsoleColorHarmonyInstance;

        public static Harmony logEventArgsToStringHarmonyInstance;

        public static void DoLogWithBackgroundColour(this ManualLogSource logger, Action action, ConsoleColor backgroundConsoleColor)
        {
#pragma warning disable
            ConsoleColourer.BackgroundColor = backgroundConsoleColor;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ConsoleColourer.BackgroundColor = ConsoleColor.Black;
                logger.LogError(ex);
            }

            ConsoleColourer.BackgroundColor = ConsoleColor.Black;
#pragma warning enable
        }

        public static void LogWithColor(this ManualLogSource logger, LogLevel logLevel, object data, ConsoleColor color)
        {
            getConsoleColorHarmonyInstance ??= new Harmony("GetConsoleColor");

            getConsoleColorHarmonyInstance = Harmony.CreateAndPatchAll(typeof(ConsoleColourer));
            ConsoleColourer.consoleColor = color;
            logger.Log(logLevel, data);
            getConsoleColorHarmonyInstance.UnpatchSelf();
        }

		/// <summary>
		/// Quickly logs an Info with the following format: <code>source: data</code>
		/// </summary>
		/// <param name="logger">
		/// You already know this.
		/// </param>
		/// <param name="source">
		/// The name of the source, protip: use nameof().
		/// </param>
		/// <param name="data">
		/// The data to print to console.
		/// </param>
        public static void QuickLogInfo(this ManualLogSource logger, object source, object data)
        {
            logger.LogInfo($"{source}: {data}");
        }

		/// <summary>
		/// Quickly logs an Info with the following format: <code>source: data</code>
		/// </summary>
		/// <param name="logger">
		/// You already know this.
		/// </param>
		/// <param name="source">
		/// The name of the source, protip: use nameof().
		/// </param>
		/// <param name="data">
		/// The data to print to console.
		/// </param>
		/// <param name="color">
		/// The colour of the line in the console.
		/// </param>
		public static void QuickLogInfoWithColor(this ManualLogSource logger, object source, object data, ConsoleColor color)
		{
			getConsoleColorHarmonyInstance ??= new Harmony("GetConsoleColor");

			getConsoleColorHarmonyInstance = Harmony.CreateAndPatchAll(typeof(ConsoleColourer));
			ConsoleColourer.consoleColor = color;
			logger.LogInfo($"{source}: {data}");
			getConsoleColorHarmonyInstance.UnpatchSelf();
		}

        public static void LogWithCustomLevelNameAndColor(this ManualLogSource logger, object data, string levelName, ConsoleColor color)
        {
            getConsoleColorHarmonyInstance = Harmony.CreateAndPatchAll(typeof(ConsoleColourer));
            ConsoleColourer.consoleColor = color;
            logEventArgsToStringHarmonyInstance = Harmony.CreateAndPatchAll(typeof(LogLevelStringChanger));
            LogLevelStringChanger.levelName = levelName;
            logger.Log((LogLevel)696969, data);
            logEventArgsToStringHarmonyInstance.UnpatchSelf();
            getConsoleColorHarmonyInstance.UnpatchSelf();
        }

        public static void LogDebugWithColor(this ManualLogSource logger, object data, ConsoleColor color)
        {
            getConsoleColorHarmonyInstance ??= new Harmony("GetConsoleColor");

            getConsoleColorHarmonyInstance = Harmony.CreateAndPatchAll(typeof(ConsoleColourer));
            ConsoleColourer.consoleColor = color;
            logger.LogDebug(data);
            getConsoleColorHarmonyInstance.UnpatchSelf();
        }

        public static void LogErrorWithColor(this ManualLogSource logger, object data, ConsoleColor color)
        {
            getConsoleColorHarmonyInstance ??= new Harmony("GetConsoleColor");

            getConsoleColorHarmonyInstance = Harmony.CreateAndPatchAll(typeof(ConsoleColourer));
            ConsoleColourer.consoleColor = color;
            logger.LogError(data);
            getConsoleColorHarmonyInstance.UnpatchSelf();
        }

        public static void LogFatalWithColor(this ManualLogSource logger, object data, ConsoleColor color)
        {
            getConsoleColorHarmonyInstance ??= new Harmony("GetConsoleColor");

            getConsoleColorHarmonyInstance = Harmony.CreateAndPatchAll(typeof(ConsoleColourer));
            ConsoleColourer.consoleColor = color;
            logger.LogFatal(data);
            getConsoleColorHarmonyInstance.UnpatchSelf();
        }

        public static void LogInfoWithColor(this ManualLogSource logger, object data, ConsoleColor color)
        {
            getConsoleColorHarmonyInstance ??= new Harmony("GetConsoleColor");

            getConsoleColorHarmonyInstance = Harmony.CreateAndPatchAll(typeof(ConsoleColourer));
            ConsoleColourer.consoleColor = color;
            logger.LogInfo(data);
            getConsoleColorHarmonyInstance.UnpatchSelf();
        }

        public static void LogMessageWithColor(this ManualLogSource logger, object data, ConsoleColor color)
        {
            getConsoleColorHarmonyInstance ??= new Harmony("GetConsoleColor");

            getConsoleColorHarmonyInstance = Harmony.CreateAndPatchAll(typeof(ConsoleColourer));
            ConsoleColourer.consoleColor = color;
            logger.LogMessage(data);
            getConsoleColorHarmonyInstance.UnpatchSelf();
        }

        public static void LogWarningWithColor(this ManualLogSource logger, object data, ConsoleColor color)
        {
            getConsoleColorHarmonyInstance ??= new Harmony("GetConsoleColor");

            getConsoleColorHarmonyInstance = Harmony.CreateAndPatchAll(typeof(ConsoleColourer));
            ConsoleColourer.consoleColor = color;
            logger.LogWarning(data);
            getConsoleColorHarmonyInstance.UnpatchSelf();
        }
    }

    public static class ConsoleColourer
    {
        internal static Type konType = null;

        [Obsolete("Not obselete but will cause errors in the Sodalite console. So use is only valid for startup :D")]
        public static ConsoleColor BackgroundColor
        {
            get { return (ConsoleColor)konType?.GetProperty("BackgroundColor").GetGetMethod(true).Invoke(null, null); }
            set { konType?.GetProperty("BackgroundColor").GetSetMethod(true).Invoke(null, new object[] { value }); }
        }

        internal static ConsoleColor consoleColor;

        [HarmonyPatch(typeof(LogLevelExtensions), nameof(LogLevelExtensions.GetConsoleColor))]
        [HarmonyPostfix]
        internal static void ChangeGetColorResult(ref ConsoleColor __result)
        {
            __result = consoleColor;
        }
    }

    public static class LogLevelStringChanger
    {
        internal static string levelName;

        [HarmonyPatch(typeof(LogEventArgs), nameof(LogEventArgs.ToString))]
        [HarmonyPostfix]
        internal static void ChangeLogEventArgsLevel(LogEventArgs __instance, ref string __result)
        {
            __result = string.Format("[{0,-7}:{1,10}] {2}", levelName, __instance.Source.SourceName, __instance.Data);
        }
    }
}
