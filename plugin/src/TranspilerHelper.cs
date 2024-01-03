using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace CiarencesUnbelievableModifications
{
    public static class TranspilerHelper
    {
        public static bool TryMatchForward(bool useEnd, IEnumerable<CodeInstruction> instructions, ILGenerator generator, out CodeMatcher codeMatcher, MethodBase __originalMethod, Action<string> logger = null, params CodeMatch[] codeMatches)
        {
            codeMatcher = new CodeMatcher(instructions, generator).MatchForward(useEnd, codeMatches);

            if (logger == null) logger = Debug.LogError;
            return (!codeMatcher.ReportFailure(__originalMethod, logger));
        }

        public static bool TryMatchForward(this CodeMatcher codeMatcher, bool useEnd, MethodBase __originalMethod, params CodeMatch[] codeMatches)
        {
            codeMatcher.MatchForward(useEnd, codeMatches);

            return (!codeMatcher.ReportFailure(__originalMethod, CiarencesUnbelievableModifications.Logger.LogError));
        }

        public static void Print(this CodeMatcher codeMatcher)
        {
            var instructs = codeMatcher.Instructions().ToArray();
            for (int i = 0; i < instructs.Length; i++)
            {
                CiarencesUnbelievableModifications.Logger.LogInfo(instructs[i].ToString());
            }
        }
    }
}
